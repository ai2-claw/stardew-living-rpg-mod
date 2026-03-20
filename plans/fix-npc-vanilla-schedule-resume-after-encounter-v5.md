# Plan: Fix NPC Vanilla Schedule Resume After Encounter (v5)

## TL;DR

V4 failed because it cleared `textAboveHeadTimer` too early in `HandoffNpcToVanillaAfterEncounter`, cutting off the last bubble before the player can read it.

The real fix is:
1. Track when the last bubble **actually finishes displaying** (not when it's queued)
2. Delay encounter completion until after the last bubble finishes
3. Clear `textAboveHeadTimer` **only** in the schedule rebind step, not during handoff

## Why V4 Failed

```csharp
// ModEntry.cs:14945 - This happens IMMEDIATELY when encounter completes
ClearEncounterMovementBlockingState(npc);  // Clears textAboveHeadTimer = 0
```

This cuts off the last bubble because:
1. Last bubble is displayed with `textAboveHeadTimer = 2000ms`
2. Encounter completes immediately (all bubbles "queued")
3. `HandoffNpcToVanillaAfterEncounter` runs and clears timer to 0
4. Last bubble disappears instantly - player can't read it

## Root Cause

### Encounter Timing Issue

```
// NpcSpeechBubbleService.cs:217 - When displaying a bubble
_encounterNextDisplayUtc[encId] = DateTime.UtcNow.AddMilliseconds(durationMs + EncounterBubblePauseBetweenMs);
```

When the LAST bubble is displayed:
1. `_encounterDisplayIndex[encId]` becomes `list.Count` (all bubbles queued)
2. `IsEncounterReadyForNextBubble()` returns `true` (no "next" bubble to wait for)
3. Encounter completes immediately
4. BUT the last bubble's `textAboveHeadTimer` is still counting down!

The problem is `_encounterNextDisplayUtc` tracks when the **NEXT** bubble should display, not when the **current** bubble finishes. When there's no next bubble, we have no timestamp for when the last bubble actually finishes.

### NPC Resume Timing

```
HandoffNpcToVanillaAfterEncounter (immediate)
    → Sets NextAttemptTick = _lastUpdateTick + 1
    → Next tick: TryRebindVanillaScheduleAtCurrentTime
        → Resets lastAttemptedSchedule, previousEndPoint
        → BUT textAboveHeadTimer was cleared too early!
```

## Proposed Changes

### Step 1: Track last bubble finish time in `NpcSpeechBubbleService`

**File:** `mod/StardewLivingRPG/Systems/NpcSpeechBubbleService.cs`

Add new field and update the `Tick()` method:

```csharp
// Add new field (line ~38):
private readonly Dictionary<string, DateTime> _encounterLastBubbleEndUtc = new(StringComparer.OrdinalIgnoreCase);

// In Tick() method, after displaying encounter bubble (line ~218):
var durationMs = GetEncounterBubbleDurationMs(sanitized);
npc.showTextAboveHead(sanitized);
TrySetTextAboveHeadTimer(npc, durationMs);
_encounterBubblesDisplayed.Add(encId);
_encounterDisplayIndex[encId] = idx + 1;
_encounterNextDisplayUtc[encId] = DateTime.UtcNow.AddMilliseconds(durationMs + EncounterBubblePauseBetweenMs);

// NEW: Track when the last bubble finishes
if (idx + 1 >= list.Count)
{
    _encounterLastBubbleEndUtc[encId] = _encounterNextDisplayUtc[encId];
}

// Add new public method:
public bool IsLastBubbleFinished(string encounterId)
{
    return !_encounterLastBubbleEndUtc.TryGetValue(encounterId, out var endUtc) || DateTime.UtcNow >= endUtc;
}

// In ClearEncounterBubbles() method (line ~102), add cleanup:
_encounterLastBubbleEndUtc.Remove(encounterId);

// In CancelAll() method (line ~230), add:
_encounterLastBubbleEndUtc.Clear();
```

### Step 2: Delay encounter completion until last bubble finishes

**File:** `mod/StardewLivingRPG/ModEntry.cs`
**Lines:** 14276-14285 (in `TryAdvanceEncounters`)

```diff
 if (_npcSpeechBubbleService.HasEncounterBubblesRemaining(encounter.EncounterId))
     continue;
 if (!_npcSpeechBubbleService.IsEncounterReadyForNextBubble(encounter.EncounterId))
     continue;
+
+// Wait for the last bubble to finish displaying before completing the encounter.
+// This ensures the player can read the last line and the textAboveHeadTimer expires naturally.
+if (!_npcSpeechBubbleService.IsLastBubbleFinished(encounter.EncounterId))
+    continue;
+
 if (!_npcSpeechBubbleService.WereEncounterBubblesDisplayed(encounter.EncounterId))
 {
     CancelEncounterScene(encounter, "bubble_display_failed");
     continue;
 }
```

### Step 3: Remove timer clearing from `HandoffNpcToVanillaAfterEncounter`

**File:** `mod/StardewLivingRPG/ModEntry.cs`
**Lines:** 14942-14945

```diff
 npc.controller = null;
 TrySetMemberValue(npc, "temporaryController", null);
 TrySetMemberValue(npc, "followSchedule", true);
-ClearEncounterMovementBlockingState(npc);
+// DON'T clear textAboveHeadTimer here - let the last bubble finish naturally.
+// Only clear movement-blocking state flags, not the text display timer.
+TrySetMemberValue(npc, "isRaider", false);
+TrySetMemberValue(npc, "isCharging", false);
+TrySetMemberValue(npc, "movementPause", 0);
+if (TryGetMemberValue(npc, "halted", out var halted) && halted is bool && (bool)halted)
+    TrySetMemberValue(npc, "halted", false);
```

### Step 4: Keep timer clearing in `TryRebindVanillaScheduleAtCurrentTime`

The timer clearing in `TryRebindVanillaScheduleAtCurrentTime` is correct - it runs after the multi-tick delay, by which time the bubble has finished. Keep this as-is (lines 15071-15073 in v4).

## Files to Modify

| File | Lines | Change |
|------|-------|--------|
| `NpcSpeechBubbleService.cs` | ~38 (new field) | Add `_encounterLastBubbleEndUtc` dictionary |
| `NpcSpeechBubbleService.cs` | ~218 (Tick method) | Track last bubble finish time |
| `NpcSpeechBubbleService.cs` | New method | Add `IsLastBubbleFinished()` |
| `NpcSpeechBubbleService.cs` | ~102, ~230 | Add cleanup for new dictionary |
| `ModEntry.cs` | 14278-14285 | Add check for `IsLastBubbleFinished()` |
| `ModEntry.cs` | 14945 | Replace `ClearEncounterMovementBlockingState()` with selective field clearing |

## Verification

1. **Build:** `dotnet build` from `mod/StardewLivingRPG/` - must compile clean

2. **Manual test:**
   - Launch game via SMAPI
   - Run `slrpg_demo_bootstrap` to trigger encounter
   - Wait for encounter to complete
   - **Last bubble should remain visible for its full duration** (player can read it)
   - **BOTH NPCs should start walking within ~1 second after the last bubble fades**
   - Neither NPC should stay frozen facing the encounter direction
   - No wall-phasing or straight-line walking through obstacles

3. **Edge cases:**
   - Trigger multiple encounters in sequence
   - Trigger encounter right before schedule transition time (e.g., 1159 → 1200)
   - Verify NPCs don't get stuck

## Why This Should Work

1. **Tracking last bubble finish time** gives us a reliable timestamp for when the encounter can safely complete
2. **Delaying encounter completion** allows the last bubble to display fully and the timer to expire naturally
3. **Not clearing timer in handoff** prevents cutting off the last bubble
4. **Clearing timer in rebind** (v4's step 2) still happens as a safety net after the delay

The "last speaker stuck" issue is resolved because we now wait for their bubble to finish before resuming. The "walking through walls" issue should be resolved because the schedule rebind happens after the text timer has expired, when vanilla state is cleaner.
