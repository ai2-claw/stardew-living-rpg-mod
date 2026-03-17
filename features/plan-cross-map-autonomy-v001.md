# Cross-Map Autonomous NPC Movement And Face-To-Face Interaction — Architecture

## Overview

The current autonomy implementation can choose intent, queue encounters, and show speech bubbles, but it does not yet satisfy the core feature because NPCs do not physically travel under self-directed schedules across the valley. This plan closes that gap by adding full cross-map movement, schedule rewriting, pathfinding, arrival staging, and face-to-face interaction execution.

The feature is only complete when NPCs can visibly or logically move from one location to another under autonomous plans, legally enter valid interiors, meet other NPCs in person, stop and face each other, and exchange sequential talk bubbles in-world.

---

## Gap Analysis

The following audit maps every component needed for autonomous NPCs against what actually exists in the codebase today. Each gap is numbered so the rest of this document can reference it.

### What Is Implemented

| Component | File | Status | Notes |
|-----------|------|--------|-------|
| Autonomy state models | `State/AutonomyRuntimeState.cs` | Done | `NpcDailyPlan`, `AutonomyPlanBlock`, `AutonomyRuntimeState`, `NpcContextSnapshot`, `ScoredAutonomyGoal`, `AutonomyGoalSuggestion` |
| Pair emotion storage | `State/NpcPairEmotionState.cs`, `State/SocialState.cs` | Done | `NpcPairEmotionEntry` with Affinity/Tension/Avoidance/EmotionAxes/ActiveFlags; integrated into `SocialState.PairEmotions` |
| Goal engine | `Systems/NpcAutonomyGoalEngine.cs` | Done | `BuildSnapshot()`, `ScoreCandidateGoals()`, `TryValidateSuggestion()` |
| Plan synthesis | `Systems/NpcAutonomyPlannerService.cs` | Done | `SynthesizeDailyPlan()`, `AdvancePlan()`, `ReplanWithFallback()` |
| Destination registry | `Systems/DestinationRegistryService.cs` | Done | `BuildLocations()`, `TryResolveVisitLocation()`, `IsVisitAllowed()`, `ResolveFallbackLocation()` |
| Pair emotion service | `Systems/PairEmotionService.cs` | Done | `GetOrCreate()`, `TryAdjustAxis()`, `Decay()` with daily capping and flag auto-set |
| Speech bubble service | `Systems/NpcSpeechBubbleService.cs` | Done | `QueueTranscriptLine()`, `ChunkText()`, `Tick()` with 60-char chunking and `showTextAboveHead()` rendering |
| Intent resolver extensions | `Systems/NpcIntentResolver.cs` | Done | `adjust_npc_pair_emotion`, `record_social_incident`, `set_npc_pair_flag`, `suggest_autonomy_goal` |
| Autonomy config | `Config/ModConfig.cs` | Done | Block limits, cooldowns, intensity multipliers, bubble timing, pair emotion caps |
| Autonomy telemetry counters | `State/TelemetryState.cs` | Done | Goals, plans, blocks, encounters, visits, bubbles, pair emotions — all counters present |

### What Is Missing (Gaps)

| # | Gap | Impact | Severity |
|---|-----|--------|----------|
| G1 | **No ModEntry wiring** — None of the autonomy services are instantiated, composed, or hooked into `OnDayStarted` or `OnUpdateTicked`. The entire pipeline is dead code. | Plans never created, advanced, or produce any in-game effect | Critical |
| G2 | **No SaveState persistence** — `SaveState` has no `Autonomy` property. Active plans, runtime state, and route progress are lost on save/reload. | Mid-day save loses all autonomous behavior | Critical |
| G3 | **No schedule override mechanism** — The planner synthesizes `AutonomyPlanBlock` objects but never patches any SMAPI NPC schedule data. The NPC sprite stays on its vanilla path. | NPCs never physically move under autonomy | Critical |
| G4 | **No world topology graph** — `DestinationRegistryService.BuildLocations()` infers flat location metadata but does not model warps, doors, or multi-hop connectivity between maps. | Cannot route NPCs across maps | Critical |
| G5 | **No cross-map route planner** — No service converts a (source location, destination location) pair into an ordered list of warp/path segments with ETAs. | No route to follow even if schedule override existed | Critical |
| G6 | **No movement execution service** — No tick-level code drives loaded NPCs toward their next waypoint, detects arrival, or detects stuck/blocked state. | NPCs don't actually walk | Critical |
| G7 | **No materialization service** — No code reconciles offscreen NPC progress with physical map presence when the player enters a new location. | Offscreen-traveling NPCs are invisible or misplaced forever | High |
| G8 | **No social encounter service** — `NpcSocialEncounterService` is specified in the parent plan but never implemented. No scoring, triggering, or lifecycle management of encounters. | Planned visits produce no conversation; co-presence is wasted | Critical |
| G9 | **No face-to-face staging** — No code stops two NPCs, faces them toward each other, enters a talking state, or sequences bubble exchange in physical space. | Bubbles could fire on NPCs walking past each other; no composed interaction | High |
| G10 | **No autonomy runtime manager** — No central object in ModEntry owns the per-NPC `AutonomyRuntimeState` dictionary, handles creation/reset on day start, or provides lookup for tick operations. | No coordinated lifecycle for multiple NPC plans | High |
| G11 | **No fallback line generator** — The plan calls for deterministic fallback dialogue when Player2 is unavailable. No implementation exists. | Silent encounters when Player2 is offline | Medium |
| G12 | **No console debug commands** — No `slrpg_autonomy_*` commands exist for inspecting plans, routes, pair emotions, or forcing replans. | No debugging or validation tooling | Medium |
| G13 | **DestinationRegistryService owner inference is fragile** — `InferOwner()` uses substring matching on location names (e.g. `"Sam"` matches `"SamHouse"`) which fails for modded locations and produces false positives for names that are substrings of other words. | Wrong owner assignments → wrong access decisions | Medium |
| G14 | **DestinationRegistryService rebuilds every call** — `BuildLocations()` is called fresh for every goal-scoring pass and every validation check. No caching. | Unnecessary allocation during tick-level operations | Low-Medium |
| G15 | **Goal engine only scores nearby NPCs for visit_npc** — `ScoreCandidateGoals()` only generates `visit_npc` goals for NPCs in `snapshot.NearbyNpcIds` (same location). An NPC across the valley is never a visit target. | Defeats the entire purpose of cross-map autonomy | Critical |
| G16 | **Plan blocks lack route data** — `AutonomyPlanBlock` has `TargetLocation` and `TargetNpcId` but no route segments, ETA, target tile, or warp chain. The planner emits intentions, not executable movement instructions. | Even with a movement executor, it wouldn't know how to get there | Critical |
| G17 | **AutonomyRuntimeState lacks route tracking** — No fields for active route segment, route cursor, target tile, offscreen progress, expected arrival time, or movement phase. | Cannot track or resume multi-segment travel | High |
| G18 | **No `arrive` or `wait_for_target` block types** — `AutonomyPlanBlockType` has `Travel` but lacks `Arrive` and `WaitForTarget`, which are needed for the staging handshake before face-to-face encounters. | No clear state transition between travel and interaction | Medium |
| G19 | **No cross-map config fields** — `ModConfig` has autonomy limits but no fields for `EnableCrossMapAutonomy`, travel time caps, encounter distance, materialization policy, etc. | Framework not configurable for cross-map behavior | Medium |
| G20 | **No NPC-to-home mapping for multi-resident homes** — Stardew homes often house multiple NPCs (e.g., SamHouse hosts Sam, Jodi, Vincent, Kent). `InferOwner()` returns only one owner per location. `IsVisitAllowed()` checks only that single owner. An NPC visiting their roommate would fail the privacy check against the wrong owner. | Household visits incorrectly denied | Medium |
| G21 | **PlayerFamily integration gap** — `SaveState.PlayerFamily` exists but the autonomy system doesn't account for the player's spouse/children being valid home-visit targets or the player's farmhouse being a potential destination. | NPCs can't visit the player's home or spouse | Low (v1 scope) |
| G22 | **Bubble service has no encounter binding** — `NpcSpeechBubbleService.QueueTranscriptLine()` takes just `(npcId, text)` with no encounter ID, no alternation enforcement, and no requirement that both speakers are co-present. Bubbles fire immediately even if the physical staging step hasn't happened. | Speech without physical grounding | Medium |
| G23 | **No SMAPI event for player map entry** — The plan requires materializing NPCs when the player enters a map. ModEntry needs to hook `GameLoop.Warped` or equivalent, which isn't done. | Materialization never triggered | High |
| G24 | **AdvancePlan auto-completes blocks by time only** — `AdvancePlan()` marks a block complete when `timeOfDay > block.EndTime` regardless of whether the NPC actually arrived. There is no arrival detection. | "Visit" blocks complete even if the NPC never reached the target | High |
| G25 | **No encounter-driven consequence hook** — The parent plan specifies that encounters should trigger `adjust_npc_pair_emotion`, `record_social_incident`, and optionally `record_town_event`. No code connects encounter completion to the intent resolver. | Social encounters produce no lasting emotional or world effects | High |

