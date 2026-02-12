# Repository Guidelines

## Project Structure & Module Organization
This repository combines design docs with a working SMAPI mod scaffold.

- `mod/StardewLivingRPG/`: main C# mod project (`net6.0`).
- `mod/StardewLivingRPG/Systems/`: gameplay systems (economy, quests, memory, resolver).
- `mod/StardewLivingRPG/UI/`: in-game menus and HUD interactions.
- `mod/StardewLivingRPG/State/`: save-state models and persistence helpers.
- `mod/StardewLivingRPG/Config/` and `Integrations/`: config schema and Player2 integration.
- Root `*.md` files: architecture, implementation plans, release and vertical-slice docs.
- `scripts/`: repo checks (currently dialogue policy guardrail script).

## Build, Test, and Development Commands
- `dotnet build stardew-living-rpg-mod.sln`: build the full solution.
- `dotnet build mod/StardewLivingRPG/StardewLivingRPG.csproj`: build only the mod project.
- `node scripts/check-dialogue-policy.mjs`: verify required additive-dialogue policy text in docs.

Prerequisite: set `SMAPI_PATH` to your game install folder containing `StardewModdingAPI.dll` and `Stardew Valley.dll`. The project copies build outputs to `$(SMAPI_PATH)\Mods\StardewLivingRPG` after build.

## Coding Style & Naming Conventions
- Use 4-space indentation and standard C# brace style.
- Keep nullable reference types enabled and avoid suppressing warnings without reason.
- Naming pattern: `PascalCase` for types/methods/properties, `_camelCase` for private fields.
- Keep systems deterministic where possible; put domain logic in `Systems/`, not UI classes.

## Testing Guidelines
There is no dedicated test project yet. Validate changes with:

- targeted smoke commands in SMAPI (e.g., `slrpg_intent_smoketest`, `slrpg_anchor_smoketest`),
- manual in-game regression for affected menus/flows,
- `node scripts/check-dialogue-policy.mjs` before merge.

When adding testable logic, prefer small pure methods in `Systems/` to enable future unit tests.

## Commit & Pull Request Guidelines
- Follow Conventional Commit style used in history: `feat(scope): ...`, `fix(scope): ...`, `docs(scope): ...`.
- Keep commits focused by feature area (example scopes: `newspaper`, `chat-ui`, `memory`).
- PRs should include: concise summary, linked issue/task, validation steps run, and screenshots/GIFs for UI changes.
- Update relevant docs (`README.md`, `DOC_INDEX.md`, `CHANGELOG.md`) when behavior or commands change.
