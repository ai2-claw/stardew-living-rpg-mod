# Feature Plan: Varied NPC Commands (v001)

## Overview
Expand the NPC command contract so Player2 NPCs can drive more than quest offers and news posts. The goal is a more varied, believable Living RPG loop where NPCs can remember interactions, surface town developments, and shift town mood in bounded deterministic ways.

## Goals
- Add a first batch of new deterministic NPC commands that increase interaction variety.
- Keep all mutations safe, bounded, idempotent, and compatible with existing fact-lock patterns.
- Reduce repetitive quest-first behavior by giving NPCs valid non-quest command paths.
- Expose `adjust_reputation`, `shift_interest_influence`, and `apply_market_modifier` as first-class proposed commands.
- Define both automatic (system-driven) and manual (player-triggered) paths for those commands.
- Ensure NPCs can naturally reject player asks when context is not appropriate.
- Keep additive dialogue policy intact (never replace vanilla lines).
- Preserve current stability and performance characteristics.

## Non-Goals
- Replacing vanilla Stardew dialogue flow.
- Adding unbounded or free-form state write commands.
- Building new major gameplay systems (pathfinding, schedule simulation, combat logic).
- Redesigning existing quest templates in this iteration.
- Multi-command transaction support in one response line.

## Current State
- Resolver currently supports: `propose_quest`, `adjust_reputation`, `shift_interest_influence`, `apply_market_modifier`, `publish_rumor`, `publish_article`.
- External schema file (`NPC_COMMAND_SCHEMA.json`) does not yet reflect full runtime support (`publish_article` drift).
- NPC spawn command payloads currently expose only `propose_quest`, `publish_article`, and `publish_rumor`.
- `adjust_reputation`, `shift_interest_influence`, and `apply_market_modifier` are resolver-supported but not broadly exposed in NPC spawn command descriptors and prompt routing.
- Existing services already support memory/town context persistence:
  - `NpcMemoryService.WriteFact(...)`
  - `TownMemoryService.RecordEvent(...)`
- Ask handling is still too permissive in some paths, so NPCs may agree too often and feel scripted.
- Current prompts strongly bias NPCs toward quest offers, which can feel scripted and repetitive.

## Proposed Architecture
### 1) Command Expansion v1
Add three new commands focused on social continuity and world texture.

- `record_memory_fact`
  - Purpose: let NPCs persist meaningful player-specific facts for future dialogue continuity.
  - Arguments:
    - `category` (enum: `preference|promise|event|relationship`)
    - `text` (string, 8..140 chars)
    - `weight` (int 1..5, optional default 2)
  - Apply path: `NpcMemoryService.WriteFact(state, npcName, category, text, day, weight)`
  - Safety:
    - max 2 accepted memory facts per NPC per day
    - reject empty/duplicate text

- `record_town_event`
  - Purpose: capture in-world happenings that can appear in later NPC chatter/newspaper context.
  - Arguments:
    - `kind` (enum: `market|social|nature|incident|community`)
    - `summary` (string, 12..160 chars)
    - `location` (string, 2..40 chars)
    - `severity` (int 1..5)
    - `visibility` (enum: `local|public`)
    - `tags` (array of up to 5 short strings, optional)
  - Apply path: `TownMemoryService.RecordEvent(...)`
  - Safety:
    - max 2 accepted town events per game day globally from NPC commands
    - rely on existing dedupe in `TownMemoryService`

- `adjust_town_sentiment`
  - Purpose: let NPC conversations create gentle shifts in town mood without direct economy hacks.
  - Arguments:
    - `axis` (enum: `economy|community|environment`)
    - `delta` (int -5..5)
    - `reason` (string, optional, <= 120 chars)
  - Apply path: mutate `state.Social.TownSentiment.<axis>` with clamp `[-100, 100]`
  - Safety:
    - max absolute net shift per axis per day from NPC commands: 10
    - max 1 accepted sentiment change per NPC per axis per day

