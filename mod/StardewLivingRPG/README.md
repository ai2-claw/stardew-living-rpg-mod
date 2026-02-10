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
- rumor board v1 with daily template quests + accept flow
- anchor event A1 trigger (Emergency Town Hall) with one-time fact lock + follow-up quest

## Debug console commands
- `slrpg_sell <crop> <count>`: queue simulated crop sales for next day tick
- `slrpg_board`: print text market board preview to SMAPI log
- `slrpg_open_board`: open Market Board menu
- `slrpg_open_news`: open latest newspaper issue
- `slrpg_open_rumors`: open Rumor Board menu
- `slrpg_accept_quest <questId>`: accept a listed rumor quest
- `slrpg_set_sentiment economy <value>`: set economy sentiment (testing anchor trigger)

## In-game
- Press `K` (default) to open the Market Board menu (configurable via `config.json`).
- Press `J` (default) to open the latest Newspaper issue (configurable via `config.json`).
- Press `L` (default) to open the Rumor Board menu (configurable via `config.json`).

## Build notes
Set `SMAPI_PATH` to your game install path containing:
- StardewModdingAPI.dll
- Stardew Valley.dll

Then build with `dotnet build`.
