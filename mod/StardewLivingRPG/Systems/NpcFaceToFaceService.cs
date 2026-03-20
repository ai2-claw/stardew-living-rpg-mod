using Microsoft.Xna.Framework;
using StardewLivingRPG.Config;
using StardewValley;

namespace StardewLivingRPG.Systems;

public enum StagingPhase { Idle, Talking, Releasing }

public sealed class NpcFaceToFaceService
{
    private enum SceneState { Active, Released }

    private sealed class StagingState
    {
        public string EncounterId { get; init; } = string.Empty;
        public string NpcAId { get; init; } = string.Empty;
        public string NpcBId { get; init; } = string.Empty;
        public string LocationName { get; init; } = string.Empty;
        public PinnedNpcState NpcAState { get; init; } = new();
        public PinnedNpcState NpcBState { get; init; } = new();
        public SceneState Scene { get; set; } = SceneState.Active;
        public StagingPhase Phase { get; set; } = StagingPhase.Talking;
    }

    private sealed class PinnedNpcState
    {
        public string NpcId { get; init; } = string.Empty;
        public bool FollowSchedule { get; init; } = true;
        public int FacingDirection { get; init; } = Game1.down;
    }

    private readonly ModConfig _config;
    private readonly NpcWalkabilityService _walkabilityService;
    private readonly Dictionary<string, StagingState> _statesByEncounterId = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _npcToEncounter = new(StringComparer.OrdinalIgnoreCase);

    public NpcFaceToFaceService(NpcWalkabilityService walkabilityService, ModConfig config)
    {
        _walkabilityService = walkabilityService;
        _config = config;
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
        if (TileDistance(npcA.Tile, new Point((int)npcB.Tile.X, (int)npcB.Tile.Y)) > Math.Max(1, _config.AutonomyFaceToFaceDistanceTiles))
            return false;
        if (!_walkabilityService.HasLineOfSight(
                location,
                new Point((int)npcA.Tile.X, (int)npcA.Tile.Y),
                new Point((int)npcB.Tile.X, (int)npcB.Tile.Y)))
        {
            return false;
        }

        var state = new StagingState
        {
            EncounterId = encounterId,
            NpcAId = npcA.Name,
            NpcBId = npcB.Name,
            LocationName = location.Name,
            NpcAState = CapturePinnedNpcState(npcA),
            NpcBState = CapturePinnedNpcState(npcB),
            Scene = SceneState.Active,
            Phase = StagingPhase.Talking
        };

        _statesByEncounterId[encounterId] = state;
        _npcToEncounter[npcA.Name] = encounterId;
        _npcToEncounter[npcB.Name] = encounterId;

        ApplyEncounterPin(npcA);
        ApplyEncounterPin(npcB);
        FaceEachOther(npcA, npcB);
        return true;
    }

