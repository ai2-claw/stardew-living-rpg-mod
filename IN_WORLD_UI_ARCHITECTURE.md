# IN_WORLD_UI_ARCHITECTURE.md

Related docs: [ARCHITECTURE](./ARCHITECTURE.md) · [IMPLEMENTATION_PLAN](./IMPLEMENTATION_PLAN.md) · [IN_WORLD_UI_IMPLEMENTATION_PLAN](./IN_WORLD_UI_IMPLEMENTATION_PLAN.md)

## Goal
Move from **console-driven debugging flows** to a **fully in-world, diegetic player experience**.

Design target:
- player can play core loop without opening SMAPI console
- NPC and board interactions feel native to Stardew
- deterministic backend remains authoritative and testable

---

## 1) Experience Principles

1. **Diegetic first**
   - actions happen through world objects, dialogue menus, and journals
   - console commands become developer/admin tools only

2. **Cozy clarity**
   - no hidden blockers; show requirements and progress visibly
   - explain outcomes in natural town language

3. **Determinism under the hood**
   - UI triggers intent-safe services, never bypasses validation
   - all mutable operations remain resolver/service mediated

4. **No AI-wrapper feel**
   - world interactions should feel authored and grounded
   - AI text augments context, not core mechanics authority

---

## 2) Interaction Surfaces

## A) Town Request Board (Rumor Board replacement in UX copy)
Primary in-world quest interaction surface.

Capabilities:
- browse available requests
- inspect details (issuer, reward, deadline, requirements)
- accept request
- complete request (with inventory checks)
- show failure reason inline (missing items)

Implementation note:
- keep internal `RumorBoardService` names for compatibility; UI copy uses “Town Requests”.

## B) Request Journal Overlay
Lightweight journal for active requests and progress.

Capabilities:
- active list + progress (have/need)
- expiry day warning
- quick complete action when eligible
- completed/failed history summary

## C) NPC Conversation Hooks
Lewis/Pierre/etc can route into request flow.

Capabilities:
- “Any work available?” opens filtered board state
- NPC response references live market signals
- quest acceptance/completion acknowledgment in dialogue tone

## D) Market Board + Newspaper Linkage
Keep existing market/news systems, add navigation and context.

Capabilities:
- from Market Board -> recommended request category (e.g., scarcity-driven)
- from Newspaper -> suggested Town Request actions

---

## 3) System Architecture (UI phase)

## Layer 1: Presentation
- `RumorBoardMenu` (renamed in UX copy)
- `RequestJournalMenu` (new)
- optional dialogue choice handlers

## Layer 2: Interaction Controllers (new)
- `RequestInteractionController`
  - maps button clicks/dialogue actions -> service calls
  - centralizes UX messages and confirmation logic

## Layer 3: Domain Services (existing + expanded)
- `RumorBoardService` (authoritative accept/progress/complete)
- `NpcIntentResolver` (command-safe mutations)
- `AnchorEventService` (event gating)
- `EconomyService` + `NewspaperService`

## Layer 4: State + Persistence
- `SaveState`
- Facts/idempotency tables
- telemetry counters

Rule:
- UI never mutates state directly; all writes go through services/controllers.

---

## 4) Core Player Flows

## Flow A: Accept request in-world
1. Player opens Town Request Board in world.
2. Selects request -> detail panel.
3. Clicks Accept.
4. Controller calls `AcceptQuest`.
5. UI updates active list + confirmation toast/log.

## Flow B: Complete item request in-world
1. Player opens Request Journal or Board.
2. Selects active request.
3. UI shows `have/need` and reward.
4. Click Complete.
5. Controller calls `CompleteQuestWithChecks`.
6. If valid: consume items, add gold, complete state, success message.
7. If invalid: show missing count and keep request active.

## Flow C: NPC-assisted request
1. Player asks NPC for work.
2. NPC emits `propose_quest` (or dialogue fallback).
3. Resolver validates and adds request.
4. Board/journal reflects new entry immediately.

## Flow D: Anchor-linked request
1. Anchor triggers emergency event.
2. Anchor follow-up request appears on board.
3. Completion updates anchor lifecycle status and visible world messaging.

---

## 5) UX and Immersion Requirements

1. Replace player-facing term “rumor quest” with “Town Request” across menus/dialogue.
2. **Never replace original vanilla NPC dialogue lines.** Mod dialogue must be additive follow-up only.
3. Use short, natural board-era labels (e.g., "New Postings") and avoid modern/digital phrasing (e.g., "channels", "feed", "sync").
4. Display clear requirement lines:
   - target item
   - required count
   - player count
5. Display payout and consequences before confirmation.
6. Keep interactions fast (minimal clicks).
7. Avoid wall-of-text responses in NPC lines.

---

## 6) Data/State Additions (UI phase)

Recommended additions:
- request UI status metadata (last opened tab/filter)
- optional per-request `UiPinned` state
- optional quest history timestamps for journal sorting

Non-breaking constraint:
- backward-compatible with existing save files; default missing fields safely.

---

## 7) Telemetry for UI Phase

Track at minimum:
- board opens
- journal opens
- request accepts/completes/fails
- completion attempt failures (missing items)
- average time from accept -> complete
- NPC intent applied/rejected/duplicate (already present)

Goal:
- identify friction points where players still fall back to console.

---

## 8) Error Handling Strategy

1. **User errors** (missing items, expired request)
   - soft warning in UI
   - no hard failure

2. **System errors** (state mismatch)
   - preserve data, reject action, log reason code
   - provide retry-safe UI state

3. **AI output errors**
   - resolver reject/repair path remains authoritative
   - UI only presents accepted outcomes

---

## 9) Console Command Policy (post-UI)

Keep commands but classify:
- **Player-facing**: minimal (`open_board`, maybe debug optional)
- **Developer-facing**: `intent_inject`, smoketests, state mutation tools

By default gameplay documentation should emphasize in-world interaction, not command usage.

---

## 10) Acceptance Criteria for UI Phase

1. A new player can complete full request loop without console.
2. Request board and journal show all required completion info.
3. Gold payout + item consumption visible and reliable in UI flow.
4. NPC can introduce requests without immersion breaks.
5. Save/reload retains request and journal state.
6. Existing deterministic safety guarantees remain intact.
