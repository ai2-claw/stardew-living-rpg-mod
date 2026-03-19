# Fix NPC Bubble Wrapping & Wall-Walking — v004

**Date:** 2026-03-20
**Branch:** living-v2
**Status:** Implemented on 2026-03-20 05:18; dotnet build passed, manual in-game validation pending
**Supersedes:** v003

---

## TL;DR

Two bugs remain after v002/v003: (1) speech bubbles render as a single long line because `WrapForBubble` measures with the wrong font, and (2) one NPC per encounter pair walks through walls because the furniture path validation nulls the PathFindController, cascading into vanilla's broken `checkSchedule()` fallback. Fix font measurement using `SpriteText.getWidthOfString()`, and stop nulling the controller when furniture is detected.

---

## Root Cause Analysis

### Issue 1 — Bubble wrapping never triggers

`WrapForBubble` measures text width with `Game1.smallFont.MeasureString()` (~9px per character). But vanilla SDV renders speech bubbles via `SpriteText.drawStringWithScrollCenteredAt()` (~18px per character). With `BubbleMaxTileWidth = 15` and `Game1.tileSize = 64`, the threshold is 960 pixels. A 90-char text measures ~810px with `smallFont` (below threshold — no wrap) but renders ~1620px with `SpriteText` (overflows badly). The font mismatch means wrapping **never triggers**.

### Issue 2 — One NPC walks through walls after encounter

After encounter completion, `TryResumeVanillaScheduleFromCurrentPosition` creates a `PathFindController` (A* path from real position). When the furniture validation (added in v003) detects furniture tiles in the path, it **nulls the controller** and falls through to:

1. `pathfindToNextScheduleLocation()` — May not set a controller; `TryInvokeVanillaMethod` returns `true` for void methods regardless of actual side effects.
2. `checkSchedule()` — Uses **pre-computed routes from day-start position** → wall-walking.

This only affects ONE NPC because the two NPCs have different schedule destinations; one's A* path is clear while the other's crosses furniture (e.g. Saloon bar counter). The NPC whose path crosses furniture gets its controller nulled and cascades into the broken fallback.

Additionally, `followSchedule = true` is set BEFORE `TryResumeVanillaScheduleFromCurrentPosition`. If all fallbacks fail, vanilla's `NPC.update()` sees `followSchedule = true` with no controller and calls `checkSchedule()` autonomously → wall-walking.

---

## Fixes

### Phase 1 — Fix Bubble Wrapping (`Systems/NpcSpeechBubbleService.cs`)

**Step 1:** Add using statement after existing imports:

```csharp
using StardewValley.BellsAndWhistles;
```

**Step 2:** In `WrapForBubble` (~L312-346), replace `Game1.smallFont` measurement with `SpriteText.getWidthOfString()`. Remove the `Game1.smallFont` null guard entirely:

```csharp
internal static string WrapForBubble(string text)
{
    if (string.IsNullOrWhiteSpace(text))
        return text;

    var maxPixelWidth = BubbleMaxTileWidth * Game1.tileSize;
    if (SpriteText.getWidthOfString(text) <= maxPixelWidth)
        return text;

    var words = text.Split(' ');
    var sb = new System.Text.StringBuilder();
    var currentLine = string.Empty;

    foreach (var word in words)
    {
        var testLine = currentLine.Length == 0 ? word : $"{currentLine} {word}";
        if (SpriteText.getWidthOfString(testLine) > maxPixelWidth && currentLine.Length > 0)
        {
            if (sb.Length > 0) sb.Append('\n');
            sb.Append(currentLine);
            currentLine = word;
        }
        else
        {
            currentLine = testLine;
        }
    }

    if (currentLine.Length > 0)
    {
        if (sb.Length > 0) sb.Append('\n');
        sb.Append(currentLine);
    }

    return sb.ToString();
}
```

---

### Phase 2 — Fix One-NPC Wall-Walking (`ModEntry.cs`)

**Step 3:** Relax furniture validation to log-only (~L14903-14919 in `TryResumeVanillaScheduleFromCurrentPosition`). When `PathFindController` creates a valid path (`pathToEndPoint.Count > 0`), **always keep the controller**. Remove `pathBlocked` and the `if (!pathBlocked)` gate:

Before:
```csharp
if (npc.controller?.pathToEndPoint is { Count: > 0 })
{
    var pathBlocked = false;
    if (_walkabilityService is not null)
    {
        foreach (var pathTile in npc.controller.pathToEndPoint)
        {
            if (!_walkabilityService.IsTileWalkable(npc.currentLocation, pathTile, npc))
            {
                pathBlocked = true;
                break;
            }
        }
    }

    if (!pathBlocked)
        return $"PathFindController({npc.currentLocation.Name}, {targetTile.X},{targetTile.Y})";
}

npc.controller = null;
```

After:
```csharp
if (npc.controller?.pathToEndPoint is { Count: > 0 })
{
    // Path exists — keep the controller even if it crosses furniture.
    // Furniture-walking is a cosmetic issue; wall-walking (caused by nulling the controller
    // and cascading into checkSchedule()) is far worse.
    return $"PathFindController({npc.currentLocation.Name}, {targetTile.X},{targetTile.Y})";
}

npc.controller = null;
```

