namespace StardewLivingRPG.CustomNpcFramework.Models;

public sealed class NpcModulesFile
{
    public Dictionary<string, NpcModuleConfig> Npcs { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

