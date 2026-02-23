using StardewModdingAPI;
using StardewLivingRPG.CustomNpcFramework.Models;
using StardewLivingRPG.CustomNpcFramework.Utilities;

namespace StardewLivingRPG.CustomNpcFramework.Services;

internal sealed class CanonBaselineService
{
    private readonly IModHelper _helper;
    private readonly IMonitor _monitor;

    public CanonBaselineFile Baseline { get; private set; } = new();
    public HashSet<string> CanonNpcTokens { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> CanonLocationTokens { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> TimelineAnchors { get; } = new(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyList<string> ForbiddenClaimPatterns => Baseline.ForbiddenClaimPatterns;

    public CanonBaselineService(IModHelper helper, IMonitor monitor)
    {
        _helper = helper;
        _monitor = monitor;
    }

    public void Load()
    {
        var loaded = _helper.Data.ReadJsonFile<CanonBaselineFile>("assets/tlv-custom-npc-canon-baseline.json");
        if (loaded is null)
        {
            _monitor.Log("Could not load assets/tlv-custom-npc-canon-baseline.json. Falling back to empty canon baseline.", LogLevel.Warn);
            loaded = new CanonBaselineFile();
        }

        Baseline = loaded;
        CanonNpcTokens.Clear();
        CanonLocationTokens.Clear();
        TimelineAnchors.Clear();

        foreach (var name in Baseline.CanonicalNpcNames)
        {
            var token = TextTokenUtility.NormalizeToken(name);
            if (!string.IsNullOrWhiteSpace(token))
                CanonNpcTokens.Add(token);
        }

        foreach (var location in Baseline.CanonicalLocationTokens)
        {
            var token = TextTokenUtility.NormalizeToken(location);
            if (!string.IsNullOrWhiteSpace(token))
                CanonLocationTokens.Add(token);
        }

        foreach (var anchor in Baseline.AllowedTimelineAnchors)
        {
            var token = TextTokenUtility.NormalizeToken(anchor);
            if (!string.IsNullOrWhiteSpace(token))
                TimelineAnchors.Add(token);
        }

        _monitor.Log(
            $"Loaded TLV canon baseline v{Baseline.Version}: npcs={CanonNpcTokens.Count}, locations={CanonLocationTokens.Count}, timeline={TimelineAnchors.Count}.",
            LogLevel.Info);
    }
}

