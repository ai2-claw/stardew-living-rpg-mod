namespace StardewLivingRPG.CustomNpcFramework.Models;

public sealed class NpcRomanceLlmFile
{
    public Dictionary<string, LoveLanguageNpcConfig> Npcs { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
