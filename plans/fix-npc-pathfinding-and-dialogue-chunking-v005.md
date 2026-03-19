# Fix NPC Bubble Multi-Line & Schedule Resume — v005

**Date:** 2026-03-20
**Branch:** living-v2
**Status:** Implemented on 2026-03-20 06:05; dotnet build passed, manual in-game validation pending
**Supersedes:** v004

---

## TL;DR

Two issues remain after v004: (1) bubble text stays single-line because vanilla's `drawStringWithScrollCenteredAt` ignores `\n` characters — requires a Harmony patch on `NPC.draw()` to intercept and stack lines. (2) NPCs get warped to wrong locations or stuck permanently because of the v004 warp/teleport fallback and conditional `followSchedule`.

---

## Root Cause Analysis

### Issue 1 — Bubble text is still single-line after WrapForBubble fix

`WrapForBubble` correctly inserts `\n` using `SpriteText.getWidthOfString()` for measurement (fixed in v004). However, vanilla `NPC.draw()` renders `textAboveHead` via:

```csharp
SpriteText.drawStringWithScrollCenteredAt(b, textAboveHead, (int)local.X, (int)local.Y, ...);
```

`drawStringWithScrollCenteredAt` renders a **single-line scroll background** and does NOT split on `\n`. The newlines inserted by `WrapForBubble` are silently ignored during rendering. No amount of `\n` insertion can fix this without changing how the text is rendered.

**Fix:** Harmony prefix+postfix on `NPC.draw()`. When `textAboveHead` contains `\n`:
- Prefix saves the text and sets `textAboveHead = null` to suppress vanilla's single-line render
- Postfix restores the text, splits on `\n`, and draws one `drawStringWithScrollCenteredAt` call per line at stacked Y offsets

### Issue 2 — NPCs warp to wrong location or get stuck permanently

v004 introduced two regressions:

1. **Warp/teleport fallback:** `targetTile` from `SchedulePathDescription` is the **final destination** (possibly on a different map). When `isOnTargetMap = true` but `PathFindController` still fails (e.g., the tile requires crossing warp points within the same map chain), the `npc.setTilePosition(targetTile)` teleports the NPC to a potentially unreachable tile mid-map.

2. **Conditional `followSchedule`:** `TrySetMemberValue(npc, "followSchedule", !string.IsNullOrWhiteSpace(method))` means when all pathfinding methods fail, `followSchedule = false`. Vanilla's `NPC.update()` then never calls `checkSchedule()` at the next time key transition, leaving the NPC frozen permanently.

---

## Fixes

### Phase 1 — Multi-Line Speech Bubbles: Harmony Patch on NPC.draw()

**Step 1:** Create new file `mod/StardewLivingRPG/Systems/NpcBubbleDrawPatcher.cs`:

```csharp
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;

namespace StardewLivingRPG.Systems;

internal static class NpcBubbleDrawPatcher
{
    [ThreadStatic]
    private static string? _savedTextAboveHead;
    [ThreadStatic]
    private static float _savedAlpha;
    [ThreadStatic]
    private static int _savedColor;

    public static void Apply(string uniqueId)
    {
        var harmony = new Harmony(uniqueId);
        var drawMethod = typeof(NPC).GetMethod("draw",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
            null,
            new[] { typeof(SpriteBatch), typeof(float) },
            null);

        if (drawMethod is null)
            return;

        harmony.Patch(
            original: drawMethod,
            prefix: new HarmonyMethod(typeof(NpcBubbleDrawPatcher), nameof(DrawPrefix)),
            postfix: new HarmonyMethod(typeof(NpcBubbleDrawPatcher), nameof(DrawPostfix)));
    }

    // Before NPC.draw() — if the text has newlines, suppress vanilla single-line rendering
    private static void DrawPrefix(NPC __instance)
    {
        _savedTextAboveHead = null;
        var text = __instance.textAboveHead;
        if (text is null || !text.Contains('\n'))
            return;

        // Save and suppress so vanilla doesn't render it as a single overflowing line
        _savedTextAboveHead = text;
        _savedAlpha = __instance.textAboveHeadAlpha;
        _savedColor = __instance.textAboveHeadColor;
        __instance.textAboveHead = null;
    }

    // After NPC.draw() — render each line of the wrapped text as its own scroll bubble
    private static void DrawPostfix(NPC __instance, SpriteBatch b)
    {
        if (_savedTextAboveHead is null)
            return;

        __instance.textAboveHead = _savedTextAboveHead;
        var lines = _savedTextAboveHead.Split('\n');
        var viewport = Game1.viewport;

        // Start at the same Y offset vanilla uses (~128px above NPC's standing Y)
        var local = Game1.GlobalToLocal(viewport,
            new Microsoft.Xna.Framework.Vector2(__instance.getStandingX(), __instance.getStandingY() - 128f));

        // Stack lines from top to bottom — each scroll background is ~SpriteText.getHeightOfString() px tall
        var lineHeight = SpriteText.getHeightOfString("A") + 4;
        var totalHeight = lines.Length * lineHeight;

        // Center the block vertically around the original offset
        var startY = (int)local.Y - (totalHeight / 2);

        for (int i = 0; i < lines.Length; i++)
        {
            var lineY = startY + i * lineHeight;
            SpriteText.drawStringWithScrollCenteredAt(
                b,
                lines[i],
                (int)local.X,
                lineY,
                "",
                _savedAlpha,
                _savedColor,
                2,
                0.99f);
        }

        _savedTextAboveHead = null;
    }
}
```

