# CHANGELOG

## 0.8.1 - 2026-02-28

### Added
- Dynamic player family state persistence (`spouse`, `children`, role flags) with backward-compatible save migration.
- Daily family lore detector for marriage and new-child milestones, with idempotent fact keys to prevent duplicate event spam.
- Tiered family awareness flow:
  - immediate seeding for spouse/household/close relations
  - gradual town-wide spread via town-memory propagation
- Prompt grounding block `PLAYER_FAMILY` plus role guidance so NPC dialogue can naturally acknowledge the player as spouse/parent when context is relevant.
- `slrpg_debug_state` family snapshot output (married/parent flags, spouse, children, last detected day).

### Changed
- `TownMemoryService.RecordEvent(...)` now returns the resolved event id (new or deduped) to support deterministic awareness seeding.
- Internal save-state schema version updated from `0.2.0` to `0.3.0` for player-family compatibility migration.

## 0.8.0 - 2026-02-28

### Added
- Ambient NPC-to-NPC multi-turn conversations with per-mode/default turn depth and pair cooldown support.
- Ambient conversation beat system (romance, intrigue, mystery, gossip, work, market, community, ambition) to reduce repetitive pleasantries.
- New debug command: `slrpg_debug_ambient_pair_chat <speaker> <listener> [topic]`.

### Changed
- Manifest version bumped to `0.8.0`.
- Ambient prompt pipeline now separates dialogue generation and extraction contexts, with stronger extraction guidance for memory/event capture.
- Ambient scheduling and fallback persistence now preserve conversation outcomes more reliably through synthetic memory/event persistence when structured commands are missing.
- Ambient overhear moments no longer use HUD toasts; overheard snippets are now queued as in-world gossip cues surfaced through NPC dialogue.

### Fixed
- Prevented ambient NPC-to-NPC lines from leaking into direct player chat replies (including initial "Let's chat" openers) by filtering ambient bleed in immediate and history fallback reply paths.
- Player chat now grounds referenced NPC whereabouts from live world state (including same-location checks), reducing stale lore answers like wrong current locations.

## 0.7.2 - 2026-02-28

### Added
- Quest progress checking via Refresh button: when an active quest is selected, Refresh shows completion progress using i18n keys.

### Changed
- Manifest version bumped to `0.7.2`.

### Fixed
- Rumor Board UI now correctly recalculates layout when window is resized (added `gameWindowSizeChanged` override).
- Progress messages from Refresh/Complete buttons are no longer overwritten by external status sync.

## 0.7.1 - 2026-02-27

### Changed
- Manifest version bumped to `0.7.1`.

### Fixed
- Market Board mini-bar history now includes the latest computed daily price, so indicators reflect current-day movement instead of lagging by one update.

## 0.7.0 - 2026-02-27

### Changed
- Manifest version bumped to `0.7.0`.
- Removed Jas/Vincent age-specific prompt directives from `ModEntry` to avoid redundancy with `assets/vanilla-canon-lore.json`.

### Fixed
- Festival interactions now support NPC chat follow-up reliably by resolving event actors (not only `currentLocation.characters`) for target detection and follow-up state checks.
- Festival events no longer block the NPC chat cursor/fallback path used to open chat after vanilla interaction flow.

### Removed
- Removed runtime child-age response normalization fallback (`NormalizeNpcAgeReply`) and related helpers from `ModEntry`; canon age grounding now relies on lore injection.

## 0.6.1 - 2026-02-27

### Changed
- Newspaper headline generation no longer enforces a hard 30-character truncation in prompt or fallback paths.
- Player2 headline prompt now requests concise tabloid-style headlines (roughly 4-10 words) instead of character-limited output.

### Fixed
- Late-night `pass_out` events now record and publish the active player name (for example, `Farmer John`) instead of generic farmer wording.
- Town event scoring now prioritizes `pass_out` and player-tagged incidents so NPC awareness and follow-up chatter surface those events more reliably.

## 0.6.0 - 2026-02-26

### Added
- Portrait emotion profile framework with per-NPC and per-variant frame mapping.
- Built-in portrait profile defaults file: `assets/portrait-emotion-profiles.json`.
- Companion pack portrait profile template: `custom_npc_pack_template/content/portrait-profiles.json`.
- New portrait profile diagnostics commands:
  - `slrpg_portrait_profile_validate`
  - `slrpg_portrait_profile_dump <npc>`
  - `slrpg_portrait_profile_probe <npc> [emotion]`
- New portrait profile config options:
  - `EnablePortraitEmotionProfiles`
  - `PortraitProfileStrictMode`
  - `LogPortraitProfileResolution`
