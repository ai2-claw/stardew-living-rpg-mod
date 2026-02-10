# EVENT_RESOLUTION.md

Related docs: [DOC_INDEX](./DOC_INDEX.md) · [DATA_MODEL](./DATA_MODEL.md) · [NPC_COMMAND_SCHEMA](./NPC_COMMAND_SCHEMA.json) · [VERTICAL_SLICE_CHECKLIST](./VERTICAL_SLICE_CHECKLIST.md)

Deterministic resolver for turning AI intents + player actions into authoritative world changes.

---

## 1) Inputs per Tick

- `player_actions[]` (trade, gift, dialogue choices, quest completion)
- `npc_intents[]` (validated command envelopes)
- `world_state` (DATA_MODEL)
- `mode_config` (cozy/story/chaos bounds)

---

## 2) Resolution Pipeline

```text
collect -> normalize -> validate -> dedupe -> score -> apply -> emit -> persist
```

### Step A: Normalize
- Convert events/intents into canonical mutation candidates.
- Attach `intent_id`, `source`, `timestamp`, `priority`.

### Step B: Validate (hard gates)
Reject candidate if any fails:
- schema invalid
- out-of-bounds delta
- impossible context (wrong season/location)
- fact lock conflict (already accepted/resolved)
- cooldown active

### Step C: Dedupe / Idempotency
- If `intent_id` in `facts.processed_intents`: skip.
- Otherwise reserve key for this tick.

### Step D: Score / Order
Sort by:
1. safety-critical system rules
2. player-earned outcomes
3. NPC-proposed outcomes
4. flavor-only consequences

### Step E: Apply with Clamps
- Apply mutations in deterministic order.
- Clamp all bounded values (rep, influence, prices).
- Enforce mode caps (cozy stricter than chaos).

### Step F: Emit Consequences
Generate:
- quest updates
- newspaper entries
- npc reaction cues
- telemetry counters

### Step G: Persist
- Commit state transaction.
- Record processed `intent_id`s.
- Write fact locks for accepted/resolved quests.

---

## 3) Pseudocode

```pseudo
function runDailyTick(state, playerActions, npcIntents, config):
  candidates = normalize(playerActions, npcIntents)

  valid = []
  for c in candidates:
    if !passesSchema(c): continue
    if !passesContext(c, state): continue
    if violatesFactLock(c, state.facts): continue
    if cooldownActive(c, state): continue
    if isProcessed(c.intent_id, state.facts): continue
    valid.push(c)

  ordered = sortDeterministically(valid)

  tx = beginTransaction(state)

  for c in ordered:
    applyMutationWithClamps(tx, c, config)
    markProcessedIntent(tx.facts, c.intent_id, c)
    if c.createsQuestAcceptanceOrResolution:
      writeFactLock(tx.facts, c.questFactKey)

  newspaperIssue = buildNewspaper(tx)
  tx.newspaper.issues.append(newspaperIssue)

  emitTelemetry(tx)

  commit(tx)
  return tx
```

---

## 4) Failure Handling

- Partial failure is not allowed inside tick transaction.
- If any apply stage panics:
  - rollback full tick
  - log diagnostics
  - fallback to previous stable state

---

## 5) Cozy Safety Rules

Default mode guardrails:
- `abs(price_delta_day) <= 10%`
- `reputation_delta_per_day_per_npc <= 10`
- `influence_delta_per_day_per_interest <= 5`
- no irreversible penalty without explicit player warning

---

## 6) Test Cases (Minimum)

1. Duplicate intent replay -> exactly one application.
2. NPC repeats already accepted quest -> blocked by fact lock.
3. Oversupply 3 days -> bounded price decline, alternative scarcity bonus appears.
4. Invalid command arguments -> rejected, no state mutation.
5. Mixed intents order -> deterministic identical output given same inputs.
