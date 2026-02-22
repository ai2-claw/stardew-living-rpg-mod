# CHANGELOG

## Unreleased

### Added
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
