# Cross-Map Autonomous NPC Movement And Face-To-Face Interaction

## Overview
The current autonomy implementation can choose intent, queue encounters, and show speech bubbles, but it does not yet satisfy the core feature because NPCs do not physically travel under self-directed schedules across the valley. This plan closes that gap by adding full cross-map movement, schedule rewriting, pathfinding, arrival staging, and face-to-face interaction execution.

The feature is only complete when NPCs can visibly or logically move from one location to another under autonomous plans, legally enter valid interiors, meet other NPCs in person, stop and face each other, and exchange sequential talk bubbles in-world.

## Goals
- Rewrite ordinary NPC schedules into autonomous movement-capable day plans.
- Support cross-map travel between public spaces, work interiors, and curated private homes.
- Execute real movement for loaded NPCs and coherent abstract travel for unloaded NPCs.
- Materialize NPCs at the correct location and time when the player enters a map.
- Stage face-to-face NPC interactions only when both NPCs are physically co-present.
- Preserve cozy readability while allowing meaningful schedule breaks and self-directed travel.
- Keep legality, routing, and state mutation deterministic even when Player2 suggests goals or dialogue.

## Non-Goals
- Replacing hard canon locks such as festivals, bedtime returns, or critical scripted scenes.
- Allowing unrestricted access to all interiors or all private homes.
- Letting Player2 directly own movement, pathing, or schedule legality.
- Simulating unloaded NPCs with perfect whole-world fidelity beyond what is needed for coherent player-facing behavior.

## Current Gap
- The existing autonomy layer can synthesize goals and runtime plan state.
- The mod does not yet own a full world-topology graph or route planner.
- NPCs do not physically execute self-directed cross-map plans.
- Ambient conversations can happen, but not yet because NPCs deliberately traveled to meet.
- Without movement execution, the feature remains system state and dialogue glue rather than living NPC behavior.

## Proposed Architecture

### 1. World Topology Layer
- Add `WorldTopologyService` to build a traversal graph from the live game world.
- Model nodes as:
  - public maps
  - semi-private interiors
  - curated private homes
  - key meeting spots within maps
  - doorway/warp connectors
- Model edges as:
  - direct warp links
  - interior entrance links
  - curated private access links
  - fallback public reroute links
- Each node or edge should expose:
  - location id
  - category
  - owner/household
  - opening hours
  - weather or season restrictions
  - event lock restrictions
  - pathability confidence

### 2. Route Planning Layer
- Add `NpcRoutePlannerService`.
- Route planning happens in two stages:
  1. world graph routing across maps
  2. local tile routing inside the active map
- Each route segment should include:
  - source location
  - destination location
  - optional warp/door target
  - target tile
  - estimated travel minutes
  - fallback segment if blocked
- Route plans must be deterministic and cacheable per day/block unless invalidated.

### 3. Autonomous Schedule Compilation
- Replace high-level autonomy goals with concrete timed movement blocks.
- Add or extend block types:
  - `travel`
  - `arrive`
  - `wait_for_target`
  - `visit_npc`
  - `socialize`
  - `wander`
  - `return_home`
- Each autonomous block must compile into:
  - destination location
  - destination tile
  - planned departure time
  - ETA
  - max slack/wait window
  - fallback route or fallback destination
- The planner should produce a real executable day plan, not only an intention list.

### 4. Schedule Rewrite Policy
- Use full rewrite for ordinary daily movement.
- Preserve vanilla or hard-lock blocks for:
  - festivals
  - bedtime/home return
  - critical scripted duties
  - player-sensitive interaction moments
- Hybrid rule:
  - hard locks become fixed anchors
  - free windows between anchors are fully autonomy-owned
- This keeps the valley coherent while still allowing genuinely new movement patterns.

### 5. Runtime Movement Execution
- Add `NpcAutonomyExecutionService`.
- For loaded NPCs:
  - create or refresh a live movement controller for the current route segment
  - detect stuck state, blocked paths, or target invalidation
  - advance block state on arrival
- For unloaded NPCs:
  - advance abstract route progress using in-game time and route ETA
  - keep runtime state synchronized so their expected position is stable
- Runtime state must track:
  - active route segment
  - last successful arrival
  - movement status
  - replan count
  - expected arrival time
  - last known location

### 6. Materialization And Offscreen Travel
- Add `NpcMaterializationService`.
- NPC travel should use a mixed strategy:
  - visible physical movement while relevant to the player
  - abstract progression when offscreen or on other maps
- On player map entry:
  - materialize the NPC at the correct location/tile if their route says they should be present
  - preserve arrival timing rather than snapping them arbitrarily
- Safe materialization rules:
  - only materialize at valid walkable tiles
  - avoid spawning on top of player or another NPC
  - respect privacy or event locks

### 7. Face-To-Face Encounter Staging
- Add `NpcFaceToFaceInteractionService`.
- Encounters should not begin until both NPCs:
  - are in the same location
  - are within encounter distance
  - have legal nearby staging tiles
  - are not blocked by a menu, cutscene, festival, or direct player interaction
