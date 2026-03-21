# Fix Encounter Start Delay by Streaming Player2 Turns into Bubbles

## Summary
The long visible pause before face-to-face encounters start talking is mostly local buffering, not staging. The encounter is staged and the Player2 task is launched immediately, but no speech bubbles are queued until the full multi-turn transcript finishes.

Fix this by changing Player2 encounter dialogue from full-transcript buffering to incremental per-turn delivery:
- queue each completed turn to the main thread as soon as Player2 returns it;
- show the first bubble after the first successful turn instead of waiting for all turns;
- keep encounter closeout gated on both final task completion and bubble drain.

This does not remove first-turn Player2 latency. It removes the self-inflicted wait for turns 2-4 before showing anything.

## Key Changes

### Incremental turn delivery
- Add one internal main-thread queue for encounter turns, separate from the existing final completion queue.
- Add one internal payload type for a single completed turn, carrying:
  - `EncounterId`
  - `Turn`
  - speaker/listener ids and short names
  - final spoken line text after command-output repair
- Keep all SMAPI/game-state mutation on the main thread:
  - the background task may only enqueue turn payloads and the final completion payload;
  - bubble queuing, encounter progress updates, cancellation checks, and memory/state writes stay on the main thread.

### Background task behavior
- In `RunEncounterConversationAsync(...)`, after each turn succeeds and passes command-output repair, enqueue that single turn immediately.
- Keep collecting the local `transcript` list for final summary/memory use.
- Remove background-thread `UpdateEncounterProgress(...)`; move that update to the main-thread turn consumer.
- If a later turn fails:
  - preserve already emitted earlier turns;
  - finish with a final aborted completion payload;
  - do not retroactively cancel already shown dialogue.

### Main-thread turn consumer
- Add a main-thread consumer such as `TryApplyPendingEncounterConversationTurns()` that runs every update before encounter completion checks.
- For each pending turn:
  - confirm the encounter still exists and is not cancelled/released;
  - validate speaker/listener drift the same way final transcript application already does;
  - update encounter progress on the main thread;
  - queue the line immediately with `NpcSpeechBubbleService.QueueEncounterBubble(...)`.
- Add per-encounter turn idempotency:
  - track highest applied turn number per encounter;
  - drop duplicate turns;
  - reject or log out-of-order turns instead of double-queuing bubbles.

### Final completion and Talking-phase rules
- Keep `EncounterConversationCompletion`, but make it status-only for encounter finalization and summary.
- `TryApplyEncounterConversationCompletion(...)` must stop queuing transcript lines once incremental turns exist; it should only:
  - record final success/failure state;
  - handle memories/summary data;
  - mark the encounter task stream as done.
- Add explicit per-encounter final-status state on the main thread:
  - `turn_stream_complete`
  - optional `turn_stream_failed`
- Update `TryAdvanceAutonomyEncounterStates()` completion logic:
  - while no turns have arrived yet: wait for first Player2 turn;
  - after some turns have arrived but final completion has not: wait for more turns;
  - after final completion arrives: wait for queued bubbles to finish;
  - only then complete the encounter.
- Completion must no longer rely only on `WereEncounterBubblesEverQueued()` as the proxy for “Player2 is done.”

### Cancellation and failure behavior
- If an encounter is cancelled after some turns were already queued:
  - drop any later pending turn payloads for that encounter;
  - clear remaining queued encounter bubbles through the existing bubble-forget path;
  - accept that already shown lines were already visible and do not try to retract them.
- If the final completion is aborted after some turns were shown:
  - do not hard-cancel the visible exchange;
  - let already queued bubbles finish;
  - then resolve the encounter with the existing safe abort/neutral handling instead of hanging.

### Logging
- Replace the current ambiguous pre-first-bubble waiting logs with explicit states:
  - `waiting_on_first_player2_turn`
  - `waiting_on_more_player2_turns`
  - `waiting_on_queued_bubbles_to_finish`
- Add incremental turn logs only at debug/trace level, one per applied turn.
- Keep the existing final encounter completion log.

## Interfaces / Types
- Add one new internal type, for example `EncounterConversationTurn`.
- Add one internal queue for pending encounter turns.
- Add one small per-encounter state record for:
  - highest applied turn
  - whether final completion has arrived
  - whether final completion succeeded or aborted
- No Player2 API changes.
- No GMCM/config changes.

## Test Plan
- Normal 4-turn Player2 encounter:
  - staging and Player2 launch still happen immediately;
  - first visible bubble appears after the first returned turn, not after full transcript completion;
  - later turns continue to appear in order.

- Ordering and chunking:
  - if a turn splits into multiple bubble chunks, all chunks of turn 1 appear before turn 2, etc.;
  - no duplicate bubbles when the final completion payload is applied.

- Partial failure:
  - if turn 3 or 4 fails, turns 1-2 remain visible;
  - encounter ends cleanly as shortened/aborted after queued bubbles finish;
  - no permanent Talking-phase hang.

- Cancellation race:
  - if encounter is cancelled after turn 1 was queued, later turn payloads are ignored;
  - remaining queued encounter bubbles are cleared correctly;
  - no wrong-pair bubble leakage after cancel/release.

- First-turn wait logs:
  - before any turn arrives, logs should indicate waiting on first turn, not generic bubble wait;
  - after final completion arrives, logs should indicate waiting only on bubble drain.

- Verification:
  - `dotnet build` passes;
  - no config changes;
  - no fake placeholder speech is introduced.

## Assumptions
- The dominant startup delay is caused by local full-transcript buffering, not face-to-face staging.
- Per-turn incremental display is enough; true token streaming from Player2 is not required for this fix.
- Remaining latency up to the first Player2 turn is acceptable in this change.
