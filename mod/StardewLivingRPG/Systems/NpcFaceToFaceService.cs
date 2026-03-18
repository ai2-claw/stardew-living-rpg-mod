using Microsoft.Xna.Framework;
using StardewValley;
namespace StardewLivingRPG.Systems;

public enum StagingPhase { Idle, Approaching, Positioned, Talking, Releasing }

public sealed class NpcFaceToFaceService
{
    private sealed class StagingState
    {
        public string EncounterId { get; init; } = string.Empty;
        public string NpcAId { get; init; } = string.Empty;
        public string NpcBId { get; init; } = string.Empty;
        public string LocationName { get; init; } = string.Empty;
        public Point TileA { get; init; }
        public Point TileB { get; init; }
        public StagingPhase Phase { get; set; } = StagingPhase.Approaching;
        public int TicksInPhase { get; set; }
    }

    private readonly NpcSpeechBubbleService _bubbleService;
    private readonly NpcWalkabilityService _walkabilityService;
    private readonly Dictionary<string, StagingState> _activeByEncounterId = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _npcToEncounter = new(StringComparer.OrdinalIgnoreCase);

    private const int ApproachTimeoutTicks = 60;

    public NpcFaceToFaceService(NpcSpeechBubbleService bubbleService, NpcWalkabilityService walkabilityService)
    {
        _bubbleService = bubbleService;
        _walkabilityService = walkabilityService;
    }

    public bool TryStage(NPC npcA, NPC npcB, GameLocation location, string encounterId)
    {
        if (npcA.currentLocation is null || npcB.currentLocation is null)
            return false;
        if (!string.Equals(npcA.currentLocation.Name, location.Name, StringComparison.OrdinalIgnoreCase))
            return false;
        if (!string.Equals(npcB.currentLocation.Name, location.Name, StringComparison.OrdinalIgnoreCase))
            return false;
        if (IsInStaging(npcA.Name) || IsInStaging(npcB.Name))
            return false;
        if (TileDistance(npcA.Tile, new Point((int)npcB.Tile.X, (int)npcB.Tile.Y)) > 5f)
            return false;

        var tiles = FindStagingTiles(location, npcA.Tile, npcB.Tile, _walkabilityService);
        if (tiles is null)
            return false;

        var state = new StagingState
        {
            EncounterId = encounterId,
            NpcAId = npcA.Name,
            NpcBId = npcB.Name,
            LocationName = location.Name,
            TileA = tiles.Value.tileA,
            TileB = tiles.Value.tileB,
            Phase = StagingPhase.Talking
        };

        _activeByEncounterId[encounterId] = state;
        _npcToEncounter[npcA.Name] = encounterId;
        _npcToEncounter[npcB.Name] = encounterId;

        return true;
    }

    public void Tick(Func<string, NPC?> resolveNpc)
    {
        foreach (var key in _activeByEncounterId.Keys.ToArray())
        {
            var state = _activeByEncounterId[key];
            state.TicksInPhase++;

            var npcA = resolveNpc(state.NpcAId);
            var npcB = resolveNpc(state.NpcBId);

            if (npcA is null || npcB is null)
            {
                Release(state);
                continue;
            }

            switch (state.Phase)
            {
                case StagingPhase.Approaching:
                    if (!string.Equals(npcA.currentLocation?.Name, state.LocationName, StringComparison.OrdinalIgnoreCase)
                        || !string.Equals(npcB.currentLocation?.Name, state.LocationName, StringComparison.OrdinalIgnoreCase))
                    {
                        Release(state);
                        break;
                    }

                    if (IsAtTile(npcA, state.TileA) && IsAtTile(npcB, state.TileB))
                    {
                        npcA.Halt();
                        npcB.Halt();
                        FaceEachOther(npcA, npcB);
                        state.Phase = StagingPhase.Talking;
                        state.TicksInPhase = 0;
                    }
                    else if (state.TicksInPhase > ApproachTimeoutTicks)
                    {
                        Release(state);
                    }
                    break;

                case StagingPhase.Positioned:
                    state.Phase = StagingPhase.Talking;
                    state.TicksInPhase = 0;
                    break;

                case StagingPhase.Talking:
                    FaceEachOther(npcA, npcB);
                    npcA.Halt();
                    npcB.Halt();
                    if (state.TicksInPhase > 1800)
                    {
                        state.Phase = StagingPhase.Releasing;
                        state.TicksInPhase = 0;
                    }
                    // Talking phase is driven externally by bubble exhaustion
                    // or timeout (30 seconds ≈ 1800 ticks at 60fps)
                    break;

                case StagingPhase.Releasing:
                    Release(state);
                    break;
            }
        }
    }

