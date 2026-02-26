namespace StardewLivingRPG.CustomNpcFramework.Models;

public sealed class PortraitEmotionProfilesFile
{
    public Dictionary<string, NpcPortraitProfileEntry> Npcs { get; set; } = new();
}

public sealed class NpcPortraitProfileEntry
{
    public List<string> Aliases { get; set; } = new();
    public PortraitEmotionFrameMap DefaultFrames { get; set; } = new();
    public List<PortraitVariantProfileEntry> Variants { get; set; } = new();
}

public sealed class PortraitVariantProfileEntry
{
    public string Id { get; set; } = string.Empty;
    public int Priority { get; set; }
    public int FrameOffset { get; set; }
    public List<string> AppearanceIdContains { get; set; } = new();
    public List<string> Seasons { get; set; } = new();
    public List<string> Locations { get; set; } = new();
    public List<string> NpcNames { get; set; } = new();
    public PortraitEmotionFrameMap Frames { get; set; } = new();
}

public sealed class PortraitEmotionFrameMap
{
    public int? Neutral { get; set; }
    public int? Happy { get; set; }
    public int? Content { get; set; }
    public int? Blush { get; set; }
    public int? Sad { get; set; }
    public int? Angry { get; set; }
    public int? Worried { get; set; }
    public int? Surprised { get; set; }
}
