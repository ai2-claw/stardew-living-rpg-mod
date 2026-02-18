# Stardew Living RPG Architecture (Product + Implementation)

Related docs: [DOC_INDEX](./DOC_INDEX.md) | [DATA_MODEL](./DATA_MODEL.md) | [EVENT_RESOLUTION](./EVENT_RESOLUTION.md) | [IN_WORLD_UI_ARCHITECTURE](./IN_WORLD_UI_ARCHITECTURE.md)

This document keeps two lenses in one place:
- Product direction: the intended player experience and design guardrails.
- Current implementation: what the code in `mod/StardewLivingRPG/...` does today.

If these diverge:
1. For debugging and QA, trust the current implementation sections.
2. For planning and scope decisions, trust the product direction sections.

## 1) Product Direction: "Stardew, but alive"

The mod should feel like Stardew Valley with living consequences, not a different genre on top.

Design rule:
- Systems should create meaningful but gentle consequences.
- Default UX should stay warm, legible, and low-friction.

## 2) Audience Segments and Design Fit

| Segment | What they want | Primary systems |
|---|---|---|
| Cozy Socials | Character continuity and town flavor | NPC memory, town memory, newspaper stories |
| Planner Farmers | Better planning signals | Dynamic crop pricing, market board, outlook hints |
| Story Seekers | Emergent, persistent outcomes | Quest branching, anchor events, rumor propagation |
| Chaos/Hardcore | Higher volatility and stronger shifts | Optional higher-intensity mode settings |

### Town Interests Model (High Level)

Community groups are modeled as soft interests, not hard faction warfare:
- Farmers' Circle
- Shopkeepers' Guild
- Adventurers' Club
- Nature Keepers

Design rule:
- Interests should usually be bridgeable, with opportunities for multi-group wins.

## 3) Experience Modes

Configured in `config.json` via `Mode`.

1. `cozy_canon` (default)
- Soft economy movement and forgiving floors
- Reversible social consequences
- Community-forward tone

2. `story_depth`
- Heavier social and quest consequences
- Stronger branch persistence

3. `living_chaos`
- Highest volatility and world shifts
- For opt-in play, not baseline

## 4) Core Pillars and Current Status

| Pillar | High-level goal | Current implementation status |
|---|---|---|
| Living NPCs | NPCs remember and react over time | Implemented via `NpcMemoryService`, `TownMemoryService`, Player2 chat stream + intent resolver |
| Dynamic Economy | Prices react to supply/demand/events while staying cozy-safe | Implemented in `EconomyService` with caps/floors, smoothing, scarcity behavior, daily transitions |
| Diegetic Feedback | Player can read simulation outcomes in-world | Implemented via `NewspaperMenu`, `MarketBoardMenu`, `RumorBoardMenu`, `RequestJournalMenu` |
| Hard Assets | Game-like anchors beyond freeform AI text | Implemented in custom board/journal/news UIs and anchor event service; deterministic templates remain core safety net |

## 5) World Impact Loop

1. Player acts (dialogue, gifts, shipping, quest decisions).
2. NPCs and systems propose outcomes (Player2 or deterministic systems).
3. `NpcIntentResolver` validates and bounds all NPC-origin world mutations.
4. Persistent state mutates (`SaveState` and substates).
5. Results surface through NPC behavior, newspaper, market board, and request board.

Ship gate:
- If player-visible world state does not change, the feature is incomplete.

## 6) Current Runtime Topology

`mod/StardewLivingRPG/ModEntry.cs` is the composition root and event router.

At entry/load, `ModEntry` wires and owns:
- `DailyTickService`
- `EconomyService`
- `MarketBoardService`
- `SalesIngestionService`
- `NewspaperService`
- `RumorBoardService`
- `NpcIntentResolver`
- `AnchorEventService`
- `NpcMemoryService`
- `TownMemoryService`
- `Player2Client`

`ModEntry` also owns:
- SMAPI lifecycle hooks (`SaveLoaded`, `DayStarted`, `DayEnding`, `UpdateTicked`, etc.)
- Player2 auth/spawn/chat/stream orchestration
- async newspaper build queue and main-thread apply
- HUD notifications for NPC publish actions
- debug and operational console commands

