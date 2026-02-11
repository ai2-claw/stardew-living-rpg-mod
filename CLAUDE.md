# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Stardew Living RPG is a SMAPI (Stardew Modding API) mod that adds a living RPG layer to Stardew Valley. The mod integrates with Player2 (an AI service) to create dynamic NPCs, economy systems, and quests while maintaining the cozy feel of vanilla Stardew Valley.

**Core Design Philosophy**: "Stardew, but alive" — not a different genre pasted on top. All systems should create meaningful but gentle consequences with warm, legible, low-friction UX.

## Rules

### Think Before Coding
- State assumptions explicitly; if uncertain, use the `AskUserQuestion` tool rather than guess
- When ambiguity exists, present multiple interpretations via `AskUserQuestion` — don't pick silently
- Push back if a simpler approach exists; stop and ask via `AskUserQuestion` when confused

### Simplicity First
- No features, abstractions, or error handling beyond what was asked
- No speculative "flexibility" or "configurability"
- If 200 lines could be 50, rewrite it
- Only create an abstraction if it's actually needed

### Surgical Changes
- Touch only what you must; don't "improve" adjacent code, comments, or formatting
- Match existing style, even if you'd do it differently
- If you notice unrelated dead code, mention it — don't delete it
- Remove imports/variables/functions that YOUR changes made unused, not pre-existing dead code

### Goal-Driven Execution
- Define verifiable success criteria before implementing
- Write or run tests first to confirm the change works
- Every action should trace back to the user's stated goal

### General
- ALWAYS read and understand relevant files before proposing edits
- If critical info is needed and you suspect your knowledge may be outdated, fetch the latest docs via Context7 MCP first
- Before writing new code, check for existing related methods/classes and reuse them
- Prefer clear function/variable names over inline comments
- Don't use emojis

## Bash Guidelines

- Do NOT pipe output through `head`, `tail`, `less`, or `more`
- Do NOT use `| head -n X` or `| tail -n X` to truncate output — these cause buffering problems
- Let commands complete fully, or use `--max-lines` flags if the command supports them
- For log monitoring, prefer reading files directly rather than piping through filters
- Run commands directly without pipes when possible
- Use command-specific flags to limit output (e.g., `git log -n 10` instead of `git log | head -10`)
- Avoid chained pipes that can cause output to buffer indefinitely

## When to Read Documentation

| Task | Read |
|------|------|
| Product direction, pillars, scope | `ARCHITECTURE.md` |
| State schemas, resolver pipeline, determinism | `DATA_MODEL.md`, `EVENT_RESOLUTION.md` |
| UX principles, interaction surfaces, diegetic design | `IN_WORLD_UI_ARCHITECTURE.md` |
| Recent changes history | `CHANGELOG.md` |
| Full doc index and reading order | `DOC_INDEX.md` |

## Build and Development

### Prerequisites
- .NET 6.0 SDK
- SMAPI installed with game path set as `SMAPI_PATH` environment variable
- `SMAPI_PATH` must point to directory containing: `StardewModdingAPI.dll`, `Stardew Valley.dll`, `xTile.dll`

### Build Commands
```bash
# Build the mod (copies to Mods folder automatically)
dotnet build

# The post-build step copies output to:
# $(SMAPI_PATH)\Mods\StardewLivingRPG
```

### Testing in Game
1. Build the project
2. Launch Stardew Valley via SMAPI
3. Press console key (`~` by default) to access SMAPI console for debug commands

## Architecture

### Service-Based Architecture

The mod follows a clean service-oriented pattern. `ModEntry.cs` is the central hub that:
- Manages all service lifetimes
- Handles SMAPI event subscriptions (DayStarted, DayEnding, SaveLoaded, Saving, etc.)
- Implements 40+ console commands for debugging
- Manages Player2 integration (AI NPC conversations)

### Core Services (`Systems/`)

| Service | Responsibility |
|----------|---------------|
| `DailyTickService` | Orchestrates daily simulation transitions |
| `EconomyService` | Dynamic crop pricing based on supply/demand/sentiment |
| `NpcIntentResolver` | Validates and applies AI-proposed world changes via command schema |
| `MarketBoardService` | UI for market information (K key) |
| `NewspaperService` | Daily newspaper generation from world state |
| `RumorBoardService` | Quest board system (L key) |
| `AnchorEventService` | Major scripted milestone events |
| `NpcMemoryService` | Persistent NPC memory across sessions |
| `TownMemoryService` | Shared town events and NPC awareness |
| `SalesIngestionService` | Tracks shipping bin sales at day end |

### Player2 Integration (`Integrations/Player2Client.cs`)

