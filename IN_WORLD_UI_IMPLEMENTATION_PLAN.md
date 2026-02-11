# IN_WORLD_UI_IMPLEMENTATION_PLAN.md

Related docs: [IN_WORLD_UI_ARCHITECTURE](./IN_WORLD_UI_ARCHITECTURE.md) · [VERTICAL_SLICE_CHECKLIST](./VERTICAL_SLICE_CHECKLIST.md) · [RELEASE_CHECKLIST](./RELEASE_CHECKLIST.md)

## Objective
Deliver a production-ready in-world interaction layer so core gameplay does not require console commands.

---

## Phase 0 — Foundations (0.5 day)

- [ ] Add this plan + architecture docs to index.
- [ ] Define command deprecation policy (player docs vs dev docs).
- [ ] Identify existing menu entry points and interaction gaps.

Exit criteria:
- clear implementation backlog approved
- no ambiguity on player-facing vs dev-facing flows

---

## Phase 1 — Town Request Board Actions (1-2 days)

### Scope
Upgrade `RumorBoardMenu` to support direct accept/complete interactions.

### Tasks
- [x] Add selectable list rows for available + active requests.
- [x] Add detail panel (issuer, target, need/have, reward, expires).
- [x] Add **Accept** button for available requests.
- [x] Add **Complete** button for active requests.
- [x] Route actions through controller/service (no direct state writes).
- [x] Add user feedback banners/toasts for success/failure.

### Technical Notes
- Reuse `GetQuestProgress` and `CompleteQuestWithChecks`.
- Keep service layer authoritative; UI only invokes.

Exit criteria:
- accept/complete works entirely in menu
- missing item failures are shown in UI, not only logs

---

## Phase 2 — Request Journal Menu (1 day)

### Scope
Add dedicated active request tracker for immersion and convenience.

### Tasks
- [x] Create `RequestJournalMenu` with active/completed tabs.
- [x] Show progress bars or textual `have/need`.
- [x] Add expiry highlighting (e.g., <=1 day warning color).
- [x] Add quick-complete action when eligible.
- [x] Add keybind/open path in config.

Exit criteria:
- player can review and complete active requests from journal alone

---

## Phase 3 — NPC Interaction Bridge (1-2 days)

### Scope
Convert NPC conversation outcomes into discoverable in-world actions.

### Tasks
- [ ] Add dialogue response option from key NPC(s): “Any work today?”
- [ ] On option select, trigger request generation path (resolver-safe).
- [ ] Add in-character acknowledgment when request posted.
- [ ] Ensure NPC phrasing uses “Town Request” terminology.
- [ ] Prevent duplicate posting spam via existing idempotency/facts.

Exit criteria:
- player can get and follow work loop via NPC conversation + board/journal

---

## Phase 4 — UI/World Cohesion (1 day)

### Scope
Link Market Board/Newspaper context with request opportunities.

### Tasks
- [ ] Add market-context hint inside board UI (“Suggested request: ...”).
- [ ] Add newspaper callout linking to relevant request type.
- [ ] Surface anchor-related request urgency in UI.
- [ ] Ensure copied terms are cohesive (Town Requests, not dev jargon).

Exit criteria:
- economy/news/request systems feel like one coherent loop

---

## Phase 5 — Migration + Resilience (0.5-1 day)

### Tasks
- [ ] Add/confirm state migration path for new UI metadata fields.
- [ ] Validate save/load across old and new state structures.
- [ ] Handle null/missing fields without crashes.
- [ ] Verify no duplicate action commits on rapid clicks.

Exit criteria:
- safe load from pre-UI saves
- stable behavior under rapid interaction

---

## Phase 6 — QA & Release Gating (1 day)

### Automated/Scripted
- [ ] Run `slrpg_intent_smoketest`.
- [ ] Run `slrpg_anchor_smoketest`.

### Manual In-World QA
- [ ] Accept request from board UI.
- [ ] Complete request from board UI.
- [ ] Complete request from journal UI.
- [ ] Verify item consumption and gold payout visible to player.
- [ ] Verify fail message when missing items.
- [ ] Verify NPC-generated request appears in board.
- [ ] Verify save/reload retains all request/journal state.

### Docs
- [ ] Update mod README for in-world player flow first.
- [ ] Keep console commands in dev/admin section.
- [ ] Update release checklist with UI-phase checks.

Exit criteria:
- no console required for normal gameplay loop
- release checklist section passes

---

## Risks and Mitigations

1. **UI complexity creep**
- Mitigation: deliver board actions first, journal second.

2. **State race on rapid clicks**
- Mitigation: disable action buttons while processing and enforce service idempotency.

3. **NPC text/quest mismatch**
- Mitigation: resolver authoritative + UI only displays accepted intents.

4. **Regression from debug-command assumptions**
- Mitigation: keep commands intact while shifting player docs and QA to in-world path.

---

## Deliverables Checklist

- [ ] Upgraded Town Request Board with accept/complete
- [ ] New Request Journal menu
- [ ] NPC “work request” interaction bridge
- [ ] Cohesive board/news/request messaging
- [ ] Updated docs and release checks

---

## Suggested Execution Order (single-threaded)

1. Phase 1 (board actions)
2. Phase 2 (journal)
3. Phase 3 (NPC bridge)
4. Phase 4 (cohesion)
5. Phase 5 (migration)
6. Phase 6 (QA/docs/release gate)
