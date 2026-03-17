using Microsoft.Xna.Framework;

namespace StardewLivingRPG.Systems;

public enum ReservationKind
{
    Transit,
    Idle,
    Chat,
    Duty
}

public sealed class NpcTileReservationService
{
    private sealed class Reservation
    {
        public string NpcId { get; init; } = string.Empty;
        public string LocationId { get; init; } = string.Empty;
        public Point Tile { get; init; }
        public string SpotId { get; init; } = string.Empty;
        public ReservationKind Kind { get; init; } = ReservationKind.Idle;
        public int Spacing { get; init; }
    }

    private readonly Dictionary<string, Reservation> _reservationByNpcId = new(StringComparer.OrdinalIgnoreCase);

    public void Clear()
    {
        _reservationByNpcId.Clear();
    }

    public void Release(string npcId)
    {
        if (string.IsNullOrWhiteSpace(npcId))
            return;

        _reservationByNpcId.Remove(npcId);
    }

    public bool IsReservedByOther(string locationId, Point tile, string npcId, ReservationKind kind = ReservationKind.Idle, int spacing = 0)
    {
        return _reservationByNpcId.Values.Any(reservation =>
            !string.Equals(reservation.NpcId, npcId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(reservation.LocationId, locationId, StringComparison.OrdinalIgnoreCase)
            && IsConflicting(reservation, tile, kind, spacing));
    }

    public bool TryReserve(string npcId, string locationId, Point tile, string spotId, ReservationKind kind = ReservationKind.Idle, int spacing = 0)
    {
        if (string.IsNullOrWhiteSpace(npcId) || string.IsNullOrWhiteSpace(locationId) || tile == Point.Zero)
            return false;

        if (IsReservedByOther(locationId, tile, npcId, kind, spacing))
            return false;

        _reservationByNpcId[npcId] = new Reservation
        {
            NpcId = npcId,
            LocationId = locationId,
            Tile = tile,
            SpotId = spotId,
            Kind = kind,
            Spacing = spacing
        };
        return true;
    }

    public string GetSummary(string locationId)
    {
        return string.Join(", ", _reservationByNpcId.Values
            .Where(reservation => string.Equals(reservation.LocationId, locationId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(reservation => reservation.NpcId, StringComparer.OrdinalIgnoreCase)
            .Select(reservation => $"{reservation.NpcId}@{reservation.SpotId}[{reservation.Tile.X},{reservation.Tile.Y}]/{reservation.Kind}"));
    }

    private static bool IsConflicting(Reservation reservation, Point tile, ReservationKind requestedKind, int requestedSpacing)
    {
        if (reservation.Tile == tile)
            return true;

        if (reservation.Kind == ReservationKind.Transit && requestedKind == ReservationKind.Transit)
            return false;

        var minimumSpacing = Math.Max(Math.Max(reservation.Spacing, requestedSpacing), ResolveDefaultSpacing(reservation.Kind, requestedKind));
        if (minimumSpacing <= 0)
            return false;

        var manhattan = Math.Abs(reservation.Tile.X - tile.X) + Math.Abs(reservation.Tile.Y - tile.Y);
        return manhattan <= minimumSpacing;
    }

    private static int ResolveDefaultSpacing(ReservationKind existing, ReservationKind requested)
    {
        if (existing == ReservationKind.Transit || requested == ReservationKind.Transit)
            return 0;
        if (existing == ReservationKind.Duty && requested == ReservationKind.Duty)
            return 1;
        if (existing == ReservationKind.Chat || requested == ReservationKind.Chat)
            return 2;
        return 2;
    }
}
