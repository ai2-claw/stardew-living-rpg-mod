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

### Changed
- NPC quest language shifted toward "town requests".
- `propose_quest` command parameter guidance now enforces strict template/urgency enums.
- Legacy `quest_*` template IDs can be repaired (unless strict mode enabled).
- Resolver now emits structured reject reason codes (`E_*`).
- Telemetry expanded for NPC intents (applied/rejected/duplicate/per-command).
- Anchor event flow hardened with cooldown/lifecycle fact locks and visible follow-up effects.

### Fixed
- Prevented hanging behavior in Player2 one-off read flow.
- Added missing UI text wrapping helper used by menu screens.
- Reduced assistant-like NPC phrasing with stricter in-character instructions.

## Notes
- For release tagging, move "Unreleased" entries under a version header (e.g., `## 0.2.0 - YYYY-MM-DD`).