- Built-in vanilla canon lore grounding file: `assets/vanilla-canon-lore.json`.
- New vanilla canon lore diagnostics commands:
  - `slrpg_vanilla_lore_validate`
  - `slrpg_vanilla_lore_dump <npc>`
- New vanilla canon lore config options:
  - `EnableVanillaCanonLoreInjection`
  - `LogVanillaCanonLoreInjectionPreview`

### Changed
- Manifest version bumped to `0.6.0`.
- Portrait emotion routing expanded with explicit `content` and `blush` support.
- Portrait profile loader now supports dependency mod profile files at:
  - `content/portrait-profiles.json`
  - `assets/portrait-profiles.json`
- Portrait template variants now use explicit `Frames` mappings (no `FrameOffset` in shipped examples).
- `slrpg_regression_targeted` now includes vanilla lore contradiction regressions (Pierre/Abigail, Elliott editor grounding, and referenced-NPC lore selection behavior).

### Fixed
- Resolved emotion-to-portrait index mapping so non-happy emotions no longer collapse to a single frame.
- Enabled portrait-only compatibility packs to load without requiring `content/npcs.json` and `content/lore.json`.
- Fixed first NPC reply emotion behavior in chat:
  - first reply now renders as neutral
  - subsequent replies use explicit/inferred emotion normally
- Fixed high-resolution portrait rendering in chat and newspaper:
  - supports non-standard sheet grids (including short grids like `2x2` and `2x5`)
  - avoids blank newspaper byline portraits for single-frame HD sheets (`1x1`)

## 0.5.0 - 2026-02-24

### Added
- Deterministic NPC location grounding in chat prompt context:
  - exact location phrase
  - indoor/outdoor exposure
  - tile coordinates
  - map micro-area labels with precision level
- Emotion-aware NPC portraits in `NpcChatInputMenu`.
- Live portrait sourcing from the current in-game NPC state, with fallback to portrait asset loads.
- Optional `<emotion:neutral|happy|sad|angry|surprised|worried>` cue support for NPC replies.

### Changed
- NPC mod follow-up chat flow now uses manual triggering after vanilla interactions instead of automatic open.
- Follow-up chat entry preserves and blends recent vanilla dialogue continuity with town-memory context.
- Chat prompt contracts now include explicit location-awareness and portrait-emotion guidance.
- Release metadata updated:
  - manifest version bumped to `0.5.0`
  - GitHub update key removed, Nexus update key retained

### Fixed
- Player-initiated chat no longer stalls behind ambient NPC stream routing in common contention cases.
- Reduced indoor weather-contradiction replies via explicit exposure-aware prompt rules.
- Added robust portrait frame clamping/fallback behavior for NPCs with non-standard portrait sheet widths.

## 0.4.0 - 2025-02-23

### Added
- Integrated custom NPC content pack framework:
  - Load custom NPCs via content packs targeting `mx146323.StardewLivingRPG`
  - Canon baseline validation with strict mode option
  - Lore injection into Player2 prompt context at runtime
  - Template pack included for pack authors at `custom_npc_pack_template/`
  - Debug commands: `slrpg_customnpc_validate`, `slrpg_customnpc_list`, `slrpg_customnpc_dump`, `slrpg_customnpc_reload`
- Scroll functionality for NPC chat menu:
  - Visual scrollbar with thumb drag and track click (page up/down)
  - Mouse wheel scroll as secondary input
  - Scissor clipping prevents text overflow beyond chat region
  - Auto-scroll to bottom on new NPC message arrival
- Config options for custom NPC framework:
  - `EnableCustomNpcFramework` (default `true`)
  - `EnableCustomNpcLoreInjection` (default `true`)
  - `EnableStrictCustomNpcCanonValidation` (default `true`)
  - `CustomNpcLoreLocaleOverride`
  - `LogCustomNpcPromptInjectionPreview` (default `false`)
  - `AmbientRecordTownEventDailyCap` (default `2`)

### Changed
- Version bumped from 0.2.0 to 0.4.0
- Added Nexus update key (`Nexus:42597`) to manifest
- Town memory events now track source NPC
- Town memory propagation expanded to full NPC roster with event-mentions-NPC awareness

## Unreleased

