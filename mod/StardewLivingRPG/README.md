# StardewLivingRPG (SMAPI Mod Scaffold)

Related docs: [../../DOC_INDEX.md](../../DOC_INDEX.md) · [../../IMPLEMENTATION_PLAN.md](../../IMPLEMENTATION_PLAN.md)

M0 + M1 scaffold includes:
- mod entry + manifest
- config model
- typed save state models
- save load/write helpers
- daily tick hook service
- economy service (cozy clamps + demand/supply/sentiment)
- shipping-bin ingestion at day end (real sold items)
- text market board preview command + in-game board menu shell
- daily newspaper issue generation from economy deltas + in-game newspaper menu
- rumor board v1 with daily template quests + accept/complete flow + automatic expiry/fail
- anchor event A1 trigger (Emergency Town Hall) with one-time fact lock + follow-up quest

## Debug console commands
- `slrpg_sell <crop> <count>`: queue simulated crop sales for next day tick
- `slrpg_board`: print text market board preview to SMAPI log
- `slrpg_open_board`: open Market Board menu
- `slrpg_open_news`: open latest newspaper issue
- `slrpg_open_rumors`: open Town Request Board menu (select requests, then Accept/Complete in-menu)
- `slrpg_accept_quest <questId>`: accept a listed rumor quest
- `slrpg_quest_progress <questId>`: show active quest progress (`have/need`) for item hand-ins
- `slrpg_quest_progress_all`: show progress summary for all active quests
- `slrpg_complete_quest <questId>`: complete an active town request quest (checks required items, consumes them, and pays gold reward)
- `slrpg_set_sentiment economy <value>`: set economy sentiment (testing anchor trigger)
- `slrpg_debug_state`: print compact daily diagnostics snapshot
- `slrpg_intent_inject <json>`: inject raw intent envelope for deterministic resolver QA
- `slrpg_intent_smoketest`: run a mini automated resolver smoke suite with pass/fail summary
- `slrpg_regression_targeted`: run targeted regression checks (chat routing, pass-out publication, market outlook, ambient command lane)
- `slrpg_anchor_smoketest`: run deterministic anchor trigger/resolution smoke test
- `slrpg_baseline_3day`: run deterministic 3-day baseline metrics simulation
- `slrpg_baseline_7day`: run deterministic 7-day scenario metrics simulation and compare with baseline
- `slrpg_ambient_pipeline_validate`: run staged validation (regressions + rate drift thresholds) for ambient consequence pipeline
- `slrpg_demo_bootstrap`: seed reproducible vertical-slice scenario
- `slrpg_p2_login`: local Player2 app auth using configured game client id (auto-fallback to device auth)
- `slrpg_p2_spawn`: spawn one Player2 NPC session
- `slrpg_p2_chat <message>`: send chat to active Player2 NPC
- `slrpg_p2_read_once`: read one NPC stream line from `/npcs/responses` (non-blocking background read)
- `slrpg_p2_read_reset`: cancel/reset stuck Player2 read
- `slrpg_p2_stream_start`: start persistent NPC response listener (auto-reconnect with backoff)
- `slrpg_p2_stream_stop`: stop persistent NPC response listener
- `slrpg_p2_status`: show login/NPC/stream state + joules balance
- `slrpg_p2_health`: compact one-line health summary (login/npc/stream/joules/last line/last command)