### 2) Contract Alignment
Eliminate command drift between runtime and schema.
- Update `NPC_COMMAND_SCHEMA.json` to include all runtime commands (including `publish_article`) and new v1 commands.
- Keep resolver allow-list and spawn command metadata synchronized with schema.

### 3) Prompt and Spawn Payload Updates
- Add new command descriptors to all NPC spawn payload command lists.
- Update NPC system prompts to prefer context-appropriate command diversity:
  - greeting/small talk -> likely no command
  - continuity moments -> `record_memory_fact`
  - notable town happenings -> `record_town_event`
  - major mood shifts -> `adjust_town_sentiment`
  - explicit task asks -> `propose_quest`

### 4) Exposure Model for Existing Commands (Automatic + Manual)
Expose `adjust_reputation`, `shift_interest_influence`, and `apply_market_modifier` through two intent lanes.

- Automatic lane (not player-triggered):
  - `adjust_reputation`:
    - Trigger on quest lifecycle outcomes (accept/complete/fail) and high-signal conversation outcomes.
    - Use strict per-NPC/day cap and small bounded deltas.
  - `shift_interest_influence`:
    - Trigger during daily simulation hooks when repeated town topics/events cluster around an interest group.
    - Use global daily cap plus per-interest cooldown.
  - `apply_market_modifier`:
    - Trigger only on significant market anomalies (scarcity/oversupply/mover thresholds).
    - Restrict to relevant NPC archetypes and enforce short durations.

- Manual lane (player-triggered):
  - Add optional conversation intents in NPC chat opener/menu:
    - relationship-focused ask -> allows `adjust_reputation`
    - town politics/community ask -> allows `shift_interest_influence`
    - market outlook ask -> allows `apply_market_modifier`
  - Pass explicit interaction context in chat payload (`intent_mode=manual`, `intent_topic=<relationship|interest|market>`) so NPC proposals are contextual instead of random.
  - Keep player-facing flow additive and optional; no forced extra menu layer.

Exposure matrix:

| Command | Auto Trigger | Manual Trigger | Primary Gate | Daily Cap |
| --- | --- | --- | --- | --- |
| `adjust_reputation` | quest accept/complete/fail outcome; repeated positive/negative social tone | relationship ask path in npc chat | heart/reputation threshold + per-npc cooldown | per npc/target: 2 applies |
| `shift_interest_influence` | clustered town events and topic trend in daily tick | town politics/community ask path | valid interest + per-interest cooldown | per interest: 2 applies |
| `apply_market_modifier` | scarcity/oversupply/mover anomaly threshold crossed | market outlook ask path | anomaly present + npc archetype allow-list | global: 2 applies |

Lane metadata carried in intents/logs:
- `intent_lane=auto|manual`
- `intent_topic=relationship|interest|market|none`

### 5) Deterministic Safety and Observability
- Extend fact-lock keys for new command cooldown gates.
- Add explicit reject reasons/codes for new range and cap failures.
- Extend smoke tests for valid/invalid envelopes and cap behavior.
- Keep one accepted mutating command per resolver call.
- Track source lane in telemetry/logs (`auto` vs `manual`) for balancing.

### 6) Natural Ask Rejection Model (Believability Gate)
Before applying player-triggered asks that would lead to mutating commands, run a deterministic NPC decision gate.

- Inputs:
  - NPC personality profile (e.g., professional/traditionalist/recluse)
  - Relationship strength (hearts + mod reputation)
  - Ask type and burden (simple chat vs favor vs risky market manipulation)
  - NPC context (time of day, weather, open-shop/busy windows, recent interactions)
  - Memory/context signals (recent promises, conflicts, repeated asks, daily caps)

- Output:
  - `accept` -> allow normal command proposal/resolve flow.
  - `reject` -> no mutating command is applied; NPC returns an in-character decline line with reason tone.
  - `defer` -> no mutation now; NPC suggests a believable follow-up condition (later time/day, improve trust, bring proof/items).

- Design constraints:
  - Deterministic and bounded (same inputs -> same decision for that tick/day context).
  - No immersion-breaking phrasing; rejection reasons are diegetic and personality-aligned.
  - Rejection should feel contextual, not random refusal.

