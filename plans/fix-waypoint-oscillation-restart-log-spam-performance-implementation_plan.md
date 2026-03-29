# Fix Waypoint Oscillation Restart + Log Spam Performance

## Problem

Three related performance issues remain after the walkability fix:

### 1. Waypoint Oscillation Restart Loop (Primary Lag)
Arthur oscillates between tiles [(34,40)→(35,40)→(36,40)→(34,40)…](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#45-52) because the target tile [(35,39)](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#45-52) is unreachable (likely a collision tile in the Downhill map mod).

**Chain of events:**
1. Hop limit (12) fires → [ClearTemporaryActiveSlotFallback](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#18141-18147) → [ResetActiveSlotFallbackState](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#18304-18311) → resets `SameMapWaypointHopCount = 0`
2. Next tick: [TryRejectInvalidResumeState](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#16342-16388) detects Arthur != target → calls [TryForcePathToActiveScheduleEntry](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#17822-17857)
3. [PrepareActiveSlotFallbackState](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#18312-18335) gives a fresh hop count → new fallback starts → oscillation restarts
4. Each hop creates a new [PathFindController](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#18911-18940) (BFS) → ~2 pathfinds/second indefinitely

### 2. Per-Tick Encounter Status Log Spam
`waiting_on_queued_bubbles_to_finish` and similar states log a TRACE line **every tick** (2x/sec) for 20+ seconds per encounter. With 2-3 concurrent encounters: 4-6 lines/second of identical status.

### 3. CrossMapLeg Progress Logging
[CrossMapLeg(progress)](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#18638-18670) logs a DEBUG line for every tile an NPC crosses during cross-map traversal. With 42-tile paths, this creates 42 sequential DEBUG writes.

---

## Proposed Changes

### ModEntry.cs — Oscillation Restart Prevention

#### [MODIFY] [ModEntry.cs](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs)

**Fix 1: Add `HopLimitExhaustedTarget` field to [PendingVanillaEncounterResume](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#539-614)**

Add a `Point?` field that records the target tile when the hop limit fires. This persists across fallback resets.

```diff
+public Point? HopLimitExhaustedTarget { get; set; }
```

**Fix 2: Set the field when hop limit fires (~line 18031)**

Before [ClearTemporaryActiveSlotFallback](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#18141-18147), record the target:

```diff
+pending.HopLimitExhaustedTarget = pending.ActiveTargetTile;
 ClearTemporaryActiveSlotFallback(npc, pending);
```

**Fix 3: Guard [TryForcePathToActiveScheduleEntry](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#17822-17857) against exhausted targets (~line 17830)**

After [PrepareActiveSlotFallbackState](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#18312-18335), check if the target matches the exhausted target:

```diff
 PrepareActiveSlotFallbackState(pending, preserveLegRetryCount);
+
+if (pending.HopLimitExhaustedTarget.HasValue
+    && pending.ActiveTargetTile.HasValue
+    && pending.HopLimitExhaustedTarget.Value == pending.ActiveTargetTile.Value)
+{
+    return false;
+}
```

**Fix 4: Clear the exhausted target when the active slot rolls over**

When [ActiveTargetTile](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#16779-16786) changes (schedule rollover), clear the guard so the NPC can try the new target:

Search for where [ActiveTargetTile](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#16779-16786) is reassigned during schedule rollover (the `active-slot rolled over` log message) and add:

```diff
+pending.HopLimitExhaustedTarget = null;
```

### ModEntry.cs — Log Throttling

**Fix 5: Throttle [CrossMapLeg(progress)](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#18638-18670) logs**

Change from `Monitor.Log(..., LogLevel.Debug)` to use [LogRuntimeThrottled](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#7802-7807) with a short interval (2 seconds), or downgrade to `LogLevel.Trace`:

```diff
-Monitor.Log(
-    $"Autonomy: [CrossMapLeg(progress)] ...",
-    LogLevel.Debug);
+Monitor.Log(
+    $"Autonomy: [CrossMapLeg(progress)] ...",
+    LogLevel.Trace);
```

**Fix 6: Throttle encounter status polling logs**

The `waiting_on_queued_bubbles_to_finish`, `waiting_on_more_player2_turns`, `waiting_on_first_player2_turn`, and `waiting_on_last_bubble_to_finish` status lines should use [LogRuntimeThrottled](file:///d:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#7802-7807) instead of raw `Monitor.Log`:

Find the encounter tick status logging and add throttling with ~5 second intervals.

---

## Verification Plan

### Build
```bash
dotnet build StardewLivingRPG.csproj
```

### Manual Verification
1. Launch game, trigger encounters involving Downhill-map NPCs (Arthur, Beckett)
2. Confirm Arthur settles or gives up without oscillation after hop limit
3. Monitor SMAPI console — should see dramatically fewer log lines per second
4. Confirm no new NPC getting-stuck regressions