**Step 4:** Remove `checkSchedule()` fallback entirely. Delete this block:
```csharp
if (TryInvokeVanillaMethod(npc, "checkSchedule", Game1.timeOfDay))
    return $"checkSchedule({Game1.timeOfDay})";
```

**Step 5:** Verify controller was actually set after `pathfindToNextScheduleLocation()`. Change:

Before:
```csharp
if (TryInvokeVanillaMethod(npc, "pathfindToNextScheduleLocation"))
    return "pathfindToNextScheduleLocation()";
```

After:
```csharp
if (TryInvokeVanillaMethod(npc, "pathfindToNextScheduleLocation")
    && npc.controller?.pathToEndPoint is { Count: > 0 })
    return "pathfindToNextScheduleLocation()";
```

**Step 6:** In `HandoffNpcToVanillaAfterEncounter`, move `followSchedule = true` to AFTER the resume call, and make it conditional on success (prevents vanilla's own `NPC.update()` from calling `checkSchedule()` when no controller exists):

Before:
```csharp
npc.controller = null;
TrySetMemberValue(npc, "temporaryController", null);
TrySetMemberValue(npc, "followSchedule", true);

if (_scheduleOverrideService?.HasOverride(npc.Name) == true)
    _scheduleOverrideService.RestoreVanillaSchedule(npc);

var method = TryResumeVanillaScheduleFromCurrentPosition(npc);
```

After:
```csharp
npc.controller = null;
TrySetMemberValue(npc, "temporaryController", null);

if (_scheduleOverrideService?.HasOverride(npc.Name) == true)
    _scheduleOverrideService.RestoreVanillaSchedule(npc);

var method = TryResumeVanillaScheduleFromCurrentPosition(npc);
TrySetMemberValue(npc, "followSchedule", !string.IsNullOrWhiteSpace(method));
```

**Step 7:** Add last-resort warp at end of `TryResumeVanillaScheduleFromCurrentPosition`, before the final `return string.Empty`:

```csharp
// Last resort: warp NPC directly to schedule destination rather than letting it walk
// through walls via stale pre-computed routes.
if (!string.IsNullOrWhiteSpace(targetLocationName) && !isOnTargetMap)
{
    var targetLocation = Game1.getLocationFromName(targetLocationName);
    if (targetLocation is not null)
    {
        Game1.warpCharacter(npc, targetLocationName, new Vector2(targetTile.X, targetTile.Y));
        return $"warp({targetLocationName}, {targetTile.X},{targetTile.Y})";
    }
}
else if (isOnTargetMap && targetTile != Point.Zero)
{
    npc.setTilePosition(targetTile);
    return $"teleport({targetTile.X},{targetTile.Y})";
}

return string.Empty;
```

---

## Files to Change

| File | Change |
|---|---|
| `Systems/NpcSpeechBubbleService.cs` | Add `using StardewValley.BellsAndWhistles;`, replace `Game1.smallFont.MeasureString()` with `SpriteText.getWidthOfString()` in `WrapForBubble` |
| `ModEntry.cs` | Relax furniture validation (remove pathBlocked gate), remove `checkSchedule()` fallback, verify controller after `pathfindToNextScheduleLocation()`, make `followSchedule` conditional, add last-resort warp |

---

## Constraints

- Do NOT create a custom PathFindController replacement — use vanilla's only
- NEVER split/chunk NPC dialogue if the sentence does not end with punctuation
- Furniture-walking is accepted as a known cosmetic issue (lower priority than wall-walking)
- `checkSchedule()` must NOT be used as a fallback — pre-computed routes from wrong positions cause wall-walking

---

## Verification

1. Build mod — no compile errors
2. Trigger a face-to-face NPC encounter in-game
3. Verify speech bubbles wrap to ~2 lines (not one long horizontal line)
4. After encounter completes, verify **BOTH** NPCs resume vanilla pathfinding (not just the last speaker)
5. Neither NPC should walk through walls
6. SMAPI log: both NPCs should show `method=PathFindController(...)` in handoff log
7. Saloon test: NPCs may still walk through bar counter (accepted trade-off vs. wall-walking)

---

## Decisions

- **Furniture validation removed (not just relaxed):** The `pathBlocked` gate is removed entirely. A valid A* path with furniture is far preferable to no controller + checkSchedule() wall-walking. The loop body is deleted too since it no longer serves any purpose.
- **`checkSchedule()` permanently removed as fallback:** Pre-computed routes from wrong positions is the root cause of wall-walking across v001-v003. Must never be used.
- **`followSchedule` set conditionally:** Prevents vanilla from calling `checkSchedule()` autonomously when no controller was established.
- **Warp as last resort:** Cosmetically imperfect (NPC pops into position) but prevents being stuck or walking through walls. Should be rare since A* PathFindController succeeds in most cases.

---

## Related Plans

- `fix-npc-pathfinding-and-dialogue-chunking-v001.md` — Raised BubbleMaxChars to 90, removed mid-sentence splitting
- `fix-npc-pathfinding-and-dialogue-chunking-v002.md` — Added A* PathFindController from actual NPC position
- `fix-npc-pathfinding-and-dialogue-chunking-v003.md` — Added furniture path validation (CAUSED the wall-walking cascade — fully reverted in this plan)
