# Fix Persistent Multi-Line Speech Bubbles - v006

**Date:** 2026-03-20
**Branch:** living-v2
**Status:** Implemented on 2026-03-20 07:22; dotnet build passed, manual in-game validation pending
**Supersedes:** v005

---

## TL;DR

Multi-line speech bubbles now render (v005 Harmony patch working), but they never fade or disappear. The `DrawPrefix` nulls `textAboveHead` to suppress vanilla's single-line render, but this also suppresses vanilla's timer/alpha lifecycle. The `DrawPostfix` draws at full opacity with no expiration, creating permanent bubbles.

---

## Root Cause Analysis

### Why bubbles are permanent

In `NpcBubbleDrawPatcher.cs`, `DrawPrefix` sets `textAboveHead = null`. Vanilla's `NPC.draw()` then skips its entire text-above-head block, which normally does:

1. `textAboveHeadTimer -= elapsed`
2. Alpha fade-in (when timer > 500): `textAboveHeadAlpha += 0.1f`
3. Alpha fade-out (when timer <= 500): `textAboveHeadAlpha -= 0.04f`
4. `SpriteText.drawStringWithScrollCenteredAt(b, textAboveHead, ..., alpha: textAboveHeadAlpha, ...)`
5. Expiration: when `textAboveHeadAlpha <= 0f` -> `textAboveHead = null`

The `DrawPostfix` restores the text and draws it with no alpha parameter (defaults to 1.0). Every frame the prefix nulls, the postfix restores and redraws at full opacity, causing an infinite loop.

### Deviation from v005 plan

The v005 plan specified saving alpha and color and passing them to `drawStringWithScrollCenteredAt`. The implementation omitted both and drew with default alpha, causing the permanent-bubble regression.

---

## Fix

### Only file to change: `Systems/NpcBubbleDrawPatcher.cs`

Rewrite `NpcBubbleDrawPatcher.cs` to:

1. Add AccessTools field accessors for `textAboveHeadTimer`, `textAboveHeadAlpha`, and `textAboveHeadColor`.
2. Add `_savedColor` alongside `_savedTextAboveHead`.
3. In `DrawPrefix`, also save color before nulling `textAboveHead`.
4. In `DrawPostfix`, replicate vanilla's timer/alpha lifecycle:
   - decrement timer by `Game1.currentGameTime.ElapsedGameTime.Milliseconds`
   - fade in while `timer > 500`
   - fade out while `timer <= 500`
   - write updated timer/alpha back to the NPC
   - permanently clear the bubble when timer and alpha are both exhausted
   - otherwise restore `textAboveHead` and draw each line with the computed alpha
5. Only draw when `alpha > 0f`.

---

## Files to Change

| File | Change |
|---|---|
| `Systems/NpcBubbleDrawPatcher.cs` | Full rewrite - add timer/alpha/color field accessors, lifecycle in postfix, alpha passthrough to draw calls |

No changes to `ModEntry.cs` or `NpcSpeechBubbleService.cs`.

---

## Constraints

- Do NOT create a custom PathFindController replacement - use vanilla's only
- NEVER split/chunk NPC dialogue if the sentence does not end with punctuation
- NPCs must WALK, not warp/teleport, after encounter ends
- `checkSchedule()` must NOT be explicitly called

---

## Verification

1. `dotnet build` passes with no errors
2. Trigger an NPC face-to-face encounter:
   - Multi-line speech bubbles appear above NPCs (text wraps at ~15 tiles)
   - Bubbles fade in on first appearance
   - Bubbles fade out after their timer expires (~2-3 seconds)
   - After fade-out, bubble is fully gone
   - Next bubble in the sequence appears on schedule
3. Short single-line messages (no `\n`) still work via vanilla path
