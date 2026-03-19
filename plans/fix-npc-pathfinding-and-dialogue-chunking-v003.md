# Fix NPC Pathfinding & Dialogue Chunking — v003

**Date:** 2026-03-20
**Branch:** living-v2
**Status:** Implemented on 2026-03-20 04:23; dotnet build passed, manual in-game validation pending

---

## Context

Following v002 (which fixed schedule-resume delay and added A* PathFindController from actual NPC position), two new issues remain:

1. **Long single-line bubbles** — Full sentences appear as one very long horizontal line that is hard to read left-to-right.
2. **Furniture walk-through persists** — Vanilla `PathFindController` does not iterate `location.furniture`, so NPCs still walk through furniture (e.g. Saloon bar counter).

---

## Root Causes

### Issue 1 — No word-wrap in speech bubbles

- `NPC.showTextAboveHead(string text)` supports `\n` for multi-line rendering natively in vanilla SDV.
- `Normalize()` in `NpcSpeechBubbleService` strips all `\n` and `\r` (replaces with spaces).
- No wrapping is applied before the `showTextAboveHead` call, so all text goes on one line.

### Issue 2 — PathFindController ignores furniture

- Vanilla `PathFindController` collision detection does NOT iterate `location.furniture`.
- `NpcWalkabilityService.IsTileWalkable()` already correctly checks furniture bounding boxes (lines 68-75), but it is never consulted after A* path generation.
- `TryResumeVanillaScheduleFromCurrentPosition` is `private static`, so it cannot access `_walkabilityService`.

---

## Fixes

### Fix 1 — Bubble word-wrap (`Systems/NpcSpeechBubbleService.cs`)

**Step 1:** Add constant after existing constants block (~line 14):
```csharp
private const int BubbleMaxTileWidth = 15;
```

**Step 2:** Add `WrapForBubble` method (after the `Normalize()` method):
```csharp
internal static string WrapForBubble(string text)
{
    if (string.IsNullOrWhiteSpace(text))
        return text;

    var font = Game1.smallFont;
    if (font is null)
        return text;

    var maxPixelWidth = BubbleMaxTileWidth * Game1.tileSize;
    if (font.MeasureString(text).X <= maxPixelWidth)
        return text;

    var words = text.Split(' ');
    var sb = new System.Text.StringBuilder();
    var currentLine = string.Empty;

    foreach (var word in words)
    {
        var testLine = currentLine.Length == 0 ? word : $"{currentLine} {word}";
        if (font.MeasureString(testLine).X > maxPixelWidth && currentLine.Length > 0)
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

**Step 3:** In `Tick()`, wrap transcript bubble display (call `WrapForBubble` just before `showTextAboveHead`):
```csharp
// Before (transcript bubble, ~L162):
npc.showTextAboveHead(chunk, ...);

// After:
var wrappedChunk = WrapForBubble(chunk);
npc.showTextAboveHead(wrappedChunk, ...);
```

**Step 4:** In `Tick()`, wrap encounter bubble display:
```csharp
// Before (encounter bubble, ~L213):
npc.showTextAboveHead(sanitized, ...);

// After:
var wrappedSanitized = WrapForBubble(sanitized);
npc.showTextAboveHead(wrappedSanitized, ...);
```

> **Important:** `WrapForBubble` must be called AFTER `Normalize()` and sanitization — i.e., at the `showTextAboveHead` call site, never inside the chunking phase. The duration calculation should continue to use the unwrapped text length.

---

### Fix 2 — Furniture-aware path validation (`ModEntry.cs`)

**Step 1:** Remove `static` from `TryResumeVanillaScheduleFromCurrentPosition`:
```csharp
// Before:
private static string TryResumeVanillaScheduleFromCurrentPosition(NPC npc)

// After:
private string TryResumeVanillaScheduleFromCurrentPosition(NPC npc)
```

**Step 2:** In the `PathFindController` try-block, after confirming the path has points, add furniture validation before accepting the path:
```csharp
try
{
    npc.controller = new PathFindController(npc, npc.currentLocation, targetTile, facingDirection);
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
}
catch
{
    npc.controller = null;
}
// falls through to pathfindToNextScheduleLocation() → checkSchedule() fallback chain
```

---

## Constraints

- Do NOT create a custom PathFindController replacement — use vanilla's only.
- NEVER split/chunk NPC dialogue if the sentence does not end with punctuation.
- `_walkabilityService` is nullable — always guard with `if (_walkabilityService is not null)`.
- `WrapForBubble` is `internal static` so it can be unit-tested independently.

---

## Files to Change

| File | Change |
|---|---|
| `Systems/NpcSpeechBubbleService.cs` | Add `BubbleMaxTileWidth` constant, add `WrapForBubble()`, update 2 `showTextAboveHead` call sites |
| `ModEntry.cs` | Remove `static` from `TryResumeVanillaScheduleFromCurrentPosition`, add furniture path-validation loop |

---

## Related Plans

- `fix-npc-pathfinding-and-dialogue-chunking-v001.md` — Raised `BubbleMaxChars` to 90, removed mid-sentence splitting
- `fix-npc-pathfinding-and-dialogue-chunking-v002.md` — Added A* PathFindController from actual NPC position, re-added `TryInvokeVanillaMethod` fallback
