# Autonomous NPC Routines And Social Encounters — Architecture

## Overview

Build a full daily autonomy planner for NPCs so they can develop new routines, roam the valley, visit safe interiors and other NPCs' homes, choose their own activities, and create meeting opportunities even when vanilla scripted schedule spots would never place them together.

This feature replaces the narrower "short detour" concept with a broader system that lets NPCs decide where to go and what to do for much of the day while still preserving hard canon locks and cozy readability.

### Design Inspiration: Dory AI Companion

Several patterns from the [Dory AI gaming companion](https://github.com/friends4payments/Dory) inform this architecture:

- **Layered memory types.** Dory structures memory as episodic (what happened), semantic (what we know), procedural (behavioral patterns), and working (current state). Our pair emotion ledger and NPC memory already cover the semantic and episodic layers. This feature adds a **working memory** layer (active plan state, current goals) and strengthens the **procedural** layer (visit frequency patterns, route preferences) so NPCs develop behavioral habits over time, not just knowledge.
- **Multi-step planning with failure re-planning.** Dory's reasoning engine decomposes a player request into ordered plan steps, executes them sequentially, and re-plans automatically when a step fails. Our `NpcAutonomyPlannerService` adopts the same pattern: synthesize an ordered daily plan, execute block by block, and re-plan into a fallback when a block fails (visit denied, path broken, target missing). The key difference is that Dory plans are player-directed while ours are NPC-self-directed.
- **State snapshot before planning.** Dory captures a full game-state snapshot (position, inventory, health, nearby entities) before asking the LLM to plan. Our goal engine captures an analogous **NPC context snapshot** (current location, time of day, weather, season, pair emotions, recent town events, active cooldowns) before scoring candidate goals.
- **LLM extraction with deterministic validation.** Dory uses an LLM to extract structured data (preferences, goals, personality) from conversations, then stores it in typed MongoDB documents. Our consequence pipeline already does this for ambient NPC chat. This feature extends it so Player2 can *suggest* autonomy goals via structured JSON, which the resolver validates and commits (or rejects) deterministically — the same proposal-validate-commit pattern.
- **System context enrichment for prompts.** Dory builds rich text blocks summarizing everything known about a player and injects them into the LLM system prompt. Our goal engine builds an analogous **NPC autonomy context block** from pair emotions, recent encounters, town events, and current plan state, then injects it when requesting Player2 goal suggestions.

---

## Goals

- Let NPCs create new routine blocks beyond vanilla scripted schedules.
- Let NPCs roam, explore maps, and visit other NPCs' homes when allowed.
- Let NPCs choose activities and destinations for themselves instead of only reacting when already nearby.
- Create more NPC-to-NPC meetings and conversations through visits, shared destinations, and world reactions.
- Show ambient NPC speech as timed in-world talk bubbles with a hard 60-character limit per bubble.
- Let NPC pairs develop durable emotions such as friendship, anger, jealousy, envy, trust, admiration, and awkwardness.
- Keep state mutation deterministic and resolver/service mediated even when Player2 is used for goal or dialogue suggestions.

## Non-Goals

- Fully replacing every vanilla hard lock, festival, sleep rule, or critical scripted scene.
- Giving Player2 direct authority over movement, pathing, save-state writes, or legality checks.
- Replacing vanilla player-facing dialogue.
- Simulating unloaded NPCs with omniscient whole-world fidelity in v1.

## Current State

- Ambient NPC-to-NPC chat already exists and is triggered from `ModEntry` when NPCs happen to be loaded and eligible.
- Ambient pair chat already supports multi-turn exchanges, beat selection, cooldowns, overhear cues, and consequence extraction.
- The mod already has deterministic persistence via `SaveState`, memory services, town memory, and `NpcIntentResolver`.
- Existing social state is too coarse for the requested feature. It only stores broad pair stance/trust and does not support rich pair emotions, active autonomous routines, destination legality, or self-directed daily planning.
- The codebase does not currently own a general-purpose schedule override or daily autonomy planner for NPC movement.

---

## System Architecture

### Component Map

```
┌─────────────────────────────────────────────────────────────────────────┐
│  ModEntry  (composition root — OnDayStarted / OnUpdateTicked)          │
│                                                                         │
│  ┌─────────────────────────┐    ┌──────────────────────────────────┐   │
│  │ NpcAutonomyPlannerService│───▶│ NpcAutonomyGoalEngine            │   │
│  │  • SynthesizeDailyPlan  │    │  • ScoreCandidateGoals            │   │
│  │  • AdvancePlanBlock     │    │  • ValidatePlayer2Suggestion      │   │
│  │  • ReplanOnFailure      │    │  • BuildNpcContextSnapshot        │   │
│  └────────┬────────────────┘    └──────────┬───────────────────────┘   │
│           │                                 │                           │
│           │  reads/writes                   │  reads                    │
│           ▼                                 ▼                           │
│  ┌─────────────────────────┐    ┌──────────────────────────────────┐   │
│  │ AutonomyRuntimeState    │    │ DestinationRegistryService       │   │
│  │  (per-NPC, transient)   │    │  • IsLocationLegal               │   │
│  │  • ActivePlan           │    │  • IsHomeAccessAllowed           │   │
│  │  • CurrentBlock         │    │  • GetReachableDestinations      │   │
│  │  • TargetLocation       │    │  • GetFallbackDestination        │   │
│  │  • RetryCount           │    └──────────────────────────────────┘   │
│  └─────────────────────────┘                                           │
│                                                                         │
│  ┌─────────────────────────┐    ┌──────────────────────────────────┐   │
│  │ NpcSocialEncounterService│───▶│ NpcPairEmotionLedger             │   │
│  │  • EvaluatePlannedVisit │    │  (SaveState.Social extension)    │   │
│  │  • EvaluateOpportunistic│    │  • Affinity, Tension, Avoidance  │   │
│  │  • ScoreEncounter       │    │  • Bounded emotion axes          │   │
│  │  • CommitEncounter      │    │  • Flags (grudge, rivalry, etc.) │   │
│  └────────┬────────────────┘    └──────────────────────────────────┘   │
│           │                                                             │
│           │  triggers                                                   │
│           ▼                                                             │
│  ┌─────────────────────────┐    ┌──────────────────────────────────┐   │
│  │ NpcSpeechBubbleService  │    │ NpcIntentResolver (extended)     │   │
│  │  • ChunkText (≤60 chars)│    │  + adjust_npc_pair_emotion       │   │
│  │  • QueueBubble          │    │  + record_social_incident        │   │
│  │  • RenderActiveBubble   │    │  + set_npc_pair_flag             │   │
│  │  • FallbackLineGen      │    │  + suggest_autonomy_goal         │   │
│  └─────────────────────────┘    └──────────────────────────────────┘   │
│                                                                         │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │  Existing services (unchanged contracts, new consumers)          │  │
│  │  NpcMemoryService · TownMemoryService · AmbientConsequenceService│  │
│  │  NpcConversationService · Player2Client · CommandPolicyService    │  │
│  └──────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────┘
```

### Data Flow: Day Start → Plan → Execute → Encounter → Consequence

```
OnDayStarted
  │
  ├─ 1. DecayPairEmotions()                 // daily decay pass on ledger
  ├─ 2. ResetDailyAutonomyCooldowns()       // per-NPC visit/block budgets
  │
  ├─ 3. FOR each active NPC:
  │      │
  │      ├─ BuildNpcContextSnapshot()        // location, time, weather,
  │      │                                   // pair emotions, recent events,
  │      │                                   // cooldowns, hard locks today
  │      │
  │      ├─ ScoreCandidateGoals(snapshot)    // deterministic scoring
  │      │   ├─ (optional) Player2 suggestion via suggest_autonomy_goal
  │      │   └─ ValidateAndMerge()
  │      │
  │      └─ SynthesizeDailyPlan(goals)       // ordered plan blocks
  │           ├─ base_anchor (if hard lock)
  │           ├─ work / errand / wander / visit_npc / socialize / rest
  │           └─ return_home (curfew anchor)
  │
  └─ 4. Log telemetry (goals proposed/accepted/rejected, plans created)

OnUpdateTicked (every N ticks)
  │
  ├─ FOR each NPC with active plan:
  │      │
  │      ├─ AdvancePlanBlock()
  │      │   ├─ Is current block complete? → advance to next
  │      │   ├─ Is current block failed?   → ReplanOnFailure()
  │      │   └─ Is hard lock imminent?     → yield to vanilla
  │      │
  │      ├─ ApplyScheduleOverride()          // SMAPI schedule patch
  │      │
  │      └─ IF arrived at destination with target NPC present:
  │           │
  │           ├─ NpcSocialEncounterService.EvaluatePlannedVisit()
  │           │   ├─ ScoreEncounter()
  │           │   └─ CommitEncounter() → triggers conversation
  │           │
  │           ├─ NpcConversationService (existing ambient flow)
  │           │   └─ Player2 streams dialogue
  │           │
  │           ├─ NpcSpeechBubbleService.QueueBubble()
  │           │   └─ ChunkText() → sequential timed bubbles
  │           │
  │           └─ Consequence extraction → NpcIntentResolver
  │               ├─ adjust_npc_pair_emotion
  │               ├─ record_social_incident
  │               └─ record_town_event (if public encounter)
  │
  └─ Opportunistic encounters (non-planned co-location):
       └─ NpcSocialEncounterService.EvaluateOpportunistic()
           └─ same scoring/commit/bubble/consequence flow
```

---

## Detailed Component Design

### 1. Daily Autonomy Planner — `NpcAutonomyPlannerService`

**File:** `Systems/NpcAutonomyPlannerService.cs`

**Responsibility:** Synthesize daily activity plans for active NPCs and manage block-by-block execution with failure recovery. Adopts the same plan→execute→replan cycle as Dory's reasoning engine, but driven by NPC self-interest rather than player commands.

#### Plan Block Types

| Block Type       | Description                                        | Preemptable |
|------------------|----------------------------------------------------|-------------|
| `base_anchor`    | Festival, canon scene, locked-duty shift            | No          |
| `travel`         | Transit between locations                           | Yes         |
| `work`           | NPC's occupational duty (shop counter, clinic, etc.)| Mode-dep.   |
| `rest`           | Idle at home or quiet location                      | Yes         |
| `wander`         | Explore a public area without a specific target     | Yes         |
| `visit_npc`      | Deliberate visit to another NPC's location or home  | Yes         |
| `errand`         | Shopping, supply run, item fetch                    | Yes         |
| `socialize`      | Congregate at saloon, square, or event hub          | Yes         |
| `observe_event`  | React to a recent town-memory event at its location | Yes         |
| `return_home`    | Curfew anchor — go home before sleep cutoff         | No          |

#### Key Methods

```csharp
public sealed class NpcAutonomyPlannerService
{
    // Day-start: build a full daily plan from scored goals.
    // Returns null if NPC is fully hard-locked for the day (festival, etc.).
    public NpcDailyPlan? SynthesizeDailyPlan(
        SaveState state,
        string npcId,
        NpcContextSnapshot snapshot,
        List<ScoredGoal> rankedGoals,
        AutonomyConfig config);

    // Tick: advance the active plan by one step.
    // Returns the schedule override to apply, or null if vanilla should run.
    public ScheduleOverride? AdvancePlanBlock(
        SaveState state,
        string npcId,
        AutonomyRuntimeState runtime,
        int timeOfDay);

    // Called when the current block fails (path broken, target missing, access denied).
    // Replaces remaining plan blocks with a safe fallback sequence.
    // Mirrors Dory's re-planning pattern: capture failure context, generate new plan suffix.
    public void ReplanOnFailure(
        SaveState state,
        string npcId,
        AutonomyRuntimeState runtime,
        PlanBlockFailure failure);
}
```

#### Plan Synthesis Algorithm

```
SynthesizeDailyPlan(state, npcId, snapshot, goals, config):
  1. Collect hard-lock windows for today (festivals, sleep, canon scenes)
  2. Insert base_anchor blocks for each hard lock (immovable)
  3. Insert return_home block at curfew time (immovable)
  4. Identify free windows between anchors
  5. FOR each free window, largest first:
       a. Pop highest-value goal that fits in the window duration
       b. Expand goal into block sequence:
            visit_npc → [travel, visit_npc, socialize]
            errand    → [travel, errand, travel]
            wander    → [wander]
       c. Validate all blocks via DestinationRegistryService
       d. If invalid, try next goal; if no goal fits, insert rest/wander
  6. Cap total blocks at config.MaxBlocksPerNpcPerDay
  7. Return ordered NpcDailyPlan
```

#### Failure Re-Planning (Dory Pattern)

When a block fails mid-execution, the planner captures a **failure context** (which block, why it failed, what state the NPC is in now) and generates a replacement suffix — exactly as Dory's reasoning engine captures `failureContext.lastFailedStep` and `failureReason` to re-plan:

```
ReplanOnFailure(state, npcId, runtime, failure):
  1. Mark failed block as status=failed
  2. Capture NpcContextSnapshot at current position
  3. Classify failure:
       path_failed    → try alternate route, then fallback to nearest public location
       access_denied  → downgrade to public socialize or wander near target
       target_missing → substitute with wander at destination map
       time_expired   → skip to next block
       cooldown_hit   → replace with rest or wander
  4. Build replacement blocks for remaining time window
  5. Validate replacement via DestinationRegistryService
  6. Splice replacement into plan, increment runtime.ReplanCount
  7. Log failure + replacement to telemetry
  8. If runtime.ReplanCount > config.MaxReplansPerBlock → abandon and rest
```

---

### 2. Destination And Access Model — `DestinationRegistryService`

**File:** `Systems/DestinationRegistryService.cs`

**Responsibility:** Curated registry of autonomy-safe locations with access rules. Prevents NPCs from entering forbidden or nonsensical locations.

#### Location Record

```csharp
public sealed class AutonomyLocation
{
    public string LocationId { get; init; }          // e.g. "SeedShop", "Mountain", "SamHouse"
    public string DisplayName { get; init; }
    public LocationCategory Category { get; init; }  // Public, SemiPrivate, Private
    public string[] RoleTags { get; init; }           // "home", "shop", "saloon", "square",
                                                      // "nature", "quiet", "work", "clinic"
    public TimeWindow? OpenHours { get; init; }       // null = always accessible
    public string[]? SeasonRestrictions { get; init; } // null = all seasons
    public string[]? WeatherRestrictions { get; init; }// null = all weather
    public string? OwnerNpcId { get; init; }          // null = public/unowned
    public string[]? HouseholdNpcIds { get; init; }   // residents of this location
    public bool RequiresPathability { get; init; }     // false for conceptual locations
}

public enum LocationCategory { Public, SemiPrivate, Private }

public sealed record TimeWindow(int OpenTime, int CloseTime);
```

#### Home Access Policy

Access to `Private` locations (NPC homes) is gated by relationship:

| Visitor Relationship to Owner | Access Rule                                           |
|-------------------------------|-------------------------------------------------------|
| Household/Family              | Always allowed during waking hours                    |
| Friend (affinity ≥ 40)        | Allowed 900–2000, max 2 visits/day to same home       |
| Acquaintance (affinity 10–39) | Allowed 1000–1800, max 1 visit/day, requires reason   |
| Neutral/Stranger (affinity <10)| Denied unless strong goal motivation (urgency ≥ 0.8) |
| Rival (tension ≥ 60)          | Denied to home; public encounters still allowed       |
| Avoidance flag active          | Denied entirely; NPC won't path toward target         |

#### Key Methods

```csharp
public sealed class DestinationRegistryService
{
    // Lookup a location record. Returns null if location is not in the curated registry.
    public AutonomyLocation? GetLocation(string locationId);

    // Check whether a specific NPC may enter a location right now.
    public AccessCheckResult IsLocationLegal(
        string visitorNpcId,
        string locationId,
        int timeOfDay,
        string season,
        string weather,
        NpcPairEmotionEntry? pairEntry);

    // Check whether visitorNpc may enter ownerNpc's home.
    public AccessCheckResult IsHomeAccessAllowed(
        string visitorNpcId,
        string ownerNpcId,
        int timeOfDay,
        NpcPairEmotionEntry? pairEntry,
        float goalUrgency);

    // Get all legal destinations for an NPC at this moment, ranked by suitability.
    public List<RankedDestination> GetReachableDestinations(
        string npcId,
        NpcContextSnapshot snapshot,
        int maxResults = 8);

    // When a planned destination fails, get the best safe fallback.
    public AutonomyLocation GetFallbackDestination(
        string npcId,
        string failedLocationId,
        int timeOfDay);
}

public sealed record AccessCheckResult(
    bool Allowed,
    string? DenialReason);   // "privacy_denied", "closed_hours", "season_blocked",
                              // "weather_blocked", "cooldown", "avoidance_active"

public sealed record RankedDestination(
    AutonomyLocation Location,
    float Score,
    string Reason);
```

#### Curated Location Registry (V1 Seed)

The registry is data-driven, loaded from an embedded JSON or hardcoded dictionary. V1 covers ~30 key Stardew locations:

```
Public:        Town, Beach, Mountain, Forest, Bus Stop, Railroad, Desert,
               Town Square, Saloon (common area), Community Center, Museum
SemiPrivate:   Clinic (lobby), Blacksmith (shop floor), General Store (shop floor),
               Adventurer's Guild, Carpenter's Shop
Private:       All NPC homes (SamHouse, HaleyHouse, ElliottCabin, etc.),
               Clinic (backroom), Wizard Tower
```

---

### 3. Goal Generation — `NpcAutonomyGoalEngine`

**File:** `Systems/NpcAutonomyGoalEngine.cs`

**Responsibility:** Decide what each NPC wants to do today. Generates a ranked list of candidate goals from deterministic scoring, optionally enriched by Player2 suggestions.

#### NPC Context Snapshot

Analogous to Dory's `StateSnapshot` captured before planning. Contains everything the goal engine needs to score candidates:

```csharp
public sealed class NpcContextSnapshot
{
    public string NpcId { get; init; }
    public string CurrentLocationId { get; init; }
    public int Day { get; init; }
    public string Season { get; init; }
    public string Weather { get; init; }
    public int TimeOfDay { get; init; }
    public string Mode { get; init; }                  // cozy_canon, story_depth, living_chaos

    // Social context
    public List<PairEmotionSummary> TopPairEmotions { get; init; }  // top-N most salient pairs
    public List<string> RecentEncounterNpcIds { get; init; }        // NPCs met in last 2 days
    public List<TownEventSummary> RecentTownEvents { get; init; }   // events from last 3 days

    // Constraints
    public List<TimeWindow> HardLockWindows { get; init; }     // festivals, canon duties
    public Dictionary<string, int> VisitCooldowns { get; init; } // locationId → remaining cooldown
    public int BlockBudgetRemaining { get; init; }
    public int HomeVisitBudgetRemaining { get; init; }
}
```

#### Goal Families

```csharp
public enum GoalFamily
{
    VisitFriend,          // Seek out a liked NPC for social time
    VisitFamily,          // Check on a household/family member
    CheckOnRival,         // Confront or observe a tense relationship
    BrowsePublic,         // Wander town, square, beach
    GetFoodDrink,         // Saloon, general store
    SpendTimeInNature,    // Forest, mountain, beach (quiet)
    RunErrand,            // Work-related supply run or shop visit
    RestAlone,            // Stay home, regain energy
    SeekGossip,           // Go where news travels — saloon, square
    PursueUnresolvedEmotion, // Visit NPC with high unresolved tension/admiration/jealousy
    RespondToTownEvent,   // Visit the location of a recent notable event
}
```

#### Scoring Algorithm

Each candidate goal is scored on a 0–1 scale using weighted factors:

```
ScoreGoal(npcId, goal, snapshot) → float:
  base = GoalFamilyBaseWeight[goal.Family]           // 0.1–0.5 per family
  + pairSalience(goal.TargetNpc, snapshot)            // 0.0–0.3 from emotion ledger
  + eventRecency(goal.TargetEvent, snapshot)           // 0.0–0.2 if responding to event
  + locationFit(goal.TargetLocation, snapshot)         // 0.0–0.15 (weather, season match)
  + varietyBonus(goal, snapshot.RecentEncounterNpcIds)  // 0.0–0.15 (haven't done this recently)
  - cooldownPenalty(goal, snapshot.VisitCooldowns)      // -0.3 if recently visited
  - avoidancePenalty(goal.TargetNpc, snapshot)          // -1.0 if avoidance flag active
  - hardLockConflict(goal, snapshot.HardLockWindows)    // -1.0 if overlaps festival
  → clamp [0.0, 1.0]
```

#### Player2 Goal Suggestions

Player2 may propose goals via the `suggest_autonomy_goal` command. These are **proposal-only** — the same pattern as Dory's LLM generating a plan that the executor validates:

```csharp
// Player2 sends this via NpcIntentResolver:
{
    "intent_id": "auto_goal_abigail_d14",
    "command": "suggest_autonomy_goal",
    "npc_id": "Abigail",
    "arguments": {
        "goal_type": "visit_friend",
        "target_npc": "Sebastian",
        "target_location": "SebastianRoom",
        "reason": "Abigail wants to check on Sebastian after the mine collapse event.",
        "urgency": 0.7
    }
}
```

Validation flow:
1. `NpcIntentResolver` validates envelope schema
2. `NpcAutonomyGoalEngine.ValidatePlayer2Suggestion()` checks:
   - `goal_type` is a known `GoalFamily`
   - `target_location` exists in registry and is legal for this NPC
   - `target_npc` is a valid NPC
   - `urgency` is in [0.0, 1.0]
   - No active cooldown blocks this goal
   - No hard lock conflict
3. If valid, inject as a candidate with `base = urgency * 0.6` (capped below deterministic top scores to prevent Player2 from dominating)
4. If invalid, reject with reason logged to telemetry

#### Key Methods

```csharp
public sealed class NpcAutonomyGoalEngine
{
    // Build the context snapshot for an NPC at day start.
    public NpcContextSnapshot BuildNpcContextSnapshot(
        SaveState state,
        string npcId,
        int timeOfDay);

    // Generate and rank all legal candidate goals for an NPC.
    public List<ScoredGoal> ScoreCandidateGoals(
        SaveState state,
        NpcContextSnapshot snapshot);

    // Validate a Player2-suggested goal. Returns null if valid, or denial reason.
    public string? ValidatePlayer2Suggestion(
        SaveState state,
        NpcContextSnapshot snapshot,
        AutonomyGoalSuggestion suggestion);

    // Merge a validated Player2 suggestion into the candidate list.
    public void MergePlayer2Suggestion(
        List<ScoredGoal> candidates,
        AutonomyGoalSuggestion suggestion,
        float urgency);

    // Build a rich text context block for Player2 prompt injection.
    // Mirrors Dory's getSystemContext() — summarizes everything relevant for goal suggestions.
    public string BuildAutonomyContextBlock(
        SaveState state,
        NpcContextSnapshot snapshot);
}

public sealed record ScoredGoal(
    GoalFamily Family,
    string? TargetNpcId,
    string? TargetLocationId,
    string? TargetEventId,
    float Score,
    string Reason);

public sealed record AutonomyGoalSuggestion(
    string GoalType,
    string? TargetNpc,
    string? TargetLocation,
    string? Reason,
    float Urgency);
```

---

### 4. Autonomy Runtime State

**Not persisted to save file.** Transient per-session state held in memory by `ModEntry`, rebuilt on day start.

```csharp
public sealed class AutonomyRuntimeState
{
    public string NpcId { get; init; }
    public NpcDailyPlan? ActivePlan { get; set; }
    public int CurrentBlockIndex { get; set; }
    public string? CurrentTargetLocationId { get; set; }
    public string? CurrentTargetNpcId { get; set; }
    public OverrideStatus OverrideStatus { get; set; }
    public int LastArrivalTime { get; set; }
    public int RetryCount { get; set; }
    public int ReplanCount { get; set; }
    public int BlocksCompletedToday { get; set; }
    public int HomeVisitsToday { get; set; }
    public HashSet<string> VisitedLocationsToday { get; init; } = new();
    public HashSet<string> VisitedNpcsToday { get; init; } = new();
    public Dictionary<string, int> LocationVisitCooldowns { get; init; } = new();
}

public enum OverrideStatus
{
    None,              // vanilla schedule active
    AutonomyActive,    // executing an autonomous block
    HardLockActive,    // yielded to festival / canon / sleep
    FailedFallback     // in fallback after failure
}

public sealed class NpcDailyPlan
{
    public string NpcId { get; init; }
    public int Day { get; init; }
    public List<PlanBlock> Blocks { get; init; } = new();
    public DateTime CreatedAtUtc { get; init; }
}

public sealed class PlanBlock
{
    public string BlockId { get; init; }           // "block_0", "block_1", ...
    public PlanBlockType Type { get; init; }
    public string? TargetLocationId { get; init; }
    public string? TargetNpcId { get; init; }
    public int StartTime { get; init; }            // game time (600 = 6AM, 1400 = 2PM)
    public int EndTime { get; init; }
    public PlanBlockStatus Status { get; set; }
    public string? FailureReason { get; set; }
}

public enum PlanBlockType
{
    BaseAnchor, Travel, Work, Rest, Wander,
    VisitNpc, Errand, Socialize, ObserveEvent, ReturnHome
}

public enum PlanBlockStatus { Pending, Active, Completed, Failed, Skipped }

public sealed record PlanBlockFailure(
    string BlockId,
    PlanBlockType BlockType,
    string TargetLocationId,
    string FailureReason,       // "path_failed", "access_denied", "target_missing",
                                // "time_expired", "cooldown_hit"
    int TimeOfDay);
```

#### Movement Rules

1. **Only route to registered locations.** Unregistered maps are never autonomy targets.
2. **Fail safely.** If SMAPI pathfinding returns no route within 200 ticks, mark block as `Failed` with `path_failed`.
3. **Reroute on failure.** `ReplanOnFailure` replaces remaining blocks with a safe fallback (nearest public location or home rest).
4. **Prevent oscillation.** An NPC may not target the same location more than twice in a single day (`VisitedLocationsToday` check).
5. **Yield to hard locks.** When a hard-lock window is within 30 game-minutes, abort current block and transition to `HardLockActive`.
6. **Max replans per block: 2.** After 2 failed replans for the same time window, insert a rest block and move on.

#### Schedule Override Mechanism

Stardew NPCs follow `ScheduleData` dictionaries. The planner applies overrides by patching the NPC's `Schedule` property at the start of each autonomous block:

```csharp
public sealed record ScheduleOverride(
    string NpcId,
    string TargetLocationId,
    int TargetX,           // tile coordinates within the target map
    int TargetY,
    int FacingDirection,
    int ScheduleTime);     // game time to begin movement
```

The override is applied via SMAPI's `NPC.Schedule` setter or `warpCharacter` for instant transitions when the NPC is on an unloaded map. When vanilla should resume (hard lock or plan end), the override is cleared and the NPC reverts to its original schedule data.

---

### 5. Social Encounters — `NpcSocialEncounterService`

**File:** `Systems/NpcSocialEncounterService.cs`

**Responsibility:** Transform NPC co-location into scored social encounters. Triggers conversations and consequence extraction. Planned visits are the primary generator; opportunistic meetings at shared locations are secondary.

#### Encounter Sources

| Source              | Trigger                                                    | Priority |
|---------------------|------------------------------------------------------------|----------|
| Planned visit       | Visitor NPC arrives at target NPC's location via plan block | High     |
| Opportunistic       | Two NPCs happen to be on the same map during wander/socialize | Medium |
| Event convergence   | Two NPCs both path toward a town-event location            | Medium   |
| Arrival reaction    | NPC arrives at a location where another NPC is already idle | Low      |

#### Encounter Scoring

```
ScoreEncounter(npcA, npcB, location, context) → float [0.0, 1.0]:
  + pairEmotionSalience(npcA, npcB)           // 0.0–0.25 (high affinity OR high tension)
  + recentEventRelevance(npcA, npcB)           // 0.0–0.15 (shared knowledge of recent event)
  + locationSuitability(location)               // 0.0–0.15 (saloon > field for gossip)
  + timeOfDayFit(context.TimeOfDay)            // 0.0–0.1 (evening socializing > early morning)
  + activityCompatibility(npcA.block, npcB.block) // 0.0–0.1 (both socializing > one working)
  + privacyMatch(location, pairEmotion)        // 0.0–0.1 (private for tense talk, public for gossip)
  + modeMultiplier(context.Mode)               // cozy=0.8, story=1.0, chaos=1.2
  - recentEncounterPenalty(npcA, npcB)         // -0.3 if met within last 8 real-minutes
  - dailyEncounterFatigue(npcA)                // -0.1 per encounter already today
  → clamp [0.0, 1.0], threshold = 0.30 to proceed
```

#### Encounter Lifecycle

```
1. ScoreEncounter() → if score ≥ threshold:
2. CommitEncounter()
   a. Register encounter in today's encounter log
   b. Select conversation beat (reuse existing ambient beat system)
   c. Determine turn depth: cozy=2, story=3, chaos=4
   d. Start NpcConversationService flow (existing ambient pipeline)
3. During conversation:
   a. Player2 streams NPC-to-NPC dialogue per turn
   b. NpcSpeechBubbleService displays lines as timed bubbles
   c. Consequence extraction runs after final turn
4. PostEncounterConsequences()
   a. adjust_npc_pair_emotion based on conversation tone
   b. record_social_incident to town memory (if public location)
   c. Update pair familiarity and last_interaction_day
   d. Optionally trigger overhear moment (existing system)
5. Telemetry: encounter_started, encounter_completed, or encounter_cancelled
```

#### Key Methods

```csharp
public sealed class NpcSocialEncounterService
{
    public float ScoreEncounter(
        SaveState state,
        string npcA,
        string npcB,
        string locationId,
        EncounterContext context);

    public EncounterResult? EvaluatePlannedVisit(
        SaveState state,
        string visitorNpcId,
        string hostNpcId,
        string locationId,
        EncounterContext context);

    public EncounterResult? EvaluateOpportunistic(
        SaveState state,
        string npcA,
        string npcB,
        string locationId,
        EncounterContext context);

    public void PostEncounterConsequences(
        SaveState state,
        EncounterResult encounter,
        ConversationOutcome outcome);
}

public sealed record EncounterContext(
    int TimeOfDay,
    string Season,
    string Weather,
    string Mode,
    PlanBlockType? VisitorBlockType,
    PlanBlockType? HostBlockType);

public sealed record EncounterResult(
    string EncounterId,
    string NpcA,
    string NpcB,
    string LocationId,
    float Score,
    string SelectedBeat,
    int TurnDepth,
    EncounterSource Source);

public enum EncounterSource { PlannedVisit, Opportunistic, EventConvergence, ArrivalReaction }
```

---

### 6. Pair Emotion Ledger

**File:** `State/NpcPairEmotionState.cs`  
**Persisted in:** `SaveState.Social.PairEmotions`

The existing `NpcRelationships` dictionary stores coarse stance/trust per NPC. The new pair emotion ledger stores **bilateral, fine-grained emotional state between any two NPC pairs** and drives autonomy, encounter scoring, and home access decisions.

Inspired by Dory's layered memory model: the ledger acts as the **semantic memory** layer (durable knowledge about relationships) while individual encounters act as **episodic memory** (recorded via `NpcMemoryService` and `TownMemoryService`). Repeated encounter patterns form **procedural memory** (visit frequency, route preferences) captured in per-day telemetry and cooldown state.

#### Data Model

```csharp
// Stored in SaveState.Social.PairEmotions
// Key: normalized pair key "abigail|sebastian" (alphabetical, lowercase)
public sealed class NpcPairEmotionEntry
{
    // Core axes — bounded [-100, 100]
    public int Affinity { get; set; }         // positive = warm, negative = cold
    public int Familiarity { get; set; }      // 0 = strangers, 100 = deeply known
    public int Tension { get; set; }          // 0 = calm, 100 = volatile
    public int Avoidance { get; set; }        // 0 = none, 100 = actively avoiding

    // Emotion axes — bounded [0, 100]
    public int Friendship { get; set; }
    public int Anger { get; set; }
    public int Jealousy { get; set; }
    public int Envy { get; set; }
    public int Trust { get; set; }
    public int Admiration { get; set; }
    public int Awkwardness { get; set; }

    // Interaction tracking
    public int LastInteractionDay { get; set; }
    public int InteractionCountLifetime { get; set; }
    public int InteractionCountThisWeek { get; set; }

    // Boolean flags
    public bool Grudge { get; set; }
    public bool RecentHelp { get; set; }
    public bool Rivalry { get; set; }
    public bool SharedSecret { get; set; }
    public bool FrequentVisitors { get; set; }
}
```

#### Mutation Rules

All mutations go through `NpcIntentResolver` via the `adjust_npc_pair_emotion` command:

| Rule                          | Constraint                                           |
|-------------------------------|------------------------------------------------------|
| Max delta per command          | ±5 per axis per command                             |
| Max total delta per day        | ±15 per axis per pair per day                       |
| Bounds                        | Core axes: [-100, 100]. Emotion axes: [0, 100]       |
| Familiarity only increases    | Never decremented; grows by +1–3 per encounter       |
| Avoidance decays daily        | -3 per day unless refreshed by negative encounter    |
| Grudge requires               | Anger ≥ 70 AND tension ≥ 60 to set; clears if anger < 30 |
| FrequentVisitors requires     | ≥ 3 visits in last 7 days (tracked via InteractionCountThisWeek) |
| Daily decay pass              | Anger: -2/day, Jealousy: -1/day, Awkwardness: -2/day, Tension: -1/day |
| Friendship/Trust/Admiration   | No daily decay (durable positive emotions)           |

#### Daily Maintenance

```
DecayPairEmotions(state):
  FOR each pair in state.Social.PairEmotions:
    entry.Anger = max(0, entry.Anger - 2)
    entry.Jealousy = max(0, entry.Jealousy - 1)
    entry.Awkwardness = max(0, entry.Awkwardness - 2)
    entry.Tension = max(0, entry.Tension - 1)
    entry.Avoidance = max(0, entry.Avoidance - 3)
    entry.RecentHelp = false if LastInteractionDay < today - 3
    IF entry.Grudge AND entry.Anger < 30: entry.Grudge = false
    IF dayOfWeek == Monday: entry.InteractionCountThisWeek = 0
    IF entry is all-zero and LastInteractionDay < today - 28: prune pair record
```

---

### 7. Bubble Dialogue Presentation — `NpcSpeechBubbleService`

**File:** `Systems/NpcSpeechBubbleService.cs`  
**UI rendering:** `UI/NpcSpeechBubbleRenderer.cs`

**Responsibility:** Show ambient NPC-to-NPC talk as timed in-world speech bubbles. Reuses the rendering approach from the Town Square Magician HUD, generalized for any live NPC.

#### Text Chunking Rules

```
ChunkText(rawText, maxChars=60) → List<BubbleChunk>:
  1. Normalize: trim whitespace, collapse multiple spaces, strip control chars
  2. If normalized.Length ≤ 60: return single chunk
  3. Split on sentence boundaries (. ! ? followed by space)
  4. If any sentence > 60 chars: split that sentence on clause boundaries (, ; — :)
  5. If any clause > 60 chars: split on last space before char 57, append "..."
  6. Greedily merge adjacent chunks that together fit in 60 chars
  7. Return ordered chunks with computed display durations
```

#### Bubble Timing

```
Duration per chunk:
  base = max(2000ms, charCount * 50ms)    // 60-char chunk = 3000ms
  min = 2000ms                            // readable floor
  max = 5000ms                            // never linger too long
  pause between chunks = 400ms            // breathing room
```

#### Display Rules

- One bubble visible at a time per speaker.
- Bubbles render above the NPC sprite using `SpriteBatch.DrawString` in the `Display.RenderedHud` event, positioned relative to the NPC's screen coordinates.
- Cancel all active bubbles if: encounter breaks, player Opens a menu/dialogue, NPC leaves the map, or player warps away.
- If both NPCs in an encounter have queued bubbles, alternate speakers (A₁ → B₁ → A₂ → B₂).

#### Fallback Line Generation

When Player2 is unavailable, stalled, or returns invalid text, the service generates deterministic fallback lines:

```csharp
public sealed class NpcFallbackLineGenerator
{
    // Returns a short in-character line based on the conversation beat and NPC speech style.
    // Lines are pre-authored templates with slot substitution, not LLM-generated.
    public string GenerateFallbackLine(
        string npcId,
        string beat,           // "gossip", "work", "intrigue", etc.
        NpcSpeechStyle style,  // Professional, Traditionalist, etc.
        string? targetNpcId);
}

// Example fallback templates:
// gossip + Professional: "I heard something interesting today."
// work + Traditionalist: "The farm keeps me busier than usual."
// intrigue + Recluse: "...I noticed something odd earlier."
```

#### Key Methods

```csharp
public sealed class NpcSpeechBubbleService
{
    public List<BubbleChunk> ChunkText(string rawText, int maxChars = 60);

    public void QueueBubble(string npcId, string text, string? sourceEncounterId);

    public void CancelBubblesForNpc(string npcId);

    public void CancelBubblesForEncounter(string encounterId);

    // Called each tick from OnUpdateTicked to advance bubble timers and expire.
    public void Tick(int elapsedMs);

    // Called from OnRenderedHud to draw active bubbles.
    public void Render(SpriteBatch spriteBatch, IEnumerable<NPC> visibleNpcs);
}

public sealed record BubbleChunk(
    string Text,
    int DurationMs,
    int PauseAfterMs);
```

---

### 8. Resolver And Safety Contract Extensions

**File:** `Systems/NpcIntentResolver.cs` (extended)

#### New Command Families

| Command                     | Authority   | Mutates SaveState? | Description                                      |
|-----------------------------|-------------|--------------------|-------------------------------------------------|
| `adjust_npc_pair_emotion`   | Deterministic/Player2 | Yes       | Bounded mutation to pair emotion axes            |
| `record_social_incident`    | Deterministic        | Yes       | Record an encounter as a town memory event       |
| `set_npc_pair_flag`         | Deterministic        | Yes       | Set or clear a pair flag (grudge, rivalry, etc.) |
| `suggest_autonomy_goal`     | Player2 only         | **No**    | Proposal-only; validated and merged into scoring |

#### Command Schemas

```json
// adjust_npc_pair_emotion
{
    "intent_id": "pair_emo_abi_seb_d14_001",
    "command": "adjust_npc_pair_emotion",
    "npc_id": "Abigail",
    "arguments": {
        "target_npc": "Sebastian",
        "adjustments": {
            "friendship": 3,
            "tension": -2,
            "familiarity": 1
        }
    }
}

// record_social_incident
{
    "intent_id": "social_inc_sal_d14_001",
    "command": "record_social_incident",
    "npc_id": "Abigail",
    "arguments": {
        "target_npc": "Sebastian",
        "incident_type": "friendly_visit",
        "location": "SebastianRoom",
        "summary": "Abigail visited Sebastian to check on him after the mine event.",
        "visibility": "local",
        "severity": 1
    }
}

// set_npc_pair_flag
{
    "intent_id": "pair_flag_abi_seb_d14",
    "command": "set_npc_pair_flag",
    "npc_id": "Abigail",
    "arguments": {
        "target_npc": "Sebastian",
        "flag": "frequent_visitors",
        "value": true
    }
}

// suggest_autonomy_goal (proposal only — never mutates state directly)
{
    "intent_id": "auto_goal_abi_d14",
    "command": "suggest_autonomy_goal",
    "npc_id": "Abigail",
    "arguments": {
        "goal_type": "visit_friend",
        "target_npc": "Sebastian",
        "target_location": "SebastianRoom",
        "reason": "Worried about Sebastian after mine collapse.",
        "urgency": 0.7
    }
}
```

#### Validation Rules

| Validation                      | Rule                                                      | Rejection Code        |
|---------------------------------|-----------------------------------------------------------|-----------------------|
| Pair key normalization          | Always alphabetize: `"abigail\|sebastian"`                | —                     |
| Delta bounds                    | Each axis adjustment ∈ [-5, +5]                            | `delta_out_of_bounds` |
| Daily cap check                 | Total absolute delta per axis per pair per day ≤ 15        | `daily_cap_exceeded`  |
| Axis bounds after apply         | Clamp to [-100, 100] or [0, 100] per axis type             | — (auto-clamped)      |
| Flag validity                   | `flag` must be a known flag name                            | `unknown_flag`        |
| Goal type validity              | `goal_type` must map to a known `GoalFamily`                | `unknown_goal_type`   |
| Location legality               | `target_location` must pass `IsLocationLegal`               | `illegal_location`    |
| NPC validity                    | `target_npc` must be a known NPC                            | `unknown_npc`         |
| Idempotency                     | `intent_id` checked against `FactTable.ProcessedIntents`    | `duplicate`           |

#### CommandPolicyService Extension

Add new context `npc_autonomy` for commands originating from the autonomy pipeline:

| Command                     | `player_chat` | `npc_to_npc_ambient` | `npc_autonomy` | `auto_*` |
|-----------------------------|---------------|----------------------|-----------------|----------|
| `adjust_npc_pair_emotion`   | blocked       | allowed              | allowed         | allowed  |
| `record_social_incident`    | blocked       | allowed              | allowed         | allowed  |
| `set_npc_pair_flag`         | blocked       | allowed              | allowed         | allowed  |
| `suggest_autonomy_goal`     | blocked       | blocked              | allowed         | allowed  |

---

## State Model Extensions

### SaveState.Social Addition

```csharp
// In SocialState.cs — add to existing class:
public sealed class SocialState
{
    // ... existing fields ...
    public Dictionary<string, InterestState> Interests { get; set; } = new();
    public Dictionary<string, int> NpcReputation { get; set; } = new();
    public Dictionary<string, RelationshipState> NpcRelationships { get; set; } = new();
    public TownSentimentState TownSentiment { get; set; } = new();

    // NEW: pair emotion ledger
    public Dictionary<string, NpcPairEmotionEntry> PairEmotions { get; set; } = new();
}
```

### TelemetryState Extension

```csharp
// Add to DailyTelemetry:

// Autonomy pipeline
public int AutonomyGoalsProposed { get; set; }
public int AutonomyGoalsAccepted { get; set; }
public int AutonomyGoalsRejected { get; set; }
public int AutonomyPlansCreated { get; set; }
public int AutonomyBlocksCompleted { get; set; }
public int AutonomyBlocksFailed { get; set; }
public int AutonomyReplans { get; set; }
public int ScheduleOverridesApplied { get; set; }
public int ArrivalsSucceeded { get; set; }
public int ArrivalsFailed { get; set; }

// Visitation
public int HomeVisitsAllowed { get; set; }
public int HomeVisitsDenied { get; set; }
public Dictionary<string, int> VisitDenialByReason { get; set; } = new();

// Social encounters
public int EncountersStarted { get; set; }
public int EncountersCompleted { get; set; }
public int EncountersCancelled { get; set; }
public int EncountersPlanned { get; set; }
public int EncountersOpportunistic { get; set; }

// Pair emotions
public int PairEmotionUpdates { get; set; }
public Dictionary<string, int> PairEmotionUpdatesByAxis { get; set; } = new();
public Dictionary<string, int> AutonomyRejectByReason { get; set; } = new();

// Speech bubbles
public int BubblesDisplayed { get; set; }
public int BubblesFallbackUsed { get; set; }
public int BubblesCancelled { get; set; }
```

---

## Configuration Extensions

### ModConfig.cs Additions

```csharp
// ── Autonomous NPC Routines ──────────────────────────────────────────────

// Master toggle
public bool EnableAutonomousRoutines { get; set; } = true;

// Plan limits (per NPC per day)
public int AutonomyMaxBlocksPerDay { get; set; } = 6;
public int AutonomyMaxHomeVisitsPerDay { get; set; } = 2;
public int AutonomyMaxReplansPerBlock { get; set; } = 2;

// Location cooldowns (in-game minutes)
public int AutonomyLocationRevisitCooldownMinutes { get; set; } = 120;
public int AutonomyNpcRevisitCooldownMinutes { get; set; } = 180;

// Encounter cadence
public int AutonomyMinEncounterIntervalMinutes { get; set; } = 8;   // real minutes
public int AutonomyMaxEncountersPerNpcPerDay { get; set; } = 4;
public float AutonomyEncounterScoreThreshold { get; set; } = 0.30f;

// Pair emotion limits
public int PairEmotionMaxDeltaPerCommand { get; set; } = 5;
public int PairEmotionMaxDeltaPerDayPerAxis { get; set; } = 15;

// Speech bubbles
public int BubbleMaxChars { get; set; } = 60;
public int BubbleMinDurationMs { get; set; } = 2000;
public int BubbleMaxDurationMs { get; set; } = 5000;
public int BubblePauseBetweenMs { get; set; } = 400;

// Player2 autonomy integration
public bool EnablePlayer2AutonomySuggestions { get; set; } = true;
public float Player2GoalMaxUrgencyInfluence { get; set; } = 0.6f;

// Mode-specific multipliers (applied to encounter scores and block counts)
// Defaults favor emergence in chaos, restraint in cozy
public float AutonomyIntensityCozy { get; set; } = 0.6f;
public float AutonomyIntensityStory { get; set; } = 1.0f;
public float AutonomyIntensityChaos { get; set; } = 1.4f;
```

---

## ModEntry Integration Points

### OnDayStarted — New Steps

Insert after existing daily tick pipeline (after newspaper build, before portrait refresh):

```
// ── Autonomous NPC Routines ──
if (config.EnableAutonomousRoutines)
{
    // 1. Daily maintenance
    PairEmotionLedger.DecayPairEmotions(state);
    AutonomyRuntimeManager.ResetDailyState();

    // 2. Synthesize plans for all active NPCs
    foreach (var npc in GetActiveNpcs())
    {
        var snapshot = GoalEngine.BuildNpcContextSnapshot(state, npc.Name, Game1.timeOfDay);
        var goals = GoalEngine.ScoreCandidateGoals(state, snapshot);
        var plan = Planner.SynthesizeDailyPlan(state, npc.Name, snapshot, goals, autonomyConfig);
        if (plan != null)
            AutonomyRuntimeManager.SetPlan(npc.Name, plan);
    }

    // 3. Log telemetry
    state.Telemetry.Today.AutonomyPlansCreated = AutonomyRuntimeManager.ActivePlanCount;
}
```

### OnUpdateTicked — New Steps

Insert in the tick loop, gated by a tick-interval check (e.g. every 30 ticks = ~0.5 seconds):

```
// ── Autonomous NPC Routines: tick ──
if (config.EnableAutonomousRoutines && tickCounter % 30 == 0)
{
    foreach (var runtime in AutonomyRuntimeManager.GetActiveRuntimes())
    {
        // Advance plan blocks, apply schedule overrides
        var scheduleOverride = Planner.AdvancePlanBlock(state, runtime.NpcId, runtime, Game1.timeOfDay);
        if (scheduleOverride != null)
            ApplyScheduleOverride(runtime.NpcId, scheduleOverride);

        // Check for encounter opportunities at current location
        if (runtime.OverrideStatus == OverrideStatus.AutonomyActive)
        {
            CheckForEncounterOpportunities(state, runtime);
        }
    }

    // Advance bubble timers
    BubbleService.Tick(tickIntervalMs);
}
```

### OnRenderedHud — New Step

```
// ── Speech bubbles ──
if (config.EnableAutonomousRoutines)
{
    BubbleService.Render(e.SpriteBatch, GetVisibleNpcs());
}
```

---

## Implementation Tasks

| #  | Task                                                                                            | Dependencies | Files                                                    |
|----|------------------------------------------------------------------------------------------------|--------------|----------------------------------------------------------|
| 1  | Add `NpcPairEmotionEntry` and `NpcPairEmotionState.cs`; extend `SocialState` with `PairEmotions` | —           | `State/NpcPairEmotionState.cs`, `State/SocialState.cs`   |
| 2  | Add autonomy runtime state models (`NpcDailyPlan`, `PlanBlock`, `AutonomyRuntimeState`)         | —           | `State/AutonomyRuntimeState.cs`                          |
| 3  | Add autonomy + bubble + encounter telemetry counters to `DailyTelemetry`                        | —           | `State/TelemetryState.cs`                                |
| 4  | Add autonomy config fields to `ModConfig.cs` with defaults; register in GMCM                    | —           | `Config/ModConfig.cs`                                    |
| 5  | Implement `DestinationRegistryService` with curated location data and access rules              | —           | `Systems/DestinationRegistryService.cs`                  |
| 6  | Implement `NpcAutonomyGoalEngine` with context snapshot, scoring, and Player2 validation        | 1, 5        | `Systems/NpcAutonomyGoalEngine.cs`                       |
| 7  | Implement `NpcAutonomyPlannerService` with plan synthesis, block advancement, and re-planning   | 2, 5, 6     | `Systems/NpcAutonomyPlannerService.cs`                   |
| 8  | Implement schedule override application (SMAPI `NPC.Schedule` patching)                         | 7           | `Systems/NpcAutonomyPlannerService.cs` (or helper)       |
| 9  | Implement `NpcSocialEncounterService` with scoring, lifecycle, and consequence extraction        | 1, 5        | `Systems/NpcSocialEncounterService.cs`                   |
| 10 | Implement `NpcSpeechBubbleService` with 60-char chunking, timing, and rendering                 | —           | `Systems/NpcSpeechBubbleService.cs`, `UI/NpcSpeechBubbleRenderer.cs` |
| 11 | Implement `NpcFallbackLineGenerator` for deterministic offline dialogue                         | —           | `Systems/NpcFallbackLineGenerator.cs`                    |
| 12 | Extend `NpcIntentResolver` with 4 new command families and validation rules                     | 1           | `Systems/NpcIntentResolver.cs`                           |
| 13 | Extend `CommandPolicyService` with `npc_autonomy` context                                       | 12          | `Systems/CommandPolicyService.cs`                        |
| 14 | Wire planner, encounter service, and bubble service into `ModEntry` day-start and tick loops     | 6–13        | `ModEntry.cs`                                            |
| 15 | Implement daily pair emotion decay and weekly counter reset                                      | 1, 14       | `ModEntry.cs` or `Systems/PairEmotionMaintenanceService.cs` |
| 16 | Add unit tests: emotion bounds, decay, access rules, bubble chunking, goal scoring              | 1–11        | `Tests/`                                                 |
| 17 | Add regression smoke tests: autonomy conflicts, hard lock override, path failure recovery        | 14–15       | `Tests/` or console commands                             |
| 18 | Add console commands: `slrpg_autonomy_status`, `slrpg_pair_emotions <npcA> <npcB>`, `slrpg_autonomy_plan <npc>` | 14 | `ModEntry.cs` |

---

## Test Cases And Scenarios

### Autonomy Planning

| # | Scenario                                                                                 | Expected                                                         |
|---|------------------------------------------------------------------------------------------|------------------------------------------------------------------|
| 1 | NPC with no hard locks generates a full daily plan with ≤ 6 blocks                        | Plan created, blocks ordered, no overlaps                        |
| 2 | NPC on festival day generates only base_anchor + return_home blocks                       | Autonomous blocks suppressed; hard lock wins                     |
| 3 | NPC plan includes visit_npc to a friend's home at valid time                              | Access allowed, arrival logged                                   |
| 4 | NPC plan includes visit_npc to a stranger's home with low urgency                         | Access denied, planner re-plans to public fallback               |
| 5 | NPC's path to a destination fails (unreachable map)                                       | Block marked failed, re-plan inserts wander at nearest public    |
| 6 | NPC re-plans twice for same block, both fail                                              | Rest block inserted, plan advances past failed window            |
| 7 | Hard lock (festival) starts while NPC is mid-autonomous-block                             | Autonomous block aborted, NPC yields to vanilla                  |

### Social Encounters

| # | Scenario                                                                                 | Expected                                                         |
|---|------------------------------------------------------------------------------------------|------------------------------------------------------------------|
| 8 | Two NPCs that rarely intersect in vanilla both plan to visit the saloon                   | Encounter scored, conversation triggered if score ≥ 0.30         |
| 9 | NPC deliberately visits another NPC's home (planned visit)                                | High-priority encounter, conversation with appropriate beat      |
| 10| Two NPCs meet opportunistically while wandering the same map                              | Lower-priority encounter, still triggers if score threshold met  |
| 11| Encounter between NPCs with active avoidance flag                                         | Score ≤ 0, encounter not triggered                               |
| 12| Encounter in private location between tense pair                                          | Privacy match bonus, intrigue/conflict beat preferred            |

### Pair Emotions

| # | Scenario                                                                                 | Expected                                                         |
|---|------------------------------------------------------------------------------------------|------------------------------------------------------------------|
| 13| Friendly encounter increases friendship +3 and familiarity +1                             | Values clamped within bounds, daily cap tracked                  |
| 14| Negative encounter increases anger +4 and tension +3                                      | If anger reaches 70 and tension 60, grudge flag auto-set         |
| 15| Daily decay reduces anger by 2, jealousy by 1, awkwardness by 2                           | All values decrease, never below 0                               |
| 16| Pair with all-zero emotions and no interaction in 28 days                                  | Pair record pruned from ledger                                   |
| 17| Player2 proposes emotion adjustment with delta > 5                                        | Rejected with `delta_out_of_bounds`                              |
| 18| Positive streak: 3 visits in 7 days                                                       | FrequentVisitors flag auto-set                                   |

### Speech Bubbles

| # | Scenario                                                                                 | Expected                                                         |
|---|------------------------------------------------------------------------------------------|------------------------------------------------------------------|
| 19| 45-character line displayed as single bubble                                              | One bubble, duration = max(2000, 45×50) = 2250ms                |
| 20| 120-character line split into two bubbles                                                 | Two chunks each ≤ 60 chars, shown sequentially with 400ms pause  |
| 21| Player opens menu during active bubble                                                    | All bubbles cancelled immediately                                |
| 22| Player2 returns invalid/empty text                                                        | Fallback line generator produces a template-based line           |
| 23| Both NPCs have queued bubbles                                                             | Alternating display: A₁ → B₁ → A₂ → B₂                         |

### Resolver Safety

| # | Scenario                                                                                 | Expected                                                         |
|---|------------------------------------------------------------------------------------------|------------------------------------------------------------------|
| 24| Player2 suggests goal targeting a closed/invalid location                                 | Rejected with `illegal_location`, state unchanged                |
| 25| Player2 suggests goal with urgency > 1.0                                                  | Rejected with schema validation error                            |
| 26| Duplicate intent_id submitted for pair emotion adjustment                                  | Rejected with `duplicate`, no double-mutation                    |
| 27| Ambient NPC speech text never appears in player's direct dialogue box                      | Bubble-only rendering, no dialogue contamination                 |

---

## Dependencies

- Existing ambient pair chat and consequence pipeline in `ModEntry`.
- Existing `NpcIntentResolver`, `NpcMemoryService`, and `TownMemoryService`.
- Existing Player2 integration for optional goal and dialogue suggestions.
- Existing bubble rendering pattern already used by the Town Square Magician flow.
- SMAPI `NPC.Schedule` API for schedule override application.

## Risks

| Risk                                                                           | Mitigation                                                                                              |
|--------------------------------------------------------------------------------|--------------------------------------------------------------------------------------------------------|
| Full routine override makes NPC behavior feel incoherent                        | Curated location registry, bounded block counts, variety bonus in scoring                              |
| Interior visitation feels invasive or silly                                     | Home access policy with relationship thresholds, time windows, and daily caps                          |
| Player2 produces noisy or repetitive goal proposals                             | Urgency cap (0.6×), deterministic scoring still dominates, cooldown enforcement                        |
| Excessive rerouting strands NPCs or causes pathing churn                        | Max 2 replans per block, fallback-to-rest as final safety net                                          |
| Pair emotions drift too fast                                                    | ±5 per command, ±15 per day per axis, daily decay, strict bounds                                       |
| Frequent bubble chatter overwhelms the player                                   | Mode-specific encounter cadence, daily encounter cap, 8-minute real-time cooldown between encounters   |
| Stale pair records accumulate over long saves                                   | 28-day prune for all-zero inactive pairs                                                               |
| Schedule override conflicts with other SMAPI mods                               | Override is additive (patches current schedule entry); check for existing overrides before applying     |

## Assumptions

- The feature should favor emergence over strict fidelity to ordinary vanilla schedule spots.
- "Mostly autonomous" means regular daily expectations may be skipped, but hard canon and safety-critical locks still apply.
- Curated access to interiors and homes is required in v1.
- Player2 is allowed to suggest goals, but it is never authoritative for movement legality or save-state mutation.
- V1 focuses on loaded-game NPCs and reachable live movement rather than a full offline simulation for unloaded actors.
- The pair emotion ledger is a **hidden** layer; the existing coarse `NpcRelationships.Stance`/`Trust` remains the player-visible social signal.

---

## Console Commands

| Command                                    | Description                                            |
|--------------------------------------------|--------------------------------------------------------|
| `slrpg_autonomy_status`                    | Print all active autonomy plans and runtime states     |
| `slrpg_autonomy_plan <npc>`                | Print the daily plan for a specific NPC                |
| `slrpg_pair_emotions <npcA> <npcB>`        | Print the pair emotion ledger entry                    |
| `slrpg_pair_emotions_all`                  | Print all non-zero pair emotion entries                 |
| `slrpg_destination_check <npc> <location>` | Test destination legality for an NPC at current time    |
| `slrpg_autonomy_force_replan <npc>`        | Force a re-plan for a specific NPC (debug)             |
| `slrpg_bubble_test <npc> <text>`           | Force-display a speech bubble on an NPC (debug)        |
