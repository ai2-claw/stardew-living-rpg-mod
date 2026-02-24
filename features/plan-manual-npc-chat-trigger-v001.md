# Manual NPC Chat Trigger Plan v001

## Overview
Players report that automatic NPC follow-up prompts after vanilla dialogue are too frequent and distracting. This feature removes the automatic follow-up trigger and replaces it with explicit manual triggering. If vanilla dialogue/menu opens, player chat becomes a same-NPC follow-up interaction. If vanilla dialogue/menu does not open, chat should open directly on that first interaction. In both cases, chat opens immediately (no intermediate choice menu) and auto-sends "Got a minute to chat?". NPC chat access must remain available even when no vanilla line appears.

## Goals
- Stop auto-opening follow-up prompt/menu immediately after vanilla dialogue closes.
- Require deliberate manual triggering while avoiding unnecessary extra clicks when no vanilla dialogue appears.
- Remove extra follow-up selection friction ("Catch you later", etc.) for this manual-trigger path.
- Make it visually obvious which NPC is currently eligible for manual follow-up chat.
- Reduce accidental wrong-NPC triggers by tightening manual follow-up click distance.
- Keep manual chat accessible for roster NPCs even if no vanilla dialogue was shown first.
- Preserve and prioritize recent vanilla dialogue context when chat starts right after vanilla dialogue.
- Keep additive-dialogue policy intact (never replace vanilla dialogue).
- Keep UX low-friction and legible (clear, predictable trigger conditions).

## Non-Goals
- Rewriting NPC chat UI layout or message rendering.
- Changing Player2 command schema or resolver behavior.
- Replacing vanilla interaction flow for first click.
- Introducing a broad new keybinding system for NPC chat.
- Removing follow-up choice menus from unrelated entry points unless explicitly scoped.

## Current State
- First action-button interaction near a roster NPC calls `TryHandleNpcWorkDialogueHook`, which arms `_npcDialogueHookArmed` and capture state, then allows vanilla dialogue to continue.
- `OnMenuChanged` currently auto-opens `OpenNpcFollowUpDialogue` when vanilla menu closes (`e.NewMenu is null` and hook armed).
- `TryHandleNpcDialogueHookFallback` can also auto-open follow-up if menu did not open shortly after arming.
- Shop-style vanilla interactions (e.g., `ShopMenu`) currently pass through the same hook arming/sync path, so menu-close auto-follow-up behavior can apply there too.
- Vanilla dialogue context is captured into `_recentVanillaDialogueByNpcToken` and injected through `BuildRecentVanillaDialogueContextBlock` and `FOLLOWUP_CONTEXT_RULE` when context tag is `player_chat_followup`.
- Current fallback behavior auto-opens in some no-dialogue cases; after removing auto-open, we need an explicit manual path so these NPCs still remain chat-accessible.
- This means continuity context already exists, but trigger timing is automatic rather than player-initiated.

## Proposed Architecture
1. Interaction contract
- Path A (vanilla opened): first click runs vanilla dialogue/menu; second click on the same roster NPC within the follow-up window opens `NpcChatInputMenu`.
- Path B (no vanilla opened): first click opens `NpcChatInputMenu` directly for that roster NPC.
- If recent vanilla context exists for that NPC, open with follow-up continuity context (`player_chat_followup`).
- If no recent vanilla context exists, open with standard context (`player_chat`) so access is never blocked.

2. Manual follow-up eligibility state
- Add lightweight state keyed to one NPC with two levels:
- `manual follow-up eligible` (vanilla opened/closed; second-click manual open is allowed for the same NPC).
- `follow-up context eligible` (recent vanilla context exists; continuity-enhanced open).
- pending NPC identity (`name` + reference token).
- armed/ready timestamp and expiration window.
- stricter second-click distance gate (smaller than current broad interaction radius; target ~1.5-2.0 tiles).
- optional marker indicating vanilla menu was observed/closed.
- For no-vanilla interactions, bypass follow-up state and allow direct first-click chat open.

3. Auto-open removal
- Remove calls that immediately open follow-up from `OnMenuChanged` close branch.
- Remove/retire fallback auto-open path in `TryHandleNpcDialogueHookFallback` for this feature.
- Ensure this applies equally to dialogue-close and `ShopMenu`-close paths: no automatic mod follow-up menu/chat opens after vanilla shop closes.
- Keep context capture behavior so immediate second click still has dialogue continuity.

4. Manual activation path
- In button handler, if player clicks action button on same NPC and follow-up state is valid:
- open `NpcChatInputMenu` directly (skip question menu), with:
  - `initialPlayerMessage: "Got a minute to chat?"`
  - `autoSendInitialPlayerMessage: true`
  - `defaultContextTag` mapped by eligibility (`player_chat_followup` or `player_chat`)
- consume/clear pending state after opening to avoid repeated auto-reentry.
- if player moves away, interacts with another NPC, opens another menu, or timeout expires: clear pending state.
- If vanilla dialogue/menu does not open for that interaction, open chat immediately on first click with `player_chat`.

