# Plan: Fix NPC Vanilla Schedule Resume After Encounter (v4)

## TL;DR

The issue persists after v3 because two vanilla SDV fields are NOT being cleared when NPCs resume after encounters:

1. **`textAboveHeadTimer`** — Set by `showTextAboveHead()` to 900-3000ms depending on text length. Vanilla SDV prevents NPC movement while this timer is active. The encounter completes immediately after the last bubble is queued, but the timer is still counting down. The last speaker always has an active `textAboveHeadTimer` when `TryResumeEncounterParticipants` is called.

2. **`textAboveHeadAlpha`** — Controls text fade-out. While this is fading, vanilla behavior may still treat the NPC as "displaying text" and block certain movements.

The "walking through walls in a straight line" behavior occurs because after `textAboveHeadTimer` expires, the NPC tries to move but `previousEndPoint` was reset before the NPC's position was finalized (or the NPC is still in an encounter-pinned state).

## Root Cause

### Encounter Completion Timing

```
// NpcSpeechBubbleService.cs:217
_encounterNextDisplayUtc[encId] = DateTime.UtcNow.AddMilliseconds(durationMs + EncounterBubblePauseBetweenMs);
```

When the LAST bubble is displayed:
1. `_encounterDisplayIndex[encId]` is incremented to `list.Count`
2. `IsEncounterReadyForNextBubble()` returns `true` because `idx >= list.Count` (no next bubble to wait for)
3. Encounter completes immediately via `TryResumeEncounterParticipants`
4. **But `textAboveHeadTimer` on the last speaker is still counting down!** (900-3000ms)

### Asymmetry

The "last speaker stuck" happens because:
- First speaker's `textAboveHeadTimer` expires naturally during the conversation
- Last speaker's `textAboveHeadTimer` is set when they speak, then encounter completes immediately
- The timer is still blocking movement when schedule resume happens

### Wall-Phasing

After `textAboveHeadTimer` expires, the NPC tries to move but:
- `previousEndPoint` may have been set to wrong position
- Or other vanilla routing state (`DirectionsToNewLocation`, etc.) is stale

## Proposed Changes

### Step 1: Clear `textAboveHeadTimer` in `HandoffNpcToVanillaAfterEncounter`

**File:** `mod/StardewLivingRPG/ModEntry.cs`
**Lines:** 14942-14944

```diff
 npc.controller = null;
 TrySetMemberValue(npc, "temporaryController", null);
 TrySetMemberValue(npc, "followSchedule", true);
+
+// Clear vanilla text display timer that blocks movement.
+// The last speaker's timer is still active when encounter completes.
+TrySetMemberValue(npc, "textAboveHeadTimer", 0);
+TrySetMemberValue(npc, "textAboveHeadAlpha", 0f);
```

### Step 2: Also clear in `TryRebindVanillaScheduleAtCurrentTime` (defense-in-depth)

**File:** `mod/StardewLivingRPG/ModEntry.cs`
**Lines:** 15071-15073

```diff
 TrySetMemberValue(npc, "lastAttemptedSchedule", -1);
 TrySetMemberValue(npc, "previousEndPoint", npc.TilePoint);
 TrySetMemberValue(npc, "currentScheduleDelay", 0.001f);
+// Clear any remaining text display state
+TrySetMemberValue(npc, "textAboveHeadTimer", 0);
+TrySetMemberValue(npc, "textAboveHeadAlpha", 0f);
 return "ScheduleRebound";
```

### Step 3: Clear other movement-blocking vanilla fields

Add these to both locations above to ensure vanilla state is clean:

```csharp
// Clear any position pinning from the encounter
TrySetMemberValue(npc, "isRaider", false);
TrySetMemberValue(npc, "isCharging", false);
TrySetMemberValue(npc, "movementPause", 0);

// Ensure the NPC is not in a halted state
if (TryGetMemberValue(npc, "halted", out var halted) && halted is bool && (bool)halted)
    TrySetMemberValue(npc, "halted", false);
```

### Step 4: (Optional) Add delay after encounter completion

If the above doesn't fully resolve the issue, add a small delay after the last bubble before completing the encounter:

**File:** `mod/StardewLivingRPG/ModEntry.cs`
**Lines:** 14278-14285

```diff
 if (_npcSpeechBubbleService.HasEncounterBubblesRemaining(encounter.EncounterId))
     continue;
 if (!_npcSpeechBubbleService.IsEncounterReadyForNextBubble(encounter.EncounterId))
     continue;
+
+// Add a brief pause after the last bubble to ensure text fade-out completes
+// before releasing NPCs. This prevents the "last speaker stuck" issue.
+var lastBubbleEndUtc = _npcSpeechBubbleService.GetLastBubbleEndUtc(encounter.EncounterId);
+if (lastBubbleEndUtc.HasValue && DateTime.UtcNow < lastBubbleEndUtc.Value)
+    continue;
+
 if (!_npcSpeechBubbleService.WereEncounterBubblesDisplayed(encounter.EncounterId))
```

This would require adding `GetLastBubbleEndUtc()` to `NpcSpeechBubbleService` to track when the last bubble actually finishes.

## Files to Modify

| File | Lines | Change |
|------|-------|--------|
| `ModEntry.cs` | 14942-14944 | Clear `textAboveHeadTimer` and `textAboveHeadAlpha` in `HandoffNpcToVanillaAfterEncounter` |
| `ModEntry.cs` | 15071-15073 | Clear `textAboveHeadTimer` and `textAboveHeadAlpha` in `TryRebindVanillaScheduleAtCurrentTime` |
| (Optional) `ModEntry.cs` | 14278-14285 | Add delay before encounter completion |
| (Optional) `NpcSpeechBubbleService.cs` | New method | Add `GetLastBubbleEndUtc()` to track last bubble completion time |

## Verification

1. **Build:** `dotnet build` from `mod/StardewLivingRPG/` - must compile clean

2. **Manual test:**
   - Launch game via SMAPI
   - Run `slrpg_demo_bootstrap` to trigger encounter
   - Wait for encounter to complete
   - **BOTH NPCs should start walking within ~1 second of the last speech bubble**
   - Neither NPC should stay frozen facing the encounter direction
   - No wall-phasing or straight-line walking through obstacles

3. **Edge cases:**
   - Trigger multiple encounters in sequence
   - Trigger encounter right before schedule transition time (e.g., 1159 -> 1200)
   - Verify NPCs don't get stuck in `textAboveHeadTimer` state

## Why This Should Work

1. **Clearing `textAboveHeadTimer`** removes the immediate blocker preventing the last speaker from moving
2. **Clearing `textAboveHeadAlpha`** ensures the text fade-out doesn't interfere
3. **Clearing other movement-blocking fields** provides defense-in-depth against vanilla state interference
4. **Optional delay** ensures the encounter doesn't complete until the text display is truly finished

The "walking through walls" issue should be resolved because the NPC can now move immediately when the schedule is rebound, rather than waiting for `textAboveHeadTimer` to expire (at which point other state may have changed).

## Status

- Implemented on 2026-03-20.
- Applied the planned release/rebind clearing for `textAboveHeadTimer`, `textAboveHeadAlpha`, `isRaider`, `isCharging`, `movementPause`, and `halted` in `ModEntry.cs`.
- The optional post-bubble delay in `NpcSpeechBubbleService` was not added in this revision.
