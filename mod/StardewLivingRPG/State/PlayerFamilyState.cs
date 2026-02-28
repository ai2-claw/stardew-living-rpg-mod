namespace StardewLivingRPG.State;

public sealed class PlayerFamilyState
{
    public string SpouseNpcId { get; set; } = string.Empty;
    public string SpouseName { get; set; } = string.Empty;
    public List<PlayerChildProfile> Children { get; set; } = new();
    public bool IsMarried { get; set; }
    public bool IsParent { get; set; }
    public int LastDetectedDay { get; set; }
    public int FactVersion { get; set; } = 1;
}

public sealed class PlayerChildProfile
{
    public string Name { get; set; } = string.Empty;
    public string AgeStage { get; set; } = "infant";
    public int FirstObservedDay { get; set; }
}