- Encounter staging sequence:
  1. reserve local interaction space
  2. halt movement
  3. face both NPCs toward each other
  4. enter talking state
  5. show alternating speech bubbles
  6. write emotional and social consequences
  7. release both NPCs back to plan execution

### 8. Bubble Dialogue Binding
- Keep `NpcSpeechBubbleService`, but bind it strictly to physical interaction state.
- Bubbles should only appear when both NPCs are co-present and staged for a conversation.
- Hard rules remain:
  - max 60 characters per bubble
  - split longer lines into sequential bubbles
  - enough duration to read
  - cancel cleanly if the encounter breaks apart

### 9. Replanning And Failure Recovery
- Add explicit failure reasons:
  - `path_failed`
  - `door_locked`
  - `privacy_denied`
  - `target_missing`
  - `target_left`
  - `event_lock`
  - `stuck`
  - `late_for_anchor`
- Replanning order:
  1. retry route locally
  2. downgrade private visit to public meeting point
  3. substitute fallback nearby activity
  4. skip block and continue next schedule item
- Never leave an NPC in a permanently stuck override state.

## Important Data And Interface Changes

### Save-State Additions
- Add `SaveState.Autonomy` with:
  - `DailyPlansByNpc`
  - `RuntimeByNpc`
  - `VisitCooldowns`
  - `KnownDestinationsByNpc`
  - `CurrentRouteSegmentByNpc`
  - `ExpectedArrivalByNpc`
  - `LastMaterializedLocationByNpc`
- Extend current autonomy runtime models with:
  - route segment list
  - route cursor
  - target tile
  - arrival slack
  - offscreen progress markers

### Service Interfaces
- `WorldTopologyService`
  - build graph from current world
  - resolve legal nodes and connectors
- `NpcRoutePlannerService`
  - route from current location to target location/tile
  - provide ETA and fallback options
- `NpcAutonomyExecutionService`
  - advance movement each tick
  - detect arrival/failure
- `NpcMaterializationService`
  - reconcile runtime route state with actual world presence
- `NpcFaceToFaceInteractionService`
  - reserve, stage, run, and release in-person conversations

### Config Additions
- `EnableCrossMapAutonomy`
- `AutonomyVisibleMovementRadius`
- `AutonomyMaxTravelMinutesPerBlock`
- `AutonomyMaxWaitForTargetMinutes`
- `AutonomyCrossMapReplanLimit`
- `AutonomyPrivateVisitFallbackToPublic`
- `AutonomyMaterializeOffscreenOnly`
- `AutonomyFaceToFaceDistanceTiles`

## Tasks
1. Add `SaveState.Autonomy` models for route-capable plans and runtime progression.
2. Implement `WorldTopologyService` from live locations, warps, and curated private access rules.
3. Implement `NpcRoutePlannerService` for cross-map graph routing and local destination tiles.
4. Extend autonomy plan compilation so every autonomous block includes executable route data.
5. Implement loaded-map movement execution with arrival and stuck detection.
6. Implement offscreen travel simulation and runtime ETA progression for unloaded NPCs.
7. Implement materialization logic when the player enters a map containing an autonomous NPC.
8. Implement face-to-face encounter staging with stop, face, reserve, talk, and release states.
9. Bind `NpcSpeechBubbleService` to physical conversation staging instead of transcript-only display.
10. Add replanning and fallback rules for denied homes, missing targets, and blocked routes.
11. Add debug commands for route inspection, current segment, ETA, and forced replans.
12. Add telemetry for route success/failure, materialization, arrival timing, and encounter staging outcomes.
13. Validate multi-day autonomous movement in-game across several NPC pairs and map combinations.

## Acceptance Scenarios
- An NPC leaves one map and arrives at a self-chosen destination on another map.
- An NPC legally visits another NPC's home during an allowed time window.
- A denied private visit reroutes to a public fallback meeting location.
- Two NPCs with non-overlapping vanilla schedules physically meet because one traveled to the other.
- They stop, face each other, and talk with alternating speech bubbles.
- Save and reload mid-route preserves destination, ETA, and next block.
- Festival and bedtime anchors still override autonomy correctly.
- A failed route or missing target replans instead of freezing the NPC.

## Risks
- Live movement APIs may behave differently for loaded vs unloaded NPCs, so the execution layer must explicitly separate visible movement from abstract travel.
- Cross-map rewrites can conflict with vanilla or other modded schedule control if overrides are not anchored carefully.
- Private-home access can feel invasive without strict legality and fallback rules.
- Materialization can look like teleporting if not limited to offscreen-safe transitions.
- Replanning loops can churn endlessly if route and fallback rules are too loose.

## Assumptions
- The feature is not complete until autonomous movement is physically represented in-world.
- Hard canon locks still win over self-directed routine blocks.
- Curated private access remains mandatory.
- Player2 may suggest goals or dialogue, but movement legality and execution stay deterministic and local.
- Offscreen abstraction is acceptable only when it preserves coherent player-facing behavior and arrival timing.
