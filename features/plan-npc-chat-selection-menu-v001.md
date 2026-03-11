# NPC Chat Selection Menu Plan v001

## Overview
Add a lightweight post-dialogue selection menu for roster NPC interactions so the player does not jump straight into the AI chat window after vanilla dialogue closes. Instead, the existing manual follow-up interaction should open a classic 3-option choice menu:

- `Talk` -> opens `NpcChatInputMenu`
- `Quest` -> opens `RumorBoardMenu` pre-focused on that NPC
- `Bye` -> closes cleanly with no further action

Assumption: this feature applies to the existing post-vanilla follow-up interaction path for NPCs, not to the first vanilla click itself. Vanilla dialogue remains first and untouched.
Constraint: the selection menu should not add a new greeting line or follow-up prompt above the three choices.

## Goals
- Replace the current immediate post-vanilla AI chat open with a simple, cozy, low-friction 3-option menu.
- Preserve the additive-dialogue policy: vanilla dialogue/menu happens first, mod options happen only afterward.
- Keep `Talk` as a one-click path into the existing AI chat flow.
- Add a `Quest` path that opens the Rumor Board focused on the specific NPC to see their requests.
- Keep the menu legible, fast, and consistent with Stardew's existing question-dialogue presentation.
- Ensure the selection menu never blocks, clears, or overrides continuity context captured from the immediately preceding vanilla dialogue.
- Ensure the auto-sent opener `"Let's chat."` functions only as a chat trigger; NPC replies must prioritize grounded context rather than replying generically to the opener itself.

## Non-Goals
- Replacing vanilla gifting rules, friendship changes, or taste resolution.
- Reworking `NpcChatInputMenu` layout or Player2 transport behavior.
- Building a custom full-screen menu if the standard question dialogue can express the 3-button layout cleanly.
- Changing unrelated NPC interaction flows such as rumor-board, newspaper, or non-follow-up chat entry points.
- Broadening this to all NPC clicks unless explicitly requested later.