Player-facing Player2 UX:
- auto-connect on save load (config: `AutoConnectPlayer2OnLoad`, default `true`)
- HUD status badge in top-left: click `Town AI: Reconnect` to trigger login -> spawn -> stream pipeline
- stream watchdog now retries the last player-triggered "New Postings" request right after stream restarts
- if repeated stalls persist, watchdog escalates to full NPC session refresh and re-queues the request again
- in-world "New Postings" action in Town Request Board triggers resolver-safe request generation via Player2
- in NPC interaction range (roster NPCs), normal action interaction keeps vanilla dialogue; after it closes, an additive postings follow-up prompt appears
- policy: never replace original vanilla NPC dialogue; mod prompts must be additive follow-up only
- Robin, Pierre, and Lewis have custom follow-up dialogue options: "Any new requests?", "Let's just talk.", "See you later."
- selecting talk opens a persistent in-world chat input box showing recent player/NPC lines instead of closing immediately
- persistent NPC memory + town memory context are injected into chat payloads (bounded/capped for low latency)
- chat UI now shows a lightweight "Thinking..." indicator while awaiting NPC response
- request routing now rotates across configured NPC roster (`Player2NpcRosterCsv`) when available (or targets the NPC you asked directly)
- generation guardrails: cooldown, max manual checks per day (`MaxUiGeneratedRequestsPerDay`), and max outstanding requests (`MaxOutstandingRequests`)
- automatic replay/retry after stream recovery does **not** consume the player's manual daily cap

NPC intent pipeline:
- validates intent envelopes against `NPC_COMMAND_SCHEMA.json` command constraints
- implemented deterministic handlers:
  - `propose_quest`
  - `adjust_reputation`
  - `shift_interest_influence`
  - `apply_market_modifier`
  - `publish_rumor`
  - `publish_article`
  - `record_memory_fact`
  - `record_town_event`
  - `adjust_town_sentiment`
- runs deterministic resolver path with explicit reject/duplicate/applied logs
- supports automatic and manual command exposure lanes with lane-aware telemetry
- manual relationship/interest/market asks now pass through a deterministic accept/defer/reject gate before mutation-capable prompts are sent

NPC grounding polish:
- chat context now includes explicit `MARKET_SIGNALS` (top movers, oversupply, scarcity, recommended alternative crop)
- chat context now includes `CurrentSeason`, `CurrentWeather`, `CurrentDayOfWeek`, and precise `CurrentTimeOfDay`
- speech identity is data-driven via `npc_speech_profiles.json` + `NpcSpeechStyleService` (profile syntax, contraction rules, heart warm-up, rain modifiers, and stat-based honorifics)
- Lewis prompt requires market answers to reference at least one live signal
- quest wording shifted toward "town requests" and command prompt now enforces strict template enums
- reward dialogue is constrained to configured payout bands to avoid mismatch with deterministic rewards

Config knobs:
- `Player2DeviceAuthBaseUrl` (default `https://api.player2.game/v1`)
- `Player2DeviceAuthTimeoutSeconds` (default `120`)
- `Player2BlockChatWhenLowJoules` (default `true`)
- `Player2MinJoulesToChat` (default `5`)
- `StrictNpcTemplateValidation` (default `false`; when `true`, reject `quest_*` template IDs instead of repairing)
- `EnableAmbientConsequencePipeline` (default `true`; toggles ambient event-to-consequence converters)

## In-game
- Press `K` (default) to open the Market Board menu (configurable via `config.json`).
- Press `J` (default) to open the latest Newspaper issue (configurable via `config.json`).
- Press `L` (default) to open the Rumor Board menu (configurable via `config.json`).

## Player2 setup (M2)
- In `config.json`, set:
  - `EnablePlayer2: true`
- Ensure Player2 desktop app is running and logged in.
- `Player2GameClientId` is built into the mod code (`CreatorPlayer2GameClientId` in `ModEntry.cs`) and is not user-configurable.
- Run `slrpg_p2_login`, then `slrpg_p2_spawn`.
- Recommended runtime loop:
  1) `slrpg_p2_stream_start`
  2) `slrpg_p2_chat hello mayor`
  3) watch incoming lines in SMAPI log
  4) `slrpg_p2_stream_stop` when done

## Build notes
Set `SMAPI_PATH` to your game install path containing:
- StardewModdingAPI.dll
- Stardew Valley.dll

Then build with `dotnet build`.

## Localization (i18n)
- Translation files live in `i18n/`.
- Base English keys are in `i18n/default.json`.
- Community translators can copy `default.json` to a locale file (for example `fr.json`) and translate values only.
