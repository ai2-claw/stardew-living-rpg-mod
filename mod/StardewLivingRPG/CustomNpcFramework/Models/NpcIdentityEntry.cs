namespace StardewLivingRPG.CustomNpcFramework.Models;

public sealed class NpcIdentityEntry
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public List<string> Aliases { get; set; } = new();
    public string HomeRegion { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
}

