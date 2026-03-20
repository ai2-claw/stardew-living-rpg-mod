# Fix: NPC Conversation Memory Persistence Across Game Restarts

## Problem Statement
Users report that NPCs forget previous conversations after game restart.

## Root Cause Analysis

After thorough investigation, I found that **the persistence mechanism is working correctly**. The state is properly saved to disk via `StateStore.Save()` in `OnSaving` and loaded via `StateStore.LoadOrCreate()` in `OnSaveLoaded`. The `NpcMemoryState` with `RecentTurns` is correctly persisted.

The actual issue is that **the memory context sent to Player2 AI is too compressed to provide meaningful conversation continuity**.

### Key Findings

1. **Memory Storage Works**: `WriteTurn()` stores up to 40 conversation turns per NPC in `NpcMemoryProfile.RecentTurns`

2. **Memory Retrieval is Overly Compressed**: `BuildMemoryBlock()` (NpcMemoryService.cs:58-85) only sends:
   - Top 4 facts
   - Top 2 recent turns (out of 40 stored)
   - Each turn truncated to ~50 characters
   - Total block capped at 700 characters

3. **Conversations are Fragmented**: Turns are stored as separate entries:
   - Turn A: `PlayerText="Hello"`, `NpcText=""`
   - Turn B: `PlayerText=""`, `NpcText="Hi there!"`

   This makes it harder to connect questions with answers.

4. **Scoring Doesn't Favor Recency Enough**: `ScoreTurn()` gives 15 points for today's turn, but tag matching (+6 per match) can override recency.

---

## Implementation Plan

### Step 1: Add Turn Merging to NpcMemoryService

**File**: `mod/StardewLivingRPG/Systems/NpcMemoryService.cs`

Add a new method `CompleteTurn` that merges NPC responses into pending player turns:

```csharp
public void CompleteTurn(SaveState state, string npcName, string npcText, int day)
{
    if (string.IsNullOrWhiteSpace(npcName) || string.IsNullOrWhiteSpace(npcText))
        return;

    var profile = GetProfile(state, npcName);

    // Find most recent turn with empty NPC text from today
    var pendingTurn = profile.RecentTurns
        .LastOrDefault(t => t.Day == day && string.IsNullOrWhiteSpace(t.NpcText) && !string.IsNullOrWhiteSpace(t.PlayerText));

    if (pendingTurn is not null)
    {
        // Merge into existing turn
        pendingTurn.NpcText = npcText.Trim();
        profile.LastUpdatedDay = day;
        return;
    }

    // No pending turn, write as new turn with empty player text (edge case)
    WriteTurn(state, npcName, string.Empty, npcText, day);
}
```

### Step 2: Update ModEntry to Use CompleteTurn

**File**: `mod/StardewLivingRPG/ModEntry.cs`

At line ~16639, change from `WriteTurn` to `CompleteTurn`:

```csharp
// Before:
_npcMemoryService.WriteTurn(_state, npcName, string.Empty, playerFacingMsg, _state.Calendar.Day);

// After:
_npcMemoryService.CompleteTurn(_state, npcName, playerFacingMsg, _state.Calendar.Day);
```

### Step 3: Increase Memory Block Capacity

**File**: `mod/StardewLivingRPG/Systems/NpcMemoryService.cs`

Update `BuildMemoryBlock` method (line 58):

```csharp
// Change default parameters:
public string BuildMemoryBlock(SaveState state, string npcName, string playerText, int day,
    int topK = 8,      // was 4
    int charCap = 1500) // was 700
```

Update turn selection (line 70-77):
```csharp
var scoredTurns = profile.RecentTurns
    .Select(t => new { t, score = ScoreTurn(t, tags, day) })
    .OrderByDescending(x => x.score)
    .Take(6)  // was 2
    .Select(x => string.IsNullOrWhiteSpace(x.t.NpcText)
        ? $"Recent: Player said '{TrimForPrompt(x.t.PlayerText, 100)}'."  // was 70
        : $"Recent: Player '{TrimForPrompt(x.t.PlayerText, 80)}' / NPC '{TrimForPrompt(x.t.NpcText, 80)}'.")  // was 50
    .ToList();
```

### Step 4: Improve Turn Scoring

**File**: `mod/StardewLivingRPG/Systems/NpcMemoryService.cs`

Update `ScoreTurn` method (line 109):

```csharp
private static int ScoreTurn(NpcMemoryTurn t, HashSet<string> tags, int day)
{
    var age = Math.Max(0, day - t.Day);
    var score = Math.Max(0, 25 - age);  // was 15

    // Bonus for complete turns (both sides of conversation)
    if (!string.IsNullOrWhiteSpace(t.PlayerText) && !string.IsNullOrWhiteSpace(t.NpcText))
        score += 10;

    foreach (var tag in tags)
    {
        if (t.PlayerText.Contains(tag, StringComparison.OrdinalIgnoreCase) ||
            t.NpcText.Contains(tag, StringComparison.OrdinalIgnoreCase))
            score += 6;
    }

    return score;
}
```

---

## Critical Files

| File | Change |
|------|--------|
| `mod/StardewLivingRPG/Systems/NpcMemoryService.cs` | Add `CompleteTurn`, update `BuildMemoryBlock`, update `ScoreTurn` |
| `mod/StardewLivingRPG/ModEntry.cs:16639` | Use `CompleteTurn` instead of `WriteTurn` for NPC responses |

---

## Verification

1. **Build**: `dotnet build`
2. **In-game test**:
   - Talk to an NPC, have a conversation about a specific topic
   - Save game and exit
   - Restart game, load save
   - Talk to same NPC again
   - NPC should reference or continue from previous conversation
3. **Console verification**: Run `slrpg_debug_state` command to see NPC memory stats
4. **Log check**: Monitor SMAPI logs for `NPC_MEMORY` block content being sent to Player2
