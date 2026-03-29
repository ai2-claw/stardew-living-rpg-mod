# Fix NPC Action-Slot Sprite False-Positive Detection

After an encounter ends and the NPC returns to their schedule tile, the mod confirms the action-slot restore succeeded using `stable_animation`. The logs report `action_confirmed=True`, but the NPC still shows a generic standing sprite instead of their intended schedule animation (yoga, football, cleaning, etc.).

## Root Cause

Two cooperating bugs produce a false-positive:

### 1. [TryDetectVisibleScheduleActionState](file:///D:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#16871-16894) accepts ANY animation as success

[TryDetectVisibleScheduleActionState](file:///D:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#L16871-L16893) checks [TryCaptureActionStateSignature](file:///D:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#16913-16957) → [TryDescribeActiveSpriteAnimation](file:///D:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#16985-17029). If `currentSingleAnimation` or `currentAnimation` on the NPC sprite is a non-empty collection, `hasActiveAnimation = true` and the method returns `stable_animation`.

**The problem**: [PathFindController](file:///D:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#18998-19027) setup briefly populates the sprite's animation list with walking frames even when the route is a zero-length or single-tile path. This generic walking animation is NOT the intended schedule behavior animation, but the detector doesn't distinguish between the two.

### 2. `checkSchedule(int)` with a terminal-route clone doesn't fire end-of-route behavior

[TryRebindActiveSlotArrivalAction](file:///D:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#L16654-L16710) clones the schedule entry with a single-tile route to the NPC's current position, then calls `checkSchedule(int)`. The game creates a [PathFindController](file:///D:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#18998-19027) whose route is already exhausted. The controller's `endBehaviorFunction` stores the intended action (e.g., `Olivia_Yoga`), but since the NPC is already at the destination, the controller never fires `pathDone()` → `endBehavior()`.

The intended schedule action is never applied. The NPC stands at the right tile, facing the right direction, with a [PathFindController](file:///D:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#18998-19027) that will never complete.

### Evidence from logs

Every affected NPC shows the same pattern in the same game tick:

```
action_state_signature=none    ← still no action
action_confirmed=True, action_confirm_method=stable_animation    ← false positive
controller=PathFindController  ← still has path controller at settle time
```

Confirmed for: Olivia (`Olivia_Yoga`), Alex (`alex_football`), Gus (`gus_clean`), Andy (`Andy_Drink2`), Chloe (`chloe_fix_truck`).

## Proposed Changes

### NPC Action-Slot Detection & Application

#### [MODIFY] [ModEntry.cs](file:///D:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs)

**Change 1: Disqualify controller-driven animation from `stable_animation` confirmation**

In [TryDetectVisibleScheduleActionState](file:///D:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#16871-16894) (line ~16871), add a guard that rejects `hasActiveAnimation` when the NPC still has a [PathFindController](file:///D:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#18998-19027) (i.e., `npc.controller is not null`). A genuine schedule action animation should persist *after* the controller is gone, not while the controller is still attached.

```diff
 if (hasActiveAnimation)
 {
+    // Reject if a PathFindController is still attached — the animation is likely
+    // from controller movement setup, not from the intended schedule action.
+    if (npc.controller is not null)
+        return false;
+
     method = "stable_animation";
     return true;
 }
```

**Change 2: Force-fire end-of-route behavior when NPC is already at target after `checkSchedule(int)`**

In [TryRebindActiveSlotArrivalAction](file:///D:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#16654-16711) (line ~16687), after `checkSchedule` is invoked and a [PathFindController](file:///D:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#18998-19027) is created, detect the degenerate case (NPC already at the controller's destination, route exhausted) and directly invoke the end-of-route behavior from the controller.

After the `checkSchedule` call, add:

```csharp
if (checkScheduleInvoked && npc.controller is PathFindController pfc)
{
    // If the controller's route is empty/exhausted (NPC is already at destination),
    // the end-of-route behavior won't fire naturally. Extract and invoke it directly.
    if (TryExtractAndFireControllerEndBehavior(npc, pfc, pending))
    {
        arrivalRebindMethod = $"full_entry_clone+{methodLabel}+direct_end_route";
        arrivalRebindDegraded = degradedClone;
        return true;
    }
}
```

**Change 3: New helper method `TryExtractAndFireControllerEndBehavior`**

This method inspects the [PathFindController](file:///D:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#18998-19027) for an exhausted/trivial route and an `endBehaviorFunction` delegate. If found, it nulls the controller and invokes the delegate to apply the schedule action.

```csharp
private bool TryExtractAndFireControllerEndBehavior(
    NPC npc,
    PathFindController controller,
    PendingVanillaEncounterResume pending)
{
    // Only fire if the controller route is trivially complete
    if (!IsControllerRouteExhausted(controller, npc.TilePoint))
        return false;

    // Extract the endBehaviorFunction delegate
    if (!TryGetMemberValue(controller, "endBehaviorFunction", out var endBehaviorFunc)
        || endBehaviorFunc is null)
        return false;

    // Clear the controller before firing so the NPC is stationary
    npc.controller = null;
    npc.Halt();

    // Apply facing
    if (pending.ActiveFacingDirection.HasValue)
        npc.faceDirection(pending.ActiveFacingDirection.Value);

    // Fire the end-of-route behavior
    try
    {
        if (endBehaviorFunc is PathFindController.endBehavior endBehavior)
        {
            endBehavior(npc, npc.currentLocation);
            return true;
        }
        // Try delegate invoke as fallback
        if (endBehaviorFunc is Delegate del)
        {
            del.DynamicInvoke(npc, npc.currentLocation);
            return true;
        }
    }
    catch (Exception ex)
    {
        Monitor.Log(
            $"Autonomy: failed to fire controller end behavior for {npc.Name}: {ex.Message}",
            LogLevel.Trace);
    }

    return false;
}

private static bool IsControllerRouteExhausted(PathFindController controller, Point npcTile)
{
    if (!TryReadControllerPath(controller, out var pathTiles))
        return true; // Can't read path = treat as exhausted

    return pathTiles.All(tile => tile == npcTile);
}
```

**Change 4: Make [HasTerminalSafeArrivalState](file:///D:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#16752-16777) reject non-exhausted controllers**

The current [IsTerminalArrivalControllerSafe](file:///D:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#16804-16814) (line ~16804) only checks if all path tiles equal the target tile. Tighten this: also verify the controller has no pending `endBehaviorFunction` that hasn't been applied. If it has one, the NPC is NOT in a terminal safe state yet — the action hasn't fired.

```diff
 private static bool IsTerminalArrivalControllerSafe(object? controller, Point activeTargetTile)
 {
     if (controller is null)
         return true;

     if (!TryReadControllerPath(controller, out var pathTiles))
         return false;

-    return pathTiles.All(tile => tile == activeTargetTile);
+    if (!pathTiles.All(tile => tile == activeTargetTile))
+        return false;
+
+    // If the controller still has an un-fired end behavior, it's not terminal
+    if (TryGetMemberValue(controller, "endBehaviorFunction", out var endBehavior)
+        && endBehavior is not null)
+        return false;
+
+    return true;
 }
```

> [!IMPORTANT]
> Change 4 is the most impactful: it prevents [IsArrivalRebindSatisfied](file:///D:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#16712-16733) from returning `true` while a controller with a pending end behavior is still attached. This forces the code through the [TryApplyDirectEndOfRouteBehavior](file:///D:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#17185-17214) fallback path (line ~16610), which already exists and correctly invokes the end-of-route behavior. Combined with Change 2 (which fires the behavior proactively), these changes form a belt-and-suspenders fix.

## Verification Plan

### Automated Tests

No automated unit test infrastructure exists in this project. The mod runs inside the SMAPI/Stardew Valley runtime, so verification is done via in-game console commands and log analysis.

### Manual Verification

1. Build the mod:
   ```
   dotnet build
   ```

2. Launch Stardew Valley via SMAPI and load a save.

3. Use the SMAPI console to trigger encounters with NPCs who have schedule behaviors:
   - `slrpg_demo_bootstrap` to set up a test scenario
   - Wait for/trigger encounters with NPCs known to have action behaviors (Olivia, Alex, Gus, etc.)

4. After encounters end, observe:
   - **Expected**: NPC should visibly perform their schedule animation (yoga, football, cleaning)
   - **Expected in logs**: `action_confirm_method` should be `stable_animation` WITHOUT `controller=PathFindController` at settle time, OR the settle method should include `direct_end_route` / `direct_end_behavior`
   - **Not expected**: `controller=PathFindController` at settle time with `action_confirmed=True`

5. Check the SMAPI console log for:
   - No `action slot restore failed` warnings
   - Settle logs should show one of:
     - `arrival_rebind_method=full_entry_clone+checkSchedule(int)+direct_end_route` (Change 2 fired)
     - `arrival_rebind_method=direct_end_behavior:*` (Change 3 fallback fired)
     - `action_confirm_method=stable_animation` with `controller=null` (genuine animation without controller)
