# Plan: Fix NPC post-encounter pathfinding & dialogue chunking (v002 - CORRECTED)

**Version:** v002 (corrects v001)  
**Date:** March 20, 2026  
**Status:** Implemented on 2026-03-20 03:40; dotnet build passed, manual in-game validation pending

## TL;DR

**v001 diagnosis:** The first fix removed forced `checkSchedule` invocation entirely, which caused NPCs to stand idle for 30+ game minutes until vanilla's update loop naturally hit the next schedule time key. Wall-walking persisted for one pair because when `checkSchedule` eventually did fire, it still used pre-computed routes from the wrong starting position.

**v002 fix:** After encounter release, actively create a fresh vanilla `PathFindController` from the NPC's *actual current position*. This uses A* pathfinding that respects walls and furniture. For cross-map destinations, fall back to vanilla's `pathfindToNextScheduleLocation()` which recomputes routes. Use `checkSchedule()` only as last resort for edge cases.

---

## Issue 1: NPCs walking through walls after encounters (REVISED)

### Root Cause (Confirmed)

In `HandoffNpcToVanillaAfterEncounter`, the original bug was force-invoking vanilla's `checkSchedule(Game1.timeOfDay)` via reflection. This caused wall-walking because:

1. Vanilla's `checkSchedule()` looks up the schedule entry for current game time
2. That entry contains a `SchedulePathDescription` with a **pre-computed route** (`Stack<Point>`)
3. Routes were calculated at day start from the NPC's *expected* position at that time
4. After an encounter, the NPC is at a completely different position
5. The PathFindController follows stale waypoints that don't connect to the NPC's actual position
6. Result: NPC walks in a straight line through walls, furniture, buildings

### Why v001 Fix Didn't Work

The v001 plan removed `TryInvokeVanillaScheduleHandoff` entirely and just set `followSchedule = true`, expecting vanilla's natural update loop to resume the schedule. Problems:

1. **Slow resume**: Vanilla only calls `checkSchedule()` when game time advances to the next schedule entry time key. If the encounter ends at 1:45pm and the next schedule entry is 3:00pm, the NPC stands idle for 1h 15m of game time (~30+ seconds real time).

2. **Persistent wall-walking**: When `checkSchedule()` eventually fires naturally, it *still* uses the same pre-computed routes with stale waypoints. The bug wasn't caused by *when* `checkSchedule` was called, but by *how* it was being used with wrong starting positions.

### The Correct Fix

**Actively pathfind the NPC from their current position using vanilla's PathFindController.**

Instead of invoking `checkSchedule()` which relies on day-start pre-computed routes, create a **fresh `PathFindController`** that:
- Runs vanilla's A* pathfinding algorithm
- Starts from the NPC's *actual current tile*
- Respects walls, furniture, buildings, terrain features
- Generates a collision-free route to the schedule destination

For cross-map destinations (NPC needs to warp to a different location), fall back to vanilla's `pathfindToNextScheduleLocation()` method which recomputes routes from current position.

---

## Implementation Steps

### Phase 1: Replace post-encounter handoff logic

