# Plan: Fix NPC post-encounter pathfinding & dialogue chunking

**Version:** v001  
**Date:** March 20, 2026  
**Status:** Implemented on 2026-03-20 03:07; `dotnet build` passed, manual in-game validation pending

## TL;DR

Two critical bugs:
1. **NPCs walk through walls after encounter conversations** — caused by force-invoking `checkSchedule` via reflection with stale pre-computed routes from wrong starting positions. **Fix:** Stop force-invoking; let vanilla's own update loop resume schedule naturally.
2. **Dialogue text splits mid-sentence into two separate bubbles** — caused by 50-char `BubbleMaxChars` limit with aggressive splitting logic that breaks at spaces/commas. **Fix:** Only chunk at sentence-ending punctuation; never split a single sentence across bubbles.

---

## Issue 1: NPCs walking through walls after encounters

### Root Cause

In `HandoffNpcToVanillaAfterEncounter` ([ModEntry.cs](../mod/StardewLivingRPG/ModEntry.cs#L14843)), after clearing controllers and setting `followSchedule = true`, the method calls `TryInvokeVanillaScheduleHandoff(npc)` which force-invokes vanilla's `checkSchedule(Game1.timeOfDay)` via reflection.

**The problem:** Vanilla's `checkSchedule()` looks up the schedule entry for the current time. That entry contains a `SchedulePathDescription` with a **pre-computed route** (`Stack<Point>`) that was calculated at day start from the NPC's *expected* position at that time. After an encounter, the NPC is at a completely different position than the schedule anticipated. The pre-computed route starts from the wrong tile, so the NPC follows waypoints that don't connect to their actual position — causing them to walk in a straight line toward stale waypoints, clipping through walls, furniture, buildings, and terrain.

Additionally, `ScheduleOverrideService.PatchSingleEntry` creates entries with **empty route stacks** (`new Stack<Point>()`). If such an entry is current when `checkSchedule` fires, PathFindController receives no route and the NPC may walk directly toward the target tile ignoring obstacles entirely.

### Implementation Steps

#### Phase 1: Remove forced schedule handoff

1. **In `HandoffNpcToVanillaAfterEncounter`** ([ModEntry.cs](../mod/StardewLivingRPG/ModEntry.cs#L14843)):
   - **Remove the call to `TryInvokeVanillaScheduleHandoff(npc)` and all its dependent branching logic**
   - Keep `npc.Halt()`, `npc.controller = null`, clearing `temporaryController`, setting `followSchedule = true`, and the `RestoreVanillaSchedule` call
   - The NPC will stand still momentarily (natural after a conversation) and vanilla's own `NPC.update()` loop will call `checkSchedule()` on the next game time tick with correct pathfinding from the NPC's actual position
   - Update logging to reflect the new behavior — log that the NPC was released to vanilla without forced invocation

2. **Remove or mark `TryInvokeVanillaScheduleHandoff` as unused** if no other callers exist.

### Relevant Files
- [ModEntry.cs](../mod/StardewLivingRPG/ModEntry.cs) — `HandoffNpcToVanillaAfterEncounter` (~L14843), `TryInvokeVanillaScheduleHandoff` (~L14882), `TryResumeEncounterParticipants` (~L14823)
- [Systems/ScheduleOverrideService.cs](../mod/StardewLivingRPG/Systems/ScheduleOverrideService.cs) — `PatchSingleEntry` (creates empty route stacks, now safe since we removed forced invocation)

---

## Issue 2: Dialogue splits sentences across bubbles

### Root Cause

`BubbleMaxChars` defaults to 50 (clamped 30–50). When a single sentence exceeds 50 chars, `ChunkText` → `SplitLongUnit` → `FindSplitIndex` tries:
1. Terminal punctuation (`.!?;`) — rarely found within 50 chars of a single sentence
2. Secondary punctuation (`,:-`) — breaks mid-sentence at commas
3. **Word boundary (space)** — **this is where most mid-sentence breaks happen**
4. Force character break with `...` appended

This creates two bubbles from one sentence — the first ends incomplete (often with "..."), the second starts without context. The user finds this very annoying.

### Implementation Steps

#### Phase 1: Fix chunking to never split mid-sentence

1. **Modify `SplitLongUnit`** ([NpcSpeechBubbleService.cs](../mod/StardewLivingRPG/Systems/NpcSpeechBubbleService.cs#L328)):
   - When a single sentence unit exceeds `maxChars`, **don't split it** — return it as one chunk
   - SDV's `showTextAboveHead()` handles word-wrapping internally
   - A slightly-too-long bubble is far better than a mid-sentence break
   - Remove the `while (remaining.Length > maxChars)` loop

2. **Restrict `FindSplitIndex`** ([NpcSpeechBubbleService.cs](../mod/StardewLivingRPG/Systems/NpcSpeechBubbleService.cs#L351)):
   - Only break at terminal punctuation (`.!?`)
   - Remove fallbacks to `,:-` and space-based breaking
   - If no terminal punctuation break exists, return the full length (don't split)

3. **Remove `ForceTerminalChunkEnding` calls** ([NpcSpeechBubbleService.cs](../mod/StardewLivingRPG/Systems/NpcSpeechBubbleService.cs#L362)):
   - Since we no longer break mid-sentence, the `...` appending logic is unnecessary

#### Phase 2: Increase bubble character limit *(parallel with Phase 1)*

4. **Raise `BubbleMaxChars` default and cap**:
   - In [ModConfig.cs](../mod/StardewLivingRPG/Config/ModConfig.cs#L49): increase default from 50 to 90
   - In [ModEntry.cs](../mod/StardewLivingRPG/ModEntry.cs#L1165) GMCM registration: change max from 50 to 120, min from 30 to 40
   - In [ModEntry.cs](../mod/StardewLivingRPG/ModEntry.cs#L1293) clamping: change max from 50 to 120, min from 30 to 40

5. **Raise `EncounterBubbleMaxDurationMs`**:
   - In [NpcSpeechBubbleService.cs](../mod/StardewLivingRPG/Systems/NpcSpeechBubbleService.cs#L12): increase from 1800ms to 3000ms
   - Ensures longer single-sentence bubbles stay visible long enough to read

#### Phase 3: Tighten LLM prompt length guidance *(parallel with Phase 1)*

6. **Encounter prompt builder** — In `BuildEncounterConversationTurnMessage` ([ModEntry.cs](../mod/StardewLivingRPG/ModEntry.cs#L14456)):
   - Change `lineRule` constant from `"Answer with one short in-character sentence..."` 
   - To: `"Answer with one short in-character sentence (under 80 characters) that sounds like something said aloud in town. "`
   - Add explicit character count guidance

7. **Ambient conversation prompts** — In ambient conversation instructions ([ModEntry.cs](../mod/StardewLivingRPG/ModEntry.cs#L5620)):
   - Change from `"Reply in 1 short in-character sentence only."`
   - To: `"Reply in 1 short in-character sentence (under 80 characters) only."`

### Relevant Files
- [Systems/NpcSpeechBubbleService.cs](../mod/StardewLivingRPG/Systems/NpcSpeechBubbleService.cs) — `ChunkText` (L246), `SplitLongUnit` (L328), `FindSplitIndex` (L351), `ForceTerminalChunkEnding` (L362), `SplitSentenceUnits` (L300), `EncounterBubbleMaxDurationMs` (L12)
- [Config/ModConfig.cs](../mod/StardewLivingRPG/Config/ModConfig.cs#L49) — `BubbleMaxChars`
- [ModEntry.cs](../mod/StardewLivingRPG/ModEntry.cs) — GMCM config (L1165), clamping (L1293), `BuildEncounterConversationTurnMessage` (L14439), ambient prompts (L5620)

---

## Verification Plan

### Manual Testing

1. **Post-encounter pathfinding**:
   - Trigger a face-to-face encounter (via autonomy or debug command)
   - After conversation ends, observe both NPCs
   - **Expected:** NPCs stand still momentarily then resume vanilla movement naturally
   - **Fail criteria:** NPC walks through walls, buildings, furniture, or terrain

2. **Dialogue bubbles**:
   - Trigger encounter conversations
   - **Expected:** Every bubble contains a complete sentence ending with punctuation
   - **Fail criteria:** Any bubble ends with "..." from a mid-sentence break, or a sentence is split across two bubbles

3. **SMAPI logs**:
   - After encounter completion, check logs
   - **Expected:** Log message confirms NPC released to vanilla without forced invocation
   - Look for: `"Autonomy: returned {npc.Name} to vanilla schedule after encounter"` or similar

### Edge Cases

4. **Long schedule gap**:
   - Encounter ends at a time with no upcoming schedule entry for 1+ hours
   - **Expected:** NPC stands still naturally until vanilla picks up next entry
   - **Acceptable:** Brief idle standing is natural post-conversation behavior

5. **Very long LLM response**:
   - If LLM generates a 120+ character sentence
   - **Expected:** Appears as one bubble (SDV handles word-wrap internally)
   - **Better than:** Being split mid-sentence into two bubbles

6. **Cross-map schedule destination**:
   - Encounter ends when NPC's next schedule destination is on a different map
   - **Expected:** Vanilla handles the warp naturally on next time tick
   - **Fail criteria:** NPC stuck or walks in wrong direction

---

## Design Decisions

### Key Tradeoffs

1. **No custom PathFindControllers** 
   - Per user requirement: rely entirely on vanilla pathfinding
   - Mod never assigns `npc.controller` after encounter release
   - Vanilla owns all movement re-engagement

2. **Single sentences never split**
   - If a sentence exceeds `BubbleMaxChars`, it goes into one bubble as-is
   - Stardew Valley's `showTextAboveHead()` handles word-wrapping naturally
   - Slightly-too-long bubbles > annoying mid-sentence breaks

3. **NPC stands still briefly after encounter**
   - Intentional tradeoff
   - Brief pause is natural conversation-ending behavior
   - Far better than wall-walking bug

4. **Prompt character hint is soft guidance**
   - LLMs may occasionally exceed 80-character suggestion
   - The chunking fix handles overflow gracefully
   - No strict enforcement at generation time

### Technical Rationale

**Why remove forced `checkSchedule` invocation?**
- Vanilla's schedule system expects to be called from the NPC's actual position
- Pre-computed routes in `SchedulePathDescription` are position-dependent
- Force-invoking after position change creates invalid pathfinding state
- Letting vanilla's own update loop handle it ensures correct behavior

**Why never split sentences?**
- User feedback: mid-sentence breaks are "very annoying"
- Stardew Valley's native text rendering handles overflow gracefully
- Better UX to see full sentence than fragmented chunks
- Terminal punctuation is the only natural breaking point

**Why increase bubble duration?**
- Longer unsplit sentences need more reading time
- Current 1800ms max is too short for 90+ character bubbles
- 3000ms cap maintains readability without feeling sluggish

---

## Further Considerations

### Potential Future Enhancements

1. **Dynamic bubble duration scaling**:
   - Current: `EncounterBubbleCharDurationMs * length` clamped to fixed max
   - Consider: Remove or raise the max cap so very long responses get proportional time

2. **Schedule restoration monitoring**:
   - Add telemetry to track how often vanilla successfully resumes schedules
   - Log cases where NPC has no schedule entry for extended periods

3. **LLM prompt optimization**:
   - Monitor actual response lengths after adding character hints
   - Fine-tune the "under 80 characters" guidance based on observed behavior

4. **Bubble overflow handling**:
   - If very long sentences cause visual issues, consider alternative display methods
   - E.g., dialogue box fallback for 150+ character responses

### Known Limitations

1. **Post-encounter idle duration**:
   - NPCs may stand still for 10+ minutes if next schedule entry is distant
   - Acceptable behavior but could be improved with "return to last known position" logic

2. **Empty route stacks**:
   - `ScheduleOverrideService.PatchSingleEntry` still creates empty routes
   - Safe now that we don't force-invoke, but vanilla must recompute route
   - Consider pre-computing routes or using vanilla's pathfinding directly

3. **Encounter timing edge cases**:
   - If encounter ends exactly at schedule transition time, vanilla's behavior is undefined
   - Rare edge case; suggest monitoring for issues

---

## Implementation Checklist

### Issue 1: Post-Encounter Pathfinding
- [x] Remove `TryInvokeVanillaScheduleHandoff` call from `HandoffNpcToVanillaAfterEncounter` (2026-03-20 03:07)
- [x] Remove dependent branching logic (the log messages that check method name) (2026-03-20 03:07)
- [x] Update logging to reflect new behavior (2026-03-20 03:07)
- [x] Mark `TryInvokeVanillaScheduleHandoff` as obsolete or remove if unused (2026-03-20 03:07)
- [ ] Test encounter completion with NPCs in various locations
- [ ] Verify no wall-walking occurs
- [ ] Verify vanilla schedule resumes naturally

### Issue 2: Dialogue Chunking
- [x] Modify `SplitLongUnit` to not split single sentences (2026-03-20 03:07)
- [x] Remove obsolete `FindSplitIndex` fallback splitting path so single sentences stay intact (2026-03-20 03:07)
- [x] Remove `ForceTerminalChunkEnding` sentence-break fallback (2026-03-20 03:07)
- [x] Change `BubbleMaxChars` default from 50 to 90 in `ModConfig.cs` (2026-03-20 03:07)
- [x] Update GMCM min/max from 30-50 to 40-120 in `ModEntry.cs` registration (2026-03-20 03:07)
- [x] Update clamping logic from 30-50 to 40-120 in `ModEntry.cs` (2026-03-20 03:07)
- [x] Increase `EncounterBubbleMaxDurationMs` from 1800 to 3000 in `NpcSpeechBubbleService.cs` (2026-03-20 03:07)
- [x] Add "(under 80 characters)" to encounter prompt `lineRule` constant (2026-03-20 03:07)
- [x] Add "(under 80 characters)" to ambient conversation prompts (2026-03-20 03:07)
- [ ] Test dialogue bubbles show complete sentences
- [ ] Test no mid-sentence "..." breaks appear
- [ ] Test bubbles remain visible long enough to read

### Documentation
- [x] Update CHANGELOG.md with bugfix entries (2026-03-20 03:07)
- [~] Add notes about post-encounter idle behavior (if needed) - covered by changelog entry; no extra doc added (2026-03-20 03:07)
- [x] Document new bubble character limits in relevant docs (2026-03-20 03:07)

---

## References

### Code Locations

**Issue 1 (Pathfinding):**
- [ModEntry.cs:14843-14880](../mod/StardewLivingRPG/ModEntry.cs#L14843) — `HandoffNpcToVanillaAfterEncounter`
- [ModEntry.cs:14882-14888](../mod/StardewLivingRPG/ModEntry.cs#L14882) — `TryInvokeVanillaScheduleHandoff`
- [ModEntry.cs:14823-14840](../mod/StardewLivingRPG/ModEntry.cs#L14823) — `TryResumeEncounterParticipants`
- [ModEntry.cs:14890-14920](../mod/StardewLivingRPG/ModEntry.cs#L14890) — `TryInvokeNpcMethod` (reflection utility)

**Issue 2 (Dialogue):**
- [Systems/NpcSpeechBubbleService.cs:246-290](../mod/StardewLivingRPG/Systems/NpcSpeechBubbleService.cs#L246) — `ChunkText`
- [Systems/NpcSpeechBubbleService.cs:328-349](../mod/StardewLivingRPG/Systems/NpcSpeechBubbleService.cs#L328) — `SplitLongUnit`
- [Systems/NpcSpeechBubbleService.cs:351-371](../mod/StardewLivingRPG/Systems/NpcSpeechBubbleService.cs#L351) — `FindSplitIndex`
- [Systems/NpcSpeechBubbleService.cs:362-376](../mod/StardewLivingRPG/Systems/NpcSpeechBubbleService.cs#L362) — `ForceTerminalChunkEnding`
- [Config/ModConfig.cs:49](../mod/StardewLivingRPG/Config/ModConfig.cs#L49) — `BubbleMaxChars` default
- [ModEntry.cs:1165-1182](../mod/StardewLivingRPG/ModEntry.cs#L1165) — GMCM bubble config
- [ModEntry.cs:1293-1296](../mod/StardewLivingRPG/ModEntry.cs#L1293) — Config value clamping
- [ModEntry.cs:14439-14520](../mod/StardewLivingRPG/ModEntry.cs#L14439) — `BuildEncounterConversationTurnMessage`
- [ModEntry.cs:5620-5628](../mod/StardewLivingRPG/ModEntry.cs#L5620) — Ambient conversation prompts

### Related Systems

- [Systems/NpcFaceToFaceService.cs](../mod/StardewLivingRPG/Systems/NpcFaceToFaceService.cs) — Encounter staging and controller clearing
- [Systems/ScheduleOverrideService.cs](../mod/StardewLivingRPG/Systems/ScheduleOverrideService.cs) — Schedule override and restoration
- [Systems/NpcAutonomyExecutionService.cs](../mod/StardewLivingRPG/Systems/NpcAutonomyExecutionService.cs) — NPC movement tick logic
- [Systems/NpcWalkabilityService.cs](../mod/StardewLivingRPG/Systems/NpcWalkabilityService.cs) — Tile walkability validation

---

**End of Plan v001**