### 7) Finalized Command Specs (Task 1)
Finalized v1 command contract and reject code baseline:

- `record_memory_fact`
  - Arguments:
    - `category` enum: `preference|promise|event|relationship`
    - `text` string length: 8..140
    - `weight` integer: 1..5 (default 2)
  - Caps:
    - max accepted per npc/day: 2
  - Reject codes:
    - `E_MEMORY_CATEGORY_INVALID`
    - `E_MEMORY_TEXT_INVALID`
    - `E_MEMORY_WEIGHT_RANGE`
    - `E_MEMORY_DAILY_CAP`
    - `E_MEMORY_DUPLICATE`

- `record_town_event`
  - Arguments:
    - `kind` enum: `market|social|nature|incident|community`
    - `summary` string length: 12..160
    - `location` string length: 2..40
    - `severity` integer: 1..5
    - `visibility` enum: `local|public`
    - `tags` optional array max length: 5; each tag length 2..24
  - Caps:
    - max accepted globally/day from npc commands: 2
  - Reject codes:
    - `E_TOWN_EVENT_KIND_INVALID`
    - `E_TOWN_EVENT_SUMMARY_INVALID`
    - `E_TOWN_EVENT_LOCATION_INVALID`
    - `E_TOWN_EVENT_SEVERITY_RANGE`
    - `E_TOWN_EVENT_VISIBILITY_INVALID`
    - `E_TOWN_EVENT_TAGS_INVALID`
    - `E_TOWN_EVENT_DAILY_CAP`

- `adjust_town_sentiment`
  - Arguments:
    - `axis` enum: `economy|community|environment`
    - `delta` integer: -5..5
    - `reason` optional string length: 0..120
  - Caps:
    - max absolute net shift per axis/day from npc commands: 10
    - max accepted per npc/axis/day: 1
  - Reject codes:
    - `E_SENTIMENT_AXIS_INVALID`
    - `E_SENTIMENT_DELTA_RANGE`
    - `E_SENTIMENT_REASON_INVALID`
    - `E_SENTIMENT_DAILY_AXIS_CAP`
    - `E_SENTIMENT_NPC_AXIS_CAP`

- Existing command exposure baseline (for automatic + manual lanes):
  - `adjust_reputation`: delta -10..10, add lane-aware cap/diagnostic rejects.
  - `shift_interest_influence`: delta -5..5, add per-interest/day gate rejects.
  - `apply_market_modifier`: delta_pct -0.15..0.15 and duration 1..7, add anomaly-gated auto exposure rejects.

## Changes Needed
- `NPC_COMMAND_SCHEMA.json`
  - Add `publish_article` contract (if missing) and three new commands.
- `mod/StardewLivingRPG/Systems/NpcIntentResolver.cs`
  - Extend allow-list.
  - Add three command handlers and validation.
  - Add daily cap checks and fact key gates.
- `mod/StardewLivingRPG/ModEntry.cs`
  - Add new spawn command descriptors for primary and roster NPC spawn requests.
  - Update system prompt guidance to reduce quest-only behavior and support exposure lanes.
  - Add automatic trigger hooks (day start/day end/quest outcome) for resolver-safe command opportunities.
  - Extend `slrpg_intent_smoketest` with new command coverage.
- `mod/StardewLivingRPG/UI/NpcChatInputMenu.cs`
  - Add manual intent entry points for relationship/interest/market-triggered command proposals.
- `mod/StardewLivingRPG/Systems/NpcSpeechStyleService.cs`
  - Provide rejection tone templates aligned to NPC verbal profiles.
- `mod/StardewLivingRPG/Systems/` (new: ask gate service)
  - Add deterministic ask acceptance/rejection evaluator used before player-triggered mutation paths.
- `mod/StardewLivingRPG/Config/ModConfig.cs`
  - Add optional toggles/caps for automatic command exposure frequency.
- `mod/StardewLivingRPG/README.md` and `ARCHITECTURE.md`
  - Update supported command list, exposure model, and safety notes.