## 7) Current Event Lifecycle

### Save load (`OnSaveLoaded`)
1. Load persisted state.
2. Initialize economy defaults/config.
3. Optionally auto-connect Player2 (`EnablePlayer2` + `AutoConnectPlayer2OnLoad`).

### Day ending (`OnDayEnding`)
1. Scan shipping bin.
2. Normalize crop keys.
3. Queue sales deltas into `SalesIngestionService`.

### Day start (`OnDayStarted`)
1. Optionally retry Player2 auto-connect.
2. Reset per-day counters (including ambient conversation scheduler).
3. Apply queued sales and run daily economy update.
4. Run daily tick scaffold (`DailyTickService`).
5. Expire/refresh deterministic rumor board quests.
6. Trigger and resolve anchor events.
7. Build newspaper:
- Player2 disabled: synchronous build.
- Player2 enabled: defer until roster/stream ready, then async build.

### Update tick (`OnUpdateTicked`)
1. Capture town incidents into memory.
2. Retry Player2 connection/stream as needed.
3. Run ambient NPC-to-NPC conversation trigger (randomized).
4. Apply completed async newspaper issues.
5. Retry pending newspaper build when readiness gates pass.
6. Replay queued UI asks once target NPC/session are ready.
7. Consume incoming Player2 lines:
- show UI-visible text
- parse intents
- resolve via `NpcIntentResolver`

## 8) Player2 Integration (Current)

Authentication:
- Local desktop fast path first.
- Device flow fallback.
- `NewspaperService` is recreated after auth to ensure authenticated story generation.

NPC session flow:
1. Spawn NPC sessions (`/npcs/spawn`) for primary and roster NPCs.
2. Spawn payload command schema includes:
- `propose_quest`
- `publish_article`
- `publish_rumor`
3. Send chat via `/npcs/{npc_id}/chat`.
4. Read responses from long-lived stream (`/npcs/responses`).
5. Watchdog reconnects/rebuilds sessions if stream stalls.

## 9) Intent Contract and Safety

Resolved centrally by `NpcIntentResolver`.

Supported commands:
- `propose_quest`
- `adjust_reputation`
- `shift_interest_influence`
- `apply_market_modifier`
- `publish_rumor`
- `publish_article`
- `record_memory_fact`
- `record_town_event`
- `adjust_town_sentiment`

Safety controls:
- strict argument validation and normalization
- bounded deltas/ranges
- idempotency tracking through processed intent facts
- fact-key cooldown gates for memory/event/sentiment command families
- lane-aware telemetry (`auto` vs `manual`) and ask-gate outcomes (`accept|defer|reject`)
- daily publish caps:
- rumors: max 1 social rumor/day
- non-social NPC articles: max 2/day

## 10) Economy + Market Board (Current)

Economy behavior is implemented in `EconomyService`:
- base prices and seasonal demand multipliers
- supply pressure from rolling sell volume
- scarcity bonus opportunities
- smoothing/caps/floors to preserve cozy readability

Player-facing market surfacing:
- `MarketBoardMenu` shows daily prices, trend arrows, and mini history visualization.
- `MarketBoardService` is currently thin; UI reads state directly for most rendering.

## 11) Newspaper + Rumor Publishing (Current)

`NewspaperService` issue build order:
1. Event-driven stories from town memory.
2. Player2 editor stories when authenticated and slots are open.
3. Deterministic seasonal filler for remaining slots.
4. NPC-published items from current-day state.
5. Headline selection/sensationalization.
6. Market section and outlook hints.

When `publish_article` or `publish_rumor` resolves in `ModEntry`:
1. HUD toast notification is shown.
2. If today's issue is present, existing visible article slot content is replaced (not appended) and headline is updated.
3. If no issue exists yet, a pending build is scheduled.

This replacement behavior is intentional to preserve newspaper layout constraints.

## 12) Ambient NPC Conversations (Current)

Ambient publishing is not tied to a fixed daily clock.

Implemented behavior:
- randomized first trigger window
- randomized interval between attempts
- capped attempts per day
- requires Player2 readiness (auth + roster + stream + active session)
- skips while a user-initiated request is actively in flight

