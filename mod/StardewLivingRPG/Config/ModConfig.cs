namespace StardewLivingRPG.Config;

using StardewModdingAPI;

public sealed class ModConfig
{
    public string Mode { get; set; } = "cozy_canon"; // cozy_canon | story_depth | living_chaos
    public float PriceFloorPct { get; set; } = 0.80f;
    public float PriceCeilingPct { get; set; } = 1.40f;
    public float DailyPriceDeltaCapPct { get; set; } = 0.10f;

    // Open text market board menu.
    public SButton OpenBoardKey { get; set; } = SButton.K;

    // Open daily newspaper menu.
    public SButton OpenNewspaperKey { get; set; } = SButton.J;

    // Open rumor board menu.
    public SButton OpenRumorBoardKey { get; set; } = SButton.L;

    // Player2 integration (M2)
    public bool EnablePlayer2 { get; set; } = true;
    public string Player2ApiBaseUrl { get; set; } = "https://api.player2.game/v1";
    public string Player2LocalAuthBaseUrl { get; set; } = "http://localhost:4315/v1";
    public string Player2DeviceAuthBaseUrl { get; set; } = "https://api.player2.game/v1";
    public int Player2DeviceAuthTimeoutSeconds { get; set; } = 120;
    public bool Player2BlockChatWhenLowJoules { get; set; } = true;
    public int Player2MinJoulesToChat { get; set; } = 5;
    public bool StrictNpcTemplateValidation { get; set; } = false; // when true, disable legacy quest_* template repair
    public bool EnableAmbientConsequencePipeline { get; set; } = true;
    public int AmbientRecordTownEventDailyCap { get; set; } = 2; // per-NPC cap in ambient context; <=0 disables cap

    // Player-facing auto-connect UX
    public bool AutoConnectPlayer2OnLoad { get; set; } = true;
    public bool ShowDeveloperConsoleCommands { get; set; } = false;

    // In-world work request anti-spam
    public int WorkRequestCooldownSeconds { get; set; } = 5;
    public int MaxUiGeneratedRequestsPerDay { get; set; } = 2;
    public int MaxOutstandingRequests { get; set; } = 6;

    // Multi-NPC scaffolding (comma-separated short names; first is default requester)
    public string Player2NpcRosterCsv { get; set; } = "Lewis,Pierre,Robin";

    // Integrated custom-NPC framework (content packs targeting mx146323.StardewLivingRPG)
    public bool EnableCustomNpcFramework { get; set; } = true;
    public bool EnableCustomNpcLoreInjection { get; set; } = true;
    public bool EnableVanillaCanonLoreInjection { get; set; } = true;
    public bool EnableStrictCustomNpcCanonValidation { get; set; } = true;
    public string CustomNpcLoreLocaleOverride { get; set; } = string.Empty;
    public bool LogCustomNpcPromptInjectionPreview { get; set; } = false;
    public bool LogVanillaCanonLoreInjectionPreview { get; set; } = false;

    // Portrait emotion profile framework (per-NPC/per-variant frame mapping).
    public bool EnablePortraitEmotionProfiles { get; set; } = true;
    public bool PortraitProfileStrictMode { get; set; } = false;
    public bool LogPortraitProfileResolution { get; set; } = false;
}