The mod connects to Player2 API for dynamic NPC conversations:
- Base URL: `https://api.player2.game/v1` (configurable)
- Local auth fast-path: `http://localhost:4315/v1` (Player2 desktop app)
- NPC commands flow through `NpcIntentResolver` for validation
- Implements exponential backoff for stream reconnection
- Maintains NPC sessions with grounding prompts

### NPC Command Schema (`NPC_COMMAND_SCHEMA.json`)

AI can only propose 5 safe, deterministic commands:
- `propose_quest` - Generate quests from validated templates
- `adjust_reputation` - Modify NPC relationships (-10 to +10)
- `shift_interest_influence` - Town group influence (-5 to +5)
- `apply_market_modifier` - Temporary price changes (-15% to +15%)
- `publish_rumor` - Spread town information

All commands are validated against JSON schema with bounded deltas and cooldown gates.

### State Management (`State/`)

- `SaveState` - Top-level state with version field for migrations
- `EconomyState` - Crop prices, demand factors, rolling 7-day sell volumes
- `SocialState` - NPC reputation, town interests/influence
- `QuestState` - Active/completed/failed quests with template-based rewards
- `FactTable` - Idempotency keys and fact locks to prevent duplicate AI intents
- Deterministic serialization; versioned for save compatibility

### UI Components (`UI/`)

| Component | Hotkey | Purpose |
|-----------|----------|---------|
| `MarketBoardMenu` | K | Shows crop prices, trends, demand outlook |
| `NewspaperMenu` | J | Daily town news and events |
| `RumorBoardMenu` | L | Quest board with AI-generated requests |
| `RequestJournalMenu` | O | Track active/completed quests |
| `NpcChatInputMenu` | - | Persistent NPC conversation interface |

### Experience Modes

Configured via `config.json` → `Mode`:
- `cozy_canon` (default) - Gentle changes, safe economy floors
- `story_depth` - Stronger consequences, heavier reputation effects
- `living_chaos` - High volatility economy, large world-state shifts

## Critical Policies

### Additive Dialogue Policy
**NEVER replace original vanilla NPC dialogue.** Mod dialogue must be additive follow-up only.

The policy is enforced by `scripts/check-dialogue-policy.mjs` which validates that key phrases exist in documentation. When adding NPC interaction code:
- Always show vanilla dialogue first via SMAPI
- After vanilla dialogue closes, optionally show a custom follow-up prompt
- Never intercept or replace standard NPC对话

The guardrail is codified in `ModEntry.cs:~200` where `_npcDialogueHookArmed` tracks whether to show follow-up options.

### Deterministic Safety
- All world changes must go through `NpcIntentResolver` or service layers
- AI outputs are first-class intents, never direct state writes
- Use bounded deltas: reputation ±10, influence ±5, market ±15%
- Fact locks prevent duplicate quest acceptance/resolution
- Cozy mode enforces daily caps and generous price floors (80% of base)

### Diegetic UX Principle
From `IN_WORLD_UI_ARCHITECTURE.md`:
- Player should complete core loop without opening SMAPI console
- Actions happen through world objects, dialogue menus, and journals
- Console commands are developer tools only
- Use natural board-era labels ("New Postings"), avoid modern/digital phrasing

## Debug Console Commands (Key Commands)

```
slrpg_debug_state              Compact daily diagnostics snapshot
slrpg_p2_health               Player2 connection one-line health summary
slrpg_intent_smoketest         Run automated resolver QA with pass/fail
slrpg_demo_bootstrap           Seed reproducible vertical-slice scenario
slrpg_open_board              Open Market Board menu
slrpg_open_news               Open latest newspaper
slrpg_open_rumors             Open Town Request Board
slrpg_open_journal            Open Request Journal
```

Full command list in `ModEntry.cs:94-122` (40+ commands).

## Documentation

- `DOC_INDEX.md` - Reading order guide for all docs
- `ARCHITECTURE.md` - Product direction, pillars, scope
- `DATA_MODEL.md` - Authoritative save state schemas
- `EVENT_RESOLUTION.md` - Deterministic resolver pipeline
- `IN_WORLD_UI_ARCHITECTURE.md` - UX principles and interaction surfaces

## Config Options (via `config.json`)

Key settings in `Config/ModConfig.cs`:
- `EnablePlayer2` - Enable Player2 AI integration
- `Player2GameClientId` - Your Player2 game client ID
- `AutoConnectPlayer2OnLoad` - Auto-connect on save load
- `Player2NpcRosterCsv` - NPC roster for work requests
- `MaxUiGeneratedRequestsPerDay` - Daily cap for AI-generated requests
- `StrictNpcTemplateValidation` - Reject unknown quest templates (default: repair)

## Save File Location

SMAPI save data stored at:
```
<Stardew Save Path>/StardewLivingRPG/<SaveName>/state.json
```

State is persisted via `StateStore.cs` using SMAPI's `Helper.Data`.
