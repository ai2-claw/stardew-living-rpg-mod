# Fix Magician Speech Bubbles During IClickableMenu Input

Speech bubbles don't display during the magician guessing game because the input box was converted from HUD-drawn UI to an `IClickableMenu` (for IME support). When `Game1.activeClickableMenu` is set, Stardew Valley pauses `NPC.update()`, which means the `textAboveHeadPreTimer` (500ms fade-in delay) never counts down and `textAboveHeadAlpha` stays at 0 — the text is set but invisible.

## Root Cause

`NPC.showTextAboveHead()` sets:
- `textAboveHeadPreTimer = 500` (fade-in delay)
- `textAboveHeadAlpha = 0f` (starts invisible)

During normal play, `NPC.update()` decrements the pre-timer, then fades alpha to 1. When an `IClickableMenu` is active, NPC updates are paused — the pre-timer never reaches 0.

The existing draw workaround in [TryDrawTownSquareMagicianBubbleInWorld](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#8683-8710) (temporarily nulling `Game1.activeClickableMenu` before calling `drawAboveAlwaysFrontLayer`) handles the draw-gating but doesn't fix the invisible-alpha problem.

## Proposed Changes

### ModEntry.cs

#### [MODIFY] [ModEntry.cs](file:///d:/talk/Stardew Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs)

**1. In [ShowTownSquareMagicianBubble](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#2134-2153) (line ~2134):** After calling `showTextAboveHead` and setting the timer, also force-clear the pre-timer and set alpha to 1 so the text appears immediately without waiting for `NPC.update()`:

```diff
 liveNpc.showTextAboveHead(cleanText);
 TrySetMemberValue(liveNpc, "textAboveHeadTimer", desiredDurationMs);
+TrySetMemberValue(liveNpc, "textAboveHeadPreTimer", 0);
+TrySetMemberValue(liveNpc, "textAboveHeadAlpha", 1f);
```

**2. In [TryDrawTownSquareMagicianBubbleInWorld](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#8683-8710) (line ~8683):** Also force alpha=1 and preTimer=0 before drawing, as a safety net in case the timer was re-set between showText and this draw call:

```diff
 Game1.activeClickableMenu = null;
+TrySetMemberValue(liveNpc, "textAboveHeadPreTimer", 0);
+TrySetMemberValue(liveNpc, "textAboveHeadAlpha", 1f);
 liveNpc.drawAboveAlwaysFrontLayer(spriteBatch);
```

These two changes together ensure:
- When a bubble is created, it's immediately visible (no pre-timer fade-in delay)  
- On every draw frame, the alpha is forced to 1 so the text stays visible even though `NPC.update()` isn't ticking to manage the fade state

## Verification Plan

### Automated Tests

No existing automated tests cover this flow. The rendering depends on Stardew Valley's NPC drawing pipeline, which can only be validated in-game.

### Manual Verification

1. `dotnet build` the mod
2. Launch Stardew Valley via SMAPI
3. Talk to the Magician NPC (Morrow) and start a guessing game
4. Verify: speech bubbles (opening line, prompt, feedback after guesses) appear **above the NPC's head while the input box is still open**
5. Verify: IME input (e.g. Japanese/Chinese) still works in the input box
6. Verify: bubbles also appear correctly after submitting a guess (feedback text)
