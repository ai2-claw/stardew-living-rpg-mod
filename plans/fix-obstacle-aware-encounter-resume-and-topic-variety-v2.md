# Fix Obstacle-Aware Encounter Resume, Schedule-Aware Endings, and Topic Repetition

## Summary
Keep NPCs' real scheduled destination tiles unchanged. Fix the post-encounter movement bug by replacing the current guessed direct-target fallback with obstacle-aware same-map routing, while keeping temporary movement controllers and existing cross-map topology/warp behavior.

Also improve encounter dialogue so NPCs reference their real current/next schedule location at conversation end, and reduce repeated "same rumor everywhere" chatter by rotating headlines, events, autonomy hooks, and rumors with per-topic cooldown.

## Key Changes

### 1. Obstacle-aware same-map routing for encounter recovery
- In `ModEntry.cs`, replace the current direct temporary-controller fallback with an A* same-map route planner.
- Use `NpcWalkabilityService.IsTileWalkable(...)` as the tile truth source for A* so furniture, counters, crates, trash cans, water, no-path tiles, terrain, buildings, and collisions are all respected.
- Apply the same A* planner to both:
  - same-map movement to the active scheduled tile;
  - same-map movement to a cross-map leg's departure/transition tile.
- Keep cross-map topology selection and explicit warp handling unchanged; only the on-map travel changes.

### 2. Execute A* routes through short validated controller legs
- Do not create one controller directly to the final schedule tile or door tile.
- Convert the A* tile route into short waypoint legs and execute them one at a time with temporary controllers.
- Waypoint compression rules:
  - only compress along straight segments;
  - every intermediate tile in a compressed segment must still be walkable;
  - do not skip across counters, furniture edges, choke points, or obstacle boundaries.
- Remove the current heuristic `PathFindController` constructor guessing logic.
- Replace it with support for only explicitly verified constructor shapes; if no known-safe controller shape can be built for a waypoint leg, stop fallback for that NPC instead of guessing.

### 3. Repathing, degraded arrival, and schedule-boundary control
- Add bounded same-map repath behavior:
  - if the NPC stops making progress on the current fallback leg, recompute A* from the current tile to the same target;
  - cap repaths per slot window to avoid endless churn.
- Target rule:
  - always try the exact scheduled tile first;
  - only if no valid route exists, try a very small-radius adjacent walkable tile around that exact destination;
  - log this as degraded so it remains exceptional.
- If the next schedule time arrives while the NPC is still on a fallback route:
  - clear the current fallback controller and pending waypoint path immediately;
  - recompute for the new active schedule slot;
  - do not let the old route continue past the slot boundary.

### 4. Schedule-aware encounter endings
- Add an encounter-only schedule context block for `npc_encounter_dialogue` that includes, for both speaker and listener:
  - current location;
  - current active schedule slot location/tile when readable;
  - next immediate later schedule slot location/tile when readable.
- Update encounter ending instructions so they use only that schedule data:
  - if the NPC is staying at the current work/place, "back to it" style endings are allowed;
  - if the NPC is heading elsewhere next, the line should reference that real destination;
  - do not derive closing lines from detour reasons, autonomy hook text, or stale fallback state.

### 5. Reduce repeated rumor dominance
- Replace rumor-first topic seeding for NPC-NPC ambient and encounter dialogue with weighted topic-source rotation.
- Candidate sources:
  - today's newspaper headline / strongest current article;
  - recent non-rumor public event;
  - autonomy/location hook;
  - recent rumor;
  - generic local fallback.
- Add per-day exact-topic usage tracking keyed by normalized topic text plus source type.
- Weight rules:
  - unused headline/article first;
  - unused public event next;
  - autonomy hook in the middle;
  - rumor allowed but not hard-first;
  - repeated exact rumor/headline/event topic gets a steep same-day penalty.

## Interfaces / Types
- Add one private same-map A* route planner.
- Extend pending encounter-resume state with current waypoint route, current waypoint index, repath count, and last-progress tick.
- Replace generic controller-constructor guessing with a small set of verified constructor handlers.
- Add one private encounter schedule-context builder for `npc_encounter_dialogue`.
- Add one private topic-usage tracker keyed by normalized topic text and source type.
- No public API or config changes.

## Test Plan
- Interior obstacle cases:
  - Saloon bar counter, SeedShop displays, Blacksmith/Hospital counters, planters, crates, trash cans, water edges.
  - NPCs must route around them to their real scheduled tile after encounters.
- Cross-map case:
  - NPC must A* to the correct departure tile, warp, then A* on the destination map.
- Dynamic blocker case:
  - if another NPC blocks the route, stale detection should trigger repath instead of straight-line clipping.
- Exact destination preservation:
  - exact scheduled tile should remain the target;
  - adjacent degraded tile should be used only when the exact tile is temporarily unreachable.
- Dialogue ending correctness:
  - no more lines that reference the wrong place when the active/next schedule says otherwise.
- Topic variety:
  - headline/event topics should surface before the same rumor keeps repeating through the whole town.
- Build:
  - `dotnet build` passes.

## Assumptions
- The real movement bug is in route execution, not destination choice.
- Temporary controllers remain acceptable as long as they execute short, obstacle-aware A* legs instead of guessed direct-target movement.
- Exact scheduled destination tiles are normally valid; degraded adjacent arrival is only a safety fallback.
- Exact-topic cooldown is required, not just rumor-vs-headline category weighting.
