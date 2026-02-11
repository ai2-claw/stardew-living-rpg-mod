# IMPLEMENTATION_PLAN.md

Related docs: [DOC_INDEX](./DOC_INDEX.md) · [ARCHITECTURE](./ARCHITECTURE.md) · [VERTICAL_SLICE_CHECKLIST](./VERTICAL_SLICE_CHECKLIST.md)

Execution plan to move from docs -> playable vertical slice -> jam demo.

## 1) Recommended Mod Stack

Primary recommendation (Stardew 1.6+):
- **SMAPI** (runtime + events)
- **C# mod project** for deterministic sim, quest hooks, save data
- **Content Patcher** for data assets/text/events where possible

Optional UI helper:
- vanilla menu APIs first; avoid heavy dependencies in v1

Reasoning:
- best stability for jam timeline
- deterministic logic easier in C# than patchwork content files

---

## 2) Repo Structure (proposed)

```text
mod/
  StardewLivingRPG/
    StardewLivingRPG.csproj
    ModEntry.cs
    Config/
      ModConfig.cs
    State/
      SaveState.cs
      EconomyState.cs
      SocialState.cs
      QuestState.cs
      FactTable.cs
    Systems/
      DailyTickService.cs
      EconomyService.cs
      IntentResolver.cs
      QuestService.cs
      NewspaperService.cs
      AnchorEventService.cs
    Integrations/
      Player2Client.cs
      NpcSessionManager.cs
      StreamConsumer.cs
    UI/
      MarketBoardMenu.cs
      RumorBoardMenu.cs
    Assets/
      i18n/
      sprites/
      data/
```

---

## 3) Milestones

Progress snapshot (2026-02-11):
- M0 ✅ complete
- M1 ✅ complete
- M2 ✅ complete (auth, stream, schema-constrained resolver, command handlers)
- M3 ✅ complete (template quests + completion checks + rewards)
- M4 ✅ implemented and hardened (A1 trigger, lock/cooldown, visible follow-up effects)
- M5 🟡 in progress (QA harness/docs polish ongoing)

## M0: Bootstrapping (0.5-1 day)
- Create SMAPI mod skeleton
- Load/save typed state
- Add config with modes (cozy/story/chaos)
- Add daily tick hook

Exit criteria:
- state persists across save/reload
- daily tick executes once/day without duplication

## M1: Economy Core + Board Readout (1-2 days)
- implement `EconomyService` formulas + clamps
- track rolling sell volumes
- create first `MarketBoardMenu` (read-only)
- print daily newspaper text from real deltas

Exit criteria:
- 10 crops update daily
- board shows today/yesterday/trend
- newspaper generated each morning

## M2: Player2 NPC Intent Pipe (1-2 days)
- auth flow integration (local app fast-path first)
- spawn 1-2 NPC sessions
- stream responses from `/npcs/responses`
- parse command envelope + schema validate

Exit criteria:
- NPC can propose valid command
- resolver accepts/rejects deterministically

## M3: Rumor Board + Template Quests (1-2 days)
- implement quest template library
- map AI context -> safe template instance
- create `RumorBoardMenu`

Exit criteria:
- player can accept/complete at least 3 template types
- no impossible quest generation

## M4: Anchor Event v1 (1 day)
- implement A1 Emergency Town Hall trigger
- hard fact lock + cooldown handling

Exit criteria:
- event triggers exactly once at threshold
- post-event state changes visible next day

## M5: Polish + Demo Loop (1 day)
- telemetry counters
- fail-safe fallback states
- jam demo script

---

## 4) Vertical Slice Definition (must ship first)

**Slice:** Overproduce blueberry -> market reacts -> board displays trend -> newspaper reports + predicts alternatives -> NPC comments -> rumor quest appears.

Pass checklist:
1. Sell large blueberry volume for 2-3 in-game days
2. Blueberry price softens within cozy cap
3. Alternative crop gets scarcity bonus
4. Market board reflects both changes
5. Newspaper includes one predictive hint
6. One NPC references market condition
7. Rumor Board offers relevant template quest

---

## 5) Risk Register

1. Player2 auth friction
- Mitigation: local app auth first; device flow fallback UI

2. Streaming instability / rate limits
- Mitigation: reconnect backoff + queue; budget mode via `/joules`

3. AI command nonsense
- Mitigation: strict schema + fallback template mapping + fact locks

4. Economy runaway
- Mitigation: hard clamps + daily delta cap + cozy floor

---

## 6) Immediate Coding Tasks (next 10)

1. Create SMAPI project scaffold
2. Add SaveState models matching `DATA_MODEL.md`
3. Hook DayStarted event -> `DailyTickService.Run()`
4. Implement crop sell-volume ingestion from shipped items
5. Implement economy formula with cozy clamps
6. Build temporary debug board (text-only menu)
7. Implement newspaper issue generation and persistence
8. Create `Player2Client` auth + `/joules` check
9. Implement stream consumer + command validation against `NPC_COMMAND_SCHEMA.json`
10. Implement minimal resolver with idempotency/fact lock writes

