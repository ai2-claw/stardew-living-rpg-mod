# Stardew Living RPG

Stardew Living RPG adds a "living simulation" layer to Stardew Valley while keeping the base game cozy and readable. NPCs react to changing town conditions, economy signals shift over time, and requests/news evolve daily through in-world menus.

## Unique Selling Proposition (USP)

- **Stardew, but alive**: dynamic systems with visible, in-world consequences.
- **Additive, not replacive**: vanilla dialogue stays intact; mod dialogue appears as follow-up.
- **AI with guardrails**: Player2-driven NPC behavior goes through deterministic validation and bounded mutations, so world changes stay stable and lore-friendly.

## Features

### 1) Living economy and market gameplay

- Dynamic pricing from demand, supply pressure, scarcity, and sentiment.
- Sales ingestion from your shipping bin affects next-day market movement.
- Market Board menu with trend indicators, spark bars, and item icons.
- Expanded market catalog support (crops + multiple produce/resource item classes).
- Safe economy clamps and anti-chaos floors for cozy play.

### 2) In-world newspaper and town news loop

- Daily newspaper generation with market and community context.
- NPC-driven publishing via `publish_article` and `publish_rumor`.
- Dynamic headline updates and replacement of same-day issue content when new publish events land.
- HUD toasts for notable publish events.
- Day-start edition supports editor content and simulation state summaries.

### 3) Town Request Board (Rumor Board)

- In-world request board UI with Available and Active sections.
- Accept and Complete actions directly in menu.
- Quest rewards paid on completion; required item quests consume items from inventory.
- Expiry handling for overdue requests.
- New Postings action with cooldown/daily cap/outstanding-request guardrails.
- HUD toast when a new request is posted.

### 4) Quest variety and progression

- Quest templates:
  - `gather_crop`
  - `deliver_item`
  - `mine_resource`
  - `social_visit`
- Target diversity using expanded supply/resource pools.
- Reward calculation tied to value and bounded by deterministic rules.
- Relationship-aware social visit tracking.

### 5) NPC chat and social simulation

- Additive NPC follow-up interaction after vanilla dialogue.
- Persistent NPC chat UI with portrait, NPC name, and 10-heart visual meter.
- NPC tone adapts to relationship hearts and speech profile (warmth scales naturally).
- NPC context awareness includes:
  - current season
  - weather
  - day of week
  - time of day
  - market signals
  - memory context
- Full vanilla roster support for Player2 NPC sessions.

### 6) Memory and ambient world behavior

- Per-NPC memory persistence.
- Shared town memory event tracking.
- Ambient NPC-to-NPC conversation hooks.
- Automatic command exposure hooks for reputation, interest influence, and market modifiers.

### 7) Deterministic safety layer

- NPC command schema validation with intent resolver gates.
- Supported command families include:
  - `propose_quest`
  - `adjust_reputation`
  - `shift_interest_influence`
  - `apply_market_modifier`
  - `publish_article`
  - `publish_rumor`
  - `record_memory_fact`
  - `record_town_event`
- Idempotency and duplicate-intent protection.
- Context/lane-aware policy controls and telemetry.

### 8) UI and UX polish

- Hotkey-toggle menus (press key again to close):
  - `K` Market Board
  - `J` Newspaper
  - `L` Town Request Board
- Local Insight HUD indicator (active/dormant) with hover tooltip.
- NPC chat keeps time progression active with slowed pacing for readability.
- In-world feedback via toasts and board/newspaper updates.

## Requirements

- Stardew Valley 1.6+
- SMAPI 4.0+
- Player2 app/account access (required)

## Installation

1. Install SMAPI for Stardew Valley.
2. Download this mod archive.
3. Extract and place the `StardewLivingRPG` folder into your `Stardew Valley/Mods` directory.
4. Launch the game once via SMAPI so config files are generated.
5. Ensure Player2 desktop app is installed/running and logged in.
6. Start game through SMAPI.

## Player2 configuration note

- Player2 is mandatory for this mod's core features.
- This release uses a fixed `Player2GameClientId` tied to the mod creator.
- Do not replace the game client ID in config; requests are intended to credit the creator account.

## Recommended first run

1. Open Market Board (`K`) to confirm economy UI is active.
2. Open Newspaper (`J`) to check day-start issue generation.
3. Open Town Request Board (`L`) and test `New Postings`.
4. Talk to an NPC, finish vanilla dialogue, then use the follow-up chat option.
