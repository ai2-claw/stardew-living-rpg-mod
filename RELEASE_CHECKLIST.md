# RELEASE_CHECKLIST.md

Pre-release checklist for The Living Valley mod.

## 1) Build & load
- [ ] Build mod successfully in local game environment (`SMAPI_PATH` set).
- [ ] Launch game with mod enabled, no startup errors.
- [ ] Confirm config loads with expected defaults.

## 2) Core gameplay loop
- [ ] Economy updates daily and Market Board reflects movers.
- [ ] Newspaper generates daily and includes predictive hints.
- [ ] Town request board shows available/active/completed flows.
- [ ] Item-gated completion checks work (`have/need`) and consume items.
- [ ] Gold payout is applied to player wallet on completion.

## 3) Player2 integration
- [ ] `slrpg_p2_login` local-app flow succeeds.
- [ ] Device flow fallback succeeds when local app unavailable.
- [ ] `slrpg_p2_stream_start` receives lines and auto-reconnects on drop.
- [ ] `slrpg_p2_status` and `slrpg_p2_health` report sane values.
- [ ] Low-joules guard behavior matches config.

## 4) Intent resolver safety
- [ ] `slrpg_intent_smoketest` all pass.
- [ ] `slrpg_intent_inject` valid/invalid envelopes behave as expected.
- [ ] Reject logs include reason codes (`E_*`).
- [ ] Duplicate intent IDs are ignored.
- [ ] Strict template validation toggle tested on/off.

## 5) Anchor events
- [ ] `slrpg_anchor_smoketest` passes (trigger once, no duplicate, resolves).
- [ ] Anchor follow-up quest appears once.
- [ ] Anchor note appears in newspaper context.
- [ ] Anchor market impact marker present.

## 6) Save/load resilience
- [ ] Save/reload preserves quests, facts, telemetry, newspaper, market events.
- [ ] No duplicate triggers/intents after reload.
- [ ] Re-run smoketests after reload to confirm stability.

## 7) Docs & release artifacts
- [ ] `README.md` command list/config list current.
- [ ] `DOC_INDEX.md` links current docs.
- [ ] `CHANGELOG.md` updated for release.
- [ ] `node scripts/check-dialogue-policy.mjs` passes.
- [ ] Tag/version chosen and release notes prepared.

## 8) Final signoff
- [ ] Full vertical slice run completed.
- [ ] No blocker defects open.
- [ ] Release approved.
