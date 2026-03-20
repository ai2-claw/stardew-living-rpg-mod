# Plan: Fix NPC Vanilla Schedule Resume After Encounter (v8 - The Real Bug)

## TL;DR

**The actual bug:** Encounters that complete via Player2 conversation are stuck in `Staging` phase and never reach the completion logic in `Talking` phase.

**Line 14268 has a redundant `continue`** that prevents encounters from advancing from `Staging` to `Talking` phase.

**The fix:** Remove the redundant `continue` statement on line 14268.

## What the Logs Revealed

```
enc_1 (Demetrius->Robin): CANCELLED → went through cancellation path → HANDOFF/REBIND worked
enc_5 (Alex->Shane): Player2 conversation completed → NO scene completion → stuck
enc_6 (Sam->Vincent): Player2 conversation completed → NO scene completion → stuck
```

**Key logs:**
```
[23:19:13] Alex->Shane staged successfully, starting conversation.
[23:19:13] Alex->Shane Player2 encounter conversation launched (turns=4).
[23:19:26] Encounter conversation completed: Alex->Shane enc=5 turns=4/4.
[MISSING] Player2 encounter enc_5 Alex->Shane completed ← NEVER HAPPENS
[MISSING] HANDOFF/REBIND logs ← NEVER CALLED
```

## The Bug

**File:** `mod/StardewLivingRPG/ModEntry.cs`
**Lines:** 14261-14273

```csharp
// Player2 encounters: transcript arrives via _pendingEncounterConversationCompletions queue,
// then gets queued as encounter bubbles — just wait for bubbles to arrive
if (isPlayer2Encounter)
{
    TryFlushDeferredEncounterConversationCompletion(encounter);
    if (_npcSpeechBubbleService.HasEncounterBubblesRemaining(encounter.EncounterId))
        continue;
    continue;  // ← BUG! This ALWAYS skips, even when bubbles are done
}

// Non-Player2 encounters should not reach here — cancel as safety net
CancelEncounterScene(encounter, "no_player2_unexpected");
```

**Why it breaks:**
1. This code is in the `Staging` phase section
2. It waits for bubbles to be queued
3. **BUG: The second `continue` always skips to next iteration**
4. Encounters never advance to `Talking` phase
5. Encounters never reach completion logic (line 14286+)
6. `TryResumeEncounterParticipants` is never called
7. NPCs never resume their schedules

**Why cancelled encounters work:**
- Cancelled encounters go through a different code path that calls `TryResumeEncounterParticipants` directly
- They bypass the `Staging` → `Talking` phase transition

## The Fix

**File:** `mod/StardewLivingRPG/ModEntry.cs`
**Lines:** 14261-14273

```diff
 // Player2 encounters: transcript arrives via _pendingEncounterConversationCompletions queue,
 // then gets queued as encounter bubbles — just wait for bubbles to arrive
 if (isPlayer2Encounter)
 {
     TryFlushDeferredEncounterConversationCompletion(encounter);
     if (_npcSpeechBubbleService.HasEncounterBubblesRemaining(encounter.EncounterId))
         continue;
-    continue;
+    // No bubbles remaining — advance to Talking phase where completion logic runs
 }
```

By removing the redundant `continue`, the code falls through to the next iteration, which should advance the encounter to `Talking` phase (via other code not shown here).

## Verification

1. **Build:** `dotnet build` from `mod/StardewLivingRPG/` - must compile clean

2. **Manual test:**
   - Launch game via SMAPI
   - Run `slrpg_demo_bootstrap` to trigger encounter
   - Wait for encounter to complete
   - **Expected logs:**
     - "Autonomy: Player2 encounter enc_X Alex->Shane completed (outcome=friendly)"
     - "Autonomy: [HANDOFF] Alex starting handoff..."
     - "Autonomy: [REBIND] Alex starting rebind..."
     - "Autonomy: returned Alex to vanilla schedule..."
   - **Expected behavior:**
     - BOTH NPCs should start moving within a few seconds after last bubble
     - Neither NPC should stay frozen
     - No wall-phasing

3. **Regression check:**
   - Verify cancelled encounters still work (like enc_1)
   - Verify NPCs don't walk through walls
   - Verify all encounter types complete successfully

## Why This Will Work

1. **Removing the redundant `continue`** allows encounters to progress to `Talking` phase
2. **In `Talking` phase**, encounters reach the completion logic (line 14286+)
3. **Completion logic** calls `TryResumeEncounterParticipants`
4. **Resume logic** calls `TryRebindVanillaScheduleAtCurrentTime` with v7b's direct `checkSchedule` call
5. **NPCs resume** their vanilla schedules immediately

## Files to Modify

| File | Lines | Change |
|------|-------|--------|
| `ModEntry.cs` | 14268 | **DELETE** the redundant `continue;` statement |

## Additional Notes

The v7b implementation (direct `checkSchedule` call) is correct and should work once encounters actually complete. The diagnostic logs from v6 are helpful and can be kept for future debugging.

The bug on line 14268 was likely introduced during a refactoring when merging multiple code paths. The second `continue` is clearly a typo as it defeats the purpose of the bubble check above it.
