# Plan v3: Fix NPC Vanilla Schedule Resume After Encounter

## TL;DR

NPCs remain stuck after encounters because two internal SDV fields block `checkSchedule` from processing loaded schedule entries:

1. **`lastAttemptedSchedule`** — an [int](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/Systems/NpcFaceToFaceService.cs#240-249) that tracks which schedule time keys have already been processed. If `lastAttemptedSchedule >= currentTime`, the vanilla engine skips the entry. After an encounter, this field still holds the value from the *previous* schedule check, so `checkSchedule` thinks it's already processed the current-time entry and does nothing.

2. **`previousEndPoint`** — the tile position where the NPC ended its *last* schedule segment, used as the **origin** for `pathfindToNextScheduleLocation` when computing routes. After an encounter, this still points to wherever the NPC was *before* the encounter began (or its day-start default position). Routes computed from the wrong origin produce zero-length or invalid paths.

The v1/v2 plans addressed schedule loading and relaxed success gates but never reset these two critical internal state fields. The fix: reset `lastAttemptedSchedule` and `previousEndPoint` via reflection before triggering `checkSchedule`, so vanilla treats the loaded schedule as fresh.

## Root Cause (from SDV 1.6 decompiled NPC.cs)

```
// SDV NPC.cs — fields
public int lastAttemptedSchedule = -1;  // reset to -1 in dayUpdate
public Point previousEndPoint;          // reset to defaultPosition in resetForNewDay

// SDV NPC.cs — checkSchedule logic (simplified)
void checkSchedule(int timeOfDay) {
    if (!followSchedule || Schedule == null || ignoreScheduleToday)
        return;
    foreach (var entry in Schedule) {
        if (entry.Key > timeOfDay) break;
        if (entry.Key <= lastAttemptedSchedule) continue;  // SKIP: already handled
        lastAttemptedSchedule = entry.Key;
        // ... pathfind from previousEndPoint to the entry's destination
        // creates DirectionsToNewLocation + controller
    }
}
```

After `TryLoadSchedule()` repopulates [Schedule](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/Systems/ScheduleOverrideService.cs#47-60), `lastAttemptedSchedule` is still at its previous value (e.g., 1100). If the encounter ends at 1130 and the next schedule entry is at 1200, `checkSchedule` skips everything <= 1100 (already processed) and won't fire until 1200. And when it does fire, it computes routes from `previousEndPoint` (wrong origin, NPC is at encounter location).

## Why V1/V2 Failed

| Plan | What it did | Why it didn't work |
|------|-------------|-------------------|
| V1 | Added PathFindController + warp fallback | Same-map PFC works; cross-map warps cause teleporting. Neither plan reset `lastAttemptedSchedule`, so vanilla never picks up the reloaded schedule |
| V2 | Removed warps, returned "ScheduleLoaded" as success | Correct to trust vanilla, but vanilla's `checkSchedule` never fires because `lastAttemptedSchedule` gates it. NPC has schedule loaded + `followSchedule=true` but vanilla skips all entries |

## Proposed Changes

### [ModEntry.cs](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs)

#### Step 1: Reset `lastAttemptedSchedule` and `previousEndPoint` in [TryRebindVanillaScheduleAtCurrentTime](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#15045-15102)

After reloading the schedule (line ~15069), add reflection calls to reset the two gating fields:

```diff
 reloadTodayOk = npc.TryLoadSchedule();
 if (!reloadTodayOk || npc.Schedule is null || npc.Schedule.Count == 0)
     return string.Empty;
 
+// Reset the internal schedule-processing gate so checkSchedule treats
+// all entries as unprocessed. Without this, entries at or before the
+// current time are skipped because lastAttemptedSchedule still holds
+// the value from before the encounter.
+TrySetMemberValue(npc, "lastAttemptedSchedule", -1);
+
+// Update previousEndPoint to the NPC's actual current tile so that
+// pathfindToNextScheduleLocation computes routes from the right origin.
+TrySetMemberValue(npc, "previousEndPoint", npc.TilePoint);
```

#### Step 2: Trigger `checkSchedule` via `currentScheduleDelay`

After resetting the gates, prod vanilla's `checkSchedule` to fire on the next update tick by setting `currentScheduleDelay`:

```diff
+// Prod vanilla to call checkSchedule on the next update tick.
+// currentScheduleDelay > 0 causes NPC.update() to count down and call
+// checkSchedule(Game1.timeOfDay) when it reaches 0.
+TrySetMemberValue(npc, "currentScheduleDelay", 0.001f);
```

This avoids calling `checkSchedule` directly via reflection (fragile, may have side effects from wrong call context). Instead, it uses SDV's existing trigger mechanism.

#### Step 3: Remove direct `PathFindController` attempt and schedule cloning

The entire [TryCloneScheduleWithCurrentEntry](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#15103-15153) + [TryPathfindToScheduleDestination](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#15285-15313) flow is unnecessary — vanilla's `checkSchedule` does this correctly once the gates are reset. Simplify [TryRebindVanillaScheduleAtCurrentTime](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#15045-15102):

```csharp
private string TryRebindVanillaScheduleAtCurrentTime(
    NPC npc,
    out bool reloadTodayOk,
    out bool reloadCurrentTimeOk,
    out bool injectedCurrentEntry,
    out int? mirroredFromTime,
    out string routeCloneType,
    out int routeCloneCount,
    out bool pathfindAttempted,
    out string pathfindMethod)
{
    reloadTodayOk = false;
    reloadCurrentTimeOk = false;
    injectedCurrentEntry = false;
    mirroredFromTime = null;
    routeCloneType = "none";
    routeCloneCount = 0;
    pathfindAttempted = false;
    pathfindMethod = "none";

    npc.controller = null;
    TrySetMemberValue(npc, "temporaryController", null);
    TrySetMemberValue(npc, "followSchedule", true);
    npc.ClearSchedule();
    reloadTodayOk = npc.TryLoadSchedule();
    if (!reloadTodayOk || npc.Schedule is null || npc.Schedule.Count == 0)
        return string.Empty;

    // Reset internal schedule-processing gate
    TrySetMemberValue(npc, "lastAttemptedSchedule", -1);
    // Set origin to current position for route computation
    TrySetMemberValue(npc, "previousEndPoint", npc.TilePoint);
    // Trigger checkSchedule on next update tick
    TrySetMemberValue(npc, "currentScheduleDelay", 0.001f);

    return "ScheduleRebound";
}
```

> [!IMPORTANT]
> This simplification removes the schedule cloning and direct PathFindController code that v1 added. The helper methods [TryCloneScheduleWithCurrentEntry](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#15103-15153), [TryCloneScheduleEntry](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#15154-15187), [CloneScheduleRouteValue](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#15188-15225), [TryGetRouteCloneInfo](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#15226-15243), [TryExtractScheduleDestination](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#15244-15284), [TryPathfindToScheduleDestination](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#15285-15313), [TryReadScheduleLocation](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#15314-15331), and [TryReadScheduleTile](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#15332-15382) become dead code and should be removed.

#### Step 4: Simplify retry logic

Since `checkSchedule` is now triggered properly, the retry loop in [TryProcessPendingVanillaEncounterResumes](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#14962-15044) can be simplified. Attempt 1 does the full rebind. Subsequent attempts just check if the NPC is moving (verifying vanilla picked up the schedule).

No structural change needed here (the existing code already does this on line 15004), but we can reduce `EncounterVanillaResumeMaxAttempts` from 10 to 5.

## Relevant Files

### Modified

#### [MODIFY] [ModEntry.cs](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs)
- Lines 15045-15101: [TryRebindVanillaScheduleAtCurrentTime](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#15045-15102) — simplify to reset `lastAttemptedSchedule`, `previousEndPoint`, and `currentScheduleDelay`, then return `"ScheduleRebound"`
- Remove dead helper methods: [TryCloneScheduleWithCurrentEntry](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#15103-15153), [TryCloneScheduleEntry](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#15154-15187), [CloneScheduleRouteValue](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#15188-15225), [TryGetRouteCloneInfo](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#15226-15243), [TryExtractScheduleDestination](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#15244-15284), [TryPathfindToScheduleDestination](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#15285-15313), [TryReadScheduleLocation](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#15314-15331), [TryReadScheduleTile](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#15332-15382)

### Reference (no changes)

- [NpcFaceToFaceService.cs](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/Systems/NpcFaceToFaceService.cs) — pin/release flow
- [ScheduleOverrideService.cs](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/Systems/ScheduleOverrideService.cs) — schedule backup/restore

## Verification

### Automated

1. `dotnet build` from `mod/StardewLivingRPG/` — must compile clean with no errors

### Manual (in-game)

> [!NOTE]
> There are no unit tests in this project. All verification is manual in-game testing.

2. **Basic resume test:**
   - Launch game via SMAPI, load a save
   - Run `slrpg_demo_bootstrap` to trigger an NPC-to-NPC encounter
   - Wait for encounter to complete (speech bubbles finish)
   - Both NPCs should start walking to their next schedule destination within ~10 game-minutes
   - Check SMAPI console: expect `"returned {npc} to vanilla schedule"` with `method=ScheduleRebound`
   - No `"failed to return"` warnings

3. **No teleporting test:**
   - Same as above — verify NPCs do NOT teleport/warp to their destination
   - They should walk naturally, following their vanilla route

4. **Cross-map resume test:**
   - If encounter happens outside an NPC's next schedule location, NPC should walk to map edge and transition naturally through door/warp points

5. **Edge case — late-day encounter:**
   - Trigger encounter after the NPC's last schedule entry (e.g., after 2200)
   - NPC should still resume some behavior (standing/idle or walking home) rather than staying frozen

## Decisions

- **Reset `lastAttemptedSchedule` to -1** — forces vanilla to re-evaluate all schedule entries from scratch. This is safe because any entries before the current time will be processed immediately (NPC walks to the right place).
- **Set `previousEndPoint` to current tile** — ensures route computation starts from the NPC's actual position, not the day-start default.
- **Use `currentScheduleDelay` trigger** — avoids calling private `checkSchedule` via reflection. Uses the existing vanilla trigger mechanism (countdown timer in `NPC.update()`).
- **Remove schedule cloning** — the `TryCloneScheduleWithCurrentEntry` approach is a workaround that's no longer needed when `checkSchedule` processes entries correctly.
- **Keep `followSchedule=true`** — still needed to tell vanilla to process the schedule.
- **Scope:** Only modifying the encounter-resume flow. Not touching encounter staging, face-to-face pinning, or schedule override paths.