### Severity Summary

- **Critical (must fix to ship):** G1, G2, G3, G4, G5, G6, G8, G15, G16
- **High (feature severely degraded without):** G7, G9, G10, G17, G23, G24, G25
- **Medium (noticeable quality gap):** G11, G12, G13, G14, G18, G19, G20, G22
- **Low (deferrable to v1.1):** G21

---

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
- NPC visits to the player's farmhouse (deferred to v1.1 — see G21).

---

## Architecture

### System Component Map

```
┌──────────────────────────────────────────────────────────────────────────────┐
│  ModEntry  (composition root)                                                │
│                                                                              │
│  OnDayStarted                              OnUpdateTicked (every 30 ticks)   │
│  ┌──────────────────────────┐              ┌──────────────────────────────┐  │
│  │ 1. PairEmotionDecay      │              │ 1. AdvancePlan per NPC       │  │
│  │ 2. ResetDailyCooldowns   │              │ 2. ExecuteMovement per NPC   │  │
│  │ 3. BuildSnapshots        │              │ 3. CheckArrival per NPC      │  │
│  │ 4. ScoreGoals per NPC    │              │ 4. EvaluateEncounters        │  │
│  │ 5. SynthesizePlans       │              │ 5. TickBubbles               │  │
│  │ 6. CompileRoutes         │              │ 6. TickStagingState          │  │
│  │ 7. ApplyScheduleOverrides│              └──────────────────────────────┘  │
│  └──────────────────────────┘                                                │
│                                            OnWarped (player enters map)      │
│  ┌─────────────────────────────────────┐   ┌──────────────────────────────┐  │
│  │ AutonomyRuntimeManager              │   │ MaterializeNpcs on this map  │  │
│  │  • Dictionary<npcId, RuntimeState>  │   └──────────────────────────────┘  │
│  │  • Create / Reset / Lookup / Save   │                                     │
│  └──────────┬──────────────────────────┘                                     │
│             │ owns                                                            │
│  ┌──────────┼──────────────────────────────────────────────────────────────┐ │
│  │          ▼                                                              │ │
│  │  ┌────────────────────┐  ┌─────────────────────┐  ┌──────────────────┐ │ │
│  │  │ GoalEngine         │  │ PlannerService       │  │ RoutePlanner     │ │ │
│  │  │ (score + validate) │─▶│ (synthesize + replan)│─▶│ (graph + ETA)   │ │ │
│  │  └────────────────────┘  └──────────┬──────────┘  └────────┬─────────┘ │ │
│  │                                      │                      │           │ │
│  │  ┌────────────────────┐  ┌──────────▼──────────┐  ┌────────▼─────────┐ │ │
│  │  │ DestinationRegistry│  │ ExecutionService     │  │ WorldTopology    │ │ │
│  │  │ (access + legality)│  │ (move + warp + stuck)│  │ (nodes + edges)  │ │ │
│  │  └────────────────────┘  └──────────┬──────────┘  └──────────────────┘ │ │
│  │                                      │                                  │ │
│  │  ┌────────────────────┐  ┌──────────▼──────────┐                       │ │
│  │  │ Materialization    │  │ ScheduleOverride     │                       │ │
│  │  │ (spawn at tile)    │  │ (patch NPC.Schedule)  │                       │ │
│  │  └────────────────────┘  └─────────────────────┘                       │ │
│  └─────────────────────────────────────────────────────────────────────────┘ │
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────────┐ │
│  │ Encounter + Dialogue Layer                                              │ │
│  │  ┌─────────────────────┐  ┌──────────────────┐  ┌───────────────────┐  │ │
│  │  │ SocialEncounterSvc  │  │ FaceToFaceStagingSvc│  │ SpeechBubbleSvc │  │ │
│  │  │ (score + lifecycle) │─▶│ (stop+face+reserve)│─▶│ (chunk + render) │  │ │
│  │  └──────────┬──────────┘  └──────────────────┘  └───────────────────┘  │ │
│  │             │                                                           │ │
│  │  ┌──────────▼──────────┐  ┌──────────────────┐  ┌───────────────────┐  │ │
│  │  │ ConsequenceHook     │  │ FallbackLineGen  │  │ PairEmotionSvc    │  │ │
│  │  │ (→ IntentResolver)  │  │ (offline dialogue)│  │ (adjust + decay)  │  │ │
│  │  └─────────────────────┘  └──────────────────┘  └───────────────────┘  │ │
│  └─────────────────────────────────────────────────────────────────────────┘ │
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────────┐ │
│  │ Existing services (unchanged contracts, new consumers)                   │ │
│  │ NpcMemoryService · TownMemoryService · NpcConversationService            │ │
│  │ AmbientConsequenceService · Player2Client · NpcIntentResolver            │ │
│  └─────────────────────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────────────────┘
```

### Data Flow: Full Lifecycle

```
OnDayStarted
  │
  ├─ 1. PairEmotionService.Decay(state)           [existing — works]
  ├─ 2. RuntimeManager.ResetDaily()                [new — G10]
  │
  ├─ 3. FOR each eligible NPC:
  │      ├─ GoalEngine.BuildSnapshot(state, npc, allNpcs)     [existing — extend for G15]
  │      ├─ GoalEngine.ScoreCandidateGoals(state, npc, snap)  [existing — extend for G15]
  │      ├─ PlannerService.SynthesizeDailyPlan(...)           [existing — extend for G16]
  │      ├─ RoutePlanner.CompileRoutes(plan.Blocks)           [new — G5]
  │      │   └─ WorldTopology.FindRoute(from, to)             [new — G4]
  │      ├─ ScheduleOverrideService.Apply(npc, plan)          [new — G3]
  │      └─ RuntimeManager.Register(npc, plan, routes)        [new — G10]
  │
  └─ 4. Telemetry: plans created, goals accepted/rejected

OnUpdateTicked (every 30 ticks ≈ 500ms)
  │
  ├─ FOR each NPC with active plan:
  │      ├─ PlannerService.AdvancePlan(runtime, timeOfDay)    [existing — fix G24]
  │      │   └─ Check arrival via distance, not just time
  │      │
  │      ├─ ExecutionService.Tick(runtime, npc)               [new — G6]
  │      │   ├─ Loaded NPC: pathfindToNextScheduleLocation / controller
  │      │   ├─ Unloaded NPC: advance abstract ETA progress
  │      │   └─ Detect stuck → ReplanWithFallback
  │      │
  │      ├─ IF block is VisitNpc AND npc arrived:
  │      │   ├─ EncounterService.EvaluatePlannedVisit(...)    [new — G8]
  │      │   │   └─ ScoreEncounter(...) ≥ threshold?
  │      │   ├─ FaceToFaceService.TryStage(npcA, npcB)       [new — G9]
  │      │   │   ├─ FindStagingTiles(location)
  │      │   │   ├─ HaltMovement(npcA, npcB)
  │      │   │   ├─ FaceToward(npcA, npcB)
  │      │   │   └─ SetStagingState(Talking)
  │      │   ├─ ConversationService or FallbackLineGen        [existing + G11]
  │      │   ├─ BubbleService.QueueBubble(encounterId, ...)   [existing — fix G22]
  │      │   └─ ConsequenceHook.OnEncounterComplete(...)      [new — G25]
  │      │       ├─ adjust_npc_pair_emotion
  │      │       ├─ record_social_incident
  │      │       └─ (optional) record_town_event
  │      │
  │      └─ Opportunistic encounters (non-planned co-location):
  │          └─ EncounterService.EvaluateOpportunistic(...)
  │
  ├─ BubbleService.Tick(resolveNpc)                           [existing — works]
  └─ FaceToFaceService.Tick(...)                              [new — G9]

OnWarped (player entered a new map)                            [new — G7, G23]
  │
  └─ MaterializationService.ReconcileMap(location)
      ├─ FOR each NPC whose route says they should be here:
      │   ├─ If NPC not present: warp to expected tile
      │   └─ If present but wrong tile: adjust position
      └─ Skip if NPC has arrival time in the future (not yet arrived)

OnSaving
  │
  └─ Save RuntimeManager state into SaveState.Autonomy        [new — G2]

OnSaveLoaded
  │
  └─ Restore runtimes from SaveState.Autonomy (or rebuild)    [new — G2]
```

