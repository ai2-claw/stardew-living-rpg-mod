# Next Steps Task Tracker

This file tracks the post-M3 polish tasks and completion status.

## 1) M4 Anchor Event v1 Hardening
- [x] Add strict one-time trigger guard for `town_hall_crisis` across reload/day boundaries.
- [x] Add explicit cooldown metadata.
- [x] Ensure anchor follow-up cannot duplicate.
- [x] Log/track anchor lifecycle states (`triggered`, `resolved`) via fact keys.

## 2) Post-Anchor World Impact (Visible)
- [x] Inject next-day market signal tied to anchor outcome.
- [x] Inject anchor note into newspaper flow.
- [x] Add related stabilizer town request quest.
- [x] Ensure state writes survive save/reload (state-backed facts/events).

## 3) Resolver/Schema Maturity
- [x] Add explicit reject reason codes (`E_*`).
- [x] Add per-command telemetry counters (applied/rejected/duplicate + per-type map).
- [x] Add strict mode toggle for template repair (`StrictNpcTemplateValidation`).

## 4) Quest System Polish
- [x] Add `slrpg_quest_progress_all` summary command.
- [x] Add clearer completion log (consumed items + gold).
- [x] Keep non-item templates supported (no item hand-in requirement).

## 5) QA & Reliability
- [x] Expand `slrpg_intent_smoketest` coverage to include market/risk handlers.
- [x] Add `slrpg_anchor_smoketest` deterministic check.
- [x] Provide injection matrix for manual deep QA (`M2_INTENT_INJECTION_MATRIX.md`).

## 6) Docs Sync
- [x] Add and maintain this task tracker.
- [x] Update `IMPLEMENTATION_PLAN.md` with progress status notes.
- [x] Update `VERTICAL_SLICE_CHECKLIST.md` with current QA commands.
