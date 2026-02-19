# Ambient Consequence Pipeline

This document defines how ambient NPC-to-NPC chatter becomes bounded world changes.

## 1. Policy Matrix

Command policy is context-tagged and enforced before resolver apply.

### Contexts
- `player_chat`: regular NPC chat with the player.
- `manual_*`: explicit player asks (relationship/market/interest).
- `npc_to_npc_ambient`: background NPC ambient exchange lane.
- `auto_*`: deterministic internal automation lane.

### Ambient lane defaults
- Allowed: `record_town_event`, `record_memory_fact`, `publish_rumor`, `publish_article`
- Conditionally enabled (event-threshold gated): `adjust_reputation`, `shift_interest_influence`, `apply_market_modifier`
- Denied by default: direct ambient `propose_quest` and any unknown command

## 2. Event-First Flow

1. Ambient line arrives from Player2.
2. Policy gate validates command for context.
3. Resolver applies allowed commands deterministically.
4. `TownMemory` stores structured events with dedupe/cooldown rules.
5. `AmbientConsequenceService` reads recent events and builds signal snapshots.
6. Auto hooks synthesize bounded intents:
   - social: `adjust_reputation`
   - interest: `shift_interest_influence`
   - market: `apply_market_modifier`
   - quest: `propose_quest` (strict eligibility + anti-repeat)
7. Downstream diegetic surfaces (newspaper, rumor board, market board, NPC chat references) render consequences.

## 3. Cadence and Repetition Controls

- Consecutive-day reuse cooldowns:
  - market crop target re-use checks yesterday
  - interest topic re-use checks yesterday
  - quest motif/target re-use checks yesterday
- Ambient cadence throttle:
  - allowed consequence mutations scale with ambient event count
  - cadence skip telemetry: `Telemetry.Daily.AmbientCadenceSkips`
- Daily axis budgets:
  - social, interest, market, quest mutation caps via auto budget facts

## 4. Safety Boundaries

- All mutations route through `NpcIntentResolver`.
- Fact locks and processed-intent tracking prevent duplicate applies.
- Unknown commands and blocked lane commands reject with explicit reason codes.
- Save compatibility normalization ensures missing/null state fields recover safely.

## 5. Tuning Knobs

Primary constants in `mod/StardewLivingRPG/ModEntry.cs`:
- `AmbientRecordTownEventDailyCap`
- `AmbientPublishRumorMinConfidence`
- `AutoMarketMinSignals`
- `AutoMarketScarcityThreshold`
- `AutoMarketStrongScarcityThreshold`
- `AutoMarketOversupplyThreshold`
- `AutoMarketDeepOversupplyThreshold`
- `AmbientEventsPerAdditionalAutoMutation`
- `AutoMutationBudgetByAxis`

Config in `mod/StardewLivingRPG/Config/ModConfig.cs`:
- `EnableAmbientConsequencePipeline`

## 6. Validation Commands

- `slrpg_intent_smoketest`
- `slrpg_regression_targeted`
- `slrpg_baseline_3day`
- `slrpg_baseline_7day`

Use these in that order for staged validation before release promotion.