---

## Detailed Component Design

### 1. World Topology Layer — `WorldTopologyService`

**File:** `Systems/WorldTopologyService.cs`  
**Fixes:** G4

**Responsibility:** Build a traversal graph of the game world from live `GameLocation` data and SMAPI warp metadata. Provides the route planner with a graph to search.

#### Graph Model

```csharp
public sealed class TopologyNode
{
    public string LocationId { get; init; } = string.Empty;
    public string Category { get; init; } = "public";    // public, semi_private, private
    public string? OwnerNpcId { get; init; }
    public string[]? HouseholdNpcIds { get; init; }       // [G20] all residents, not just owner
    public Point DefaultTile { get; init; }
    public bool IsInterior { get; init; }
}

public sealed class TopologyEdge
{
    public string FromLocationId { get; init; } = string.Empty;
    public string ToLocationId { get; init; } = string.Empty;
    public Point WarpFromTile { get; init; }               // tile in source map near the door/warp
    public Point WarpToTile { get; init; }                 // arrival tile in destination map
    public int EstimatedTravelMinutes { get; init; } = 5;  // walking ETA within source map
    public bool IsWarp { get; init; }                      // true = instant transition
    public bool IsDoor { get; init; }                      // true = door animation
}

public sealed class WorldGraph
{
    public Dictionary<string, TopologyNode> Nodes { get; init; } = new();
    public Dictionary<string, List<TopologyEdge>> AdjacencyBySource { get; init; } = new();
}
```

#### Key Methods

```csharp
public sealed class WorldTopologyService
{
    // Build graph from all live game locations. Cache per-day.
    // Discovers warps and doors from GameLocation.warps and Building.indoors.
    public WorldGraph BuildGraph(IEnumerable<GameLocation> locations);

    // Find the shortest hop-sequence from source to destination.
    // Returns null if no route exists (disconnected or all paths access-denied).
    public List<TopologyEdge>? FindRoute(
        WorldGraph graph,
        string fromLocationId,
        string toLocationId);

    // Estimate total transit time in game-minutes for a route.
    public int EstimateTotalMinutes(List<TopologyEdge> route);

    // Resolve all residents of a home location (fixes G20).
    public string[] ResolveHousehold(string locationId, IEnumerable<GameLocation> locations);
}
```

#### Warp Discovery

Stardew's `GameLocation.warps` contains `Warp` objects with (fromX, fromY, targetName, targetX, targetY). Doors are exposed via `GameLocation.doors` or `Building.indoors`. The service iterates both:

```
BuildGraph(locations):
  FOR each location:
    1. Create TopologyNode with category, owner, household, defaultTile
    2. FOR each warp in location.warps:
         Create TopologyEdge (location → warp.targetName) with tiles
    3. FOR each door in location.doors:
         Create TopologyEdge (location → door target) with IsDoor=true
    4. IF location is Building with indoors:
         Create bidirectional TopologyEdge (outdoor → indoor and back)
  Cache graph for the day (invalidate on festival or event lock change)
```

#### Household Resolution (G20)

Instead of mapping one owner per location, the topology service resolves all NPCs whose `DefaultMap` property points to that location:

```
ResolveHousehold(locationId, locations):
  return allNpcs.Where(npc => npc.DefaultMap == locationId)
                .Select(npc => npc.Name)
                .ToArray();
```

This means Sam, Jodi, Vincent, and Kent are all recorded as household members of `SamHouse`. Visiting any of them grants household-level access to the address.

---

### 2. Route Planning — `NpcRoutePlannerService`

**File:** `Systems/NpcRoutePlannerService.cs`  
**Fixes:** G5, G16

**Responsibility:** Convert a (source location, destination location) pair into an ordered list of route segments with per-segment ETAs. Integrates with `WorldTopologyService` for graph search and `DestinationRegistryService` for access checks at each hop.

#### Route Segment Model

```csharp
public sealed class RouteSegment
{
    public string FromLocationId { get; init; } = string.Empty;
    public string ToLocationId { get; init; } = string.Empty;
    public Point DepartureTile { get; init; }
    public Point ArrivalTile { get; init; }
    public int EstimatedMinutes { get; init; }
    public bool IsWarp { get; init; }
    public bool IsDoor { get; init; }
    public RouteSegmentStatus Status { get; set; } = RouteSegmentStatus.Pending;
}

public enum RouteSegmentStatus { Pending, InProgress, Completed, Failed }

public sealed class CompiledRoute
{
    public string SourceLocationId { get; init; } = string.Empty;
    public string DestinationLocationId { get; init; } = string.Empty;
    public List<RouteSegment> Segments { get; init; } = new();
    public int TotalEstimatedMinutes { get; init; }
    public Point FinalArrivalTile { get; init; }
}
```

#### Key Methods

```csharp
public sealed class NpcRoutePlannerService
{
    // Plan a full route from current location to target.
    // Returns null if no legal route exists (access-denied, disconnected).
    public CompiledRoute? PlanRoute(
        WorldGraph graph,
        SaveState state,
        string npcId,
        string fromLocationId,
        string toLocationId,
        int timeOfDay);

    // Plan routes for all blocks in a daily plan.
    // Populates each block's compiled route data.
    public void CompileRoutesForPlan(
        WorldGraph graph,
        SaveState state,
        string npcId,
        NpcDailyPlan plan,
        int startTimeOfDay);

    // Get the fallback route when the primary route fails.
    // Routes to nearest public location instead.
    public CompiledRoute? PlanFallbackRoute(
        WorldGraph graph,
        string npcId,
        string currentLocationId);
}
```

#### Route Planning Algorithm

```
PlanRoute(graph, state, npcId, from, to, timeOfDay):
  1. edgePath = WorldTopologyService.FindRoute(graph, from, to)
     (BFS/Dijkstra on topology edges, weighted by EstimatedTravelMinutes)
  2. IF edgePath is null → return null
  3. FOR each edge in edgePath:
       a. Check DestinationRegistryService.IsVisitAllowed for the destination
       b. IF access denied at any intermediate hop → try alternate path or return null
  4. Convert edges to RouteSegments with departure/arrival tiles
  5. Sum estimated minutes; reject if total > config.AutonomyMaxTravelMinutesPerBlock
  6. Return CompiledRoute
```

---

### 3. Autonomous Schedule Compilation (Extended)

**Fixes:** G3, G16, G18

