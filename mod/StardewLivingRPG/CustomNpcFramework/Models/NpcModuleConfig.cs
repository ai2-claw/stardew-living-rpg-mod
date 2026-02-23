namespace StardewLivingRPG.CustomNpcFramework.Models;

public sealed class NpcModuleConfig
{
    public bool EnableQuestProposals { get; set; } = false;
    public bool EnableRumors { get; set; } = false;
    public bool EnableArticles { get; set; } = false;
    public bool EnableTownEvents { get; set; } = true;
}

