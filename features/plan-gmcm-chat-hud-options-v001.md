# GMCM Chat and HUD Options Plan v001

## Overview
Add optional Generic Mod Config Menu (GMCM) support so players can adjust core config without editing `config.json`:
- Disable player-chat (NPC chat menu entry points).
- Hide the Player2 connection HUD badge.
- Select gameplay mode (`cozy_canon`, `story_depth`, `living_chaos`).
- Tune economy bounds (`PriceFloorPct`, `PriceCeilingPct`, `DailyPriceDeltaCapPct`).
- Rebind UI hotkeys (`OpenBoardKey`, `OpenNewspaperKey`, `OpenRumorBoardKey`).

The integration must be optional: if GMCM is not installed, the mod continues to run and uses `config.json` values only.

## Goals
- Add an optional GMCM config page for this mod that exposes the requested booleans, mode/economy values, and hotkeys.
- Persist both options through existing SMAPI config read/write flow.
- Keep defaults aligned with current behavior so existing players see no change after update.
- When player-chat is disabled, prevent opening `NpcChatInputMenu` from NPC interaction paths and remove chat cursor affordance.
- When Player2 HUD is hidden, do not draw the HUD badge and do not allow click-to-connect through that hidden badge.
- Constrain `Mode` to valid values and keep economy-number inputs bounded to safe ranges.
- Support rebinding the three in-world menu keys from GMCM.
- Preserve additive-dialogue behavior: vanilla dialogue remains untouched.

## Non-Goals
- Disabling Player2 systems globally.
- Removing console commands or changing command behavior.
- Redesigning NPC chat UI or HUD visuals.
- Adding extra chat/HUD customization beyond the two requested toggles.
- Changing economy formulas or mode semantics themselves.
- Refactoring unrelated dialogue-hook code paths.

## Current State
- `ModConfig` already defines `Mode`, `PriceFloorPct`, `PriceCeilingPct`, `DailyPriceDeltaCapPct`, and the three menu hotkeys (`OpenBoardKey`, `OpenNewspaperKey`, `OpenRumorBoardKey`), but they are only editable via `config.json`.
- `ModConfig` does not yet include GMCM-only toggles for player-chat enablement or HUD visibility.
- `ModEntry.Entry` subscribes to `RenderedHud`, `ButtonPressed`, and other events, but no `GameLaunched` handler exists for optional mod API registration.
- `OnRenderedHud` always draws the Player2 status badge when world is ready and no menu/event blocks rendering.
- `OnButtonPressed` always allows left-click interaction with the HUD rectangle to trigger `StartPlayer2AutoConnect(...)` when disconnected.
- Player-chat entry points call `OpenNpcChatMenu(...)` from manual follow-up and no-vanilla fallback paths.
- `TryApplyNpcChatCursorIndicator` always sets talk cursor for eligible roster NPCs in range, signaling chat availability.
- No GMCM API shim interface currently exists in the codebase.

## Proposed Architecture
1. Add GMCM-exposed config set:
- `EnablePlayerChatMenu = true`
- `ShowPlayer2ConnectionHud = true`
- Existing config fields to expose in GMCM:
  - `Mode`
  - `PriceFloorPct`
  - `PriceCeilingPct`
  - `DailyPriceDeltaCapPct`
  - `OpenBoardKey`
  - `OpenNewspaperKey`
  - `OpenRumorBoardKey`

2. Register GMCM options at game launch, only if API is available:
- Subscribe to `GameLoop.GameLaunched`.
- Resolve API via `Helper.ModRegistry.GetApi<...>("spacechase0.GenericModConfigMenu")`.
- If API is missing, no-op with optional trace/debug log.
- If present, register mod page with reset/save callbacks tied to `_config`.
- Group options clearly (for example: `Chat & HUD`, `Economy`, `Hotkeys`) to keep menu legible.
- Map fields to appropriate GMCM controls:
  - bool options for chat/HUD toggles.
  - constrained text/choice option for `Mode` (three allowed values only).
  - number options for `PriceFloorPct`, `PriceCeilingPct`, `DailyPriceDeltaCapPct`.
  - keybind options for `OpenBoardKey`, `OpenNewspaperKey`, `OpenRumorBoardKey`.
- Add tooltip text for every option so effects are clear before players change values.
- Apply basic guardrails on save (clamp numeric bounds and enforce `PriceFloorPct <= PriceCeilingPct`).

3. Gate chat entry behavior behind a single config check:
- Prevent chat-menu opening from manual follow-up/no-vanilla interaction paths when disabled.
- Prevent chat cursor indicator from showing when disabled.
- Add a safety guard in `OpenNpcChatMenu(...)` so future callers cannot bypass the setting unintentionally.

