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
- `slrpg_open_rumors`: open Rumor Board menu
- `slrpg_accept_quest <questId>`: accept a listed rumor quest
- `slrpg_complete_quest <questId>`: complete an active rumor quest
- `slrpg_set_sentiment economy <value>`: set economy sentiment (testing anchor trigger)
- `slrpg_debug_state`: print compact daily diagnostics snapshot
- `slrpg_demo_bootstrap`: seed reproducible vertical-slice scenario
- `slrpg_p2_login`: local Player2 app auth using configured game client id
- `slrpg_p2_spawn`: spawn one Player2 NPC session
- `slrpg_p2_chat <message>`: send chat to active Player2 NPC
- `slrpg_p2_read_once`: read one NPC stream line from `/npcs/responses` (non-blocking background read)
- `slrpg_p2_read_reset`: cancel/reset stuck Player2 read
- `slrpg_p2_stream_start`: start persistent NPC response listener (auto-reconnect with backoff)
- `slrpg_p2_stream_stop`: stop persistent NPC response listener
- `slrpg_p2_status`: show login/NPC/stream state + joules balance

## In-game
- Press `K` (default) to open the Market Board menu (configurable via `config.json`).
- Press `J` (default) to open the latest Newspaper issue (configurable via `config.json`).
- Press `L` (default) to open the Rumor Board menu (configurable via `config.json`).

## Player2 setup (M2)
- In `config.json`, set:
  - `EnablePlayer2: true`
  - `Player2GameClientId: <your_game_client_id>`
- Ensure Player2 desktop app is running and logged in.
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
