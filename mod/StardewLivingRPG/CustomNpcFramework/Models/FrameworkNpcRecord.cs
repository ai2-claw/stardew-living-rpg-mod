namespace StardewLivingRPG.CustomNpcFramework.Models;

public sealed class FrameworkNpcRecord
{
    public string PackId { get; init; } = string.Empty;
    public string PackName { get; init; } = string.Empty;
    public string NpcToken { get; init; } = string.Empty;
    public string NpcId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string HomeRegionToken { get; init; } = string.Empty;
    public IReadOnlyList<string> Aliases { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
    public NpcLoreEntry Lore { get; init; } = new();
    public NpcModuleConfig Modules { get; init; } = new();
}

