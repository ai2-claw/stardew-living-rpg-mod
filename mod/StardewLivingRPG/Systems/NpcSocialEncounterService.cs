using StardewLivingRPG.Config;
using StardewLivingRPG.State;
using StardewValley;

namespace StardewLivingRPG.Systems;

public sealed class NpcSocialEncounterService
{
    private readonly PairEmotionService _pairEmotionService;
    private readonly ModConfig _config;

    public NpcSocialEncounterService(PairEmotionService pairEmotionService, ModConfig config)
    {
        _pairEmotionService = pairEmotionService;
        _config = config;
    }

    public float ScoreEncounter(SaveState state, NPC speaker, NPC listener, AutonomyPlanBlock? block)
    {
        if (speaker is null || listener is null)
            return 0f;
        if (speaker.currentLocation is null || listener.currentLocation is null)
            return 0f;
        if (!string.Equals(speaker.currentLocation.Name, listener.currentLocation.Name, StringComparison.OrdinalIgnoreCase))
            return 0f;

        var pair = _pairEmotionService.GetOrCreate(state, speaker.Name, listener.Name);
        var friendship = TryGetAxis(pair, "friendship");
        var trust = TryGetAxis(pair, "trust");
        var jealousy = TryGetAxis(pair, "jealousy");
        var anger = TryGetAxis(pair, "anger");

        var score = 0.15f;
        score += friendship / 250f;
        score += trust / 300f;
        score -= jealousy / 350f;
        score -= anger / 300f;

        if (block?.Type == AutonomyPlanBlockType.VisitNpc)
            score += 0.25f;
        else if (block?.Type == AutonomyPlanBlockType.Socialize)
            score += 0.12f;

        if (speaker.currentLocation.IsOutdoors)
            score += 0.05f;

        return Math.Clamp(score, 0f, 1f);
    }

    public bool ShouldStartEncounter(SaveState state, NPC speaker, NPC listener, AutonomyPlanBlock? block)
    {
        var score = ScoreEncounter(state, speaker, listener, block);
        return score >= _config.AutonomyEncounterScoreThreshold;
    }

    private static int TryGetAxis(NpcPairEmotionEntry pair, string axis)
    {
        return pair.EmotionAxes.TryGetValue(axis, out var value) ? value : 0;
    }
}