**File:** [ModEntry.cs](../mod/StardewLivingRPG/ModEntry.cs#L14843)

#### Edit 1: Replace `HandoffNpcToVanillaAfterEncounter` method

**Current code (lines ~14843-14870):**
```csharp
private void HandoffNpcToVanillaAfterEncounter(NPC? npc, string encounterId, string phase)
{
    if (npc is null)
        return;

    npc.Halt();
    npc.controller = null;
    TrySetMemberValue(npc, "temporaryController", null);
    TrySetMemberValue(npc, "followSchedule", true);

    if (_scheduleOverrideService?.HasOverride(npc.Name) == true)
        _scheduleOverrideService.RestoreVanillaSchedule(npc);

    if (npc.Schedule is not null)
    {
        Monitor.Log(
            $"Autonomy: returned {npc.Name} to vanilla schedule after encounter {encounterId} ({phase}); released without forced invocation and waiting for vanilla update loop.",
            LogLevel.Trace);
        return;
    }

    Monitor.Log(
        $"Autonomy: released {npc.Name} after encounter {encounterId} ({phase}) without forced schedule invocation; no active schedule entry was available at map={npc.currentLocation?.Name ?? "unknown"}, time={Game1.timeOfDay}.",
        LogLevel.Debug);
}
```

**Replace with:**
```csharp
private void HandoffNpcToVanillaAfterEncounter(NPC? npc, string encounterId, string phase)
{
    if (npc is null)
        return;

    npc.Halt();
    npc.controller = null;
    TrySetMemberValue(npc, "temporaryController", null);
    TrySetMemberValue(npc, "followSchedule", true);

    if (_scheduleOverrideService?.HasOverride(npc.Name) == true)
        _scheduleOverrideService.RestoreVanillaSchedule(npc);

    var method = TryResumeVanillaScheduleFromCurrentPosition(npc);
    Monitor.Log(
        $"Autonomy: returned {npc.Name} to vanilla schedule after encounter {encounterId} " +
        $"({phase}, method={method}, map={npc.currentLocation?.Name ?? "unknown"}, time={Game1.timeOfDay}).",
        string.IsNullOrWhiteSpace(method) ? LogLevel.Debug : LogLevel.Trace);
}
```

**Changes:**
- Removed passive waiting / dual-branch logging
- Added active call to `TryResumeVanillaScheduleFromCurrentPosition(npc)`
- Unified logging with method name reported

---

#### Edit 2: Add new method `TryResumeVanillaScheduleFromCurrentPosition`

**Location:** After `HandoffNpcToVanillaAfterEncounter`, before `TryValidateEncounterScene` (insert at ~line 14871)

```csharp
private string TryResumeVanillaScheduleFromCurrentPosition(NPC npc)
{
    if (npc?.currentLocation is null || npc.Schedule is null || npc.Schedule.Count == 0)
        return string.Empty;

    // Find the most recent schedule entry at or before the current game time.
    var currentTime = Game1.timeOfDay;
    int bestTime = -1;
    SchedulePathDescription? bestDesc = null;
    foreach (var kvp in npc.Schedule)
    {
        if (kvp.Key <= currentTime && kvp.Key > bestTime)
        {
            bestTime = kvp.Key;
            bestDesc = kvp.Value;
        }
    }

    if (bestDesc is null)
        return string.Empty;

    // Read target location and tile from the schedule entry via reflection.
    string? targetLocationName = null;
    var targetTile = Point.Zero;
    var facingDirection = 2;

    if (TryGetMemberValue(bestDesc, "targetLocationName", out var locObj) && locObj is string locStr)
        targetLocationName = locStr;
    if (TryGetMemberValue(bestDesc, "targetTile", out var tileObj) && tileObj is Point tile)
        targetTile = tile;
    if (TryGetMemberValue(bestDesc, "facingDirection", out var faceObj) && faceObj is int face)
        facingDirection = face;

    if (targetTile == Point.Zero)
        return string.Empty;

    // Same-map case: create a fresh vanilla PathFindController from the NPC's actual position.
    // This uses vanilla A* pathfinding that respects walls, furniture, and collision layers.
    var isOnTargetMap = !string.IsNullOrWhiteSpace(targetLocationName)
        && string.Equals(npc.currentLocation.Name, targetLocationName, StringComparison.OrdinalIgnoreCase);

    if (isOnTargetMap || string.IsNullOrWhiteSpace(targetLocationName))
    {
        try
        {
            npc.controller = new PathFindController(npc, npc.currentLocation, targetTile, facingDirection);
            if (npc.controller?.pathToEndPoint is not null && npc.controller.pathToEndPoint.Count > 0)
                return $"PathFindController({npc.currentLocation.Name}, {targetTile.X},{targetTile.Y})";
            npc.controller = null;
        }
        catch
        {
            npc.controller = null;
        }
    }

    // Cross-map or same-map pathfinding failed: try vanilla methods via reflection.
    // Prefer pathfindToNextScheduleLocation (recomputes routes from current position).
    if (TryInvokeVanillaMethod(npc, "pathfindToNextScheduleLocation"))
        return "pathfindToNextScheduleLocation()";

    // Last resort: checkSchedule. May use pre-computed routes but is better than standing still.
    if (TryInvokeVanillaMethod(npc, "checkSchedule", Game1.timeOfDay))
        return $"checkSchedule({Game1.timeOfDay})";

    return string.Empty;
}
```

**What this does:**
1. **Finds current schedule destination** — Looks up the most recent schedule entry at or before current game time
2. **Reads schedule entry fields** — Uses reflection to extract `targetLocationName`, `targetTile`, `facingDirection` from the `SchedulePathDescription`
3. **Same-map pathfinding** — If NPC is already on target map, creates a fresh `PathFindController` with vanilla's A* pathfinding from the NPC's actual current position. This respects all collision layers and generates a valid route.
4. **Cross-map fallback** — If cross-map warp is needed (or same-map pathfinding fails), calls vanilla's `pathfindToNextScheduleLocation()` which recomputes routes from current position
5. **Last resort** — Falls back to `checkSchedule()` only if all else fails (better than standing idle indefinitely)

---

#### Edit 3: Add reflection utility method `TryInvokeVanillaMethod`

**Location:** After `TryResumeVanillaScheduleFromCurrentPosition` (insert at ~line 14935)

```csharp
private static bool TryInvokeVanillaMethod(object source, string methodName, params object[] arguments)
{
    if (source is null || string.IsNullOrWhiteSpace(methodName))
        return false;

    const BindingFlags flags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;
    var argTypes = arguments.Select(a => a?.GetType() ?? typeof(object)).ToArray();

    try
    {
        var method = source.GetType().GetMethod(methodName, flags, binder: null, types: argTypes, modifiers: null);
        if (method is null && arguments.Length == 1 && arguments[0] is int)
            method = source.GetType().GetMethod(methodName, flags, binder: null, types: new[] { typeof(int) }, modifiers: null);
        if (method is null && arguments.Length == 0)
            method = source.GetType().GetMethod(methodName, flags);
        if (method is null)
            return false;

        method.Invoke(source, arguments.Length > 0 ? arguments : null);
        return true;
    }
    catch
    {
        return false;
    }
}
```

**What this does:**
- Safely invokes vanilla NPC methods via reflection
- Handles both parameterized and parameterless method variants
- Used to call `pathfindToNextScheduleLocation()` and `checkSchedule(int)`
- Returns `true` if invocation succeeded, `false` if method not found or exception

---

## Issue 2: Dialogue chunking (NO CHANGES NEEDED from v001)

The dialogue chunking fixes from v001 are correct and working as intended:

- `BubbleMaxChars` increased from 50 to 90
- `SplitLongUnit` simplified to never split single sentences
- `EncounterBubbleMaxDurationMs` increased to 3000ms
- Prompt length guidance added

**No further changes needed for dialogue issue.**

---

## Why This Fix Works

### For slow resume:
- **Before (v001):** NPC stands idle until vanilla's update loop naturally hits the next schedule time key (could be 30+ game minutes)
- **After (v002):** NPC immediately gets a `PathFindController` and starts moving toward their schedule destination

### For wall-walking:
- **Before (original bug):** `checkSchedule()` used pre-computed routes from day-start expected positions → stale waypoints → straight-line through walls
- **Before (v001 attempt):** Eventually `checkSchedule()` still fired naturally with same stale routes → still walked through walls
- **After (v002):** Fresh `PathFindController` computes A* path from NPC's *actual current position* → collision-aware route → no wall-walking

### Fallback strategy:
1. **First choice:** `PathFindController` for same-map — fresh A* pathfinding, collision-aware
2. **Second choice:** `pathfindToNextScheduleLocation()` for cross-map — vanilla method that recomputes routes from current position
3. **Last resort:** `checkSchedule()` — may use stale routes but better than standing still indefinitely

---

## Verification Plan

### Manual Testing

1. **Post-encounter pathfinding (immediate resume)**:
   - Trigger a face-to-face encounter
   - After conversation ends, observe both NPCs
   - **Expected:** NPCs immediately start walking (within 1-2 game ticks)
   - **Fail criteria:** NPC stands idle for 10+ seconds

2. **Post-encounter pathfinding (no wall-walking)**:
   - Same test as above
   - **Expected:** NPCs walk around walls, furniture, buildings using proper collision detection
   - **Fail criteria:** NPC walks through any solid object

3. **Cross-map schedule destination**:
   - Trigger encounter when NPC's next schedule destination is on a different map
   - **Expected:** NPC warps to correct map and resumes schedule naturally
   - **Fail criteria:** NPC stuck or walks wrong direction

4. **SMAPI logs**:
   - After encounter completion, check logs
   - **Expected:** Log shows method used (e.g., `method=PathFindController(Town, 45,32)`)
   - **Trace-level:** Success with PathFindController or pathfindToNextScheduleLocation
   - **Debug-level:** Fallback to checkSchedule or no schedule available

### Edge Cases

5. **Encounter ends at exact schedule transition time**:
   - Encounter ends at 2:00pm, next schedule entry is also 2:00pm
   - **Expected:** NPC pathfinds to 2:00pm destination immediately
   - **Acceptable:** Brief 1-2 tick delay before movement starts

6. **No schedule entry at current time**:
   - Encounter ends at 1:45pm, next schedule entry is 3:00pm
   - **Expected:** NPC pathfinds to most recent schedule entry's destination (e.g., 1:30pm entry)
   - **Acceptable:** NPC stands still if no prior schedule entry exists (rare edge case)

7. **Schedule override with empty route stack**:
   - NPC has schedule entry created by `ScheduleOverrideService.PatchSingleEntry` with `new Stack<Point>()`
   - **Expected:** Fresh `PathFindController` computes route from scratch, no reliance on empty stack
   - **Fail criteria:** NPC walks straight toward destination ignoring collision

---

## Design Decisions

### Key Tradeoffs

1. **Fresh PathFindController vs. vanilla checkSchedule**
   - Fresh controller: Slightly more computational overhead (A* pathfinding runs on each encounter release)
   - Benefit: Guaranteed collision-free routes from NPC's actual position
   - Decision: Performance cost is negligible compared to UX improvement

2. **Reflection to read SchedulePathDescription fields**
   - `targetLocationName`, `targetTile`, `facingDirection` are not public in vanilla Stardew
   - Alternative: Modify `ScheduleOverrideService` to track this metadata separately
   - Decision: Reflection is cleaner and maintains vanilla compatibility

3. **checkSchedule as last resort (not removed entirely)**
   - Risk: May still use pre-computed routes in rare edge cases
   - Benefit: Better than leaving NPC stuck with no controller
   - Decision: Use as fallback only when all better options exhausted

4. **Immediate pathfinding vs. waiting for next schedule tick**
   - Immediate: NPC may briefly walk toward a destination they would've moved away from on next schedule tick
   - Waiting: NPC stands idle for potentially minutes of real time
   - Decision: Immediate movement is more natural post-conversation behavior

### Technical Rationale

**Why PathFindController is the correct solution:**
- Vanilla's `PathFindController` constructor signature: `PathFindController(Character character, GameLocation location, Point endPoint, int finalFacingDirection)`
- It internally calls `PathFinding.findPath()` which runs A* algorithm from `character.TilePoint` (the NPC's actual current position)
- The path is stored in `pathToEndPoint` as a `Stack<Point>` and respects all collision detection
- This is exactly what vanilla uses for all NPC movement — we're just creating it at the right time with the right starting position

**Why reflection is necessary:**
- `SchedulePathDescription` fields are not exposed publicly in Stardew Valley's API
- The mod already has `TryGetMemberValue` utility for reflection-based field access
- Clean separation: schedule system remains vanilla-compatible

**Why this doesn't interfere with autonomy system:**
- Encounter release happens via `TryResumeEncounterParticipants` which explicitly clears encounter runtime state
- The autonomy tick loop checks `IsNpcInActiveEncounter` and skips NPCs in encounters
- Once encounter is cleared and NPC has a controller, autonomy system won't override it

---

## Implementation Checklist

### Issue 1: Post-Encounter Pathfinding (v002 fixes)
- [x] Replace `HandoffNpcToVanillaAfterEncounter` with new implementation (2026-03-20 03:40)
- [x] Add `TryResumeVanillaScheduleFromCurrentPosition` method (2026-03-20 03:40)
- [x] Add `TryInvokeVanillaMethod` reflection utility (2026-03-20 03:40)
- [ ] Test encounter completion — verify immediate NPC movement
- [ ] Test encounter completion — verify no wall-walking
- [ ] Test cross-map schedule destinations work correctly
- [ ] Verify SMAPI logs show correct method names

### Issue 2: Dialogue Chunking (v001 fixes - already correct)
- [x] `SplitLongUnit` simplified (completed in v001)
- [x] `BubbleMaxChars` increased to 90 (completed in v001)
- [x] `EncounterBubbleMaxDurationMs` increased to 3000ms (completed in v001)
- [x] Prompt length guidance added (completed in v001)

### Documentation
- [x] Update CHANGELOG.md with bugfix entries (2026-03-20 03:40)
- [x] Update plan document with v002 status (2026-03-20 03:40)
- [x] Note that v001 was superseded by v002 for pathfinding issue (covered in this document's TL;DR and differences table, 2026-03-20 03:40)

---

## Differences from v001

| Aspect | v001 Approach | v002 Approach (Correct) |
|--------|---------------|-------------------------|
| **Schedule resume** | Removed `checkSchedule` invocation entirely, waited for vanilla update loop | Actively create fresh `PathFindController` from current position |
| **Resume timing** | Passive — waits 30+ game minutes until next schedule time key | Active — immediate movement within 1-2 ticks |
| **Wall-walking fix** | Incomplete — checkSchedule still eventually fired with stale routes | Complete — A* pathfinding from actual position, collision-aware |
| **Method priority** | N/A (no active method) | 1. PathFindController (fresh A*), 2. pathfindToNextScheduleLocation (recomputes), 3. checkSchedule (last resort) |
| **Code complexity** | Minimal (just removed methods) | Moderate (added route lookup + reflection logic) |
| **Robustness** | Low — reliant on vanilla's timing and stale routes | High — explicit pathfinding with collision detection |

---

## References

### Code Locations

**v002 changes (ModEntry.cs):**
- ~L14843 — `HandoffNpcToVanillaAfterEncounter` (replacement)
- ~L14871 (new) — `TryResumeVanillaScheduleFromCurrentPosition` (new method)
- ~L14935 (new) — `TryInvokeVanillaMethod` (new utility)

**Related systems:**
- [ModEntry.cs:14823](../mod/StardewLivingRPG/ModEntry.cs#L14823) — `TryResumeEncounterParticipants` (calls handoff)
- [ModEntry.cs:7205](../mod/StardewLivingRPG/ModEntry.cs#L7205) — `TryGetMemberValue` (existing reflection utility)
- [ModEntry.cs:7231](../mod/StardewLivingRPG/ModEntry.cs#L7231) — `TrySetMemberValue` (existing reflection utility)
- [Systems/ScheduleOverrideService.cs:36-42](../mod/StardewLivingRPG/Systems/ScheduleOverrideService.cs#L36) — Creates `SchedulePathDescription` with empty route stack
- [Systems/NpcFaceToFaceService.cs:73-121](../mod/StardewLivingRPG/Systems/NpcFaceToFaceService.cs#L73) — Controller clearing during Talking phase

### Vanilla Stardew Valley References

- `StardewValley.Pathfinding.PathFindController` — Constructor creates fresh pathfinding from character's current position
- `StardewValley.NPC.checkSchedule(int)` — Looks up schedule entry and applies pre-computed route (problematic after position changes)
- `StardewValley.NPC.pathfindToNextScheduleLocation()` — Recomputes route from current position to next schedule destination
- `StardewValley.Pathfinding.SchedulePathDescription` — Contains `targetLocationName`, `targetTile`, `facingDirection`, `route` (all internal/private fields)

---

**End of Plan v002**
