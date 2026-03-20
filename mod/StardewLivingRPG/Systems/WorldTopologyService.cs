using Microsoft.Xna.Framework;
using StardewLivingRPG.State;
using StardewValley;

namespace StardewLivingRPG.Systems;

public sealed class WorldTopologyService
{
    private WorldGraph? _cachedGraph;
    private int _cachedDay = -1;

    public WorldGraph BuildGraph(IEnumerable<GameLocation> locations)
    {
        var currentDay = Game1.Date?.TotalDays ?? -1;
        if (_cachedGraph is not null && _cachedDay == currentDay)
            return _cachedGraph;

        var locationList = locations
            .Where(location => location is not null && !string.IsNullOrWhiteSpace(location.Name))
            .ToList();
        var locationByName = new Dictionary<string, GameLocation>(StringComparer.OrdinalIgnoreCase);
        foreach (var location in locationList)
        {
            var locationName = location.Name?.Trim();
            if (!string.IsNullOrWhiteSpace(locationName) && !locationByName.ContainsKey(locationName))
                locationByName[locationName] = location;
        }

        var graph = new WorldGraph();

        foreach (var location in locationList)
        {
            var locId = location.Name.Trim();
            if (graph.Nodes.ContainsKey(locId))
                continue;

            var category = InferCategory(locId, location);
            graph.Nodes[locId] = new TopologyNode
            {
                LocationId = locId,
                Category = category,
                OwnerNpcId = InferPrimaryOwner(locId, location),
                HouseholdNpcIds = ResolveHousehold(locId, location),
                DefaultTile = InferDefaultTile(location),
                IsInterior = !location.IsOutdoors
            };

            if (!graph.AdjacencyBySource.ContainsKey(locId))
                graph.AdjacencyBySource[locId] = new List<TopologyEdge>();

            // Discover warps
            if (location.warps is not null)
            {
                foreach (var warp in location.warps)
                {
                    if (warp is null || string.IsNullOrWhiteSpace(warp.TargetName))
                        continue;

                    graph.AdjacencyBySource[locId].Add(new TopologyEdge
                    {
                        FromLocationId = locId,
                        ToLocationId = warp.TargetName.Trim(),
                        WarpFromTile = new Point(warp.X, warp.Y),
                        WarpToTile = new Point(warp.TargetX, warp.TargetY),
                        EstimatedTravelMinutes = 5,
                        IsWarp = true
                    });
                }
            }

            // Discover doors
            if (location.doors?.Pairs is not null)
            {
                foreach (var door in location.doors.Pairs)
                {
                    if (string.IsNullOrWhiteSpace(door.Value))
                        continue;

                    var targetLocationId = door.Value.Trim();
                    graph.AdjacencyBySource[locId].Add(new TopologyEdge
                    {
                        FromLocationId = locId,
                        ToLocationId = targetLocationId,
                        WarpFromTile = new Point(door.Key.X, door.Key.Y),
                        WarpToTile = ResolveDoorArrivalTile(locationByName, locId, targetLocationId),
                        EstimatedTravelMinutes = 2,
                        IsDoor = true
                    });
                }
            }
        }

        // Ensure every edge target has an adjacency list
        foreach (var edges in graph.AdjacencyBySource.Values.ToArray())
        {
            foreach (var edge in edges)
            {
                if (!graph.AdjacencyBySource.ContainsKey(edge.ToLocationId))
                    graph.AdjacencyBySource[edge.ToLocationId] = new List<TopologyEdge>();
            }
        }

        _cachedGraph = graph;
        _cachedDay = currentDay;
        return graph;
    }

    public List<TopologyEdge>? FindRoute(WorldGraph graph, string fromLocationId, string toLocationId)
    {
        if (string.Equals(fromLocationId, toLocationId, StringComparison.OrdinalIgnoreCase))
            return new List<TopologyEdge>();

        // BFS shortest-hop route
        var queue = new Queue<string>();
        var parentEdge = new Dictionary<string, TopologyEdge?>(StringComparer.OrdinalIgnoreCase);

        queue.Enqueue(fromLocationId);
        parentEdge[fromLocationId] = null;

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!graph.AdjacencyBySource.TryGetValue(current, out var edges))
                continue;

