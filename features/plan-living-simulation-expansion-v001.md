# Living Simulation Expansion Plan v001

## Overview
This plan upgrades Stardew Living RPG toward a true living simulation where NPC-to-NPC ambient behavior produces believable world changes through deterministic systems, not direct free-form mutation. The key design is event-first simulation: ambient dialogue creates structured town events, and services convert those events into bounded social, economic, and narrative outcomes.

## Goals
- Make ambient NPC conversations impact more than `publish_article` and `publish_rumor`.
- Preserve deterministic safety by routing all world mutations through validated command lanes.
- Keep player-facing outcomes coherent across chat, rumor board, newspaper, and market board.
- Maintain cozy balance by adding cooldowns, caps, and anti-repetition gates.
- Make outcomes feel earned over time instead of instantly scripted.

## Non-Goals
- Replacing vanilla hearts with a new relationship system.
- Letting Player2 directly mutate save state outside resolver/service guardrails.
- Building a full autonomous simulation that ignores player actions.
- Reworking multiplayer core architecture in this initiative.

## Current State
- Ambient prompt in `ModEntry` is currently biased toward `publish_article` and `publish_rumor`.
- Spawned NPC command schema already includes `propose_quest`, `adjust_reputation`, `shift_interest_influence`, `apply_market_modifier`, publish commands, and event/memory commands.
- `NpcIntentResolver` supports those commands with bounded validation.
- Automatic exposure hooks exist (`adjust_reputation`, `shift_interest_influence`, `apply_market_modifier`) but are mostly state-snapshot driven, not strongly event-pipeline driven.
- Ambient and player chat routing exists, but needs careful policy separation and telemetry to avoid cross-lane side effects.

## Proposed Architecture
1. Ambient Event Lane
- Ambient NPC-to-NPC messages should prefer `record_town_event` (and optional `record_memory_fact`) as primary output.
- Publish commands become optional byproducts when event visibility/confidence is high.

2. Deterministic Consequence Lane
- New event-to-consequence hooks synthesize safe intents from recent events:
- Social outcomes -> `adjust_reputation`.
- Group pressure outcomes -> `shift_interest_influence`.
- Market anomaly outcomes -> `apply_market_modifier`.
- Quest opportunities -> `propose_quest` only when player-facing conditions are met.

3. Context-Aware Command Policy
- Per-context policy matrix (`player_chat`, `manual_*`, `npc_to_npc_ambient`, `auto_*`) defines which commands are allowed and under what gates.

4. Event Memory and Decay
- Event confidence, recency windows, source corroboration, and decay prevent repetitive or unrealistic swings.

5. Diegetic Visibility
- Outcomes should appear in-world through newspaper headlines, rumor board updates, market shifts, and NPC dialogue references.

## Changes Needed
- `mod/StardewLivingRPG/ModEntry.cs`
- `mod/StardewLivingRPG/Systems/NpcIntentResolver.cs`
- `mod/StardewLivingRPG/Systems/TownMemoryService.cs`
- `mod/StardewLivingRPG/Systems/NewspaperService.cs`
- `mod/StardewLivingRPG/Systems/RumorBoardService.cs`
- `mod/StardewLivingRPG/Systems/EconomyService.cs`
- `mod/StardewLivingRPG/State/*` (if event metadata extension is required)
- Add new service(s) if needed:
- `mod/StardewLivingRPG/Systems/AmbientConsequenceService.cs`
- `mod/StardewLivingRPG/Systems/CommandPolicyService.cs`

## Tasks (Numbered)

### Phase 0: Baseline and Safety Harness
1. [x] Add debug snapshot command output for ambient lanes (`contextTag`, command type, apply/reject reason, source NPC). (completed 2026-02-19 14:19)
2. [x] Add counters for ambient command attempts by type (applied, rejected, duplicate). (completed 2026-02-19 14:21)
3. [x] Add unit-like resolver smoke tests for each command under ambient context. (completed 2026-02-19 14:25)
4. [x] Add regression checks for player chat routing to ensure ambient lines never appear in player chat. (completed 2026-02-19 14:27)
5. [x] Define baseline metrics from a 3-day simulation run: command distribution, quest rate, market modifier rate. (completed 2026-02-19 14:31)

### Phase 1: Command Policy Matrix
6. [x] Implement `CommandPolicyService` with allow/deny rules per context tag. (completed 2026-02-19 14:32)
7. [x] Define ambient allowed commands: `record_town_event`, `record_memory_fact`, `publish_rumor`, `publish_article`. (completed 2026-02-19 14:33)
8. [x] Define ambient conditional commands (disabled by default, enabled only via event thresholds): `adjust_reputation`, `shift_interest_influence`, `apply_market_modifier`. (completed 2026-02-19 14:34)
9. [x] Enforce policy at command-application boundary before resolver apply. (completed 2026-02-19 14:36)
10. [x] Add explicit reject reason codes for policy failures (for telemetry and tuning). (completed 2026-02-19 14:37)
11. [x] Update system prompts to reflect policy and reduce invalid command attempts. (completed 2026-02-19 14:39)

### Phase 2: Ambient Event-First Generation
12. [x] Rewrite ambient prompt to request event capture first (`record_town_event`) and command sparing behavior. (completed 2026-02-19 14:41)
13. [x] Require event fields quality: kind, summary, location, severity, visibility, tags. (completed 2026-02-19 14:43)
14. [x] Add anti-noise gate: ignore low-information ambient outputs. (completed 2026-02-19 14:44)
15. [x] Add duplicate suppression for semantically similar events in short windows. (completed 2026-02-19 14:46)
16. [x] Add per-NPC ambient cooldown and per-day ambient event cap. (completed 2026-02-19 14:49)
17. [x] Ensure publish commands are only used when confidence/visibility criteria are met. (completed 2026-02-19 14:51)

