namespace StardewLivingRPG.CustomNpcFramework.Models;

public sealed class LoveLanguageNpcConfig
{
    public string Mechanic { get; set; } = "LoveLanguageEngine";
    public string Version { get; set; } = "1.0.0";
    public string StateKey { get; set; } = string.Empty;
    public List<string> ProfileAxes { get; set; } = new();
    public LoveLanguageOutputContract LLMOutputContract { get; set; } = new();
    public LoveLanguageMicroDateWhitelist MicroDateWhitelist { get; set; } = new();
    public List<string> FallbackTopics { get; set; } = new();
}

public sealed class LoveLanguageOutputContract
{
    public List<string> RequiredFields { get; set; } = new();
    public List<string> NextBeatAllowed { get; set; } = new();
}

public sealed class LoveLanguageMicroDateWhitelist
{
    public List<string> ObjectiveTypes { get; set; } = new();
    public List<string> RewardBundles { get; set; } = new();
}
