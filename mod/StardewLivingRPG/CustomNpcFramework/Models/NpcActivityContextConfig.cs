namespace StardewLivingRPG.CustomNpcFramework.Models;

public sealed class NpcActivityContextConfig
{
    public string FallbackActivity { get; set; } = string.Empty;
    public List<NpcActivityHotspotConfig> Hotspots { get; set; } = new();
}

public sealed class NpcActivityHotspotConfig
{
    public string Location { get; set; } = string.Empty;
    public int TileX { get; set; }
    public int TileY { get; set; }
    public int Radius { get; set; } = 2;
    public string Activity { get; set; } = string.Empty;
}

