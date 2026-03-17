using Microsoft.Xna.Framework;
using StardewLivingRPG.State;
using StardewValley;

namespace StardewLivingRPG.Systems;

public sealed class Player2SpatialPlanSuggestion
{
    public string TargetZoneId { get; set; } = string.Empty;
    public string TargetSpotRole { get; set; } = string.Empty;
    public bool LeaveMapIfCrowded { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int? TargetTileX { get; set; }
    public int? TargetTileY { get; set; }
}

public sealed class NpcMapLayoutBrief
{
    public string LocationId { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public string[] Blockers { get; init; } = Array.Empty<string>();
    public string[] KeepClearZones { get; init; } = Array.Empty<string>();
    public string[] SocialNorms { get; init; } = Array.Empty<string>();
    public Rectangle[] KeepClearAreas { get; init; } = Array.Empty<Rectangle>();
    public Dictionary<string, Rectangle[]> ZoneAreas { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class NpcLocationSpot
{
    public string SpotId { get; init; } = string.Empty;
    public string LocationId { get; init; } = string.Empty;
    public string ZoneId { get; init; } = string.Empty;
    public string SpotRole { get; init; } = string.Empty;
    public Point PreferredTile { get; init; }
    public string[] AllowedNpcIds { get; init; } = Array.Empty<string>();
    public int Priority { get; init; }
}

public sealed class NpcLocationSpotService
{
    private readonly Dictionary<string, NpcMapLayoutBrief> _briefsByLocationId = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<NpcLocationSpot>> _spotsByLocationId = new(StringComparer.OrdinalIgnoreCase);

    public NpcLocationSpotService()
    {
        AddLayout(
            "SeedShop",
            "Shared family home and general store. Front entrance is bottom center. Public shop floor is the lower half. Pierre's counter/work lane stays clear. Caroline and Abigail belong on the family side and upstairs/home side, not in the entrance lane.",
            blockers: new[] { "front counter", "display furniture", "walls", "front entrance choke point", "warp arrival tiles" },
            keepClear: new[] { "entrance_buffer", "counter_clear", "warp_clear" },
            norms: new[] { "Workers stay at the counter.", "Residents can idle deeper in the home side.", "Visitors browse the floor and do not linger in the doorway." },
            keepClearAreas: new[] { Rect(7, 16, 7, 4), Rect(2, 14, 5, 3) },
            zoneAreas: new Dictionary<string, Rectangle[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["counter"] = new[] { Rect(2, 13, 5, 4) },
                ["family_side"] = new[] { Rect(10, 4, 8, 8), Rect(11, 8, 6, 4) },
                ["shop_floor"] = new[] { Rect(5, 9, 9, 7) }
            },
            spots: new[]
            {
                Spot("seedshop_pierre_post", "SeedShop", "counter", "worker_post", new Point(4, 15), allowedNpcIds: new[] { "Pierre" }, priority: 100),
                Spot("seedshop_caroline_home", "SeedShop", "family_side", "home_idle", new Point(13, 6), allowedNpcIds: new[] { "Caroline" }, priority: 95),
                Spot("seedshop_abigail_home", "SeedShop", "family_side", "home_room", new Point(16, 6), allowedNpcIds: new[] { "Abigail" }, priority: 95),
                Spot("seedshop_family_shared", "SeedShop", "family_side", "home_idle", new Point(14, 9), allowedNpcIds: new[] { "Caroline", "Abigail", "Pierre" }, priority: 70),
                Spot("seedshop_browse_a", "SeedShop", "shop_floor", "shop_browse", new Point(8, 12), priority: 60),
                Spot("seedshop_browse_b", "SeedShop", "shop_floor", "visitor_idle", new Point(11, 12), priority: 55),
                Spot("seedshop_chat_a", "SeedShop", "shop_floor", "chat_node", new Point(8, 10), priority: 50),
                Spot("seedshop_chat_b", "SeedShop", "family_side", "chat_node", new Point(13, 10), priority: 45)
            });

        AddLayout(
            "ScienceHouse",
            "Shared carpenter shop and family home. Robin's work lane stays clear. Family members belong deeper in the home side or their own corners, not clustered in the shop entrance.",
            blockers: new[] { "counter", "furniture", "walls", "shop entry lane", "warp arrival tiles" },
            keepClear: new[] { "entrance_buffer", "counter_clear", "warp_clear" },
            norms: new[] { "Robin works at the counter.", "Family members use home-side spots.", "Visitors do not idle in the entry lane." },
            keepClearAreas: new[] { Rect(6, 15, 6, 4), Rect(4, 13, 5, 3) },
            zoneAreas: new Dictionary<string, Rectangle[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["counter"] = new[] { Rect(4, 13, 5, 4) },
                ["family_side"] = new[] { Rect(3, 4, 11, 8), Rect(9, 7, 6, 5) },
                ["shop_floor"] = new[] { Rect(6, 10, 7, 5) }
            },
            spots: new[]
            {
                Spot("science_robin_post", "ScienceHouse", "counter", "worker_post", new Point(6, 14), allowedNpcIds: new[] { "Robin" }, priority: 100),
                Spot("science_demetrius_home", "ScienceHouse", "family_side", "home_idle", new Point(13, 6), allowedNpcIds: new[] { "Demetrius" }, priority: 90),
                Spot("science_maru_home", "ScienceHouse", "family_side", "home_room", new Point(11, 10), allowedNpcIds: new[] { "Maru" }, priority: 90),
                Spot("science_sebastian_home", "ScienceHouse", "family_side", "home_room", new Point(4, 7), allowedNpcIds: new[] { "Sebastian" }, priority: 90),
                Spot("science_family_shared", "ScienceHouse", "family_side", "home_idle", new Point(9, 7), allowedNpcIds: new[] { "Robin", "Demetrius", "Maru", "Sebastian" }, priority: 60),
                Spot("science_family_chat", "ScienceHouse", "family_side", "chat_node", new Point(10, 8), allowedNpcIds: new[] { "Robin", "Demetrius", "Maru", "Sebastian" }, priority: 58),
                Spot("science_visitor", "ScienceHouse", "shop_floor", "visitor_idle", new Point(8, 12), priority: 40)
            });

        AddLayout(
            "Saloon",
            "Public social interior with a service bar. Gus works behind the bar. Tables and walk lanes should stay open. Patrons should spread across the room, not stack in the doorway.",
            blockers: new[] { "bar counter", "tables", "chairs", "walls", "warp arrival tiles" },
            keepClear: new[] { "entrance_buffer", "bar_clear", "warp_clear" },
            norms: new[] { "Gus stays at the bar.", "Patrons occupy table-side or standing social spots.", "Doorway stays clear." },
            keepClearAreas: new[] { Rect(14, 18, 5, 3), Rect(8, 17, 5, 3) },
            zoneAreas: new Dictionary<string, Rectangle[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["bar"] = new[] { Rect(8, 17, 5, 3) },
                ["floor"] = new[] { Rect(13, 11, 8, 8), Rect(10, 13, 10, 6) }
            },
            spots: new[]
            {
                Spot("saloon_gus_post", "Saloon", "bar", "worker_post", new Point(10, 18), allowedNpcIds: new[] { "Gus" }, priority: 100),
                Spot("saloon_table_a", "Saloon", "floor", "visitor_idle", new Point(15, 13), priority: 60),
                Spot("saloon_table_b", "Saloon", "floor", "visitor_idle", new Point(18, 15), priority: 55),
                Spot("saloon_chat_a", "Saloon", "floor", "chat_node", new Point(14, 17), priority: 50),
                Spot("saloon_chat_b", "Saloon", "floor", "chat_node", new Point(19, 18), priority: 45)
            });

        AddLayout(
            "Hospital",
            "Clinic interior with Harvey's front desk and exam space. Counter lane and doorway stay clear. Patients and visitors should wait to the side, not on the entrance.",
            blockers: new[] { "front desk", "beds", "walls", "medical furniture", "warp arrival tiles" },
            keepClear: new[] { "entrance_buffer", "counter_clear", "warp_clear" },
            norms: new[] { "Harvey stays at the clinic post.", "Visitors wait off to the side.", "Doorway stays clear." },
            keepClearAreas: new[] { Rect(5, 14, 5, 4), Rect(10, 12, 4, 3) },
            zoneAreas: new Dictionary<string, Rectangle[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["counter"] = new[] { Rect(10, 12, 4, 3) },
                ["waiting"] = new[] { Rect(5, 9, 5, 5) }
            },
            spots: new[]
            {
                Spot("hospital_harvey_post", "Hospital", "counter", "worker_post", new Point(12, 13), allowedNpcIds: new[] { "Harvey" }, priority: 100),
                Spot("hospital_wait_a", "Hospital", "waiting", "visitor_idle", new Point(6, 12), priority: 60),
                Spot("hospital_wait_b", "Hospital", "waiting", "visitor_idle", new Point(8, 12), priority: 55),
                Spot("hospital_chat", "Hospital", "waiting", "chat_node", new Point(7, 10), priority: 45)
            });

        AddLayout(
            "Blacksmith",
            "Workshop with Clint's counter and forge area. Work lane and entrance stay clear. Visitors wait near the shop floor, not on the doorway.",
            blockers: new[] { "counter", "forge area", "walls", "furniture", "warp arrival tiles" },
            keepClear: new[] { "entrance_buffer", "counter_clear", "warp_clear" },
            norms: new[] { "Clint stays at the work counter.", "Visitors browse or wait away from the entrance." },
            keepClearAreas: new[] { Rect(7, 15, 5, 4), Rect(2, 13, 4, 3) },
            zoneAreas: new Dictionary<string, Rectangle[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["counter"] = new[] { Rect(2, 13, 4, 3) },
                ["floor"] = new[] { Rect(7, 9, 6, 6) }
            },
            spots: new[]
            {
                Spot("blacksmith_clint_post", "Blacksmith", "counter", "worker_post", new Point(3, 14), allowedNpcIds: new[] { "Clint" }, priority: 100),
                Spot("blacksmith_wait_a", "Blacksmith", "floor", "visitor_idle", new Point(8, 13), priority: 60),
                Spot("blacksmith_wait_b", "Blacksmith", "floor", "shop_browse", new Point(10, 12), priority: 55),
                Spot("blacksmith_chat", "Blacksmith", "floor", "chat_node", new Point(9, 10), priority: 45)
            });
    }

    public bool SupportsLocation(string locationId)
    {
        return !string.IsNullOrWhiteSpace(locationId) && _briefsByLocationId.ContainsKey(locationId);
    }

    public bool TryGetLayoutBrief(string locationId, out NpcMapLayoutBrief brief)
    {
        return _briefsByLocationId.TryGetValue(locationId, out brief!);
    }

    public bool IsLongLivedTileAllowed(GameLocation location, Point tile, string zoneId, string spotRole)
    {
        if (location is null || string.IsNullOrWhiteSpace(location.Name))
            return false;
        if (!_briefsByLocationId.TryGetValue(location.Name, out var brief))
            return true;

        if (IsKeepClearTile(brief, tile))
            return false;
        if (spotRole is not "worker_post" && IsNearWarpTile(location, tile, 1))
            return false;

        if (!string.IsNullOrWhiteSpace(zoneId)
            && brief.ZoneAreas.TryGetValue(zoneId, out var zones)
            && zones.Length > 0
            && !zones.Any(zone => zone.Contains(tile)))
        {
            return false;
        }

        return true;
    }

    public Player2SpatialPlanSuggestion BuildLocalFallbackSuggestion(
        NPC npc,
        AutonomyPlanBlock block,
        bool isResident,
        bool isWorker)
    {
        if (block.Type == AutonomyPlanBlockType.RequiredDuty || isWorker)
        {
            return new Player2SpatialPlanSuggestion
            {
                TargetZoneId = "counter",
                TargetSpotRole = "worker_post",
                Reason = "hold the required worker post"
            };
        }

        if (block.Type is AutonomyPlanBlockType.ReturnHome or AutonomyPlanBlockType.Rest)
        {
            return new Player2SpatialPlanSuggestion
            {
                TargetZoneId = isResident ? "family_side" : "waiting",
                TargetSpotRole = isResident ? "home_idle" : "visitor_idle",
                Reason = isResident ? "settle into the home side" : "wait off the main lane"
            };
        }

        if (block.Type == AutonomyPlanBlockType.VisitNpc)
        {
            return new Player2SpatialPlanSuggestion
            {
                TargetZoneId = isResident ? "family_side" : "shop_floor",
                TargetSpotRole = "chat_node",
                Reason = "stand somewhere fit for a face to face visit"
            };
        }

        return new Player2SpatialPlanSuggestion
        {
            TargetZoneId = isResident ? "family_side" : "shop_floor",
            TargetSpotRole = isResident ? "home_idle" : "visitor_idle",
            Reason = isResident ? "idle deeper in the shared home side" : "idle away from the entrance"
        };
    }

    public bool TryResolveConcreteSpot(
        GameLocation location,
        NPC npc,
        AutonomyPlanBlock block,
        Player2SpatialPlanSuggestion suggestion,
        NpcTileReservationService reservations,
        NpcWalkabilityService walkabilityService,
        out NpcLocationSpot spot,
        out Point tile)
    {
        spot = new NpcLocationSpot();
        tile = Point.Zero;

        if (location is null || string.IsNullOrWhiteSpace(location.Name))
            return false;

        if (!_spotsByLocationId.TryGetValue(location.Name, out var spots) || spots.Count == 0)
            return false;

        var preferredRole = string.IsNullOrWhiteSpace(suggestion.TargetSpotRole) ? InferRoleFromBlock(block) : suggestion.TargetSpotRole.Trim().ToLowerInvariant();
        var preferredZone = string.IsNullOrWhiteSpace(suggestion.TargetZoneId) ? string.Empty : suggestion.TargetZoneId.Trim().ToLowerInvariant();
        var npcId = npc.Name ?? string.Empty;
        var reservationKind = ResolveReservationKind(preferredRole, block);

        var compatibleSpots = spots
            .Where(candidate => candidate.AllowedNpcIds.Length == 0 || candidate.AllowedNpcIds.Contains(npcId, StringComparer.OrdinalIgnoreCase))
            .OrderByDescending(candidate => candidate.AllowedNpcIds.Contains(npcId, StringComparer.OrdinalIgnoreCase))
            .ThenByDescending(candidate => string.Equals(candidate.SpotRole, preferredRole, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(candidate => string.Equals(candidate.ZoneId, preferredZone, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(candidate => candidate.Priority)
            .ToList();

        if (suggestion.TargetTileX.HasValue && suggestion.TargetTileY.HasValue)
        {
            var exactTile = new Point(suggestion.TargetTileX.Value, suggestion.TargetTileY.Value);
            var exactSpot = ResolveSpotForExactTile(compatibleSpots, exactTile, preferredZone, preferredRole)
                ?? new NpcLocationSpot
                {
                    SpotId = $"p2_exact:{location.Name}:{preferredZone}:{preferredRole}:{exactTile.X}:{exactTile.Y}",
                    LocationId = location.Name,
                    ZoneId = preferredZone,
                    SpotRole = preferredRole,
                    PreferredTile = exactTile,
                    AllowedNpcIds = Array.Empty<string>(),
                    Priority = 1
                };
            if (exactSpot is not null
                && walkabilityService.IsTileWalkable(location, exactTile, npc)
                && IsLongLivedTileAllowed(location, exactTile, exactSpot.ZoneId, exactSpot.SpotRole)
                && !reservations.IsReservedByOther(location.Name, exactTile, npcId, reservationKind, ResolveMinimumSpacing(exactSpot.SpotRole))
                && reservations.TryReserve(npcId, location.Name, exactTile, exactSpot.SpotId, reservationKind, ResolveMinimumSpacing(exactSpot.SpotRole)))
            {
                spot = exactSpot;
                tile = exactTile;
                return true;
            }

            return false;
        }

        foreach (var candidate in compatibleSpots)
        {
            if (!walkabilityService.TryFindNearestWalkableTile(location, candidate.PreferredTile, 2, npc, out var safeTile))
                continue;
            if (!IsLongLivedTileAllowed(location, safeTile, candidate.ZoneId, candidate.SpotRole))
                continue;
            if (reservations.IsReservedByOther(location.Name, safeTile, npcId, reservationKind, ResolveMinimumSpacing(candidate.SpotRole)))
                continue;
            if (!reservations.TryReserve(npcId, location.Name, safeTile, candidate.SpotId, reservationKind, ResolveMinimumSpacing(candidate.SpotRole)))
                continue;

            spot = candidate;
            tile = safeTile;
            return true;
        }

        return false;
    }

    public string BuildPromptBlock(string locationId)
    {
        if (!TryGetLayoutBrief(locationId, out var brief))
            return string.Empty;

        var spots = _spotsByLocationId.TryGetValue(locationId, out var entries)
            ? string.Join("; ", entries.Select(spot => $"{spot.SpotId}:{spot.ZoneId}:{spot.SpotRole}@({spot.PreferredTile.X},{spot.PreferredTile.Y})"))
            : string.Empty;
        var keepClear = string.Join("; ", brief.KeepClearAreas.Select(area => $"{area.X},{area.Y},{area.Width},{area.Height}"));
        var zones = string.Join("; ", brief.ZoneAreas.Select(zone => $"{zone.Key}={string.Join("|", zone.Value.Select(area => $"{area.X},{area.Y},{area.Width},{area.Height}"))}"));

        return string.Join(" ",
            $"MAP_LAYOUT: {brief.Summary}",
            $"MAP_BLOCKERS: {(brief.Blockers.Length == 0 ? "none" : string.Join(", ", brief.Blockers))}.",
            $"MAP_KEEP_CLEAR: {(brief.KeepClearZones.Length == 0 ? "none" : string.Join(", ", brief.KeepClearZones))}.",
            $"MAP_KEEP_CLEAR_RECTS: {(string.IsNullOrWhiteSpace(keepClear) ? "none" : keepClear)}.",
            $"MAP_ZONES: {(string.IsNullOrWhiteSpace(zones) ? "none" : zones)}.",
            $"MAP_SOCIAL_NORMS: {(brief.SocialNorms.Length == 0 ? "none" : string.Join(" ", brief.SocialNorms))}.",
            $"MAP_SPOTS: {(string.IsNullOrWhiteSpace(spots) ? "none" : spots)}.");
    }

    private static NpcLocationSpot? ResolveSpotForExactTile(
        List<NpcLocationSpot> compatibleSpots,
        Point exactTile,
        string preferredZone,
        string preferredRole)
    {
        var strict = compatibleSpots
            .Where(candidate =>
                (string.IsNullOrWhiteSpace(preferredZone) || string.Equals(candidate.ZoneId, preferredZone, StringComparison.OrdinalIgnoreCase))
                && (string.IsNullOrWhiteSpace(preferredRole) || string.Equals(candidate.SpotRole, preferredRole, StringComparison.OrdinalIgnoreCase))
                && TileDistance(candidate.PreferredTile, exactTile) <= 4)
            .OrderBy(candidate => TileDistance(candidate.PreferredTile, exactTile))
            .ThenByDescending(candidate => candidate.Priority)
            .FirstOrDefault();
        if (strict is not null)
            return strict;

        return compatibleSpots
            .Where(candidate => TileDistance(candidate.PreferredTile, exactTile) <= 4)
            .OrderBy(candidate => TileDistance(candidate.PreferredTile, exactTile))
            .ThenByDescending(candidate => candidate.Priority)
            .FirstOrDefault();
    }

    private static int TileDistance(Point a, Point b)
    {
        return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
    }

    private static bool IsNearWarpTile(GameLocation location, Point tile, int radius)
    {
        return location.warps.Any(warp => Math.Abs(warp.X - tile.X) <= radius && Math.Abs(warp.Y - tile.Y) <= radius);
    }

    private static bool IsKeepClearTile(NpcMapLayoutBrief brief, Point tile)
    {
        return brief.KeepClearAreas.Any(area => area.Contains(tile));
    }

    private static int ResolveMinimumSpacing(string spotRole)
    {
        return spotRole switch
        {
            "chat_node" => 2,
            "worker_post" => 1,
            _ => 2
        };
    }

    private static ReservationKind ResolveReservationKind(string spotRole, AutonomyPlanBlock block)
    {
        if (block.Type == AutonomyPlanBlockType.RequiredDuty || string.Equals(spotRole, "worker_post", StringComparison.OrdinalIgnoreCase))
            return ReservationKind.Duty;
        if (string.Equals(spotRole, "chat_node", StringComparison.OrdinalIgnoreCase))
            return ReservationKind.Chat;
        if (block.Type == AutonomyPlanBlockType.Travel)
            return ReservationKind.Transit;
        return ReservationKind.Idle;
    }

    private void AddLayout(
        string locationId,
        string summary,
        string[] blockers,
        string[] keepClear,
        string[] norms,
        Rectangle[] keepClearAreas,
        Dictionary<string, Rectangle[]> zoneAreas,
        IEnumerable<NpcLocationSpot> spots)
    {
        _briefsByLocationId[locationId] = new NpcMapLayoutBrief
        {
            LocationId = locationId,
            Summary = summary,
            Blockers = blockers,
            KeepClearZones = keepClear,
            SocialNorms = norms,
            KeepClearAreas = keepClearAreas,
            ZoneAreas = zoneAreas
        };
        _spotsByLocationId[locationId] = spots.OrderByDescending(spot => spot.Priority).ToList();
    }

    private static NpcLocationSpot Spot(
        string spotId,
        string locationId,
        string zoneId,
        string spotRole,
        Point preferredTile,
        string[]? allowedNpcIds = null,
        int priority = 0)
    {
        return new NpcLocationSpot
        {
            SpotId = spotId,
            LocationId = locationId,
            ZoneId = zoneId,
            SpotRole = spotRole,
            PreferredTile = preferredTile,
            AllowedNpcIds = allowedNpcIds ?? Array.Empty<string>(),
            Priority = priority
        };
    }

    private static Rectangle Rect(int x, int y, int width, int height)
    {
        return new Rectangle(x, y, width, height);
    }

    private static string InferRoleFromBlock(AutonomyPlanBlock block)
    {
        return block.Type switch
        {
            AutonomyPlanBlockType.RequiredDuty => "worker_post",
            AutonomyPlanBlockType.ReturnHome => "home_idle",
            AutonomyPlanBlockType.Rest => "home_idle",
            AutonomyPlanBlockType.VisitNpc => "chat_node",
            AutonomyPlanBlockType.Socialize => "visitor_idle",
            _ => "visitor_idle"
        };
    }
}