5. Context continuity guarantee
- Continue storing vanilla context via existing capture pipeline.
- Ensure manual-opened chat keeps `defaultContextTag: "player_chat_followup"` so `FOLLOWUP_CONTEXT_RULE` remains active for immediate continuation.
- Use `defaultContextTag: "player_chat"` when no recent vanilla context exists.
- Keep stale-pruning (`VanillaDialogueContextMaxAge`) behavior unchanged unless tuning proves necessary.

6. UX feedback (minimal)
- Reuse the vanilla hover chat-bubble affordance for manual-chat-eligible NPCs; tint indicates continuity-ready follow-up state.
- Show the indicator only on hover/targeting (no persistent above-head prompt system).
- Do not interrupt gameplay with extra popups.

## Changes Needed
- `mod/StardewLivingRPG/ModEntry.cs`
- Possibly `mod/StardewLivingRPG/Config/ModConfig.cs` if a toggle/window duration is added (only if needed).
- `mod/StardewLivingRPG/README.md` (update interaction description from automatic follow-up to adaptive manual trigger flow).
- Optional i18n keys if player-facing availability text is added.
- Likely render-hook update for world-space hover indicator drawing (vanilla-style bubble + tint).

## Tasks (numbered)
1. Define trigger contract and acceptance behavior:
   - same NPC required.
   - tighter max interaction radius for manual second-click (target ~1.5-2.0 tiles).
   - timeout window after vanilla close (follow-up path).
   - clear conditions (distance/menu/day/location changes).
   - first-click direct-open behavior when no vanilla dialogue/menu appears.
   - context mapping rules: `player_chat_followup` when recent vanilla context exists, otherwise `player_chat`.
2. Refactor hook state model:
   - split “capture armed” from “manual follow-up ready”.
   - add explicit pending manual-follow-up state fields for post-vanilla flow.
   - add a no-vanilla direct-open branch that does not depend on follow-up-ready state.
3. Update `OnMenuChanged` flow:
   - stop auto-calling `OpenNpcFollowUpDialogue` on menu close.
   - set manual-follow-up-ready state only when vanilla actually opened and closed.
   - explicitly validate shop-close (`ShopMenu`) path also never auto-opens mod follow-up/chat.
4. Remove/adjust fallback auto-open path:
   - replace with state expiry/cleanup logic.
   - ensure no unexpected menu opens when player does nothing.
   - ensure no-dialogue interactions route to explicit first-click manual open instead of silent no-op.
5. Update button handling for manual activation:
   - detect eligible NPC click and determine path (post-vanilla follow-up vs no-vanilla direct open).
   - reject wrong NPC or out-of-range click without opening chat.
   - open `OpenNpcChatMenu(...)` directly (skip follow-up choice menu).
   - pass `initialPlayerMessage: InitialNpcChatPrompt`, `autoSendInitialPlayerMessage: true`, and context tag by eligibility (`player_chat_followup` or `player_chat`).
   - clear state after activation.
6. Add visual eligibility indicator:
   - detect hovered/targeted NPC and check manual follow-up eligibility.
   - draw a vanilla-style hover chat bubble for eligible NPC only, tinted with a distinct color.
   - do not draw persistent above-head prompts when not hovered.
   - clear indicator automatically when follow-up state is consumed/expired/cancelled.
7. Preserve continuity rules:
   - verify manual-open path still sets `player_chat_followup` context tag.
   - verify `BuildRecentVanillaDialogueContextBlock` still contributes prompt context.
8. Add regression coverage via existing smoke/debug command path:
   - first click only -> vanilla dialogue, no auto follow-up.
   - first click opening vanilla shop -> closing shop causes no auto-follow-up/chat open.
   - eligible NPC shows indicator; non-eligible NPCs do not.
   - second click same NPC in window -> follow-up opens.
   - second click after timeout, wrong NPC, or too far away -> no follow-up.
   - immediate chat still references recent vanilla context.
   - NPC with no vanilla line opens manual chat on first click (non-followup context).
9. Update docs and player-facing notes in README/changelog.

## Dependencies
- Existing additive-dialogue hook framework in `ModEntry`.
- Existing vanilla dialogue context capture/injection pipeline.
- SMAPI input/menu event ordering (`ButtonPressed`, `MenuChanged`, update tick) staying consistent.
- Existing NPC chat entry points (`OpenNpcChatMenu`) and prompt constants (`InitialNpcChatPrompt`).

## Risks
- Event ordering edge cases may cause follow-up state to arm/clear at wrong times.
- If second click also triggers vanilla dialogue before suppression, UX may feel inconsistent or briefly double-handle interaction.
- Overly short/long eligibility window can feel unresponsive or intrusive.
- State not cleared correctly could allow stale follow-up prompts with wrong NPC/context.
- Tint selection might be hard to distinguish for some players or clash with vanilla readability.
- Documentation drift if behavior changes but README guidance is not updated.

