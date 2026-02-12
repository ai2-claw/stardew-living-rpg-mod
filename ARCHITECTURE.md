# Stardew Valley Living RPG Mod (Working Title)

Related docs: [DOC_INDEX](./DOC_INDEX.md) · [DATA_MODEL](./DATA_MODEL.md) · [EVENT_RESOLUTION](./EVENT_RESOLUTION.md) · [IMPLEMENTATION_PLAN](./IMPLEMENTATION_PLAN.md)

## 1) Product Direction: Build for Stardew’s Core Audience First

This mod should feel like **Stardew, but alive** — not a different genre pasted on top.

### What the majority of Stardew players typically value
1. Cozy progression (farm growth, routine, low-pressure loops)
2. Relationships and heart-event storytelling
3. Seasonal rhythm and planning
4. Light strategy/optimization (profit planning without punishing complexity)
5. Town identity and atmosphere

### Design principle
Use dynamic systems to create **meaningful but gentle consequences**.  
Default experience should be warm, legible, and low-friction.

---

## 2) Target Player Segments and Feature Mapping

| Segment | What they want | Must-have mod features | What to avoid by default |
|---|---|---|---|
| Cozy Socials (largest) | Character moments, emotional continuity | NPC memory, relationship callbacks, town gossip/newspaper | Harsh penalties, constant conflict |
| Planner Farmers | Better planning inputs | Dynamic crop pricing, trend hints, seasonal demand signals | Wild volatile markets |
| Story Seekers | Emergent stories | Branching events, persistent consequences, evolving NPC arcs | Purely cosmetic dialogue |
| Chaos/Hardcore (smaller) | High-stakes simulation | Optional “High Drama” mode with stronger world shifts | Forcing this mode on everyone |

---

## 3) Experience Modes (Audience Safety Valve)

### A) Cozy Canon (Default)
- Price movement cap: small daily range (e.g., +/- 5–10%)
- Town “interests” (soft blocs), not hard political factions
- Relationship and quest consequences are meaningful but reversible
- Newspaper tone: community updates + opportunities

### B) Story Depth (Optional)
- Stronger branching outcomes
- Heavier reputation effects
- Multi-day NPC plans and conflicts

### C) Living Chaos (Optional)
- High volatility economy
- Strong alliance/betrayal arcs
- Large world-state shifts

---

## 4) Core Gameplay Pillars

## Pillar 1: Living NPCs
- NPCs remember player actions, gifts, promises, slights
- NPC responses can propose world actions via command schema
- Personality persists across days/seasons

## Pillar 2: Dynamic World Economy
- Market responds to aggregate sell volume, seasonality, and events
- Overproducing one crop lowers near-term price (with floors/caps)
- Scarcity, festivals, weather can raise demand

## Pillar 3: Diegetic Feedback (Daily Newspaper)
- Daily paper reports market shifts + social/world consequences
- Includes predictive signals ("Festival demand is rising next week") so planners can act early
- Converts hidden simulation into clear player-readable world narrative

## Pillar 4: Hard Assets (Non-AI Anchors)
- Custom UI surfaces and world objects that expose simulation state directly
- Hand-authored rewards (items/crops) that make progression tangible
- Scripted "anchor" events that provide cinematic payoff at key thresholds
- Template-driven quest board so objectives are always completable

---

## 5) “Town Interests” Model (Stardew-friendly alternative to factions)

Replace militant “factions” with softer community groups:
- Farmers’ Circle
- Shopkeepers’ Guild
- Adventurers’ Club
- Nature Keepers

Each group has:
- trust with player
- current priorities
- influence score

Player choices shift influence gradually. This still gives systemic depth without breaking cozy tone.

Design rule: interests are not strict zero-sum enemies. Prefer "bridge events" that can align multiple groups
(e.g., a Forest Festival can satisfy Nature Keepers and Shopkeepers at the same time).

---

## 6) Economy System (v1)

## State tracked daily
- `sell_volume[crop]` (rolling 7-day)
- `demand[crop]` (seasonal + event modifier)
- `price[crop]`
- `town_sentiment[crop]`

## Suggested formula
`price_today = clamp(base_price * demand_factor * supply_pressure_factor * sentiment_factor, floor, ceiling)`

Where:
- `demand_factor` increases for in-season preferences, festivals, quests
- `supply_pressure_factor` gently lowers price when rolling sell volume is high
- `sentiment_factor` reflects newspaper/narrative trends

Cozy-economy rule: use positive reinforcement where possible.
- Oversupply should also create opportunities elsewhere ("scarcity bonus" on alternatives)
- Avoid hard punishment loops for successful harvests

## Stability controls
- daily change cap
- EMA smoothing on demand
- diminishing oversupply penalties
- generous hard floor (target >= 80% of base in Cozy Canon)
- soft ceiling to avoid runaway spikes

---

## 7) World Impact Loop (What judges will care about)