    public void Tick(Func<string, NPC?> resolveNpc)
    {
        foreach (var key in _statesByEncounterId.Keys.ToArray())
        {
            var state = _statesByEncounterId[key];
            if (state.Scene != SceneState.Active)
                continue;

            var npcA = resolveNpc(state.NpcAId);
            var npcB = resolveNpc(state.NpcBId);
            if (npcA is null || npcB is null)
            {
                ReleaseScene(state);
                continue;
            }

            if (!string.Equals(npcA.currentLocation?.Name, state.LocationName, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(npcB.currentLocation?.Name, state.LocationName, StringComparison.OrdinalIgnoreCase))
            {
                ReleaseScene(state);
                continue;
            }

            if (state.Phase == StagingPhase.Talking)
            {
                MaintainEncounterPin(npcA);
                MaintainEncounterPin(npcB);
                FaceEachOther(npcA, npcB);
                continue;
            }

            if (state.Phase == StagingPhase.Releasing)
                ReleaseScene(state);
        }
    }

    public void FinishTalking(string encounterId)
    {
        if (_statesByEncounterId.TryGetValue(encounterId, out var state))
            ReleaseScene(state);
    }

    public void CancelEncounter(string encounterId)
    {
        if (_statesByEncounterId.TryGetValue(encounterId, out var state))
            ReleaseScene(state);
    }

    public bool ReleaseEncounter(string encounterId)
    {
        if (!_statesByEncounterId.TryGetValue(encounterId, out var state))
            return false;

        RestorePinnedNpcState(state.NpcAState);
        RestorePinnedNpcState(state.NpcBState);
        ReleaseScene(state);
        DiscardState(state);
        return true;
    }

    public void CancelAll(string reason)
    {
        foreach (var state in _statesByEncounterId.Values.ToArray())
        {
            RestorePinnedNpcState(state.NpcAState);
            RestorePinnedNpcState(state.NpcBState);
            ReleaseScene(state);
            DiscardState(state);
        }
    }

    public bool IsInStaging(string npcId) => _npcToEncounter.ContainsKey(npcId);

    public string? GetActiveEncounterId(string npcId) =>
        _npcToEncounter.TryGetValue(npcId, out var id) ? id : null;

    public StagingPhase GetPhase(string encounterId) =>
        _statesByEncounterId.TryGetValue(encounterId, out var state) && state.Scene == SceneState.Active
            ? state.Phase
            : StagingPhase.Idle;

    public bool IsReadyToTalk(string encounterId) =>
        _statesByEncounterId.TryGetValue(encounterId, out var state)
        && state.Scene == SceneState.Active
        && state.Phase == StagingPhase.Talking;

    private void ReleaseScene(StagingState state)
    {
        if (state.Scene == SceneState.Released)
            return;

        state.Scene = SceneState.Released;
        state.Phase = StagingPhase.Releasing;
        _npcToEncounter.Remove(state.NpcAId);
        _npcToEncounter.Remove(state.NpcBId);
    }

    private void DiscardState(StagingState state)
    {
        _npcToEncounter.Remove(state.NpcAId);
        _npcToEncounter.Remove(state.NpcBId);
        _statesByEncounterId.Remove(state.EncounterId);
    }

    private static void FaceEachOther(NPC a, NPC b)
    {
        var dx = b.Tile.X - a.Tile.X;
        var dy = b.Tile.Y - a.Tile.Y;

        int dirA;
        int dirB;
        if (Math.Abs(dx) >= Math.Abs(dy))
        {
            dirA = dx > 0 ? 1 : 3;
            dirB = dx > 0 ? 3 : 1;
        }
        else
        {
            dirA = dy > 0 ? 2 : 0;
            dirB = dy > 0 ? 0 : 2;
        }

        a.faceDirection(dirA);
        b.faceDirection(dirB);
    }

    private static int TileDistance(Vector2 tileA, Point tileB)
    {
        var dx = Math.Abs((int)tileA.X - tileB.X);
        var dy = Math.Abs((int)tileA.Y - tileB.Y);
        return dx + dy;
    }

    private static PinnedNpcState CapturePinnedNpcState(NPC npc)
    {
        var followSchedule = true;
        if (TryGetMemberValue(npc, "followSchedule", out var followScheduleValue) && followScheduleValue is bool savedFollowSchedule)
            followSchedule = savedFollowSchedule;

        return new PinnedNpcState
        {
            NpcId = npc.Name,
            FollowSchedule = followSchedule,
            FacingDirection = npc.FacingDirection
        };
    }

    private static void ApplyEncounterPin(NPC npc)
    {
        TrySetMemberValue(npc, "followSchedule", false);
        npc.controller = null;
        TrySetTemporaryController(npc, null);
        npc.Halt();
    }

    private static void MaintainEncounterPin(NPC npc)
    {
        TrySetMemberValue(npc, "followSchedule", false);
        if (npc.controller is not null)
            npc.controller = null;
        if (HasTemporaryController(npc))
            TrySetTemporaryController(npc, null);
        npc.Halt();
    }

    private static void RestorePinnedNpcState(PinnedNpcState state)
    {
        if (string.IsNullOrWhiteSpace(state.NpcId))
            return;

        if (Game1.getCharacterFromName(state.NpcId) is not NPC npc)
            return;

        TrySetMemberValue(npc, "followSchedule", state.FollowSchedule);
        npc.faceDirection(state.FacingDirection);
    }

    private static bool HasTemporaryController(NPC npc)
    {
        return TryGetMemberValue(npc, "temporaryController", out var value) && value is not null;
    }

    private static void TrySetTemporaryController(NPC npc, object? value)
    {
        TrySetMemberValue(npc, "temporaryController", value);
    }

    private static bool TryGetMemberValue(object source, string memberName, out object? value)
    {
        value = null;
        if (source is null || string.IsNullOrWhiteSpace(memberName))
            return false;

        const System.Reflection.BindingFlags flags =
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.IgnoreCase;

        var type = source.GetType();
        var property = type.GetProperty(memberName, flags);
        if (property is not null)
        {
            value = property.GetValue(source);
            return true;
        }

        var field = type.GetField(memberName, flags);
        if (field is null)
            return false;

        value = field.GetValue(source);
        return true;
    }

    private static bool TrySetMemberValue(object source, string memberName, object? value)
    {
        if (source is null || string.IsNullOrWhiteSpace(memberName))
            return false;

        const System.Reflection.BindingFlags flags =
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.IgnoreCase;

        try
        {
            var type = source.GetType();
            var property = type.GetProperty(memberName, flags);
            if (property is not null && property.CanWrite)
            {
                property.SetValue(source, value);
                return true;
            }

            var field = type.GetField(memberName, flags);
            if (field is not null)
            {
                field.SetValue(source, value);
                return true;
            }
        }
        catch
        {
        }

        return false;
    }
}