The existing `NpcAutonomyPlannerService.SynthesizeDailyPlan()` produces abstract `AutonomyPlanBlock` objects with `TargetLocation` but no route data. The step after plan synthesis is **route compilation**, which enriches every block with executable movement instructions.

#### State Model Extensions (G16, G17, G18)

Add to `AutonomyPlanBlock`:

```csharp
// Add fields to existing AutonomyPlanBlock:
public CompiledRoute? Route { get; set; }       // compiled route from current to target
public Point TargetTile { get; set; }           // specific tile within target location
public int EstimatedArrivalTime { get; set; }   // game time ETA
public int MaxWaitMinutes { get; set; } = 30;   // max wait at destination for target NPC
```

Add block types to `AutonomyPlanBlockType`:

```csharp
// Add to existing enum:
Arrive,           // NPC has reached destination, transitioning to activity
WaitForTarget     // NPC is at destination, waiting for target NPC to arrive
```

Add route tracking to `AutonomyRuntimeState` (G17):

```csharp
// Add fields to existing AutonomyRuntimeState:
public int ActiveRouteSegmentIndex { get; set; } = -1;
public string ExpectedLocationId { get; set; } = string.Empty;
public Point ExpectedTile { get; set; }
public int ExpectedArrivalTime { get; set; }
public int OffscreenProgressMinutes { get; set; }
public string MovementPhase { get; set; } = "idle";  // idle, walking, warping, arrived, stuck
public string? ActiveEncounterId { get; set; }
public string? StagingTargetNpcId { get; set; }
```

#### Schedule Override Mechanism (G3)

**File:** `Systems/ScheduleOverrideService.cs`

Stardew NPCs follow `NPC.Schedule`, a `Dictionary<int, SchedulePathDescription>`. Each entry maps a game-time key to a path description containing target map, tile, facing direction, and animation.

The override service rewrites the NPC's schedule to insert autonomous destinations:

```csharp
public sealed class ScheduleOverrideService
{
    // Replace the NPC's vanilla schedule with autonomous plan entries.
    // Preserves hard-lock entries (festival, sleep) by keeping them in place.
    public void ApplyDailyOverride(NPC npc, NpcDailyPlan plan);

    // Clear all autonomy overrides and restore vanilla schedule.
    public void RestoreVanillaSchedule(NPC npc);

    // Patch a single schedule entry for an immediate route change (replan).
    public void PatchSingleEntry(NPC npc, int gameTime, string locationId, Point tile, int facing);
}
```

Implementation approach:

```
ApplyDailyOverride(npc, plan):
  1. Read npc.Schedule (vanilla entries)
  2. Identify hard-lock entries (festivals, cutscene triggers, sleep)
  3. FOR each AutonomyPlanBlock in plan.Blocks:
       IF block.Type is BaseAnchor or ReturnHome → keep vanilla entry
       ELSE:
         a. Create SchedulePathDescription for block.TargetLocation / block.TargetTile
         b. Insert at block.StartTime, overwriting vanilla entry for that time slot
  4. Write modified schedule back to npc.Schedule
  5. Call npc.checkSchedule(Game1.timeOfDay) to trigger immediate pathfinding
```

**Important SMAPI detail:** NPC schedules are loaded once per day via `NPC.getSchedule()`. We must apply overrides *after* the vanilla schedule is loaded (during `OnDayStarted`, after the game has initialized schedules for the day) and before the NPC starts following its first schedule point.

---

### 4. Runtime Movement Execution — `NpcAutonomyExecutionService`

**File:** `Systems/NpcAutonomyExecutionService.cs`  
**Fixes:** G6, G24

**Responsibility:** Drive NPCs toward their next route segment waypoint each tick. Detect arrival, stuck state, and trigger replanning on failure.

#### Key Methods

```csharp
public sealed class NpcAutonomyExecutionService
{
    // Tick the movement state for a single NPC.
    // Returns the movement outcome for this tick.
    public MovementTickResult Tick(
        NPC? npc,                          // null if NPC is unloaded
        AutonomyRuntimeState runtime,
        AutonomyPlanBlock activeBlock,
        int gameTimeOfDay);

    // Check if the NPC has physically arrived at the target tile/location.
    // Uses tile distance for loaded NPCs, time comparison for unloaded.
    public bool HasArrived(
        NPC? npc,
        AutonomyRuntimeState runtime,
        AutonomyPlanBlock activeBlock);

    // Force-warp an NPC to a specific tile on a specific map.
    // Used for materialization and offscreen fast-travel.
    public void WarpNpcTo(NPC npc, string locationId, Point tile);
}

public enum MovementTickResult
{
    InProgress,     // still walking/warping toward target
    Arrived,        // reached destination
    Stuck,          // no progress for N ticks, needs replan
    WaitingForLoad, // NPC is offscreen, advancing abstract ETA
    BlockComplete,  // block time expired (with arrival confirmed)
    HardLockYield   // yielding to an imminent festival/canon lock
}
```

#### Tick Algorithm

```
Tick(npc, runtime, block, time):
  IF npc is loaded AND on same map as player:
    // Visible movement — let SMAPI pathfinding drive the NPC
    1. Ensure npc.controller is targeting the correct tile
       (use npc.controller or issue pathfindToNextScheduleLocation)
    2. Check distance from npc.Tile to block.TargetTile:
       IF distance ≤ 2 tiles → return Arrived
    3. IF npc hasn't moved for 120 ticks (stuck detection):
       runtime.RetryCount++
       IF RetryCount > 3 → return Stuck
       ELSE → re-issue pathfinding command
    4. Return InProgress

  IF npc is null OR npc is on different map:
    // Abstract offscreen travel
    1. Advance runtime.OffscreenProgressMinutes by (tickInterval / 60fps × gameMinutesPerTick)
    2. IF OffscreenProgressMinutes >= block.Route.TotalEstimatedMinutes:
       runtime.ExpectedLocationId = block.TargetLocation
       runtime.ExpectedTile = block.TargetTile
       return Arrived (pending materialization)
    3. ELSE:
       // Calculate which intermediate segment the NPC is "at" for materialization
       Update runtime.ExpectedLocationId based on cumulative segment progress
       return WaitingForLoad

  IF approaching hard lock (festival within 30 game-minutes):
    return HardLockYield
```

#### Arrival Detection Fix (G24)

The existing `AdvancePlan()` auto-completes blocks when `timeOfDay > block.EndTime`. This must be changed so that blocks with a target location only complete when *either*:
- `HasArrived()` returns true, OR
- `timeOfDay > block.EndTime + MaxWaitMinutes` (hard timeout to prevent stuck blocks)

Blocks that time out without arrival should be marked `Failed`, not `Completed`.

---

### 5. Materialization — `NpcMaterializationService`

**File:** `Systems/NpcMaterializationService.cs`  
**Fixes:** G7, G23

**Responsibility:** When the player enters a new map, reconcile offscreen NPC autonomy progress with physical presence. Warp NPCs to the correct tile if their route says they should be on this map.

#### Key Methods

```csharp
public sealed class NpcMaterializationService
{
    // Called from ModEntry.OnWarped when the player enters a new location.
    public void ReconcileMap(
        GameLocation location,
        AutonomyRuntimeManager runtimeManager,
        NpcAutonomyExecutionService executionService);

    // Find a safe tile near the target that is walkable and unoccupied.
    public Point FindSafeTile(GameLocation location, Point preferredTile, NPC npc);
}
```

#### Reconciliation Algorithm