- `CHANGELOG.md`
  - Add behavior and safety updates.

## Tasks
- [x] 1. Finalize v1 command specs (argument contract, caps, reject codes). (Completed: 2026-02-18 07:07)
- [x] 2. Define exposure matrix for `adjust_reputation`, `shift_interest_influence`, and `apply_market_modifier` across `auto` and `manual` lanes. (Completed: 2026-02-18 07:08)
- [x] 3. Update `NPC_COMMAND_SCHEMA.json` to match runtime and include new commands. (Completed: 2026-02-18 07:08)
- [x] 4. Extend resolver allow-list and command switch for new handlers. (Completed: 2026-02-18 07:09)
- [x] 5. Implement `record_memory_fact` resolver path using `NpcMemoryService`. (Completed: 2026-02-18 07:10)
- [x] 6. Implement `record_town_event` resolver path using `TownMemoryService`. (Completed: 2026-02-18 07:11)
- [x] 7. Implement `adjust_town_sentiment` resolver path with axis/day caps. (Completed: 2026-02-18 07:12)
- [x] 8. Add fact-lock/cooldown helpers for new command limits. (Completed: 2026-02-18 07:12)
- [x] 9. Expose existing commands in spawn payload descriptors and prompt contracts. (Completed: 2026-02-18 07:13)
- [x] 10. Implement automatic exposure hooks (quest lifecycle + daily simulation + market anomaly checks). (Completed: 2026-02-18 07:16)
- [x] 11. Implement manual exposure hooks in NPC chat menu/intents. (Completed: 2026-02-18 07:17)
- [x] 12. Implement deterministic ask acceptance/rejection gate with factors: personality, relationship, ask burden, context, and recent history. (Completed: 2026-02-18 07:19)
- [x] 13. Add rejection/defer response templates and prompt rules for in-character decline behavior. (Completed: 2026-02-18 07:20)
- [x] 14. Add lane-aware telemetry/logging (`auto` vs `manual`) plus decision outcome (`accept|reject|defer`) diagnostics. (Completed: 2026-02-18 07:23)
- [x] 15. Expand `slrpg_intent_smoketest` with positive and negative coverage (including lane gates and rejection logic). (Completed: 2026-02-18 07:24)
- [~] 16. Run in-game validation pass for varied interactions (no immediate quest spam, no command floods, believable refusals). (Blocked: local SMAPI runtime validation unavailable in this environment; build output path is currently locked. Checked: 2026-02-18 07:24)
- [x] 17. Update docs/changelog to keep contract and behavior aligned. (Completed: 2026-02-18 07:25)

## Dependencies
- `NpcMemoryService` and `TownMemoryService` must be injectable/accessible from resolver call paths.
- Player2 command/tool invocation must accept expanded command list from spawn payloads.
- Existing fact table and telemetry pathways must remain backward compatible.
- NPC chat UI must support manual intent prompts without breaking additive dialogue flow.
- Daily tick hooks and market signals must be available to automatic exposure checks.
- Relationship/heart-level context must be available at decision time for believable rejection logic.

## Risks
- Over-mutation risk from more commands.
  - Mitigation: strict caps, clamped ranges, per-day gates, deterministic rejects.
- Schema/runtime drift risk.
  - Mitigation: enforce single source of truth workflow and smoke test coverage.
- NPC output quality still biased to quests.
  - Mitigation: prompt updates plus command availability and explicit variety rules.
- Automatic lane could become spammy or feel random.
  - Mitigation: trigger thresholds, per-command daily budgets, per-NPC cooldown keys, and lane telemetry audits.
- Manual lane could overwhelm players with extra options.
  - Mitigation: keep options concise/contextual and only show when relevant.
- Rejections could feel arbitrary or frustrating.
  - Mitigation: deterministic reasoned gate, clear in-character rationale, and defer paths with actionable follow-up.
- Increased log noise from rejects during tuning.
  - Mitigation: concise reject codes and targeted debugging commands.
