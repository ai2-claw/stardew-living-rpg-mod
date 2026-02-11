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

    // Open request journal menu.
    public SButton OpenRequestJournalKey { get; set; } = SButton.O;

    // Open additive NPC postings follow-up prompt (without overriding vanilla action dialogue).
    public SButton OpenNpcPostingsPromptKey { get; set; } = SButton.P;

    // Player2 integration (M2)
    public bool EnablePlayer2 { get; set; } = false;
    public string Player2GameClientId { get; set; } = "";
    public string Player2ApiBaseUrl { get; set; } = "https://api.player2.game/v1";
    public string Player2LocalAuthBaseUrl { get; set; } = "http://localhost:4315/v1";
    public string Player2DeviceAuthBaseUrl { get; set; } = "https://api.player2.game/v1";
    public int Player2DeviceAuthTimeoutSeconds { get; set; } = 120;
    public bool Player2BlockChatWhenLowJoules { get; set; } = true;
    public int Player2MinJoulesToChat { get; set; } = 5;
    public bool StrictNpcTemplateValidation { get; set; } = false; // when true, disable legacy quest_* template repair

    // Player-facing auto-connect UX
    public bool AutoConnectPlayer2OnLoad { get; set; } = true;

    // In-world work request anti-spam
    public int WorkRequestCooldownSeconds { get; set; } = 5;
    public int MaxUiGeneratedRequestsPerDay { get; set; } = 2;
    public int MaxOutstandingRequests { get; set; } = 6;

    // Multi-NPC scaffolding (comma-separated short names; first is default requester)
    public string Player2NpcRosterCsv { get; set; } = "Lewis,Pierre,Robin";
}