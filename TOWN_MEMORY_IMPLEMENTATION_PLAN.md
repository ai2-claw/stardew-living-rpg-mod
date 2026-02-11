# TOWN_MEMORY_IMPLEMENTATION_PLAN.md

Related docs: [NPC_MEMORY_IMPLEMENTATION_PLAN](./NPC_MEMORY_IMPLEMENTATION_PLAN.md) · [DATA_MODEL](./DATA_MODEL.md) · [EVENT_RESOLUTION](./EVENT_RESOLUTION.md)

## Goal
Implement **Town Memory**: shared, persistent, believable world knowledge that makes Pelican Town react to major player/world events.

Examples:
- Player fainted in the mines
- Crops failed during a bad weather streak
- A big shipment shortage affected prices
- A festival incident everyone heard about

Design requirements:
- fast retrieval (no chat lag)
- deterministic source of truth in mod state
- believable propagation (who knows what, when)
- additive dialogue only (never replace vanilla lines)

---

## 1) Scope

### In scope (v1)
- Persistent town-event ledger in SaveState
- Event visibility + propagation rules
- Fast top-event retrieval for NPC context injection
- Cooldown/decay to avoid repetitive references

### Out of scope (v1)
- full rumor simulation graph
- heavy NLP semantic indexing
- external services/DB

---

## 2) Data Model

Add to `SaveState`:
- `TownMemoryState TownMemory`

### `TownMemoryState`
- `List<TownMemoryEvent> Events` (bounded ring, e.g. 300)
- `Dictionary<string, NpcTownKnowledge> KnowledgeByNpc` (key: short name)
- `int LastPruneDay`

### `TownMemoryEvent`
- `string EventId`
- `string Kind` (`incident`, `economy`, `festival`, `social`, `weather`)
- `string Summary` (canonical one-liner)
- `int Day`
- `string Location`
- `int Severity` (1..5)
- `string Visibility` (`private`, `local`, `public`)
- `string[] Tags`
- `int MentionBudget` (max total references before decay suppression)

### `NpcTownKnowledge`
- `Dictionary<string, TownKnowledgeEntry> ByEventId`

### `TownKnowledgeEntry`
- `bool Knows`
- `int LearnedDay`
- `int MentionCount`
- `int LastMentionDay`
- `string Angle` (e.g., `concerned`, `gossipy`, `practical`, `official`)

---

## 3) Event Ingestion

Create deterministic hooks that write Town Memory events:

- fainting/low-health incidents (mines/cave)
- anchor event outcomes
- economy shocks from `EconomyService`
- notable quest chain outcomes

Ingestion rules:
- dedupe by `(kind, location, day window)`
- normalize summary text
- assign severity + visibility deterministically

Example event:
- `kind=incident`
- `summary=Player fainted in the mines last night`
- `visibility=local`
- `severity=3`
- `tags=["mines","health","rescue"]`

---

## 4) Propagation Model (Believability)

When event is created, compute who knows:

1. **Immediate knowers** (same day)
   - role-linked NPCs (e.g., doctor/mayor/witness/location-adjacent)
2. **Next-day spread**
   - close social/central-town NPCs
3. **Delayed/never**
   - distant NPCs for low visibility events

Use deterministic mapping table (no AI guesswork) in config/service.

---

## 5) Fast Query Path

Per chat turn to NPC X:

1. fetch `NpcTownKnowledge` for X
2. select known events with:
   - highest relevance to current player text tags
   - recency bonus
   - severity bonus
   - low mention count bonus (prefer not-yet-overused)
3. return top 1-2 snippets

Hard cap:
- <= 2 town snippets
- <= 280 chars total for town block

Performance target:
- <= 3ms retrieval local average

---

## 6) Prompt Injection Contract

Append compact block in `game_state_info`:

`TOWN_MEMORY_FOR_ROBIN:`
- `Player fainted in the mines yesterday (known to Robin; concern angle).`

Rules:
- only inject events NPC is marked as knowing
- avoid repeating same event every turn (cooldown)
- never fabricate world events; only from ledger

---

## 7) Dialogue Behavior Rules

- Town Memory lines are additive flavor/context only.
- Must not override deterministic systems (quests/economy/facts).
- Respect additive-only policy: no replacement of vanilla dialogue.
- One town-memory callback max per interaction unless player follows up.

---

## 8) Repetition Controls

Per NPC/event:
- mention cooldown: e.g. 2 in-game days
- max mentions per event: e.g. 3
- decay relevance after N days (based on severity)

Global:
- prune stale low-severity events after horizon (e.g. 14 days)
- keep severe/public events longer (30+ days)

---

## 9) UX (Optional but recommended)

For chat UI:
- while waiting on response: `Thinking...`
- if town memory context injected, subtle debug/dev tag in logs only (not player-facing spam)

Future:
- newspaper tie-in can surface top public Town Memory event of the day

---

## 10) Milestones

### M1 — State + Models (0.5 day)
- add TownMemory classes
- wire save/load + prune

Exit: survives restart

### M2 — Ingestion Hooks (1 day)
- add event writers for cave faint + key systems
- dedupe + normalization

Exit: events appear reliably in ledger

### M3 — Propagation + Query (1 day)
- deterministic knower mapping
- fast retrieval function

Exit: NPC-specific known events retrieved under latency budget

### M4 — Injection + Repetition Controls (0.5-1 day)
- inject town snippets into chat payload
- implement cooldown/mention budgets

Exit: natural references, no repetitive spam

### M5 — QA + Tuning (0.5 day)
- multi-day simulation checks
- tune spread/decay thresholds

Exit: believable and performant behavior

---

## 11) QA Checklist

- [ ] Event persists across save/load and reconnects
- [ ] Robin can reference cave faint incident when appropriate
- [ ] NPC that should not know event does not reference it
- [ ] Mentions respect cooldown and do not spam
- [ ] Query overhead stays within budget
- [ ] No vanilla dialogue replacement side effects

---

## 12) Immediate Next Tasks

1. Create `State/TownMemoryState.cs`
2. Add `Systems/TownMemoryService.cs`
3. Add event hook for player faint in cave/mines
4. Add `GetRelevantTownMemory(npcName, playerText)`
5. Inject block into `SendPlayer2ChatInternal`
6. Add debug commands:
   - `slrpg_town_memory_dump`
   - `slrpg_town_memory_npc <name>`
