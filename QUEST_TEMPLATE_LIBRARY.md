# QUEST_TEMPLATE_LIBRARY.md

Related docs: [DOC_INDEX](./DOC_INDEX.md) · [ARCHITECTURE](./ARCHITECTURE.md) · [NPC_COMMAND_SCHEMA](./NPC_COMMAND_SCHEMA.json) · [ANCHOR_EVENTS](./ANCHOR_EVENTS.md)

Safe quest templates used by Rumor Mill Quest Board.
AI provides context/flavor only; objectives and rewards are template-bound.

## 1) Template Design Rules
- Always completable with known game systems
- Clear objective + explicit completion condition
- Bounded rewards
- Expiry windows to avoid stale clutter

---

## 2) Core Templates (v1)

## T1: gather_crop
- Objective: deliver specific crop x count
- Parameters: `crop`, `count`, `quality_min?`
- Completion: hand-in confirmed
- Reward bands: gold + small rep + interest influence

## T2: deliver_item
- Objective: deliver crafted/foraged item x count
- Parameters: `item`, `count`
- Completion: hand-in confirmed
- Reward bands: gold + chance newspaper mention

## T3: mine_resource
- Objective: collect ore/gem/resource x count
- Parameters: `resource`, `count`, `min_mine_level?`
- Completion: inventory + hand-in
- Reward bands: gold + adventurer influence

## T4: social_visit
- Objective: talk/gift to target NPC within time window
- Parameters: `npc`, `days_window`, `gift_tag?`
- Completion: conversation/gift event flags
- Reward bands: reputation + social rumor boost

## T5: community_drive
- Objective: contribute mixed items to town goal
- Parameters: `bundle_id`
- Completion: bundle threshold met
- Reward bands: multi-interest influence + anchor-event trigger chance

---

## 3) Reward Guardrails
- gold bounded per template tier
- daily reward cap per player
- influence delta caps by mode
- no irreversible penalties in Cozy Canon

---

## 4) AI Mapping Layer

AI `propose_quest` output can set:
- narrative context text
- preferred template id
- urgency
- optional target

System decides:
- final template
- final objective numbers
- final rewards
- expiry date

If AI proposes invalid template/params:
- fallback to nearest valid template
- log schema violation metric

---

## 5) Validation Checklist
- objective references valid in-game IDs
- required NPC/item exists
- count bounds reasonable for season/day
- expiry >= minimum completion window