## Current State
- `OnButtonPressed` routes action-button NPC interactions through `TryHandleNpcWorkDialogueHook` in [mod/StardewLivingRPG/ModEntry.cs](/D:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#L1453).
- First interaction arms `_npcDialogueHookArmed` and allows vanilla dialogue or shop menus to proceed unchanged.
- When the vanilla menu closes, `OnMenuChanged` marks `_manualNpcFollowUp...` state ready instead of auto-opening chat in [mod/StardewLivingRPG/ModEntry.cs](/D:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#L6149).
- A second interaction on the same NPC within the follow-up window resolves through `TryOpenNpcManualFollowUpFromAction`, which currently suppresses the click and opens `OpenNpcChatMenu(...)` immediately in [mod/StardewLivingRPG/ModEntry.cs](/D:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#L1493).
- The code already preserves recent vanilla dialogue context by tagging these openings as `player_chat_followup`.

## Proposed Architecture
1. Follow-up interaction contract
- Keep the first interaction unchanged: vanilla dialogue or shop opens first.
- Keep the existing manual follow-up window and same-NPC validation.
- Replace the immediate AI chat open on the second interaction with `OpenNpcFollowUpChoiceDialogue(...)`.

2. Menu presentation
- Use Stardew's built-in `createQuestionDialogue(...)` response menu rather than a custom `IClickableMenu`.
- Present exactly three responses in this order: `Talk`, `Quest`, `Bye`.
- Do not show any extra greeting text; the menu should be a clean option list only, since vanilla dialogue already handled the spoken line.

3. Talk branch
- `Talk` should call the same `OpenNpcChatMenu(...)` path used today.
- Preserve:
  - `initialPlayerMessage: InitialNpcChatPrompt`
  - `autoSendInitialPlayerMessage: true`
  - `defaultContextTag: "player_chat_followup"`
- Preserve previously captured vanilla dialogue context exactly as the current direct-open path does; opening the selection menu must not reset or replace that context.
- Treat `"Let's chat."` as a low-information opener. The first NPC response should continue from the strongest available grounded context in this order:
  - recent `pass_out` awareness if present
  - immediately preceding vanilla dialogue continuity
  - current news awareness
  - town memory
  - NPC memory
- The NPC should not behave as though `"Let's chat."` is the substantive topic when stronger grounded context is available.
- Continue recording social-visit progress at the same point the follow-up is consumed.

4. Quest branch
- Selecting `Quest` opens the `RumorBoardMenu` with a focus on the clicked NPC.
- Shows NPC-specific postings or empty-state text with a `Show All` toggle.

5. State model
- Reuse existing `_manualNpcFollowUp...` state for the post-vanilla menu entry window.
- Keep vanilla-dialogue continuity data in its existing storage path; the selection menu may reference it, but must not mutate or discard it merely because the menu was opened or closed.
- Clear state aggressively on:
  - timeout
  - location/menu changes
  - wrong NPC
  - successful talk open
  - successful quest board open

6. Input/render integration
- `TryOpenNpcManualFollowUpFromAction(...)` becomes the seam that opens the 3-choice menu instead of chat.

## Changes Needed
- [mod/StardewLivingRPG/ModEntry.cs](/D:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs)
  - replace direct follow-up chat open with a 3-choice follow-up menu (`Talk / Quest / Bye`)
  - wire `Quest` option to `OpenRumorBoard(npc)`
- Potentially [mod/StardewLivingRPG/Utils/I18n.cs](/D:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/Utils/I18n.cs) and [mod/StardewLivingRPG/i18n/default.json](/D:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/i18n/default.json)
  - if the button labels and short prompt text should be localized instead of inline
- [CHANGELOG.md](/D:/talk/Stardew%20Mod/stardew-living-rpg-mod/CHANGELOG.md)
  - document the new post-dialogue `Talk / Quest / Bye` interaction flow

## Tasks
- [x] Define the exact interaction contract. Decision: this feature changes only the existing post-vanilla follow-up path; it preserves current action-button behavior instead of making the menu right-click-only; it uses the existing follow-up timeout window. Completed 2026-03-11 21:08 +08:00.
- [x] Refactor the current follow-up open path by replacing the direct `OpenNpcChatMenu(...)` call in `TryOpenNpcManualFollowUpFromAction(...)` with a new follow-up choice-dialogue method while preserving same-NPC validation, distance gating, and follow-up context continuity. Completed 2026-03-11 09:21 +08:00.
- [x] Implement the 3-option follow-up menu with `createQuestionDialogue(...)`, using exactly `Talk`, `Quest`, and `Bye`, with no added greeting or spoken prompt text. Completed 2026-03-11 09:21 +08:00.
- [x] Wire the `Talk` branch so the existing AI chat open path stays intact behind the menu and social-visit progress plus suppression timing still happen once. Completed 2026-03-11 09:21 +08:00.
- [x] Wire the `Quest` branch to open the Rumor Board focused on the target NPC. Completed 2026-03-11 09:21 +08:00.
- [x] Add cleanup and regression handling so follow-up state clears on menu transitions, distance breaks, day changes, and timeout expiry, while `Bye` exits only the selection state and leaves earlier vanilla-dialogue context intact for the normal continuity window. Completed 2026-03-11 09:21 +08:00.
- [x] Add verification coverage for post-vanilla second-click menu opening, `Talk` follow-up continuity, preserved vanilla context after menu dismissal, first-reply grounding priority (`pass_out` > vanilla > news > town memory > NPC memory), and unchanged no-vanilla direct-chat fallback. Completed 2026-03-11 09:21 +08:00.
- [x] Update docs/changelog after behavior is verified. Completed 2026-03-11 09:21 +08:00.

## Dependencies
- Existing manual follow-up hook state and menu lifecycle in [mod/StardewLivingRPG/ModEntry.cs](/D:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs).
- Existing `NpcChatInputMenu` open path and follow-up prompt context tags.
- Stardew's built-in question dialogue UI.
- Existing Rumor Board focus flow.

## Risks
- Current input handling treats both mouse buttons as valid action buttons; tightening this to right-click-only could unintentionally change established interaction feel.
- If follow-up timeout is too short or too long, the UX will feel either brittle or sticky.

## Risk Mitigations
1. Input-surface ambiguity
- Default implementation should preserve current action-button behavior unless the user explicitly wants right-click-only enforcement.
- Add targeted regression checks for mouse left, mouse right, and keyboard/controller action input so behavior changes are intentional.

2. Weak opener dominating the reply
- Keep or strengthen prompt rules so small-talk openers like `"Let's chat."` are explicitly treated as low-information.
- Preserve a clear context-priority order in prompt assembly, with recent `pass_out` context highest, then vanilla follow-up continuity, then news, town memory, and NPC memory.
- Add regression checks that fail if the first reply ignores available grounding and responds with generic pleasantries to the opener.

3. Stale pending state
- Clear follow-up state on every likely boundary: menu opened, menu closed unexpectedly, location change, NPC mismatch, timeout, successful interaction, and day transition.
- Do not couple selection-state cleanup to the recent-vanilla-context cache; those lifecycles should remain separate so the menu cannot accidentally wipe prompt continuity.

4. Timeout tuning risk
- Keep follow-up window short but explicit in code so it is easy to adjust during testing.
- Add debug logging around timeout expiry and state clears during development to identify whether players are timing out unexpectedly.
- Validate the timing against real interaction loops in-game before documenting the final UX as stable.
