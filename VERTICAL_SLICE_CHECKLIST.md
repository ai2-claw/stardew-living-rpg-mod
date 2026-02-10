# VERTICAL_SLICE_CHECKLIST.md

## Goal
Deliver one complete loop proving this is a **gameplay system**, not chatbot wrapper.

## Scenario
"Blueberry Oversupply Week"

## Setup
- New or seeded save on day >= 3
- Market board enabled
- NPC session for Pierre or Lewis active
- Rumor Board active

## Steps
1. Player sells high blueberry volume for 3 days.
2. Daily tick updates economy.
3. Market Board shows blueberry softening + alternative scarcity bonus.
4. Newspaper prints market headline + predictive hint.
5. NPC line references current market condition.
6. Rumor Board posts one related quest template.
7. Player accepts and completes the quest.

## Assertions
- Price delta/day remains within cozy cap.
- No duplicate quests from repeated NPC intent.
- At least 1 fact lock written for quest acceptance.
- At least 1 telemetry event for board open + quest completion.
- Save/reload preserves all above state.

## Demo-Ready Output
- screenshot: Market Board trend
- screenshot: Newspaper issue
- screenshot: Rumor quest accepted/completed
- log excerpt: resolver applied intent + idempotency key