### Phase 3: Event to Consequence Conversion
18. [x] Create `AmbientConsequenceService` that reads recent `TownMemory` events. (completed 2026-02-19 14:52)
19. [x] Implement social converter: event patterns -> bounded `adjust_reputation` intents. (completed 2026-02-19 14:53)
20. [x] Implement interest converter: repeated topical events -> bounded `shift_interest_influence` intents. (completed 2026-02-19 14:55)
21. [x] Implement market converter: anomaly events + economy signals -> bounded `apply_market_modifier` intents. (completed 2026-02-19 14:57)
22. [x] Implement quest converter: eligible event motifs -> `propose_quest` candidates (with strict request gating). (completed 2026-02-19 14:58)
23. [x] Enforce idempotency keys per converter to avoid duplicate applications in same day. (completed 2026-02-19 14:59)
24. [x] Add per-axis daily mutation budgets to keep simulation cozy and legible. (completed 2026-02-19 15:00)

### Phase 4: Personality and Relationship Gating
25. [x] Add NPC archetype behavior weights for ambient outputs (civic, merchant, recluse, enthusiast, etc.). (completed 2026-02-19 15:01)
26. [x] Use hearts + mod reputation + personality for acceptance/rejection likelihood of asks. (completed 2026-02-19 15:02)
27. [x] Add refusal/defer templates for natural declines. (completed 2026-02-19 15:04)
28. [x] Tie willingness to context factors: weather, time, day-of-week, prior interactions. (completed 2026-02-19 15:05)
29. [x] Add anti-familiarity tone gates for low-heart relationships in ambient references. (completed 2026-02-19 15:06)

### Phase 5: Diegetic Feedback Integration
30. [x] Newspaper: prioritize meaningful ambient-derived events for headline candidates. (completed 2026-02-19 15:08)
31. [x] Rumor Board: surface event-derived opportunities with non-repetitive targets. (completed 2026-02-19 15:10)
32. [x] Market Board: show shifts linked to market-event causes without exposing internals. (completed 2026-02-19 15:11)
33. [x] NPC chat: include references to recent public events when contextually relevant. (completed 2026-02-19 15:13)
34. [x] Add subtle HUD/notification hooks for major simulation changes (no spam). (completed 2026-02-19 15:14)

### Phase 6: Balance and Coherence Tuning
35. [x] Tune thresholds for scarcity/oversupply and event confidence to reduce false positives. (completed 2026-02-19 15:16)
36. [x] Tune reward bands for event-derived quests against live market values. (completed 2026-02-19 15:17)
37. [x] Add cooldowns to prevent same crop/topic overuse across consecutive days. (completed 2026-02-19 15:26)
38. [x] Add cadence controls so not every ambient exchange causes a world mutation. (completed 2026-02-19 15:27)
39. [x] Run 7-day simulation scenarios and compare against baseline metrics. (completed 2026-02-19 15:28)

### Phase 7: Hardening and Release
40. [x] Add migration logic if new state fields are introduced. (completed 2026-02-19 15:30)
41. [x] Add compatibility checks for existing saves and fallback defaults. (completed 2026-02-19 15:30)
42. [x] Add targeted regression suite for: chat routing, pass-out publication, market outlook, ambient command lane. (completed 2026-02-19 15:32)
43. [x] Prepare developer docs for policy matrix, event pipeline, and tuning knobs. (completed 2026-02-19 15:34)
44. [x] Ship behind a config flag (`EnableAmbientConsequencePipeline`) and run staged validation. (completed 2026-02-19 15:35)
45. [x] Promote feature flag to default after stability and telemetry targets are met. (completed 2026-02-19 15:35)

## Dependencies
- Stable Player2 connectivity and command output formatting.
- `NpcIntentResolver` validation schema remains authoritative.
- Existing telemetry/state persistence remains deterministic.
- Market and rumor systems must expose required hooks for converter integration.

## Risks
- Over-triggered mutations can make the world feel noisy or scripted.
- Under-triggered mutations can make ambient chat feel cosmetic.
- Cross-lane leakage (ambient -> player chat) can break immersion.
- Save migration mistakes can corrupt or flatten progression.
- Prompt drift may increase invalid command attempts.

## Acceptance Criteria by Phase
- Phase 1: Ambient command policy enforced with measurable reject reasons.
- Phase 2: Ambient runs produce mostly structured events, low invalid command rate.
- Phase 3: At least 3 command families can be indirectly driven by ambient events through deterministic converters.
- Phase 4: Low-heart NPCs visibly reject/defer more often than high-heart NPCs.
- Phase 5: Players can observe ambient consequences in at least 3 diegetic surfaces.
- Phase 6: 7-day runs show varied outcomes without repetitive crop/topic loops.
- Phase 7: Feature-flagged rollout passes compile, save-load, and regression checks.

## Rollout Order Recommendation
1. Phase 0 and Phase 1 first (safety and control).
2. Phase 2 and Phase 3 next (event-first behavior and real consequences).
3. Phase 4 and Phase 5 for realism and player-facing coherence.
4. Phase 6 and Phase 7 for tuning and release hardening.
