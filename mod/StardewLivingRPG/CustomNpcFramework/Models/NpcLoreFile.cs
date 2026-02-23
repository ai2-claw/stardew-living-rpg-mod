namespace StardewLivingRPG.CustomNpcFramework.Models;

public sealed class NpcLoreFile
{
    public Dictionary<string, NpcLoreEntry> Npcs { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> Locations { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

