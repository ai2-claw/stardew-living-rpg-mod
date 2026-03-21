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
    public bool EnableAmbientNpcMultiTurn { get; set; } = true;
    public int AmbientNpcConversationTurnDepth { get; set; } = 0; // 0=mode default (cozy=2, story=3, chaos=4)
    public int AmbientNpcConversationDailyLimit { get; set; } = 3;
    public int AmbientNpcPairCooldownDays { get; set; } = 2;
    public bool EnableAmbientNpcOverhearMoments { get; set; } = true;
    public int AmbientNpcOverhearCadenceDays { get; set; } = 2; // target cadence: every 2-3 in-game days
    public bool EnableAutonomousRoutines { get; set; } = true;
    public int AutonomyMaxBlocksPerDay { get; set; } = 2;
    public int AutonomyMaxHomeVisitsPerDay { get; set; } = 2;
    public int AutonomyMaxReplansPerBlock { get; set; } = 2;
    public int AutonomyLocationRevisitCooldownMinutes { get; set; } = 120;
    public int AutonomyNpcRevisitCooldownMinutes { get; set; } = 180;
    public int AutonomyMinEncounterIntervalMinutes { get; set; } = 8;
    public int AutonomyMaxEncountersPerNpcPerDay { get; set; } = 6;
    public float AutonomyEncounterScoreThreshold { get; set; } = 0.12f;
    public int AutonomyFaceToFaceEncounterChancePct { get; set; } = 50;
    public int PairEmotionMaxDeltaPerCommand { get; set; } = 5;
    public int PairEmotionMaxDeltaPerDayPerAxis { get; set; } = 15;
    public int BubbleMaxChars { get; set; } = 50;
    public float BubbleDurationMultiplier { get; set; } = 1.75f;
    public int BubbleMinDurationMs { get; set; } = 2000;
    public int BubbleMaxDurationMs { get; set; } = 5000;
    public int BubblePauseBetweenMs { get; set; } = 400;
    public int AutonomyMinimumConversationTurns { get; set; } = 4;
    public int AutonomyMaximumConversationTurns { get; set; } = 6;
    public bool AutonomyRequireConversationClosing { get; set; } = true;
    public bool EnablePlayer2AutonomySuggestions { get; set; } = true;
    public float Player2GoalMaxUrgencyInfluence { get; set; } = 0.6f;
    public float AutonomyIntensityCozy { get; set; } = 0.6f;
    public float AutonomyIntensityStory { get; set; } = 1.0f;
    public float AutonomyIntensityChaos { get; set; } = 1.4f;

    // Cross-Map Autonomy
    public bool EnableCrossMapAutonomy { get; set; } = true;
    public int AutonomyMaxTravelMinutesPerBlock { get; set; } = 60;
    public int AutonomyMaxWaitForTargetMinutes { get; set; } = 30;
    public int AutonomyCrossMapReplanLimit { get; set; } = 3;
    public bool AutonomyPrivateVisitFallbackToPublic { get; set; } = true;
    public int AutonomyFaceToFaceDistanceTiles { get; set; } = 3;
    public int AutonomyStagingTimeoutTicks { get; set; } = 180;
    public int AutonomyStuckDetectionTicks { get; set; } = 120;
    public int AutonomyMaterializationMaxRadius { get; set; } = 5;

    // Player-facing auto-connect UX
    public bool AutoConnectPlayer2OnLoad { get; set; } = true;
    public bool EnablePlayerChatMenu { get; set; } = true;
    public bool ShowPlayer2ConnectionHud { get; set; } = true;
    public bool ShowDeveloperConsoleCommands { get; set; } = true;
    public bool EnableTownSquareMagicianMinigame { get; set; } = true;

    // Chicken Race mini-game
    public int MaxChickenRacesPerDay { get; set; } = 5;
    public int MinBetAmount { get; set; } = 100;
    public int MaxBetAmount { get; set; } = 5000;

    // In-world work request anti-spam
    public int WorkRequestCooldownSeconds { get; set; } = 5;
    public int MaxUiGeneratedRequestsPerDay { get; set; } = 2;
    public int MaxOutstandingRequests { get; set; } = 6;

    // Multi-NPC scaffolding (comma-separated short names; first is default requester)
    public string Player2NpcRosterCsv { get; set; } = "Lewis,Pierre,Robin";

    // Integrated custom-NPC framework (content packs targeting mx146323.StardewLivingRPG)
    public bool EnableCustomNpcFramework { get; set; } = true;
    public bool EnableCustomNpcLoreInjection { get; set; } = true;
    public bool EnableExternalNpcAutodiscovery { get; set; } = true;
    public bool EnableExternalNpcAutoLore { get; set; } = true;
    public bool EnableVanillaCanonLoreInjection { get; set; } = true;
    public bool EnableStrictCustomNpcCanonValidation { get; set; } = true;
    public string CustomNpcLoreLocaleOverride { get; set; } = string.Empty;
    public bool LogCustomNpcPromptInjectionPreview { get; set; } = false;
    public bool LogVanillaCanonLoreInjectionPreview { get; set; } = false;

    // Love language romance engine (LLM-driven overlay on top of vanilla hearts).
    public bool EnableLoveLanguageEngine { get; set; } = true;
    public int LoveLanguageMaxFriendshipPointsPerChat { get; set; } = 20;
    public int LoveLanguageFriendshipDailyCap { get; set; } = 40;

    // Portrait emotion profile framework (per-NPC/per-variant frame mapping).
    public bool EnablePortraitEmotionProfiles { get; set; } = true;
    public bool PortraitProfileStrictMode { get; set; } = false;
    public bool LogPortraitProfileResolution { get; set; } = false;
}