4. Gate HUD visibility and click behavior behind one config check:
- Skip `OnRenderedHud` drawing when hidden.
- Ignore HUD rectangle click-to-connect when hidden.
- Leave background Player2 auto-connect logic unchanged (HUD toggle is visual/manual-click UX only).

5. Expose optional compatibility in manifest metadata:
- Add optional dependency declaration for `spacechase0.GenericModConfigMenu`.

## Changes Needed
- `mod/StardewLivingRPG/Config/ModConfig.cs`
- `mod/StardewLivingRPG/ModEntry.cs`
- `mod/StardewLivingRPG/manifest.json`
- New API shim file (for example): `mod/StardewLivingRPG/Integrations/IGenericModConfigMenuApi.cs`
- `mod/StardewLivingRPG/README.md` (document new config/GMCM toggles)

## Tasks (numbered)
1. Add config surface.
   Add the two booleans in `ModConfig` with defaults `true` so current behavior is preserved unless a player changes settings; keep existing mode/economy/hotkey fields unchanged.
2. Add optional GMCM API shim.
   Create a small interface for needed GMCM methods and wire a `GameLaunched` handler in `ModEntry`.
3. Implement GMCM registration.
   Register page, set reset/save callbacks, and add all requested options:
   - `EnablePlayerChatMenu`
   - `ShowPlayer2ConnectionHud`
   - `Mode` (allowed: `cozy_canon`, `story_depth`, `living_chaos`)
   - `PriceFloorPct`
   - `PriceCeilingPct`
   - `DailyPriceDeltaCapPct`
   - `OpenBoardKey`
   - `OpenNewspaperKey`
   - `OpenRumorBoardKey`
   Include per-option tooltip text plus range constraints and floor/ceiling consistency handling.
4. Implement player-chat disable path.
   Add centralized check and apply it to `TryHandleNpcWorkDialogueHook`, `TryHandleNpcDialogueHookFallback`, `TryApplyNpcChatCursorIndicator`, and `OpenNpcChatMenu` safety guard so NPC chat menu never opens while disabled.
5. Implement Player2 HUD hide path.
   Add centralized check and apply it to `OnRenderedHud` and HUD click handling in `OnButtonPressed`.
6. Add optional manifest dependency.
   Update `manifest.json` with non-required dependency metadata for GMCM.
7. Update docs.
   Add the settings and their behavior notes to README config/player UX sections.
8. Validate with build and behavior checks.
   Run `dotnet build`, then verify:
   - GMCM absent: no errors, config still loads.
   - GMCM present: all requested options appear and persist.
   - Player-chat disabled: no NPC chat menu opens; vanilla interaction still works.
   - HUD hidden: no HUD badge rendered and no HUD click target.
   - Mode option only allows supported values and persists selected mode.
   - Economy options persist and enforce guardrails (including `floor <= ceiling`).
   - Hotkey options rebind successfully for board/news/rumor menus.
   - Tooltips appear for each GMCM option and describe the effect accurately.
   - Both enabled: behavior matches current baseline.

## Tooltip Matrix
- `EnablePlayerChatMenu`: "Enable in-world NPC chat menu follow-ups. Disable to keep NPC interactions vanilla-only."
- `ShowPlayer2ConnectionHud`: "Show the top-left Player2 status badge and click-to-connect button."
- `Mode`: "Select world simulation intensity: cozy_canon (gentle), story_depth (stronger consequences), living_chaos (high volatility)."
- `PriceFloorPct`: "Minimum crop price as a fraction of base price. Example: 0.80 means 80% floor."
- `PriceCeilingPct`: "Maximum crop price as a fraction of base price. Example: 1.40 means 140% ceiling."
- `DailyPriceDeltaCapPct`: "Maximum single-day price movement as a fraction of current price. Example: 0.10 means +/-10% per day."
- `OpenBoardKey`: "Keybind to open the Market Board."
- `OpenNewspaperKey`: "Keybind to open the daily Newspaper."
- `OpenRumorBoardKey`: "Keybind to open the Rumor Board."

## Dependencies
- SMAPI mod API resolution (`Helper.ModRegistry.GetApi<T>`).
- Optional external mod: `spacechase0.GenericModConfigMenu`.
- Existing config persistence lifecycle (`helper.ReadConfig` / `helper.WriteConfig`).
- Existing NPC chat and HUD event flow in `ModEntry`.

## Risks
- Runtime toggle edge cases may leave stale follow-up state if player-chat is disabled mid-session.
- Over-broad chat gating could accidentally suppress unrelated interaction logic if checks are not placed surgically.
- Hidden HUD can reduce discoverability of manual reconnect flow for players who disable auto-connect.
- Numeric config validation mistakes (especially floor/ceiling relationship) could create unintended market behavior.
- GMCM keybind choices can conflict with other mods or vanilla bindings and cause player confusion.
- GMCM API signature drift could break registration if shim method signatures do not match installed GMCM version.
