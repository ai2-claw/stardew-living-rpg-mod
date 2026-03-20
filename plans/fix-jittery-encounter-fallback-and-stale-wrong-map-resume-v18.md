# Fix Jittery Encounter Fallback Movement and Stale Wrong-Map Resume States

## Summary
The two regressions share one root cause: the encounter-resume worker currently owns movement too aggressively in the wrong places.

- The new A* fallback is being executed as one temporary controller per tile waypoint. That constantly resets facing/controller state, which causes sideways jitter and broken lateral walk animation.
- Morrow’s case proves the invalid-resume recovery is incomplete. The worker correctly detects the wrong Town-local endpoint, but after the first rejection it only logs repeated invalid state and lets the NPC keep moving on the stale bad path.

The fix should be:

1. stop executing A* as tile-by-tile controllers;
2. use A* only as route validation / target reachability if needed, but let a single vanilla-style `PathFindController` own each same-map leg;
3. make invalid-resume rejection take full ownership until the wrong movement is actually stopped or replaced.

## Key Changes

### 1. Replace waypoint-controller A* execution with single-leg controller execution
- In `ModEntry.cs`, remove the current waypoint path executor from encounter fallback:
  - no more one-controller-per-tile `FallbackWaypointPath` stepping;
  - no more controller replacement every tile in `TryAdvanceFallbackWaypointController(...)`.
- Keep the same encounter fallback structure:
  - same-map fallback to active schedule tile;
  - cross-map fallback to departure tile, warp, then final target-map fallback.
- For each same-map leg, create one controller to the resolved leg target and let that controller handle movement normally.
- Keep obstacle awareness by one of these two allowed paths:
  - preferred: use the vanilla `PathFindController` on the real target tile/departure tile and trust it to route around obstacles;
  - optional safety check: keep A* only to confirm a path exists before starting the controller, not to drive animation step-by-step.
- Preserve the real scheduled destination tile as the intended target.
  - only keep the current adjacent-walkable degraded fallback when the exact tile is genuinely unreachable.

### 2. Tighten controller creation so it never falls back to “guessed” movement
- Keep the post-encounter fallback controller approach; do not remove temporary controllers entirely.
- But change controller construction to support only explicitly verified constructor shapes.
- Do not use broad “best effort” integer stuffing beyond those verified signatures.
- If no known-safe `PathFindController` signature matches, do not create a fallback controller for that NPC on that attempt.
- This keeps obstacle-aware vanilla pathfinding while avoiding the old straight-line / wrong-constructor regression.

### 3. Fix invalid-resume ownership so wrong movement cannot keep running
- In `TryRejectInvalidResumeState(...)`, rejection must do more than clear `controller`.
- When a resume state is invalid:
  - clear `controller`;
  - clear `temporaryController`;
  - call `npc.Halt()` and clear any movement velocity / stale motion state that survives controller removal;
  - keep `followSchedule = true`;
  - immediately replace the bad state with the current-slot fallback if one can be started.
- If the same invalid state repeats within the same `timeOfDay`, do not merely “defer repeat invalid” while the NPC is still moving on the wrong endpoint.
  - repeated invalid state should either:
    - remain halted while pending a better retry, or
    - remain under the mod’s fallback controller ownership.
- The throttle should suppress repeated logging, not suppress corrective action.

### 4. Fix cross-map expected-leg validation to compare against the real current leg
- Morrow’s logs show `expected_leg=(43,56)` while he was moving toward `Town (23,2)`, which is one of his actual schedule tiles, not the correct current cross-map departure leg.
- Rework cross-map validation so it compares live movement against the currently owned fallback leg when one exists:
  - if the mod has started a cross-map fallback leg, validate against that exact leg’s departure/approach tile;
  - if the mod has not yet started one, compute the expected first leg from the current active schedule slot.
- This prevents stale or recomputed leg expectations from drifting away from the actual recovery target.

### 5. Keep the broad schedule-aware validation, but finalize only on real owned-good state
- Do not remove the schedule-aware validation added for Morrow-like bugs.
- For same-map slots, valid means:
  - vanilla controller endpoint matches the active target tile, or
  - the mod-owned same-map fallback controller is active for that same target.
- For cross-map slots, valid means:
  - the mod-owned current cross-map leg is active, or
  - the NPC has reached the target map / correct target-map endpoint.
- Invalid same-time repeat states should no longer continue visually moving after rejection.

## Test Plan

### Visual movement / animation
- Same-map post-encounter resume in interiors like `Saloon`, `SeedShop`, `Blacksmith`, `Hospital`.
- NPC should move with normal walking animation and facing transitions:
  - no sideways jitter from per-tile controller resets;
  - no “hopping/skipping” when moving left/right;
  - no rapid facing flicker while still moving forward.

### Obstacle safety
- NPC must still route around counters, furniture, planters, trash cans, and water.
- Exact scheduled destination tile remains the target unless runtime blockage forces degraded adjacent arrival.

### Morrow regression
- Reproduce Morrow encounter before `1200 SeedShop 10,23 / 1330 SeedShop 15,20`.
- If vanilla produces a wrong Town-local endpoint again, the worker must:
  - reject it;
  - stop the wrong movement immediately;
  - start or maintain the correct current-slot recovery path instead of letting him keep walking north of Town.
- He must no longer end up stranded at an unscheduled Town tile after rejection.

### Same-time repeated invalid state
- Cases like Morrow, Victor, Beckett from the latest log should not spam “deferred repeat invalid” while `isMoving=True` on the wrong endpoint.
- After first rejection, the NPC should either be halted or visibly on the correct fallback path.

### Cross-map regression
- Cross-map leg fallback still works for Shane/Sam/Vincent-style cases.
- Removing waypoint stepping must not break explicit warp transition handling.

### Verification
- `dotnet build` passes.
- No repo-tracked schedule content changes.
- No change to GMCM/config behavior.

## Assumptions
- The built-in `PathFindController`, when constructed with a verified safe signature, provides better obstacle-aware movement and animation than per-tile controller swapping.
- The jitter problem is caused by waypoint-controller churn, not by A* search itself.
- The Morrow bug persists because rejection throttling currently suppresses corrective ownership after the first rejection within the same clock time.
- Exact scheduled tiles remain the intended destinations; no curated replacement spots should be introduced.
