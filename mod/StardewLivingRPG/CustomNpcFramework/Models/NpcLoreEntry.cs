namespace StardewLivingRPG.CustomNpcFramework.Models;

public sealed class NpcLoreEntry
{
    public string Role { get; set; } = string.Empty;
    public string Persona { get; set; } = string.Empty;
    public string Speech { get; set; } = string.Empty;
    public string Ties { get; set; } = string.Empty;
    public string Boundaries { get; set; } = string.Empty;
    public List<string> TimelineAnchors { get; set; } = new();
    public List<string> KnownLocations { get; set; } = new();
    public List<string> TiesToNpcs { get; set; } = new();
    public List<string> ForbiddenClaims { get; set; } = new();
}

