# StardewLivingRPG (SMAPI Mod Scaffold)

M0 + M1 scaffold includes:
- mod entry + manifest
- config model
- typed save state models
- save load/write helpers
- daily tick hook service
- economy service (cozy clamps + demand/supply/sentiment)
- text market board preview command
- simulated sales ingestion command for vertical-slice testing

## Debug console commands
- `slrpg_sell <crop> <count>`: queue simulated crop sales for next day tick
- `slrpg_board`: print text market board preview to SMAPI log

## Build notes
Set `SMAPI_PATH` to your game install path containing:
- StardewModdingAPI.dll
- Stardew Valley.dll

Then build with `dotnet build`.
