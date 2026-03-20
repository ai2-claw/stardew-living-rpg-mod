# Fix Cross-Map Encounter Resume - Door Arrival Tile (v15)

## Problem Summary

v14's topology-guided cross-map legs work - Shane walks to the JojaMart door - but he's stuck there because:

**Evidence from logs:**
```
[CrossMapLeg(start)] Shane from=Town to=JojaMart departure_tile=(95,50) arrival_tile=(0,0)
[CrossMapLeg(progress)] Shane map=Town tile=(95,50)  ← Reached the door!
```

But `arrival_tile=(0,0)` is **invalid**, so the handoff logic can't trigger the warp to JojaMart.

## Root Cause

In `Systems/WorldTopologyService.cs` lines 64-81, when discovering door edges:

```csharp
if (location.doors?.Pairs is not null)
{
    foreach (var door in location.doors.Pairs)
    {
        graph.AdjacencyBySource[locId].Add(new TopologyEdge
        {
            FromLocationId = locId,
            ToLocationId = door.Value.Trim(),
            WarpFromTile = new Point(door.Key.X, door.Key.Y),
            WarpToTile = Point.Zero,  // ← BUG: Always (0,0) for doors
            EstimatedTravelMinutes = 2,
            IsDoor = true
        });
    }
}
```

**Why `WarpToTile = Point.Zero`:**
- `location.doors` is `Dictionary<Vector2, string>` (door position → target location name)
- It doesn't contain the target tile position
- For warps, the `Warp` object has both `TargetX` and `TargetY`, so we can set `WarpToTile` correctly
- For doors, we need to look up the **reverse door** in the target location

## Solution: Resolve Door Arrival Tiles Bidirectionally

When creating a door edge from Location A → Location B:
1. Find Location B's `doors` dictionary
2. Look for a door entry that points back to Location A
3. Use that door's position as the `WarpToTile`

Doors are typically bidirectional - if Town has a door at (95,50) leading to "JojaMart", then JojaMart should have a door leading back to "Town" at some position.

## Implementation

### File: `Systems/WorldTopologyService.cs`

#### Modify door edge discovery (around lines 64-81):

**BEFORE:**
```csharp
if (location.doors?.Pairs is not null)
{
    foreach (var door in location.doors.Pairs)
    {
        if (string.IsNullOrWhiteSpace(door.Value))
            continue;

        graph.AdjacencyBySource[locId].Add(new TopologyEdge
        {
            FromLocationId = locId,
            ToLocationId = door.Value.Trim(),
            WarpFromTile = new Point(door.Key.X, door.Key.Y),
            WarpToTile = Point.Zero,
            EstimatedTravelMinutes = 2,
            IsDoor = true
        });
    }
}
```

**AFTER:**
```csharp
if (location.doors?.Pairs is not null)
{
    foreach (var door in location.doors.Pairs)
    {
        if (string.IsNullOrWhiteSpace(door.Value))
            continue;

        var targetLocationId = door.Value.Trim();
        Point arrivalTile = Point.Zero;

        // Try to resolve the arrival tile by finding the reverse door in the target location
        var targetLocation = locations.FirstOrDefault(l => l.Name?.Equals(targetLocationId, StringComparison.OrdinalIgnoreCase) == true);
        if (targetLocation?.doors?.Pairs is not null)
        {
            foreach (var reverseDoor in targetLocation.doors.Pairs)
            {
                if (reverseDoor.Value?.Equals(locId, StringComparison.OrdinalIgnoreCase) == true)
                {
                    arrivalTile = new Point(reverseDoor.Key.X, reverseDoor.Key.Y);
                    break;
                }
            }
        }

        graph.AdjacencyBySource[locId].Add(new TopologyEdge
        {
            FromLocationId = locId,
            ToLocationId = targetLocationId,
            WarpFromTile = new Point(door.Key.X, door.Key.Y),
            WarpToTile = arrivalTile,  // Now resolved from reverse door
            EstimatedTravelMinutes = 2,
            IsDoor = true
        });
    }
}
```

## Files to Modify

| File | Lines | Change |
|------|-------|--------|
| `Systems/WorldTopologyService.cs` | 64-81 | Resolve door arrival tiles by looking up reverse door |

## Verification

1. **Build:** `dotnet build` must pass

2. **Manual test:**
   - Trigger Alex->Shane encounter in Town (time ~920)
   - Wait for encounter to complete
   - **Expected logs:**
     ```
     [CrossMapLeg(start)] Shane from=Town to=JojaMart departure_tile=(95,50) arrival_tile=(X,Y)
     ```
     Where `(X,Y)` is a valid tile in JojaMart (NOT `0,0`)
   - **Expected behavior:**
     - Shane walks to the JojaMart door at `(95,50)` in Town
     - When he reaches the door, the pending worker detects he's at the departure tile
     - The system clears the current leg controller
     - Shane warps to JojaMart at the arrival tile
     - A new same-map leg is created inside JojaMart to the final target `(9,17)`

3. **Regression check:**
   - Same-map fallback cases should still work
   - Warps (non-door edges) should still work with existing `WarpToTile` logic

4. **Edge cases:**
   - If reverse door is not found (one-way door), `arrivalTile` stays `Point.Zero` - current behavior
   - Log a warning when reverse door lookup fails for debugging

## Why This Will Work

1. **Doors are bidirectional in Stardew Valley:**
   - If Town has a door to JojaMart, JojaMart has a door back to Town
   - The reverse door's position is exactly where the NPC appears after warping

2. **Arrival tile is required for handoff:**
   - The pending worker needs to know when the NPC has reached the destination map
   - With a valid `arrivalTile`, we can detect when `npc.currentLocation.Name == targetMap` and `npc.TilePoint` is near `arrivalTile`

3. **Topology service already iterates all locations:**
   - We can look up the target location by name
   - We can search its `doors` dictionary for the reverse edge
   - This adds minimal overhead to the existing topology build process

## Key Design Decisions

- **Graceful degradation:** If reverse door is not found, fall back to `Point.Zero` (current behavior)
- **No new data structures:** Reuse existing `doors` dictionary from `GameLocation`
- **Minimal changes:** Only modify door edge discovery, warp logic stays the same
- **Debuggable:** Consider adding a log when reverse door lookup fails (optional)