            foreach (var edge in edges)
            {
                if (parentEdge.ContainsKey(edge.ToLocationId))
                    continue;

                parentEdge[edge.ToLocationId] = edge;
                if (string.Equals(edge.ToLocationId, toLocationId, StringComparison.OrdinalIgnoreCase))
                {
                    // Reconstruct path
                    var path = new List<TopologyEdge>();
                    var node = toLocationId;
                    while (parentEdge.TryGetValue(node, out var pe) && pe is not null)
                    {
                        path.Add(pe);
                        node = pe.FromLocationId;
                    }
                    path.Reverse();
                    return path;
                }

                queue.Enqueue(edge.ToLocationId);
            }
        }

        return null; // No route found
    }

    public int EstimateTotalMinutes(List<TopologyEdge>? route)
    {
        if (route is null || route.Count == 0)
            return 0;

        return route.Sum(edge => edge.EstimatedTravelMinutes);
    }

    public string[] ResolveHousehold(string locationId, GameLocation location)
    {
        var residents = new List<string>();
        foreach (var npc in Utility.getAllCharacters())
        {
            if (npc is null || string.IsNullOrWhiteSpace(npc.Name))
                continue;
            if (string.Equals(npc.DefaultMap, locationId, StringComparison.OrdinalIgnoreCase))
                residents.Add(npc.Name);
        }
        return residents.ToArray();
    }

    public void InvalidateCache()
    {
        _cachedGraph = null;
        _cachedDay = -1;
    }

    private static Point ResolveDoorArrivalTile(
        Dictionary<string, GameLocation> locationByName,
        string sourceLocationId,
        string targetLocationId)
    {
        if (!locationByName.TryGetValue(targetLocationId, out var targetLocation)
            || targetLocation.doors?.Pairs is null)
        {
            return Point.Zero;
        }

        foreach (var reverseDoor in targetLocation.doors.Pairs)
        {
            if (string.Equals(reverseDoor.Value?.Trim(), sourceLocationId, StringComparison.OrdinalIgnoreCase))
                return new Point(reverseDoor.Key.X, reverseDoor.Key.Y);
        }

        return Point.Zero;
    }

    private static string InferCategory(string locationName, GameLocation location)
    {
        if (locationName.Contains("House", StringComparison.OrdinalIgnoreCase)
            || locationName.Contains("Cabin", StringComparison.OrdinalIgnoreCase)
            || locationName.Contains("Trailer", StringComparison.OrdinalIgnoreCase)
            || locationName.Contains("FarmHouse", StringComparison.OrdinalIgnoreCase))
        {
            return "private";
        }

        if (locationName.Contains("Shop", StringComparison.OrdinalIgnoreCase)
            || locationName.Contains("Hospital", StringComparison.OrdinalIgnoreCase)
            || locationName.Contains("Saloon", StringComparison.OrdinalIgnoreCase))
        {
            return "semi_private";
        }

        return "public";
    }

    private static string? InferPrimaryOwner(string locationId, GameLocation location)
    {
        foreach (var npc in Utility.getAllCharacters())
        {
            if (npc is not null
                && !string.IsNullOrWhiteSpace(npc.Name)
                && string.Equals(npc.DefaultMap, locationId, StringComparison.OrdinalIgnoreCase))
            {
                return npc.Name;
            }
        }
        return null;
    }

    private static Point InferDefaultTile(GameLocation location)
    {
        if (location.characters?.Count > 0)
        {
            var npc = location.characters[0];
            return new Point((int)npc.Tile.X, (int)npc.Tile.Y);
        }
        return Point.Zero;
    }
}