    public void FinishTalking(string encounterId)
    {
        if (_activeByEncounterId.TryGetValue(encounterId, out var state) && state.Phase == StagingPhase.Talking)
        {
            state.Phase = StagingPhase.Releasing;
            state.TicksInPhase = 0;
        }
    }

    public void CancelEncounter(string encounterId)
    {
        if (_activeByEncounterId.TryGetValue(encounterId, out var state))
            Release(state);
    }

    public void CancelAll(string reason)
    {
        foreach (var state in _activeByEncounterId.Values.ToArray())
            Release(state);
    }

    public bool IsInStaging(string npcId) => _npcToEncounter.ContainsKey(npcId);

    public string? GetActiveEncounterId(string npcId) =>
        _npcToEncounter.TryGetValue(npcId, out var id) ? id : null;

    public StagingPhase GetPhase(string encounterId) =>
        _activeByEncounterId.TryGetValue(encounterId, out var s) ? s.Phase : StagingPhase.Idle;

    public bool IsReadyToTalk(string encounterId) =>
        _activeByEncounterId.TryGetValue(encounterId, out var state) && state.Phase == StagingPhase.Talking;

    private void Release(StagingState state)
    {
        _npcToEncounter.Remove(state.NpcAId);
        _npcToEncounter.Remove(state.NpcBId);
        _activeByEncounterId.Remove(state.EncounterId);
    }

    private static void FaceEachOther(NPC a, NPC b)
    {
        var dx = b.Tile.X - a.Tile.X;
        var dy = b.Tile.Y - a.Tile.Y;

        int dirA, dirB;
        if (Math.Abs(dx) >= Math.Abs(dy))
        {
            dirA = dx > 0 ? 1 : 3; // 1=right, 3=left
            dirB = dx > 0 ? 3 : 1;
        }
        else
        {
            dirA = dy > 0 ? 2 : 0; // 2=down, 0=up
            dirB = dy > 0 ? 0 : 2;
        }

        a.FacingDirection = dirA;
        b.FacingDirection = dirB;
    }

    private static (Point tileA, Point tileB)? FindStagingTiles(GameLocation location, Vector2 npcATile, Vector2 npcBTile, NpcWalkabilityService walkabilityService)
    {
        var currentA = new Point((int)npcATile.X, (int)npcATile.Y);
        var currentB = new Point((int)npcBTile.X, (int)npcBTile.Y);

        if (walkabilityService.IsTileWalkable(location, currentA)
            && walkabilityService.IsTileWalkable(location, currentB)
            && !walkabilityService.IsNearWarpTile(location, currentA, 1)
            && !walkabilityService.IsNearWarpTile(location, currentB, 1)
            && TileDistance(npcATile, currentB) <= 1.5f)
        {
            return (currentA, currentB);
        }

        // Spiral search using IsTileStageable (lenient — ignores NPC/player occupancy)
        return null;
    }

    private static bool IsAtTile(NPC npc, Point tile)
    {
        return Vector2.Distance(npc.Tile, new Vector2(tile.X, tile.Y)) <= 0.75f;
    }

    private static float TileDistance(Vector2 a, Point b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }
}