```
ReconcileMap(location, runtimeManager, executionService):
  FOR each NPC with an active autonomy plan:
    runtime = runtimeManager.Get(npcId)
    IF runtime.ExpectedLocationId != location.Name → skip

    npc = Game1.getCharacterFromName(npcId)
    IF npc is null → skip (NPC doesn't exist in world)

    IF npc.currentLocation?.Name == location.Name:
      // Already here — check if tile is reasonable
      IF distance(npc.Tile, runtime.ExpectedTile) > 8:
        // Snap to expected position (was probably on vanilla schedule)
        safeTile = FindSafeTile(location, runtime.ExpectedTile, npc)
        executionService.WarpNpcTo(npc, location.Name, safeTile)
    ELSE:
      // NPC is offscreen but should be here
      IF runtime.OffscreenProgressMinutes >= route estimated time:
        safeTile = FindSafeTile(location, runtime.ExpectedTile, npc)
        executionService.WarpNpcTo(npc, location.Name, safeTile)
      ELSE:
        // Not yet arrived according to abstract progress — don't materialize early
        skip
```

#### Safe Tile Rules

```
FindSafeTile(location, preferred, npc):
  1. IF preferred tile is walkable AND unoccupied → return preferred
  2. Search adjacent tiles in expanding ring (1-tile, 2-tile, 3-tile radius)
  3. Return first walkable + unoccupied tile
  4. If none found within 5-tile radius → return preferred anyway (accept overlap)
  Never materialize:
    - On water tiles
    - On impassable terrain
    - Inside walls (check location.isTilePassable)
    - On top of the player (check distance ≥ 2 from farmer)
```

---

### 6. Social Encounter Service — `NpcSocialEncounterService`

**File:** `Systems/NpcSocialEncounterService.cs`  
**Fixes:** G8, G25

**Responsibility:** Score, trigger, and manage the lifecycle of NPC-to-NPC encounters. This is the bridge between physical co-presence and conversation/consequence.

#### Encounter Model

```csharp
public sealed class ActiveEncounter
{
    public string EncounterId { get; init; } = string.Empty;
    public string NpcA { get; init; } = string.Empty;
    public string NpcB { get; init; } = string.Empty;
    public string LocationId { get; init; } = string.Empty;
    public EncounterSource Source { get; init; }
    public float Score { get; init; }
    public string SelectedBeat { get; init; } = string.Empty;
    public int TurnDepth { get; init; }
    public EncounterPhase Phase { get; set; } = EncounterPhase.Pending;
    public int TurnsCompleted { get; set; }
    public DateTime StartedUtc { get; init; }
    public DateTime? CompletedUtc { get; set; }
}

public enum EncounterSource { PlannedVisit, Opportunistic, EventConvergence }
public enum EncounterPhase { Pending, Staging, Talking, Consequences, Complete, Cancelled }
```

#### Scoring

```
ScoreEncounter(state, npcA, npcB, locationId, context) → float [0.0, 1.0]:
  pair = PairEmotionService.GetOrCreate(state, npcA, npcB)

  salience = 0.0
    + (|pair.Affinity| / 400f)             // high affinity or high coldness = interesting
    + (pair.Tension / 500f)                 // tension makes encounters dramatic
    + (pair.Familiarity / 500f)             // known pairs talk more

  eventBonus = 0.0
    IF both NPCs know a recent town event → +0.12

  locationFit = 0.0
    IF location has tag "saloon" → +0.10
    IF location has tag "square" → +0.08
    IF location has tag "nature" AND pair.Tension < 20 → +0.06

  activityFit = 0.0
    IF both NPCs are in Socialize or Wander blocks → +0.08
    IF one is Working → -0.05

  privacyFit = 0.0
    IF location is Private AND pair.Tension >= 40 → +0.08  (private confrontation)
    IF location is Public AND pair.Tension < 20 → +0.06    (casual public chat)

  modeMultiplier = context.Mode switch { cozy=0.8, story=1.0, chaos=1.2 }

  recentPenalty = 0.0
    IF this pair met within last 8 real-minutes → -0.30

  fatiguePenalty = 0.0
    - 0.08 per encounter npcA already had today

  score = (salience + eventBonus + locationFit + activityFit + privacyFit)
          × modeMultiplier
          + recentPenalty + fatiguePenalty

  return clamp(score, 0.0, 1.0)
```

#### Key Methods

```csharp
public sealed class NpcSocialEncounterService
{
    public float ScoreEncounter(
        SaveState state,
        string npcA, string npcB,
        string locationId,
        EncounterContext context);

    // Evaluate a planned visit encounter. Returns encounter if score ≥ threshold.
    public ActiveEncounter? EvaluatePlannedVisit(
        SaveState state,
        AutonomyRuntimeState visitorRuntime,
        string hostNpcId,
        string locationId);

    // Evaluate an opportunistic co-presence encounter.
    public ActiveEncounter? EvaluateOpportunistic(
        SaveState state,
        string npcA, string npcB,
        string locationId);

    // Run post-encounter consequences through the intent resolver.
    public void ProcessConsequences(
        SaveState state,
        ActiveEncounter encounter,
        string conversationTone);   // "friendly", "tense", "neutral", "hostile"

    // Get all active encounters (for tick management).
    public IReadOnlyList<ActiveEncounter> GetActiveEncounters();

    // Cancel an encounter (player interrupted, NPC left, etc.).
    public void CancelEncounter(string encounterId, string reason);
}
```

#### Consequence Hook (G25)

When an encounter completes, the service generates and submits intent commands through the existing resolver:

```
ProcessConsequences(state, encounter, tone):
  pair = PairEmotionService.GetOrCreate(state, encounter.NpcA, encounter.NpcB)

  // 1. Emotion adjustment based on conversation tone
  adjustments = tone switch {
    "friendly" → { friendship: +2, trust: +1, tension: -1 },
    "tense"    → { anger: +2, tension: +2, awkwardness: +1 },
    "hostile"  → { anger: +4, tension: +3, friendship: -1 },
    "neutral"  → { familiarity: +1 }
  }
  FOR each (axis, delta) in adjustments:
    IntentResolver.ResolveFromStreamLine(state, adjust_npc_pair_emotion envelope)

  // 2. Record social incident to town memory
  IntentResolver.ResolveFromStreamLine(state, record_social_incident envelope)

  // 3. Telemetry
  state.Telemetry.Daily.EncountersCompleted++
```

---

### 7. Face-To-Face Staging — `NpcFaceToFaceService`

**File:** `Systems/NpcFaceToFaceService.cs`  
**Fixes:** G9, G22

**Responsibility:** Manage the physical staging of two NPCs for a conversation: stop movement, face each other, reserve interaction space, sequence bubble display, and release when done.

#### Staging State Machine

```
States: Idle → Approaching → Positioned → Talking → Releasing → Idle

Approaching:
  Both NPCs are being walked to adjacent staging tiles.
  Timeout: 60 ticks. If not positioned in time → cancel and resume plan.

Positioned:
  Both NPCs are on their staging tiles and facing each other.
  Movement controllers are paused.
  Transition: immediately → Talking

Talking:
  Bubbles are being displayed in alternation (A₁ → B₁ → A₂ → B₂).
  Duration: determined by turn depth and text length.
  No NPC movement. Both sprites are idle-facing.
  Exit: all bubbles exhausted OR timeout (30 seconds) OR interruption.

Releasing:
  Clean up: resume movement controllers, clear staging state.
  Trigger encounter consequences.
  Transition: immediately → Idle
```

#### Staging Tile Selection

```
FindStagingTiles(location, npcA, npcB):
  // Find two adjacent walkable tiles near both NPCs
  midpoint = average(npcA.Tile, npcB.Tile)
  candidates = all tile pairs within 3-tile radius of midpoint
                where both tiles are walkable, adjacent, and unoccupied
  IF candidates found:
    pick pair closest to midpoint
  ELSE:
    use npcA.Tile and nearest walkable tile to npcB
  return (tileA, tileB)
```

#### Key Methods

```csharp
public sealed class NpcFaceToFaceService
{
    // Try to stage a face-to-face encounter between two NPCs.
    // Returns false if NPCs are not co-present, staging tiles unavailable, or interrupted.
    public bool TryStage(
        NPC npcA, NPC npcB,
        GameLocation location,
        ActiveEncounter encounter);

    // Tick the staging state machine. Called each update tick.
    public void Tick();

    // Cancel all active stagings (player opened menu, warped away, etc.).
    public void CancelAll(string reason);

    // Check if an NPC is currently in a staged encounter.
    public bool IsInStaging(string npcId);

    // Get the encounter ID for an NPC currently in staging.
    public string? GetActiveEncounterId(string npcId);
}
```

