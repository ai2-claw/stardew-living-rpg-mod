# M2 Intent Injection Matrix

Purpose: deterministic QA for `NpcIntentResolver` without relying on model output quality.

Use in SMAPI console with:
- `slrpg_intent_inject <json>`

Notes:
- Keep `intent_id` unique per test unless testing duplicate handling.
- Expected outcomes shown below should appear in SMAPI logs.

---

## 1) propose_quest (valid)

```text
slrpg_intent_inject {"intent_id":"qa_pq_001","npc_id":"lewis","command":"propose_quest","arguments":{"template_id":"gather_crop","target":"blueberry","urgency":"high"}}
```

Expected:
- applied
- quest created (`quest_ai_*`)
- mapping log with requested/applied values

## 2) propose_quest (invalid template)

```text
slrpg_intent_inject {"intent_id":"qa_pq_002","npc_id":"lewis","command":"propose_quest","arguments":{"template_id":"invalid_template","target":"blueberry","urgency":"high"}}
```

Expected:
- rejected: invalid template_id

## 3) propose_quest (unexpected argument)

```text
slrpg_intent_inject {"intent_id":"qa_pq_003","npc_id":"lewis","command":"propose_quest","arguments":{"template_id":"gather_crop","target":"blueberry","urgency":"high","hack":true}}
```

Expected:
- rejected: unexpected argument fields

## 4) propose_quest (duplicate intent id)

```text
slrpg_intent_inject {"intent_id":"qa_pq_004","npc_id":"lewis","command":"propose_quest","arguments":{"template_id":"gather_crop","target":"parsnip","urgency":"low"}}
slrpg_intent_inject {"intent_id":"qa_pq_004","npc_id":"lewis","command":"propose_quest","arguments":{"template_id":"gather_crop","target":"parsnip","urgency":"low"}}
```

Expected:
- first: applied
- second: duplicate ignored

---

## 5) adjust_reputation (valid)

```text
slrpg_intent_inject {"intent_id":"qa_rep_001","npc_id":"lewis","command":"adjust_reputation","arguments":{"target":"haley","delta":3,"reason":"helped with market day"}}
```

Expected:
- applied
- `Social.NpcReputation[haley]` increased (clamped)

## 6) adjust_reputation (out of range)

```text
slrpg_intent_inject {"intent_id":"qa_rep_002","npc_id":"lewis","command":"adjust_reputation","arguments":{"target":"haley","delta":99}}
```

Expected:
- rejected: delta out of range (-10..10)

---

## 7) shift_interest_influence (valid)

```text
slrpg_intent_inject {"intent_id":"qa_int_001","npc_id":"lewis","command":"shift_interest_influence","arguments":{"interest":"farmers_circle","delta":2,"reason":"bumper crop season"}}
```

Expected:
- applied
- interest influence adjusted (created if missing)

## 8) shift_interest_influence (invalid interest)

```text
slrpg_intent_inject {"intent_id":"qa_int_002","npc_id":"lewis","command":"shift_interest_influence","arguments":{"interest":"pirates_guild","delta":1}}
```

Expected:
- rejected: invalid interest

---

## 9) apply_market_modifier (valid)

```text
slrpg_intent_inject {"intent_id":"qa_mkt_001","npc_id":"lewis","command":"apply_market_modifier","arguments":{"crop":"blueberry","delta_pct":-0.08,"duration_days":3,"reason":"oversupply"}}
```

Expected:
- applied
- `Economy.MarketEvents` appended with npc modifier event

## 10) apply_market_modifier (invalid range)

```text
slrpg_intent_inject {"intent_id":"qa_mkt_002","npc_id":"lewis","command":"apply_market_modifier","arguments":{"crop":"blueberry","delta_pct":-0.50,"duration_days":3}}
```

Expected:
- rejected: delta_pct out of range (-0.15..0.15)

---

## 11) publish_rumor (valid)

```text
slrpg_intent_inject {"intent_id":"qa_rmr_001","npc_id":"lewis","command":"publish_rumor","arguments":{"topic":"Blueberry surplus may continue","confidence":0.72,"target_group":"shopkeepers_guild"}}
```

Expected:
- applied
- newspaper issue appended with rumor headline/section/hint

## 12) publish_rumor (invalid confidence)

```text
slrpg_intent_inject {"intent_id":"qa_rmr_002","npc_id":"lewis","command":"publish_rumor","arguments":{"topic":"Wild claim","confidence":1.5,"target_group":"town"}}
```

Expected:
- rejected: confidence out of range (0..1)

---

## 13) unknown command

```text
slrpg_intent_inject {"intent_id":"qa_unk_001","npc_id":"lewis","command":"launch_rocket","arguments":{}}
```

Expected:
- rejected: unknown command

---

## Suggested quick verification commands

After running matrix samples:
- `slrpg_debug_state`
- `slrpg_open_rumors`
- `slrpg_open_news`
- `slrpg_p2_health`

These confirm side effects landed in state and telemetry.
