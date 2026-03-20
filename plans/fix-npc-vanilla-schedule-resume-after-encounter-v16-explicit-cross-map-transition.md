# Fix Cross-Map Encounter Resume by Explicitly Executing Map Transitions

## Summary
The real bug is not just bad door arrival tiles. The current cross-map fallback walks NPCs to a departure tile, then waits for a map change that never happens. The fix should make cross-map fallback a two-step state machine:

1. walk to the current leg's transition point on the current map;
2. explicitly warp to the next map using the route edge's arrival tile, then continue the next leg or final same-map target.

This should be paired with better door-edge arrival-tile resolution so door routes do not carry `WarpToTile = (0,0)` when a reverse door exists.

## Key Changes
- In `ModEntry.cs`, change cross-map fallback from "wait for map change" to "perform map transition when leg is reached".
  - Keep vanilla `checkSchedule(...)` first and same-map active-slot fallback unchanged.
  - For cross-map fallback legs, store both:
    - the actual transition tile from topology (`edge.WarpFromTile`)
    - the walkable approach tile used for the controller
  - When the NPC reaches the approach tile and is within transition tolerance of the real transition tile, clear the leg controller and call `_executionService.WarpNpcTo(...)` to the next map.
  - After warp:
    - if the NPC is now on the active target map, switch to the existing same-map active-slot fallback;
    - otherwise plan the next cross-map leg and continue.
  - Do not keep waiting for an automatic door/warp handoff from the controller.

- In `WorldTopologyService.cs`, improve edge data for door-based transitions.
  - Build a location lookup once during graph construction.
  - For door edges, resolve `WarpToTile` by looking for a reverse door in the destination map that points back to the source map.
  - If no reverse door exists, leave the edge usable by falling back later during explicit warp handling instead of assuming `(0,0)` is meaningful.
  - Add a low-noise debug/warn log when a door edge has no reverse arrival tile.

- In the pending encounter-resume state in `ModEntry.cs`, extend fallback-leg state so the worker can distinguish routing vs transition.
  - Add fields for:
    - `FallbackLegTransitionTile`
    - `FallbackLegApproachTile`
    - `FallbackLegArrivalTileResolved`
    - `FallbackWarpIssued`
  - Keep the existing stale-leg tracking, but reset it after a successful warp so the next leg starts fresh.

- Add a single explicit arrival-tile resolver for cross-map fallback.
  - Resolution order:
    1. topology edge `WarpToTile` when non-zero;
    2. reverse door / reverse warp lookup if needed;
    3. safe fallback tile on the destination map using existing walkability logic.
  - Never rely on `Point.Zero` as a usable arrival tile.
  - Use `_executionService.WarpNpcTo(...)` for the actual transfer so destination tiles are validated the same way as other repo warp flows.

- Update logs so the cross-map state machine is auditable.
  - Add logs for:
    - `CrossMapLeg(transition_ready)` when the NPC reaches the leg handoff point
    - `CrossMapLeg(warping)` with `from`, `to`, `transition_tile`, `approach_tile`, `arrival_tile`
    - existing `CrossMapLeg(warped)` after the map actually changes
  - Keep existing stale/retry logs, but they should now mean "could not reach transition point", not "stood on a door forever".

## Interfaces / Types
- No public API changes.
- Extend the private pending encounter-resume record in `ModEntry.cs` with separate transition-vs-approach tile state.
- Reuse `NpcAutonomyExecutionService.WarpNpcTo(...)` as the only explicit map-transfer mechanism for encounter fallback.

## Test Plan
- Shane in Town -> JojaMart:
  - encounter ends around `920`
  - expect `CrossMapLeg(start)` with a valid transition tile
  - expect `CrossMapLeg(transition_ready)` when Shane reaches the JojaMart door
  - expect `CrossMapLeg(warping)` and then `CrossMapLeg(warped)` into `JojaMart`
  - expect same-map fallback inside `JojaMart` toward the active-slot tile `(9,17)`

- Sam/Vincent in SamHouse -> Town:
  - both already have valid `arrival_tile=(10,86)` in the latest logs
  - when they reach `(4,24)`, they should now warp instead of standing there
  - Vincent should then plan the next leg from `Town` toward `ArchaeologyHouse`

- Same-map regression:
  - cases like Caroline in `SeedShop` still use the existing same-map fallback only
  - Alex "already at active-slot destination" remains a no-op wait, with no extra controller or warp

- Door edge data regression:
  - routes with real warps still keep their existing `TargetX/TargetY`
  - door edges with reverse doors should no longer log `arrival_tile=(0,0)`
  - one-way / unresolved doors should still function through explicit arrival fallback instead of hanging

- Stale-leg recovery:
  - if an NPC never reaches the approach tile, stale detection and one retry still apply
  - if an NPC reaches the approach tile but warp fails because destination lookup is unavailable, log it and leave the NPC pending for the next retry rather than spinning or soft-locking

## Assumptions and Edge Cases
- The universal failure mode is "cross-map fallback reaches the leg endpoint but never performs the transition", not just Shane's missing door arrival tile.
- Explicit warp on cross-map fallback is acceptable here because the mod is already using temporary fallback controllers and already has a repo-standard warp helper.
- Important edge cases to cover:
  - non-walkable transition tiles: path to a nearby walkable approach tile, but warp using the real transition tile semantics;
  - one-way doors or missing reverse-door metadata: resolve a safe arrival tile with walkability fallback;
  - schedule boundary changes mid-leg: keep the existing "clear fallback and return control to vanilla at the next slot" behavior;
  - repeated warp attempts: guard with per-leg state so the worker does not issue duplicate warps on successive ticks.
