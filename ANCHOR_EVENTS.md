# ANCHOR_EVENTS.md

Related docs: [DOC_INDEX](./DOC_INDEX.md) · [ARCHITECTURE](./ARCHITECTURE.md) · [QUEST_TEMPLATE_LIBRARY](./QUEST_TEMPLATE_LIBRARY.md)

Hand-authored milestone scenes triggered by simulation thresholds.
These are the cinematic payoffs that anchor emergent systems.

## 1) Design Principles
- Triggered by hard state thresholds (not LLM text only)
- Short, high-signal, memorable scenes
- Must acknowledge player-caused world changes
- Reusable across runs with variant lines

---

## 2) Trigger Model

Each anchor event requires:
- `event_id`
- trigger conditions (state predicates)
- cooldown / one-time lock
- effects on state

Example predicate style:
```json
{
  "all": [
    {"fact_missing": "anchor:town_hall_crisis:seen"},
    {"town_sentiment.economy_lte": -30},
    {"calendar.day_gte": 7}
  ]
}
```

---

## 3) V1 Anchor Events

## A1: Emergency Town Hall
- Trigger: economy sentiment below threshold for 2+ days
- Scene: Lewis convenes public meeting about market instability
- Effects:
  - unlocks stabilizer quests on Rumor Mill
  - sets fact lock `anchor:town_hall_crisis:seen`

## A2: Harvest Unity Festival
- Trigger: two interests simultaneously high trust/influence via bridge event path
- Scene: joint celebration cutscene showing reconciliation/cooperation
- Effects:
  - temporary cross-interest bonus
  - unlock heirloom seed option

## A3: Forest Strain Warning
- Trigger: repeated monocrop pattern + low nature keeper trust
- Scene: Linus/Wizard warning dialogue + environmental cue
- Effects:
  - introduces biodiversity questline
  - increases scarcity bonus on undergrown crops

---

## 4) Authoring Structure
- Scene script: hand-authored dialogue/camera beats
- Variant lines: small parameterized inserts from current state
- No critical logic in generated text

---

## 5) Integration with AI Layer
- AI can reference completed anchors via facts in prompt context
- AI cannot trigger anchors directly; only proposes intents
- Resolver decides and writes anchor fact locks

---

## 6) QA Checklist
- event triggers exactly once (or by designed repeat cadence)
- event cannot deadlock quest progression
- event can be skipped without breaking save
- post-event world effects are visible next day (NPC lines/newspaper/board)
