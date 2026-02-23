namespace StardewLivingRPG.CustomNpcFramework.Models;

public sealed class LoadedNpcPack
{
    public string PackId { get; init; } = string.Empty;
    public string PackName { get; init; } = string.Empty;
    public string PackVersion { get; init; } = string.Empty;
    public string FrameworkMinVersion { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, FrameworkNpcRecord> NpcsByToken { get; init; }
        = new Dictionary<string, FrameworkNpcRecord>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyDictionary<string, string> LocationLoreByToken { get; init; }
        = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

