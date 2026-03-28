# Fix NPC Schedule Animation After Encounter Resume

NPCs arrive at their correct schedule tile post-encounter but show an idle standing sprite instead of their intended schedule animation (yoga, football, cleaning, etc.). The false-positive detection is patched — now the animation itself needs to be applied.

## Root Cause

Stardew Valley schedule animations use a **string-based** system, not a delegate:

1. Each `SchedulePathDescription` stores an `endOfRouteBehavior` string (e.g. `"Olivia_Yoga"`, `"alex_football"`, `"gus_clean"`)
2. When an NPC reaches their destination, the game calls **`NPC.loadEndOfRouteBehavior(string name)`**
3. That method reads animation data from **`Data/animationDescriptions`** and applies the looping sprite animation

### Why the current code fails

The current code has two paths that both miss the correct method:

| Path | What it does | Why it fails |
|------|-------------|--------------|
| [TryExtractAndFireControllerEndBehavior](file:///D:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#17212-17256) (line 17212) | Reads `PathFindController.endBehaviorFunction` delegate | That delegate is for **scripted code callbacks** (signature: `bool(Character, GameLocation)`), not string-based schedule animations. Most schedule-driven controllers don't set this delegate at all — the schedule animation is stored as a string, not a delegate. |
| [TryApplyDirectEndOfRouteBehavior](file:///D:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#17274-17303) (line 17274) | Reflection-calls `doEndOfRouteBehavior` / `performTenMinuteUpdate` | (a) Gated behind 3+ failed rebind attempts, so it's only a last resort. (b) It doesn't try `loadEndOfRouteBehavior`, the actual method the game uses. |

### Log evidence

From the pre-patch logs, every "successful" settle showed `controller=PathFindController` — meaning `checkSchedule(int)` created a controller whose `endBehaviorFunction` was null (because the schedule uses string-based behavior, not a delegate). So [TryExtractAndFireControllerEndBehavior](file:///D:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#17212-17256) reads null from the delegate field and returns false. The animation is never applied.

## Proposed Changes

All changes in [ModEntry.cs](file:///D:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs).

---

### Change 1: Call `loadEndOfRouteBehavior` directly after clearing the controller

In [TryRebindActiveSlotArrivalAction](file:///D:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#16654-16729) (line ~16687), after `checkSchedule` is invoked and [TryExtractAndFireControllerEndBehavior](file:///D:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#17212-17256) returns false (as it will for string-based behaviors), add a direct call to `NPC.loadEndOfRouteBehavior(ActiveBehavior)`:

```diff
 if (checkScheduleInvoked
     && npc.controller is PathFindController pathFindController
     && TryExtractAndFireControllerEndBehavior(npc, pathFindController, pending))
 {
     arrivalRebindMethod = $"full_entry_clone+{methodLabel}+direct_end_route";
     arrivalRebindDegraded = degradedClone;
     return true;
 }
+
+if (checkScheduleInvoked
+    && !string.IsNullOrWhiteSpace(pending.ActiveBehavior)
+    && TryApplyLoadEndOfRouteBehavior(npc, pending))
+{
+    arrivalRebindMethod = $"full_entry_clone+{methodLabel}+loadEndOfRoute";
+    arrivalRebindDegraded = degradedClone;
+    return true;
+}
```

Apply the same pattern to the original-entry fallback block (line ~16711).

---

### Change 2: New helper `TryApplyLoadEndOfRouteBehavior`

This method:
1. Nulls the controller (it's exhausted and won't fire).
2. Halts the NPC and applies facing direction.
3. Reflection-calls `NPC.loadEndOfRouteBehavior(string name)` with `pending.ActiveBehavior`.

```csharp
private bool TryApplyLoadEndOfRouteBehavior(NPC npc, PendingVanillaEncounterResume pending)
{
    if (string.IsNullOrWhiteSpace(pending.ActiveBehavior))
        return false;

    // Clear the exhausted controller so the NPC is stationary
    npc.controller = null;
    TrySetMemberValue(npc, "temporaryController", null);
    npc.Halt();
    ClearEncounterMotionState(npc);
    if (pending.ActiveFacingDirection.HasValue)
        npc.faceDirection(pending.ActiveFacingDirection.Value);

    // Try NPC.loadEndOfRouteBehavior(string name) — the vanilla method
    const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    foreach (var methodName in new[] {
        "loadEndOfRouteBehavior", "LoadEndOfRouteBehavior" })
    {
        var method = npc.GetType().GetMethod(methodName, flags,
            null, new[] { typeof(string) }, null);
        if (method is null)
            continue;

        try
        {
            method.Invoke(npc, new object[] { pending.ActiveBehavior });
            Monitor.Log(
                $"Autonomy: applied loadEndOfRouteBehavior for {npc.Name} with behavior={pending.ActiveBehavior}.",
                LogLevel.Debug);
            return true;
        }
        catch (Exception ex)
        {
            Monitor.Log(
                $"Autonomy: loadEndOfRouteBehavior failed for {npc.Name}: {ex.Message}",
                LogLevel.Trace);
        }
    }

    return false;
}
```

---

### Change 3: Add `loadEndOfRouteBehavior` to [TryApplyDirectEndOfRouteBehavior](file:///D:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#17274-17303) fallback list

In the existing [TryApplyDirectEndOfRouteBehavior](file:///D:/talk/Stardew%20Mod/stardew-living-rpg-mod/mod/StardewLivingRPG/ModEntry.cs#17274-17303) method (line 17278), prepend `loadEndOfRouteBehavior` to the method name search list so it's tried first:

```diff
-foreach (var methodName in new[] {
-    "doEndOfRouteBehavior", "DoEndOfRouteBehavior",
-    "performTenMinuteUpdate", "PerformTenMinuteUpdate" })
+foreach (var methodName in new[] {
+    "loadEndOfRouteBehavior", "LoadEndOfRouteBehavior",
+    "doEndOfRouteBehavior", "DoEndOfRouteBehavior",
+    "performTenMinuteUpdate", "PerformTenMinuteUpdate" })
```

This ensures the 3-retry fallback path also uses the correct method if it's reached.

---

## Summary of Flow After Fix

```
NPC arrives at target tile
  → TryRebindActiveSlotArrivalAction
    → checkSchedule(int) creates PathFindController
    → TryExtractAndFireControllerEndBehavior: fails (no endBehaviorFunction delegate)
    → TryApplyLoadEndOfRouteBehavior: NEW — calls loadEndOfRouteBehavior("Olivia_Yoga")
      → clears controller, applies facing, loads animation from Data/animationDescriptions
      → NPC now shows yoga animation ✓
    → returns with method="full_entry_clone+checkSchedule(int)+loadEndOfRoute"
```

> [!IMPORTANT]
> The key insight is that `PathFindController.endBehaviorFunction` (a code delegate) and `SchedulePathDescription.endOfRouteBehavior` (a string that maps to `Data/animationDescriptions`) are two completely different systems. The current code only tries the delegate path. The fix adds the string-based path, which is what 99% of vanilla schedule animations use.

## Verification Plan

### Manual Verification

1. Build: `dotnet build`
2. Launch via SMAPI, load save
3. Wait for/trigger encounters involving NPCs with known schedule behaviors:
   - **Olivia** → `Olivia_Yoga` (yoga pose)
   - **Alex** → `alex_football` (tossing football)
   - **Gus** → `gus_clean` (cleaning)
   - **Chloe** → `chloe_fix_truck` (fixing truck)
4. After encounter ends and NPC walks back to slot:
   - **Expected**: NPC performs their animation (not idle standing)
   - **Expected in logs**: `arrival_rebind_method` contains `loadEndOfRoute`
   - **Expected in logs**: `controller=null` at settle time
5. Verify no regressions for NPCs without behaviors (plain slots should still work with `stationary_plain_slot`)
