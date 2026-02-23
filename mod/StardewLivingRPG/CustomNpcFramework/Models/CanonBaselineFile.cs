namespace StardewLivingRPG.CustomNpcFramework.Models;

public sealed class CanonBaselineFile
{
    public string Version { get; set; } = "1.0.0";
    public List<string> CanonicalNpcNames { get; set; } = new();
    public List<string> CanonicalLocationTokens { get; set; } = new();
    public List<string> AllowedTimelineAnchors { get; set; } = new();
    public List<string> ForbiddenClaimPatterns { get; set; } = new();
}

