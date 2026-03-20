# Fix Player2 Encounter Hang by Preserving Bubble Completion History Until Scene Release

## Summary
The v8 plan is not sound.

`MarkTalking(...)` already mutates the live encounter object in `NpcSocialEncounterService`, so the extra `continue` in the `Staging` block does not trap encounters there. The stronger code-and-log match is elsewhere:

- Player2 encounters do reach transcript completion.
- No later completion log or cancel log appears.
- `NpcSpeechBubbleService.ClearEncounterState(...)` deletes `_encounterBubblesEverQueued` and `_encounterBubblesDisplayed`.
- `TryAdvanceAutonomyEncounterStates()` relies on those flags to decide whether a Player2 encounter can complete.

That means once the final bubble expires, or if the bubble queue is cleared early, the encounter forgets that bubbles were ever queued/displayed and can sit in `Talking` forever.

## Key Changes
- Leave the `Staging` block in `ModEntry.cs` unchanged.
  - Do not implement the v8 “delete the redundant continue” change as the primary fix.
  - `MarkTalking(...)` already moves the encounter into `EncounterPhase.Talking` for the next tick.

- Split encounter bubble cleanup in `NpcSpeechBubbleService.cs` into two paths:
  - `ClearEncounterActiveState(encounterId)`:
    - remove only active queue/runtime fields:
      - `_encounterBubbles`
      - `_encounterDisplayIndex`
      - `_encounterNextDisplayUtc`
      - `_encounterLastBubbleEndUtc`
    - keep:
      - `_encounterBubblesEverQueued`
      - `_encounterBubblesDisplayed`
  - `ForgetEncounter(encounterId)`:
    - remove all encounter bubble state, including the two history flags
    - use this only when the encounter is fully released/cancelled and the system is done needing completion history

- Change the encounter bubble tick behavior to preserve history through completion.
  - When the final bubble has finished and the active queue is exhausted, call `ClearEncounterActiveState(...)`, not full forget.
  - When a speaker becomes invalid, unavailable, or the queue data is malformed, also clear only active state so the encounter loop can detect `WereEncounterBubblesEverQueued == true` plus `WereEncounterBubblesDisplayed == false` and cancel with `bubble_display_failed` instead of hanging silently.

- Update explicit cleanup call sites in `ModEntry.cs`.
  - `CancelEncounterScene(...)` should call the full-forget path because the encounter is terminal.
  - Successful Player2 completion should also fully forget the bubble history after:
    - consequence processing
    - `TryResumeEncounterParticipants(...)`
    - `ClearEncounterRuntimeLinks(...)`
  - Do not leave historical bubble flags resident after a completed/cancelled encounter has been finalized.

- Add a small amount of targeted diagnostics.
  - In `NpcSpeechBubbleService`, log when active encounter bubble state is cleared without full forget, including a short reason such as:
    - `final_bubble_finished`
    - `invalid_speaker`
    - `speaker_missing`
    - `bubble_list_missing`
  - In `TryAdvanceAutonomyEncounterStates()`, add one debug/trace log before the Player2 `continue` gates when an encounter is still waiting, including:
    - `ever_queued`
    - `remaining`
    - `ready_next`
    - `last_finished`
    - `displayed`
  - Keep this concise so future hangs can be diagnosed from one log slice.

## Interfaces / Types
- No public API changes.
- Add one new internal/full cleanup method in `NpcSpeechBubbleService`:
  - `ForgetEncounter(string encounterId)`
- Rename or replace the current private `ClearEncounterState(...)` with the split active-state/full-forget behavior so the cleanup intent is explicit.

## Test Plan
- Player2 encounter normal completion:
  - trigger Alex->Shane or Sam->Vincent
  - expect:
    - `Encounter conversation completed ...`
    - later `Autonomy: Player2 encounter enc_X ... completed`
    - later `[HANDOFF]` / `[REBIND]`
  - NPCs should no longer remain face-to-face forever after the last bubble

- Final bubble expiry path:
  - confirm the last bubble disappears naturally
  - confirm the next update completes the encounter instead of hanging in `Talking`

- Bubble invalidation path:
  - force a case where the speaker becomes invalid/unavailable before display
  - expect a logged cancel such as `bubble_display_failed`
  - do not allow the encounter to wait forever with no follow-up log

- Cancellation regression:
  - Player2-unavailable cancellation must still release NPCs and fully forget bubble state
  - no stale `_encounterBubblesEverQueued` / `_encounterBubblesDisplayed` should survive into later unrelated encounters

- Verification:
  - `dotnet build` passes
  - no changes to the existing vanilla `checkSchedule(...)` resume worker are required for this fix beyond confirming it still runs after successful encounter completion

## Assumptions
- The latest `smapi-logs.md` is authoritative.
- The missing completion/cancel logs after `Encounter conversation completed ...` indicate a silent wait in the Player2 bubble-completion gate, not a resume-worker failure.
- The previously added direct `checkSchedule(...)` resume logic can remain in place; the immediate blocker is that Player2 encounters are not consistently reaching the completion/handoff path.