### Added
- Multi-turn ambient NPC-to-NPC chat orchestration with mode-based default depths (`cozy=2`, `story=3`, `chaos=4`, max `4`).
- New ambient debugging command: `slrpg_debug_ambient_pair_chat <speaker> <listener> [topic]`.
- Optional overhear HUD moments for public ambient exchanges with cadence gating.
- External NPC autodiscovery pipeline for no-patch compatibility with player-added NPC mods.
- On-demand Player2 session spawn for discovered roster NPCs.
- Deterministic fallback external NPC lore extraction from observed dialogue/town events when official pack lore is unavailable.
- New diagnostics command: `slrpg_externalnpc_list`.
- Deterministic NPC intent resolver with schema-constrained command validation.
- Runtime handlers for full NPC command set:
  - `propose_quest`
  - `adjust_reputation`
  - `shift_interest_influence`
  - `apply_market_modifier`
  - `publish_rumor`
  - `publish_article`
  - `record_memory_fact`
  - `record_town_event`
  - `adjust_town_sentiment`
- Deterministic NPC ask gate service for manual relationship/interest/market asks with `accept|defer|reject` outcomes.
- Automatic command exposure hooks for:
  - quest lifecycle -> `adjust_reputation`
  - daily town simulation -> `shift_interest_influence`
  - market anomaly checks -> `apply_market_modifier`
- Player2 stream auto-reconnect with exponential backoff.
- Player2 diagnostics commands:
  - `slrpg_p2_status`
  - `slrpg_p2_health`
- Device auth fallback for Player2 login when local app auth is unavailable.
- Canon grounding and in-character style hardening for Lewis NPC responses.
- Market-grounded NPC context (`MARKET_SIGNALS`) injected per chat.
- Quest completion upgrades:
  - item requirement checks
  - item consumption on completion
  - player gold payout on completion
- Quest progress diagnostics:
  - `slrpg_quest_progress`
  - `slrpg_quest_progress_all`
- Intent QA tooling:
  - `slrpg_intent_inject`
  - `slrpg_intent_smoketest`
  - `M2_INTENT_INJECTION_MATRIX.md`
- Anchor QA tooling:
  - `slrpg_anchor_smoketest`
- Child-NPC age guardrails for chat:
  - prompt rule injection for `Jas`/`Vincent`
  - response normalization fallback when a child NPC claims adult ages

### Changed
- External NPC support is additive and compatibility-safe: integrated custom NPC content pack lore remains authoritative and overrides generated fallback lore.
- Player2 roster readiness now tracks only required spawn roster entries, so auto-discovered external NPCs do not stall auto-connect/session health checks.
- Expanded NPC targeting, social-visit inference, and prompt canon lists now include auto-discovered external NPCs when enabled.
- Ambient command policy now blocks `publish_article` in `npc_to_npc_ambient` lane.
- Added config knobs for ambient chat depth/limit/pair-cooldown/overhear cadence.
- NPC quest language shifted toward "town requests".
- Board action label refined to "New Postings" with non-digital in-world status phrasing.
- NPC follow-up prompts adjusted to explicitly additive board checks; no vanilla dialogue replacement.
- `propose_quest` command parameter guidance now enforces strict template/urgency enums.
- Legacy `quest_*` template IDs can be repaired (unless strict mode enabled).
- Resolver now emits structured reject reason codes (`E_*`).
- Telemetry expanded for NPC intents (applied/rejected/duplicate/per-command).
- Telemetry expanded with intent lanes (`auto`/`manual`) and ask-gate outcomes.
- Anchor event flow hardened with cooldown/lifecycle fact locks and visible follow-up effects.
- NPC spawn command descriptors and prompt contracts now expose non-quest mutation commands with stricter usage rules.
- NPC follow-up hook target selection now prioritizes the intended interaction target (facing/action tile), then syncs to the actual opened speaker/menu owner.
- Shop-menu owner resolution expanded to support reflected owner fields/properties and name-token owner values, with Joja-context fallback to Morris.
- NPC follow-up interaction/fallback distance thresholds widened for counter/shop interactions.
- First-follow-up greeting now respects vanilla met-state and suppresses first-time phrasing when follow-up occurs after a vanilla dialogue/menu cycle.

### Fixed
- Prevented hanging behavior in Player2 one-off read flow.
- Added missing UI text wrapping helper used by menu screens.
- Reduced assistant-like NPC phrasing with stricter in-character instructions.
- Fixed follow-up chat opening the wrong nearby NPC when multiple NPCs were in similar range.
- Fixed missing follow-up chat for Morris/Joja shop interactions.
- Fixed child NPCs (Jas/Vincent) incorrectly claiming adult ages.
- Fixed first follow-up chat line incorrectly saying "we haven't spoken before" immediately after vanilla dialogue.

## Notes
- For release tagging, move "Unreleased" entries under a version header (e.g., `## 0.2.0 - YYYY-MM-DD`).
