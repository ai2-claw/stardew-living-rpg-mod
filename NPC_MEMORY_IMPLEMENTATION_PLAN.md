# NPC_MEMORY_IMPLEMENTATION_PLAN.md

Related docs: [IN_WORLD_UI_ARCHITECTURE](./IN_WORLD_UI_ARCHITECTURE.md) · [DATA_MODEL](./DATA_MODEL.md) · [EVENT_RESOLUTION](./EVENT_RESOLUTION.md)

## Goal
Add **durable mod-side NPC memory** that survives reconnects/session resets and is fast enough to keep in-world chat responsive.

Design constraints:
- retrieval must be near-instant (target: <10ms per chat turn)
- deterministic/authoritative memory source in mod state
- player sees lightweight "thinking..." UI while request is in-flight

---

## 1) Scope and Non-Goals

## In scope
- Persistent per-NPC memory store in `SaveState`
- Fast memory indexing and retrieval pipeline
- Memory injection into Player2 chat payload (`game_state_info`)
- Chat UI thinking indicator for send->response latency

## Out of scope (v1)
- heavy semantic vector search
- external DB/services
- long freeform transcripts stored verbatim forever

---

## 2) Data Model (v1)

Add to `SaveState`:
- `NpcMemoryState Memory`

### `NpcMemoryState`
- `Dictionary<string, NpcMemoryProfile> Profiles` keyed by NPC short name

### `NpcMemoryProfile`
- `List<NpcMemoryFact> Facts` (bounded ring buffer, e.g. max 200)
- `List<NpcMemoryTurn> RecentTurns` (bounded, e.g. max 40)
- `Dictionary<string,int> TopicCounters` (small frequency map)
- `int LastUpdatedDay`

### `NpcMemoryFact`
- `string FactId` (stable id)
- `string Category` (`promise`, `quest`, `preference`, `relationship`, `event`)
- `string Text` (short, canonicalized)
- `int Day`
- `int Weight` (1..5 importance)
- `int LastReferencedDay`

### `NpcMemoryTurn`
- `int Day`
- `string PlayerText`
- `string NpcText`
- `string[] Tags` (small tag set)

Memory hygiene:
- enforce max sizes
- evict oldest low-weight entries first

---

## 3) Fast Query Strategy

No expensive semantic search in v1.
Use a **hybrid deterministic scorer**:

1. Tokenize player text into normalized keywords
2. Candidate selection from:
   - same-NPC facts
   - recent turns
   - high-weight facts
3. Score =
   - keyword overlap
   - recency bonus
   - weight bonus
   - category boost for quest/event terms
4. Return top K compact snippets (K=3..6)

Target performance:
- O(n) over bounded list per NPC (small enough)
- typical retrieval <= 1-3ms local

---

## 4) Write Pipeline (When to Save Memory)

Write memory entries only for meaningful events:

- accepted/completed/failed requests
- explicit NPC commitments/promises
- significant player intent in chat (manual tag rules)
- anchor event impacts

Avoid writing every token of every line.
Use concise canonical entries to keep retrieval fast.

---

## 5) Chat Injection Contract

Before `SendNpcChatAsync`:
1. fetch top relevant memory snippets for target NPC
2. append compact block to `game_state_info`:

`NPC_MEMORY[Robin]:`
- `Fact: player helped with lumber request on day 4`
- `Fact: player prefers short practical tasks`
- `Recent: discussed tool shed repair`

Rules:
- cap chars for memory block (e.g. 500-900 chars)
- keep only high-signal snippets
- never inject contradictory stale facts (prefer latest)

---

## 6) Thinking Indicator UX

In `NpcChatInputMenu` and board/journal request actions:

States:
- `Idle`
- `Sending...`
- `Thinking...`
- `Reply received`
- `Recovery...` (watchdog)

Implementation:
- set state to `Thinking...` immediately after send
- animate dots (`Thinking.`, `Thinking..`, `Thinking...`) by tick timer
- clear on first matching `npc_id` response line

Timeout UX:
- after threshold, show `Still thinking... reconnecting stream`
- preserve conversation UI; don’t silently close

---

## 7) Milestones

## M1 — Data Structures + Persistence (0.5 day)
- add memory classes and wire into SaveState
- add bounded collections + pruning helpers

Exit:
- save/load memory survives restart

## M2 — Retrieval Engine (1 day)
- implement keyword/recency weighted retriever
- benchmark retrieval path

Exit:
- top-K retrieval under target latency

## M3 — Write Hooks (1 day)
- write memory on quest/events/chat signals
- add normalization/tagging helpers

Exit:
- meaningful memory entries appear during gameplay

## M4 — Payload Injection + UI Thinking (1 day)
- inject memory snippets into chat payload
- add thinking animation/status in chat UI

Exit:
- visible responsive chat UX + contextual continuity after reconnect

## M5 — QA + Tuning (0.5-1 day)
- stress test with long sessions
- tune caps, scoring, pruning

Exit:
- no noticeable chat lag; stable memory quality

---

## 8) Performance Budgets

Hard budgets per chat turn:
- retrieval: <= 10ms max (target <= 3ms)
- memory block composition: <= 5ms
- total local pre-send overhead: <= 20ms

Guardrails:
- bounded lists
- capped snippet counts/chars
- no dynamic file I/O during query path

---

## 9) QA Checklist

- [ ] Memory persists after game restart and Player2 respawn
- [ ] NPC remembers key facts in follow-up conversation
- [ ] Retrieval time remains within budget (logged in debug mode)
- [ ] No runaway memory growth
- [ ] Thinking indicator visible and accurate
- [ ] Watchdog recovery preserves pending message + memory context

---

## 10) First Implementation Tasks (immediate)

1. Add `NpcMemoryState` model files in `State/`
2. Add memory pruning utility in `Systems/`
3. Add `NpcMemoryService` with `WriteFact`, `WriteTurn`, `GetRelevantSnippets`
4. Integrate into `SendPlayer2ChatInternal`
5. Add thinking-state label/animation in `NpcChatInputMenu`
6. Add debug command: `slrpg_memory_debug <npc>`
