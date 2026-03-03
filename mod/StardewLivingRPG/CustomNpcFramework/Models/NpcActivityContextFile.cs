namespace StardewLivingRPG.CustomNpcFramework.Models;

public sealed class NpcActivityContextFile
{
    public Dictionary<string, NpcActivityContextConfig> Npcs { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

