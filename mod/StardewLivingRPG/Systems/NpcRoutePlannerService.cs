using Microsoft.Xna.Framework;
using StardewLivingRPG.Config;
using StardewLivingRPG.State;

namespace StardewLivingRPG.Systems;

public sealed class NpcRoutePlannerService
{
    private readonly WorldTopologyService _topologyService;
    private readonly DestinationRegistryService _destinationRegistryService;

    public NpcRoutePlannerService(
        WorldTopologyService topologyService,
        DestinationRegistryService destinationRegistryService)
    {
        _topologyService = topologyService;
        _destinationRegistryService = destinationRegistryService;
    }

    public CompiledRoute? PlanRoute(
        WorldGraph graph,
        string npcId,
        string fromLocationId,
        string toLocationId,
        int maxTravelMinutes)
    {
        if (string.Equals(fromLocationId, toLocationId, StringComparison.OrdinalIgnoreCase))
        {
            return new CompiledRoute
            {
                SourceLocationId = fromLocationId,
                DestinationLocationId = toLocationId,
                TotalEstimatedMinutes = 0,
                FinalArrivalTile = graph.Nodes.TryGetValue(toLocationId, out var destNode) ? destNode.DefaultTile : Point.Zero
            };
        }

        var edgePath = _topologyService.FindRoute(graph, fromLocationId, toLocationId);
        if (edgePath is null || edgePath.Count == 0)
            return null;

        var totalMinutes = _topologyService.EstimateTotalMinutes(edgePath);
        if (totalMinutes > maxTravelMinutes)
            return null;

        var segments = new List<RouteSegment>();
        foreach (var edge in edgePath)
        {
            segments.Add(new RouteSegment
            {
                FromLocationId = edge.FromLocationId,
                ToLocationId = edge.ToLocationId,
                DepartureTile = edge.WarpFromTile,
                ArrivalTile = edge.WarpToTile,
                EstimatedMinutes = edge.EstimatedTravelMinutes,
                IsWarp = edge.IsWarp,
                IsDoor = edge.IsDoor
            });
        }

        var finalTile = graph.Nodes.TryGetValue(toLocationId, out var node) ? node.DefaultTile : Point.Zero;
        if (segments.Count > 0)
            finalTile = segments[^1].ArrivalTile;

        return new CompiledRoute
        {
            SourceLocationId = fromLocationId,
            DestinationLocationId = toLocationId,
            Segments = segments,
            TotalEstimatedMinutes = totalMinutes,
            FinalArrivalTile = finalTile
        };
    }

    public void CompileRoutesForPlan(
        WorldGraph graph,
        string npcId,
        NpcDailyPlan plan,
        string startLocation,
        int maxTravelMinutes)
    {
        var currentLocation = startLocation;

        foreach (var block in plan.Blocks)
        {
            if (string.IsNullOrWhiteSpace(block.TargetLocation)
                || string.Equals(block.TargetLocation, currentLocation, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var route = PlanRoute(graph, npcId, currentLocation, block.TargetLocation, maxTravelMinutes);
            if (route is not null)
            {
                block.Route = route;
                block.EstimatedArrivalTime = AddMinutes(block.StartTime, route.TotalEstimatedMinutes);
                currentLocation = block.TargetLocation;
            }
            else
            {
                // Route unreachable — mark for fallback during execution
                block.FailureReason = "path_failed";
            }
        }
    }

    public CompiledRoute? PlanFallbackRoute(
        WorldGraph graph,
        string npcId,
        string currentLocationId)
    {
        // Find the nearest public location as fallback
        foreach (var node in graph.Nodes.Values)
        {
            if (string.Equals(node.Category, "public", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(node.LocationId, currentLocationId, StringComparison.OrdinalIgnoreCase))
            {
                var route = PlanRoute(graph, npcId, currentLocationId, node.LocationId, 30);
                if (route is not null)
                    return route;
            }
        }
        return null;
    }

    private static int AddMinutes(int timeOfDay, int minutes)
    {
        var hours = timeOfDay / 100;
        var mins = timeOfDay % 100;
        mins += minutes;
        hours += mins / 60;
        mins %= 60;
        return (hours * 100) + mins;
    }
}