**Step 2:** In `ModEntry.cs`, find where `MarketSellPricePatcher.Apply(...)` is called and add the new patcher call alongside it:

```csharp
NpcBubbleDrawPatcher.Apply(ModManifest.UniqueID);
```

**Step 3:** Keep `WrapForBubble` and both `showTextAboveHead` call sites in `NpcSpeechBubbleService.cs` unchanged — they already insert `\n` at correct pixel-width boundaries.

---

### Phase 2 — Fix NPC Resume After Encounter

**Step 4:** In `TryResumeVanillaScheduleFromCurrentPosition`, remove the entire warp/teleport fallback block (~L14918-14931). The method should end with:

```csharp
        if (TryInvokeVanillaMethod(npc, "pathfindToNextScheduleLocation")
            && npc.controller?.pathToEndPoint is { Count: > 0 })
            return "pathfindToNextScheduleLocation()";

        return string.Empty;
    }
```

**Step 5:** In `HandoffNpcToVanillaAfterEncounter`, change `followSchedule` back to always true. Change:

```csharp
TrySetMemberValue(npc, "followSchedule", !string.IsNullOrWhiteSpace(method));
```

To:

```csharp
TrySetMemberValue(npc, "followSchedule", true);
```

This ensures vanilla's `NPC.update()` can call `checkSchedule()` at the next time key transition if our PathFindController/pathfindToNextScheduleLocation methods both failed — preventing NPCs from being permanently stuck.

---

## Files to Change

| File | Change |
|---|---|
| **NEW** `Systems/NpcBubbleDrawPatcher.cs` | Harmony prefix+postfix on `NPC.draw(SpriteBatch, float)` |
| `ModEntry.cs` (init) | Call `NpcBubbleDrawPatcher.Apply(ModManifest.UniqueID)` near `MarketSellPricePatcher.Apply(...)` |
| `ModEntry.cs` ~L14857 | `followSchedule` always `true` (not conditional) |
| `ModEntry.cs` ~L14918-14931 | Delete the warp/teleport fallback block |
| `Systems/NpcSpeechBubbleService.cs` | No changes needed |

---

## Constraints

- Do NOT create a custom PathFindController replacement — use vanilla's only
- NEVER split/chunk NPC dialogue if the sentence does not end with punctuation
- NPCs must WALK, not warp/teleport, after encounter ends
- `checkSchedule()` must NOT be explicitly called — but vanilla may call it autonomously via `followSchedule = true`

---

## Verification

1. Build mod — no compile errors
2. Speech bubbles should render with one scroll background per line, stacked vertically
3. After encounter completes, **both** NPCs resume walking (no warping, no stuck NPCs)
4. NPCs that fail pathfinding reclaim their schedule via vanilla's update loop at next time key
5. SMAPI log: both NPCs show `method=PathFindController(...)` or `pathfindToNextScheduleLocation()` — never `warp()` or `teleport()`
6. Test in Saloon: NPCs may still walk through bar counter furniture (accepted trade-off)

---

## Decisions

- **Harmony patch is necessary** for multi-line bubbles because `drawStringWithScrollCenteredAt` is a single-line renderer with no supported multi-line override via the public API. Harmony is already a project dependency (`0Harmony.dll`).
- **ThreadStatic fields in patcher** to safely pass data between prefix and postfix without mutating shared state.
- **`followSchedule = true` unconditionally**: When pathfinding fails (rare), vanilla's time-key schedule system handles resumption naturally. NPCs may stand still briefly but will never be permanently frozen or teleported.
- **No warp/teleport:** Per user requirement.

---

## Related Plans

- `fix-npc-pathfinding-and-dialogue-chunking-v001.md` — Raised BubbleMaxChars to 90, removed mid-sentence splitting
- `fix-npc-pathfinding-and-dialogue-chunking-v002.md` — Added A* PathFindController from actual NPC position
- `fix-npc-pathfinding-and-dialogue-chunking-v003.md` — Added furniture path validation (caused wall-walking cascade — see v004)
- `fix-npc-pathfinding-and-dialogue-chunking-v004.md` — Fixed font measurement (SpriteText), relaxed furniture validation, removed checkSchedule fallback, introduced warp/teleport fallback (CAUSED teleport bug — fixed in this plan)