Implementation entry points in `ModEntry.cs`:
- `ResetAmbientNpcConversationScheduleForDay`
- `TryTriggerAmbientNpcConversation`
- `TryPickAmbientNpcConversationPair`

## 13) Code Map: Files and Ownership

### `mod/StardewLivingRPG/ModEntry.cs`

| File | Responsibility |
|---|---|
| `mod/StardewLivingRPG/ModEntry.cs` | Composition root, SMAPI event wiring, Player2 lifecycle, UI openings, command handlers, stream/watchdog, ambient conversation scheduling, intent application, HUD notifications, async newspaper orchestration |

### `mod/StardewLivingRPG/Systems`

| File | Responsibility |
|---|---|
| `mod/StardewLivingRPG/Systems/DailyTickService.cs` | Daily simulation scaffold and day advancement helpers |
| `mod/StardewLivingRPG/Systems/EconomyService.cs` | Price model, demand/supply pressure, smoothing, floors/caps, trend/state updates |
| `mod/StardewLivingRPG/Systems/MarketBoardService.cs` | Lightweight board service surface used by market UI flow |
| `mod/StardewLivingRPG/Systems/NewspaperService.cs` | Newspaper pipeline, editor story generation, fillers, headline and market sections |
| `mod/StardewLivingRPG/Systems/NpcIntentResolver.cs` | Validates and resolves NPC commands into deterministic state mutations |
| `mod/StardewLivingRPG/Systems/NpcMemoryService.cs` | Per-NPC memory storage, retrieval, scoring, and prompt block shaping |
| `mod/StardewLivingRPG/Systems/QuestProposalResult.cs` | Result contract for normalized quest proposal outcomes |
| `mod/StardewLivingRPG/Systems/RumorBoardService.cs` | Daily request board refresh, accept/progress/complete flows, reward application |
| `mod/StardewLivingRPG/Systems/SalesIngestionService.cs` | Shipping-bin sales queue and day-transition ingestion |
| `mod/StardewLivingRPG/Systems/TownMemoryService.cs` | Shared town events, propagation/awareness model, prompt block generation |
| `mod/StardewLivingRPG/Systems/AnchorEventService.cs` | Scripted anchor milestone triggers and resolution side effects |

### `mod/StardewLivingRPG/UI`

| File | Responsibility |
|---|---|
| `mod/StardewLivingRPG/UI/MarketBoardMenu.cs` | In-world market board UI with cards, trend glyphs, and short history charting |
| `mod/StardewLivingRPG/UI/NewspaperMenu.cs` | Newspaper rendering (masthead, sections, two-column story layout, optional portraits) |
| `mod/StardewLivingRPG/UI/NpcChatInputMenu.cs` | Persistent NPC chat input and response polling surface |
| `mod/StardewLivingRPG/UI/RequestJournalMenu.cs` | Active/completed request tracking and completion actions |
| `mod/StardewLivingRPG/UI/RumorBoardMenu.cs` | Board postings UI, accept/complete actions, and "ask for work" interaction |

## 14) Console Surface (Current)

Key command groups:
- economy and board (`slrpg_sell`, `slrpg_open_board`)
- newspaper/rumors/journal (`slrpg_open_news`, `slrpg_open_rumors`, `slrpg_open_journal`)
- quest operations (`slrpg_accept_quest`, `slrpg_complete_quest`, progress commands)
- state/diagnostics (`slrpg_debug_state`, intent inject/smoketests, memory dumps)
- Player2 control and health (`slrpg_p2_login`, `slrpg_p2_spawn`, `slrpg_p2_chat`, stream commands, `slrpg_p2_health`)
- news publish debug (`slrpg_debug_news_toast`)

## 15) Known Constraints and Alignment Notes

- `DailyTickService` remains intentionally lightweight scaffold.
- `MarketBoardService` is intentionally thin; most rendering decisions live in UI classes.
- Rumor board deterministic templates remain the guaranteed baseline; AI proposals are additive.
- Newspaper replacement on NPC publish is deliberate so 2-story layout stays stable.
- Architecture intent remains "high-level cozy simulation with visible consequences"; implementation should keep converging without breaking determinism or in-world UX.