1. Player acts (dialogue/trade/gift/quest choice)
2. NPC interprets and proposes intents
3. Mod rules validate and resolve outcome
4. Persistent world state mutates (prices, trust, opportunities)
5. Effects surfaced via NPC behavior + newspaper + quest graph

If world state does not mutate, feature does not ship.

---

## 8) Player2 API Integration Plan (from provided OpenAPI)

Base URL: `https://api.player2.game/v1`

## Authentication
- Preferred desktop fast-path: `POST http://localhost:4315/v1/login/web/{game_client_id}` -> `p2Key`
- Fallbacks: Device flow (`/login/device/new`, `/login/device/token`) or Auth Code PKCE
- Header: `Authorization: Bearer <p2Key>`

## NPC pipeline
- Spawn NPC: `POST /npcs/spawn`
- Send player message: `POST /npcs/{npc_id}/chat`
- Receive stream: `GET /npcs/responses` (SSE or NDJSON)
- Fetch history: `GET /npcs/{npc_id}/history`
- Cleanup: `POST /npcs/{npc_id}/kill`

## Budget/ops
- Joules monitoring: `GET /joules`
- Handle stream errors: `insufficient_credits`, `service_unavailable`, `rate_limited`

---

## 9) NPC Command Contract (world-safe)

NPCs can propose actions, but game logic remains authoritative.

Example command set:
- `propose_quest({type, target, urgency, reward_hint})`
- `adjust_reputation({target, delta, reason})`
- `shift_interest_influence({interest, delta, reason})`
- `apply_market_modifier({crop, delta_pct, duration_days, reason})`
- `publish_rumor({topic, confidence, target_group})`

Validation rules:
- bounded deltas
- cooldown gates
- context checks (season, location, quest state)
- anti-loop idempotency keys
- memory-lock fact table checks (NPC cannot re-issue already accepted/resolved quest intents)

---

## 10) V1 Scope (Jam-ready)

- 6 key NPCs with persistent memory, chosen to cover different systems:
  - Pierre or Morris (economy pressure)
  - Robin (world-state/build progression)
  - Lewis (town policy/events)
  - Linus or Wizard (nature/magic interests)
  - Haley or Alex (social/gossip propagation)
  - Demetrius (science/planning signal layer)
- 3 town interests + influence model
- Dynamic pricing for 10 core crops
- Daily newspaper generation
- 12 branching event templates
- 5 validated command types wired to world state
- Cozy Canon mode complete; Story Depth mode partial

### V1.1 Hard-Asset Additions (anti-"AI wrapper" layer)
1. **Pierre's Market Board** (in-world object + custom UI)
   - Paper-styled menu matching "The Pelican Times" aesthetic
   - Displays 8 crops in 2×4 grid with vanilla crop sprites
   - Each crop shows: name, price (color-coded green/red), trend arrows (↑↑↑ to ↓↓↓)
   - 7-day price history bar chart (green/red bars showing movement)
   - Demand/Supply factors and scarcity bonus displayed
   - Designed as a daily planning ritual (like weather/luck checks)
   - All data dynamic: prices update daily based on season, supply pressure, sentiment
2. **Heirloom Crops / Regional Specialties** (3-5 items)
   - unlocked by high influence in specific Town Interests
   - hand-authored sprites + hard-coded economy hooks
3. **Anchor Events** (hybrid milestones)
   - 1-2 hard-scripted scenes per key NPC, triggered by simulation thresholds
   - example: low town sentiment triggers emergency town hall event
4. **Rumor Mill Quest Board (Community Board 2.0)**
   - AI proposes context; objectives/rewards are selected from safe quest templates
   - guarantees completable, testable quests

Success criteria:
- noticeable world change by Day 7 of a normal run
- repeated runs produce meaningfully different outcomes
- no severe economy runaway under normal play

---

## 11) Telemetry & Evaluation

Track:
- number of meaningful world mutations/day
- quest branch divergence rate
- market volatility vs comfort target
- repeat-session retention signals
- hard-asset engagement:
  - Market Board opens/day
  - Rumor Mill quest acceptance/completion rates
  - Heirloom unlock rate
  - Anchor event trigger/completion counts

Player sentiment checks:
- “Did this still feel like Stardew?”
- “Did my choices visibly change the world?”
- “Did the systems feel game-like, not chatbot-like?”

---

## 12) Next Docs to Add

1. `DATA_MODEL.md` (state schemas)
2. `EVENT_RESOLUTION.md` (deterministic resolver pseudocode)
3. `NPC_COMMAND_SCHEMA.json`
4. `NEWSPAPER_TEMPLATES.md`
5. `BALANCE_GUIDELINES.md`
6. `FACT_TABLE.md` (memory-lock + one-truth records for accepted/resolved intents)
7. `MARKET_BOARD_UI_SPEC.md` (screen states, chart glyphs, morning ritual flow)
8. `ANCHOR_EVENTS.md` (thresholds -> scripted cutscenes)
9. `QUEST_TEMPLATE_LIBRARY.md` (safe objective/reward templates mapped from AI context)