#### Bubble Binding Fix (G22)

The existing `NpcSpeechBubbleService.QueueTranscriptLine(npcId, text)` must be extended with encounter awareness:

```csharp
// New overload that binds bubbles to a staged encounter:
public void QueueEncounterBubble(
    string encounterId,
    string speakerNpcId,
    string text,
    int sequenceIndex);   // 0-based turn order for alternation

// Modified Tick() behavior:
// - Only display a bubble if the speaker's encounter is in Talking phase
// - Alternate between speakers based on sequenceIndex
// - Cancel all bubbles for an encounter if the staging is cancelled
```

---

### 8. Goal Engine Extension (G15)

The existing `NpcAutonomyGoalEngine.ScoreCandidateGoals()` only generates `visit_npc` goals for NPCs currently on the same map (`snapshot.NearbyNpcIds`). This defeats cross-map autonomy entirely.

**Required change:** Score `visit_npc` goals for **all known NPCs** in the world, not just nearby ones. The scoring formula should factor in travel distance (prefer closer NPCs, but don't exclude distant ones):

```
// Replace the existing nearby-NPC-only visit scoring with:
FOR each NPC in allWorldNpcs (excluding self):
  pair = PairEmotionService.GetOrCreate(state, thisNpc, otherNpc)
  friendship = GetAxis(pair, "friendship")
  trust = GetAxis(pair, "trust")
  tension = pair.Tension

  // Base visit motivation from relationship
  motivation = 0.15 + ((friendship + trust) / 250f) - (tension / 400f)

  // Travel distance penalty (new)
  route = RoutePlanner.PlanRoute(graph, state, thisNpc, currentLocation, otherNpc.homeLocation, timeOfDay)
  IF route is null → skip (unreachable)
  travelPenalty = (route.TotalEstimatedMinutes / 200f)  // 60 min travel = -0.30 penalty
  motivation -= travelPenalty

  // Adjacency bonus (already nearby = much more likely)
  IF otherNpc is on same map → motivation += 0.15

  IF motivation <= 0 → skip

  goals.Add(visit_npc goal with TargetLocation = otherNpc.currentLocation or home)
```

The `NpcContextSnapshot` should also be extended to include the world graph and all world NPCs, not just nearby ones:

```csharp
// Add to NpcContextSnapshot:
public IReadOnlyList<string> AllWorldNpcIds { get; init; } = Array.Empty<string>();
```

---

### 9. DestinationRegistry Fixes (G13, G14, G20)

#### Owner Inference Fix (G13)

Replace the substring-based `InferOwner()` with a lookup against NPC `DefaultMap`:

```csharp
// Replace InferOwner(string locationName) with:
private string InferOwner(string locationName, IEnumerable<NPC> allNpcs)
{
    // Use NPC.DefaultMap to find who lives at this location
    var residents = allNpcs
        .Where(npc => string.Equals(npc.DefaultMap, locationName, StringComparison.OrdinalIgnoreCase))
        .Select(npc => npc.Name)
        .ToList();

    // Return first resident as "primary owner" (for backward compat)
    return residents.FirstOrDefault() ?? string.Empty;
}
```

#### Caching Fix (G14)

Cache the result of `BuildLocations()` for the duration of a game day. Invalidate on day start or when a new location is loaded:

```csharp
private IReadOnlyList<AutonomyLocation>? _cachedLocations;
private int _cachedDay = -1;

public IReadOnlyList<AutonomyLocation> BuildLocations(IEnumerable<GameLocation> worldLocations)
{
    if (_cachedLocations != null && _cachedDay == Game1.Date.TotalDays)
        return _cachedLocations;

    // ... existing build logic ...
    _cachedLocations = results;
    _cachedDay = Game1.Date.TotalDays;
    return results;
}
```

#### Multi-Resident Access Fix (G20)

Extend `IsVisitAllowed()` to check affinity against *any* household member, not just the single owner:

```csharp
public bool IsVisitAllowed(
    SaveState state,
    string visitorNpcId,
    AutonomyLocation location,
    int timeOfDay,
    string[] householdNpcIds,    // NEW: all residents
    out string reasonCode)
{
    // If visitor IS a household member → always allowed during waking hours
    if (householdNpcIds.Contains(visitorNpcId, StringComparer.OrdinalIgnoreCase))
    {
        reasonCode = "ok";
        return timeOfDay >= 600 && timeOfDay <= 2400;
    }

    // Check affinity against the BEST household relationship
    var bestAffinity = int.MinValue;
    var worstAvoidance = 0;
    foreach (var resident in householdNpcIds)
    {
        var pairKey = PairEmotionService.BuildPairKey(visitorNpcId, resident);
        state.Social.PairEmotions.TryGetValue(pairKey, out var pair);
        bestAffinity = Math.Max(bestAffinity, pair?.Affinity ?? 0);
        worstAvoidance = Math.Max(worstAvoidance, pair?.Avoidance ?? 0);
    }

    // Access decision uses best-relationship-in-household
    // (visiting Sam's house because you're friends with Jodi = fine)
    // ... rest of affinity/tension/avoidance checks using bestAffinity ...
}
```

---

### 10. Autonomy Runtime Manager (G10)

**File:** `Systems/AutonomyRuntimeManager.cs`

**Responsibility:** Central lifecycle manager for all per-NPC autonomy runtime states. Owned by `ModEntry`, provides creation, lookup, reset, and persistence.

```csharp
public sealed class AutonomyRuntimeManager
{
    private readonly Dictionary<string, AutonomyRuntimeState> _runtimes = new(StringComparer.OrdinalIgnoreCase);

    // Create or reset runtime for an NPC at day start.
    public AutonomyRuntimeState Register(string npcId, NpcDailyPlan plan)
    {
        var runtime = new AutonomyRuntimeState
        {
            NpcId = npcId,
            ActivePlan = plan,
            ActiveBlockIndex = 0,
            OverrideStatus = AutonomyOverrideStatus.Planned,
            LastProgressUtc = DateTime.UtcNow
        };
        _runtimes[npcId] = runtime;
        return runtime;
    }

    public AutonomyRuntimeState? Get(string npcId)
        => _runtimes.TryGetValue(npcId, out var rt) ? rt : null;

    public IEnumerable<AutonomyRuntimeState> GetAllActive()
        => _runtimes.Values.Where(rt => rt.OverrideStatus != AutonomyOverrideStatus.Idle);

    public int ActivePlanCount => _runtimes.Count(rt => rt.Value.ActivePlan != null);

    public void ResetDaily()
    {
        _runtimes.Clear();
    }

    // Serialize to SaveState for persistence (G2)
    public Dictionary<string, AutonomyRuntimeState> ExportForSave()
        => new(_runtimes);

    // Restore from SaveState on load (G2)
    public void ImportFromSave(Dictionary<string, AutonomyRuntimeState>? saved)
    {
        _runtimes.Clear();
        if (saved == null) return;
        foreach (var (key, value) in saved)
            _runtimes[key] = value;
    }
}
```

---

### 11. SaveState Persistence (G2)

Add to `SaveState`:

```csharp
public sealed class SaveState
{
    // ... existing properties ...
    public AutonomySaveState Autonomy { get; set; } = new();
}

public sealed class AutonomySaveState
{
    public Dictionary<string, AutonomyRuntimeState> RuntimeByNpc { get; set; } = new();
    public Dictionary<string, int> VisitCooldownByPairKey { get; set; } = new();
    public Dictionary<string, int> LocationCooldownByNpcKey { get; set; } = new();
}
```

In `ModEntry.OnSaving`: serialize `AutonomyRuntimeManager.ExportForSave()` into `state.Autonomy.RuntimeByNpc`.

In `ModEntry.OnSaveLoaded`: restore via `AutonomyRuntimeManager.ImportFromSave(state.Autonomy.RuntimeByNpc)`.

**Note:** `CompiledRoute` objects within `AutonomyPlanBlock` are transient (contain no persistent state beyond segment status). On reload, routes that were `InProgress` are recalculated for the remaining segments. This avoids persisting warp tile coordinates that could change if the player installed a map-altering mod between sessions.

---

### 12. Fallback Line Generator (G11)

**File:** `Systems/NpcFallbackLineGenerator.cs`

When Player2 is unavailable, stalled, or returns invalid text, the encounter uses pre-authored fallback templates so the conversation isn't silent:

```csharp
public sealed class NpcFallbackLineGenerator
{
    // Generate a short deterministic line for the given beat and speaker.
    public string GenerateLine(
        string npcId,
        string beat,              // "gossip", "work", "intrigue", etc.
        string? targetNpcId,
        int turnIndex);           // 0 = opening, 1+ = response

    // Example templates by beat:
    // gossip:   "Did you hear what happened yesterday?"
    //           "I keep hearing things about {targetNpc}..."
    // work:     "It's been a long day already."
    //           "The shop's been busier than usual."
    // intrigue: "Something doesn't quite add up."
    //           "...I wonder what {targetNpc} is really up to."
    // default:  "Nice to run into you."
    //           "How have you been?"
}
```

---

### 13. Config Extensions (G19)

Add to `ModConfig.cs`:

```csharp
// ── Cross-Map Autonomy ──
public bool EnableCrossMapAutonomy { get; set; } = true;
public int AutonomyMaxTravelMinutesPerBlock { get; set; } = 60;
public int AutonomyMaxWaitForTargetMinutes { get; set; } = 30;
public int AutonomyCrossMapReplanLimit { get; set; } = 3;
public bool AutonomyPrivateVisitFallbackToPublic { get; set; } = true;
public int AutonomyFaceToFaceDistanceTiles { get; set; } = 6;
public int AutonomyStagingTimeoutTicks { get; set; } = 60;
public int AutonomyStuckDetectionTicks { get; set; } = 120;
public int AutonomyMaterializationMaxRadius { get; set; } = 5;
```

---

### 14. ModEntry Wiring (G1, G23)

The most critical gap: none of the autonomy services are instantiated or hooked. The following must be added to `ModEntry.cs`:

#### Service Fields

```csharp
// ── Autonomy services ──
private AutonomyRuntimeManager? _autonomyRuntimeManager;
private NpcAutonomyGoalEngine? _goalEngine;
private NpcAutonomyPlannerService? _plannerService;
private WorldTopologyService? _worldTopologyService;
private NpcRoutePlannerService? _routePlannerService;
private NpcAutonomyExecutionService? _executionService;
private NpcMaterializationService? _materializationService;
private NpcSocialEncounterService? _encounterService;
private NpcFaceToFaceService? _faceToFaceService;
private NpcSpeechBubbleService? _bubbleService;
private ScheduleOverrideService? _scheduleOverrideService;
private NpcFallbackLineGenerator? _fallbackLineGenerator;
```

#### Entry() — Instantiation

```csharp
// After existing service construction:
_autonomyRuntimeManager = new AutonomyRuntimeManager();
_pairEmotionService = new PairEmotionService(config.PairEmotionMaxDeltaPerCommand, config.PairEmotionMaxDeltaPerDayPerAxis);
_destinationRegistryService = new DestinationRegistryService();
_goalEngine = new NpcAutonomyGoalEngine(_destinationRegistryService, _pairEmotionService, config);
_plannerService = new NpcAutonomyPlannerService(_destinationRegistryService);
_worldTopologyService = new WorldTopologyService();
_routePlannerService = new NpcRoutePlannerService(_worldTopologyService, _destinationRegistryService);
_executionService = new NpcAutonomyExecutionService();
_materializationService = new NpcMaterializationService();
_encounterService = new NpcSocialEncounterService(_pairEmotionService, _npcConversationService, config);
_faceToFaceService = new NpcFaceToFaceService(_bubbleService);
_bubbleService = new NpcSpeechBubbleService(config);
_scheduleOverrideService = new ScheduleOverrideService();
_fallbackLineGenerator = new NpcFallbackLineGenerator();
```

#### Event Hooks

```csharp
// In OnGameLaunched or Entry:
helper.Events.Player.Warped += OnPlayerWarped;     // G23

// Handler:
private void OnPlayerWarped(object? sender, WarpedEventArgs e)
{
    if (!_config.EnableAutonomousRoutines || !_config.EnableCrossMapAutonomy) return;
    _materializationService?.ReconcileMap(e.NewLocation, _autonomyRuntimeManager!, _executionService!);
}
```

---

### 15. Replanning And Failure Recovery (Improved)

The existing `ReplanWithFallback()` appends a single wander block. With cross-map routing, failure recovery needs a richer strategy:

```
HandleBlockFailure(runtime, block, reason):
  runtime.FailedBlocksToday++
  block.Status = Failed
  block.FailureReason = reason  // NEW field (see below)

  SWITCH reason:
    "path_failed":
      // Try alternate route through different intermediate map
      altRoute = RoutePlanner.PlanRoute(graph, ..., excludeEdge=failedEdge)
      IF altRoute exists → patch current block's route
      ELSE → downgrade to fallback

    "door_locked" | "privacy_denied":
      IF config.AutonomyPrivateVisitFallbackToPublic:
        // Redirect to nearest public meeting point
        fallback = DestinationRegistry.ResolveFallbackLocation(...)
        Replace block with Socialize at fallback
      ELSE:
        Skip block entirely

    "target_missing" | "target_left":
      // Target NPC left before we arrived
      Replace block with Wander at current location

    "event_lock":
      // Festival or cutscene started
      Yield to hard lock, mark block Cancelled

    "stuck":
      IF runtime.RetryCount <= config.AutonomyMaxReplansPerBlock:
        Re-issue pathfinding at slightly offset tile
      ELSE:
        Warp NPC home as safety net
        Replace remaining blocks with Rest

    "late_for_anchor":
      // About to miss a hard lock
      Clear current block, insert ReturnHome or BaseAnchor
```

Add `FailureReason` field to `AutonomyPlanBlock`:

```csharp
public string? FailureReason { get; set; }  // path_failed, door_locked, privacy_denied,
                                             // target_missing, target_left, event_lock,
                                             // stuck, late_for_anchor
```

---

## Implementation Tasks (Revised, Ordered by Dependency)

| # | Task | Fixes Gaps | Dependencies | Files |
|---|------|-----------|--------------|-------|
| 1 | Add `AutonomySaveState` to `SaveState`; add persistence in save/load hooks | G2 | — | `State/SaveState.cs`, `ModEntry.cs` |
| 2 | Add cross-map config fields to `ModConfig.cs` and register in GMCM | G19 | — | `Config/ModConfig.cs` |
| 3 | Add `Arrive`, `WaitForTarget` to `AutonomyPlanBlockType`; add `Route`, `TargetTile`, `ETA`, `MaxWaitMinutes`, `FailureReason` to `AutonomyPlanBlock`; add route tracking fields to `AutonomyRuntimeState` | G16, G17, G18 | — | `State/AutonomyRuntimeState.cs` |
| 4 | Implement `AutonomyRuntimeManager` | G10 | — | `Systems/AutonomyRuntimeManager.cs` |
| 5 | Implement `WorldTopologyService` with warp/door discovery and household resolution | G4, G20 | — | `Systems/WorldTopologyService.cs` |
| 6 | Implement `NpcRoutePlannerService` with graph search and per-block route compilation | G5 | 5 | `Systems/NpcRoutePlannerService.cs` |
| 7 | Implement `ScheduleOverrideService` for SMAPI `NPC.Schedule` patching | G3 | 6 | `Systems/ScheduleOverrideService.cs` |
| 8 | Implement `NpcAutonomyExecutionService` with loaded/unloaded movement and arrival detection | G6, G24 | 6, 7 | `Systems/NpcAutonomyExecutionService.cs` |
| 9 | Implement `NpcMaterializationService` with safe tile placement | G7 | 8 | `Systems/NpcMaterializationService.cs` |
| 10 | Implement `NpcSocialEncounterService` with scoring, lifecycle, and consequence hook | G8, G25 | 4 | `Systems/NpcSocialEncounterService.cs` |
| 11 | Implement `NpcFaceToFaceService` with staging state machine | G9, G22 | 10 | `Systems/NpcFaceToFaceService.cs` |
| 12 | Implement `NpcFallbackLineGenerator` with per-beat templates | G11 | — | `Systems/NpcFallbackLineGenerator.cs` |
| 13 | Fix `DestinationRegistryService`: replace substring owner inference with `DefaultMap` lookup; add caching; extend `IsVisitAllowed` for multi-resident homes | G13, G14, G20 | — | `Systems/DestinationRegistryService.cs` |
| 14 | Fix `NpcAutonomyGoalEngine.ScoreCandidateGoals()` to score all world NPCs, not just nearby | G15 | 6 | `Systems/NpcAutonomyGoalEngine.cs` |
| 15 | Fix `NpcAutonomyPlannerService.AdvancePlan()` to use arrival detection, not time-only completion | G24 | 8 | `Systems/NpcAutonomyPlannerService.cs` |
| 16 | Extend `NpcSpeechBubbleService` with encounter-bound bubble queueing and alternation | G22 | 11 | `Systems/NpcSpeechBubbleService.cs` |
| 17 | Wire all services into `ModEntry`: instantiation, `OnDayStarted`, `OnUpdateTicked`, `OnWarped`, `OnSaving`, `OnSaveLoaded` | G1, G23 | 1–16 | `ModEntry.cs` |
| 18 | Add console commands: `slrpg_autonomy_status`, `slrpg_autonomy_plan <npc>`, `slrpg_pair_emotions <npcA> <npcB>`, `slrpg_route_check <npc> <from> <to>`, `slrpg_autonomy_force_replan <npc>`, `slrpg_bubble_test <npc> <text>` | G12 | 17 | `ModEntry.cs` |
| 19 | Add telemetry counting into all new services (route success/fail, materialization, staging outcomes) | — | 17 | All new service files |
| 20 | Unit tests: topology graph building, route planning, access rules, bubble chunking, encounter scoring, staging state machine | — | 1–16 | `Tests/` |
| 21 | Integration regression: multi-day cross-map movement, save/reload mid-route, festival override, denied-home fallback, stuck recovery | — | 17 | `Tests/` or console commands |

---

## Acceptance Scenarios (Revised)

### Cross-Map Movement

| # | Scenario | Expected | Gaps Fixed |
|---|----------|----------|------------|
| 1 | NPC with no hard locks generates a plan with cross-map travel blocks | Plan contains route segments with ETAs | G5, G16 |
| 2 | NPC physically leaves Town and arrives at Mountain via warp | Schedule override applied; NPC follows pathing to warp; appears at destination tile | G3, G6 |
| 3 | Player enters Mountain where an NPC should have arrived 30 game-minutes ago | NPC materialized at expected tile near their destination | G7, G23 |
| 4 | NPC's route passes through a locked building at night | Route planner excludes locked edge; uses alternate path or fails gracefully | G4, G5 |
| 5 | Player saves mid-day while NPCs are in-route | Reload restores runtime state; incomplete routes are recompiled for remaining segments | G2 |

### Visits And Access

| # | Scenario | Expected | Gaps Fixed |
|---|----------|----------|------------|
| 6 | NPC visits Sam's house while being friends with Jodi but not Sam | Access allowed based on best household relationship | G20 |
| 7 | NPC attempts to visit a stranger's home with low urgency | Visit denied; planner re-routes to saloon or town square | G13, G8 |
| 8 | NPC from across the valley visits a friend they never see in vanilla | Goal engine scores distant NPC with travel penalty; still visits if relationship is strong | G15 |

### Face-To-Face Encounters

| # | Scenario | Expected | Gaps Fixed |
|---|----------|----------|------------|
| 9 | Two NPCs arrive at the same map through separate plans | Encounter service scores the meeting; if ≥ threshold, staging begins | G8 |
| 10 | Staging: both NPCs stop, face each other, exchange alternating bubbles | State machine transitions through Approaching→Positioned→Talking→Releasing | G9 |
| 11 | Player opens menu during an active NPC conversation | All staging and bubbles cancelled immediately | G9, G22 |
| 12 | Player2 is offline during a planned encounter | Fallback line generator produces template-based dialogue | G11 |
| 13 | Encounter between tense pair in a private home | Higher encounter score due to privacy fit; intrigue/conflict beat selected | G8 |

### Consequences

| # | Scenario | Expected | Gaps Fixed |
|---|----------|----------|------------|
| 14 | Friendly encounter completes | adjust_npc_pair_emotion: friendship +2, trust +1; record_social_incident logged | G25 |
| 15 | Hostile encounter between rivals | anger +4, tension +3; grudge flag set if thresholds crossed | G25 |
| 16 | Three visits in 7 days between same pair | frequent_visitors flag auto-set | G25 |

### Hard Locks And Safety

| # | Scenario | Expected | Gaps Fixed |
|---|----------|----------|------------|
| 17 | Festival starts while NPC is mid-travel | Block cancelled; NPC yields to vanilla festival schedule | G3 |
| 18 | NPC gets stuck pathing through a narrow passage | Stuck detected after 120 ticks; replan issued; max 3 retries before warp-home | G6 |
| 19 | All autonomy services are called but `EnableAutonomousRoutines` is false | All hooks are no-ops; vanilla behavior preserved | G1 |

### Debugging

| # | Scenario | Expected | Gaps Fixed |
|---|----------|----------|------------|
| 20 | `slrpg_autonomy_plan Abigail` | Prints today's plan blocks with routes, ETAs, and current status | G12 |
| 21 | `slrpg_route_check Abigail Town Mountain` | Prints route segments, estimated minutes, and access checks | G12 |

---

## Risks (Revised)

| Risk | Mitigation | Severity |
|------|------------|----------|
| SMAPI `NPC.Schedule` patching conflicts with other mods that also override schedules | Check for existing overrides before applying; provide config to disable schedule overwriting; restore vanilla on toggle-off | High |
| `Game1.warpCharacter` can produce visual glitches if called at wrong game state | Only warp unloaded/offscreen NPCs; use schedule-based movement for loaded NPCs on the player's map | High |
| Cross-map routing can produce impossibly long travel plans | Config cap `AutonomyMaxTravelMinutesPerBlock = 60`; route planner rejects routes exceeding this | Medium |
| Stuck NPCs that can't be pathed out of tight spaces | Stuck detection at 120 ticks; after 3 retries, safety-warp home; never leave NPC in permanently stuck override state | Medium |
| Materialization makes NPCs appear to "teleport" in front of the player | Only materialize offscreen NPCs; if player is within 8 tiles of expected spawn, delay materialization until player looks away or delay by 1 second | Medium |
| Pair emotion fact-key proliferation from daily delta tracking | Prune fact keys for emotion deltas older than 7 days during daily maintenance | Low |
| Saving large route data in `AutonomySaveState` bloats save file | Routes are marked transient (recompiled on load); only runtime block status and progress markers are persisted | Low |

## Assumptions

- The feature is not complete until autonomous movement is physically represented in-world.
- Hard canon locks still win over self-directed routine blocks.
- Curated private access remains mandatory.
- Player2 may suggest goals or dialogue, but movement legality and execution stay deterministic and local.
- Offscreen abstraction is acceptable only when it preserves coherent player-facing behavior and arrival timing.
- Multi-resident household access is required in v1 (G20 is not deferrable).
- Player farmhouse visits are deferred to v1.1 (G21).
