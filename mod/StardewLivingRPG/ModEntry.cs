using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;
using StardewLivingRPG.Config;
using StardewLivingRPG.CustomNpcFramework.Models;
using StardewLivingRPG.CustomNpcFramework.Services;
using StardewLivingRPG.Integrations;
using StardewLivingRPG.State;
using StardewLivingRPG.Systems;
using StardewLivingRPG.UI;
using StardewLivingRPG.Utils;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;

namespace StardewLivingRPG;

public sealed class ModEntry : Mod
{
    private const int NpcPublishAfternoonStartTime = 1300;
    private const int NpcPublishMinimumIntervalMinutes = 120;
    private const int AmbientLaneDebugSnapshotLimit = 20;
    private const int AmbientNpcCooldownMinutes = 8;
    private const float AmbientPublishRumorMinConfidence = 0.62f;
    private const int AutoMarketMinSignals = 2;
    private const float AutoMarketScarcityThreshold = 0.05f;
    private const float AutoMarketStrongScarcityThreshold = 0.09f;
    private const float AutoMarketOversupplyThreshold = 0.90f;
    private const float AutoMarketDeepOversupplyThreshold = 0.86f;
    private const int AmbientEventsPerAdditionalAutoMutation = 2;
    private const int MaxSimulationToastsPerDay = 2;
    private const int SimulationToastCooldownSeconds = 25;
    private const double StagedValidationMaxQuestRateDelta = 1.25d;
    private const double StagedValidationMaxMarketRateDelta = 0.75d;
    private const double DefaultMsPerNpcChatClockStep = 7000d;
    private const double NpcChatClockSlowdownMultiplier = 2d;
    private const float NpcDialogueHookInteractionRadiusTiles = 3.5f;
    private const float NpcDialogueHookFallbackRadiusTiles = 3.75f;
    private static readonly TimeSpan PendingFallbackQuestOfferMaxAge = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan VanillaDialogueContextMaxAge = TimeSpan.FromMinutes(12);
    private const int VanillaDialogueContextSequenceMaxLines = 6;
    private const string InitialNpcChatPrompt = "Got a minute to chat?";
    private const string CalendarLastWorldAbsoluteDayFactKey = "calendar:last_world_absolute_day";
    private const string CreatorPlayer2GameClientId = "019c4693-2a12-7ef5-bae2-ff29ee9fa674";

    private static readonly MethodInfo? PerformTenMinuteClockUpdateMethod = typeof(Game1).GetMethod(
        "performTenMinuteClockUpdate",
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
        binder: null,
        types: Type.EmptyTypes,
        modifiers: null);
    private static readonly FieldInfo? RealMsPerGameMinuteField = typeof(Game1).GetField(
        "realMilliSecondsPerGameMinute",
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
    private static readonly MethodInfo? UpdateAmbientLightingMethod = typeof(GameLocation).GetMethod(
        "_updateAmbientLighting",
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
        binder: null,
        types: Type.EmptyTypes,
        modifiers: null);
    private static readonly GameTime NpcChatZeroGameTime = new(TimeSpan.Zero, TimeSpan.Zero);
    private static readonly GameTime NpcChatVisualRefreshGameTime = new(TimeSpan.Zero, TimeSpan.FromMilliseconds(16d));
    private static readonly string[] ShopMenuOwnerMemberCandidates =
    {
        "portraitPerson",
        "potraitPerson",
        "portraitPersonName",
        "potraitPersonName",
        "storeOwner",
        "storeOwnerName",
        "owner",
        "ownerName",
        "shopOwner",
        "shopOwnerName",
        "PortraitPerson",
        "PortraitPersonName",
        "StoreOwner",
        "StoreOwnerName"
    };
    private static readonly HashSet<string> ChildNpcNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Jas",
        "Jaz",
        "Vincent"
    };

    private sealed class NpcPublishHeadlineUpdate
    {
        public int Day { get; init; }
        public string Command { get; init; } = string.Empty;
        public string OutcomeId { get; init; } = string.Empty;
        public string? SourceNpcId { get; init; }
        public string Headline { get; init; } = string.Empty;
    }

    private sealed class PendingFallbackQuestOffer
    {
        public PendingFallbackQuestOffer(string templateId, string target, string urgency, int requestedCount, DateTime offeredUtc)
        {
            TemplateId = templateId;
            Target = target;
            Urgency = urgency;
            RequestedCount = requestedCount;
            OfferedUtc = offeredUtc;
        }

        public string TemplateId { get; }
        public string Target { get; }
        public string Urgency { get; }
        public int RequestedCount { get; }
        public DateTime OfferedUtc { get; }
    }

    private sealed class RecentVanillaDialogueContext
    {
        public string NpcName { get; set; } = string.Empty;
        public string NpcDisplayName { get; set; } = string.Empty;
        public string LastDialogueLine { get; set; } = string.Empty;
        public List<string> DialogueSequence { get; } = new();
        public int Day { get; set; }
        public int TimeOfDay { get; set; }
        public DateTime CapturedUtc { get; set; }
    }

    private static readonly Dictionary<string, string> PublishSourceNpcFallbackMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Editor"] = "Elliott"
    };
    private static readonly string[] VanillaNpcRoster =
    {
        "Lewis", "Pierre", "Robin",
        "Abigail", "Alex", "Caroline", "Clint", "Demetrius",
        "Dwarf", "Elliott", "Emily", "Evelyn", "George", "Gil", "Gunther",
        "Gus", "Haley", "Harvey", "Jas", "Jodi", "Kent",
        "Krobus", "Leah", "Leo", "Linus", "Marnie", "Marlon", "Maru", "Morris",
        "Pam", "Penny", "Qi", "Sam", "Sandy", "Sebastian", "Shane",
        "Vincent", "Willy", "Wizard"
    };
    private static readonly HashSet<string> LowInfoAmbientMessages = new(StringComparer.OrdinalIgnoreCase)
    {
        "ok",
        "okay",
        "alright",
        "all right",
        "sure",
        "noted",
        "understood",
        "hmm",
        "...",
        "nothing new",
        "no news"
    };
    private static readonly Dictionary<string, int> AutoMutationBudgetByAxis = new(StringComparer.OrdinalIgnoreCase)
    {
        ["social"] = 2,
        ["interest"] = 1,
        ["market"] = 2,
        ["quest"] = 1
    };
    private static readonly string[] SpringNpcSupplyPool =
    {
        "parsnip", "potato", "cauliflower", "kale", "garlic", "strawberry", "green_bean", "rhubarb"
    };
    private static readonly string[] SummerNpcSupplyPool =
    {
        "blueberry", "melon", "tomato", "corn", "hot_pepper", "radish", "wheat", "hops"
    };
    private static readonly string[] FallNpcSupplyPool =
    {
        "pumpkin", "cranberry", "corn", "wheat", "eggplant", "yam", "bok_choy", "grape", "beet", "amaranth"
    };
    private static readonly string[] WinterNpcSupplyPool =
    {
        "wheat", "potato", "kale", "corn", "apple", "orange", "peach", "pomegranate"
    };
    private static readonly string[] OrchardNpcSupplyPool =
    {
        "apple", "orange", "peach", "pomegranate", "apricot", "cherry"
    };
    private static readonly string[] ForageNpcSupplyPool =
    {
        "wild_horseradish", "daffodil", "leek", "dandelion",
        "spice_berry", "sweet_pea", "blackberry", "hazelnut",
        "winter_root", "crystal_fruit", "snow_yam", "crocus"
    };
    private static readonly string[] FishingNpcSupplyPool =
    {
        "anchovy", "sardine", "herring", "tuna", "salmon", "sunfish", "catfish",
        "smallmouth_bass", "largemouth_bass", "carp", "bream", "tilapia", "halibut", "walleye", "eel"
    };
    private static readonly string[] MiningNpcResourcePool =
    {
        "stone", "copper_ore", "iron_ore", "gold_ore", "iridium_ore",
        "coal", "quartz", "refined_quartz", "earth_crystal", "frozen_tear", "fire_quartz",
        "amethyst", "topaz", "jade", "aquamarine", "ruby", "emerald", "diamond"
    };

    private ModConfig _config = new();
    private string _legacyPlayer2GameClientId = string.Empty;
    private SaveState _state = SaveState.CreateDefault();
    private DailyTickService? _dailyTickService;
    private EconomyService? _economyService;
    private MarketBoardService? _marketBoardService;
    private SalesIngestionService? _salesIngestionService;
    private NewspaperService? _newspaperService;
    private RumorBoardService? _rumorBoardService;
    private NpcIntentResolver? _intentResolver;
    private AnchorEventService? _anchorEventService;
    private NpcMemoryService? _npcMemoryService;
    private TownMemoryService? _townMemoryService;
    private AmbientConsequenceService? _ambientConsequenceService;
    private NpcSpeechStyleService? _npcSpeechStyleService;
    private NpcAskGateService? _npcAskGateService;
    private CommandPolicyService? _commandPolicyService;
    private CanonBaselineService? _customNpcCanonBaselineService;
    private NpcRegistry? _customNpcRegistry;
    private NpcPackLoader? _customNpcPackLoader;
    private IReadOnlyList<LoadedNpcPack> _customNpcLoadedPacks = Array.Empty<LoadedNpcPack>();
    private IReadOnlyList<ValidationIssue> _customNpcValidationIssues = Array.Empty<ValidationIssue>();

    // Player2 M2 runtime session state
    private Player2Client? _player2Client;
    private Player2Client? _authenticatedPlayer2Client;  // NEW: Store authenticated client separately
    private string? _player2Key;
    private string? _activeNpcId;
    private readonly ConcurrentQueue<string> _pendingPlayer2Lines = new();
    private readonly ConcurrentQueue<string> _pendingPlayer2ChatLines = new();
    private DateTime _player2LastLineUtc;
    private DateTime _player2LastCommandAppliedUtc;
    private string _player2LastCommandApplied = "(none)";
    private int _player2ReadInFlight;
    private DateTime _player2ReadStartedUtc;
    private CancellationTokenSource? _player2ReadCts;

    private CancellationTokenSource? _player2StreamCts;
    private int _player2StreamRunning;
    private bool _player2StreamDesired;
    private int _player2StreamBackoffSec = 1;
    private DateTime _player2NextReconnectUtc;

    private int _player2ConnectInFlight;
    private string _player2UiStatus = "Player2: idle";
    private const string BoardSearchStatusPostingAdded = "New posting added to the board.";
    private const string BoardSearchStatusNoRequest = "No one has a new posting right now.";
    private const string BoardSearchStatusNoPostingCreated = "No posting was added from that reply.";
    private DateTime _player2LastAutoConnectAttemptUtc;
    private string? _pendingUiMayorWorkRequest;
    private string? _pendingUiRequesterShortName;
    private string? _lastUiWorkPrompt;
    private string? _lastUiWorkRequesterShortName;
    private bool _uiBoardSearchAwaitingResult;
    private string? _uiBoardSearchRequesterShortName;
    private string? _uiBoardSearchNpcId;
    private DateTime _lastUiWorkRequestUtc;
    private int _uiWorkRequestInFlight;
    private int _uiRequesterRoundRobinIndex;
    private readonly Dictionary<string, string> _player2NpcIdsByShortName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _player2NpcShortNameById = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, ConcurrentQueue<string>> _npcUiMessagesById = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, ConcurrentQueue<bool>> _npcResponseRoutingById = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _npcLastReceivedMessageById = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _npcLastNonPlayerMessageById = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, DateTime> _npcLastNonPlayerMessageUtcById = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, DateTime> _npcLastPlayerChatRequestUtcById = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _npcLastPlayerPromptById = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _npcLastContextTagById = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, DateTime> _npcLastPlayerQuestAskUtcById = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, PendingFallbackQuestOffer> _npcPendingFallbackQuestOfferById = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, int> _npcUiPendingById = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentQueue<string> _ambientLaneDebugSnapshots = new();
    private int _player2PendingResponseCount;
    private DateTime _player2LastChatSentUtc;
    private DateTime _player2LastStreamRecoveryUtc;
    private DateTime _player2LastStreamStartUtc;
    private DateTime _player2StreamConnectedUtc;
    private int _player2WatchdogRecoveries;
    private DateTime _player2WatchdogWindowStartUtc;
    private bool _streamChatAwaitingResponse;
    private string? _lastStreamChatMessage;
    private string? _lastStreamChatTargetNpcId;
    private string? _lastStreamChatRequesterShortName;
    private string? _lastStreamChatSenderNameOverride;
    private string? _lastStreamChatContextTag;
    private string? _pendingStreamReplayMessage;
    private string? _pendingStreamReplayTargetNpcId;
    private string? _pendingStreamReplayRequesterShortName;
    private string? _pendingStreamReplaySenderNameOverride;
    private string? _pendingStreamReplayContextTag;
    private DateTime _pendingStreamReplayQueuedUtc;

    private int _uiManualRequestCountToday;
    private int _uiManualRequestCountDay = -1;
    private int _pendingNewspaperRefreshDay = -1;
    private int _pendingDayStartStreamRecycleDay = -1;
    private int _newspaperBuildInFlight;
    private readonly ConcurrentQueue<NewspaperIssue> _completedNewspaperIssues = new();
    private readonly ConcurrentQueue<NpcPublishHeadlineUpdate> _completedNpcPublishHeadlineUpdates = new();
    private readonly List<NpcPublishHeadlineUpdate> _pendingNpcPublishHeadlineUpdates = new();
    private int _lastNpcPublishAppliedDay = -1;
    private int _lastNpcPublishAppliedTimeOfDay = -1;
    private int _pendingLateNightPassOutDay = -1;
    private string _pendingLateNightPassOutLocation = "Town";
    private int _simulationToastDay = -1;
    private int _simulationToastsToday;
    private DateTime _lastSimulationToastUtc;
    private readonly Random _ambientNpcRandom = new();
    private int _ambientNpcConversationDay = -1;
    private int _ambientNpcConversationsToday;
    private DateTime _nextAmbientNpcConversationUtc;
    private int _ambientNpcConversationInFlight;
    private readonly ConcurrentDictionary<string, DateTime> _ambientNpcLastConversationUtcByNpcId = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, RecentVanillaDialogueContext> _recentVanillaDialogueByNpcToken = new(StringComparer.OrdinalIgnoreCase);

    private string? _pendingNpcDialogueHookName;
    private NPC? _pendingNpcDialogueHookNpc;
    private bool _npcDialogueHookArmed;
    private bool _npcDialogueHookMenuOpened;
    private DateTime _npcDialogueHookArmedUtc;
    private DateTime _npcChatClockLastTickUtc;
    private double _npcChatClockAccumulatorMs;
    private bool _npcChatClockMethodMissingLogged;
    private bool _npcChatVisualRefreshFailedLogged;
    private bool _npcChatLocationUpdateFailedLogged;
    private bool _player2AutoConnectSuppressedByUser;

    private readonly object _player2DeviceAuthUiLock = new();
    private bool _player2DeviceAuthUiActive;
    private string _player2DeviceAuthVerificationUrl = string.Empty;
    private string _player2DeviceAuthUserCode = string.Empty;
    private string _player2DeviceAuthStatus = string.Empty;
    private DateTime _player2DeviceAuthExpiresUtc;
    private CancellationTokenSource? _player2DeviceAuthCts;

    public override void Entry(IModHelper helper)
    {
        _config = helper.ReadConfig<ModConfig>();
        I18n.Initialize(helper.Translation);
        TryMigrateLegacyPlayer2Config(helper);
        EnsureRequiredPlayer2Enabled(helper);
        _dailyTickService = new DailyTickService(Monitor, _config);
        _economyService = new EconomyService();
        _marketBoardService = new MarketBoardService();
        _salesIngestionService = new SalesIngestionService();
        _newspaperService = new NewspaperService(Monitor, _player2Client);
        _rumorBoardService = new RumorBoardService();
        _npcMemoryService = new NpcMemoryService();
        _townMemoryService = new TownMemoryService();
        _ambientConsequenceService = new AmbientConsequenceService();
        _intentResolver = new NpcIntentResolver(_rumorBoardService, _npcMemoryService, _townMemoryService, _config.StrictNpcTemplateValidation);
        _anchorEventService = new AnchorEventService();
        var speechStyleConfig = helper.Data.ReadJsonFile<NpcSpeechStyleConfig>("npc_speech_profiles.json")
            ?? NpcSpeechStyleConfig.CreateDefault();
        _npcSpeechStyleService = new NpcSpeechStyleService(speechStyleConfig);
        _npcAskGateService = new NpcAskGateService();
        _commandPolicyService = new CommandPolicyService();
        _player2Client = new Player2Client();
        InitializeCustomNpcFramework(helper);

        RegisterPlayerConsoleCommands(helper);
        if (_config.ShowDeveloperConsoleCommands)
        {
            RegisterDeveloperConsoleCommands(helper);
            Monitor.Log("Developer console commands enabled (ShowDeveloperConsoleCommands=true).", LogLevel.Debug);
        }

        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        helper.Events.GameLoop.Saving += OnSaving;
        helper.Events.GameLoop.DayEnding += OnDayEnding;
        helper.Events.GameLoop.DayStarted += OnDayStarted;
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        helper.Events.Display.RenderedHud += OnRenderedHud;
        helper.Events.Display.MenuChanged += OnMenuChanged;
        helper.Events.Input.ButtonPressed += OnButtonPressed;

        Monitor.Log("The Living Valley loaded.", LogLevel.Info);
    }

    private void RegisterPlayerConsoleCommands(IModHelper helper)
    {
        helper.ConsoleCommands.Add("slrpg_open_board", "Open Market Board menu.", OnOpenBoardCommand);
        helper.ConsoleCommands.Add("slrpg_open_news", "Open latest newspaper issue.", OnOpenNewsCommand);
        helper.ConsoleCommands.Add("slrpg_open_rumors", "Open rumor board menu.", OnOpenRumorsCommand);
        helper.ConsoleCommands.Add("slrpg_p2_login", "Player2 local app login using built-in game client id.", OnPlayer2LoginCommand);
        helper.ConsoleCommands.Add("slrpg_p2_status", "Show Player2 session + joules + stream status.", OnPlayer2StatusCommand);
        helper.ConsoleCommands.Add("slrpg_town_memory_events", "List recent town-memory events: slrpg_town_memory_events [count]", OnTownMemoryEventsCommand);
    }

    private void RegisterDeveloperConsoleCommands(IModHelper helper)
    {
        helper.ConsoleCommands.Add("slrpg_sell", "Record simulated crop sale: slrpg_sell <crop> <count>", OnSellCommand);
        helper.ConsoleCommands.Add("slrpg_board", "Print text market board preview.", OnBoardCommand);
        helper.ConsoleCommands.Add("slrpg_accept_quest", "Accept rumor quest: slrpg_accept_quest <questId>", OnAcceptQuestCommand);
        helper.ConsoleCommands.Add("slrpg_quest_progress", "Show active quest progress: slrpg_quest_progress <questId>", OnQuestProgressCommand);
        helper.ConsoleCommands.Add("slrpg_quest_progress_all", "Show progress for all active quests.", OnQuestProgressAllCommand);
        helper.ConsoleCommands.Add("slrpg_complete_quest", "Complete active quest: slrpg_complete_quest <questId>", OnCompleteQuestCommand);
        helper.ConsoleCommands.Add("slrpg_set_sentiment", "Set sentiment: slrpg_set_sentiment economy <value>", OnSetSentimentCommand);
        helper.ConsoleCommands.Add("slrpg_debug_state", "Print compact state snapshot for QA.", OnDebugStateCommand);
        helper.ConsoleCommands.Add("slrpg_intent_inject", "Inject raw NPC intent envelope JSON for resolver QA.", OnIntentInjectCommand);
        helper.ConsoleCommands.Add("slrpg_debug_news_toast", "Inject debug publish_article/publish_rumor intent and trigger HUD toast: slrpg_debug_news_toast <article|rumor> [text]", OnDebugNewsToastCommand);
        helper.ConsoleCommands.Add("slrpg_intent_smoketest", "Run mini automated intent resolver smoke tests.", OnIntentSmokeTestCommand);
        helper.ConsoleCommands.Add("slrpg_regression_targeted", "Run targeted regression checks (chat routing, pass-out publication, market outlook, ambient command lane).", OnTargetedRegressionCommand);
        helper.ConsoleCommands.Add("slrpg_anchor_smoketest", "Run deterministic anchor trigger/resolution smoke test.", OnAnchorSmokeTestCommand);
        helper.ConsoleCommands.Add("slrpg_baseline_3day", "Run deterministic 3-day baseline metrics simulation.", OnBaselineThreeDayCommand);
        helper.ConsoleCommands.Add("slrpg_baseline_7day", "Run deterministic 7-day scenario simulation and compare against 3-day baseline.", OnBaselineSevenDayCommand);
        helper.ConsoleCommands.Add("slrpg_ambient_pipeline_validate", "Run staged validation for ambient consequence pipeline.", OnAmbientPipelineValidateCommand);
        helper.ConsoleCommands.Add("slrpg_demo_bootstrap", "Seed reproducible vertical-slice scenario.", OnDemoBootstrapCommand);
        helper.ConsoleCommands.Add("slrpg_memory_debug", "Dump NPC memory summary: slrpg_memory_debug <npc>", OnMemoryDebugCommand);
        helper.ConsoleCommands.Add("slrpg_town_memory_dump", "Dump town-memory event count.", OnTownMemoryDumpCommand);
        helper.ConsoleCommands.Add("slrpg_town_memory_npc", "Dump town-memory knowledge for npc: slrpg_town_memory_npc <npc>", OnTownMemoryNpcCommand);
        helper.ConsoleCommands.Add("slrpg_p2_spawn", "Spawn one Player2 NPC session.", OnPlayer2SpawnNpcCommand);
        helper.ConsoleCommands.Add("slrpg_p2_chat", "Send chat to active Player2 NPC: slrpg_p2_chat <message>", OnPlayer2ChatCommand);
        helper.ConsoleCommands.Add("slrpg_p2_read_once", "Read one line from /npcs/responses stream.", OnPlayer2ReadOnceCommand);
        helper.ConsoleCommands.Add("slrpg_p2_read_reset", "Reset/cancel stuck Player2 read_once.", OnPlayer2ReadResetCommand);
        helper.ConsoleCommands.Add("slrpg_p2_stream_start", "Start persistent Player2 response stream listener.", OnPlayer2StreamStartCommand);
        helper.ConsoleCommands.Add("slrpg_p2_stream_stop", "Stop persistent Player2 response stream listener.", OnPlayer2StreamStopCommand);
        helper.ConsoleCommands.Add("slrpg_p2_health", "Compact Player2 health summary line.", OnPlayer2HealthCommand);
        helper.ConsoleCommands.Add("slrpg_customnpc_validate", "Validate integrated custom-NPC content packs.", OnCustomNpcValidatePacksCommand);
        helper.ConsoleCommands.Add("slrpg_customnpc_list", "List loaded integrated custom NPCs.", OnCustomNpcListCommand);
        helper.ConsoleCommands.Add("slrpg_customnpc_dump", "Dump integrated custom NPC lore: slrpg_customnpc_dump <npc>", OnCustomNpcDumpCommand);
        helper.ConsoleCommands.Add("slrpg_customnpc_reload", "Reload integrated custom-NPC packs.", OnCustomNpcReloadCommand);
    }

    private void TryMigrateLegacyPlayer2Config(IModHelper helper)
    {
        var configPath = Path.Combine(helper.DirectoryPath, "config.json");
        if (!File.Exists(configPath))
            return;

        var hasLegacyKey = false;
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(configPath));
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return;

            if (doc.RootElement.TryGetProperty("Player2GameClientId", out var legacyIdEl))
            {
                hasLegacyKey = true;
                if (legacyIdEl.ValueKind == JsonValueKind.String)
                    _legacyPlayer2GameClientId = (legacyIdEl.GetString() ?? string.Empty).Trim();
            }
        }
        catch
        {
            return;
        }

        if (!hasLegacyKey)
            return;

        try
        {
            helper.WriteConfig(_config);
            Monitor.Log("Migrated config: removed deprecated Player2GameClientId key.", LogLevel.Info);
        }
        catch (Exception ex)
        {
            Monitor.Log($"Config migration skipped: {ex.Message}", LogLevel.Warn);
        }

        if (!HasBuiltInPlayer2GameClientId() && !string.IsNullOrWhiteSpace(_legacyPlayer2GameClientId))
        {
            Monitor.Log("Using legacy Player2GameClientId fallback from previous config for this build.", LogLevel.Warn);
        }
    }

    private void EnsureRequiredPlayer2Enabled(IModHelper helper)
    {
        if (_config.EnablePlayer2)
            return;

        _config.EnablePlayer2 = true;
        try
        {
            helper.WriteConfig(_config);
            Monitor.Log("Updated config: EnablePlayer2 is required and has been set to true.", LogLevel.Info);
        }
        catch (Exception ex)
        {
            Monitor.Log($"Could not persist EnablePlayer2=true migration: {ex.Message}", LogLevel.Warn);
        }
    }

    private string ResolvePlayer2GameClientId()
    {
        return HasBuiltInPlayer2GameClientId()
            ? CreatorPlayer2GameClientId.Trim()
            : _legacyPlayer2GameClientId;
    }

    private static bool HasBuiltInPlayer2GameClientId()
    {
        var value = (CreatorPlayer2GameClientId ?? string.Empty).Trim();
        return !string.IsNullOrWhiteSpace(value)
            && !value.Equals("REPLACE_WITH_CREATOR_PLAYER2_GAME_CLIENT_ID", StringComparison.Ordinal);
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        _state = StateStore.LoadOrCreate(Helper, Monitor);
        _state.ApplyConfig(_config);
        _recentVanillaDialogueByNpcToken.Clear();
        SyncCalendarSeasonFromWorld();
        _economyService?.EnsureInitialized(_state.Economy);
        _rumorBoardService?.ExpireOverdueQuests(_state);
        if (_config.EnableCustomNpcFramework)
        {
            ReloadCustomNpcPacks();
            InjectCustomNpcTargetsIntoRumorBoard("SaveLoaded");
        }
        Monitor.Log($"State loaded (version={_state.Version}, mode={_state.Config.Mode}).", LogLevel.Info);

        if (_config.EnablePlayer2 && _config.AutoConnectPlayer2OnLoad)
            StartPlayer2AutoConnect("save-loaded", force: false);
    }

    private void OnSaving(object? sender, SavingEventArgs e)
    {
        StateStore.Save(Helper, _state, Monitor);
    }

    private void OnDayEnding(object? sender, DayEndingEventArgs e)
    {
        CollectShippingBinSales();
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        if (_dailyTickService is null)
            return;

        SyncCalendarSeasonFromWorld();
        _pendingDayStartStreamRecycleDay = -1;
        _recentVanillaDialogueByNpcToken.Clear();
        TryCapturePendingLateNightPassOut();
        _lastNpcPublishAppliedDay = -1;
        _lastNpcPublishAppliedTimeOfDay = -1;

        // Give auto-connect a brief head start so day-start newspaper can use Player2 when available.
        if (_config.EnablePlayer2
            && _config.AutoConnectPlayer2OnLoad
            && string.IsNullOrWhiteSpace(_player2Key))
        {
            StartPlayer2AutoConnect("day-start-news", force: false);
        }

        if (_uiManualRequestCountDay != _state.Calendar.Day)
        {
            _uiManualRequestCountDay = _state.Calendar.Day;
            _uiManualRequestCountToday = 0;
        }

        ResetAmbientNpcConversationScheduleForDay();

        _economyService?.EnsureInitialized(_state.Economy);
        var sold = _salesIngestionService?.DrainPendingSales() ?? new Dictionary<string, int>();
        AppendSimulatedNpcMarketSales(sold);
        _economyService?.IngestSales(_state.Economy, sold);
        _economyService?.RunDailyPricing(_state);
        _dailyTickService.Run(_state);

        _rumorBoardService?.ExpireOverdueQuests(_state);
        _rumorBoardService?.RefreshDailyRumors(_state);

        string? anchorNote = null;
        if (_anchorEventService is not null)
        {
            if (_anchorEventService.TryTriggerEmergencyTownHall(_state, out var note))
                anchorNote = note;

            _anchorEventService.TryResolveEmergencyTownHall(_state);
        }

        if (_newspaperService is not null)
        {
            if (_config.EnablePlayer2)
            {
                _pendingNewspaperRefreshDay = _state.Calendar.Day;
                _pendingDayStartStreamRecycleDay = _state.Calendar.Day;
                if (!IsPlayer2ReadyForNewspaper())
                {
                    Monitor.Log($"Deferred newspaper build for day {_state.Calendar.Day} until Player2 roster + stream are ready.", LogLevel.Debug);
                }

                TryRefreshPendingNewspaperIssue("day-start");
            }
            else
            {
                BuildAndStoreNewspaperIssue();
                _pendingNewspaperRefreshDay = -1;
            }
        }

        Monitor.Log($"Daily tick complete for day {_state.Calendar.Day} ({_state.Calendar.Season} Y{_state.Calendar.Year}).", LogLevel.Debug);
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        if (TryHandleMenuHotkeyToggle(e.Button))
            return;

        if (Game1.activeClickableMenu is not null)
            return;

        TryHandleNpcWorkDialogueHook(e);

        if (e.Button == SButton.MouseLeft)
        {
            var point = new Point(Game1.getMouseX(), Game1.getMouseY());
            if (GetPlayer2HudRect().Contains(point) && !IsLocalInsightHudActive())
                StartPlayer2AutoConnect("hud-button", force: true);
        }
    }

    private bool TryHandleMenuHotkeyToggle(SButton button)
    {
        if (button == _config.OpenBoardKey)
        {
            if (Game1.activeClickableMenu is MarketBoardMenu)
            {
                CloseActiveMenuFromHotkey();
                return true;
            }

            if (Game1.activeClickableMenu is null)
            {
                OpenMarketBoard();
                return true;
            }

            return false;
        }

        if (button == _config.OpenNewspaperKey)
        {
            if (Game1.activeClickableMenu is NewspaperMenu)
            {
                CloseActiveMenuFromHotkey();
                return true;
            }

            if (Game1.activeClickableMenu is null)
            {
                OpenNewspaper();
                return true;
            }

            return false;
        }

        if (button == _config.OpenRumorBoardKey)
        {
            if (Game1.activeClickableMenu is RumorBoardMenu)
            {
                CloseActiveMenuFromHotkey();
                return true;
            }

            if (Game1.activeClickableMenu is null)
            {
                OpenRumorBoard();
                return true;
            }

            return false;
        }

        return false;
    }

    private static void CloseActiveMenuFromHotkey()
    {
        if (Game1.activeClickableMenu is null)
            return;

        Game1.activeClickableMenu.exitThisMenuNoSound();
        Game1.playSound("bigDeSelect");
    }

    private bool TryHandleNpcWorkDialogueHook(ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return false;

        if (!e.Button.IsActionButton())
            return false;

        if (Game1.activeClickableMenu is not null || Game1.dialogueUp)
            return false;

        var loc = Game1.currentLocation;
        if (loc is null)
            return false;

        var nearbyRequester = TryResolveNpcDialogueHookTarget(loc);

        if (nearbyRequester is null)
            return false;

        // Additive hook: don't block vanilla interaction.
        // We arm a follow-up question shown after vanilla dialogue/menu closes.
        ArmNpcDialogueHook(nearbyRequester);
        return false;
    }

    private NPC? TryResolveNpcDialogueHookTarget(GameLocation location)
    {
        var playerTile = Game1.player.Tile;
        var facingTile = GetPlayerFacingTile(playerTile, Game1.player.FacingDirection);
        var facingUnit = GetFacingUnitVector(Game1.player.FacingDirection);

        return location.characters
            .Where(npc => npc is not null && IsRosterNpc(npc))
            .Select(npc => new
            {
                Npc = npc,
                PlayerDistance = Vector2.Distance(npc.Tile, playerTile),
                FacingTileDistance = Vector2.Distance(npc.Tile, facingTile),
                FacingAlignment = GetFacingAlignment(playerTile, npc.Tile, facingUnit)
            })
            .Where(x => x.PlayerDistance <= NpcDialogueHookInteractionRadiusTiles)
            .OrderBy(x => x.FacingTileDistance)
            .ThenByDescending(x => x.FacingAlignment)
            .ThenBy(x => x.PlayerDistance)
            .Select(x => x.Npc)
            .FirstOrDefault();
    }

    private static Vector2 GetPlayerFacingTile(Vector2 playerTile, int facingDirection)
    {
        return facingDirection switch
        {
            0 => new Vector2(playerTile.X, playerTile.Y - 1f),
            1 => new Vector2(playerTile.X + 1f, playerTile.Y),
            3 => new Vector2(playerTile.X - 1f, playerTile.Y),
            _ => new Vector2(playerTile.X, playerTile.Y + 1f)
        };
    }

    private static Vector2 GetFacingUnitVector(int facingDirection)
    {
        return facingDirection switch
        {
            0 => new Vector2(0f, -1f),
            1 => new Vector2(1f, 0f),
            3 => new Vector2(-1f, 0f),
            _ => new Vector2(0f, 1f)
        };
    }

    private static float GetFacingAlignment(Vector2 originTile, Vector2 targetTile, Vector2 facingUnit)
    {
        var delta = targetTile - originTile;
        var lenSq = delta.LengthSquared();
        if (lenSq <= 0.0001f)
            return float.MinValue;

        delta /= MathF.Sqrt(lenSq);
        return Vector2.Dot(delta, facingUnit);
    }

    private void ArmNpcDialogueHook(NPC npc)
    {
        if (npc is null || string.IsNullOrWhiteSpace(npc.Name))
            return;

        SetNpcDialogueHookTarget(npc);
        BeginVanillaDialogueCaptureSession(npc);
        _npcDialogueHookArmed = true;
        _npcDialogueHookMenuOpened = false;
        _npcDialogueHookArmedUtc = DateTime.UtcNow;
    }

    private void SetNpcDialogueHookTarget(NPC npc)
    {
        if (npc is null || string.IsNullOrWhiteSpace(npc.Name))
            return;

        _pendingNpcDialogueHookName = npc.Name;
        _pendingNpcDialogueHookNpc = npc;
    }

    private void TryArmNpcDialogueHookFromMenu(IClickableMenu? menu)
    {
        if (_npcDialogueHookArmed || menu is not ShopMenu shopMenu)
            return;

        var owner = TryResolveNpcFromOpenedMenu(shopMenu);
        if (owner is null && Game1.currentLocation is not null)
            owner = TryResolveNpcDialogueHookTarget(Game1.currentLocation);
        if (owner is null)
            owner = ResolveJojaFallbackNpc(shopMenu);

        if (owner is null || !IsRosterNpc(owner))
            return;

        ArmNpcDialogueHook(owner);
    }

    private void TrySyncNpcDialogueHookTargetFromMenu(IClickableMenu menu)
    {
        if (!_npcDialogueHookArmed)
            return;

        var target = TryResolveNpcFromOpenedMenu(menu);
        if (target is null || !IsRosterNpc(target))
            return;

        SetNpcDialogueHookTarget(target);
    }

    private NPC? TryResolveNpcFromOpenedMenu(IClickableMenu menu)
    {
        if (menu is ShopMenu shopMenu)
        {
            var owner = TryResolveShopMenuOwnerNpc(shopMenu);
            if (owner is not null)
                return owner;

            var jojaFallback = ResolveJojaFallbackNpc(shopMenu);
            if (jojaFallback is not null)
                return jojaFallback;
        }

        if (Game1.currentSpeaker is not null && !string.IsNullOrWhiteSpace(Game1.currentSpeaker.Name))
            return Game1.currentSpeaker;

        if (Game1.currentLocation is not null)
            return TryResolveNpcDialogueHookTarget(Game1.currentLocation);

        return null;
    }

    private NPC? ResolveJojaFallbackNpc(IClickableMenu menu)
    {
        if (!IsLikelyJojaShopContext(menu))
            return null;

        return ResolveNpcByName("Morris");
    }

    private static bool IsLikelyJojaShopContext(IClickableMenu menu)
    {
        if (Game1.currentLocation?.Name?.Contains("joja", StringComparison.OrdinalIgnoreCase) == true)
            return true;

        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var menuType = menu.GetType();

        foreach (var field in menuType.GetFields(Flags))
        {
            if (!typeof(string).IsAssignableFrom(field.FieldType))
                continue;

            if (field.GetValue(menu) is string value
                && value.Contains("joja", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        foreach (var property in menuType.GetProperties(Flags))
        {
            if (!typeof(string).IsAssignableFrom(property.PropertyType) || property.GetIndexParameters().Length > 0)
                continue;

            try
            {
                if (property.GetValue(menu) is string value
                    && value.Contains("joja", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            catch
            {
            }
        }

        return false;
    }

    private NPC? TryResolveShopMenuOwnerNpc(ShopMenu shopMenu)
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var menuType = shopMenu.GetType();

        foreach (var memberName in ShopMenuOwnerMemberCandidates)
        {
            var field = menuType.GetField(memberName, Flags);
            if (field is not null)
            {
                var fromField = ResolveNpcFromOwnerToken(field.GetValue(shopMenu));
                if (fromField is not null)
                    return fromField;
            }

            var property = menuType.GetProperty(memberName, Flags);
            if (property is null || property.GetIndexParameters().Length > 0)
                continue;

            try
            {
                var fromProperty = ResolveNpcFromOwnerToken(property.GetValue(shopMenu));
                if (fromProperty is not null)
                    return fromProperty;
            }
            catch
            {
            }
        }

        foreach (var field in menuType.GetFields(Flags))
        {
            var npc = ResolveNpcFromOwnerToken(field.GetValue(shopMenu));
            if (npc is not null)
                return npc;
        }

        foreach (var property in menuType.GetProperties(Flags))
        {
            if (property.GetIndexParameters().Length > 0)
                continue;

            try
            {
                var npc = ResolveNpcFromOwnerToken(property.GetValue(shopMenu));
                if (npc is not null)
                    return npc;
            }
            catch
            {
            }
        }

        return null;
    }

    private NPC? ResolveNpcFromOwnerToken(object? ownerToken)
    {
        if (ownerToken is NPC npc && !string.IsNullOrWhiteSpace(npc.Name))
            return npc;

        if (ownerToken is string ownerName)
            return ResolveNpcByName(ownerName);

        return null;
    }

    private NPC? ResolveNpcByName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var trimmed = name.Trim();
        var byInternalName = Game1.getCharacterFromName(trimmed);
        if (byInternalName is not null)
            return byInternalName;

        var currentLocationMatch = Game1.currentLocation?.characters
            ?.FirstOrDefault(c => string.Equals(c?.displayName, trimmed, StringComparison.OrdinalIgnoreCase));
        if (currentLocationMatch is not null)
            return currentLocationMatch;

        foreach (var location in Game1.locations)
        {
            var locationMatch = location?.characters
                ?.FirstOrDefault(c => string.Equals(c?.Name, trimmed, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(c?.displayName, trimmed, StringComparison.OrdinalIgnoreCase));
            if (locationMatch is not null)
                return locationMatch;
        }

        return null;
    }

    private NPC? ResolvePendingNpcDialogueHookNpc(string requesterName, GameLocation? location)
    {
        var npc = location?.characters?.FirstOrDefault(c => string.Equals(c?.Name, requesterName, StringComparison.OrdinalIgnoreCase));
        if (npc is not null)
            return npc;

        if (_pendingNpcDialogueHookNpc is not null
            && string.Equals(_pendingNpcDialogueHookNpc.Name, requesterName, StringComparison.OrdinalIgnoreCase))
        {
            return _pendingNpcDialogueHookNpc;
        }

        return null;
    }

    private bool IsRosterNpc(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        var roster = GetExpandedNpcRoster();

        return roster.Any(n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsRosterNpc(NPC npc)
    {
        if (npc is null)
            return false;

        if (IsRosterNpc(npc.Name))
            return true;

        return !string.IsNullOrWhiteSpace(npc.displayName) && IsRosterNpc(npc.displayName);
    }

    private List<string> GetExpandedNpcRoster()
    {
        var merged = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var configuredRoster = (_config.Player2NpcRosterCsv ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var shortName in configuredRoster)
        {
            if (string.IsNullOrWhiteSpace(shortName))
                continue;

            if (seen.Add(shortName))
                merged.Add(shortName);
        }

        foreach (var shortName in VanillaNpcRoster)
        {
            if (seen.Add(shortName))
                merged.Add(shortName);
        }

        if (_config.EnableCustomNpcFramework && _customNpcRegistry is not null)
        {
            foreach (var npc in _customNpcRegistry.NpcsByToken.Values)
            {
                if (!string.IsNullOrWhiteSpace(npc.DisplayName) && seen.Add(npc.DisplayName))
                    merged.Add(npc.DisplayName);
                if (!string.IsNullOrWhiteSpace(npc.NpcId) && seen.Add(npc.NpcId))
                    merged.Add(npc.NpcId);
            }
        }

        return merged;
    }

    private List<string> GetPlayer2SpawnRoster()
    {
        var merged = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Prioritize integrated custom NPCs first so they are spawned even if Player2 session limits are reached.
        if (_config.EnableCustomNpcFramework && _customNpcRegistry is not null)
        {
            foreach (var npc in _customNpcRegistry.NpcsByToken.Values.OrderBy(n => n.DisplayName, StringComparer.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(npc.NpcId) && seen.Add(npc.NpcId))
                    merged.Add(npc.NpcId);
                else if (!string.IsNullOrWhiteSpace(npc.DisplayName) && seen.Add(npc.DisplayName))
                    merged.Add(npc.DisplayName);
            }
        }

        var configuredRoster = (_config.Player2NpcRosterCsv ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var shortName in configuredRoster)
        {
            if (!string.IsNullOrWhiteSpace(shortName) && seen.Add(shortName))
                merged.Add(shortName);
        }

        foreach (var shortName in VanillaNpcRoster)
        {
            if (seen.Add(shortName))
                merged.Add(shortName);
        }

        // Keep compatibility with any additional names injected into expanded roster.
        foreach (var shortName in GetExpandedNpcRoster())
        {
            if (seen.Add(shortName))
                merged.Add(shortName);
        }

        return merged;
    }

    private void RegisterCustomNpcSpawnAliases(string rosterKey, string npcId)
    {
        if (!_config.EnableCustomNpcFramework || _customNpcRegistry is null)
            return;

        if (!_customNpcRegistry.TryGetNpcByName(rosterKey, out var customNpc))
            return;

        if (!string.IsNullOrWhiteSpace(customNpc.NpcId))
            _player2NpcIdsByShortName[customNpc.NpcId] = npcId;
        if (!string.IsNullOrWhiteSpace(customNpc.DisplayName))
            _player2NpcIdsByShortName[customNpc.DisplayName] = npcId;

        foreach (var alias in customNpc.Aliases)
        {
            if (!string.IsNullOrWhiteSpace(alias))
                _player2NpcIdsByShortName[alias] = npcId;
        }
    }

    private bool TryCreateRosterTalkDialogue(GameLocation loc, NPC npc, bool suppressFirstInteractionGreeting = false)
    {
        var name = npc.Name ?? string.Empty;
        if (!IsRosterNpc(name))
            return false;

        var prompt = BuildNpcFollowUpGreeting(npc, suppressFirstInteractionGreeting);
        var responses = new List<Response>();
        if (HasPendingQuestForNpc(name))
            responses.Add(new Response("town_word", "What's the word around town?"));
        responses.Add(new Response("talk", "Got a minute to chat?"));
        responses.Add(new Response("later", "Catch you later!"));

        loc.createQuestionDialogue(
            $"{npc.displayName}: {prompt}",
            responses.ToArray(),
            (_, answer) =>
            {
                if (string.Equals(answer, "town_word", StringComparison.OrdinalIgnoreCase))
                {
                    if (HasPendingQuestForNpc(name))
                    {
                        OpenRumorBoard();
                        return;
                    }

                    if (_state.Newspaper.Issues.Any())
                    {
                        OpenNewspaper();
                        return;
                    }

                    Game1.drawObjectDialogue($"{npc.displayName}: Quiet day so far. Nothing urgent on my side.");
                    return;
                }

                if (string.Equals(answer, "talk", StringComparison.OrdinalIgnoreCase))
                {
                    OpenNpcChatMenu(
                        npc,
                        initialPlayerMessage: InitialNpcChatPrompt,
                        autoSendInitialPlayerMessage: true,
                        defaultContextTag: "player_chat_followup");
                    return;
                }
            },
            npc);

        return true;
    }

    private void OpenNpcChatMenu(
        NPC npc,
        string? initialPlayerMessage = null,
        bool autoSendInitialPlayerMessage = false,
        string? defaultContextTag = null)
    {
        var npcName = npc.Name ?? npc.displayName;
        var heartLevel = GetNpcHeartLevel(npcName);
        var npcIdForChat = _player2NpcIdsByShortName.TryGetValue(npcName, out var knownNpcId)
            ? knownNpcId
            : _activeNpcId;

        Game1.activeClickableMenu = new NpcChatInputMenu(
            npc.displayName,
            text =>
            {
                if (!string.IsNullOrWhiteSpace(npcIdForChat))
                    SendPlayer2ChatInternal(text, npcIdForChat, npcName, contextTag: defaultContextTag);
                else
                    SendPlayer2ChatInternal(text, contextTag: defaultContextTag);
            },
            () => string.IsNullOrWhiteSpace(npcIdForChat) ? null : DequeueNpcUiMessage(npcIdForChat),
            () => !string.IsNullOrWhiteSpace(npcIdForChat) && IsNpcThinking(npcIdForChat),
            heartLevel,
            initialPlayerMessage,
            autoSendInitialPlayerMessage);
    }

    private bool HasPendingQuestForNpc(string npcName)
    {
        if (string.IsNullOrWhiteSpace(npcName))
            return false;

        var issuer = npcName.Trim().ToLowerInvariant();
        return _state.Quests.Available.Any(q =>
            !string.IsNullOrWhiteSpace(q.Issuer)
            && q.Issuer.Equals(issuer, StringComparison.OrdinalIgnoreCase)
            && (q.ExpiresDay <= 0 || q.ExpiresDay >= _state.Calendar.Day));
    }

    private bool IsFirstInteractionWithNpc(string npcName)
    {
        if (string.IsNullOrWhiteSpace(npcName))
            return true;

        if (HasNpcMetPlayer(npcName))
            return false;

        if (!_state.NpcMemory.Profiles.TryGetValue(npcName, out var profile))
            return true;

        return profile.RecentTurns.Count == 0 && profile.Facts.Count == 0;
    }

    private string BuildNpcFollowUpGreeting(NPC npc, bool suppressFirstInteractionGreeting = false)
    {
        var npcName = string.IsNullOrWhiteSpace(npc.Name) ? npc.displayName : npc.Name;
        var profile = _npcSpeechStyleService?.GetProfile(npcName) ?? NpcVerbalProfile.Traditionalist;
        var heartLevel = GetNpcHeartLevel(npcName);
        var isReserved = heartLevel <= 2;
        var playerAddress = ResolvePlayerAddressForNpc(npcName);
        var dayPeriod = Game1.timeOfDay switch
        {
            < 1200 => "morning",
            < 1700 => "afternoon",
            < 2200 => "evening",
            _ => "night"
        };

        if (!suppressFirstInteractionGreeting && IsFirstInteractionWithNpc(npcName))
        {
            if (isReserved)
            {
                return profile switch
                {
                    NpcVerbalProfile.Professional => $"Good {dayPeriod}. I do not think we have spoken before.",
                    NpcVerbalProfile.Intellectual => $"Good {dayPeriod}. I do not believe we have met properly.",
                    NpcVerbalProfile.Enthusiast => $"Good {dayPeriod}. I do not think we have talked before.",
                    NpcVerbalProfile.Recluse => $"...Good {dayPeriod}. I do not know you yet.",
                    _ => $"Good {dayPeriod}, {playerAddress}. I do not think we have spoken before."
                };
            }

            return profile switch
            {
                NpcVerbalProfile.Professional => $"Good {dayPeriod}. I do not think we have chatted properly before. Name's {npc.displayName}.",
                NpcVerbalProfile.Intellectual => $"Good {dayPeriod}. I do not believe we have spoken much before. I am {npc.displayName}.",
                NpcVerbalProfile.Enthusiast => $"Hey! Good {dayPeriod}! I do not think we have talked much yet.",
                NpcVerbalProfile.Recluse => $"...Oh. You are new to me. I am {npc.displayName}.",
                _ => $"Good {dayPeriod}, {playerAddress}. I do not think we have properly talked before."
            };
        }

        var greetings = new List<string>();
        var weather = GetCurrentWeatherLabel();

        if (!isReserved && TryBuildMemoryGreeting(npcName, heartLevel, out var memoryGreeting))
            greetings.Add(memoryGreeting);
        if (!isReserved && TryBuildTownPulseGreeting(out var townGreeting))
            greetings.Add(townGreeting);
        if (!isReserved && TryBuildEconomySignalGreeting(out var economyGreeting))
            greetings.Add(economyGreeting);

        var baseGreeting = profile switch
        {
            NpcVerbalProfile.Professional => isReserved
                ? $"Good {dayPeriod}. What is it?"
                : $"Good {dayPeriod}. Keeping things steady around town today.",
            NpcVerbalProfile.Intellectual => isReserved
                ? $"Good {dayPeriod}. What is your question?"
                : $"Good {dayPeriod}. I have been making a few observations around town.",
            NpcVerbalProfile.Enthusiast => isReserved
                ? $"Good {dayPeriod}. Need something?"
                : $"Hey! Good {dayPeriod}! The town feels lively today.",
            NpcVerbalProfile.Recluse => isReserved
                ? $"...{dayPeriod}. What do you need?"
                : $"...{dayPeriod} then. Town is quieter than usual.",
            _ => isReserved
                ? $"Good {dayPeriod}. What do you need?"
                : $"Good {dayPeriod}, {playerAddress}. How are things on your side?"
        };
        greetings.Add(baseGreeting);

        if (isReserved)
            return baseGreeting;

        if (weather == "rain")
        {
            greetings.Add(profile switch
            {
                NpcVerbalProfile.Professional => "Rain's slowing everyone down a little, but we'll manage.",
                NpcVerbalProfile.Recluse => "Rain again... keeps people indoors.",
                _ => "Rain's settled in over town today."
            });
        }
        else if (weather == "snow")
        {
            greetings.Add("Snow has everyone moving a bit slower today.");
        }

        return greetings[_ambientNpcRandom.Next(greetings.Count)];
    }

    private bool TryBuildMemoryGreeting(string npcName, int heartLevel, out string greeting)
    {
        greeting = string.Empty;
        if (string.IsNullOrWhiteSpace(npcName))
            return false;

        if (!_state.NpcMemory.Profiles.TryGetValue(npcName, out var profile))
            return false;

        var lastTurn = profile.RecentTurns.LastOrDefault();
        if (lastTurn is null)
            return false;

        var topic = lastTurn.Tags?.FirstOrDefault(t => !string.IsNullOrWhiteSpace(t));
        if (!string.IsNullOrWhiteSpace(topic))
        {
            greeting = heartLevel >= 6
                ? $"Hey, I was still thinking about what you said on {topic}."
                : $"I was still thinking about what you said on {topic}.";
        }
        else
        {
            greeting = heartLevel >= 6
                ? "Hey, good to see you again."
                : "Good to see you again.";
        }

        return !string.IsNullOrWhiteSpace(greeting);
    }

    private bool TryBuildTownPulseGreeting(out string greeting)
    {
        greeting = string.Empty;
        var recent = _state.TownMemory.Events
            .Where(ev => ev.Day >= _state.Calendar.Day - 2 && !string.IsNullOrWhiteSpace(ev.Summary))
            .OrderByDescending(ev => ev.Day)
            .ThenByDescending(ev => ev.Severity)
            .FirstOrDefault();

        if (recent is null)
            return false;

        var summary = recent.Summary.Trim();
        if (summary.Length > 72)
            summary = summary[..69] + "...";

        greeting = $"Town's been buzzing: {summary}";
        return true;
    }

    private bool TryBuildEconomySignalGreeting(out string greeting)
    {
        greeting = string.Empty;
        if (_state.Economy.Crops.Count == 0)
            return false;

        var mover = _state.Economy.Crops
            .Select(kv => new { Crop = kv.Key, Delta = kv.Value.PriceToday - kv.Value.PriceYesterday })
            .OrderByDescending(x => Math.Abs(x.Delta))
            .FirstOrDefault();

        if (mover is null || Math.Abs(mover.Delta) < 2)
            return false;

        var direction = mover.Delta > 0 ? "up" : "down";
        greeting = $"{mover.Crop} prices are {direction} today.";
        return true;
    }

    private void TryAdvanceClockWhileNpcChatOpen()
    {
        if (Game1.activeClickableMenu is not NpcChatInputMenu
            || Game1.eventUp
            || Game1.dialogueUp
            || Game1.currentLocation is null)
        {
            _npcChatClockLastTickUtc = default;
            _npcChatClockAccumulatorMs = 0d;
            return;
        }

        var now = DateTime.UtcNow;
        if (_npcChatClockLastTickUtc == default)
        {
            _npcChatClockLastTickUtc = now;
            return;
        }

        var elapsedMs = (now - _npcChatClockLastTickUtc).TotalMilliseconds;
        _npcChatClockLastTickUtc = now;
        if (elapsedMs <= 0d)
            return;

        // Clamp to avoid huge jumps after tabbing out.
        elapsedMs = Math.Min(elapsedMs, 250d);
        TryUpdateNpcChatLocationVisuals(elapsedMs);
        _npcChatClockAccumulatorMs += elapsedMs;

        var msPerClockStep = GetNpcChatClockStepMs(Game1.currentLocation);
        while (_npcChatClockAccumulatorMs >= msPerClockStep)
        {
            _npcChatClockAccumulatorMs -= msPerClockStep;
            if (!TryInvokeTenMinuteClockUpdate())
            {
                _npcChatClockAccumulatorMs = 0d;
                return;
            }
        }
    }

    private double GetNpcChatClockStepMs(GameLocation location)
    {
        var baseMsPerMinute = DefaultMsPerNpcChatClockStep / 10d;
        var raw = RealMsPerGameMinuteField?.GetValue(null);
        switch (raw)
        {
            case int i when i > 0:
                baseMsPerMinute = i;
                break;
            case float f when f > 0f:
                baseMsPerMinute = f;
                break;
            case double d when d > 0d:
                baseMsPerMinute = d;
                break;
        }

        // performTenMinuteClockUpdate advances the world by 10 in-game minutes per call.
        // Apply slowdown multiplier so chat time moves slower than normal.
        var perMinuteTotal = Math.Max(1d, baseMsPerMinute + Math.Max(0, location.ExtraMillisecondsPerInGameMinute));
        return Math.Max(300d, perMinuteTotal * 10d * NpcChatClockSlowdownMultiplier);
    }

    private bool TryInvokeTenMinuteClockUpdate()
    {
        if (PerformTenMinuteClockUpdateMethod is null)
        {
            if (!_npcChatClockMethodMissingLogged)
            {
                _npcChatClockMethodMissingLogged = true;
                Monitor.Log("NPC chat unpause fallback unavailable: performTenMinuteClockUpdate not found.", LogLevel.Warn);
            }

            return false;
        }

        try
        {
            PerformTenMinuteClockUpdateMethod.Invoke(null, null);
            TryRefreshNpcChatVisuals();
            return true;
        }
        catch (Exception ex)
        {
            if (!_npcChatClockMethodMissingLogged)
            {
                _npcChatClockMethodMissingLogged = true;
                Monitor.Log($"NPC chat unpause failed to invoke clock update: {ex.Message}", LogLevel.Warn);
            }

            return false;
        }
    }

    private void TryRefreshNpcChatVisuals()
    {
        var location = Game1.currentLocation;
        if (location is null)
            return;

        try
        {
            Game1.UpdateGameClock(NpcChatZeroGameTime);
            Game1.updateWeather(NpcChatVisualRefreshGameTime);
            UpdateAmbientLightingMethod?.Invoke(location, null);
        }
        catch (Exception ex)
        {
            if (_npcChatVisualRefreshFailedLogged)
                return;

            _npcChatVisualRefreshFailedLogged = true;
            Monitor.Log($"NPC chat lighting refresh failed: {ex.Message}", LogLevel.Trace);
        }
    }

    private void TryUpdateNpcChatLocationVisuals(double elapsedMs)
    {
        var location = Game1.currentLocation;
        if (location is null)
            return;

        var ms = Math.Clamp(elapsedMs, 1d, 250d);
        var gameTime = new GameTime(TimeSpan.FromMilliseconds(ms), TimeSpan.FromMilliseconds(ms));

        try
        {
            Game1.UpdateGameClock(gameTime);
            Game1.updateWeather(gameTime);
            UpdateAmbientLightingMethod?.Invoke(location, null);
        }
        catch (Exception ex)
        {
            if (_npcChatLocationUpdateFailedLogged)
                return;

            _npcChatLocationUpdateFailedLogged = true;
            Monitor.Log($"NPC chat location visual update failed: {ex.Message}", LogLevel.Trace);
        }
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        SyncCalendarSeasonFromWorld();
        SyncPlayer2DeviceAuthModal();
        TryAdvanceClockWhileNpcChatOpen();
        TrackLateNightPassOutWindow();
        TryCaptureTownIncidents();
        TryCaptureLiveWorldEvents();
        TryCaptureVanillaDialogueContextFromMenu(Game1.activeClickableMenu, _pendingNpcDialogueHookName);
        TryHandleNpcDialogueHookFallback();

        if (_config.EnablePlayer2 && _config.AutoConnectPlayer2OnLoad)
        {
            var shouldAttempt = string.IsNullOrWhiteSpace(_player2Key)
                || string.IsNullOrWhiteSpace(_activeNpcId)
                || !_player2StreamDesired
                || !IsPlayer2RosterReady();
            if (shouldAttempt && DateTime.UtcNow - _player2LastAutoConnectAttemptUtc > TimeSpan.FromSeconds(20))
                StartPlayer2AutoConnect("auto-retry", force: false);
        }

        TryTriggerAmbientNpcConversation();

        if (e.IsMultipleOf(60))
            TryRunAutomaticNpcCommandExposureHooks();

        TryApplyCompletedNewspaperIssues();
        TryApplyCompletedNpcPublishHeadlineUpdates();
        TryRefreshPendingNewspaperIssue("update-tick");

        if (_player2ReadInFlight == 1 && _player2ReadStartedUtc != default)
        {
            var elapsed = DateTime.UtcNow - _player2ReadStartedUtc;
            if (elapsed > TimeSpan.FromSeconds(15))
            {
                _player2ReadCts?.Cancel();
                Interlocked.Exchange(ref _player2ReadInFlight, 0);
                _pendingPlayer2Lines.Enqueue("__ERR__Timed out waiting for Player2 stream line (>15s). Read state reset.");
            }
        }

        if (_player2StreamDesired
            && _player2Client is not null
            && !string.IsNullOrWhiteSpace(_player2Key)
            && Interlocked.CompareExchange(ref _player2StreamRunning, 0, 0) == 0
            && DateTime.UtcNow >= _player2NextReconnectUtc)
        {
            StartPlayer2StreamListenerAttempt();
        }

        if (_player2PendingResponseCount > 0
            && _player2LastChatSentUtc != default
            && DateTime.UtcNow - _player2LastChatSentUtc > TimeSpan.FromSeconds(25)
            && DateTime.UtcNow - _player2LastStreamRecoveryUtc > TimeSpan.FromSeconds(15))
        {
            _player2LastStreamRecoveryUtc = DateTime.UtcNow;

            if (_player2WatchdogWindowStartUtc == default || DateTime.UtcNow - _player2WatchdogWindowStartUtc > TimeSpan.FromMinutes(3))
            {
                _player2WatchdogWindowStartUtc = DateTime.UtcNow;
                _player2WatchdogRecoveries = 0;
            }

            _player2WatchdogRecoveries += 1;

            if (_streamChatAwaitingResponse
                && string.IsNullOrWhiteSpace(_pendingStreamReplayMessage)
                && !string.IsNullOrWhiteSpace(_lastStreamChatMessage))
            {
                QueuePendingStreamReplay(
                    _lastStreamChatMessage!,
                    _lastStreamChatTargetNpcId,
                    _lastStreamChatRequesterShortName,
                    _lastStreamChatSenderNameOverride,
                    _lastStreamChatContextTag,
                    "watchdog");
            }

            _player2UiStatus = "No NPC response yet; recovering stream and retrying request...";
            Monitor.Log($"Player2 response watchdog: no stream line after chat; restarting listener (attempt {_player2WatchdogRecoveries}).", LogLevel.Warn);

            _player2StreamDesired = true;
            _player2StreamCts?.Cancel();
            _player2StreamCts = null;
            Interlocked.Exchange(ref _player2StreamRunning, 0);
            _player2StreamConnectedUtc = default;
            _player2PendingResponseCount = 0;
            ResetNpcResponseTracking();
            _player2StreamBackoffSec = Math.Min(Math.Max(2, _player2StreamBackoffSec * 2), 30);
            _player2NextReconnectUtc = DateTime.UtcNow.AddSeconds(_player2StreamBackoffSec);

            if (_player2WatchdogRecoveries >= 6)
            {
                Monitor.Log("Player2 watchdog escalation: forcing session refresh (respawn + reconnect).", LogLevel.Error);
                _player2UiStatus = "Town AI stalled. Refreshing NPC sessions and retrying your request...";

                var replayPrompt = _pendingUiMayorWorkRequest ?? _lastUiWorkPrompt;
                var replayRequester = _pendingUiRequesterShortName ?? _lastUiWorkRequesterShortName;

                _activeNpcId = null;
                _player2NpcIdsByShortName.Clear();
                _player2NpcShortNameById.Clear();
                _player2PendingResponseCount = 0;
                _pendingUiMayorWorkRequest = replayPrompt;
                _pendingUiRequesterShortName = replayRequester;
                _player2WatchdogRecoveries = 0;
                _player2WatchdogWindowStartUtc = DateTime.UtcNow;
                _streamChatAwaitingResponse = false;

                StartPlayer2AutoConnect("watchdog-escalation", force: true);
            }
        }

        if (!string.IsNullOrWhiteSpace(_pendingStreamReplayMessage)
            && !string.IsNullOrWhiteSpace(_player2Key)
            && !string.IsNullOrWhiteSpace(_activeNpcId)
            && IsPlayer2StreamReadyForChat())
        {
            if (_pendingStreamReplayQueuedUtc != default
                && DateTime.UtcNow - _pendingStreamReplayQueuedUtc > TimeSpan.FromMinutes(2))
            {
                Monitor.Log("Dropped stale pending stream replay (>2 minutes old).", LogLevel.Warn);
                ClearPendingStreamReplay();
            }
            else
            {
                var replayMessage = _pendingStreamReplayMessage!;
                var replayTargetNpcId = _pendingStreamReplayTargetNpcId;
                var replayRequester = _pendingStreamReplayRequesterShortName;
                var replaySender = _pendingStreamReplaySenderNameOverride;
                var replayContext = _pendingStreamReplayContextTag;

                if (!string.IsNullOrWhiteSpace(replayRequester)
                    && _player2NpcIdsByShortName.TryGetValue(replayRequester, out var remappedNpcId))
                {
                    replayTargetNpcId = remappedNpcId;
                }
                else if (string.IsNullOrWhiteSpace(replayTargetNpcId))
                {
                    replayTargetNpcId = _activeNpcId;
                }

                if (string.Equals(replayContext, "player_request_board", StringComparison.OrdinalIgnoreCase))
                {
                    _pendingUiMayorWorkRequest = null;
                    _pendingUiRequesterShortName = null;
                }

                ClearPendingStreamReplay();
                SendPlayer2ChatInternal(
                    replayMessage,
                    replayTargetNpcId,
                    replayRequester,
                    senderNameOverride: replaySender,
                    contextTag: replayContext,
                    captureForPlayerChat: false);
                Monitor.Log("Replayed pending stream chat after listener recovery.", LogLevel.Warn);
            }
        }

        if (!string.IsNullOrWhiteSpace(_pendingUiMayorWorkRequest)
            && !string.IsNullOrWhiteSpace(_player2Key)
            && !string.IsNullOrWhiteSpace(_activeNpcId)
            && Interlocked.CompareExchange(ref _player2StreamRunning, 0, 0) == 1)
        {
            var req = _pendingUiMayorWorkRequest!;
            var requester = _pendingUiRequesterShortName;

            // If a specific requester was selected, wait until that NPC session exists
            // so we don't accidentally send to fallback Lewis during reconnect.
            if (!string.IsNullOrWhiteSpace(requester)
                && !_player2NpcIdsByShortName.ContainsKey(requester))
            {
                // keep pending until roster spawn catches up
            }
            else
            {
                _pendingUiMayorWorkRequest = null;
                _pendingUiRequesterShortName = null;

                if (!string.IsNullOrWhiteSpace(requester) && _player2NpcIdsByShortName.TryGetValue(requester, out var pendingNpcId))
                {
                    _uiBoardSearchNpcId = pendingNpcId;
                    SendPlayer2ChatInternal(req, pendingNpcId, requester, contextTag: "player_request_board", captureForPlayerChat: false);
                }
                else
                {
                    _uiBoardSearchNpcId = _activeNpcId;
                    SendPlayer2ChatInternal(req, contextTag: "player_request_board", captureForPlayerChat: false);
                }
            }
        }

        while (_pendingPlayer2Lines.TryDequeue(out var line))
        {
            if (line.StartsWith("__ERR__", StringComparison.Ordinal))
            {
                Monitor.Log($"Player2 read failed: {line[7..]}", LogLevel.Error);
                continue;
            }

            if (line == "__EMPTY__")
            {
                Monitor.Log("No response line received (timeout/empty).", LogLevel.Trace);
                continue;
            }

            var streamNpcId = TryExtractNpcIdFromLine(line);
            if (!string.IsNullOrWhiteSpace(streamNpcId)
                && _npcLastPlayerChatRequestUtcById.TryGetValue(streamNpcId, out var recentPlayerChatUtc)
                && DateTime.UtcNow - recentPlayerChatUtc <= TimeSpan.FromSeconds(20))
            {
                var hasPendingAmbientRouting = _npcResponseRoutingById.TryGetValue(streamNpcId, out var routingQueue)
                    && !routingQueue.IsEmpty;
                if (!hasPendingAmbientRouting)
                {
                    Monitor.Log($"Ignored non-ambient stream line for NPC {streamNpcId}; player chat uses request/response path.", LogLevel.Trace);
                    continue;
                }
            }

            if (ShouldIgnoreLowInformationAmbientOutput(line, streamNpcId))
            {
                _state.Telemetry.Daily.AmbientLowInfoSuppressed += 1;
                Monitor.Log($"Ignored low-information ambient stream line for NPC {streamNpcId}.", LogLevel.Trace);
                continue;
            }

            Monitor.Log($"Player2 stream line: {line}", LogLevel.Trace);
            _player2LastLineUtc = DateTime.UtcNow;
            _player2StreamBackoffSec = 1;
            var routedToPlayerChat = CaptureNpcUiMessage(line, allowPlayerChatRouting: false);
            if (!routedToPlayerChat)
            {
                if (_player2PendingResponseCount > 0)
                    _player2PendingResponseCount -= 1;
                _streamChatAwaitingResponse = false;
                ClearPendingStreamReplay();
            }
            _player2WatchdogRecoveries = 0;
            _player2WatchdogWindowStartUtc = default;
            var appliedNpcCommand = TryApplyNpcCommandFromLine(line);
            TryResolveUiBoardSearchStatusFromStreamLine(streamNpcId, line, appliedNpcCommand);
        }

        while (_pendingPlayer2ChatLines.TryDequeue(out var line))
        {
            if (line.StartsWith("__ERR__", StringComparison.Ordinal))
            {
                Monitor.Log($"Player2 chat read failed: {line[7..]}", LogLevel.Error);
                continue;
            }

            if (line == "__EMPTY__")
            {
                Monitor.Log("No player chat response line received (timeout/empty).", LogLevel.Trace);
                continue;
            }

            Monitor.Log($"Player2 chat line: {line}", LogLevel.Trace);
            CaptureNpcUiMessage(line, allowPlayerChatRouting: true);
            var appliedNpcCommand = TryApplyNpcCommandFromLine(line);
            if (!appliedNpcCommand)
                TryApplyFallbackQuestFromPlayerChatLine(line);
        }
    }

    private void ResetAmbientNpcConversationScheduleForDay()
    {
        _ambientNpcConversationDay = _state.Calendar.Day;
        _ambientNpcConversationsToday = 0;
        _ambientNpcLastConversationUtcByNpcId.Clear();
        _nextAmbientNpcConversationUtc = DateTime.UtcNow.AddSeconds(_ambientNpcRandom.Next(120, 360));
    }

    private void TryTriggerAmbientNpcConversation()
    {
        if (!_config.EnablePlayer2)
            return;

        if (_ambientNpcConversationDay != _state.Calendar.Day)
            ResetAmbientNpcConversationScheduleForDay();

        if (_ambientNpcConversationsToday >= 3)
            return;

        if (_nextAmbientNpcConversationUtc == default)
            _nextAmbientNpcConversationUtc = DateTime.UtcNow.AddSeconds(_ambientNpcRandom.Next(120, 360));

        if (DateTime.UtcNow < _nextAmbientNpcConversationUtc)
            return;

        if (!IsPlayer2ReadyForNewspaper())
            return;

        if (_player2PendingResponseCount > 0 || !string.IsNullOrWhiteSpace(_pendingUiMayorWorkRequest))
            return;

        if (Interlocked.CompareExchange(ref _ambientNpcConversationInFlight, 1, 0) == 1)
            return;

        try
        {
            if (!TryPickAmbientNpcConversationPair(out var speakerShortName, out var speakerNpcId, out var listenerShortName))
                return;
            if (IsAmbientNpcConversationCoolingDown(speakerNpcId))
            {
                Monitor.Log($"Ambient NPC cooldown active for speaker {speakerShortName}; skipping this tick.", LogLevel.Trace);
                return;
            }

            string? listenerNpcId = null;
            var hasListenerNpcId = !string.IsNullOrWhiteSpace(listenerShortName)
                && _player2NpcIdsByShortName.TryGetValue(listenerShortName, out listenerNpcId);
            if (hasListenerNpcId && listenerNpcId is not null && IsAmbientNpcConversationCoolingDown(listenerNpcId))
            {
                Monitor.Log($"Ambient NPC cooldown active for listener {listenerShortName}; skipping this tick.", LogLevel.Trace);
                return;
            }

            var ambientWeights = _npcSpeechStyleService?.GetAmbientBehaviorWeights(speakerShortName);
            var ambientArchetypeRule = ambientWeights is null
                ? string.Empty
                : $"Archetype={ambientWeights.Archetype} weights(event={ambientWeights.EventWeight}, memory={ambientWeights.MemoryWeight}, publish={ambientWeights.PublishWeight}). Favor higher-weight actions when context supports them. ";
            var promptLanguageRule = I18n.BuildPromptLanguageInstruction();

            var prompt =
                $"{speakerShortName}, you had a brief offscreen conversation with {listenerShortName} about today's town happenings. " +
                "Stay in-character and reply naturally. " +
                promptLanguageRule + " " +
                "Keep command names and argument keys in English; localize only message/topic/title/content string values. " +
                "Ambient policy for this context: prefer record_town_event first and keep command usage sparse. " +
                "Allowed command set here is record_town_event, record_memory_fact, publish_rumor, publish_article unless policy unlocks additional commands. " +
                "If anything notable happened, capture it with record_town_event before considering publish commands. " +
                ambientArchetypeRule +
                "For record_town_event, provide complete fields: kind, summary, location, severity, visibility, and tags. " +
                "Use publish commands only when confidence and visibility are strong enough for public sharing. " +
                "If there is meaningful concrete news, use publish_article with a concise title/content/category (title+content must be <= 100 characters total). " +
                "If it is gossip-level information, use publish_rumor with topic, optional title/content, confidence, and target_group (title+content must be <= 100 characters total when provided). " +
                "Do not force a command when nothing notable happened.";

            SendPlayer2ChatInternal(
                prompt,
                speakerNpcId,
                speakerShortName,
                senderNameOverride: listenerShortName,
                contextTag: "npc_to_npc_ambient",
                captureForPlayerChat: false);
            _ambientNpcConversationsToday += 1;
            MarkAmbientNpcConversationTimestamp(speakerNpcId);
            if (hasListenerNpcId)
                MarkAmbientNpcConversationTimestamp(listenerNpcId!);
            Monitor.Log($"Ambient NPC conversation triggered: {speakerShortName} -> {listenerShortName}.", LogLevel.Trace);
        }
        finally
        {
            Interlocked.Exchange(ref _ambientNpcConversationInFlight, 0);
            _nextAmbientNpcConversationUtc = DateTime.UtcNow.AddSeconds(_ambientNpcRandom.Next(180, 480));
        }
    }

    private bool IsAmbientNpcConversationCoolingDown(string npcId)
    {
        if (string.IsNullOrWhiteSpace(npcId))
            return false;

        if (!_ambientNpcLastConversationUtcByNpcId.TryGetValue(npcId, out var lastUtc))
            return false;

        return DateTime.UtcNow - lastUtc < TimeSpan.FromMinutes(AmbientNpcCooldownMinutes);
    }

    private void MarkAmbientNpcConversationTimestamp(string npcId)
    {
        if (string.IsNullOrWhiteSpace(npcId))
            return;

        _ambientNpcLastConversationUtcByNpcId[npcId] = DateTime.UtcNow;
    }

    private bool TryPickAmbientNpcConversationPair(out string speakerShortName, out string speakerNpcId, out string listenerShortName)
    {
        speakerShortName = string.Empty;
        speakerNpcId = string.Empty;
        listenerShortName = string.Empty;

        var roster = _player2NpcIdsByShortName
            .Where(kv => !string.IsNullOrWhiteSpace(kv.Key) && !string.IsNullOrWhiteSpace(kv.Value))
            .Select(kv => kv.Key)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (roster.Count == 0)
        {
            if (string.IsNullOrWhiteSpace(_activeNpcId))
                return false;

            speakerShortName = "Lewis";
            speakerNpcId = _activeNpcId!;
            listenerShortName = "Town";
            return true;
        }

        var speakerIndex = _ambientNpcRandom.Next(roster.Count);
        speakerShortName = roster[speakerIndex];
        if (!_player2NpcIdsByShortName.TryGetValue(speakerShortName, out var speakerId))
            return false;
        speakerNpcId = speakerId;

        if (roster.Count == 1)
        {
            listenerShortName = "Town";
            return true;
        }

        var listeners = new List<string>();
        foreach (var candidate in roster)
        {
            if (!candidate.Equals(speakerShortName, StringComparison.OrdinalIgnoreCase))
                listeners.Add(candidate);
        }
        if (listeners.Count == 0)
        {
            listenerShortName = "Town";
            return true;
        }

        listenerShortName = listeners[_ambientNpcRandom.Next(listeners.Count)];
        return true;
    }

    private void TryCaptureTownIncidents()
    {
        if (_townMemoryService is null || Game1.player is null || Game1.currentLocation is null)
            return;

        if (Game1.player.health > 0)
            return;

        var loc = Game1.currentLocation.Name ?? "unknown";
        var isMineLike = loc.Contains("Mine", StringComparison.OrdinalIgnoreCase)
            || loc.Contains("Cave", StringComparison.OrdinalIgnoreCase)
            || loc.Contains("Skull", StringComparison.OrdinalIgnoreCase);

        if (!isMineLike)
            return;

        var key = $"town_incident:faint:{_state.Calendar.Day}";
        if (_state.Facts.Facts.ContainsKey(key))
            return;

        _state.Facts.Facts[key] = new FactValue { Value = true, SetDay = _state.Calendar.Day, Source = "system" };
        _townMemoryService.RecordEvent(
            _state,
            "fainting",
            "Player fainted in the caves recently.",
            loc,
            _state.Calendar.Day,
            severity: 3,
            visibility: "local",
            "mines", "health", "rescue");
    }

    private void TryCaptureLiveWorldEvents()
    {
        if (_townMemoryService is null || !Context.IsWorldReady || !Game1.eventUp || Game1.CurrentEvent is null)
            return;
        if (!_config.EnableCustomNpcFramework || _customNpcRegistry is null || _customNpcRegistry.NpcsByToken.Count == 0)
            return;

        var eventObject = (object)Game1.CurrentEvent;
        var actorNames = ExtractWorldEventActorNames(eventObject);
        if (actorNames.Count == 0)
            return;

        var customActors = actorNames
            .Where(IsCustomNpcNameOrAlias)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (customActors.Count == 0)
            customActors = FindCustomNpcNamesInEventCommands(eventObject, maxMatches: 2);
        if (customActors.Count == 0)
            return;

        var locationName = Game1.currentLocation?.Name ?? "Town";
        var identity = BuildWorldEventIdentity(eventObject, locationName, actorNames);
        if (string.IsNullOrWhiteSpace(identity))
            return;

        var hash = Math.Abs(identity.GetHashCode()) % 1000000;
        var factKey = $"town:event:world:{_state.Calendar.Day}:{hash}";
        if (_state.Facts.Facts.ContainsKey(factKey))
            return;

        _state.Facts.Facts[factKey] = new FactValue
        {
            Value = true,
            SetDay = _state.Calendar.Day,
            Source = "world_event"
        };

        var sourceNpc = ResolvePreferredWorldEventSourceNpc(customActors);
        var summary = BuildWorldEventSummary(customActors, locationName);
        var visibility = IsLikelyPublicTownLocation(locationName) ? "public" : "local";
        var severity = customActors.Count >= 2 ? 3 : 2;
        var tags = BuildWorldEventTags(customActors, locationName);

        _townMemoryService.RecordEvent(
            _state,
            "community",
            summary,
            locationName,
            _state.Calendar.Day,
            severity,
            visibility,
            sourceNpc,
            tags);

        if (_npcMemoryService is not null && !string.IsNullOrWhiteSpace(sourceNpc))
            _npcMemoryService.WriteFact(_state, sourceNpc, "event", summary, _state.Calendar.Day, weight: 3);
    }

    private List<string> FindCustomNpcNamesInEventCommands(object eventObject, int maxMatches)
    {
        var found = new List<string>();
        if (_customNpcRegistry is null || _customNpcRegistry.NpcsByToken.Count == 0)
            return found;
        if (!TryGetMemberValue(eventObject, "eventCommands", out var rawCommands) || rawCommands is null)
            return found;

        var commands = new List<string>();
        if (rawCommands is IEnumerable<string> typed)
        {
            commands.AddRange(typed.Where(c => !string.IsNullOrWhiteSpace(c)).Take(80));
        }
        else if (rawCommands is System.Collections.IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                var text = item?.ToString();
                if (string.IsNullOrWhiteSpace(text))
                    continue;
                commands.Add(text);
                if (commands.Count >= 80)
                    break;
            }
        }

        if (commands.Count == 0)
            return found;

        var mergedText = string.Join(' ', commands);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var npc in _customNpcRegistry.NpcsByToken.Values)
        {
            var mentioned =
                ContainsTargetToken(mergedText, npc.DisplayName)
                || ContainsTargetToken(mergedText, npc.NpcId)
                || npc.Aliases.Any(alias => ContainsTargetToken(mergedText, alias));
            if (!mentioned)
                continue;

            var canonicalName = string.IsNullOrWhiteSpace(npc.DisplayName) ? npc.NpcId : npc.DisplayName;
            if (string.IsNullOrWhiteSpace(canonicalName) || !seen.Add(canonicalName))
                continue;

            found.Add(canonicalName);
            if (found.Count >= Math.Max(1, maxMatches))
                break;
        }

        return found;
    }

    private bool IsCustomNpcNameOrAlias(string? rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName) || _customNpcRegistry is null)
            return false;

        return _customNpcRegistry.TryGetNpcByName(rawName, out _);
    }

    private string ResolvePreferredWorldEventSourceNpc(IReadOnlyList<string> actorNames)
    {
        if (_customNpcRegistry is null)
            return string.Empty;

        foreach (var actor in actorNames)
        {
            if (!_customNpcRegistry.TryGetNpcByName(actor, out var npc))
                continue;

            var display = (npc.DisplayName ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(display))
                return display;
        }

        return string.Empty;
    }

    private static string BuildWorldEventSummary(IReadOnlyList<string> actors, string locationName)
    {
        var names = actors
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .Select(a => a.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(2)
            .ToArray();
        var location = string.IsNullOrWhiteSpace(locationName) ? "Town" : locationName;

        if (names.Length == 0)
            return $"A live town event just happened near {location}.";
        if (names.Length == 1)
            return $"A live town event involving {names[0]} just happened near {location}.";

        return $"A live town event involving {names[0]} and {names[1]} just happened near {location}.";
    }

    private static bool IsLikelyPublicTownLocation(string locationName)
    {
        if (string.IsNullOrWhiteSpace(locationName))
            return false;

        var token = locationName.ToLowerInvariant();
        return token.Contains("town", StringComparison.Ordinal)
               || token.Contains("saloon", StringComparison.Ordinal)
               || token.Contains("blacksmith", StringComparison.Ordinal)
               || token.Contains("museum", StringComparison.Ordinal)
               || token.Contains("beach", StringComparison.Ordinal)
               || token.Contains("forest", StringComparison.Ordinal)
               || token.Contains("mountain", StringComparison.Ordinal)
               || token.Contains("busstop", StringComparison.Ordinal);
    }

    private static string[] BuildWorldEventTags(IReadOnlyList<string> actors, string locationName)
    {
        var tags = new List<string>
        {
            "world_event",
            "source_mod_event",
            "custom_npc"
        };

        tags.Add(NormalizeTargetToken(locationName));
        foreach (var actor in actors)
            tags.Add(NormalizeTargetToken(actor));

        return tags
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToArray();
    }

    private static string BuildWorldEventIdentity(object eventObject, string locationName, IReadOnlyList<string> actorNames)
    {
        var eventId = TryReadEventMemberAsString(eventObject, "id");
        if (string.IsNullOrWhiteSpace(eventId))
            eventId = TryReadEventMemberAsString(eventObject, "eventId");
        if (string.IsNullOrWhiteSpace(eventId))
            eventId = BuildWorldEventCommandSignature(eventObject);

        var actorToken = string.Join(
            ",",
            actorNames
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .Select(NormalizeTargetToken)
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .OrderBy(a => a, StringComparer.OrdinalIgnoreCase)
                .Take(4));

        return $"{NormalizeTargetToken(locationName)}|{eventId}|{actorToken}";
    }

    private static string BuildWorldEventCommandSignature(object eventObject)
    {
        if (!TryGetMemberValue(eventObject, "eventCommands", out var rawCommands) || rawCommands is null)
            return "runtime";

        var sample = new List<string>();

        if (rawCommands is IEnumerable<string> typedCommands)
        {
            sample.AddRange(typedCommands
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim())
                .Take(4));
        }
        else if (rawCommands is System.Collections.IEnumerable rawEnumerable)
        {
            foreach (var item in rawEnumerable)
            {
                var text = item?.ToString();
                if (string.IsNullOrWhiteSpace(text))
                    continue;
                sample.Add(text.Trim());
                if (sample.Count >= 4)
                    break;
            }
        }

        if (sample.Count == 0)
            return "runtime";

        var hash = Math.Abs(string.Join('|', sample).GetHashCode()) % 1000000;
        return $"cmd_{hash}";
    }

    private static List<string> ExtractWorldEventActorNames(object eventObject)
    {
        var names = new List<string>();

        TryAppendNamesFromMember(eventObject, "actors", names);
        TryAppendNamesFromMember(eventObject, "actorNames", names);
        TryAppendNamesFromMember(eventObject, "farmerActors", names);

        return names
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n.Trim())
            .Where(n => !n.Equals("farmer", StringComparison.OrdinalIgnoreCase)
                        && !n.Equals("player", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void TryAppendNamesFromMember(object source, string memberName, List<string> names)
    {
        if (!TryGetMemberValue(source, memberName, out var value) || value is null)
            return;

        AppendNamesFromUnknownValue(value, names);
    }

    private static void AppendNamesFromUnknownValue(object rawValue, List<string> names)
    {
        switch (rawValue)
        {
            case NPC npc:
                AddName(names, npc.Name);
                AddName(names, npc.displayName);
                return;
            case string text:
                AddName(names, text);
                return;
            case IEnumerable<string> strings:
                foreach (var s in strings)
                    AddName(names, s);
                return;
            case IEnumerable<NPC> npcs:
                foreach (var n in npcs)
                {
                    AddName(names, n?.Name);
                    AddName(names, n?.displayName);
                }
                return;
            case System.Collections.IDictionary dictionary:
                foreach (System.Collections.DictionaryEntry entry in dictionary)
                {
                    AddName(names, entry.Key?.ToString());
                    if (entry.Value is NPC entryNpc)
                    {
                        AddName(names, entryNpc.Name);
                        AddName(names, entryNpc.displayName);
                    }
                    else
                    {
                        AddName(names, entry.Value?.ToString());
                    }
                }
                return;
            case System.Collections.IEnumerable enumerable:
                foreach (var item in enumerable)
                {
                    if (item is null)
                        continue;
                    if (item is NPC itemNpc)
                    {
                        AddName(names, itemNpc.Name);
                        AddName(names, itemNpc.displayName);
                        continue;
                    }
                    if (item is string itemText)
                    {
                        AddName(names, itemText);
                        continue;
                    }

                    if (TryReadObjectMemberAsString(item, "Name", out var reflectedName))
                        AddName(names, reflectedName);
                    if (TryReadObjectMemberAsString(item, "displayName", out var reflectedDisplay))
                        AddName(names, reflectedDisplay);
                }
                return;
            default:
                AddName(names, rawValue.ToString());
                return;
        }
    }

    private static bool TryReadObjectMemberAsString(object source, string memberName, out string value)
    {
        value = string.Empty;
        if (!TryGetMemberValue(source, memberName, out var raw) || raw is null)
            return false;

        value = (raw.ToString() ?? string.Empty).Trim();
        return !string.IsNullOrWhiteSpace(value);
    }

    private static string TryReadEventMemberAsString(object source, string memberName)
    {
        if (!TryGetMemberValue(source, memberName, out var raw) || raw is null)
            return string.Empty;

        return raw switch
        {
            string s => s.Trim(),
            int i => i.ToString(CultureInfo.InvariantCulture),
            long l => l.ToString(CultureInfo.InvariantCulture),
            _ => (raw.ToString() ?? string.Empty).Trim()
        };
    }

    private static bool TryGetMemberValue(object source, string memberName, out object? value)
    {
        value = null;
        if (source is null || string.IsNullOrWhiteSpace(memberName))
            return false;

        const BindingFlags flags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;

        var type = source.GetType();
        var prop = type.GetProperty(memberName, flags);
        if (prop is not null)
        {
            value = prop.GetValue(source);
            return true;
        }

        var field = type.GetField(memberName, flags);
        if (field is null)
            return false;

        value = field.GetValue(source);
        return true;
    }

    private static void AddName(List<string> names, string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return;

        var value = raw.Trim();
        if (!string.IsNullOrWhiteSpace(value))
            names.Add(value);
    }

    private void TrackLateNightPassOutWindow()
    {
        if (_townMemoryService is null || Game1.player is null || Game1.currentLocation is null)
            return;

        if (Game1.timeOfDay < 2600)
            return;

        if (Game1.player.health <= 0)
            return;

        if (_pendingLateNightPassOutDay == _state.Calendar.Day)
            return;

        var locationName = Game1.currentLocation.Name ?? "Town";
        if (IsSleepSafeLocation(locationName))
            return;

        _pendingLateNightPassOutDay = _state.Calendar.Day;
        _pendingLateNightPassOutLocation = locationName;
    }

    private void TryCapturePendingLateNightPassOut()
    {
        if (_townMemoryService is null)
            return;

        if (_pendingLateNightPassOutDay < 0)
            return;

        var currentDay = _state.Calendar.Day;
        var isCurrentDayCapture = _pendingLateNightPassOutDay == currentDay;
        var isYesterdayCapture = _pendingLateNightPassOutDay == currentDay - 1;
        if (!isCurrentDayCapture && !isYesterdayCapture)
            return;

        var locationName = string.IsNullOrWhiteSpace(_pendingLateNightPassOutLocation)
            ? "Town"
            : _pendingLateNightPassOutLocation;
        var key = $"town_incident:passout:{_pendingLateNightPassOutDay}";
        if (!_state.Facts.Facts.ContainsKey(key))
        {
            _state.Facts.Facts[key] = new FactValue { Value = true, SetDay = _pendingLateNightPassOutDay, Source = "system" };
            _townMemoryService.RecordEvent(
                _state,
                "pass_out",
                $"A farmer was found passed out late at night near {locationName}.",
                locationName,
                _pendingLateNightPassOutDay,
                severity: 2,
                visibility: "public",
                "late-night", "pass-out", "rescue");
        }

        _pendingLateNightPassOutDay = -1;
        _pendingLateNightPassOutLocation = "Town";
    }

    private static bool IsSleepSafeLocation(string locationName)
    {
        if (string.IsNullOrWhiteSpace(locationName))
            return false;

        return locationName.Contains("FarmHouse", StringComparison.OrdinalIgnoreCase)
            || locationName.Contains("Cabin", StringComparison.OrdinalIgnoreCase);
    }

    private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        if (e.NewMenu is not null)
        {
            TryArmNpcDialogueHookFromMenu(e.NewMenu);
            TrySyncNpcDialogueHookTargetFromMenu(e.NewMenu);
        }

        if (!_npcDialogueHookArmed)
            return;

        // Wait until dialogue/menu closes, then append our choice as a follow-up question.
        if (e.NewMenu is not null)
        {
            _npcDialogueHookMenuOpened = true;
            TryCaptureVanillaDialogueContextFromMenu(e.NewMenu, _pendingNpcDialogueHookName);
            return;
        }

        TryCaptureVanillaDialogueContextFromMenu(e.OldMenu, _pendingNpcDialogueHookName);

        if (string.IsNullOrWhiteSpace(_pendingNpcDialogueHookName))
        {
            ClearNpcDialogueHook();
            return;
        }

        var requesterName = _pendingNpcDialogueHookName;
        var loc = Game1.currentLocation;
        var npc = ResolvePendingNpcDialogueHookNpc(requesterName, loc);
        var suppressFirstInteractionGreeting = _npcDialogueHookMenuOpened;
        ClearNpcDialogueHook();
        if (npc is null)
            return;

        TryRecordSocialVisitProgress(npc.Name);
        OpenNpcFollowUpDialogue(loc!, npc, suppressFirstInteractionGreeting);
    }

    private void TryHandleNpcDialogueHookFallback()
    {
        if (!_npcDialogueHookArmed || _npcDialogueHookMenuOpened)
            return;

        if (string.IsNullOrWhiteSpace(_pendingNpcDialogueHookName))
        {
            ClearNpcDialogueHook();
            return;
        }

        if (Game1.eventUp || Game1.dialogueUp || Game1.activeClickableMenu is not null)
            return;

        // Give vanilla interaction a brief moment to open dialogue/menu first.
        if (_npcDialogueHookArmedUtc != default
            && DateTime.UtcNow - _npcDialogueHookArmedUtc < TimeSpan.FromMilliseconds(350))
            return;

        var requesterName = _pendingNpcDialogueHookName;
        var loc = Game1.currentLocation;
        var npc = ResolvePendingNpcDialogueHookNpc(requesterName, loc);
        if (npc is null || Vector2.Distance(npc.Tile, Game1.player.Tile) > NpcDialogueHookFallbackRadiusTiles)
        {
            ClearNpcDialogueHook();
            return;
        }

        ClearNpcDialogueHook();
        TryRecordSocialVisitProgress(npc.Name);
        OpenNpcFollowUpDialogue(loc!, npc);
    }

    private void OpenNpcFollowUpDialogue(GameLocation loc, NPC npc, bool suppressFirstInteractionGreeting = false)
    {
        if (TryCreateRosterTalkDialogue(loc, npc, suppressFirstInteractionGreeting))
            return;

        var responses = new List<Response>();
        if (HasPendingQuestForNpc(npc.Name ?? npc.displayName))
            responses.Add(new Response("town_word", "What's the word around town?"));
        responses.Add(new Response("talk", "Got a minute to chat?"));
        responses.Add(new Response("later", "Catch you later!"));

        loc.createQuestionDialogue(
            $"{npc.displayName}: {BuildNpcFollowUpGreeting(npc, suppressFirstInteractionGreeting)}",
            responses.ToArray(),
            (_, answer) =>
            {
                if (string.Equals(answer, "town_word", StringComparison.OrdinalIgnoreCase))
                {
                    if (HasPendingQuestForNpc(npc.Name ?? npc.displayName))
                        OpenRumorBoard();
                    else if (_state.Newspaper.Issues.Any())
                        OpenNewspaper();
                    else
                        Game1.drawObjectDialogue($"{npc.displayName}: Quiet day so far. Nothing urgent on my side.");
                    return;
                }

                if (string.Equals(answer, "talk", StringComparison.OrdinalIgnoreCase))
                    OpenNpcChatMenu(
                        npc,
                        initialPlayerMessage: InitialNpcChatPrompt,
                        autoSendInitialPlayerMessage: true,
                        defaultContextTag: "player_chat_followup");
            },
            npc);
    }

    private void ClearNpcDialogueHook()
    {
        _pendingNpcDialogueHookName = null;
        _pendingNpcDialogueHookNpc = null;
        _npcDialogueHookArmed = false;
        _npcDialogueHookMenuOpened = false;
        _npcDialogueHookArmedUtc = default;
    }

    private void TryRecordSocialVisitProgress(string? npcName)
    {
        if (_rumorBoardService is null || string.IsNullOrWhiteSpace(npcName))
            return;

        var completedVisits = _rumorBoardService.RecordSocialVisitProgress(_state, npcName);
        if (completedVisits > 0)
            Monitor.Log($"Social visit progress updated for {npcName} ({completedVisits} request(s)).", LogLevel.Trace);
    }

    private void OnRenderedHud(object? sender, RenderedHudEventArgs e)
    {
        if (!Context.IsWorldReady || Game1.eventUp || Game1.activeClickableMenu is not null)
            return;

        var connected = IsLocalInsightHudActive();
        var rect = GetPlayer2HudRect();

        var fill = connected ? new Color(126, 170, 86) : new Color(106, 88, 74);
        var border = connected ? new Color(54, 86, 34) : new Color(64, 50, 40);
        const int borderThickness = 4;
        const int shadowOffset = 3;

        var shadowRect = new Rectangle(rect.X + shadowOffset, rect.Y + shadowOffset, rect.Width, rect.Height);
        e.SpriteBatch.Draw(Game1.staminaRect, shadowRect, Color.Black * 0.35f);

        e.SpriteBatch.Draw(Game1.staminaRect, rect, fill);
        e.SpriteBatch.Draw(Game1.staminaRect, new Rectangle(rect.X, rect.Y, rect.Width, borderThickness), border);
        e.SpriteBatch.Draw(Game1.staminaRect, new Rectangle(rect.X, rect.Bottom - borderThickness, rect.Width, borderThickness), border);
        e.SpriteBatch.Draw(Game1.staminaRect, new Rectangle(rect.X, rect.Y, borderThickness, rect.Height), border);
        e.SpriteBatch.Draw(Game1.staminaRect, new Rectangle(rect.Right - borderThickness, rect.Y, borderThickness, rect.Height), border);

        var point = new Point(Game1.getMouseX(), Game1.getMouseY());
        if (rect.Contains(point))
        {
            var tooltip = connected
                ? I18n.Get("hud.local_insight.active", "Local Insight: Active")
                : I18n.Get("hud.local_insight.dormant", "Local Insight: Dormant (click to connect)");
            IClickableMenu.drawHoverText(e.SpriteBatch, tooltip, Game1.smallFont);
        }
    }

    private Rectangle GetPlayer2HudRect()
    {
        return new Rectangle(16, 16, 32, 32);
    }

    private bool IsLocalInsightHudActive()
    {
        if (string.IsNullOrWhiteSpace(_player2Key))
            return false;

        var streamRunning = Interlocked.CompareExchange(ref _player2StreamRunning, 0, 0) == 1;
        var connectInFlight = Interlocked.CompareExchange(ref _player2ConnectInFlight, 0, 0) == 1;
        return connectInFlight
            || !string.IsNullOrWhiteSpace(_activeNpcId)
            || _player2StreamDesired
            || streamRunning;
    }

    private void StartPlayer2AutoConnect(string reason, bool force)
    {
        if (!_config.EnablePlayer2 || _player2Client is null)
            return;

        if (string.Equals(reason, "hud-button", StringComparison.OrdinalIgnoreCase))
        {
            _player2AutoConnectSuppressedByUser = false;
        }
        else if (_player2AutoConnectSuppressedByUser)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(ResolvePlayer2GameClientId()))
        {
            _player2UiStatus = "Player2 disabled: missing built-in game client id.";
            if (_uiBoardSearchAwaitingResult)
                ClearUiBoardSearchAwaitingResult();
            return;
        }

        if (!force)
        {
            var alreadyConnected = !string.IsNullOrWhiteSpace(_player2Key)
                && !string.IsNullOrWhiteSpace(_activeNpcId)
                && (_player2StreamDesired || Interlocked.CompareExchange(ref _player2StreamRunning, 0, 0) == 1)
                && IsPlayer2RosterReady();
            if (alreadyConnected)
                return;
        }

        if (Interlocked.Exchange(ref _player2ConnectInFlight, 1) == 1)
            return;

        _player2LastAutoConnectAttemptUtc = DateTime.UtcNow;
        _player2UiStatus = $"Player2 connecting ({reason})...";

        _ = Task.Run(() =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_player2Key))
                    OnPlayer2LoginCommand("slrpg_p2_login", Array.Empty<string>());

                if (!string.IsNullOrWhiteSpace(_player2Key) && string.IsNullOrWhiteSpace(_activeNpcId))
                    OnPlayer2SpawnNpcCommand("slrpg_p2_spawn", Array.Empty<string>());

                if (!string.IsNullOrWhiteSpace(_player2Key) && !string.IsNullOrWhiteSpace(_activeNpcId))
                    SpawnAdditionalConfiguredNpcs();

                if (!string.IsNullOrWhiteSpace(_player2Key) && !_player2StreamDesired)
                    OnPlayer2StreamStartCommand("slrpg_p2_stream_start", Array.Empty<string>());

                var ok = !string.IsNullOrWhiteSpace(_player2Key)
                    && !string.IsNullOrWhiteSpace(_activeNpcId)
                    && (_player2StreamDesired || Interlocked.CompareExchange(ref _player2StreamRunning, 0, 0) == 1)
                    && IsPlayer2RosterReady();

                _player2UiStatus = ok ? "Player2 connected." : "Player2 partially connected. Click reconnect.";
            }
            catch (Exception ex)
            {
                _player2UiStatus = "Player2 connect failed: " + ex.Message;
                if (_uiBoardSearchAwaitingResult)
                    ClearUiBoardSearchAwaitingResult();
            }
            finally
            {
                Interlocked.Exchange(ref _player2ConnectInFlight, 0);
            }
        });
    }

    private void BeginPlayer2DeviceAuthUi(string verificationUrl, string userCode, int expiresInSeconds, CancellationTokenSource authCts)
    {
        var boundedSeconds = Math.Max(30, expiresInSeconds);
        lock (_player2DeviceAuthUiLock)
        {
            _player2DeviceAuthUiActive = true;
            _player2DeviceAuthVerificationUrl = verificationUrl ?? string.Empty;
            _player2DeviceAuthUserCode = userCode ?? string.Empty;
            _player2DeviceAuthStatus = "Waiting for authorization...";
            _player2DeviceAuthExpiresUtc = DateTime.UtcNow.AddSeconds(boundedSeconds);
            _player2DeviceAuthCts = authCts;
        }
    }

    private void UpdatePlayer2DeviceAuthUiStatus(string status)
    {
        lock (_player2DeviceAuthUiLock)
        {
            if (!_player2DeviceAuthUiActive)
                return;

            _player2DeviceAuthStatus = status ?? string.Empty;
        }
    }

    private void EndPlayer2DeviceAuthUi(string status)
    {
        lock (_player2DeviceAuthUiLock)
        {
            _player2DeviceAuthStatus = status ?? string.Empty;
            _player2DeviceAuthUiActive = false;
            _player2DeviceAuthCts = null;
        }
    }

    private void CancelPlayer2DeviceAuthFromUi()
    {
        CancellationTokenSource? authCts;
        lock (_player2DeviceAuthUiLock)
        {
            authCts = _player2DeviceAuthCts;
            _player2DeviceAuthStatus = "Authorization canceled.";
            _player2DeviceAuthUiActive = false;
        }

        _player2AutoConnectSuppressedByUser = true;
        _player2UiStatus = "Local Insight dormant. Click HUD square to connect.";
        authCts?.Cancel();
    }

    private void OpenPlayer2DeviceAuthBrowser()
    {
        string url;
        lock (_player2DeviceAuthUiLock)
            url = _player2DeviceAuthVerificationUrl;

        if (string.IsNullOrWhiteSpace(url))
            return;

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Monitor.Log($"Could not open browser for device auth: {ex.Message}", LogLevel.Warn);
        }
    }

    private void CopyPlayer2DeviceAuthCodeToClipboard()
    {
        var code = GetPlayer2DeviceAuthUserCode();
        if (string.IsNullOrWhiteSpace(code))
        {
            UpdatePlayer2DeviceAuthUiStatus("No code available to copy.");
            return;
        }

        if (TryCopyToClipboard(code.Trim()))
        {
            UpdatePlayer2DeviceAuthUiStatus("Code copied. Paste it into the approval page.");
            Game1.addHUDMessage(new HUDMessage(
                I18n.Get("hud.player2.code_copied", "Copied Player2 code to clipboard."),
                HUDMessage.newQuest_type));
        }
        else
        {
            UpdatePlayer2DeviceAuthUiStatus("Clipboard unavailable. Copy code manually.");
            Game1.addHUDMessage(new HUDMessage(
                I18n.Get("hud.player2.clipboard_unavailable", "Could not access clipboard."),
                HUDMessage.newQuest_type));
        }
    }

    private static bool TryCopyToClipboard(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        if (TryCopyViaType("StardewValley.BellsAndWhistles.DesktopClipboard", text))
            return true;

        if (TryCopyViaType("StardewValley.DesktopClipboard", text))
            return true;

        if (TryCopyViaPlatformInstance(text))
            return true;

        if (OperatingSystem.IsWindows())
            return TryCopyViaWindowsClipboard(text);

        if (OperatingSystem.IsMacOS())
            return TryRunClipboardCommand("pbcopy", string.Empty, text);

        // Linux/SteamDeck fallbacks.
        return TryRunClipboardCommand("wl-copy", string.Empty, text)
            || TryRunClipboardCommand("xclip", "-selection clipboard", text)
            || TryRunClipboardCommand("xsel", "--clipboard --input", text);
    }

    private static bool TryCopyViaType(string fullTypeName, string text)
    {
        var type = Type.GetType($"{fullTypeName}, Stardew Valley");
        if (type is null)
            return false;

        var method = type
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .FirstOrDefault(m =>
            {
                if (m.ReturnType != typeof(void))
                    return false;

                var parameters = m.GetParameters();
                if (parameters.Length != 1 || parameters[0].ParameterType != typeof(string))
                    return false;

                return m.Name is "SetText" or "SetClipboard" or "SetClipboardString";
            });

        if (method is null)
            return false;

        try
        {
            method.Invoke(null, new object[] { text });
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryCopyViaPlatformInstance(string text)
    {
        var gameInstance = Game1.game1;
        if (gameInstance is null)
            return false;

        var gameType = gameInstance.GetType();
        var platform = gameType.GetField("platform", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(gameInstance)
            ?? gameType.GetProperty("platform", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(gameInstance);
        if (platform is null)
            return false;

        var method = platform.GetType()
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .FirstOrDefault(m =>
            {
                if (m.ReturnType != typeof(void))
                    return false;

                var parameters = m.GetParameters();
                if (parameters.Length != 1 || parameters[0].ParameterType != typeof(string))
                    return false;

                return m.Name is "SetClipboard" or "SetClipboardString" or "SetText";
            });

        if (method is null)
            return false;

        try
        {
            method.Invoke(platform, new object[] { text });
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryCopyViaWindowsClipboard(string text)
    {
        return TryCopyViaWindowsClipExe(text) || TryCopyViaWindowsPowerShellClipboard(text);
    }

    private static bool TryCopyViaWindowsClipExe(string text)
    {
        try
        {
            var windowsDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            var clipExe = string.IsNullOrWhiteSpace(windowsDir)
                ? "clip.exe"
                : Path.Combine(windowsDir, "System32", "clip.exe");

            return TryRunClipboardCommand(clipExe, string.Empty, text)
                || TryRunClipboardCommand("cmd.exe", "/c clip", text);
        }
        catch
        {
            return false;
        }
    }

    private static bool TryCopyViaWindowsPowerShellClipboard(string text)
    {
        try
        {
            var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
            var command = "$t=[System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String('" + base64 + "')); Set-Clipboard -Value $t";
            var args = "-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"" + command + "\"";
            return TryRunClipboardCommand("powershell.exe", args, string.Empty);
        }
        catch
        {
            return false;
        }
    }

    private static bool TryRunClipboardCommand(string fileName, string arguments, string stdinText)
    {
        try
        {
            using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardInput = true,
                CreateNoWindow = true
            });

            if (process is null)
                return false;

            if (!string.IsNullOrEmpty(stdinText))
                process.StandardInput.Write(stdinText);
            process.StandardInput.Close();
            process.WaitForExit(2500);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private string GetPlayer2DeviceAuthVerificationUrl()
    {
        lock (_player2DeviceAuthUiLock)
            return _player2DeviceAuthVerificationUrl;
    }

    private string GetPlayer2DeviceAuthUserCode()
    {
        lock (_player2DeviceAuthUiLock)
            return _player2DeviceAuthUserCode;
    }

    private string GetPlayer2DeviceAuthStatus()
    {
        lock (_player2DeviceAuthUiLock)
        {
            var status = _player2DeviceAuthStatus;
            if (_player2DeviceAuthUiActive && _player2DeviceAuthExpiresUtc > DateTime.UtcNow)
            {
                var remaining = Math.Max(0, (int)Math.Ceiling((_player2DeviceAuthExpiresUtc - DateTime.UtcNow).TotalSeconds));
                status = $"{status} ({remaining}s)";
            }

            return status;
        }
    }

    private bool IsPlayer2DeviceAuthUiActive()
    {
        lock (_player2DeviceAuthUiLock)
            return _player2DeviceAuthUiActive;
    }

    private void SyncPlayer2DeviceAuthModal()
    {
        var isActive = IsPlayer2DeviceAuthUiActive();
        if (isActive)
        {
            if (Game1.activeClickableMenu is null)
            {
                Game1.activeClickableMenu = new Player2DeviceAuthMenu(
                    GetPlayer2DeviceAuthVerificationUrl,
                    GetPlayer2DeviceAuthUserCode,
                    GetPlayer2DeviceAuthStatus,
                    OpenPlayer2DeviceAuthBrowser,
                    CopyPlayer2DeviceAuthCodeToClipboard,
                    CancelPlayer2DeviceAuthFromUi);
            }

            return;
        }

        if (Game1.activeClickableMenu is Player2DeviceAuthMenu)
            Game1.activeClickableMenu.exitThisMenuNoSound();
    }

    private void CollectShippingBinSales()
    {
        if (!Context.IsWorldReady || _economyService is null || _salesIngestionService is null)
            return;

        var farm = Game1.getFarm();
        if (farm is null)
            return;

        foreach (var item in farm.getShippingBin(Game1.player))
        {
            if (item is null)
                continue;

            var displayName = item.DisplayName ?? string.Empty;
            if (!_economyService.TryNormalizeCropKey(displayName, out var cropKey))
                continue;

            var count = Math.Max(1, item.Stack);
            _salesIngestionService.AddSale(cropKey, count);
        }
    }

    private void AppendSimulatedNpcMarketSales(Dictionary<string, int> sold)
    {
        if (sold is null || _economyService is null || _state.Economy.Crops.Count == 0)
            return;

        var simulated = BuildNpcSimulatedMarketSales();
        if (simulated.Count == 0)
            return;

        foreach (var (crop, count) in simulated)
        {
            sold.TryGetValue(crop, out var existing);
            sold[crop] = existing + count;
        }
    }

    private Dictionary<string, int> BuildNpcSimulatedMarketSales()
    {
        var simulated = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var season = (_state.Calendar.Season ?? "spring").Trim().ToLowerInvariant();
        var day = Math.Max(1, _state.Calendar.Day);
        var year = Math.Max(1, _state.Calendar.Year);

        void AddIfKnown(string rawKey, int minCount, int maxCount, string seed)
        {
            if (minCount <= 0 || maxCount < minCount)
                return;
            if (!_economyService!.TryNormalizeCropKey(rawKey, out var cropKey))
                return;
            if (!_state.Economy.Crops.ContainsKey(cropKey))
                return;

            var quantity = RollDeterministicRange(minCount, maxCount, seed, day, year, season);
            if (quantity <= 0)
                return;

            simulated.TryGetValue(cropKey, out var existing);
            simulated[cropKey] = existing + quantity;
        }

        // Marnie ranch throughput: poultry + dairy always influence supply.
        AddIfKnown("egg", 6, 12, "marnie_egg");
        AddIfKnown("brown_egg", 3, 7, "marnie_brown_egg");
        AddIfKnown("milk", 4, 8, "marnie_milk");
        if (RollDeterministicModulo("marnie_duck_egg", day, year, season, 3) == 0)
            AddIfKnown("duck_egg", 1, 3, "marnie_duck_egg_qty");
        if (RollDeterministicModulo("marnie_void_egg", day, year, season, 4) == 0)
            AddIfKnown("void_egg", 1, 2, "marnie_void_egg_qty");
        if (RollDeterministicModulo("marnie_goat_milk", day, year, season, 2) == 0)
            AddIfKnown("goat_milk", 1, 3, "marnie_goat_milk_qty");
        if (RollDeterministicModulo("marnie_large_milk", day, year, season, 2) == 0)
            AddIfKnown("large_milk", 1, 2, "marnie_large_milk_qty");
        if (RollDeterministicModulo("marnie_large_goat_milk", day, year, season, 4) == 0)
            AddIfKnown("large_goat_milk", 1, 2, "marnie_large_goat_milk_qty");

        var seasonalPool = GetSeasonalNpcSupplyPool(season);
        if (seasonalPool.Length > 0)
        {
            var start = RollDeterministicModulo("town_seasonal_start", day, year, season, seasonalPool.Length);
            for (var i = 0; i < Math.Min(3, seasonalPool.Length); i++)
            {
                var crop = seasonalPool[(start + (i * 2)) % seasonalPool.Length];
                AddIfKnown(crop, 3, 10, $"town_seasonal_{i}_{crop}");
            }
        }

        if (season is "summer" or "fall")
        {
            var startFruit = RollDeterministicModulo("town_orchard_start", day, year, season, OrchardNpcSupplyPool.Length);
            for (var i = 0; i < 2; i++)
            {
                var fruit = OrchardNpcSupplyPool[(startFruit + i) % OrchardNpcSupplyPool.Length];
                AddIfKnown(fruit, 2, 6, $"town_orchard_{i}_{fruit}");
            }
        }

        return simulated;
    }

    private static string[] GetSeasonalNpcSupplyPool(string season)
    {
        return season switch
        {
            "spring" => SpringNpcSupplyPool,
            "summer" => SummerNpcSupplyPool,
            "fall" => FallNpcSupplyPool,
            "winter" => WinterNpcSupplyPool,
            _ => SpringNpcSupplyPool
        };
    }

    private static int RollDeterministicRange(int min, int max, string seed, int day, int year, string season)
    {
        if (max <= min)
            return min;

        var span = max - min + 1;
        return min + (RollDeterministicModulo(seed, day, year, season, span));
    }

    private static int RollDeterministicModulo(string seed, int day, int year, string season, int modulo)
    {
        if (modulo <= 1)
            return 0;

        unchecked
        {
            var hash = 17;
            foreach (var ch in seed)
                hash = (hash * 31) + ch;
            foreach (var ch in season)
                hash = (hash * 31) + ch;
            hash = (hash * 31) + day;
            hash = (hash * 31) + year;
            return (hash & int.MaxValue) % modulo;
        }
    }

    private void TryRefreshPendingNewspaperIssue(string source)
    {
        if (_pendingNewspaperRefreshDay < 0)
            return;

        if (_newspaperService is null)
            return;

        if (_pendingNewspaperRefreshDay != _state.Calendar.Day)
        {
            _pendingNewspaperRefreshDay = -1;
            return;
        }

        if (_config.EnablePlayer2 && !IsPlayer2ReadyForNewspaper())
            return;

        TryStartPendingNewspaperBuild(source);
    }

    private void OpenMarketBoard()
    {
        if (_marketBoardService is null)
            return;

        SyncCalendarSeasonFromWorld();
        Game1.activeClickableMenu = new MarketBoardMenu(_state);
        _state.Telemetry.Daily.MarketBoardOpens += 1;
    }

    private void OpenNewspaper()
    {
        SyncCalendarSeasonFromWorld();
        TryApplyCompletedNewspaperIssues();
        TryRefreshPendingNewspaperIssue("open-newspaper");

        var issue = _state.Newspaper.Issues.LastOrDefault();
        Game1.activeClickableMenu = new NewspaperMenu(issue);
    }

    private void OpenRumorBoard()
    {
        if (_rumorBoardService is null)
            return;

        SyncCalendarSeasonFromWorld();
        _rumorBoardService.ExpireOverdueQuests(_state);
        Game1.activeClickableMenu = new RumorBoardMenu(_state, _rumorBoardService, Monitor, () => OnUiAskMayorForWork(), () => _player2UiStatus);
    }

    private void OnSellCommand(string name, string[] args)
    {
        if (_salesIngestionService is null || args.Length < 2)
        {
            Monitor.Log("Usage: slrpg_sell <crop> <count>", LogLevel.Info);
            return;
        }

        var crop = args[0].Trim().ToLowerInvariant();
        if (!int.TryParse(args[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var count) || count <= 0)
        {
            Monitor.Log("Count must be a positive integer.", LogLevel.Warn);
            return;
        }

        _salesIngestionService.AddSale(crop, count);
        Monitor.Log($"Queued sale: {crop} x{count}", LogLevel.Info);
    }

    private void OnBoardCommand(string name, string[] args)
    {
        if (_marketBoardService is null)
            return;

        SyncCalendarSeasonFromWorld();
        Monitor.Log($"=== Pierre's Market Board - Day {_state.Calendar.Day} ({_state.Calendar.Season}) ===", LogLevel.Info);

        foreach (var (crop, entry) in _state.Economy.Crops.OrderByDescending(kv => kv.Value.TrendEma).Take(8))
        {
            var arrow = entry.PriceToday > entry.PriceYesterday ? "^" : entry.PriceToday < entry.PriceYesterday ? "v" : "->";
            Monitor.Log($"{crop,-12} {entry.PriceToday,4}g {arrow} (demand {entry.DemandFactor:F2}, supply {entry.SupplyPressureFactor:F2}, scarcity+ {entry.ScarcityBonus:P0})", LogLevel.Info);
        }
    }

    private void OnOpenBoardCommand(string name, string[] args)
    {
        if (!Context.IsWorldReady)
            return;

        OpenMarketBoard();
    }

    private void OnOpenNewsCommand(string name, string[] args)
    {
        if (!Context.IsWorldReady)
            return;

        OpenNewspaper();
    }

    private void OnOpenRumorsCommand(string name, string[] args)
    {
        if (!Context.IsWorldReady)
            return;

        OpenRumorBoard();
    }

    private void OnAcceptQuestCommand(string name, string[] args)
    {
        if (!Context.IsWorldReady || _rumorBoardService is null || args.Length < 1)
        {
            Monitor.Log("Usage: slrpg_accept_quest <questId>", LogLevel.Info);
            return;
        }

        var questId = args[0].Trim();
        var quest = _state.Quests.Available.FirstOrDefault(q => q.QuestId.Equals(questId, StringComparison.OrdinalIgnoreCase));
        if (_rumorBoardService.AcceptQuest(_state, questId))
        {
            var title = quest is null ? questId : QuestTextHelper.BuildQuestTitle(quest);
            Monitor.Log($"Accepted request: {title}", LogLevel.Info);
        }
        else
            Monitor.Log($"Request not found: {questId}", LogLevel.Warn);
    }

    private void OnQuestProgressCommand(string name, string[] args)
    {
        if (!Context.IsWorldReady || _rumorBoardService is null || args.Length < 1)
        {
            Monitor.Log("Usage: slrpg_quest_progress <questId>", LogLevel.Info);
            return;
        }

        var questId = args[0].Trim();
        var progress = _rumorBoardService.GetQuestProgress(_state, questId, Game1.player);
        if (!progress.Exists || progress.Quest is null)
        {
            Monitor.Log($"Active quest not found: {questId}", LogLevel.Warn);
            return;
        }

        var title = QuestTextHelper.BuildQuestTitle(progress.Quest);
        if (!progress.RequiresItems)
        {
            Monitor.Log($"Request {title}: no item hand-in required (template={progress.Quest.TemplateId}). Ready={progress.IsReadyToComplete}", LogLevel.Info);
            return;
        }

        Monitor.Log($"Request {title}: {progress.HaveCount}/{progress.NeedCount} {progress.Quest.TargetItem} | ready={progress.IsReadyToComplete}", LogLevel.Info);
    }

    private void OnQuestProgressAllCommand(string name, string[] args)
    {
        if (!Context.IsWorldReady || _rumorBoardService is null)
        {
            Monitor.Log("World not ready.", LogLevel.Warn);
            return;
        }

        if (_state.Quests.Active.Count == 0)
        {
            Monitor.Log("No active quests.", LogLevel.Info);
            return;
        }

        foreach (var q in _state.Quests.Active)
        {
            var progress = _rumorBoardService.GetQuestProgress(_state, q.QuestId, Game1.player);
            if (!progress.Exists || progress.Quest is null)
                continue;

            var title = QuestTextHelper.BuildQuestTitle(progress.Quest);
            if (!progress.RequiresItems)
                Monitor.Log($"Request {title}: no item hand-in required | ready={progress.IsReadyToComplete}", LogLevel.Info);
            else
                Monitor.Log($"Request {title}: {progress.HaveCount}/{progress.NeedCount} {progress.Quest.TargetItem} | ready={progress.IsReadyToComplete}", LogLevel.Info);
        }
    }

    private void OnCompleteQuestCommand(string name, string[] args)
    {
        if (!Context.IsWorldReady || _rumorBoardService is null || args.Length < 1)
        {
            Monitor.Log("Usage: slrpg_complete_quest <questId>", LogLevel.Info);
            return;
        }

        var questId = args[0].Trim();
        var result = _rumorBoardService.CompleteQuestWithChecks(_state, questId, Game1.player, consumeItems: true);
        if (result.Success)
        {
            var completed = _state.Quests.Completed.LastOrDefault(q => q.QuestId.Equals(questId, StringComparison.OrdinalIgnoreCase));
            if (completed is not null && _npcMemoryService is not null && !string.IsNullOrWhiteSpace(completed.Issuer))
                _npcMemoryService.WriteFact(_state, completed.Issuer, "quest", $"Player completed request '{QuestTextHelper.BuildQuestTitle(completed)}'.", _state.Calendar.Day, 3);

            Monitor.Log(result.Message, LogLevel.Info);
        }
        else
            Monitor.Log(result.Message, LogLevel.Warn);
    }

    private void OnSetSentimentCommand(string name, string[] args)
    {
        if (args.Length < 2)
        {
            Monitor.Log("Usage: slrpg_set_sentiment economy <value>", LogLevel.Info);
            return;
        }

        var axis = args[0].Trim().ToLowerInvariant();
        if (!int.TryParse(args[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            Monitor.Log("Value must be an integer.", LogLevel.Warn);
            return;
        }

        value = Math.Max(-100, Math.Min(100, value));
        switch (axis)
        {
            case "economy":
                _state.Social.TownSentiment.Economy = value;
                Monitor.Log($"Set economy sentiment to {value}.", LogLevel.Info);
                break;
            case "community":
                _state.Social.TownSentiment.Community = value;
                Monitor.Log($"Set community sentiment to {value}.", LogLevel.Info);
                break;
            case "environment":
                _state.Social.TownSentiment.Environment = value;
                Monitor.Log($"Set environment sentiment to {value}.", LogLevel.Info);
                break;
            default:
                Monitor.Log("Axis must be one of: economy|community|environment", LogLevel.Warn);
                break;
        }
    }

    private void OnDebugStateCommand(string name, string[] args)
    {
        Monitor.Log("=== SLRPG State Snapshot ===", LogLevel.Info);
        Monitor.Log($"Day {_state.Calendar.Day} | Season {_state.Calendar.Season} | Year {_state.Calendar.Year}", LogLevel.Info);
        Monitor.Log($"Mode {_state.Config.Mode} | Sentiment E/C/Env: {_state.Social.TownSentiment.Economy}/{_state.Social.TownSentiment.Community}/{_state.Social.TownSentiment.Environment}", LogLevel.Info);
        Monitor.Log($"Ambient consequence pipeline enabled: {_config.EnableAmbientConsequencePipeline}", LogLevel.Info);

        var anchorSeen = _state.Facts.Facts.ContainsKey("anchor:town_hall_crisis:seen");
        Monitor.Log($"Anchor town_hall_crisis seen: {anchorSeen}", LogLevel.Info);

        var top = _state.Economy.Crops
            .OrderByDescending(kv => Math.Abs(kv.Value.PriceToday - kv.Value.PriceYesterday))
            .Take(3)
            .ToList();

        if (top.Count == 0)
        {
            Monitor.Log("No crop economy entries yet.", LogLevel.Info);
        }
        else
        {
            Monitor.Log("Top market movers:", LogLevel.Info);
            foreach (var (crop, e) in top)
            {
                var d = e.PriceToday - e.PriceYesterday;
                Monitor.Log($"- {crop}: {e.PriceYesterday}g -> {e.PriceToday}g ({d:+#;-#;0}) | demand {e.DemandFactor:F2}, supply {e.SupplyPressureFactor:F2}, scarcity {e.ScarcityBonus:P0}", LogLevel.Info);
            }
        }

        Monitor.Log($"Quests | available: {_state.Quests.Available.Count}, active: {_state.Quests.Active.Count}, completed: {_state.Quests.Completed.Count}, failed: {_state.Quests.Failed.Count}", LogLevel.Info);
        Monitor.Log($"Telemetry | opens(board): {_state.Telemetry.Daily.MarketBoardOpens}, accepts: {_state.Telemetry.Daily.RumorBoardAccepts}, completes: {_state.Telemetry.Daily.RumorBoardCompletions}, anchors: {_state.Telemetry.Daily.AnchorEventsTriggered}, mutations: {_state.Telemetry.Daily.WorldMutations}", LogLevel.Info);
        Monitor.Log($"Telemetry NPC intents | applied: {_state.Telemetry.Daily.NpcIntentsApplied}, rejected: {_state.Telemetry.Daily.NpcIntentsRejected}, duplicate: {_state.Telemetry.Daily.NpcIntentsDuplicate}", LogLevel.Info);
        Monitor.Log($"Telemetry NPC lanes | auto(applied/rejected): {_state.Telemetry.Daily.NpcIntentsAutoApplied}/{_state.Telemetry.Daily.NpcIntentsAutoRejected}, manual(applied/rejected): {_state.Telemetry.Daily.NpcIntentsManualApplied}/{_state.Telemetry.Daily.NpcIntentsManualRejected}", LogLevel.Info);
        Monitor.Log($"Telemetry ask gate | accept: {_state.Telemetry.Daily.NpcAskGateAccepted}, defer: {_state.Telemetry.Daily.NpcAskGateDeferred}, reject: {_state.Telemetry.Daily.NpcAskGateRejected}", LogLevel.Info);
        Monitor.Log($"Telemetry policy rejects | reasons: {FormatCounterMap(_state.Telemetry.Daily.NpcPolicyRejectByReason)}", LogLevel.Info);
        Monitor.Log($"Telemetry ambient low-info suppressed: {_state.Telemetry.Daily.AmbientLowInfoSuppressed}", LogLevel.Info);
        Monitor.Log($"Telemetry ambient cadence skips: {_state.Telemetry.Daily.AmbientCadenceSkips}", LogLevel.Info);
        Monitor.Log($"Telemetry ambient by type | applied: {FormatCounterMap(_state.Telemetry.Daily.AmbientCommandAppliedByType)}", LogLevel.Info);
        Monitor.Log($"Telemetry ambient by type | rejected: {FormatCounterMap(_state.Telemetry.Daily.AmbientCommandRejectedByType)}", LogLevel.Info);
        Monitor.Log($"Telemetry ambient by type | duplicate: {FormatCounterMap(_state.Telemetry.Daily.AmbientCommandDuplicateByType)}", LogLevel.Info);

        var ambientSnapshots = _ambientLaneDebugSnapshots.ToArray();
        if (ambientSnapshots.Length == 0)
        {
            Monitor.Log("Ambient lane snapshots: (none captured yet).", LogLevel.Info);
        }
        else
        {
            Monitor.Log($"Ambient lane snapshots (latest {ambientSnapshots.Length}):", LogLevel.Info);
            foreach (var snapshot in ambientSnapshots)
                Monitor.Log($"- {snapshot}", LogLevel.Info);
        }
    }

    private void OnBaselineThreeDayCommand(string name, string[] args)
    {
        if (_intentResolver is null)
        {
            Monitor.Log("Intent resolver unavailable.", LogLevel.Warn);
            return;
        }

        var metrics = RunThreeDayBaselineMetricsSimulation();
        var questRate = metrics.DaysSimulated <= 0 ? 0d : (double)metrics.QuestCount / metrics.DaysSimulated;
        var marketModifierRate = metrics.DaysSimulated <= 0 ? 0d : (double)metrics.MarketModifierCount / metrics.DaysSimulated;

        Monitor.Log($"3-day baseline simulation complete | days={metrics.DaysSimulated} intents(applied/rejected/duplicate)={metrics.Applied}/{metrics.Rejected}/{metrics.Duplicate}", LogLevel.Info);
        Monitor.Log($"3-day baseline command distribution | applied: {FormatCounterMap(metrics.CommandDistribution)}", LogLevel.Info);
        Monitor.Log($"3-day baseline rates | quest/day={questRate:F2} market_modifier/day={marketModifierRate:F2}", LogLevel.Info);
    }

    private void OnBaselineSevenDayCommand(string name, string[] args)
    {
        if (_intentResolver is null)
        {
            Monitor.Log("Intent resolver unavailable.", LogLevel.Warn);
            return;
        }

        var baseline = RunThreeDayBaselineMetricsSimulation();
        var scenario = RunSevenDayScenarioMetricsSimulation();
        var baselineQuestRate = baseline.DaysSimulated <= 0 ? 0d : (double)baseline.QuestCount / baseline.DaysSimulated;
        var baselineMarketRate = baseline.DaysSimulated <= 0 ? 0d : (double)baseline.MarketModifierCount / baseline.DaysSimulated;
        var scenarioQuestRate = scenario.DaysSimulated <= 0 ? 0d : (double)scenario.QuestCount / scenario.DaysSimulated;
        var scenarioMarketRate = scenario.DaysSimulated <= 0 ? 0d : (double)scenario.MarketModifierCount / scenario.DaysSimulated;

        Monitor.Log($"7-day scenario simulation complete | days={scenario.DaysSimulated} intents(applied/rejected/duplicate)={scenario.Applied}/{scenario.Rejected}/{scenario.Duplicate}", LogLevel.Info);
        Monitor.Log($"7-day scenario command distribution | applied: {FormatCounterMap(scenario.CommandDistribution)}", LogLevel.Info);
        Monitor.Log($"7-day scenario rates | quest/day={scenarioQuestRate:F2} market_modifier/day={scenarioMarketRate:F2}", LogLevel.Info);
        Monitor.Log(
            $"7-day comparison vs baseline | quest/day {scenarioQuestRate:F2} vs {baselineQuestRate:F2} ({scenarioQuestRate - baselineQuestRate:+0.00;-0.00;0.00}), market_modifier/day {scenarioMarketRate:F2} vs {baselineMarketRate:F2} ({scenarioMarketRate - baselineMarketRate:+0.00;-0.00;0.00})",
            LogLevel.Info);
    }

    private void OnAmbientPipelineValidateCommand(string name, string[] args)
    {
        if (_intentResolver is null)
        {
            Monitor.Log("Intent resolver unavailable.", LogLevel.Warn);
            return;
        }

        var baseline = RunThreeDayBaselineMetricsSimulation();
        var scenario = RunSevenDayScenarioMetricsSimulation();
        var baselineQuestRate = baseline.DaysSimulated <= 0 ? 0d : (double)baseline.QuestCount / baseline.DaysSimulated;
        var baselineMarketRate = baseline.DaysSimulated <= 0 ? 0d : (double)baseline.MarketModifierCount / baseline.DaysSimulated;
        var scenarioQuestRate = scenario.DaysSimulated <= 0 ? 0d : (double)scenario.QuestCount / scenario.DaysSimulated;
        var scenarioMarketRate = scenario.DaysSimulated <= 0 ? 0d : (double)scenario.MarketModifierCount / scenario.DaysSimulated;

        var liveState = _state;
        var baselineState = CloneSaveState(liveState);
        (int Pass, int Fail, int Total) targeted;
        try
        {
            _state = CloneSaveState(baselineState);
            targeted = RunTargetedRegressionChecks();
        }
        finally
        {
            _state = liveState;
        }

        var questDelta = Math.Abs(scenarioQuestRate - baselineQuestRate);
        var marketDelta = Math.Abs(scenarioMarketRate - baselineMarketRate);
        var driftOk = questDelta <= StagedValidationMaxQuestRateDelta
            && marketDelta <= StagedValidationMaxMarketRateDelta;
        var regressionOk = targeted.Fail == 0;
        var overallPass = driftOk && regressionOk;

        Monitor.Log(
            $"Ambient pipeline staged validation | enabled={_config.EnableAmbientConsequencePipeline} regressions={targeted.Pass}/{targeted.Total} quest/day={scenarioQuestRate:F2} (baseline {baselineQuestRate:F2}) market_modifier/day={scenarioMarketRate:F2} (baseline {baselineMarketRate:F2})",
            LogLevel.Info);
        Monitor.Log(
            $"Ambient pipeline staged validation thresholds | quest_delta={questDelta:F2}<= {StagedValidationMaxQuestRateDelta:F2}, market_delta={marketDelta:F2}<= {StagedValidationMaxMarketRateDelta:F2}",
            driftOk ? LogLevel.Info : LogLevel.Warn);
        Monitor.Log(
            overallPass
                ? "Ambient consequence pipeline validation PASSED. Keep default enabled."
                : "Ambient consequence pipeline validation needs tuning before release promotion.",
            overallPass ? LogLevel.Info : LogLevel.Warn);
    }

    private ThreeDayBaselineMetrics RunThreeDayBaselineMetricsSimulation()
    {
        var simState = CloneSaveState(_state);
        var startDay = Math.Max(1, simState.Calendar.Day);
        var metrics = new ThreeDayBaselineMetrics { DaysSimulated = 3 };

        for (var dayOffset = 0; dayOffset < metrics.DaysSimulated; dayOffset++)
        {
            var simulationDay = dayOffset + 1;
            simState.Calendar.Day = startDay + dayOffset;

            foreach (var payload in BuildThreeDayBaselinePayloads(simulationDay))
            {
                var result = _intentResolver!.ResolveFromStreamLine(simState, payload);
                if (result.AppliedOk)
                {
                    metrics.Applied += 1;
                    IncrementCounter(metrics.CommandDistribution, result.Command);
                    if (result.Command.Equals("propose_quest", StringComparison.OrdinalIgnoreCase))
                        metrics.QuestCount += 1;
                    else if (result.Command.Equals("apply_market_modifier", StringComparison.OrdinalIgnoreCase))
                        metrics.MarketModifierCount += 1;
                }
                else if (result.IsRejected)
                {
                    metrics.Rejected += 1;
                }
                else if (result.IsDuplicate)
                {
                    metrics.Duplicate += 1;
                }
            }
        }

        return metrics;
    }

    private SevenDayScenarioMetrics RunSevenDayScenarioMetricsSimulation()
    {
        var simState = CloneSaveState(_state);
        var startDay = Math.Max(1, simState.Calendar.Day);
        var metrics = new SevenDayScenarioMetrics { DaysSimulated = 7 };

        for (var dayOffset = 0; dayOffset < metrics.DaysSimulated; dayOffset++)
        {
            var simulationDay = dayOffset + 1;
            simState.Calendar.Day = startDay + dayOffset;

            foreach (var payload in BuildSevenDayScenarioPayloads(simulationDay))
            {
                var result = _intentResolver!.ResolveFromStreamLine(simState, payload);
                if (result.AppliedOk)
                {
                    metrics.Applied += 1;
                    IncrementCounter(metrics.CommandDistribution, result.Command);
                    if (result.Command.Equals("propose_quest", StringComparison.OrdinalIgnoreCase))
                        metrics.QuestCount += 1;
                    else if (result.Command.Equals("apply_market_modifier", StringComparison.OrdinalIgnoreCase))
                        metrics.MarketModifierCount += 1;
                }
                else if (result.IsRejected)
                {
                    metrics.Rejected += 1;
                }
                else if (result.IsDuplicate)
                {
                    metrics.Duplicate += 1;
                }
            }
        }

        return metrics;
    }

    private static IEnumerable<string> BuildThreeDayBaselinePayloads(int simulationDay)
    {
        switch (simulationDay)
        {
            case 1:
                yield return "{\"intent_id\":\"baseline_d1_evt\",\"npc_id\":\"ambient_baseline_lewis\",\"command\":\"record_town_event\",\"arguments\":{\"kind\":\"community\",\"summary\":\"Shops coordinated a short cleanup around the square before lunch.\",\"location\":\"Town Square\",\"severity\":2,\"visibility\":\"public\",\"tags\":[\"community\",\"cleanup\"]}}";
                yield return "{\"intent_id\":\"baseline_d1_rumor\",\"npc_id\":\"ambient_baseline_pierre\",\"command\":\"publish_rumor\",\"arguments\":{\"topic\":\"Folks noticed extra foot traffic near Pierre's this morning.\",\"confidence\":0.62,\"target_group\":\"shopkeepers_guild\"}}";
                yield return "{\"intent_id\":\"baseline_d1_quest\",\"npc_id\":\"ambient_baseline_robin\",\"command\":\"propose_quest\",\"arguments\":{\"template_id\":\"gather_crop\",\"target\":\"potato\",\"urgency\":\"medium\"}}";
                yield break;
            case 2:
                yield return "{\"intent_id\":\"baseline_d2_memory\",\"npc_id\":\"ambient_baseline_caroline\",\"command\":\"record_memory_fact\",\"arguments\":{\"category\":\"event\",\"text\":\"The farmer helped restock seed shelves before noon.\",\"weight\":2}}";
                yield return "{\"intent_id\":\"baseline_d2_market\",\"npc_id\":\"ambient_baseline_pierre\",\"command\":\"apply_market_modifier\",\"arguments\":{\"crop\":\"blueberry\",\"delta_pct\":-0.06,\"duration_days\":2,\"reason\":\"noticeable surplus at shipping stalls\"}}";
                yield return "{\"intent_id\":\"baseline_d2_interest\",\"npc_id\":\"ambient_baseline_lewis\",\"command\":\"shift_interest_influence\",\"arguments\":{\"interest\":\"farmers_circle\",\"delta\":2,\"reason\":\"neighbor chatter favored local growers\"}}";
                yield break;
            default:
                yield return "{\"intent_id\":\"baseline_d3_rep\",\"npc_id\":\"ambient_baseline_haley\",\"command\":\"adjust_reputation\",\"arguments\":{\"target\":\"farmer\",\"delta\":2,\"reason\":\"kept yesterday's board promise\"}}";
                yield return "{\"intent_id\":\"baseline_d3_article\",\"npc_id\":\"ambient_baseline_lewis\",\"command\":\"publish_article\",\"arguments\":{\"title\":\"Board Update\",\"content\":\"Neighbors posted one shared request and kept chatter focused at the square.\",\"category\":\"community\"}}";
                yield return "{\"intent_id\":\"baseline_d3_sentiment\",\"npc_id\":\"ambient_baseline_evelyn\",\"command\":\"adjust_town_sentiment\",\"arguments\":{\"axis\":\"community\",\"delta\":2,\"reason\":\"steady volunteer turnout\"}}";
                yield return "{\"intent_id\":\"baseline_d3_quest\",\"npc_id\":\"ambient_baseline_gus\",\"command\":\"propose_quest\",\"arguments\":{\"template_id\":\"social_visit\",\"target\":\"Linus\",\"urgency\":\"low\"}}";
                yield break;
        }
    }

    private static IEnumerable<string> BuildSevenDayScenarioPayloads(int simulationDay)
    {
        switch (simulationDay)
        {
            case 1:
                yield return "{\"intent_id\":\"scenario_d1_evt\",\"npc_id\":\"ambient_scenario_lewis\",\"command\":\"record_town_event\",\"arguments\":{\"kind\":\"market\",\"summary\":\"Shops flagged a small potato shortage before noon.\",\"location\":\"Town Square\",\"severity\":2,\"visibility\":\"public\",\"tags\":[\"market\",\"potato\",\"scarcity\"]}}";
                yield return "{\"intent_id\":\"scenario_d1_mem\",\"npc_id\":\"ambient_scenario_robin\",\"command\":\"record_memory_fact\",\"arguments\":{\"category\":\"event\",\"text\":\"Neighbors coordinated produce sorting near the board.\",\"weight\":2}}";
                yield break;
            case 2:
                yield return "{\"intent_id\":\"scenario_d2_evt\",\"npc_id\":\"ambient_scenario_caroline\",\"command\":\"record_town_event\",\"arguments\":{\"kind\":\"social\",\"summary\":\"Volunteers checked on elders after the morning rain.\",\"location\":\"Pelican Town\",\"severity\":2,\"visibility\":\"public\",\"tags\":[\"community\",\"social\",\"rain\"]}}";
                yield return "{\"intent_id\":\"scenario_d2_rumor\",\"npc_id\":\"ambient_scenario_gus\",\"command\":\"publish_rumor\",\"arguments\":{\"topic\":\"Folks heard Pierre may restock potatoes by evening.\",\"confidence\":0.72,\"target_group\":\"shopkeepers_guild\"}}";
                yield break;
            case 3:
                yield return "{\"intent_id\":\"scenario_d3_market\",\"npc_id\":\"ambient_scenario_pierre\",\"command\":\"apply_market_modifier\",\"arguments\":{\"crop\":\"potato\",\"delta_pct\":0.06,\"duration_days\":2,\"reason\":\"short-run shortage pressure\"}}";
                yield return "{\"intent_id\":\"scenario_d3_interest\",\"npc_id\":\"ambient_scenario_lewis\",\"command\":\"shift_interest_influence\",\"arguments\":{\"interest\":\"farmers_circle\",\"delta\":1,\"reason\":\"board chatter focused on staple crops\"}}";
                yield break;
            case 4:
                yield return "{\"intent_id\":\"scenario_d4_evt\",\"npc_id\":\"ambient_scenario_demetrius\",\"command\":\"record_town_event\",\"arguments\":{\"kind\":\"nature\",\"summary\":\"Cold winds slowed crop growth near the river plots.\",\"location\":\"Riverland\",\"severity\":3,\"visibility\":\"public\",\"tags\":[\"weather\",\"nature\",\"crop\"]}}";
                yield return "{\"intent_id\":\"scenario_d4_article\",\"npc_id\":\"ambient_scenario_elliott\",\"command\":\"publish_article\",\"arguments\":{\"title\":\"Cold Snap Slows Harvest\",\"content\":\"Growers are adjusting schedules after cooler winds hit riverside plots.\",\"category\":\"nature\"}}";
                yield break;
            case 5:
                yield return "{\"intent_id\":\"scenario_d5_quest\",\"npc_id\":\"ambient_scenario_gus\",\"command\":\"propose_quest\",\"arguments\":{\"template_id\":\"gather_crop\",\"target\":\"potato\",\"urgency\":\"medium\"}}";
                yield return "{\"intent_id\":\"scenario_d5_rep\",\"npc_id\":\"ambient_scenario_haley\",\"command\":\"adjust_reputation\",\"arguments\":{\"target\":\"farmer\",\"delta\":1,\"reason\":\"kept promises from town requests\"}}";
                yield break;
            case 6:
                yield return "{\"intent_id\":\"scenario_d6_evt\",\"npc_id\":\"ambient_scenario_marnie\",\"command\":\"record_town_event\",\"arguments\":{\"kind\":\"community\",\"summary\":\"Residents shared food deliveries after late-night repairs.\",\"location\":\"Town\",\"severity\":2,\"visibility\":\"public\",\"tags\":[\"community\",\"support\"]}}";
                yield return "{\"intent_id\":\"scenario_d6_quest\",\"npc_id\":\"ambient_scenario_lewis\",\"command\":\"propose_quest\",\"arguments\":{\"template_id\":\"social_visit\",\"target\":\"Linus\",\"urgency\":\"low\"}}";
                yield break;
            default:
                yield return "{\"intent_id\":\"scenario_d7_market\",\"npc_id\":\"ambient_scenario_pierre\",\"command\":\"apply_market_modifier\",\"arguments\":{\"crop\":\"cauliflower\",\"delta_pct\":-0.05,\"duration_days\":2,\"reason\":\"short-term surplus after delivery rush\"}}";
                yield return "{\"intent_id\":\"scenario_d7_sent\",\"npc_id\":\"ambient_scenario_evelyn\",\"command\":\"adjust_town_sentiment\",\"arguments\":{\"axis\":\"community\",\"delta\":1,\"reason\":\"steady turnout for volunteer board requests\"}}";
                yield break;
        }
    }

    private sealed class ThreeDayBaselineMetrics
    {
        public int DaysSimulated { get; init; }
        public int Applied { get; set; }
        public int Rejected { get; set; }
        public int Duplicate { get; set; }
        public int QuestCount { get; set; }
        public int MarketModifierCount { get; set; }
        public Dictionary<string, int> CommandDistribution { get; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private sealed class SevenDayScenarioMetrics
    {
        public int DaysSimulated { get; init; }
        public int Applied { get; set; }
        public int Rejected { get; set; }
        public int Duplicate { get; set; }
        public int QuestCount { get; set; }
        public int MarketModifierCount { get; set; }
        public Dictionary<string, int> CommandDistribution { get; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private void OnIntentInjectCommand(string name, string[] args)
    {
        if (_intentResolver is null)
        {
            Monitor.Log("Intent resolver unavailable.", LogLevel.Warn);
            return;
        }

        if (args.Length == 0)
        {
            Monitor.Log("Usage: slrpg_intent_inject {\"intent_id\":\"id123\",\"npc_id\":\"lewis\",\"command\":\"adjust_reputation\",\"arguments\":{\"target\":\"haley\",\"delta\":2}}", LogLevel.Info);
            return;
        }

        var json = string.Join(' ', args);
        try
        {
            var result = _intentResolver.ResolveFromStreamLine(_state, json);
            if (!result.HasIntent)
            {
                Monitor.Log("Injected payload produced no intent.", LogLevel.Warn);
                return;
            }

            if (result.IsRejected)
            {
                Monitor.Log($"Injected intent rejected: {result.Reason}", LogLevel.Warn);
                return;
            }

            if (result.IsDuplicate)
            {
                Monitor.Log($"Injected intent duplicate ignored: {result.IntentId}", LogLevel.Info);
                return;
            }

            Monitor.Log($"Injected intent applied: cmd={result.Command} outcome={result.OutcomeId} intent={result.IntentId}", LogLevel.Info);
            if (result.Proposal is not null)
            {
                var p = result.Proposal;
                Monitor.Log($"Injected quest mapping | requested={p.RequestedTemplate}/{p.RequestedTarget}/{p.RequestedUrgency} applied={p.AppliedTemplate}/{p.AppliedTarget}/{p.AppliedUrgency}", LogLevel.Info);
            }

            _player2LastCommandApplied = $"{result.Command}:{result.OutcomeId}";
            _player2LastCommandAppliedUtc = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            Monitor.Log($"Intent injection failed: {ex.Message}", LogLevel.Error);
        }
    }

    private void OnDebugNewsToastCommand(string name, string[] args)
    {
        if (_intentResolver is null)
        {
            Monitor.Log("Intent resolver unavailable.", LogLevel.Warn);
            return;
        }

        if (args.Length == 0)
        {
            Monitor.Log("Usage: slrpg_debug_news_toast <article|rumor> [text]", LogLevel.Info);
            return;
        }

        var mode = args[0].Trim().ToLowerInvariant();
        var text = args.Length > 1 ? string.Join(' ', args.Skip(1)).Trim() : string.Empty;
        var npcId = !string.IsNullOrWhiteSpace(_activeNpcId) ? _activeNpcId! : "debug_npc";
        var intentId = $"debug_news_{mode}_{_state.Calendar.Day}_{DateTime.UtcNow.Ticks}";

        string payload;
        if (mode == "article")
        {
            var title = string.IsNullOrWhiteSpace(text) ? "Debug Market Bulletin" : text;
            payload = JsonSerializer.Serialize(new
            {
                intent_id = intentId,
                npc_id = npcId,
                command = "publish_article",
                arguments = new
                {
                    title,
                    content = "Debug article payload for HUD toast validation.",
                    category = "community"
                }
            });
        }
        else if (mode == "rumor")
        {
            var topic = string.IsNullOrWhiteSpace(text) ? "Blueberry demand may cool next week" : text;
            payload = JsonSerializer.Serialize(new
            {
                intent_id = intentId,
                npc_id = npcId,
                command = "publish_rumor",
                arguments = new
                {
                    topic,
                    confidence = 0.72f,
                    target_group = "shopkeepers"
                }
            });
        }
        else
        {
            Monitor.Log("Usage: slrpg_debug_news_toast <article|rumor> [text]", LogLevel.Info);
            return;
        }

        TryApplyNpcCommandFromLine(payload);
        Monitor.Log($"Injected debug news intent via stream path: {mode}", LogLevel.Info);
    }

    private void OnIntentSmokeTestCommand(string name, string[] args)
    {
        if (_intentResolver is null)
        {
            Monitor.Log("Intent resolver unavailable.", LogLevel.Warn);
            return;
        }

        var tests = new (string Name, string Json, string Expected)[]
        {
            (
                "propose_quest valid",
                "{\"intent_id\":\"smoke_pq_001\",\"npc_id\":\"lewis\",\"command\":\"propose_quest\",\"arguments\":{\"template_id\":\"gather_crop\",\"target\":\"blueberry\",\"urgency\":\"high\"}}",
                "applied"
            ),
            (
                "propose_quest duplicate",
                "{\"intent_id\":\"smoke_pq_001\",\"npc_id\":\"lewis\",\"command\":\"propose_quest\",\"arguments\":{\"template_id\":\"gather_crop\",\"target\":\"blueberry\",\"urgency\":\"high\"}}",
                "duplicate"
            ),
            (
                "adjust_reputation invalid delta",
                "{\"intent_id\":\"smoke_rep_001\",\"npc_id\":\"lewis\",\"command\":\"adjust_reputation\",\"arguments\":{\"target\":\"haley\",\"delta\":99}}",
                "rejected"
            ),
            (
                "shift_interest_influence valid",
                "{\"intent_id\":\"smoke_int_001\",\"npc_id\":\"lewis\",\"command\":\"shift_interest_influence\",\"arguments\":{\"interest\":\"farmers_circle\",\"delta\":2}}",
                "applied"
            ),
            (
                "apply_market_modifier valid",
                "{\"intent_id\":\"smoke_mkt_001\",\"npc_id\":\"lewis\",\"command\":\"apply_market_modifier\",\"arguments\":{\"crop\":\"blueberry\",\"delta_pct\":-0.08,\"duration_days\":3}}",
                "applied"
            ),
            (
                "publish_rumor valid",
                "{\"intent_id\":\"smoke_rmr_001\",\"npc_id\":\"lewis\",\"command\":\"publish_rumor\",\"arguments\":{\"topic\":\"Blueberry surplus\",\"confidence\":0.7,\"target_group\":\"shopkeepers_guild\"}}",
                "applied"
            ),
            (
                "publish_article valid",
                "{\"intent_id\":\"smoke_art_001\",\"npc_id\":\"lewis\",\"command\":\"publish_article\",\"arguments\":{\"title\":\"Town Bulletin\",\"content\":\"Market lane stable today.\",\"category\":\"community\"}}",
                "applied"
            ),
            (
                "record_memory_fact valid",
                "{\"intent_id\":\"smoke_mem_001\",\"npc_id\":\"lewis\",\"command\":\"record_memory_fact\",\"arguments\":{\"category\":\"event\",\"text\":\"The farmer helped sort crates at the shop.\",\"weight\":3}}",
                "applied"
            ),
            (
                "record_memory_fact duplicate text",
                "{\"intent_id\":\"smoke_mem_002\",\"npc_id\":\"lewis\",\"command\":\"record_memory_fact\",\"arguments\":{\"category\":\"event\",\"text\":\"The farmer helped sort crates at the shop.\",\"weight\":2}}",
                "rejected"
            ),
            (
                "record_town_event valid",
                "{\"intent_id\":\"smoke_evt_001\",\"npc_id\":\"lewis\",\"command\":\"record_town_event\",\"arguments\":{\"kind\":\"market\",\"summary\":\"Shops saw a clear blueberry demand bump this morning.\",\"location\":\"Town Square\",\"severity\":2,\"visibility\":\"public\",\"tags\":[\"market\",\"blueberry\"]}}",
                "applied"
            ),
            (
                "record_town_event cap 2",
                "{\"intent_id\":\"smoke_evt_002\",\"npc_id\":\"pierre\",\"command\":\"record_town_event\",\"arguments\":{\"kind\":\"community\",\"summary\":\"Residents gathered to clean the square at noon today.\",\"location\":\"Town Square\",\"severity\":1,\"visibility\":\"public\",\"tags\":[\"community\"]}}",
                "applied"
            ),
            (
                "record_town_event cap exceeded",
                "{\"intent_id\":\"smoke_evt_003\",\"npc_id\":\"robin\",\"command\":\"record_town_event\",\"arguments\":{\"kind\":\"social\",\"summary\":\"Another social note that should hit the daily town-event cap.\",\"location\":\"Saloon\",\"severity\":1,\"visibility\":\"local\",\"tags\":[\"social\"]}}",
                "rejected"
            ),
            (
                "adjust_town_sentiment valid",
                "{\"intent_id\":\"smoke_sent_001\",\"npc_id\":\"lewis\",\"command\":\"adjust_town_sentiment\",\"arguments\":{\"axis\":\"community\",\"delta\":2,\"reason\":\"helpful town discussion\"}}",
                "applied"
            ),
            (
                "adjust_town_sentiment npc-axis cap",
                "{\"intent_id\":\"smoke_sent_002\",\"npc_id\":\"lewis\",\"command\":\"adjust_town_sentiment\",\"arguments\":{\"axis\":\"community\",\"delta\":1,\"reason\":\"repeat same npc axis\"}}",
                "rejected"
            ),
            (
                "adjust_town_sentiment daily axis cap boundary",
                "{\"intent_id\":\"smoke_sent_003\",\"npc_id\":\"pierre\",\"command\":\"adjust_town_sentiment\",\"arguments\":{\"axis\":\"economy\",\"delta\":5,\"reason\":\"market signal 1\"}}",
                "applied"
            ),
            (
                "adjust_town_sentiment daily axis cap exceeded",
                "{\"intent_id\":\"smoke_sent_004\",\"npc_id\":\"robin\",\"command\":\"adjust_town_sentiment\",\"arguments\":{\"axis\":\"economy\",\"delta\":6,\"reason\":\"out of range and cap\"}}",
                "rejected"
            ),
            (
                "unknown command",
                "{\"intent_id\":\"smoke_unk_001\",\"npc_id\":\"lewis\",\"command\":\"launch_rocket\",\"arguments\":{}}",
                "rejected"
            )
        };

        var pass = 0;
        var fail = 0;
        var total = 0;
        var liveState = _state;
        var baselineState = CloneSaveState(liveState);

        Monitor.Log("Running slrpg_intent_smoketest...", LogLevel.Info);

        try
        {
            _state = CloneSaveState(baselineState);

            foreach (var t in tests)
            {
                try
                {
                    var r = _intentResolver.ResolveFromStreamLine(_state, t.Json);
                    var actual = r.AppliedOk ? "applied" : r.IsDuplicate ? "duplicate" : r.IsRejected ? "rejected" : "none";

                    if (string.Equals(actual, t.Expected, StringComparison.OrdinalIgnoreCase))
                    {
                        pass++;
                        Monitor.Log($"[PASS] {t.Name} -> {actual}", LogLevel.Info);
                    }
                    else
                    {
                        fail++;
                        Monitor.Log($"[FAIL] {t.Name} -> expected {t.Expected}, got {actual}", LogLevel.Warn);
                    }
                }
                catch (Exception ex)
                {
                    fail++;
                    Monitor.Log($"[FAIL] {t.Name} threw: {ex.Message}", LogLevel.Error);
                }
            }
            total += tests.Length;

            if (_npcAskGateService is not null)
            {
                var gateReject = _npcAskGateService.Evaluate(
                    _state,
                    "Linus",
                    NpcVerbalProfile.Recluse,
                    heartLevel: 0,
                    askTopic: "manual_market",
                    timeOfDay: 2300,
                    isRaining: true);
                if (gateReject.Decision == NpcAskDecision.Reject)
                {
                    pass++;
                    Monitor.Log("[PASS] ask gate reject scenario -> reject", LogLevel.Info);
                }
                else
                {
                    fail++;
                    Monitor.Log($"[FAIL] ask gate reject scenario -> expected reject, got {gateReject.Decision}", LogLevel.Warn);
                }

                var gateAccept = _npcAskGateService.Evaluate(
                    _state,
                    "Lewis",
                    NpcVerbalProfile.Professional,
                    heartLevel: 8,
                    askTopic: "manual_market",
                    timeOfDay: 1200,
                    isRaining: false);
                if (gateAccept.Decision == NpcAskDecision.Accept)
                {
                    pass++;
                    Monitor.Log("[PASS] ask gate accept scenario -> accept", LogLevel.Info);
                }
                else
                {
                    fail++;
                    Monitor.Log($"[FAIL] ask gate accept scenario -> expected accept, got {gateAccept.Decision}", LogLevel.Warn);
                }

                total += 2;
            }

            _state = CloneSaveState(baselineState);
            var ambient = RunAmbientIntentSmokeTests();
            pass += ambient.Pass;
            fail += ambient.Fail;
            total += ambient.Total;

            var targeted = RunTargetedRegressionChecks();
            pass += targeted.Pass;
            fail += targeted.Fail;
            total += targeted.Total;
        }
        finally
        {
            _state = liveState;
        }

        Monitor.Log($"Intent smoketest complete: pass={pass} fail={fail} total={total}", fail == 0 ? LogLevel.Info : LogLevel.Warn);
    }

    private void OnTargetedRegressionCommand(string name, string[] args)
    {
        var liveState = _state;
        var baselineState = CloneSaveState(liveState);

        try
        {
            _state = CloneSaveState(baselineState);
            var result = RunTargetedRegressionChecks();
            Monitor.Log(
                $"Targeted regression complete: pass={result.Pass} fail={result.Fail} total={result.Total}",
                result.Fail == 0 ? LogLevel.Info : LogLevel.Warn);
        }
        finally
        {
            _state = liveState;
        }
    }

    private (int Pass, int Fail, int Total) RunTargetedRegressionChecks()
    {
        var pass = 0;
        var fail = 0;
        var total = 0;

        var routing = RunPlayerChatRoutingRegressionChecks();
        pass += routing.Pass;
        fail += routing.Fail;
        total += routing.Total;

        if (RunPassOutPublicationRegressionCheck())
            pass++;
        else
            fail++;
        total++;

        if (RunMarketOutlookRegressionCheck())
            pass++;
        else
            fail++;
        total++;

        var ambientLane = RunAmbientCommandLaneRegressionChecks();
        pass += ambientLane.Pass;
        fail += ambientLane.Fail;
        total += ambientLane.Total;

        return (pass, fail, total);
    }

    private bool RunPassOutPublicationRegressionCheck()
    {
        if (_newspaperService is null)
        {
            Monitor.Log("[FAIL] pass-out publication regression -> NewspaperService unavailable", LogLevel.Warn);
            return false;
        }

        _state.Calendar.Day = Math.Max(3, _state.Calendar.Day);
        var yesterday = _state.Calendar.Day - 1;
        _state.TownMemory.Events.Add(new TownMemoryEvent
        {
            EventId = $"regression_passout_{yesterday}",
            Kind = "pass_out",
            Summary = "A farmer was found passed out near the bus stop.",
            Day = yesterday,
            Location = "Bus Stop",
            Severity = 2,
            Visibility = "public",
            Tags = new[] { "late-night", "pass-out", "rescue" }
        });

        var issue = _newspaperService.BuildIssue(_state, _config, player2: null, _player2Key: null);
        var published = issue.Articles.Any(a =>
            a.Title.Contains("Late-Night Collapse", StringComparison.OrdinalIgnoreCase)
            || a.Content.Contains("passed out", StringComparison.OrdinalIgnoreCase));

        if (published)
            Monitor.Log("[PASS] pass-out publication regression includes late-night collapse coverage", LogLevel.Info);
        else
            Monitor.Log("[FAIL] pass-out publication regression missing expected pass-out article", LogLevel.Warn);

        return published;
    }

    private bool RunMarketOutlookRegressionCheck()
    {
        if (_newspaperService is null)
        {
            Monitor.Log("[FAIL] market outlook regression -> NewspaperService unavailable", LogLevel.Warn);
            return false;
        }

        _state.Calendar.Day = Math.Max(4, _state.Calendar.Day);
        _state.Economy.Crops.Clear();
        _state.Economy.Crops["potato"] = new CropEconomyEntry
        {
            BasePrice = 80,
            PriceYesterday = 80,
            PriceToday = 104,
            DemandFactor = 1.06f,
            SupplyPressureFactor = 0.95f,
            ScarcityBonus = 0.04f
        };
        _state.Economy.Crops["cauliflower"] = new CropEconomyEntry
        {
            BasePrice = 175,
            PriceYesterday = 175,
            PriceToday = 142,
            DemandFactor = 0.97f,
            SupplyPressureFactor = 1.07f,
            ScarcityBonus = 0.0f
        };

        var issue = _newspaperService.BuildIssue(_state, _config, player2: null, _player2Key: null);
        var hints = issue.PredictiveHints
            .Where(h => !string.IsNullOrWhiteSpace(h))
            .ToList();
        var hasLiveCropReference = hints.Any(h =>
            h.Contains("Potato", StringComparison.OrdinalIgnoreCase)
            || h.Contains("Cauliflower", StringComparison.OrdinalIgnoreCase));

        var passed = hints.Count > 0 && hasLiveCropReference;
        if (passed)
            Monitor.Log("[PASS] market outlook regression uses live movers in printed hints", LogLevel.Info);
        else
            Monitor.Log("[FAIL] market outlook regression did not surface live mover data", LogLevel.Warn);

        return passed;
    }

    private (int Pass, int Fail, int Total) RunAmbientCommandLaneRegressionChecks()
    {
        const string npcId = "ambient_lane_regression_npc";
        const string blockedCommand = "propose_quest";
        const string allowedCommand = "record_town_event";
        const string blockedPayload = "{\"intent_id\":\"ambient_lane_block_001\",\"npc_id\":\"ambient_lane_regression_npc\",\"command\":\"propose_quest\",\"arguments\":{\"template_id\":\"gather_crop\",\"target\":\"potato\",\"urgency\":\"low\"}}";
        const string allowedPayload = "{\"intent_id\":\"ambient_lane_allow_001\",\"npc_id\":\"ambient_lane_regression_npc\",\"command\":\"record_town_event\",\"arguments\":{\"kind\":\"community\",\"summary\":\"Neighbors shared notes near the board this afternoon.\",\"location\":\"Town Square\",\"severity\":2,\"visibility\":\"public\",\"tags\":[\"community\",\"board\"]}}";

        var pass = 0;
        var fail = 0;
        var previousContext = _npcLastContextTagById.TryGetValue(npcId, out var contextTag) ? contextTag : null;
        var hadShortName = _player2NpcShortNameById.TryGetValue(npcId, out var previousShortName);

        try
        {
            _npcLastContextTagById[npcId] = "npc_to_npc_ambient";
            _player2NpcShortNameById[npcId] = "AmbientRegression";

            _state.Telemetry.Daily.AmbientCommandRejectedByType.TryGetValue(blockedCommand, out var rejectBefore);
            var blockedApplied = TryApplyNpcCommandFromLine(blockedPayload);
            _state.Telemetry.Daily.AmbientCommandRejectedByType.TryGetValue(blockedCommand, out var rejectAfter);
            if (!blockedApplied && rejectAfter == rejectBefore + 1)
            {
                pass++;
                Monitor.Log("[PASS] ambient command lane regression blocks propose_quest by policy", LogLevel.Info);
            }
            else
            {
                fail++;
                Monitor.Log($"[FAIL] ambient command lane regression expected blocked propose_quest, got applied={blockedApplied} rejectedBefore={rejectBefore} rejectedAfter={rejectAfter}", LogLevel.Warn);
            }

            _state.Telemetry.Daily.AmbientCommandAppliedByType.TryGetValue(allowedCommand, out var appliedBefore);
            var allowedApplied = TryApplyNpcCommandFromLine(allowedPayload);
            _state.Telemetry.Daily.AmbientCommandAppliedByType.TryGetValue(allowedCommand, out var appliedAfter);
            if (allowedApplied && appliedAfter == appliedBefore + 1)
            {
                pass++;
                Monitor.Log("[PASS] ambient command lane regression allows record_town_event", LogLevel.Info);
            }
            else
            {
                fail++;
                Monitor.Log($"[FAIL] ambient command lane regression expected applied record_town_event, got applied={allowedApplied} appliedBefore={appliedBefore} appliedAfter={appliedAfter}", LogLevel.Warn);
            }
        }
        finally
        {
            if (previousContext is null)
                _npcLastContextTagById.TryRemove(npcId, out _);
            else
                _npcLastContextTagById[npcId] = previousContext;

            if (hadShortName && previousShortName is not null)
                _player2NpcShortNameById[npcId] = previousShortName;
            else
                _player2NpcShortNameById.Remove(npcId);
        }

        return (pass, fail, 2);
    }

    private (int Pass, int Fail, int Total) RunAmbientIntentSmokeTests()
    {
        var ambientTests = new (string Name, string Command, string SourceNpcId, string Json, bool ExpectApplied)[]
        {
            (
                "ambient propose_quest",
                "propose_quest",
                "ambient_smoke_quest",
                "{\"intent_id\":\"ambient_smoke_pq_001\",\"npc_id\":\"ambient_smoke_quest\",\"command\":\"propose_quest\",\"arguments\":{\"template_id\":\"gather_crop\",\"target\":\"corn\",\"urgency\":\"medium\"}}",
                false
            ),
            (
                "ambient adjust_reputation",
                "adjust_reputation",
                "ambient_smoke_rep",
                "{\"intent_id\":\"ambient_smoke_rep_001\",\"npc_id\":\"ambient_smoke_rep\",\"command\":\"adjust_reputation\",\"arguments\":{\"target\":\"haley\",\"delta\":2}}",
                false
            ),
            (
                "ambient shift_interest_influence",
                "shift_interest_influence",
                "ambient_smoke_interest",
                "{\"intent_id\":\"ambient_smoke_int_001\",\"npc_id\":\"ambient_smoke_interest\",\"command\":\"shift_interest_influence\",\"arguments\":{\"interest\":\"farmers_circle\",\"delta\":1}}",
                false
            ),
            (
                "ambient apply_market_modifier",
                "apply_market_modifier",
                "ambient_smoke_market",
                "{\"intent_id\":\"ambient_smoke_mkt_001\",\"npc_id\":\"ambient_smoke_market\",\"command\":\"apply_market_modifier\",\"arguments\":{\"crop\":\"corn\",\"delta_pct\":0.05,\"duration_days\":2}}",
                false
            ),
            (
                "ambient publish_rumor",
                "publish_rumor",
                "ambient_smoke_rumor",
                "{\"intent_id\":\"ambient_smoke_rmr_001\",\"npc_id\":\"ambient_smoke_rumor\",\"command\":\"publish_rumor\",\"arguments\":{\"topic\":\"Bakery line was longer than usual today.\",\"confidence\":0.6,\"target_group\":\"shopkeepers_guild\"}}",
                false
            ),
            (
                "ambient publish_article",
                "publish_article",
                "ambient_smoke_article",
                "{\"intent_id\":\"ambient_smoke_art_001\",\"npc_id\":\"ambient_smoke_article\",\"command\":\"publish_article\",\"arguments\":{\"title\":\"Square Cleanup\",\"content\":\"Neighbors coordinated a quick cleanup near the fountain before dusk.\",\"category\":\"community\"}}",
                false
            ),
            (
                "ambient record_memory_fact",
                "record_memory_fact",
                "ambient_smoke_memory",
                "{\"intent_id\":\"ambient_smoke_mem_001\",\"npc_id\":\"ambient_smoke_memory\",\"command\":\"record_memory_fact\",\"arguments\":{\"category\":\"event\",\"text\":\"Noted the farmer helping carry seed bags before noon.\",\"weight\":2}}",
                true
            ),
            (
                "ambient record_town_event",
                "record_town_event",
                "ambient_smoke_event",
                "{\"intent_id\":\"ambient_smoke_evt_001\",\"npc_id\":\"ambient_smoke_event\",\"command\":\"record_town_event\",\"arguments\":{\"kind\":\"community\",\"summary\":\"Vendors coordinated a quick cleanup around the board this afternoon.\",\"location\":\"Town Square\",\"severity\":2,\"visibility\":\"public\",\"tags\":[\"community\",\"cleanup\"]}}",
                true
            ),
            (
                "ambient adjust_town_sentiment",
                "adjust_town_sentiment",
                "ambient_smoke_sentiment",
                "{\"intent_id\":\"ambient_smoke_sent_001\",\"npc_id\":\"ambient_smoke_sentiment\",\"command\":\"adjust_town_sentiment\",\"arguments\":{\"axis\":\"community\",\"delta\":2,\"reason\":\"ambient civic momentum\"}}",
                false
            )
        };

        var pass = 0;
        var fail = 0;
        var priorContextByNpcId = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var priorShortNameByNpcId = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        try
        {
            foreach (var test in ambientTests)
            {
                if (!priorContextByNpcId.ContainsKey(test.SourceNpcId))
                    priorContextByNpcId[test.SourceNpcId] = _npcLastContextTagById.TryGetValue(test.SourceNpcId, out var context) ? context : null;
                if (!priorShortNameByNpcId.ContainsKey(test.SourceNpcId))
                    priorShortNameByNpcId[test.SourceNpcId] = _player2NpcShortNameById.TryGetValue(test.SourceNpcId, out var shortName) ? shortName : null;

                _npcLastContextTagById[test.SourceNpcId] = "npc_to_npc_ambient";
                _player2NpcShortNameById[test.SourceNpcId] = test.SourceNpcId;

                _state.Telemetry.Daily.AmbientCommandAppliedByType.TryGetValue(test.Command, out var beforeAppliedCount);
                _state.Telemetry.Daily.AmbientCommandRejectedByType.TryGetValue(test.Command, out var beforeRejectedCount);
                var applied = TryApplyNpcCommandFromLine(test.Json);
                _state.Telemetry.Daily.AmbientCommandAppliedByType.TryGetValue(test.Command, out var afterAppliedCount);
                _state.Telemetry.Daily.AmbientCommandRejectedByType.TryGetValue(test.Command, out var afterRejectedCount);

                var passed = test.ExpectApplied
                    ? applied && afterAppliedCount == beforeAppliedCount + 1
                    : !applied && afterRejectedCount == beforeRejectedCount + 1;

                if (passed)
                {
                    pass++;
                    Monitor.Log($"[PASS] {test.Name} -> {(test.ExpectApplied ? "applied" : "rejected")}", LogLevel.Info);
                }
                else
                {
                    fail++;
                    Monitor.Log(
                        $"[FAIL] {test.Name} -> expected {(test.ExpectApplied ? "applied" : "rejected")} with ambient counter increment, got applied={applied} appliedBefore={beforeAppliedCount} appliedAfter={afterAppliedCount} rejectedBefore={beforeRejectedCount} rejectedAfter={afterRejectedCount}",
                        LogLevel.Warn);
                }
            }
        }
        finally
        {
            foreach (var entry in priorContextByNpcId)
            {
                if (entry.Value is null)
                    _npcLastContextTagById.TryRemove(entry.Key, out _);
                else
                    _npcLastContextTagById[entry.Key] = entry.Value;
            }

            foreach (var entry in priorShortNameByNpcId)
            {
                if (entry.Value is null)
                    _player2NpcShortNameById.Remove(entry.Key);
                else
                    _player2NpcShortNameById[entry.Key] = entry.Value;
            }
        }

        return (pass, fail, ambientTests.Length);
    }

    private (int Pass, int Fail, int Total) RunPlayerChatRoutingRegressionChecks()
    {
        const string npcId = "routing_smoke_npc";
        const string ambientLine = "{\"npc_id\":\"routing_smoke_npc\",\"message\":\"Ambient chatter should not enter player chat.\"}";
        const string playerReplyLine = "{\"npc_id\":\"routing_smoke_npc\",\"message\":\"Sure, I can chat for a minute.\"}";

        var pass = 0;
        var fail = 0;

        try
        {
            ResetNpcRoutingSmokeState(npcId);

            _npcUiPendingById[npcId] = 1;
            var streamRoutingQueue = _npcResponseRoutingById.GetOrAdd(npcId, _ => new ConcurrentQueue<bool>());
            streamRoutingQueue.Enqueue(true);
            var streamRouted = CaptureNpcUiMessage(ambientLine, allowPlayerChatRouting: false);
            var streamLeak = DequeueNpcUiMessage(npcId);
            if (!streamRouted && streamLeak is null)
            {
                pass++;
                Monitor.Log("[PASS] routing regression stream lane blocks ambient line from player chat", LogLevel.Info);
            }
            else
            {
                fail++;
                Monitor.Log($"[FAIL] routing regression stream lane expected no route, got routed={streamRouted} leaked='{streamLeak}'", LogLevel.Warn);
            }

            ResetNpcRoutingSmokeState(npcId);

            _npcUiPendingById[npcId] = 1;
            var denyQueue = _npcResponseRoutingById.GetOrAdd(npcId, _ => new ConcurrentQueue<bool>());
            denyQueue.Enqueue(false);
            var denyRouted = CaptureNpcUiMessage(ambientLine, allowPlayerChatRouting: true);
            var denyLeak = DequeueNpcUiMessage(npcId);
            if (!denyRouted && denyLeak is null)
            {
                pass++;
                Monitor.Log("[PASS] routing regression deny token blocks ambient line from player chat", LogLevel.Info);
            }
            else
            {
                fail++;
                Monitor.Log($"[FAIL] routing regression deny token expected no route, got routed={denyRouted} leaked='{denyLeak}'", LogLevel.Warn);
            }

            ResetNpcRoutingSmokeState(npcId);

            _npcUiPendingById[npcId] = 1;
            var allowQueue = _npcResponseRoutingById.GetOrAdd(npcId, _ => new ConcurrentQueue<bool>());
            allowQueue.Enqueue(true);
            var allowRouted = CaptureNpcUiMessage(playerReplyLine, allowPlayerChatRouting: true);
            var allowMsg = DequeueNpcUiMessage(npcId);
            if (allowRouted && !string.IsNullOrWhiteSpace(allowMsg))
            {
                pass++;
                Monitor.Log("[PASS] routing regression player lane still delivers intended chat reply", LogLevel.Info);
            }
            else
            {
                fail++;
                Monitor.Log($"[FAIL] routing regression player lane expected routed reply, got routed={allowRouted} message='{allowMsg}'", LogLevel.Warn);
            }
        }
        finally
        {
            ResetNpcRoutingSmokeState(npcId);
        }

        return (pass, fail, 3);
    }

    private void ResetNpcRoutingSmokeState(string npcId)
    {
        _npcUiMessagesById.TryRemove(npcId, out _);
        _npcResponseRoutingById.TryRemove(npcId, out _);
        _npcUiPendingById.TryRemove(npcId, out _);
        _npcLastReceivedMessageById.TryRemove(npcId, out _);
        _npcLastNonPlayerMessageById.TryRemove(npcId, out _);
        _npcLastNonPlayerMessageUtcById.TryRemove(npcId, out _);
        _npcLastPlayerPromptById.TryRemove(npcId, out _);
    }

    private void OnAnchorSmokeTestCommand(string name, string[] args)
    {
        if (_anchorEventService is null)
        {
            Monitor.Log("Anchor service unavailable.", LogLevel.Warn);
            return;
        }

        var prevDay = _state.Calendar.Day;
        var prevEco = _state.Social.TownSentiment.Economy;

        _state.Calendar.Day = Math.Max(7, _state.Calendar.Day);
        _state.Social.TownSentiment.Economy = -35;

        var first = _anchorEventService.TryTriggerEmergencyTownHall(_state, out var note1);
        var second = _anchorEventService.TryTriggerEmergencyTownHall(_state, out var note2);

        // Force resolution path check.
        if (_state.Quests.Available.FirstOrDefault(q => q.Source == "anchor_event") is { } anchorQuest)
        {
            anchorQuest.Status = "completed";
            _state.Quests.Available.Remove(anchorQuest);
            _state.Quests.Completed.Add(anchorQuest);
        }

        _anchorEventService.TryResolveEmergencyTownHall(_state);
        var resolved = _state.Facts.Facts.ContainsKey("anchor:town_hall_crisis:status:resolved");

        Monitor.Log($"Anchor smoketest | firstTrigger={first} secondTrigger={second} resolved={resolved}", LogLevel.Info);
        if (first)
            Monitor.Log($"Anchor note: {note1}", LogLevel.Info);
        if (!string.IsNullOrWhiteSpace(note2))
            Monitor.Log($"Anchor second-note: {note2}", LogLevel.Debug);

        _state.Calendar.Day = prevDay;
        _state.Social.TownSentiment.Economy = prevEco;
    }

    private void OnDemoBootstrapCommand(string name, string[] args)
    {
        if (!Context.IsWorldReady || _salesIngestionService is null || _economyService is null)
        {
            Monitor.Log("World not ready.", LogLevel.Warn);
            return;
        }

        // Seed a reproducible scenario for vertical-slice QA.
        _state.Calendar.Season = "summer";
        _state.Calendar.Day = Math.Max(_state.Calendar.Day, 7);

        _state.Social.TownSentiment.Economy = -35; // primes anchor trigger path
        _state.Social.TownSentiment.Community = Math.Min(_state.Social.TownSentiment.Community, 0);

        _economyService.EnsureInitialized(_state.Economy);
        _salesIngestionService.AddSale("blueberry", 300);
        _salesIngestionService.AddSale("blueberry", 250);
        _salesIngestionService.AddSale("blueberry", 200);

        Monitor.Log("Demo bootstrap applied: summer day>=7, economy sentiment -35, queued heavy blueberry sales.", LogLevel.Info);
        Monitor.Log("Advance one day (sleep) then run slrpg_debug_state, slrpg_open_board, slrpg_open_news, slrpg_open_rumors.", LogLevel.Info);
    }

    private void OnMemoryDebugCommand(string name, string[] args)
    {
        if (_npcMemoryService is null)
            return;

        var npc = args.Length > 0 ? args[0].Trim() : "Robin";
        Monitor.Log(_npcMemoryService.DumpNpcMemory(_state, npc), LogLevel.Info);
    }

    private void OnTownMemoryDumpCommand(string name, string[] args)
    {
        Monitor.Log($"Town memory events: {_state.TownMemory.Events.Count}", LogLevel.Info);
    }

    private void OnTownMemoryEventsCommand(string name, string[] args)
    {
        var count = 5;
        if (args.Length > 0 && int.TryParse(args[0], out var parsed))
            count = Math.Clamp(parsed, 1, 20);

        var recent = _state.TownMemory.Events
            .OrderByDescending(ev => ev.Day)
            .ThenByDescending(ev => ev.Severity)
            .Take(count)
            .ToList();
        if (recent.Count == 0)
        {
            Monitor.Log("Town memory events: none", LogLevel.Info);
            return;
        }

        Monitor.Log($"Town memory recent events ({recent.Count}):", LogLevel.Info);
        foreach (var ev in recent)
        {
            Monitor.Log(
                $"- day={ev.Day} kind={ev.Kind} source={ev.SourceNpc} loc={ev.Location} severity={ev.Severity} summary={ev.Summary}",
                LogLevel.Info);
        }
    }

    private void OnTownMemoryNpcCommand(string name, string[] args)
    {
        if (_townMemoryService is null)
            return;

        var npc = args.Length > 0 ? args[0].Trim() : "Robin";
        Monitor.Log(_townMemoryService.DumpNpcTownMemory(_state, npc), LogLevel.Info);
    }

    private void OnPlayer2LoginCommand(string name, string[] args)
    {
        if (_player2Client is null)
            return;

        if (IsPlayer2DeviceAuthUiActive())
        {
            Monitor.Log("Player2 device authorization already in progress.", LogLevel.Info);
            return;
        }

        if (!_config.EnablePlayer2)
        {
            Monitor.Log("Player2 integration disabled. Set EnablePlayer2=true in config.json.", LogLevel.Warn);
            return;
        }

        var gameClientId = ResolvePlayer2GameClientId();
        if (string.IsNullOrWhiteSpace(gameClientId))
        {
            Monitor.Log("Missing built-in Player2 game client id. Set CreatorPlayer2GameClientId in ModEntry.cs.", LogLevel.Warn);
            return;
        }

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            _player2Key = _player2Client
                .LoginViaLocalAppAsync(_config.Player2LocalAuthBaseUrl, gameClientId, cts.Token)
                .GetAwaiter().GetResult();

            // CRITICAL: SetCredentials on Player2Client and store authenticated client
            _player2Client.SetCredentials(_config.Player2LocalAuthBaseUrl, _player2Key);
            _authenticatedPlayer2Client = _player2Client;
            Monitor.Log("Player2 login successful (local app).", LogLevel.Info);
            ShowPlayer2AuthorizedToast();

            // Recreate NewspaperService with authenticated client (prefer authenticated, fallback to unauthenticated)
            var clientForService = _authenticatedPlayer2Client ?? _player2Client;
            _newspaperService = new NewspaperService(Monitor, clientForService);
            Monitor.Log("NewspaperService recreated after Player2 local app login", LogLevel.Info);
            return;
        }
        catch (Exception ex)
        {
            Monitor.Log($"Player2 local login failed: {ex.Message}", LogLevel.Warn);
        }

        try
        {
            var timeoutSec = Math.Max(30, _config.Player2DeviceAuthTimeoutSeconds);
            var authCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSec));
            try
            {
                var start = _player2Client
                    .StartDeviceAuthAsync(_config.Player2DeviceAuthBaseUrl, gameClientId, authCts.Token)
                    .GetAwaiter().GetResult();

                var verifyUrl = start.GetVerificationUrlOrFallback();
                var intervalSec = Math.Clamp(start.IntervalSeconds <= 0 ? 5 : start.IntervalSeconds, 2, 15);
                var expiresInSec = Math.Max(30, Math.Min(timeoutSec, start.ExpiresIn <= 0 ? timeoutSec : start.ExpiresIn));
                var expiresUtc = DateTime.UtcNow.AddSeconds(expiresInSec);

                BeginPlayer2DeviceAuthUi(verifyUrl, start.UserCode, expiresInSec, authCts);
                Monitor.Log($"Player2 device login started. Open: {verifyUrl}", LogLevel.Info);
                Monitor.Log($"Enter code: {start.UserCode} (expires in ~{expiresInSec}s).", LogLevel.Info);
                Monitor.Log("Waiting for device authorization...", LogLevel.Info);
                var lastPollDiagnostic = string.Empty;

                while (!authCts.IsCancellationRequested)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(intervalSec));
                    if (authCts.IsCancellationRequested)
                        break;

                    var poll = _player2Client
                        .PollDeviceAuthTokenAsync(_config.Player2DeviceAuthBaseUrl, start.DeviceCode, authCts.Token, gameClientId, start.UserCode)
                        .GetAwaiter().GetResult();
                    var diagnostic = $"{poll.Status}|{poll.ErrorMessage}";
                    if (!string.Equals(diagnostic, lastPollDiagnostic, StringComparison.Ordinal))
                    {
                        lastPollDiagnostic = diagnostic;
                        if (!string.Equals(poll.Status, "pending", StringComparison.OrdinalIgnoreCase)
                            || !string.IsNullOrWhiteSpace(poll.ErrorMessage))
                        {
                            Monitor.Log($"Player2 device auth poll: status={poll.Status} message={poll.ErrorMessage}".Trim(), LogLevel.Info);
                        }
                    }

                    if (poll.IsAuthorized && !string.IsNullOrWhiteSpace(poll.P2Key))
                    {
                        _player2Key = poll.P2Key;
                        EndPlayer2DeviceAuthUi("Authorization approved.");

                        // CRITICAL: SetCredentials on Player2Client and store authenticated client
                        _player2Client.SetCredentials(_config.Player2DeviceAuthBaseUrl, _player2Key);
                        _authenticatedPlayer2Client = _player2Client;
                        Monitor.Log("Player2 login successful (device flow).", LogLevel.Info);
                        ShowPlayer2AuthorizedToast();

                        // Recreate NewspaperService ONLY if authenticated client is available
                        if (_authenticatedPlayer2Client != null)
                        {
                            _newspaperService = new NewspaperService(Monitor, _authenticatedPlayer2Client);
                            Monitor.Log("NewspaperService recreated after Player2 device login", LogLevel.Info);
                        }
                        else
                        {
                            Monitor.Log("Skipping NewspaperService recreation - authenticated client not available yet", LogLevel.Warn);
                        }
                        return;
                    }

                    if (poll.IsTerminalFailure)
                    {
                        EndPlayer2DeviceAuthUi("Authorization failed.");
                        Monitor.Log($"Player2 device login failed: {poll.Status} {poll.ErrorMessage}".Trim(), LogLevel.Error);
                        return;
                    }

                    var secondsLeft = Math.Max(0, (int)Math.Ceiling((expiresUtc - DateTime.UtcNow).TotalSeconds));
                    UpdatePlayer2DeviceAuthUiStatus($"Waiting for authorization... {secondsLeft}s left");
                }

                var timedOut = DateTime.UtcNow >= expiresUtc;
                if (timedOut)
                {
                    EndPlayer2DeviceAuthUi("Authorization timed out.");
                    Monitor.Log("Player2 device login timed out waiting for authorization.", LogLevel.Error);
                }
                else
                {
                    EndPlayer2DeviceAuthUi("Authorization canceled.");
                    Monitor.Log("Player2 device login canceled before authorization.", LogLevel.Warn);
                }
            }
            finally
            {
                lock (_player2DeviceAuthUiLock)
                {
                    if (ReferenceEquals(_player2DeviceAuthCts, authCts))
                        _player2DeviceAuthCts = null;
                }

                authCts.Dispose();
            }
        }
        catch (Exception ex)
        {
            EndPlayer2DeviceAuthUi("Authorization failed.");
            Monitor.Log($"Player2 device login failed: {ex.Message}", LogLevel.Error);
        }
    }

    private void OnPlayer2SpawnNpcCommand(string name, string[] args)
    {
        if (_player2Client is null || string.IsNullOrWhiteSpace(_player2Key))
        {
            Monitor.Log("Login first: slrpg_p2_login", LogLevel.Warn);
            return;
        }

        try
        {
            var promptLanguageRule = I18n.BuildPromptLanguageInstruction();
            var req = new SpawnNpcRequest
            {
                ShortName = "Lewis",
                Name = "Mayor Lewis",
                CharacterDescription = "Mayor Lewis of Pelican Town in Stardew Valley. Canon-grounded, practical, cooperative, and non-fabricating.",
                SystemPrompt = "You are Mayor Lewis from Stardew Valley (Pelican Town). Stay fully in-character as an NPC, not an AI assistant. Tone: warm, practical, brief. Prefer 1-3 short sentences and natural townfolk phrasing. Avoid bullet lists unless explicitly requested. Never say phrases like 'as an AI', 'canon list', 'provided context', or 'feel free to ask'. Strict canon mode: never invent town names, regions, NPCs, or lore. Use only game_state_info facts. If uncertain, say you are unsure in-character. When asked about the market, mention at least one concrete current market signal from game_state_info (movers, oversupply, scarcity, or recommendation). For quest asks, use the propose_quest command with template_id EXACTLY one of [gather_crop, deliver_item, mine_resource, social_visit] (never quest IDs). Use target types by template: gather/deliver=item or crop, mine=resource, social_visit=NPC name. Never offer or describe a concrete player task without emitting propose_quest in the same reply. If no suitable request exists, say so in-character and do not invent a task. For social outcomes, you may use adjust_reputation sparingly for meaningful shifts only. For town-group dynamics, you may use shift_interest_influence only when discussion clearly concerns a town group priority. For market dynamics, use apply_market_modifier only when there is a clear market anomaly and keep changes bounded. For publish_article and publish_rumor, keep title+content within 100 characters total. IMPORTANT: do not promise exact gold amounts unless they match REWARD_RULES in game_state_info; prefer wording like modest/solid/high payout band. " + promptLanguageRule + " Keep command names and argument keys in English; localize only player-facing values.",
                KeepGameState = true,
                Commands = new List<SpawnNpcCommand>
                {
                    new()
                    {
                        Name = "propose_quest",
                        Description = "Propose a safe town request quest",
                        Parameters = new
                        {
                            type = "object",
                            properties = new
                            {
                                template_id = new { type = "string", @enum = new[] { "gather_crop", "deliver_item", "mine_resource", "social_visit" } },
                                target = new { type = "string", description = "gather/deliver=item_or_crop, mine=resource, social_visit=npc_name" },
                                urgency = new { type = "string", @enum = new[] { "low", "medium", "high" } },
                                count = new { type = "integer", minimum = 1, maximum = 99, description = "optional explicit quantity when the request names an exact amount" }
                            },
                            required = new[] { "template_id", "target", "urgency" },
                            additionalProperties = false
                        },
                        NeverRespondWithMessage = false
                    },
                    new()
                    {
                        Name = "adjust_reputation",
                        Description = "Adjust social relationship standing after a meaningful interaction outcome",
                        Parameters = new
                        {
                            type = "object",
                            properties = new
                            {
                                target = new { type = "string" },
                                delta = new { type = "integer", minimum = -10, maximum = 10 },
                                reason = new { type = "string" }
                            },
                            required = new[] { "target", "delta" },
                            additionalProperties = false
                        },
                        NeverRespondWithMessage = false
                    },
                    new()
                    {
                        Name = "shift_interest_influence",
                        Description = "Shift influence for a town interest group based on discussion outcomes",
                        Parameters = new
                        {
                            type = "object",
                            properties = new
                            {
                                interest = new { type = "string", @enum = new[] { "farmers_circle", "shopkeepers_guild", "adventurers_club", "nature_keepers" } },
                                delta = new { type = "integer", minimum = -5, maximum = 5 },
                                reason = new { type = "string" }
                            },
                            required = new[] { "interest", "delta" },
                            additionalProperties = false
                        },
                        NeverRespondWithMessage = false
                    },
                    new()
                    {
                        Name = "apply_market_modifier",
                        Description = "Apply a bounded temporary market modifier when a clear anomaly is present",
                        Parameters = new
                        {
                            type = "object",
                            properties = new
                            {
                                crop = new { type = "string" },
                                delta_pct = new { type = "number", minimum = -0.15, maximum = 0.15 },
                                duration_days = new { type = "integer", minimum = 1, maximum = 7 },
                                reason = new { type = "string" }
                            },
                            required = new[] { "crop", "delta_pct", "duration_days" },
                            additionalProperties = false
                        },
                        NeverRespondWithMessage = false
                    },
                    new()
                    {
                        Name = "publish_article",
                        Description = "Publish a concise in-world newspaper article (title+content <= 100 characters total)",
                        Parameters = new
                        {
                            type = "object",
                            properties = new
                            {
                                title = new { type = "string" },
                                content = new { type = "string" },
                                category = new { type = "string", @enum = new[] { "community", "market", "social", "nature" } }
                            },
                            required = new[] { "title", "content", "category" },
                            additionalProperties = false
                        },
                        NeverRespondWithMessage = false
                    },
                    new()
                    {
                        Name = "publish_rumor",
                        Description = "Publish a short town rumor with optional title/content (title+content <= 100 characters total when provided)",
                        Parameters = new
                        {
                            type = "object",
                            properties = new
                            {
                                topic = new { type = "string" },
                                title = new { type = "string" },
                                content = new { type = "string" },
                                confidence = new { type = "number" },
                                target_group = new { type = "string" }
                            },
                            required = new[] { "topic", "confidence", "target_group" },
                            additionalProperties = false
                        },
                        NeverRespondWithMessage = false
                    }
                }
            };

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            _activeNpcId = _player2Client
                .SpawnNpcAsync(_config.Player2ApiBaseUrl, _player2Key!, req, cts.Token)
                .GetAwaiter().GetResult();

            _player2NpcIdsByShortName[req.ShortName] = _activeNpcId;
            _player2NpcShortNameById[_activeNpcId] = req.ShortName;
            Monitor.Log($"Player2 NPC spawned: {req.ShortName} -> {_activeNpcId}", LogLevel.Info);

            SpawnAdditionalConfiguredNpcs();
        }
        catch (Exception ex)
        {
            Monitor.Log($"Player2 spawn failed: {ex.Message}", LogLevel.Error);
        }
    }

    private void OnPlayer2ChatCommand(string name, string[] args)
    {
        if (_player2Client is null || string.IsNullOrWhiteSpace(_player2Key) || string.IsNullOrWhiteSpace(_activeNpcId))
        {
            Monitor.Log("Need login + spawned NPC first (slrpg_p2_login, slrpg_p2_spawn).", LogLevel.Warn);
            return;
        }

        var message = args.Length == 0 ? "How is the town market today?" : string.Join(' ', args);
        SendPlayer2ChatInternal(message);
    }

    private void SpawnAdditionalConfiguredNpcs()
    {
        if (_player2Client is null || string.IsNullOrWhiteSpace(_player2Key))
            return;

        var roster = GetPlayer2SpawnRoster();
        var promptLanguageRule = I18n.BuildPromptLanguageInstruction();

        foreach (var shortName in roster)
        {
            if (string.IsNullOrWhiteSpace(shortName))
                continue;

            if (_player2NpcIdsByShortName.ContainsKey(shortName))
                continue;

            try
            {
                var identityPrompt = shortName.ToLowerInvariant() switch
                {
                    "robin" => "You are Robin, the carpenter of Pelican Town in Stardew Valley. Never claim to be Lewis or any other NPC.",
                    "pierre" => "You are Pierre, the shopkeeper of Pelican Town in Stardew Valley. Never claim to be Lewis or any other NPC.",
                    "lewis" => "You are Mayor Lewis of Pelican Town in Stardew Valley.",
                    "jas" => "You are Jas, a child living in Pelican Town in Stardew Valley. Never claim to be an adult or in your twenties.",
                    "vincent" => "You are Vincent, a child living in Pelican Town in Stardew Valley. Never claim to be an adult or in your twenties.",
                    _ => $"You are {shortName} of Pelican Town in Stardew Valley. Never claim to be another NPC."
                };

                var req = new SpawnNpcRequest
                {
                    ShortName = shortName,
                    Name = shortName,
                    CharacterDescription = $"{shortName} in Pelican Town, practical and grounded.",
                    SystemPrompt = identityPrompt + " Stay in-character, grounded in Stardew canon. Never impersonate another NPC. For quest asks, and whenever you offer a task/request, you must emit propose_quest with template_id EXACTLY one of [gather_crop, deliver_item, mine_resource, social_visit] and valid target/urgency. Never give a text-only task offer without propose_quest in the same reply. If no suitable request exists, say no request is available in-character. Use adjust_reputation sparingly for meaningful social outcomes. Use shift_interest_influence only for clear town-group dynamics. Use apply_market_modifier only when there is a clear market anomaly and keep changes bounded. For publish_article and publish_rumor, keep title+content within 100 characters total. " + promptLanguageRule + " Keep command names and argument keys in English; localize only player-facing values.",
                    KeepGameState = true,
                    Commands = new List<SpawnNpcCommand>
                    {
                        new()
                        {
                            Name = "propose_quest",
                            Description = "Propose a safe town request quest",
                            Parameters = new
                            {
                                type = "object",
                                properties = new
                                {
                                    template_id = new { type = "string", @enum = new[] { "gather_crop", "deliver_item", "mine_resource", "social_visit" } },
                                    target = new { type = "string" },
                                    urgency = new { type = "string", @enum = new[] { "low", "medium", "high" } },
                                    count = new { type = "integer", minimum = 1, maximum = 99 }
                                },
                                required = new[] { "template_id", "target", "urgency" },
                                additionalProperties = false
                            }
                        },
                        new()
                        {
                            Name = "adjust_reputation",
                            Description = "Adjust social relationship standing after a meaningful interaction outcome",
                            Parameters = new
                            {
                                type = "object",
                                properties = new
                                {
                                    target = new { type = "string" },
                                    delta = new { type = "integer", minimum = -10, maximum = 10 },
                                    reason = new { type = "string" }
                                },
                                required = new[] { "target", "delta" },
                                additionalProperties = false
                            }
                        },
                        new()
                        {
                            Name = "shift_interest_influence",
                            Description = "Shift influence for a town interest group based on discussion outcomes",
                            Parameters = new
                            {
                                type = "object",
                                properties = new
                                {
                                    interest = new { type = "string", @enum = new[] { "farmers_circle", "shopkeepers_guild", "adventurers_club", "nature_keepers" } },
                                    delta = new { type = "integer", minimum = -5, maximum = 5 },
                                    reason = new { type = "string" }
                                },
                                required = new[] { "interest", "delta" },
                                additionalProperties = false
                            }
                        },
                        new()
                        {
                            Name = "apply_market_modifier",
                            Description = "Apply a bounded temporary market modifier when a clear anomaly is present",
                            Parameters = new
                            {
                                type = "object",
                                properties = new
                                {
                                    crop = new { type = "string" },
                                    delta_pct = new { type = "number", minimum = -0.15, maximum = 0.15 },
                                    duration_days = new { type = "integer", minimum = 1, maximum = 7 },
                                    reason = new { type = "string" }
                                },
                                required = new[] { "crop", "delta_pct", "duration_days" },
                                additionalProperties = false
                            }
                        },
                        new()
                        {
                            Name = "publish_article",
                            Description = "Publish a concise in-world newspaper article (title+content <= 100 characters total)",
                            Parameters = new
                            {
                                type = "object",
                                properties = new
                                {
                                    title = new { type = "string" },
                                    content = new { type = "string" },
                                    category = new { type = "string", @enum = new[] { "community", "market", "social", "nature" } }
                                },
                                required = new[] { "title", "content", "category" },
                                additionalProperties = false
                            }
                        },
                        new()
                        {
                            Name = "publish_rumor",
                            Description = "Publish a short town rumor with optional title/content (title+content <= 100 characters total when provided)",
                            Parameters = new
                            {
                                type = "object",
                                properties = new
                                {
                                    topic = new { type = "string" },
                                    title = new { type = "string" },
                                    content = new { type = "string" },
                                    confidence = new { type = "number" },
                                    target_group = new { type = "string" }
                                },
                                required = new[] { "topic", "confidence", "target_group" },
                                additionalProperties = false
                            }
                        }
                    }
                };

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                var npcId = _player2Client.SpawnNpcAsync(_config.Player2ApiBaseUrl, _player2Key!, req, cts.Token)
                    .GetAwaiter().GetResult();

                _player2NpcIdsByShortName[shortName] = npcId;
                _player2NpcShortNameById[npcId] = shortName;
                RegisterCustomNpcSpawnAliases(shortName, npcId);
                Monitor.Log($"Player2 NPC spawned (roster): {shortName} -> {npcId}", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Monitor.Log($"Player2 additional NPC spawn failed ({shortName}): {ex.Message}", LogLevel.Warn);
            }
        }
    }

    private void SendPlayer2ChatInternal(
        string message,
        string? targetNpcId = null,
        string? requesterShortName = null,
        string? senderNameOverride = null,
        string? contextTag = null,
        bool captureForPlayerChat = true)
    {
        if (_player2Client is null || string.IsNullOrWhiteSpace(_player2Key) || string.IsNullOrWhiteSpace(_activeNpcId))
            return;

        var npcId = string.IsNullOrWhiteSpace(targetNpcId) ? _activeNpcId! : targetNpcId;
        var isPlayerInitiated = string.IsNullOrWhiteSpace(senderNameOverride);
        var routeToPlayerChat = captureForPlayerChat && isPlayerInitiated;
        if (routeToPlayerChat)
        {
            SendPlayer2ChatPerMessage(message, npcId, requesterShortName, senderNameOverride, contextTag);
            return;
        }

        EnsurePlayer2StreamReadyForChat();
        if (!IsPlayer2StreamReadyForChat())
        {
            QueuePendingStreamReplay(
                message,
                npcId,
                requesterShortName,
                senderNameOverride,
                contextTag,
                "listener-not-ready");
            Monitor.Log("Queued stream-bound NPC chat because listener is not connected yet.", LogLevel.Trace);
            return;
        }

        try
        {
            if (_config.Player2BlockChatWhenLowJoules)
            {
                var joules = TryGetJoules(out var joulesInfo);
                if (joules.HasValue && joules.Value < Math.Max(0, _config.Player2MinJoulesToChat))
                {
                    Monitor.Log($"Player2 chat blocked: low joules ({joules.Value} < {_config.Player2MinJoulesToChat}). Use slrpg_p2_status to inspect account state.", LogLevel.Warn);
                    return;
                }

                if (joulesInfo is not null)
                    Monitor.Log($"Player2 joules preflight | balance={joulesInfo.Joules} tier={joulesInfo.PatronTier}", LogLevel.Trace);
            }

            var who = string.IsNullOrWhiteSpace(requesterShortName) ? GetNpcShortNameById(npcId) : requesterShortName;
            var senderName = isPlayerInitiated ? (Game1.player?.Name ?? "Player") : senderNameOverride!.Trim();
            _npcLastContextTagById[npcId] = string.IsNullOrWhiteSpace(contextTag) ? "player_chat" : contextTag!;

            var req = new NpcChatRequest
            {
                SenderName = string.IsNullOrWhiteSpace(senderName) ? "Player" : senderName,
                SenderMessage = message,
                GameStateInfo = BuildCompactGameStateInfo(who, message, contextTag)
            };

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            _player2Client.SendNpcChatAsync(_config.Player2ApiBaseUrl, _player2Key!, npcId, req, cts.Token)
                .GetAwaiter().GetResult();

            _player2PendingResponseCount += 1;
            _player2LastChatSentUtc = DateTime.UtcNow;
            var routing = _npcResponseRoutingById.GetOrAdd(npcId, _ => new ConcurrentQueue<bool>());
            routing.Enqueue(false);
            _streamChatAwaitingResponse = true;
            _lastStreamChatMessage = message;
            _lastStreamChatTargetNpcId = npcId;
            _lastStreamChatRequesterShortName = requesterShortName;
            _lastStreamChatSenderNameOverride = senderNameOverride;
            _lastStreamChatContextTag = contextTag;
            ClearPendingStreamReplay();

            Monitor.Log($"Sent chat to Player2 NPC ({who}) id={npcId}. Keep stream listener running to receive response lines.", LogLevel.Debug);
        }
        catch (Exception ex)
        {
            QueuePendingStreamReplay(
                message,
                npcId,
                requesterShortName,
                senderNameOverride,
                contextTag,
                "send-failed");
            Monitor.Log($"Player2 chat failed: {ex.Message}", LogLevel.Error);
        }
    }

    private void SendPlayer2ChatPerMessage(
        string message,
        string npcId,
        string? requesterShortName,
        string? senderNameOverride,
        string? contextTag)
    {
        if (_player2Client is null || string.IsNullOrWhiteSpace(_player2Key))
            return;

        try
        {
            if (_config.Player2BlockChatWhenLowJoules)
            {
                var joules = TryGetJoules(out var joulesInfo);
                if (joules.HasValue && joules.Value < Math.Max(0, _config.Player2MinJoulesToChat))
                {
                    Monitor.Log($"Player2 chat blocked: low joules ({joules.Value} < {_config.Player2MinJoulesToChat}). Use slrpg_p2_status to inspect account state.", LogLevel.Warn);
                    return;
                }

                if (joulesInfo is not null)
                    Monitor.Log($"Player2 joules preflight | balance={joulesInfo.Joules} tier={joulesInfo.PatronTier}", LogLevel.Trace);
            }

            var who = string.IsNullOrWhiteSpace(requesterShortName) ? GetNpcShortNameById(npcId) : requesterShortName;
            var senderName = string.IsNullOrWhiteSpace(senderNameOverride) ? (Game1.player?.Name ?? "Player") : senderNameOverride.Trim();
            var effectiveContextTag = contextTag;
            if (string.IsNullOrWhiteSpace(effectiveContextTag)
                && TryInferManualAskContextTag(message, out var inferredManualTag))
            {
                effectiveContextTag = inferredManualTag;
            }
            var playerAskedForQuest = IsPlayerAskingForQuest(message);
            if (string.IsNullOrWhiteSpace(effectiveContextTag) && playerAskedForQuest)
                effectiveContextTag = "player_chat_quest_request";

            if (!string.IsNullOrWhiteSpace(effectiveContextTag)
                && effectiveContextTag.StartsWith("manual_", StringComparison.OrdinalIgnoreCase))
            {
                var heartLevel = GetNpcHeartLevel(who);
                var profile = _npcSpeechStyleService?.GetProfile(who) ?? NpcVerbalProfile.Traditionalist;
                var gate = _npcAskGateService?.Evaluate(
                    _state,
                    who,
                    profile,
                    heartLevel,
                    effectiveContextTag,
                    Game1.timeOfDay,
                    Game1.isRaining);

                if (gate is not null && gate.Decision != NpcAskDecision.Accept)
                {
                    if (gate.Decision == NpcAskDecision.Defer)
                        _state.Telemetry.Daily.NpcAskGateDeferred += 1;
                    else
                        _state.Telemetry.Daily.NpcAskGateRejected += 1;

                    Monitor.Log($"Manual ask gate: npc={who} topic={effectiveContextTag} decision={gate.Decision} reason={gate.ReasonCode}", LogLevel.Debug);
                    if (!string.IsNullOrWhiteSpace(gate.PlayerFacingMessage))
                    {
                        EnqueueNpcUiMessage(npcId, gate.PlayerFacingMessage);
                        if (_npcMemoryService is not null)
                            _npcMemoryService.WriteTurn(_state, who, message, gate.PlayerFacingMessage, _state.Calendar.Day);
                    }

                    return;
                }

                _state.Telemetry.Daily.NpcAskGateAccepted += 1;
                if (gate is not null)
                    Monitor.Log($"Manual ask gate: npc={who} topic={effectiveContextTag} decision={gate.Decision} reason={gate.ReasonCode}", LogLevel.Trace);
            }

            _npcLastContextTagById[npcId] = string.IsNullOrWhiteSpace(effectiveContextTag) ? "player_chat" : effectiveContextTag!;
            NpcHistorySnapshot? previousHistorySnapshot = null;
            try
            {
                using var historyCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                previousHistorySnapshot = _player2Client
                    .TryGetLatestNpcHistorySnapshotAsync(_config.Player2ApiBaseUrl, _player2Key!, npcId, historyCts.Token)
                    .GetAwaiter()
                    .GetResult();
            }
            catch
            {
            }

            if (_npcMemoryService is not null)
                _npcMemoryService.WriteTurn(_state, who, message, string.Empty, _state.Calendar.Day);

            var req = new NpcChatRequest
            {
                SenderName = string.IsNullOrWhiteSpace(senderName) ? "Player" : senderName,
                SenderMessage = message,
                GameStateInfo = BuildCompactGameStateInfo(who, message, effectiveContextTag)
            };

            _npcUiPendingById.AddOrUpdate(npcId, 1, (_, v) => v + 1);
            _npcLastPlayerChatRequestUtcById[npcId] = DateTime.UtcNow;
            _npcLastPlayerPromptById[npcId] = message;
            if (playerAskedForQuest)
                _npcLastPlayerQuestAskUtcById[npcId] = DateTime.UtcNow;

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            var immediatePayload = _player2Client.SendNpcChatAsync(_config.Player2ApiBaseUrl, _player2Key!, npcId, req, cts.Token)
                .GetAwaiter()
                .GetResult();

            var immediateLine = TryBuildImmediateNpcResponseLine(immediatePayload, npcId);
            if (!string.IsNullOrWhiteSpace(immediateLine))
            {
                _pendingPlayer2ChatLines.Enqueue(immediateLine);
            }
            else
            {
                StartPlayerChatHistoryFallback(npcId, previousHistorySnapshot, message);
            }

            Monitor.Log($"Sent player chat via per-message flow to Player2 NPC ({who}) id={npcId}.", LogLevel.Trace);
        }
        catch (Exception ex)
        {
            _npcUiPendingById.AddOrUpdate(npcId, 0, (_, v) => Math.Max(0, v - 1));
            Monitor.Log($"Player2 chat failed: {ex.Message}", LogLevel.Error);
        }
    }

    private void OnUiAskMayorForWork(string? preferredRequester = null)
    {
        var cooldownSec = Math.Max(1, _config.WorkRequestCooldownSeconds);
        if (Interlocked.CompareExchange(ref _uiWorkRequestInFlight, 0, 0) == 1)
        {
            _player2UiStatus = "Work request already in progress...";
            return;
        }

        var elapsed = DateTime.UtcNow - _lastUiWorkRequestUtc;
        if (elapsed < TimeSpan.FromSeconds(cooldownSec))
        {
            var wait = Math.Max(1, cooldownSec - (int)elapsed.TotalSeconds);
            _player2UiStatus = $"Please wait {wait}s before checking new postings again.";
            return;
        }

        var outstanding = _state.Quests.Available.Count + _state.Quests.Active.Count;
        if (outstanding >= Math.Max(1, _config.MaxOutstandingRequests))
        {
            _player2UiStatus = "Board is full. Complete current requests before checking for more.";
            return;
        }

        if (_uiManualRequestCountDay != _state.Calendar.Day)
        {
            _uiManualRequestCountDay = _state.Calendar.Day;
            _uiManualRequestCountToday = 0;
        }

        if (_uiManualRequestCountToday >= Math.Max(1, _config.MaxUiGeneratedRequestsPerDay))
        {
            _player2UiStatus = "No new postings right now. Check back tomorrow.";
            return;
        }

        Interlocked.Exchange(ref _uiWorkRequestInFlight, 1);
        _uiManualRequestCountToday += 1;

        try
        {
            var (requester, requesterNpcId) = GetNextRequester(preferredRequester);
            var prompt = $"{requester}, do you have a practical town request for me today? Use propose_quest with a safe template and parameters.";
            _lastUiWorkPrompt = prompt;
            _lastUiWorkRequesterShortName = requester;
            BeginUiBoardSearchAwaitingResult(requester, requesterNpcId);

            var connected = !string.IsNullOrWhiteSpace(_player2Key)
                && !string.IsNullOrWhiteSpace(_activeNpcId)
                && (_player2StreamDesired || Interlocked.CompareExchange(ref _player2StreamRunning, 0, 0) == 1);

            if (!connected)
            {
                _pendingUiMayorWorkRequest = prompt;
                _pendingUiRequesterShortName = requester;
                _player2UiStatus = "Connecting Town AI to request work...";
                _lastUiWorkRequestUtc = DateTime.UtcNow;
                StartPlayer2AutoConnect("ui-ask-work", force: true);
                return;
            }

            SendPlayer2ChatInternal(prompt, requesterNpcId, requester, contextTag: "player_request_board", captureForPlayerChat: false);
            _lastUiWorkRequestUtc = DateTime.UtcNow;
            _player2UiStatus = $"Checked with {requester} for new board postings.";
        }
        finally
        {
            Interlocked.Exchange(ref _uiWorkRequestInFlight, 0);
        }
    }

    private void BeginUiBoardSearchAwaitingResult(string requesterShortName, string? npcId)
    {
        _uiBoardSearchAwaitingResult = true;
        _uiBoardSearchRequesterShortName = string.IsNullOrWhiteSpace(requesterShortName) ? null : requesterShortName.Trim();
        _uiBoardSearchNpcId = string.IsNullOrWhiteSpace(npcId) ? null : npcId.Trim();
    }

    private void ClearUiBoardSearchAwaitingResult()
    {
        _uiBoardSearchAwaitingResult = false;
        _uiBoardSearchRequesterShortName = null;
        _uiBoardSearchNpcId = null;
    }

    private bool IsUiBoardSearchResponseLine(string? npcId)
    {
        if (!_uiBoardSearchAwaitingResult || string.IsNullOrWhiteSpace(npcId))
            return false;

        if (!string.IsNullOrWhiteSpace(_uiBoardSearchNpcId)
            && string.Equals(_uiBoardSearchNpcId, npcId, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(_uiBoardSearchRequesterShortName)
            && _player2NpcIdsByShortName.TryGetValue(_uiBoardSearchRequesterShortName, out var mappedNpcId)
            && string.Equals(mappedNpcId, npcId, StringComparison.OrdinalIgnoreCase))
        {
            _uiBoardSearchNpcId = mappedNpcId;
            return true;
        }

        return _npcLastContextTagById.TryGetValue(npcId, out var contextTag)
            && string.Equals(contextTag, "player_request_board", StringComparison.OrdinalIgnoreCase);
    }

    private void TryResolveUiBoardSearchStatusFromStreamLine(string? npcId, string line, bool appliedNpcCommand)
    {
        if (!IsUiBoardSearchResponseLine(npcId))
            return;

        var attemptedCommand = TryExtractCommandNameFromLine(line);
        var message = TryExtractMessageFromLine(line);

        if (appliedNpcCommand && attemptedCommand.Equals("propose_quest", StringComparison.OrdinalIgnoreCase))
        {
            _player2UiStatus = BoardSearchStatusPostingAdded;
            ClearUiBoardSearchAwaitingResult();
            return;
        }

        if (attemptedCommand.Equals("propose_quest", StringComparison.OrdinalIgnoreCase))
        {
            _player2UiStatus = BoardSearchStatusNoPostingCreated;
            ClearUiBoardSearchAwaitingResult();
            return;
        }

        if (!string.IsNullOrWhiteSpace(message))
        {
            _player2UiStatus = LooksLikeQuestDecline(message)
                ? BoardSearchStatusNoRequest
                : BoardSearchStatusNoPostingCreated;
            ClearUiBoardSearchAwaitingResult();
            return;
        }

        if (appliedNpcCommand)
        {
            _player2UiStatus = BoardSearchStatusNoPostingCreated;
            ClearUiBoardSearchAwaitingResult();
        }
    }

    private (string RequesterShortName, string? NpcId) GetNextRequester(string? preferredRequester = null)
    {
        var roster = GetExpandedNpcRoster();

        if (roster.Count == 0)
            return ("Mayor Lewis", _activeNpcId);

        if (!string.IsNullOrWhiteSpace(preferredRequester)
            && _player2NpcIdsByShortName.TryGetValue(preferredRequester, out var preferredNpcId))
            return (preferredRequester, preferredNpcId);

        for (var i = 0; i < roster.Count; i++)
        {
            var idx = (_uiRequesterRoundRobinIndex + i) % roster.Count;
            var candidate = roster[idx];
            if (!_player2NpcIdsByShortName.TryGetValue(candidate, out var npcId))
                continue;

            _uiRequesterRoundRobinIndex = (idx + 1) % roster.Count;
            return (candidate, npcId);
        }

        // fallback to first configured name + active NPC id
        return (roster[0], _activeNpcId);
    }

    private string GetNpcShortNameById(string npcId)
    {
        foreach (var kv in _player2NpcIdsByShortName)
        {
            if (string.Equals(kv.Value, npcId, StringComparison.OrdinalIgnoreCase))
                return kv.Key;
        }

        return "NPC";
    }

    private void OnPlayer2ReadOnceCommand(string name, string[] args)
    {
        if (_player2Client is null || string.IsNullOrWhiteSpace(_player2Key))
        {
            Monitor.Log("Need login first (slrpg_p2_login).", LogLevel.Warn);
            return;
        }

        if (Interlocked.Exchange(ref _player2ReadInFlight, 1) == 1)
        {
            Monitor.Log("Player2 read already in progress.", LogLevel.Warn);
            return;
        }

        Monitor.Log("Reading one Player2 stream line in background...", LogLevel.Trace);
        _player2ReadStartedUtc = DateTime.UtcNow;
        _player2ReadCts?.Cancel();
        _player2ReadCts = new CancellationTokenSource(TimeSpan.FromSeconds(12));

        _ = Task.Run(async () =>
        {
            try
            {
                var line = await _player2Client.ReadOneNpcResponseLineAsync(_config.Player2ApiBaseUrl, _player2Key!, _player2ReadCts.Token);
                _pendingPlayer2Lines.Enqueue(string.IsNullOrWhiteSpace(line) ? "__EMPTY__" : line);
            }
            catch (Exception ex)
            {
                _pendingPlayer2Lines.Enqueue("__ERR__" + ex.Message);
            }
            finally
            {
                _player2ReadStartedUtc = default;
                Interlocked.Exchange(ref _player2ReadInFlight, 0);
            }
        });
    }

    private void OnPlayer2ReadResetCommand(string name, string[] args)
    {
        _player2ReadCts?.Cancel();
        _player2ReadStartedUtc = default;
        Interlocked.Exchange(ref _player2ReadInFlight, 0);
        Monitor.Log("Player2 read state reset.", LogLevel.Info);
    }

    private void OnPlayer2StreamStartCommand(string name, string[] args)
    {
        if (_player2Client is null || string.IsNullOrWhiteSpace(_player2Key))
        {
            Monitor.Log("Need login first (slrpg_p2_login).", LogLevel.Warn);
            return;
        }

        _player2StreamDesired = true;
        _player2StreamBackoffSec = 1;
        _player2NextReconnectUtc = DateTime.UtcNow;

        if (Interlocked.CompareExchange(ref _player2StreamRunning, 0, 0) == 1)
        {
            Monitor.Log("Player2 stream listener already running.", LogLevel.Warn);
            return;
        }

        StartPlayer2StreamListenerAttempt();
    }

    private void TryStartPendingNewspaperBuild(string source)
    {
        if (_newspaperService is null)
            return;

        if (Interlocked.CompareExchange(ref _newspaperBuildInFlight, 1, 0) == 1)
            return;

        var targetDay = _pendingNewspaperRefreshDay;
        if (targetDay < 0 || targetDay != _state.Calendar.Day)
        {
            Interlocked.Exchange(ref _newspaperBuildInFlight, 0);
            return;
        }

        SaveState snapshot;
        try
        {
            snapshot = CloneSaveState(_state);
        }
        catch (Exception ex)
        {
            Monitor.Log($"Failed to clone state for newspaper build after {source}: {ex.Message}", LogLevel.Warn);
            Interlocked.Exchange(ref _newspaperBuildInFlight, 0);
            return;
        }

        var service = _newspaperService;
        var clientForService = _authenticatedPlayer2Client ?? _player2Client;
        var player2Key = _player2Key;
        var apiBaseUrl = _config.Player2ApiBaseUrl;

        if (!string.IsNullOrWhiteSpace(apiBaseUrl) && !string.IsNullOrWhiteSpace(player2Key))
            clientForService?.SetCredentials(apiBaseUrl, player2Key);

        _ = Task.Run(async () =>
        {
            try
            {
                var issue = await service.BuildIssueAsync(snapshot, null);
                _completedNewspaperIssues.Enqueue(issue);
            }
            catch (Exception ex)
            {
                Monitor.Log($"Newspaper build failed after {source}: {ex.Message}", LogLevel.Warn);
            }
            finally
            {
                Interlocked.Exchange(ref _newspaperBuildInFlight, 0);
            }
        });
    }

    private void TryApplyCompletedNewspaperIssues()
    {
        while (_completedNewspaperIssues.TryDequeue(out var issue))
        {
            if (issue.Day != _state.Calendar.Day)
                continue;

            var existingIndex = _state.Newspaper.Issues.FindIndex(i => i.Day == issue.Day);
            if (existingIndex >= 0)
                _state.Newspaper.Issues[existingIndex] = issue;
            else
                _state.Newspaper.Issues.Add(issue);

            _pendingNewspaperRefreshDay = -1;
            Monitor.Log($"Newspaper build completed: day={issue.Day}, headline='{issue.Headline}'", LogLevel.Debug);

            if (_pendingDayStartStreamRecycleDay == issue.Day)
            {
                if (IssueContainsEditorArticle(issue))
                    ShowDayStartNewspaperReadyToast(issue);

                _pendingDayStartStreamRecycleDay = -1;
                TryRecyclePlayer2StreamAfterDayStartIssue(issue.Day);
            }
        }
    }

    private void TryApplyCompletedNpcPublishHeadlineUpdates()
    {
        while (_completedNpcPublishHeadlineUpdates.TryDequeue(out var update))
        {
            if (update.Day != _state.Calendar.Day)
                continue;

            UpsertPendingNpcPublishUpdate(update);
        }

        TryApplyPendingNpcPublishUpdates();
    }

    private void TryApplyPendingNpcPublishUpdates()
    {
        for (var i = _pendingNpcPublishHeadlineUpdates.Count - 1; i >= 0; i--)
        {
            var pending = _pendingNpcPublishHeadlineUpdates[i];
            if (pending.Day != _state.Calendar.Day)
            {
                _pendingNpcPublishHeadlineUpdates.RemoveAt(i);
                continue;
            }

            if (!CanPublishNpcNewsNow(pending.Day))
                continue;

            if (!TryReplaceTodayIssueWithNpcArticle(pending.Command, pending.OutcomeId, pending.Headline))
            {
                var hasTodayIssue = _state.Newspaper.Issues.Any(issue => issue.Day == _state.Calendar.Day);
                if (!hasTodayIssue)
                {
                    _pendingNewspaperRefreshDay = _state.Calendar.Day;
                    TryRefreshPendingNewspaperIssue($"npc-command:{pending.Command}:afternoon");
                }

                continue;
            }

            ShowNewspaperCommandNotification(pending.Command, pending.OutcomeId, pending.SourceNpcId);
            Monitor.Log($"Published deferred NPC news for {pending.Command} in afternoon.", LogLevel.Trace);
            _lastNpcPublishAppliedDay = _state.Calendar.Day;
            _lastNpcPublishAppliedTimeOfDay = Game1.timeOfDay;
            _pendingNpcPublishHeadlineUpdates.RemoveAt(i);
        }
    }

    private void UpsertPendingNpcPublishUpdate(NpcPublishHeadlineUpdate update)
    {
        var existingIndex = _pendingNpcPublishHeadlineUpdates.FindIndex(pending =>
            IsSameNpcPublishHeadlineTarget(pending, update));
        if (existingIndex < 0)
        {
            _pendingNpcPublishHeadlineUpdates.Add(update);
            return;
        }

        var existing = _pendingNpcPublishHeadlineUpdates[existingIndex];
        if (string.IsNullOrWhiteSpace(update.Headline) && !string.IsNullOrWhiteSpace(existing.Headline))
        {
            update = new NpcPublishHeadlineUpdate
            {
                Day = update.Day,
                Command = update.Command,
                OutcomeId = update.OutcomeId,
                SourceNpcId = update.SourceNpcId,
                Headline = existing.Headline
            };
        }

        _pendingNpcPublishHeadlineUpdates[existingIndex] = update;
    }

    private bool CanPublishNpcNewsNow(int day)
    {
        if (day != _state.Calendar.Day)
            return false;

        if (Game1.timeOfDay < NpcPublishAfternoonStartTime)
            return false;

        if (_lastNpcPublishAppliedDay != day || _lastNpcPublishAppliedTimeOfDay <= 0)
            return true;

        var nowMinutes = ToMinutesFromTimeOfDay(Game1.timeOfDay);
        var lastMinutes = ToMinutesFromTimeOfDay(_lastNpcPublishAppliedTimeOfDay);
        return nowMinutes - lastMinutes >= NpcPublishMinimumIntervalMinutes;
    }

    private static int ToMinutesFromTimeOfDay(int hhmm)
    {
        var clamped = Math.Clamp(hhmm, 0, 2600);
        var hours = clamped / 100;
        var minutes = clamped % 100;
        return (hours * 60) + Math.Clamp(minutes, 0, 59);
    }

    private static bool IsSameNpcPublishHeadlineTarget(NpcPublishHeadlineUpdate left, NpcPublishHeadlineUpdate right)
    {
        return left.Day == right.Day
            && left.Command.Equals(right.Command, StringComparison.OrdinalIgnoreCase)
            && left.OutcomeId.Equals(right.OutcomeId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(left.SourceNpcId ?? string.Empty, right.SourceNpcId ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IssueContainsEditorArticle(NewspaperIssue issue)
    {
        if (issue?.Articles is null || issue.Articles.Count == 0)
            return false;

        foreach (var article in issue.Articles)
        {
            var source = (article?.SourceNpc ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(source))
                continue;

            if (source.Equals("Pelican Times Editor", StringComparison.OrdinalIgnoreCase)
                || source.Equals("Editor", StringComparison.OrdinalIgnoreCase)
                || source.Contains("Editor", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private void ShowDayStartNewspaperReadyToast(NewspaperIssue issue)
    {
        if (!Context.IsWorldReady || issue is null)
            return;

        var headline = TrimForHud(issue.Headline, 30);
        var message = I18n.Get("hud.newspaper.ready", $"Morning edition ready: {headline}", new { headline });
        Game1.addHUDMessage(new HUDMessage(message, HUDMessage.newQuest_type));
    }

    private void ShowPlayer2AuthorizedToast()
    {
        if (!Context.IsWorldReady)
            return;

        Game1.addHUDMessage(new HUDMessage(
            I18n.Get("hud.player2.authorized", "Player2 authorized. Local Insight is now active."),
            HUDMessage.newQuest_type));
    }

    private void TryShowSimulationMutationToast(NpcIntentResolveResult result, string intentLane, bool isAmbientContext)
    {
        if (!Context.IsWorldReady)
            return;
        if (result is null || string.IsNullOrWhiteSpace(result.Command))
            return;
        if (!string.Equals(intentLane, "auto", StringComparison.OrdinalIgnoreCase) && !isAmbientContext)
            return;

        var message = result.Command.ToLowerInvariant() switch
        {
            "apply_market_modifier" => I18n.Get(
                "hud.simulation.market_shift",
                $"Market shift: {QuestTextHelper.PrettyName(result.OutcomeId)} prices moved.",
                new { target = QuestTextHelper.PrettyName(result.OutcomeId) }),
            "shift_interest_influence" => I18n.Get("hud.simulation.interest_shift", "Town groups shifted their focus."),
            "adjust_town_sentiment" => I18n.Get(
                "hud.simulation.sentiment_shift",
                $"Town mood shifted around {result.OutcomeId}.",
                new { axis = result.OutcomeId }),
            _ => string.Empty
        };
        if (string.IsNullOrWhiteSpace(message))
            return;

        if (_simulationToastDay != _state.Calendar.Day)
        {
            _simulationToastDay = _state.Calendar.Day;
            _simulationToastsToday = 0;
        }

        if (_simulationToastsToday >= MaxSimulationToastsPerDay)
            return;
        if (_lastSimulationToastUtc != default
            && DateTime.UtcNow - _lastSimulationToastUtc < TimeSpan.FromSeconds(SimulationToastCooldownSeconds))
        {
            return;
        }

        Game1.addHUDMessage(new HUDMessage(message, HUDMessage.newQuest_type));
        _simulationToastsToday += 1;
        _lastSimulationToastUtc = DateTime.UtcNow;
    }

    private static SaveState CloneSaveState(SaveState state)
    {
        var json = JsonSerializer.Serialize(state);
        return JsonSerializer.Deserialize<SaveState>(json) ?? SaveState.CreateDefault();
    }

    private void ShowQuestPostedToast(string questId, string? sourceNpcId)
    {
        if (!Context.IsWorldReady || string.IsNullOrWhiteSpace(questId))
            return;

        var issuer = I18n.Get("hud.rumor.issuer.fallback", "A villager");
        if (!string.IsNullOrWhiteSpace(sourceNpcId)
            && _player2NpcShortNameById.TryGetValue(sourceNpcId, out var shortName)
            && !string.IsNullOrWhiteSpace(shortName))
        {
            issuer = shortName.Trim();
        }

        var quest = _state.Quests.Available
            .FirstOrDefault(q => q.QuestId.Equals(questId, StringComparison.OrdinalIgnoreCase));
        if (quest is not null && !string.IsNullOrWhiteSpace(quest.Issuer))
            issuer = QuestTextHelper.PrettyName(quest.Issuer);

        var title = quest is null
            ? I18n.Get("hud.rumor.title.fallback", "New request on the board")
            : QuestTextHelper.BuildQuestTitle(quest);
        var message = I18n.Get(
            "hud.rumor.posted",
            $"{TrimForHud(issuer, 18)} posted: {TrimForHud(title, 30)}",
            new { issuer = TrimForHud(issuer, 18), title = TrimForHud(title, 30) });
        Game1.addHUDMessage(new HUDMessage(message, HUDMessage.newQuest_type));
    }

    private NewspaperIssue BuildAndStoreNewspaperIssue()
    {
        if (_newspaperService is null)
            throw new InvalidOperationException("NewspaperService is not available.");

        var clientForService = _authenticatedPlayer2Client ?? _player2Client;
        var issue = _newspaperService.BuildIssue(_state, _config, clientForService, _player2Key);

        var existingIndex = _state.Newspaper.Issues.FindIndex(i => i.Day == issue.Day);
        if (existingIndex >= 0)
            _state.Newspaper.Issues[existingIndex] = issue;
        else
            _state.Newspaper.Issues.Add(issue);

        return issue;
    }

    private bool IsPlayer2ReadyForNewspaper()
    {
        if (!_config.EnablePlayer2)
            return true;

        if (string.IsNullOrWhiteSpace(_player2Key) || string.IsNullOrWhiteSpace(_activeNpcId))
            return false;

        var streamRunning = Interlocked.CompareExchange(ref _player2StreamRunning, 0, 0) == 1;
        if (!streamRunning)
            return false;

        return IsPlayer2RosterReady();
    }

    private bool IsPlayer2RosterReady()
    {
        if (string.IsNullOrWhiteSpace(_activeNpcId))
            return false;

        var roster = GetExpandedNpcRoster();

        if (roster.Count == 0)
            return true;

        foreach (var shortName in roster)
        {
            if (!_player2NpcIdsByShortName.ContainsKey(shortName))
                return false;
        }

        return true;
    }

    private void TryRecyclePlayer2StreamAfterDayStartIssue(int day)
    {
        if (!_config.EnablePlayer2 || _player2Client is null || string.IsNullOrWhiteSpace(_player2Key))
            return;

        _player2StreamDesired = true;
        _player2StreamCts?.Cancel();
        _player2StreamCts = null;
        Interlocked.Exchange(ref _player2StreamRunning, 0);
        _player2StreamConnectedUtc = default;
        _player2PendingResponseCount = 0;
        ResetNpcResponseTracking();
        _player2StreamBackoffSec = 1;
        _player2NextReconnectUtc = DateTime.UtcNow;
        StartPlayer2StreamListenerAttempt();
        Monitor.Log($"Recycled Player2 stream after day-start newspaper build (day {day}).", LogLevel.Debug);
    }

    private void OnPlayer2StreamStopCommand(string name, string[] args)
    {
        _player2StreamDesired = false;
        _player2StreamCts?.Cancel();
        _player2StreamCts = null;
        Interlocked.Exchange(ref _player2StreamRunning, 0);
        _player2StreamConnectedUtc = default;
        Monitor.Log("Stopped Player2 stream listener.", LogLevel.Info);
    }

    private void OnPlayer2StatusCommand(string name, string[] args)
    {
        var loggedIn = !string.IsNullOrWhiteSpace(_player2Key);
        var npc = string.IsNullOrWhiteSpace(_activeNpcId) ? "(none)" : _activeNpcId;
        var running = Interlocked.CompareExchange(ref _player2StreamRunning, 0, 0) == 1;
        Monitor.Log($"Player2 status | loggedIn={loggedIn} npc={npc} streamRunning={running} desired={_player2StreamDesired}", LogLevel.Info);

        if (!loggedIn || _player2Client is null)
            return;

        var joules = TryGetJoules(out var j);
        if (!joules.HasValue || j is null)
            return;

        Monitor.Log($"Player2 joules | balance={j.Joules} tier={j.PatronTier} user={j.UserId}", LogLevel.Info);
    }

    private void OnPlayer2HealthCommand(string name, string[] args)
    {
        var loggedIn = !string.IsNullOrWhiteSpace(_player2Key);
        var npc = string.IsNullOrWhiteSpace(_activeNpcId) ? "(none)" : _activeNpcId;
        var running = Interlocked.CompareExchange(ref _player2StreamRunning, 0, 0) == 1;
        var lineAgo = _player2LastLineUtc == default ? "never" : $"{(int)(DateTime.UtcNow - _player2LastLineUtc).TotalSeconds}s";
        var cmdAgo = _player2LastCommandAppliedUtc == default ? "never" : $"{(int)(DateTime.UtcNow - _player2LastCommandAppliedUtc).TotalSeconds}s";

        var joules = TryGetJoules(out var j);
        var joulesText = joules.HasValue && j is not null ? joules.Value.ToString() : "n/a";

        Monitor.Log($"P2 health | login={loggedIn} npc={npc} stream={running}/{_player2StreamDesired} joules={joulesText} pending={_player2PendingResponseCount} lastLineAgo={lineAgo} lastCmd={_player2LastCommandApplied} lastCmdAgo={cmdAgo}", LogLevel.Info);
    }

    private bool CaptureNpcUiMessage(string line, bool allowPlayerChatRouting)
    {
        try
        {
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;
            if (!root.TryGetProperty("npc_id", out var idEl) || idEl.ValueKind != JsonValueKind.String)
                return false;
            var npcId = idEl.GetString();
            if (string.IsNullOrWhiteSpace(npcId))
                return false;

            var hasPendingPlayerUi = _npcUiPendingById.TryGetValue(npcId, out var pending)
                && pending > 0;
            var routeToPlayerChat = allowPlayerChatRouting && hasPendingPlayerUi;
            if (_npcResponseRoutingById.TryGetValue(npcId, out var routingQueue) && routingQueue.TryDequeue(out var routed))
                routeToPlayerChat = allowPlayerChatRouting && routed && hasPendingPlayerUi;

            if (!root.TryGetProperty("message", out var msgEl) || msgEl.ValueKind != JsonValueKind.String)
            {
                if (routeToPlayerChat)
                    _npcUiPendingById.AddOrUpdate(npcId, 0, (_, v) => Math.Max(0, v - 1));
                return routeToPlayerChat;
            }

            var msg = msgEl.GetString();
            if (string.IsNullOrWhiteSpace(msg))
            {
                if (routeToPlayerChat)
                    _npcUiPendingById.AddOrUpdate(npcId, 0, (_, v) => Math.Max(0, v - 1));
                return routeToPlayerChat;
            }
            var rawMessage = msg.Trim();
            var now = DateTime.UtcNow;
            if (routeToPlayerChat
                && _npcLastNonPlayerMessageById.TryGetValue(npcId, out var lastNonPlayerMessage)
                && _npcLastNonPlayerMessageUtcById.TryGetValue(npcId, out var lastNonPlayerUtc)
                && now - lastNonPlayerUtc <= TimeSpan.FromSeconds(12)
                && string.Equals(lastNonPlayerMessage, rawMessage, StringComparison.Ordinal))
            {
                routeToPlayerChat = false;
            }

            _npcLastReceivedMessageById[npcId] = rawMessage;

            var playerFacingMsg = NormalizePlayerFacingNpcMessage(msg);
            if (string.IsNullOrWhiteSpace(playerFacingMsg))
                playerFacingMsg = rawMessage;
            var npcName = GetNpcShortNameById(npcId);
            playerFacingMsg = NormalizeNpcAgeReply(npcName, playerFacingMsg);

            if (routeToPlayerChat)
            {
                _npcUiPendingById.AddOrUpdate(npcId, 0, (_, v) => Math.Max(0, v - 1));
                playerFacingMsg = NormalizeNpcTimeReply(npcId, playerFacingMsg);
                var q = _npcUiMessagesById.GetOrAdd(npcId, _ => new ConcurrentQueue<string>());
                q.Enqueue(playerFacingMsg);
            }
            else
            {
                _npcLastNonPlayerMessageById[npcId] = rawMessage;
                _npcLastNonPlayerMessageUtcById[npcId] = now;
            }

            if (_npcMemoryService is not null && routeToPlayerChat)
            {
                _npcMemoryService.WriteTurn(_state, npcName, string.Empty, playerFacingMsg, _state.Calendar.Day);
            }

            return routeToPlayerChat;
        }
        catch
        {
            // ignore non-json or malformed lines for chat UI capture.
            return false;
        }
    }

    private static bool IsPlayerAskingForClockTime(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var value = text.ToLowerInvariant();
        return value.Contains("what time", StringComparison.Ordinal)
            || value.Contains("time is it", StringComparison.Ordinal)
            || value.Contains("tell me the time", StringComparison.Ordinal)
            || value.Contains("current time", StringComparison.Ordinal)
            || value.Contains("clock", StringComparison.Ordinal)
            || value.Equals("time", StringComparison.Ordinal)
            || value.Contains("time?", StringComparison.Ordinal)
            || value.Contains("hour", StringComparison.Ordinal);
    }

    private string NormalizeNpcTimeReply(string npcId, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return message;

        if (!_npcLastPlayerPromptById.TryGetValue(npcId, out var lastPrompt)
            || !IsPlayerAskingForClockTime(lastPrompt))
        {
            return message;
        }

        if (Regex.IsMatch(message, @"\b\d{1,2}:\d{2}\s*[AaPp]\.?[Mm]\.?\b", RegexOptions.CultureInvariant))
            return message;

        var minuteOnly = Regex.Match(
            message,
            @"(?<!:)\b(?<min>\d{1,2})\s*(?<ampm>[AaPp]\.?[Mm]\.?)\b",
            RegexOptions.CultureInvariant);

        if (!minuteOnly.Success)
            return message;

        if (!int.TryParse(minuteOnly.Groups["min"].Value, out var minute) || minute is < 0 or > 59)
            return message;

        var canonical = GetCurrentTimeOfDayLabel(out _, out _);
        var split = canonical.Split(" (", 2, StringSplitOptions.None);
        var clockTime = split[0];
        return $"It's {clockTime}.";
    }

    private static string NormalizeNpcAgeReply(string npcName, string message)
    {
        if (string.IsNullOrWhiteSpace(message) || !IsChildNpcName(npcName))
            return message;

        if (!LooksLikeAdultAgeClaim(message))
            return message;

        return "I'm still a kid in Pelican Town.";
    }

    private static bool LooksLikeAdultAgeClaim(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return false;

        const RegexOptions opts = RegexOptions.CultureInvariant | RegexOptions.IgnoreCase;
        var explicitYearsOld = Regex.Match(
            message,
            @"\b(?:i am|i['’]m|im|my age is)\s+(?<age>\d{2})\s*years?\s*old\b",
            opts);
        if (explicitYearsOld.Success
            && int.TryParse(explicitYearsOld.Groups["age"].Value, out var ageYearsOld)
            && ageYearsOld >= 18)
        {
            return true;
        }

        var explicitAgeSentence = Regex.Match(
            message,
            @"\b(?:i am|i['’]m|im)\s+(?<age>\d{2})(?:[.!?,]|$)",
            opts);
        if (explicitAgeSentence.Success
            && int.TryParse(explicitAgeSentence.Groups["age"].Value, out var ageSentence)
            && ageSentence >= 18)
        {
            return true;
        }

        var decadeNumber = Regex.Match(
            message,
            @"\b(?:i am|i['’]m|im)\s+in\s+my\s+(?<decade>\d{2})s\b",
            opts);
        if (decadeNumber.Success
            && int.TryParse(decadeNumber.Groups["decade"].Value, out var decade)
            && decade >= 20)
        {
            return true;
        }

        return Regex.IsMatch(message, @"\b(?:i am|i['’]m|im)\s+in\s+my\s+twenties\b", opts)
            || Regex.IsMatch(message, @"\bin\s+my\s+twenties\b", opts);
    }

    private static bool IsChildNpcName(string? npcName)
    {
        if (string.IsNullOrWhiteSpace(npcName))
            return false;

        return ChildNpcNames.Contains(npcName.Trim());
    }

    private string? DequeueNpcUiMessage(string npcId)
    {
        if (!_npcUiMessagesById.TryGetValue(npcId, out var q))
            return null;

        return q.TryDequeue(out var msg) ? msg : null;
    }

    private void EnqueueNpcUiMessage(string npcId, string message)
    {
        if (string.IsNullOrWhiteSpace(npcId) || string.IsNullOrWhiteSpace(message))
            return;

        var q = _npcUiMessagesById.GetOrAdd(npcId, _ => new ConcurrentQueue<string>());
        q.Enqueue(message.Trim());
    }

    private bool IsNpcThinking(string npcId)
    {
        return _npcUiPendingById.TryGetValue(npcId, out var c) && c > 0;
    }

    private int? TryGetJoules(out JoulesResponse? info)
    {
        info = null;
        if (_player2Client is null || string.IsNullOrWhiteSpace(_player2Key))
            return null;

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            info = _player2Client.GetJoulesAsync(_config.Player2ApiBaseUrl, _player2Key!, cts.Token).GetAwaiter().GetResult();
            return info.Joules;
        }
        catch (Exception ex)
        {
            Monitor.Log($"Player2 joules check failed: {ex.Message}", LogLevel.Warn);
            return null;
        }
    }

    private void StartPlayer2StreamListenerAttempt()
    {
        if (_player2Client is null || string.IsNullOrWhiteSpace(_player2Key))
            return;

        if (Interlocked.Exchange(ref _player2StreamRunning, 1) == 1)
            return;

        _player2LastStreamStartUtc = DateTime.UtcNow;
        _player2StreamConnectedUtc = default;
        _player2StreamCts?.Cancel();
        _player2StreamCts = new CancellationTokenSource();
        var ct = _player2StreamCts.Token;

        Monitor.Log($"Starting Player2 stream listener... (backoff={_player2StreamBackoffSec}s)", LogLevel.Debug);

        _ = Task.Run(async () =>
        {
            try
            {
                await _player2Client.StreamNpcResponsesAsync(
                    _config.Player2ApiBaseUrl,
                    _player2Key!,
                    async line =>
                    {
                        _pendingPlayer2Lines.Enqueue(line);
                        await Task.CompletedTask;
                    },
                    ct,
                    async () =>
                    {
                        _player2StreamConnectedUtc = DateTime.UtcNow;
                        await Task.CompletedTask;
                    });

                if (!ct.IsCancellationRequested)
                    _pendingPlayer2Lines.Enqueue("__ERR__Player2 stream closed by server.");
            }
            catch (Exception ex)
            {
                if (!ct.IsCancellationRequested)
                    _pendingPlayer2Lines.Enqueue("__ERR__Player2 stream failed: " + ex.Message);
            }
            finally
            {
                Interlocked.Exchange(ref _player2StreamRunning, 0);
                _player2StreamConnectedUtc = default;

                if (_player2StreamDesired)
                {
                    _player2NextReconnectUtc = DateTime.UtcNow.AddSeconds(_player2StreamBackoffSec);
                    _player2StreamBackoffSec = Math.Min(_player2StreamBackoffSec * 2, 30);
                }
            }
        });
    }

    private string BuildCompactGameStateInfo(string? npcName = null, string? playerText = null, string? contextTag = null)
    {
        SyncCalendarSeasonFromWorld();
        var localeCode = I18n.GetCurrentLocaleCode();
        var promptLanguageRule = I18n.BuildPromptLanguageInstruction();
        var currentSeason = GetCurrentSeasonLabel();
        var weather = GetCurrentWeatherLabel();
        var dayOfWeek = GetCurrentDayOfWeekLabel();
        var timeOfDay = GetCurrentTimeOfDayLabel(out var hour24, out var minute);
        var heartLevel = GetNpcHeartLevel(npcName);
        var npcReputation = GetNpcReputation(npcName);
        var npcHasMetPlayer = HasNpcMetPlayer(npcName);
        var playerName = GetPlayerDisplayNameForContext();
        var preferredAddress = npcHasMetPlayer ? playerName : "Farmer";
        var charismaStat = GetPlayerRpgStat("charisma");
        var socialStat = GetPlayerRpgStat("social");
        var speechStyleBlock = _npcSpeechStyleService?.BuildPromptBlock(
            npcName,
            heartLevel,
            Game1.isRaining,
            charismaStat,
            socialStat) ?? string.Empty;
        var movers = _state.Economy.Crops
            .OrderByDescending(kv => Math.Abs(kv.Value.PriceToday - kv.Value.PriceYesterday))
            .Take(3)
            .Select(kv => $"{kv.Key}:{kv.Value.PriceYesterday}g->{kv.Value.PriceToday}g")
            .ToArray();

        var oversupply = _state.Economy.Crops
            .OrderByDescending(kv => kv.Value.SupplyPressureFactor)
            .FirstOrDefault();

        var scarcity = _state.Economy.Crops
            .OrderByDescending(kv => kv.Value.ScarcityBonus)
            .FirstOrDefault();

        var recommendation = _state.Economy.Crops
            .OrderByDescending(kv => kv.Value.ScarcityBonus - kv.Value.SupplyPressureFactor)
            .FirstOrDefault();

        var canonNpcs = BuildCompactCanonNpcPromptList();
        var activeQuestTemplateCounts = FormatQuestTemplateCounts(_state.Quests.Active);
        var availableQuestTemplateCounts = FormatQuestTemplateCounts(_state.Quests.Available);

        var oversupplyText = oversupply.Key is null
            ? "none"
            : $"{oversupply.Key} pressure={oversupply.Value.SupplyPressureFactor:F2}";

        var scarcityText = scarcity.Key is null
            ? "none"
            : $"{scarcity.Key} scarcity={scarcity.Value.ScarcityBonus:F2}";

        var recText = recommendation.Key is null
            ? "none"
            : recommendation.Key;

        var npcMemory = string.Empty;
        var townMemory = string.Empty;
        var newsContext = BuildNewsAwarenessBlock();
        var eventsContext = BuildRecentEventAwarenessBlock(playerText);
        var vanillaDialogueContext = BuildRecentVanillaDialogueContextBlock(npcName);
        var sourceDialogueContext = BuildSourceModDialogueContextBlock(npcName, playerText);
        var vanillaDialogueFollowUpRule = string.IsNullOrWhiteSpace(vanillaDialogueContext)
            ? string.Empty
            : "FOLLOWUP_DIALOGUE_RULE: If player opens with small-talk like 'Got a minute to chat?', continue naturally from VANILLA_DIALOGUE_CONTEXT before switching topics.";
        var playerAskedForRequest = IsPlayerAskingForQuest(playerText);
        var questDiversityContext = BuildQuestDiversityBlock(npcName, playerAskedForRequest);
        var effectiveContextTag = string.IsNullOrWhiteSpace(contextTag) ? "player_chat" : contextTag!;
        var followUpContextRule = string.Equals(effectiveContextTag, "player_chat_followup", StringComparison.OrdinalIgnoreCase)
            ? "FOLLOWUP_CONTEXT_RULE: This chat started immediately after vanilla NPC dialogue; prioritize continuity with VANILLA_DIALOGUE_CONTEXT."
            : string.Empty;
        var commandPolicyRule = _commandPolicyService?.BuildPromptRule(effectiveContextTag) ?? string.Empty;
        var ambientEventFirstRule = string.Equals(effectiveContextTag, "npc_to_npc_ambient", StringComparison.OrdinalIgnoreCase)
            ? "AMBIENT_EVENT_RULE: Prefer record_town_event first when anything notable happened; keep command use sparse and skip commands when nothing meaningful occurred."
            : string.Empty;
        var ambientFamiliarityRule = string.Equals(effectiveContextTag, "npc_to_npc_ambient", StringComparison.OrdinalIgnoreCase) && heartLevel <= 2
            ? "AMBIENT_TONE_RULE: Low-heart references to the player must stay neutral and guarded; avoid affectionate, over-familiar, or intimate framing."
            : string.Empty;
        var npcAgeRule = BuildNpcAgePromptRule(npcName);
        var manualIntentRule = contextTag switch
        {
            "manual_relationship" => "MANUAL_INTENT_RULE: Player explicitly asked a relationship check. If trust is low or context is poor, reject or defer in-character with a brief reason.",
            "manual_interest" => "MANUAL_INTENT_RULE: Player explicitly asked about town groups. If evidence is weak, defer or decline in-character rather than forcing a shift.",
            "manual_market" => "MANUAL_INTENT_RULE: Player explicitly asked market pulse. If no anomaly exists, decline market modifiers and explain briefly in-character.",
            _ => string.Empty
        };
        if (!string.IsNullOrWhiteSpace(npcName))
        {
            if (_npcMemoryService is not null)
                npcMemory = _npcMemoryService.BuildMemoryBlock(_state, npcName, playerText ?? string.Empty, _state.Calendar.Day);
            if (_townMemoryService is not null)
                townMemory = _townMemoryService.BuildTownMemoryBlock(_state, npcName, playerText ?? string.Empty, _state.Calendar.Day);
        }

        var basePrompt = string.Join(" ",
            "CANON_WORLD: Stardew Valley.",
            "CANON_TOWN: Pelican Town.",
            $"CANON_NPCS: [{canonNpcs}].",
            $"CONTEXT: {effectiveContextTag}.",
            "RULE: Never invent towns, regions, or citizens outside this canon list.",
            $"STYLE: Reply strictly in-character as {(string.IsNullOrWhiteSpace(npcName) ? "the addressed NPC" : npcName)}, concise, natural, no assistant-speak.",
            "STYLE: Prefer 1-3 short sentences; avoid bullet lists unless explicitly requested.",
            "STYLE: Do not mention 'canon list', 'context', or other meta-AI framing.",
            promptLanguageRule,
            "LANGUAGE_RULE: For structured command outputs, keep command names and argument keys in English; localize only string values.",
            "RULE: If unsure, say unsure in-character and ask a short follow-up.",
            vanillaDialogueFollowUpRule,
            followUpContextRule,
            commandPolicyRule,
            ambientEventFirstRule,
            ambientFamiliarityRule,
            npcAgeRule,
            speechStyleBlock,
            "RELATIONSHIP_RULE: Match familiarity to STATE: RelationshipHearts. At 0-2 hearts, keep distance, be concise, and avoid affectionate language.",
            "RELATIONSHIP_RULE: At 0-1 hearts, avoid warm enthusiasm and long monologues; answer briefly and cautiously unless the player asks follow-up details.",
            "TRUST_RULE: Use STATE: NpcReputation as reliability/trust for commitments only; do not use it to override warmth from hearts.",
            "TRUST_RULE: If hearts are high but reputation is low, remain warm in tone but cautious on promises, favors, and high-stakes requests.",
            "PLAYER_NAME_RULE: Use PLAYER_KNOWLEDGE to decide how to address the player. If NpcHasMetPlayer is false, do not call the player by name.",
            "TIME_RULE: If asked for time, answer with hour and minute plus AM/PM (for example: 6:30 AM). Never answer with minutes only.",
            "QUEST_RULE: If you offer or describe a concrete task/request/quest, include propose_quest in the same reply.",
            "QUEST_RULE: Never give text-only task offers without propose_quest.",
            "QUEST_RULE: If your request includes an exact amount, set propose_quest.count to that number and keep target as only the item/resource/NPC name.",
            "QUEST_RULE: If no suitable request exists, explicitly say none is available in-character.",
            "QUEST_RULE: Do not proactively offer a new quest during normal small talk. Offer quests only when the player asks for work/request or explicitly agrees to help.",
            "QUEST_TARGET_RULE: Valid targets include market produce, animal goods, forage items, fish, and mine resources when appropriate to template.",
            "QUEST_DIVERSITY_RULE: Avoid repeating the same crop request across many NPCs. If recent requests cluster on one target, choose another valid target or another template.",
            "QUEST_DIVERSITY_RULE: When player asks for work, prioritize PreferredTemplates and PreferredTargets from QUEST_DIVERSITY; repeat a recent target only with a clear market/event reason.",
            "EVENT_QUALITY_RULE: record_town_event requires kind, summary, location, severity(1-5), visibility(local/public), and concise tags.",
            "EVENT_QUALITY_RULE: Keep summary concrete, location specific, severity proportional, and tags short/non-duplicated.",
            "SOCIAL_RULE: Use adjust_reputation only for meaningful interaction outcomes, not routine greetings.",
            "INTEREST_RULE: Use shift_interest_influence only when the conversation clearly concerns town groups or priorities.",
            "MARKET_MOD_RULE: Use apply_market_modifier only when MARKET_SIGNALS show a clear anomaly, and keep changes bounded/temporary.",
            "DECLINE_RULE: If a request is not appropriate for personality/relationship/context, reject or defer naturally in-character with a short reason.",
            manualIntentRule,
            playerAskedForRequest
                ? "QUEST_CONTEXT: Player explicitly asked for work/request now. You must either emit propose_quest or decline clearly; no text-only task offers."
                : string.Empty,
            "NEWS_RULE: If asked about news, rumors, or recent events, answer using NEWS_CONTEXT and RECENT_EVENTS first.",
            "EVENT_TIME_RULE: Treat UPCOMING_EVENTS as future-only and use future tense. Never describe UPCOMING_EVENTS as already happened.",
            "EVENT_TIME_RULE: RECENT_EVENTS are already observed and can be described in past/present tense.",
            "RULE: For publish_article/publish_rumor commands, keep title+content within 100 characters total.",
            "MARKET_RULE: For market questions, mention at least one live signal from MARKET_SIGNALS.",
            "REWARD_RULE: Never promise arbitrary gold numbers; follow REWARD_RULES bands.",
            "REWARD_RULES: Rewards are dynamic from target value x count with urgency bands (low=modest, medium=solid, high=premium). Social visits stay in a small fixed band.",
            $"STATE: CurrentSeason {currentSeason}.",
            $"STATE: CurrentWeather {weather}.",
            $"STATE: CurrentDayOfWeek {dayOfWeek}.",
            $"STATE: CurrentTimeOfDay {timeOfDay}.",
            $"STATE: RelationshipHearts {(string.IsNullOrWhiteSpace(npcName) ? 0 : heartLevel)}.",
            $"STATE: NpcReputation {(string.IsNullOrWhiteSpace(npcName) ? 0 : npcReputation)}.",
            $"STATE: CurrentHour24 {hour24:00}.",
            $"STATE: CurrentMinute {minute:00}.",
            $"STATE: PlayerLocale {localeCode}.",
            $"PLAYER_KNOWLEDGE: PlayerName='{playerName}' NpcHasMetPlayer={npcHasMetPlayer} PreferredAddress='{preferredAddress}'.",
            $"STATE: PlayerStats Charisma={charismaStat} Social={socialStat}.",
            $"STATE: Day {_state.Calendar.Day} {currentSeason}.",
            $"STATE: EconomySentiment {_state.Social.TownSentiment.Economy}.",
            $"MARKET_SIGNALS: TopMovers [{string.Join(", ", movers)}]. Oversupply {oversupplyText}. Scarcity {scarcityText}. RecommendedAlternative {recText}.",
            questDiversityContext,
            newsContext,
            eventsContext,
            $"STATE: AvailableTownRequests {_state.Quests.Available.Count} by_template=[{availableQuestTemplateCounts}].",
            $"STATE: ActiveTownRequests {_state.Quests.Active.Count} by_template=[{activeQuestTemplateCounts}].",
            npcMemory,
            townMemory,
            vanillaDialogueContext,
            sourceDialogueContext
        );

        if (_config.EnableCustomNpcFramework
            && _config.EnableCustomNpcLoreInjection
            && _customNpcRegistry is not null)
        {
            var customLoreBlock = _customNpcRegistry.BuildLorePromptBlock(
                npcName,
                Game1.currentLocation?.Name,
                effectiveContextTag);

            if (!string.IsNullOrWhiteSpace(customLoreBlock))
            {
                basePrompt = $"{basePrompt} {customLoreBlock}".Trim();
                if (_config.LogCustomNpcPromptInjectionPreview)
                    Monitor.Log($"Injected custom NPC lore block for '{npcName ?? "(none)"}'.", LogLevel.Trace);
            }

            var referencedNpcLore = _customNpcRegistry.BuildReferencedNpcLorePromptBlock(
                playerText,
                speakingNpcName: npcName,
                maxMatches: 2);
            if (!string.IsNullOrWhiteSpace(referencedNpcLore))
            {
                var awarenessRule = "CUSTOM_NPC_AWARENESS_RULE: Referenced custom NPCs are canonical in this save. Do not claim you have never heard of them; if details are limited, answer with partial knowledge.";
                basePrompt = $"{basePrompt} {awarenessRule} {referencedNpcLore}".Trim();
                if (_config.LogCustomNpcPromptInjectionPreview)
                    Monitor.Log($"Injected referenced custom NPC lore block for context '{effectiveContextTag}'.", LogLevel.Trace);
            }
        }

        return basePrompt;
    }

    private string BuildCompactCanonNpcPromptList()
    {
        var merged = new List<string>
        {
            "Lewis", "Robin", "Pierre", "Linus", "Haley", "Alex", "Demetrius", "Wizard", "Jas", "Vincent"
        };
        var seen = new HashSet<string>(merged, StringComparer.OrdinalIgnoreCase);

        if (_config.EnableCustomNpcFramework && _customNpcRegistry is not null)
        {
            foreach (var npc in _customNpcRegistry.GetAllNpcDisplayNames())
            {
                if (seen.Add(npc))
                    merged.Add(npc);
            }
        }

        return string.Join(", ", merged);
    }

    private static string BuildNpcAgePromptRule(string? npcName)
    {
        if (!IsChildNpcName(npcName))
            return string.Empty;

        return "AGE_RULE: You are a child in Pelican Town canon. Never claim to be an adult, in your twenties, or older. If asked your age, answer as a kid without adult numeric ages.";
    }
    private static string GetPlayerDisplayNameForContext()
    {
        var raw = Game1.player?.Name ?? string.Empty;
        return string.IsNullOrWhiteSpace(raw) ? "Farmer" : raw.Trim();
    }

    private static string ResolvePlayerAddressForNpc(string? npcName)
    {
        if (!HasNpcMetPlayer(npcName))
            return "Farmer";

        return GetPlayerDisplayNameForContext();
    }

    private static bool HasNpcMetPlayer(string? npcName)
    {
        if (string.IsNullOrWhiteSpace(npcName) || Game1.player is null)
            return false;

        var normalizedNpc = npcName.Trim();
        if (normalizedNpc.Length == 0)
            return false;

        try
        {
            var escapedNpc = normalizedNpc.Replace("\"", "\\\"", StringComparison.Ordinal);
            var query = $"PLAYER_HAS_MET Current \"{escapedNpc}\"";
            if (GameStateQuery.CheckConditions(query, Game1.currentLocation, Game1.player, null, null, null, null))
                return true;
        }
        catch
        {
            // Fallback below if game-state query fails for any reason.
        }

        try
        {
            if (Game1.player.friendshipData is not null && Game1.player.friendshipData.ContainsKey(normalizedNpc))
                return true;
        }
        catch
        {
            // Ignore and continue.
        }

        return Game1.player.mailReceived.Contains("Introductions", StringComparer.OrdinalIgnoreCase);
    }

    private string BuildNewsAwarenessBlock()
    {
        var day = _state.Calendar.Day;
        var latestIssue = _state.Newspaper.Issues
            .OrderByDescending(i => i.Day)
            .FirstOrDefault();

        var headline = TrimForContext(latestIssue?.Headline, 48, "none");
        var issueDay = latestIssue?.Day ?? 0;

        var editorItems = (latestIssue?.Articles ?? new List<NewspaperArticle>())
            .Where(a => IsEditorNewsSource(a.SourceNpc))
            .Select(a => TrimForContext(a.Title, 34, "untitled"))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(2)
            .ToArray();

        var rumorItems = _state.Newspaper.Articles
            .Where(a =>
                a.Day <= day
                && a.ExpirationDay >= day
                && a.Category.Equals("social", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(a => a.Day)
            .Select(a => TrimForContext(a.Title, 34, "rumor"))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .ToArray();

        var bulletinItems = _state.Newspaper.Articles
            .Where(a =>
                a.Day <= day
                && a.ExpirationDay >= day
                && !a.Category.Equals("social", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(a => a.Day)
            .Select(a => TrimForContext(a.Title, 34, "bulletin"))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .ToArray();

        return
            $"NEWS_CONTEXT: issue_day={issueDay} headline='{headline}' " +
            $"editor=[{JoinContextItems(editorItems)}] rumors=[{JoinContextItems(rumorItems)}] " +
            $"bulletins=[{JoinContextItems(bulletinItems)}].";
    }

    private string BuildRecentEventAwarenessBlock(string? playerText = null)
    {
        var day = _state.Calendar.Day;
        var currentTimeOfDay = Game1.timeOfDay;
        var focusTokens = ExtractContextFocusTokens(playerText);
        var recent = _state.TownMemory.Events
            .Where(ev =>
                ev.Day >= day - 3
                && ev.Day <= day + 2
                && !string.IsNullOrWhiteSpace(ev.Summary))
            .ToList();

        var publicRecent = recent
            .Where(ev => string.Equals(ev.Visibility, "public", StringComparison.OrdinalIgnoreCase))
            .ToList();
        var source = publicRecent.Count > 0 ? publicRecent : recent;

        var ranked = source
            .OrderByDescending(ev =>
            {
                var visibilityBoost = string.Equals(ev.Visibility, "public", StringComparison.OrdinalIgnoreCase) ? 6 : 0;
                var relevanceBoost = CountEventFocusMatches(ev, focusTokens) * 3;
                var recencyBoost = Math.Max(0, 3 - Math.Abs(day - ev.Day));
                return (ev.Severity * 4) + visibilityBoost + relevanceBoost + recencyBoost;
            })
            .ThenByDescending(ev => ev.Day)
            .ThenByDescending(ev => ev.Severity)
            .ToList();

        var recentEvents = ranked
            .Where(ev => !TownEventTemporalHelper.IsUpcoming(ev, day, currentTimeOfDay))
            .Take(3)
            .Select(ev => $"{TownEventTemporalHelper.BuildTemporalLabel(ev, day, currentTimeOfDay)}:{ev.Kind}:{TrimForContext(ev.Summary, 44, "event")}")
            .ToArray();
        var upcomingEvents = ranked
            .Where(ev => TownEventTemporalHelper.IsUpcoming(ev, day, currentTimeOfDay))
            .Take(2)
            .Select(ev => $"{TownEventTemporalHelper.BuildTemporalLabel(ev, day, currentTimeOfDay)}:{ev.Kind}:{TrimForContext(ev.Summary, 44, "event")}")
            .ToArray();

        return $"RECENT_EVENTS: [{JoinContextItems(recentEvents)}]. UPCOMING_EVENTS: [{JoinContextItems(upcomingEvents)}].";
    }

    private string BuildRecentVanillaDialogueContextBlock(string? npcName)
    {
        if (!TryGetRecentVanillaDialogueContext(npcName, out var context))
            return string.Empty;
        if (string.IsNullOrWhiteSpace(context.LastDialogueLine) && context.DialogueSequence.Count == 0)
            return string.Empty;

        var npcDisplayName = string.IsNullOrWhiteSpace(context.NpcDisplayName)
            ? context.NpcName
            : context.NpcDisplayName;
        var safeLine = TrimForContext(context.LastDialogueLine, 130, "none").Replace("'", "’", StringComparison.Ordinal);
        var sequence = context.DialogueSequence
            .TakeLast(4)
            .Select(line => TrimForContext(line, 96, "none").Replace("'", "’", StringComparison.Ordinal))
            .ToArray();
        return $"VANILLA_DIALOGUE_CONTEXT[{npcDisplayName}]: day={context.Day} time={context.TimeOfDay:0000} last_line='{safeLine}' sequence=[{JoinContextItems(sequence)}].";
    }

    private bool TryGetRecentVanillaDialogueContext(string? npcName, out RecentVanillaDialogueContext context)
    {
        context = null!;
        if (string.IsNullOrWhiteSpace(npcName) || _recentVanillaDialogueByNpcToken.Count == 0)
            return false;

        PruneStaleVanillaDialogueContext();

        var lookupTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddLookupToken(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return;

            var token = NormalizeTargetToken(raw);
            if (!string.IsNullOrWhiteSpace(token))
                lookupTokens.Add(token);
        }

        AddLookupToken(npcName);
        var resolvedNpc = ResolveNpcByName(npcName);
        AddLookupToken(resolvedNpc?.Name);
        AddLookupToken(resolvedNpc?.displayName);

        foreach (var token in lookupTokens)
        {
            if (_recentVanillaDialogueByNpcToken.TryGetValue(token, out var found))
            {
                context = found;
                return true;
            }
        }

        return false;
    }

    private void PruneStaleVanillaDialogueContext()
    {
        if (_recentVanillaDialogueByNpcToken.Count == 0)
            return;

        var nowUtc = DateTime.UtcNow;
        var staleKeys = _recentVanillaDialogueByNpcToken
            .Where(kv =>
                nowUtc - kv.Value.CapturedUtc > VanillaDialogueContextMaxAge
                || kv.Value.Day < _state.Calendar.Day - 1)
            .Select(kv => kv.Key)
            .ToArray();

        foreach (var key in staleKeys)
            _recentVanillaDialogueByNpcToken.Remove(key);
    }

    private void BeginVanillaDialogueCaptureSession(NPC npc)
    {
        if (!Context.IsWorldReady || npc is null || string.IsNullOrWhiteSpace(npc.Name))
            return;

        var npcToken = NormalizeTargetToken(npc.Name);
        if (string.IsNullOrWhiteSpace(npcToken))
            return;

        var npcDisplayName = string.IsNullOrWhiteSpace(npc.displayName)
            ? npc.Name
            : npc.displayName;
        var session = new RecentVanillaDialogueContext
        {
            NpcName = npc.Name,
            NpcDisplayName = npcDisplayName,
            Day = _state.Calendar.Day,
            TimeOfDay = Game1.timeOfDay,
            CapturedUtc = DateTime.UtcNow
        };

        _recentVanillaDialogueByNpcToken[npcToken] = session;
        var displayToken = NormalizeTargetToken(npcDisplayName);
        if (!string.IsNullOrWhiteSpace(displayToken))
            _recentVanillaDialogueByNpcToken[displayToken] = session;
    }

    private void TryCaptureVanillaDialogueContextFromMenu(IClickableMenu? menu, string? fallbackNpcName)
    {
        if (!_npcDialogueHookArmed && string.IsNullOrWhiteSpace(fallbackNpcName))
            return;
        if (!Context.IsWorldReady || menu is not DialogueBox dialogueBox)
            return;

        if (!TryExtractDialogueLineFromMenu(dialogueBox, out var rawDialogueLine))
            return;

        var normalizedLine = NormalizeLiveDialogueLineForContext(rawDialogueLine);
        if (normalizedLine.Length < 8)
            return;

        var speaker = ResolveDialogueSpeakerForCapturedContext(dialogueBox, fallbackNpcName);
        if (speaker is null || string.IsNullOrWhiteSpace(speaker.Name) || !IsRosterNpc(speaker))
            return;

        AppendVanillaDialogueContextLine(speaker, normalizedLine);
    }

    private void AppendVanillaDialogueContextLine(NPC speaker, string normalizedLine)
    {
        if (speaker is null || string.IsNullOrWhiteSpace(speaker.Name) || string.IsNullOrWhiteSpace(normalizedLine))
            return;

        var speakerToken = NormalizeTargetToken(speaker.Name);
        if (string.IsNullOrWhiteSpace(speakerToken))
            return;
        var speakerDisplayName = string.IsNullOrWhiteSpace(speaker.displayName)
            ? speaker.Name
            : speaker.displayName;

        if (!_recentVanillaDialogueByNpcToken.TryGetValue(speakerToken, out var context))
        {
            context = new RecentVanillaDialogueContext();
            _recentVanillaDialogueByNpcToken[speakerToken] = context;
        }

        context.NpcName = speaker.Name;
        context.NpcDisplayName = speakerDisplayName;
        context.Day = _state.Calendar.Day;
        context.TimeOfDay = Game1.timeOfDay;
        context.CapturedUtc = DateTime.UtcNow;

        var effectiveLastLine = normalizedLine;
        if (context.DialogueSequence.Count == 0)
        {
            context.DialogueSequence.Add(normalizedLine);
        }
        else
        {
            var lastIndex = context.DialogueSequence.Count - 1;
            var lastLine = context.DialogueSequence[lastIndex];
            if (string.Equals(lastLine, normalizedLine, StringComparison.OrdinalIgnoreCase))
            {
                effectiveLastLine = normalizedLine;
            }
            else if (normalizedLine.StartsWith(lastLine, StringComparison.OrdinalIgnoreCase))
            {
                // Dialogue text often reveals progressively; keep one evolving line instead of appending fragments.
                context.DialogueSequence[lastIndex] = normalizedLine;
                effectiveLastLine = normalizedLine;
            }
            else if (lastLine.StartsWith(normalizedLine, StringComparison.OrdinalIgnoreCase))
            {
                effectiveLastLine = lastLine;
            }
            else
            {
                context.DialogueSequence.Add(normalizedLine);
                effectiveLastLine = normalizedLine;
            }
        }

        while (context.DialogueSequence.Count > VanillaDialogueContextSequenceMaxLines)
            context.DialogueSequence.RemoveAt(0);

        context.LastDialogueLine = effectiveLastLine;

        _recentVanillaDialogueByNpcToken[speakerToken] = context;
        var displayToken = NormalizeTargetToken(speakerDisplayName);
        if (!string.IsNullOrWhiteSpace(displayToken))
            _recentVanillaDialogueByNpcToken[displayToken] = context;
    }

    private NPC? ResolveDialogueSpeakerForCapturedContext(IClickableMenu sourceMenu, string? fallbackNpcName)
    {
        var menuNpc = TryResolveNpcFromOpenedMenu(sourceMenu);
        if (menuNpc is not null && !string.IsNullOrWhiteSpace(menuNpc.Name))
            return menuNpc;

        if (!string.IsNullOrWhiteSpace(fallbackNpcName))
        {
            var fallbackNpc = ResolveNpcByName(fallbackNpcName);
            if (fallbackNpc is not null)
                return fallbackNpc;
        }

        if (_pendingNpcDialogueHookNpc is not null && !string.IsNullOrWhiteSpace(_pendingNpcDialogueHookNpc.Name))
            return _pendingNpcDialogueHookNpc;

        if (Game1.currentSpeaker is not null && !string.IsNullOrWhiteSpace(Game1.currentSpeaker.Name))
            return Game1.currentSpeaker;

        return null;
    }

    private bool TryExtractDialogueLineFromMenu(IClickableMenu menu, out string dialogueLine)
    {
        dialogueLine = string.Empty;
        if (menu is null)
            return false;

        if (TryExtractDialogueLineFromMembers(
            menu,
            out dialogueLine,
            "getCurrentString",
            "GetCurrentString",
            "currentString",
            "CurrentString",
            "displayText",
            "dialogueText",
            "message",
            "Message"))
        {
            return true;
        }

        if (TryExtractDialogueLineFromMembers(
            menu,
            out dialogueLine,
            "characterDialogue",
            "CharacterDialogue",
            "currentDialogue",
            "CurrentDialogue",
            "dialogue",
            "Dialogue",
            "dialogues",
            "Dialogues"))
        {
            return true;
        }

        if (Game1.currentSpeaker is not null
            && TryExtractDialogueLineFromMembers(
                Game1.currentSpeaker,
                out dialogueLine,
                "CurrentDialogue",
                "currentDialogue",
                "getCurrentDialogue",
                "GetCurrentDialogue"))
        {
            return true;
        }

        return false;
    }

    private static bool TryExtractDialogueLineFromMembers(object source, out string dialogueLine, params string[] memberNames)
    {
        dialogueLine = string.Empty;

        foreach (var memberName in memberNames)
        {
            if (TryGetParameterlessMethodValue(source, memberName, out var methodValue)
                && TryExtractDialogueTextFromValue(methodValue, out dialogueLine))
            {
                return true;
            }

            if (TryGetDialogueMemberValue(source, memberName, out var memberValue)
                && TryExtractDialogueTextFromValue(memberValue, out dialogueLine))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryGetParameterlessMethodValue(object source, string methodName, out object? value)
    {
        value = null;
        if (source is null || string.IsNullOrWhiteSpace(methodName))
            return false;

        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        try
        {
            var method = source.GetType().GetMethod(methodName, Flags, binder: null, types: Type.EmptyTypes, modifiers: null);
            if (method is null)
                return false;

            value = method.Invoke(source, null);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryGetDialogueMemberValue(object source, string memberName, out object? value)
    {
        value = null;
        if (source is null || string.IsNullOrWhiteSpace(memberName))
            return false;

        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var type = source.GetType();

        try
        {
            var property = type.GetProperty(memberName, Flags);
            if (property is not null && property.GetIndexParameters().Length == 0)
            {
                value = property.GetValue(source);
                return true;
            }
        }
        catch
        {
        }

        try
        {
            var field = type.GetField(memberName, Flags);
            if (field is not null)
            {
                value = field.GetValue(source);
                return true;
            }
        }
        catch
        {
        }

        return false;
    }

    private static bool TryExtractDialogueTextFromValue(object? value, out string dialogueLine, int depth = 0)
    {
        dialogueLine = string.Empty;
        if (value is null || depth > 3)
            return false;

        if (value is string str)
        {
            dialogueLine = str;
            return !string.IsNullOrWhiteSpace(dialogueLine);
        }

        if (value is IEnumerable<string> stringItems)
        {
            foreach (var item in stringItems)
            {
                if (TryExtractDialogueTextFromValue(item, out dialogueLine, depth + 1))
                    return true;
            }
        }

        if (value is System.Collections.IEnumerable enumerable && value is not string)
        {
            foreach (var item in enumerable)
            {
                if (TryExtractDialogueTextFromValue(item, out dialogueLine, depth + 1))
                    return true;
            }
        }

        var methodCandidates = new[]
        {
            "getCurrentDialogue",
            "GetCurrentDialogue",
            "getCurrentString",
            "GetCurrentString",
            "Peek"
        };
        foreach (var methodName in methodCandidates)
        {
            if (TryGetParameterlessMethodValue(value, methodName, out var methodValue)
                && methodValue is not null
                && !ReferenceEquals(methodValue, value)
                && TryExtractDialogueTextFromValue(methodValue, out dialogueLine, depth + 1))
            {
                return true;
            }
        }

        var memberCandidates = new[]
        {
            "CurrentDialogue",
            "currentDialogue",
            "Dialogue",
            "dialogue",
            "Text",
            "text",
            "Message",
            "message",
            "currentString",
            "CurrentString"
        };
        foreach (var memberName in memberCandidates)
        {
            if (TryGetDialogueMemberValue(value, memberName, out var memberValue)
                && memberValue is not null
                && !ReferenceEquals(memberValue, value)
                && TryExtractDialogueTextFromValue(memberValue, out dialogueLine, depth + 1))
            {
                return true;
            }
        }

        return false;
    }

    private string NormalizeLiveDialogueLineForContext(string rawDialogueLine)
    {
        if (string.IsNullOrWhiteSpace(rawDialogueLine))
            return string.Empty;

        var cleaned = rawDialogueLine
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);
        var firstSegment = cleaned
            .Split(new[] { '#', '^' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(firstSegment))
            cleaned = firstSegment;

        cleaned = cleaned.Replace("@", GetPlayerDisplayNameForContext(), StringComparison.Ordinal);
        cleaned = Regex.Replace(cleaned, @"\[[^\]]+\]", " ", RegexOptions.CultureInvariant);
        cleaned = Regex.Replace(cleaned, @"\$[a-z0-9_]+", " ", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        cleaned = Regex.Replace(cleaned, @"%[a-z0-9_]+", " ", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        cleaned = Regex.Replace(cleaned, @"\s+", " ", RegexOptions.CultureInvariant).Trim();

        return cleaned;
    }

    private string BuildSourceModDialogueContextBlock(string? speakerNpcName, string? playerText)
    {
        if (!Context.IsWorldReady || string.IsNullOrWhiteSpace(speakerNpcName))
            return string.Empty;

        var dialogueLines = LoadSourceDialogueLinesForNpc(speakerNpcName);
        if (dialogueLines.Count == 0)
            return string.Empty;

        var focusCustomNpcNames = FindCustomNpcNamesInText(playerText, maxMatches: 2);
        var matchedLines = new List<string>();

        if (focusCustomNpcNames.Count > 0)
        {
            foreach (var line in dialogueLines)
            {
                if (focusCustomNpcNames.Any(name => ContainsTargetToken(line, name)))
                    matchedLines.Add(line);

                if (matchedLines.Count >= 2)
                    break;
            }
        }

        if (matchedLines.Count == 0 && IsCustomNpcNameOrAlias(speakerNpcName))
            matchedLines.AddRange(dialogueLines.Take(2));

        if (matchedLines.Count == 0)
            return string.Empty;

        var snippets = matchedLines
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(2)
            .Select(line => TrimForContext(line, 100, "line"))
            .ToArray();
        if (snippets.Length == 0)
            return string.Empty;

        var mentions = focusCustomNpcNames.Count == 0
            ? "none"
            : string.Join(", ", focusCustomNpcNames.Take(2));

        return $"SOURCE_MOD_DIALOGUE[{speakerNpcName.Trim()}]: mentions=[{mentions}] lines=[{JoinContextItems(snippets)}].";
    }

    private List<string> LoadSourceDialogueLinesForNpc(string speakerNpcName)
    {
        var candidates = BuildDialogueAssetCandidates(speakerNpcName);
        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate))
                continue;

            try
            {
                var assetName = $"Characters/Dialogue/{candidate}";
                var data = Game1.content.Load<Dictionary<string, string>>(assetName);
                if (data is null || data.Count == 0)
                    continue;

                var lines = data.Values
                    .SelectMany(ExtractDialogueSnippets)
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(40)
                    .ToList();
                if (lines.Count > 0)
                    return lines;
            }
            catch
            {
                // Ignore missing/invalid dialogue assets and continue with next candidate.
            }
        }

        return new List<string>();
    }

    private IEnumerable<string> BuildDialogueAssetCandidates(string speakerNpcName)
    {
        var ordered = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddCandidate(string? raw)
        {
            var value = (raw ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(value))
                return;
            if (seen.Add(value))
                ordered.Add(value);
        }

        AddCandidate(speakerNpcName);

        try
        {
            var worldNpc = Game1.getCharacterFromName(speakerNpcName);
            AddCandidate(worldNpc?.Name);
            AddCandidate(worldNpc?.displayName);
        }
        catch
        {
            // Ignore world lookup failures.
        }

        if (_customNpcRegistry is not null && _customNpcRegistry.TryGetNpcByName(speakerNpcName, out var customNpc))
        {
            AddCandidate(customNpc.NpcId);
            AddCandidate(customNpc.DisplayName);
            foreach (var alias in customNpc.Aliases)
                AddCandidate(alias);
        }

        return ordered;
    }

    private List<string> FindCustomNpcNamesInText(string? text, int maxMatches)
    {
        var found = new List<string>();
        if (string.IsNullOrWhiteSpace(text) || _customNpcRegistry is null || _customNpcRegistry.NpcsByToken.Count == 0)
            return found;

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var npc in _customNpcRegistry.NpcsByToken.Values)
        {
            var mentioned =
                ContainsTargetToken(text, npc.DisplayName)
                || ContainsTargetToken(text, npc.NpcId)
                || npc.Aliases.Any(alias => ContainsTargetToken(text, alias));
            if (!mentioned)
                continue;

            var canonicalName = string.IsNullOrWhiteSpace(npc.DisplayName)
                ? npc.NpcId
                : npc.DisplayName;
            if (string.IsNullOrWhiteSpace(canonicalName) || !seen.Add(canonicalName))
                continue;

            found.Add(canonicalName);
            if (found.Count >= Math.Max(1, maxMatches))
                break;
        }

        return found;
    }

    private static IEnumerable<string> ExtractDialogueSnippets(string? rawDialogue)
    {
        if (string.IsNullOrWhiteSpace(rawDialogue))
            yield break;

        var flattened = rawDialogue
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);
        var segments = flattened.Split(new[] { '#', '^' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var segment in segments)
        {
            var cleaned = segment;
            cleaned = Regex.Replace(cleaned, @"\[[^\]]+\]", " ", RegexOptions.CultureInvariant);
            cleaned = Regex.Replace(cleaned, @"\$[a-z0-9_]+", " ", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
            cleaned = Regex.Replace(cleaned, @"%[a-z0-9_]+", " ", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
            cleaned = cleaned.Replace("@", "Farmer", StringComparison.Ordinal);
            cleaned = Regex.Replace(cleaned, @"\s+", " ", RegexOptions.CultureInvariant).Trim();
            if (cleaned.Length < 12)
                continue;

            yield return cleaned;
        }
    }

    private static string FormatQuestTemplateCounts(IEnumerable<QuestEntry> quests)
    {
        var counts = quests
            .Where(q => q is not null)
            .GroupBy(
                q => string.IsNullOrWhiteSpace(q.TemplateId) ? "unknown" : q.TemplateId.Trim().ToLowerInvariant(),
                StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => $"{g.Key}:{g.Count()}")
            .ToArray();

        return counts.Length == 0 ? "none" : string.Join(", ", counts);
    }

    private string BuildQuestDiversityBlock(string? npcName, bool playerAskedForRequest)
    {
        if (!playerAskedForRequest)
            return "QUEST_DIVERSITY: not_applicable.";

        static bool IsItemOrResourceTemplate(string? templateId)
        {
            return string.Equals(templateId, "gather_crop", StringComparison.OrdinalIgnoreCase)
                || string.Equals(templateId, "deliver_item", StringComparison.OrdinalIgnoreCase)
                || string.Equals(templateId, "mine_resource", StringComparison.OrdinalIgnoreCase);
        }

        var recentTargets = _state.Quests.Available
            .Concat(_state.Quests.Active)
            .Concat(_state.Quests.Completed.TakeLast(6))
            .Concat(_state.Quests.Failed.TakeLast(6))
            .Where(q => IsItemOrResourceTemplate(q.TemplateId))
            .Select(q => NormalizeTargetToken(q.TargetItem))
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToArray();

        var repeatedTargets = recentTargets
            .GroupBy(t => t, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() >= 2)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .Select(g => g.Key)
            .ToArray();

        var preferredTemplates = SelectQuestDiversityTemplates(npcName);
        var preferredTargets = SelectQuestDiversityTargets(npcName, repeatedTargets);

        return
            $"QUEST_DIVERSITY: AvoidRepeatTargets [{JoinContextItems(repeatedTargets)}]. " +
            $"PreferredTemplates [{JoinContextItems(preferredTemplates)}]. " +
            $"PreferredTargets [{JoinContextItems(preferredTargets)}].";
    }

    private string[] SelectQuestDiversityTemplates(string? npcName)
    {
        var profile = _npcSpeechStyleService?.GetProfile(npcName) ?? NpcVerbalProfile.Traditionalist;
        var baseOrder = profile switch
        {
            NpcVerbalProfile.Professional => new[] { "deliver_item", "gather_crop", "mine_resource", "social_visit" },
            NpcVerbalProfile.Traditionalist => new[] { "social_visit", "gather_crop", "deliver_item", "mine_resource" },
            NpcVerbalProfile.Intellectual => new[] { "mine_resource", "deliver_item", "social_visit", "gather_crop" },
            NpcVerbalProfile.Enthusiast => new[] { "social_visit", "gather_crop", "mine_resource", "deliver_item" },
            NpcVerbalProfile.Recluse => new[] { "mine_resource", "deliver_item", "gather_crop", "social_visit" },
            _ => new[] { "gather_crop", "deliver_item", "social_visit", "mine_resource" }
        };

        var offset = GetQuestDiversitySeedIndex(baseOrder.Length, npcName, "template");
        return baseOrder
            .Skip(offset)
            .Concat(baseOrder.Take(offset))
            .Take(2)
            .ToArray();
    }

    private string[] SelectQuestDiversityTargets(string? npcName, IEnumerable<string> avoidTargets)
    {
        var ranked = BuildQuestSupplyCandidateList();

        if (ranked.Count == 0)
        {
            ranked = new List<string>
            {
                "parsnip", "potato", "cauliflower", "blueberry", "melon", "pumpkin", "corn", "tomato",
                "milk", "egg", "wild_horseradish", "sunfish", "copper_ore"
            };
        }

        var avoid = new HashSet<string>(
            (avoidTargets ?? Array.Empty<string>()).Where(t => !string.IsNullOrWhiteSpace(t)),
            StringComparer.OrdinalIgnoreCase);
        var filtered = ranked.Where(t => !avoid.Contains(t)).ToList();
        if (filtered.Count == 0)
            filtered = ranked;

        var offset = GetQuestDiversitySeedIndex(filtered.Count, npcName, "target");
        return filtered
            .Skip(offset)
            .Concat(filtered.Take(offset))
            .Take(Math.Min(3, filtered.Count))
            .ToArray();
    }

    private List<string> BuildQuestSupplyCandidateList()
    {
        var ranked = _state.Economy.Crops
            .Where(kv => !string.IsNullOrWhiteSpace(kv.Key))
            .OrderByDescending(kv => kv.Value.ScarcityBonus)
            .ThenByDescending(kv => kv.Value.DemandFactor)
            .ThenByDescending(kv => Math.Abs(kv.Value.PriceToday - kv.Value.PriceYesterday))
            .Select(kv => NormalizeTargetToken(kv.Key))
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .ToList();

        ranked.AddRange(VanillaCropCatalog.GetEntries().Keys
            .Select(NormalizeTargetToken)
            .Where(k => !string.IsNullOrWhiteSpace(k)));

        ranked.AddRange(GetSeasonalNpcSupplyPool((_state.Calendar.Season ?? "spring").Trim().ToLowerInvariant())
            .Select(NormalizeTargetToken));
        ranked.AddRange(OrchardNpcSupplyPool.Select(NormalizeTargetToken));
        ranked.AddRange(ForageNpcSupplyPool.Select(NormalizeTargetToken));
        ranked.AddRange(FishingNpcSupplyPool.Select(NormalizeTargetToken));
        ranked.AddRange(MiningNpcResourcePool.Select(NormalizeTargetToken));

        return ranked
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private int GetQuestDiversitySeedIndex(int count, string? npcName, string axis)
    {
        if (count <= 1)
            return 0;

        var seed = $"{npcName ?? "npc"}|{axis}|{_state.Calendar.Day}|{_state.Calendar.Season}";
        var hash = 17;
        foreach (var ch in seed)
            hash = unchecked((hash * 31) + char.ToLowerInvariant(ch));

        return Math.Abs(hash) % count;
    }

    private static HashSet<string> ExtractContextFocusTokens(string? playerText)
    {
        if (string.IsNullOrWhiteSpace(playerText))
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        return playerText
            .ToLowerInvariant()
            .Split(new[] { ' ', ',', '.', '!', '?', ':', ';', '\'', '"', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(token => token.Length >= 4)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(12)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static int CountEventFocusMatches(TownMemoryEvent ev, HashSet<string> focusTokens)
    {
        if (focusTokens.Count == 0)
            return 0;

        var count = 0;
        foreach (var token in focusTokens)
        {
            if (ev.Summary.Contains(token, StringComparison.OrdinalIgnoreCase)
                || ev.Kind.Contains(token, StringComparison.OrdinalIgnoreCase)
                || ev.Tags.Any(tag => tag.Contains(token, StringComparison.OrdinalIgnoreCase)))
            {
                count += 1;
            }
        }

        return count;
    }

    private static bool IsEditorNewsSource(string? sourceNpc)
    {
        var source = (sourceNpc ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(source))
            return false;

        return source.Equals("Pelican Times Editor", StringComparison.OrdinalIgnoreCase)
            || source.Equals("Editor", StringComparison.OrdinalIgnoreCase)
            || source.Contains("Editor", StringComparison.OrdinalIgnoreCase)
            || source.Equals("Town Reporter", StringComparison.OrdinalIgnoreCase)
            || source.Equals("Town Report", StringComparison.OrdinalIgnoreCase);
    }

    private static string JoinContextItems(IEnumerable<string> values)
    {
        var items = values
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v.Trim())
            .ToArray();

        if (items.Length == 0)
            return "none";

        return string.Join(" | ", items);
    }

    private static string TrimForContext(string? raw, int maxLength, string fallback)
    {
        var value = (raw ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        if (value.Length <= maxLength)
            return value;

        return value[..Math.Max(1, maxLength - 3)] + "...";
    }

    private static string GetCurrentWeatherLabel()
    {
        if (Game1.isLightning)
            return "storm";
        if (Game1.isRaining)
            return "rain";
        if (Game1.isSnowing)
            return "snow";
        return "clear";
    }

    private static string GetCurrentSeasonLabel()
    {
        var season = (Game1.currentSeason ?? string.Empty).Trim().ToLowerInvariant();
        return string.IsNullOrWhiteSpace(season) ? "spring" : season;
    }

    private static int GetCurrentSeasonIndex()
    {
        return GetCurrentSeasonLabel() switch
        {
            "spring" => 0,
            "summer" => 1,
            "fall" => 2,
            "winter" => 3,
            _ => 0
        };
    }

    private static int GetCurrentWorldAbsoluteDay()
    {
        var year = Math.Max(1, Game1.year);
        var seasonIndex = GetCurrentSeasonIndex();
        var dayOfMonth = Math.Clamp(Game1.dayOfMonth, 1, 28);
        return ((year - 1) * 112) + (seasonIndex * 28) + dayOfMonth;
    }

    private void SyncCalendarSeasonFromWorld()
    {
        if (!Context.IsWorldReady)
            return;

        var worldSeason = GetCurrentSeasonLabel();
        var worldYear = Math.Max(1, Game1.year);
        var worldAbsoluteDay = GetCurrentWorldAbsoluteDay();

        _state.Calendar.Season = worldSeason;
        _state.Calendar.Year = worldYear;

        if (TryReadFactSourceInt(CalendarLastWorldAbsoluteDayFactKey, out var previousWorldAbsoluteDay))
        {
            var delta = worldAbsoluteDay - previousWorldAbsoluteDay;
            if (delta != 0)
                _state.Calendar.Day = Math.Max(1, _state.Calendar.Day + delta);
        }
        else if (_state.Calendar.Day <= 0 || Math.Abs(worldAbsoluteDay - _state.Calendar.Day) > 1)
        {
            _state.Calendar.Day = worldAbsoluteDay;
        }

        WriteFactSourceInt(CalendarLastWorldAbsoluteDayFactKey, worldAbsoluteDay);
    }

    private bool TryReadFactSourceInt(string key, out int value)
    {
        value = 0;
        if (!_state.Facts.Facts.TryGetValue(key, out var fact))
            return false;

        return int.TryParse(fact.Source ?? string.Empty, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    private void WriteFactSourceInt(string key, int value)
    {
        _state.Facts.Facts[key] = new FactValue
        {
            Value = true,
            SetDay = _state.Calendar.Day,
            Source = value.ToString(CultureInfo.InvariantCulture)
        };
    }

    private static string GetCurrentDayOfWeekLabel()
    {
        var day = Math.Clamp(Game1.dayOfMonth, 1, 28);
        var dayNames = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
        return dayNames[(day - 1) % dayNames.Length];
    }

    private static string GetCurrentTimeOfDayLabel(out int hour24, out int minute)
    {
        var hhmm = Math.Clamp(Game1.timeOfDay, 0, 2600);
        hour24 = hhmm / 100;
        minute = hhmm % 100;
        if (minute > 59)
            minute = 59;

        var amPm = hour24 >= 12 ? "PM" : "AM";
        var hour12 = hour24 % 12;
        if (hour12 == 0)
            hour12 = 12;

        var period = hhmm switch
        {
            < 1200 => "morning",
            < 1700 => "afternoon",
            < 2200 => "evening",
            _ => "night"
        };

        return $"{hour12}:{minute:00} {amPm} ({period})";
    }

    private static int GetNpcHeartLevel(string? npcName)
    {
        if (string.IsNullOrWhiteSpace(npcName) || Game1.player is null)
            return 0;

        try
        {
            return Math.Max(0, Game1.player.getFriendshipHeartLevelForNPC(npcName));
        }
        catch
        {
            return 0;
        }
    }

    private int GetNpcReputation(string? npcName)
    {
        if (string.IsNullOrWhiteSpace(npcName))
            return 0;

        if (_state.Social.NpcReputation.TryGetValue(npcName, out var direct))
            return Math.Clamp(direct, -100, 100);

        var normalized = npcName.Trim().ToLowerInvariant();
        if (_state.Social.NpcReputation.TryGetValue(normalized, out var normalizedValue))
            return Math.Clamp(normalizedValue, -100, 100);

        return 0;
    }

    private static int GetPlayerRpgStat(string statKey)
    {
        if (Game1.player is null || string.IsNullOrWhiteSpace(statKey))
            return 0;

        var keys = new[]
        {
            $"slrpg.stat.{statKey}",
            $"stardewlivingrpg.stat.{statKey}",
            statKey
        };

        foreach (var key in keys)
        {
            if (!Game1.player.modData.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw))
                continue;

            if (int.TryParse(raw, out var parsed))
                return Math.Max(0, parsed);
        }

        return 0;
    }

    private static bool IsPlayerAskingForQuest(string? playerText)
    {
        if (string.IsNullOrWhiteSpace(playerText))
            return false;

        var text = playerText.ToLowerInvariant();
        return text.Contains("quest", StringComparison.Ordinal)
            || text.Contains("task", StringComparison.Ordinal)
            || text.Contains("request", StringComparison.Ordinal)
            || text.Contains("job", StringComparison.Ordinal)
            || text.Contains("work", StringComparison.Ordinal)
            || text.Contains("posting", StringComparison.Ordinal)
            || text.Contains("errand", StringComparison.Ordinal)
            || text.Contains("help", StringComparison.Ordinal)
            || text.Contains("mission", StringComparison.Ordinal)
            || text.Contains("favor", StringComparison.Ordinal);
    }

    private static bool TryInferManualAskContextTag(string? playerText, out string contextTag)
    {
        contextTag = string.Empty;
        if (string.IsNullOrWhiteSpace(playerText))
            return false;

        var text = playerText.Trim().ToLowerInvariant();

        if (ContainsAny(
                text,
                "market pulse",
                "market outlook",
                "market today",
                "market right now",
                "what's the market",
                "whats the market",
                "price today",
                "prices today",
                "oversupply",
                "scarcity",
                "supply and demand"))
        {
            contextTag = "manual_market";
            return true;
        }

        if (ContainsAny(
                text,
                "town groups",
                "groups lately",
                "which group",
                "who has influence",
                "influence in town",
                "town priorities",
                "farmers circle",
                "shopkeepers guild",
                "adventurers club",
                "nature keepers"))
        {
            contextTag = "manual_interest";
            return true;
        }

        if (ContainsAny(
                text,
                "how are we doing",
                "where do we stand",
                "our relationship",
                "do you trust me",
                "what do you think of me",
                "how are you doing",
                "how are you feeling about me"))
        {
            contextTag = "manual_relationship";
            return true;
        }

        return false;
    }

    private bool TryApplyNpcCommandFromLine(string line)
    {
        try
        {
            if (_intentResolver is null)
                return false;

            string? sourceNpcId = null;
            try
            {
                using var lineDoc = JsonDocument.Parse(line);
                if (lineDoc.RootElement.TryGetProperty("npc_id", out var npcEl) && npcEl.ValueKind == JsonValueKind.String)
                    sourceNpcId = npcEl.GetString();
            }
            catch
            {
                // ignore npc_id extraction failure; resolver still handles command parsing.
            }

            var resolverNpcId = sourceNpcId;
            if (!string.IsNullOrWhiteSpace(sourceNpcId)
                && _player2NpcShortNameById.TryGetValue(sourceNpcId, out var mappedSourceShortName)
                && !string.IsNullOrWhiteSpace(mappedSourceShortName))
            {
                resolverNpcId = mappedSourceShortName;
            }

            var attemptedCommand = TryExtractCommandNameFromLine(line);
            var extractedIntentId = TryExtractIntentIdFromLine(line);
            var isAmbientContext = IsAmbientContext(sourceNpcId);
            if (!string.IsNullOrWhiteSpace(attemptedCommand))
            {
                var contextTag = ResolveContextTagForPolicy(sourceNpcId);
                var policyRejectCode = string.Empty;
                var policyRejectReason = string.Empty;

                if (_commandPolicyService is not null)
                {
                    var policyDecision = _commandPolicyService.Evaluate(contextTag, attemptedCommand);
                    contextTag = policyDecision.ContextTag;
                    if (!policyDecision.Allowed)
                    {
                        policyRejectCode = policyDecision.ReasonCode;
                        policyRejectReason = $"command '{attemptedCommand}' denied for context '{contextTag}'";
                    }
                }

                if (string.IsNullOrWhiteSpace(policyRejectCode)
                    && isAmbientContext
                    && attemptedCommand.Equals("record_town_event", StringComparison.OrdinalIgnoreCase))
                {
                    var ambientTownEventCap = Math.Max(0, _config.AmbientRecordTownEventDailyCap);
                    if (ambientTownEventCap > 0)
                    {
                        var eventCountToday = CountAmbientTownEventsForSourceToday(resolverNpcId);
                        if (eventCountToday >= ambientTownEventCap)
                        {
                            policyRejectCode = "E_POLICY_AMBIENT_EVENT_CAP";
                            var sourceLabel = string.IsNullOrWhiteSpace(resolverNpcId) ? "unknown" : resolverNpcId;
                            policyRejectReason = $"ambient record_town_event daily cap reached for '{sourceLabel}' ({ambientTownEventCap})";
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(policyRejectCode)
                    && isAmbientContext
                    && attemptedCommand.Equals("publish_rumor", StringComparison.OrdinalIgnoreCase))
                {
                    var confidence = TryExtractNumericArgumentFromLine(line, "confidence");
                    if (!confidence.HasValue || confidence.Value < AmbientPublishRumorMinConfidence)
                    {
                        policyRejectCode = "E_POLICY_PUBLISH_CONFIDENCE_LOW";
                        policyRejectReason = $"ambient publish_rumor requires confidence >= {AmbientPublishRumorMinConfidence:F2}";
                    }
                    else if (!HasRecentVisibleTownEventForPublish(minSeverity: 1, requirePublicVisibility: false))
                    {
                        policyRejectCode = "E_POLICY_PUBLISH_VISIBILITY_LOW";
                        policyRejectReason = "ambient publish_rumor requires a recent visible town event signal";
                    }
                }

                if (string.IsNullOrWhiteSpace(policyRejectCode)
                    && isAmbientContext
                    && attemptedCommand.Equals("publish_article", StringComparison.OrdinalIgnoreCase)
                    && !HasRecentVisibleTownEventForPublish(minSeverity: 2, requirePublicVisibility: true))
                {
                    policyRejectCode = "E_POLICY_PUBLISH_VISIBILITY_LOW";
                    policyRejectReason = "ambient publish_article requires a recent public town event with severity >= 2";
                }

                if (!string.IsNullOrWhiteSpace(policyRejectCode))
                {
                    var policyLane = ResolveIntentLane(extractedIntentId, sourceNpcId);
                    _state.Telemetry.Daily.NpcIntentsRejected += 1;
                    if (policyLane == "auto")
                        _state.Telemetry.Daily.NpcIntentsAutoRejected += 1;
                    else if (policyLane == "manual")
                        _state.Telemetry.Daily.NpcIntentsManualRejected += 1;

                    if (isAmbientContext)
                        IncrementCounter(_state.Telemetry.Daily.AmbientCommandRejectedByType, attemptedCommand);
                    IncrementCounter(_state.Telemetry.Daily.NpcPolicyRejectByReason, policyRejectCode);

                    var policyResult = NpcIntentResolveResult.Rejected(policyRejectReason, policyRejectCode);
                    Monitor.Log($"NPC intent policy rejected lane={policyLane} [{policyRejectCode}] context={contextTag} cmd={attemptedCommand} reason={policyRejectReason}", LogLevel.Warn);
                    TryRecordAmbientLaneSnapshot(sourceNpcId, policyResult, policyLane, attemptedCommand);
                    return false;
                }
            }

            var result = _intentResolver.ResolveFromStreamLine(_state, line, resolverNpcId);
            if (!result.HasIntent)
                return false;

            var intentLane = ResolveIntentLane(
                !string.IsNullOrWhiteSpace(result.IntentId) ? result.IntentId : extractedIntentId,
                sourceNpcId);
            var metricCommand = ResolveCommandNameForMetrics(result, attemptedCommand);

            if (result.IsRejected)
            {
                _state.Telemetry.Daily.NpcIntentsRejected += 1;
                if (intentLane == "auto")
                    _state.Telemetry.Daily.NpcIntentsAutoRejected += 1;
                else if (intentLane == "manual")
                    _state.Telemetry.Daily.NpcIntentsManualRejected += 1;

                if (isAmbientContext)
                    IncrementCounter(_state.Telemetry.Daily.AmbientCommandRejectedByType, metricCommand);

                Monitor.Log($"NPC intent rejected lane={intentLane} [{result.ReasonCode}]: {result.Reason}", LogLevel.Warn);
                TryRecordAmbientLaneSnapshot(sourceNpcId, result, intentLane, attemptedCommand);
                return false;
            }

            if (result.IsDuplicate)
            {
                _state.Telemetry.Daily.NpcIntentsDuplicate += 1;
                if (isAmbientContext)
                    IncrementCounter(_state.Telemetry.Daily.AmbientCommandDuplicateByType, metricCommand);
                Monitor.Log($"NPC intent duplicate ignored: {result.IntentId}", LogLevel.Debug);
                TryRecordAmbientLaneSnapshot(sourceNpcId, result, intentLane, attemptedCommand);
                return false;
            }

            if (!result.AppliedOk)
                return false;

            _state.Telemetry.Daily.NpcIntentsApplied += 1;
            if (intentLane == "auto")
                _state.Telemetry.Daily.NpcIntentsAutoApplied += 1;
            else if (intentLane == "manual")
                _state.Telemetry.Daily.NpcIntentsManualApplied += 1;

            _state.Telemetry.Daily.NpcCommandAppliedByType.TryGetValue(result.Command, out var cmdCount);
            _state.Telemetry.Daily.NpcCommandAppliedByType[result.Command] = cmdCount + 1;
            if (isAmbientContext)
                IncrementCounter(_state.Telemetry.Daily.AmbientCommandAppliedByType, metricCommand);

            Monitor.Log($"Applied NPC command lane={intentLane}: {result.Command} -> outcome {result.OutcomeId} (intent={result.IntentId})", LogLevel.Debug);
            TryRecordAmbientLaneSnapshot(sourceNpcId, result, intentLane, attemptedCommand);
            TryShowSimulationMutationToast(result, intentLane, isAmbientContext);

            if (result.Proposal is not null)
            {
                var p = result.Proposal;
                Monitor.Log($"Quest mapping | requested: template={p.RequestedTemplate}, target={p.RequestedTarget}, urgency={p.RequestedUrgency} | applied: template={p.AppliedTemplate}, target={p.AppliedTarget}, urgency={p.AppliedUrgency}, count={p.Count}, reward={p.RewardGold}, expires+{p.ExpiresDelta}d | fallback={result.FallbackUsed}", LogLevel.Trace);

                if (!string.IsNullOrWhiteSpace(sourceNpcId)
                    && _player2NpcShortNameById.TryGetValue(sourceNpcId, out var issuerShortName))
                {
                    var q = _state.Quests.Available.FirstOrDefault(x => x.QuestId.Equals(result.OutcomeId, StringComparison.OrdinalIgnoreCase));
                    if (q is not null)
                        q.Issuer = issuerShortName.ToLowerInvariant();
                }
            }

            if (result.Command.Equals("propose_quest", StringComparison.OrdinalIgnoreCase)
                && !isAmbientContext
                && !string.Equals(intentLane, "auto", StringComparison.OrdinalIgnoreCase))
            {
                ShowQuestPostedToast(result.OutcomeId, sourceNpcId);
            }

            _player2LastCommandApplied = $"{result.Command}:{result.OutcomeId}";
            _player2LastCommandAppliedUtc = DateTime.UtcNow;

            if (result.Command.Equals("publish_rumor", StringComparison.OrdinalIgnoreCase)
                || result.Command.Equals("publish_article", StringComparison.OrdinalIgnoreCase))
            {
                var outcomeId = result.OutcomeId;
                TryApplyNpcPublishSourceName(result.Command, outcomeId, sourceNpcId);
                UpsertPendingNpcPublishUpdate(new NpcPublishHeadlineUpdate
                {
                    Day = _state.Calendar.Day,
                    Command = result.Command,
                    OutcomeId = outcomeId,
                    SourceNpcId = sourceNpcId,
                    Headline = string.Empty
                });
                QueueNpcPublishHeadlineGeneration(result.Command, outcomeId, sourceNpcId);
                TryApplyCompletedNpcPublishHeadlineUpdates();
            }

            return true;
        }
        catch (Exception ex)
        {
            Monitor.Log($"NPC command parse skipped: {ex.Message}", LogLevel.Trace);
            return false;
        }
    }

    private int CountAmbientTownEventsForSourceToday(string? sourceNpcName)
    {
        if (string.IsNullOrWhiteSpace(sourceNpcName))
            return _state.TownMemory.Events.Count(e => e.Day == _state.Calendar.Day);

        return _state.TownMemory.Events.Count(e =>
            e.Day == _state.Calendar.Day
            && string.Equals(e.SourceNpc, sourceNpcName, StringComparison.OrdinalIgnoreCase));
    }

    private string ResolveIntentLane(string? intentId, string? sourceNpcId)
    {
        if (!string.IsNullOrWhiteSpace(intentId)
            && intentId.StartsWith("auto_", StringComparison.OrdinalIgnoreCase))
        {
            return "auto";
        }

        if (!string.IsNullOrWhiteSpace(sourceNpcId)
            && _npcLastContextTagById.TryGetValue(sourceNpcId, out var contextTag)
            && !string.IsNullOrWhiteSpace(contextTag)
            && contextTag.StartsWith("manual_", StringComparison.OrdinalIgnoreCase))
        {
            return "manual";
        }

        return "chat";
    }

    private string ResolveContextTagForPolicy(string? sourceNpcId)
    {
        if (!string.IsNullOrWhiteSpace(sourceNpcId)
            && _npcLastContextTagById.TryGetValue(sourceNpcId, out var contextTag)
            && !string.IsNullOrWhiteSpace(contextTag))
        {
            return contextTag;
        }

        return "player_chat";
    }

    private bool IsAmbientContext(string? sourceNpcId)
    {
        if (string.IsNullOrWhiteSpace(sourceNpcId))
            return false;

        return _npcLastContextTagById.TryGetValue(sourceNpcId, out var contextTag)
            && string.Equals(contextTag, "npc_to_npc_ambient", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveCommandNameForMetrics(NpcIntentResolveResult result, string attemptedCommand)
    {
        if (!string.IsNullOrWhiteSpace(result.Command))
            return result.Command;

        return string.IsNullOrWhiteSpace(attemptedCommand) ? "(unknown)" : attemptedCommand;
    }

    private static void IncrementCounter(Dictionary<string, int> counters, string key)
    {
        var normalizedKey = string.IsNullOrWhiteSpace(key) ? "(unknown)" : key.Trim().ToLowerInvariant();
        counters.TryGetValue(normalizedKey, out var count);
        counters[normalizedKey] = count + 1;
    }

    private static string FormatCounterMap(Dictionary<string, int> counters)
    {
        if (counters is null || counters.Count == 0)
            return "(none)";

        return string.Join(", ",
            counters
                .OrderByDescending(kv => kv.Value)
                .ThenBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
                .Take(6)
                .Select(kv => $"{kv.Key}:{kv.Value}"));
    }

    private void TryRecordAmbientLaneSnapshot(string? sourceNpcId, NpcIntentResolveResult result, string intentLane, string attemptedCommand)
    {
        if (string.IsNullOrWhiteSpace(sourceNpcId))
            return;

        if (!_npcLastContextTagById.TryGetValue(sourceNpcId, out var contextTag)
            || !string.Equals(contextTag, "npc_to_npc_ambient", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var sourceLabel = _player2NpcShortNameById.TryGetValue(sourceNpcId, out var shortName)
            ? shortName
            : sourceNpcId;

        var command = !string.IsNullOrWhiteSpace(result.Command)
            ? result.Command
            : (string.IsNullOrWhiteSpace(attemptedCommand) ? "(unknown)" : attemptedCommand);

        string outcome;
        if (result.IsRejected)
            outcome = $"rejected[{result.ReasonCode}] {TruncateDebugText(result.Reason, 120)}";
        else if (result.IsDuplicate)
            outcome = "duplicate";
        else if (result.AppliedOk)
            outcome = $"applied outcome={result.OutcomeId}";
        else
            outcome = "no-op";

        var snapshot = $"{DateTime.UtcNow:HH:mm:ss} npc={sourceLabel} context={contextTag} lane={intentLane} cmd={command} {outcome}";
        _ambientLaneDebugSnapshots.Enqueue(snapshot);
        while (_ambientLaneDebugSnapshots.Count > AmbientLaneDebugSnapshotLimit && _ambientLaneDebugSnapshots.TryDequeue(out _))
        {
        }
    }

    private bool ShouldIgnoreLowInformationAmbientOutput(string line, string? sourceNpcId)
    {
        if (!IsAmbientContext(sourceNpcId))
            return false;

        if (!string.IsNullOrWhiteSpace(TryExtractCommandNameFromLine(line)))
            return false;

        var message = TryExtractMessageFromLine(line);
        if (string.IsNullOrWhiteSpace(message))
            return true;

        var normalized = Regex.Replace(message.Trim().ToLowerInvariant(), @"\s+", " ");
        if (LowInfoAmbientMessages.Contains(normalized))
            return true;

        var wordCount = normalized
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Length;
        if (normalized.Length < 22 && wordCount <= 4)
            return true;

        if (normalized.StartsWith("nothing to report", StringComparison.Ordinal)
            || normalized.StartsWith("nothing happened", StringComparison.Ordinal)
            || normalized.StartsWith("no updates", StringComparison.Ordinal))
        {
            return true;
        }

        return false;
    }

    private static string TryExtractMessageFromLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return string.Empty;

        try
        {
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;
            if (!root.TryGetProperty("message", out var msgEl) || msgEl.ValueKind != JsonValueKind.String)
                return string.Empty;

            return (msgEl.GetString() ?? string.Empty).Trim();
        }
        catch
        {
        }

        return string.Empty;
    }

    private bool HasRecentVisibleTownEventForPublish(int minSeverity, bool requirePublicVisibility, int maxAgeDays = 2)
    {
        var currentDay = _state.Calendar.Day;
        foreach (var ev in _state.TownMemory.Events)
        {
            var age = currentDay - ev.Day;
            if (age < 0 || age > maxAgeDays)
                continue;
            if (ev.Severity < minSeverity)
                continue;
            if (requirePublicVisibility && !string.Equals(ev.Visibility, "public", StringComparison.OrdinalIgnoreCase))
                continue;
            if (string.IsNullOrWhiteSpace(ev.Summary))
                continue;

            return true;
        }

        return false;
    }

    private static float? TryExtractNumericArgumentFromLine(string line, string argumentName)
    {
        if (string.IsNullOrWhiteSpace(line) || string.IsNullOrWhiteSpace(argumentName))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;
            if (TryReadNumericArgument(root, argumentName, out var directValue))
                return directValue;

            if (!root.TryGetProperty("command", out var commandEl) || commandEl.ValueKind != JsonValueKind.Array)
                return null;

            foreach (var commandItem in commandEl.EnumerateArray())
            {
                if (TryReadNumericArgument(commandItem, argumentName, out var nestedValue))
                    return nestedValue;
            }
        }
        catch
        {
        }

        return null;
    }

    private static bool TryReadNumericArgument(JsonElement element, string argumentName, out float value)
    {
        value = 0f;
        if (element.ValueKind != JsonValueKind.Object)
            return false;
        if (!element.TryGetProperty("arguments", out var argsEl) || argsEl.ValueKind != JsonValueKind.Object)
            return false;
        if (!argsEl.TryGetProperty(argumentName, out var valueEl) || valueEl.ValueKind != JsonValueKind.Number)
            return false;
        if (!valueEl.TryGetSingle(out value))
            return false;

        return true;
    }

    private static string TryExtractIntentIdFromLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return string.Empty;

        try
        {
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;
            if (!root.TryGetProperty("intent_id", out var intentIdEl) || intentIdEl.ValueKind != JsonValueKind.String)
                return string.Empty;

            return (intentIdEl.GetString() ?? string.Empty).Trim();
        }
        catch
        {
        }

        return string.Empty;
    }

    private static string TryExtractCommandNameFromLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return string.Empty;

        try
        {
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;
            if (!root.TryGetProperty("command", out var commandEl))
                return string.Empty;

            if (commandEl.ValueKind == JsonValueKind.String)
                return (commandEl.GetString() ?? string.Empty).Trim();

            if (commandEl.ValueKind != JsonValueKind.Array)
                return string.Empty;

            foreach (var cmd in commandEl.EnumerateArray())
            {
                if (cmd.ValueKind != JsonValueKind.Object)
                    continue;

                if (!cmd.TryGetProperty("name", out var nameEl) || nameEl.ValueKind != JsonValueKind.String)
                    continue;

                return (nameEl.GetString() ?? string.Empty).Trim();
            }
        }
        catch
        {
        }

        return string.Empty;
    }

    private static string TruncateDebugText(string? value, int maxLength)
    {
        var text = (value ?? string.Empty).Trim();
        if (text.Length <= maxLength)
            return text;

        return text[..Math.Max(1, maxLength - 3)] + "...";
    }

    private void TryRunAutomaticNpcCommandExposureHooks()
    {
        if (!Context.IsWorldReady || _intentResolver is null)
            return;

        TryRunAutoQuestLifecycleReputationHooks();
        if (!_config.EnableAmbientConsequencePipeline)
            return;
        if (!HasAmbientCadenceMutationAllowance())
            return;
        TryRunAutoSocialReputationFromEvents();
        TryRunAutoMarketModifierHooks();
        TryRunAutoInterestShiftHooks();
        TryRunAutoQuestFromEventsHooks();
    }

    private bool HasAmbientCadenceMutationAllowance()
    {
        _state.Telemetry.Daily.AmbientCommandAppliedByType.TryGetValue("record_town_event", out var ambientEventsToday);
        var cadenceKey = $"auto:cadence:check:{_state.Calendar.Day}:{ambientEventsToday}";
        if (ambientEventsToday <= 0)
        {
            RecordAmbientCadenceSkipOnce(cadenceKey);
            return false;
        }

        var allowedMutations = 1 + ((ambientEventsToday - 1) / Math.Max(1, AmbientEventsPerAdditionalAutoMutation));
        var appliedMutations = CountAppliedAmbientConsequenceMutationsToday();
        if (appliedMutations >= allowedMutations)
        {
            RecordAmbientCadenceSkipOnce(cadenceKey);
            return false;
        }

        return true;
    }

    private int CountAppliedAmbientConsequenceMutationsToday()
    {
        var today = _state.Calendar.Day;
        return _state.Facts.ProcessedIntents.Count(entry =>
            IsAmbientConsequenceAutoMutation(entry.Key, entry.Value, today));
    }

    private static bool IsAmbientConsequenceAutoMutation(string intentId, ProcessedIntentValue processed, int today)
    {
        if (processed is null)
            return false;
        if (processed.Day != today)
            return false;
        if (!processed.Status.Equals("applied", StringComparison.OrdinalIgnoreCase))
            return false;
        if (string.IsNullOrWhiteSpace(intentId))
            return false;

        if (intentId.StartsWith("auto_rep_evt_", StringComparison.OrdinalIgnoreCase))
            return processed.Command.Equals("adjust_reputation", StringComparison.OrdinalIgnoreCase);
        if (intentId.StartsWith("auto_interest_", StringComparison.OrdinalIgnoreCase))
            return processed.Command.Equals("shift_interest_influence", StringComparison.OrdinalIgnoreCase);
        if (intentId.StartsWith("auto_mkt_", StringComparison.OrdinalIgnoreCase))
            return processed.Command.Equals("apply_market_modifier", StringComparison.OrdinalIgnoreCase);
        if (intentId.StartsWith("auto_evt_q_", StringComparison.OrdinalIgnoreCase))
            return processed.Command.Equals("propose_quest", StringComparison.OrdinalIgnoreCase);

        return false;
    }

    private void RecordAmbientCadenceSkipOnce(string key)
    {
        if (_state.Facts.Facts.ContainsKey(key))
            return;

        _state.Facts.Facts[key] = new FactValue
        {
            Value = true,
            SetDay = _state.Calendar.Day,
            Source = "auto_command"
        };
        _state.Telemetry.Daily.AmbientCadenceSkips += 1;
    }

    private void TryRunAutoQuestLifecycleReputationHooks()
    {
        // Snapshot keys first because applying intents mutates the facts table.
        var lifecycleFactKeysToday = _state.Facts.Facts
            .Where(kv => kv.Value.SetDay == _state.Calendar.Day)
            .Select(kv => kv.Key)
            .ToArray();

        foreach (var key in lifecycleFactKeysToday)
        {
            if (!TryParseQuestLifecycleFact(key, out var questId, out var lifecycle))
                continue;

            var delta = lifecycle switch
            {
                "accepted" => 1,
                "completed" => 2,
                "failed" => -2,
                _ => 0
            };
            if (delta == 0)
                continue;

            var issuer = ResolveQuestIssuerById(questId);
            if (string.IsNullOrWhiteSpace(issuer))
                continue;

            var intentId = $"auto_rep_q_{_state.Calendar.Day}_{lifecycle}_{questId}";
            TryApplyAutoIntentOnce(
                intentId,
                "auto_quest",
                "adjust_reputation",
                new
                {
                    target = issuer,
                    delta,
                    reason = $"auto:{lifecycle}:quest_lifecycle"
                });
        }
    }

    private void TryRunAutoSocialReputationFromEvents()
    {
        if (_ambientConsequenceService is null)
            return;

        var dayGateKey = $"auto:rep:event:day:{_state.Calendar.Day}";
        if (_state.Facts.Facts.ContainsKey(dayGateKey))
            return;

        var recentEvents = _ambientConsequenceService
            .ReadRecentEvents(_state, maxAgeDays: 1, minSeverity: 2, maxCount: 8)
            .OrderByDescending(ev => ev.Severity)
            .ThenByDescending(ev => ev.Day)
            .ThenBy(ev => ev.EventId, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (recentEvents.Count == 0)
            return;

        foreach (var ev in recentEvents)
        {
            var target = ResolveNpcTargetFromTownEvent(ev);
            if (string.IsNullOrWhiteSpace(target))
                continue;

            var baseDelta = ev.Kind.Equals("incident", StringComparison.OrdinalIgnoreCase)
                || ev.Kind.Equals("fainting", StringComparison.OrdinalIgnoreCase)
                ? -1
                : 1;
            if (ev.Severity >= 4)
                baseDelta *= 2;

            var delta = Math.Clamp(baseDelta, -2, 2);
            var eventToken = Regex.Replace(ev.EventId ?? "evt", @"[^a-zA-Z0-9_]+", "_");
            var intentId = $"auto_rep_evt_{_state.Calendar.Day}_{eventToken}_{target}";
            TryApplyAutoIntentOnce(
                intentId,
                "auto_town",
                "adjust_reputation",
                new
                {
                    target,
                    delta,
                    reason = $"auto:event_pattern:{ev.Kind}:{ev.Visibility}"
                });

            _state.Facts.Facts[dayGateKey] = new FactValue
            {
                Value = true,
                SetDay = _state.Calendar.Day,
                Source = "auto_command"
            };
            break;
        }
    }

    private void TryRunAutoMarketModifierHooks()
    {
        if (_ambientConsequenceService is null || _state.Economy.Crops.Count == 0)
            return;

        var upGateKey = $"auto:market:up:day:{_state.Calendar.Day}";
        var downGateKey = $"auto:market:down:day:{_state.Calendar.Day}";
        var snapshot = _ambientConsequenceService.BuildSignalSnapshot(_state, maxAgeDays: 2, minSeverity: 2, maxCount: 20);
        var marketSignals = GetSignalCount(snapshot.KindCounts, "market")
            + GetSignalCount(snapshot.TagCounts, "market", "price", "demand", "supply", "scarcity", "shortage", "oversupply", "surplus");
        if (marketSignals < AutoMarketMinSignals)
            return;

        var scarcitySignals = GetSignalCount(snapshot.TagCounts, "scarcity", "shortage", "demand", "stockout");
        var oversupplySignals = GetSignalCount(snapshot.TagCounts, "oversupply", "surplus", "glut", "flooded", "supply");

        var scarcityCandidate = _state.Economy.Crops
            .OrderByDescending(kv => kv.Value.ScarcityBonus)
            .ThenByDescending(kv => kv.Value.TrendEma)
            .FirstOrDefault();
        if (!_state.Facts.Facts.ContainsKey(upGateKey)
            && !string.IsNullOrWhiteSpace(scarcityCandidate.Key)
            && !WasAutoCommandTargetUsedYesterday("apply_market_modifier", scarcityCandidate.Key)
            && scarcityCandidate.Value.ScarcityBonus >= AutoMarketScarcityThreshold
            && (scarcitySignals > 0 || scarcityCandidate.Value.ScarcityBonus >= AutoMarketStrongScarcityThreshold))
        {
            var deltaUp = scarcitySignals >= 2 ? 0.07f : 0.05f;
            var intentId = $"auto_mkt_up_{_state.Calendar.Day}_{scarcityCandidate.Key}";
            TryApplyAutoIntentOnce(
                intentId,
                "auto_market",
                "apply_market_modifier",
                new
                {
                    crop = scarcityCandidate.Key,
                    delta_pct = deltaUp,
                    duration_days = 2,
                    reason = "auto:market_scarcity_event_signal"
                });
            _state.Facts.Facts[upGateKey] = new FactValue
            {
                Value = true,
                SetDay = _state.Calendar.Day,
                Source = "auto_command"
            };
        }

        var oversupplyCandidate = _state.Economy.Crops
            .OrderByDescending(kv => kv.Value.RollingSellVolume7D)
            .ThenBy(kv => kv.Value.SupplyPressureFactor)
            .FirstOrDefault();
        if (!_state.Facts.Facts.ContainsKey(downGateKey)
            && !string.IsNullOrWhiteSpace(oversupplyCandidate.Key)
            && !WasAutoCommandTargetUsedYesterday("apply_market_modifier", oversupplyCandidate.Key)
            && oversupplyCandidate.Value.RollingSellVolume7D > 0
            && oversupplyCandidate.Value.SupplyPressureFactor <= AutoMarketOversupplyThreshold
            && (oversupplySignals > 0 || oversupplyCandidate.Value.SupplyPressureFactor <= AutoMarketDeepOversupplyThreshold))
        {
            var deltaDown = oversupplySignals >= 2 ? -0.07f : -0.05f;
            var intentId = $"auto_mkt_down_{_state.Calendar.Day}_{oversupplyCandidate.Key}";
            TryApplyAutoIntentOnce(
                intentId,
                "auto_market",
                "apply_market_modifier",
                new
                {
                    crop = oversupplyCandidate.Key,
                    delta_pct = deltaDown,
                    duration_days = 2,
                    reason = "auto:market_oversupply_event_signal"
                });
            _state.Facts.Facts[downGateKey] = new FactValue
            {
                Value = true,
                SetDay = _state.Calendar.Day,
                Source = "auto_command"
            };
        }
    }

    private void TryRunAutoInterestShiftHooks()
    {
        if (_ambientConsequenceService is null)
            return;

        var dayGateKey = $"auto:interest:day:{_state.Calendar.Day}";
        if (_state.Facts.Facts.ContainsKey(dayGateKey))
            return;

        var snapshot = _ambientConsequenceService.BuildSignalSnapshot(_state, maxAgeDays: 2, minSeverity: 2, maxCount: 20);
        if (snapshot.TotalEvents < 2)
            return;
        if (!TryResolveInterestFromRepeatedSignals(snapshot, out var interest, out var signalCount))
            return;
        if (WasAutoCommandTargetUsedYesterday("shift_interest_influence", interest))
            return;

        var delta = signalCount >= 4 ? 2 : 1;
        var intentId = $"auto_interest_{_state.Calendar.Day}_{interest}_{signalCount}";
        TryApplyAutoIntentOnce(
            intentId,
            "auto_town",
            "shift_interest_influence",
            new
            {
                interest,
                delta,
                reason = $"auto:repeated_topic_signal:{signalCount}"
            });
        _state.Facts.Facts[dayGateKey] = new FactValue
        {
            Value = true,
            SetDay = _state.Calendar.Day,
            Source = "auto_command"
        };
    }

    private void TryRunAutoQuestFromEventsHooks()
    {
        if (_ambientConsequenceService is null)
            return;

        var dayGateKey = $"auto:quest:event:day:{_state.Calendar.Day}";
        if (_state.Facts.Facts.ContainsKey(dayGateKey))
            return;
        if (_state.Quests.Available.Count + _state.Quests.Active.Count >= 8)
            return;

        var recentEvents = _ambientConsequenceService
            .ReadRecentEvents(_state, maxAgeDays: 1, minSeverity: 2, maxCount: 12)
            .OrderByDescending(ev => ev.Severity)
            .ThenByDescending(ev => ev.Day)
            .ThenBy(ev => ev.EventId, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (recentEvents.Count == 0)
            return;

        foreach (var ev in recentEvents)
        {
            if (!TryBuildQuestCandidateFromEvent(ev, out var templateId, out var target, out var urgency))
                continue;
            if (!IsQuestCandidateEligible(templateId, target))
                continue;
            if (WasAutoQuestMotifUsedYesterday(templateId, target))
                continue;

            var eventToken = Regex.Replace(ev.EventId ?? "evt", @"[^a-zA-Z0-9_]+", "_");
            var targetToken = Regex.Replace(target, @"[^a-zA-Z0-9_]+", "_");
            var intentId = $"auto_evt_q_{_state.Calendar.Day}_{eventToken}_{templateId}_{targetToken}";
            TryApplyAutoIntentOnce(
                intentId,
                "auto_town",
                "propose_quest",
                new
                {
                    template_id = templateId,
                    target,
                    urgency
                });

            _state.Facts.Facts[dayGateKey] = new FactValue
            {
                Value = true,
                SetDay = _state.Calendar.Day,
                Source = "auto_command"
            };
            break;
        }
    }

    private void TryApplyAutoIntentOnce(string intentId, string npcId, string command, object arguments)
    {
        var attemptKey = $"auto:intent:attempted:{intentId}";
        if (_state.Facts.Facts.ContainsKey(attemptKey))
            return;
        if (!HasAutoMutationBudgetCapacity(command))
        {
            var axis = ResolveMutationAxis(command);
            Monitor.Log($"Skipped auto intent due daily budget axis={axis} command={command} intent={intentId}", LogLevel.Trace);
            return;
        }

        var payload = JsonSerializer.Serialize(new
        {
            intent_id = intentId,
            npc_id = npcId,
            command,
            arguments
        });

        var applied = TryApplyNpcCommandFromLine(payload);
        if (applied)
            RecordAutoMutationBudgetUsage(command, intentId);
        _state.Facts.Facts[attemptKey] = new FactValue
        {
            Value = true,
            SetDay = _state.Calendar.Day,
            Source = "auto_command"
        };
    }

    private bool HasAutoMutationBudgetCapacity(string command)
    {
        var axis = ResolveMutationAxis(command);
        if (string.IsNullOrWhiteSpace(axis))
            return true;
        if (!AutoMutationBudgetByAxis.TryGetValue(axis, out var budget) || budget <= 0)
            return true;

        var prefix = $"auto:budget:{_state.Calendar.Day}:{axis}:";
        var used = _state.Facts.Facts.Keys.Count(key => key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        return used < budget;
    }

    private void RecordAutoMutationBudgetUsage(string command, string intentId)
    {
        var axis = ResolveMutationAxis(command);
        if (string.IsNullOrWhiteSpace(axis))
            return;

        var key = $"auto:budget:{_state.Calendar.Day}:{axis}:{intentId}";
        _state.Facts.Facts[key] = new FactValue
        {
            Value = true,
            SetDay = _state.Calendar.Day,
            Source = "auto_command"
        };
    }

    private static string ResolveMutationAxis(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return string.Empty;

        return command.Trim().ToLowerInvariant() switch
        {
            "adjust_reputation" => "social",
            "shift_interest_influence" => "interest",
            "apply_market_modifier" => "market",
            "propose_quest" => "quest",
            _ => string.Empty
        };
    }

    private bool TryBuildQuestCandidateFromEvent(TownMemoryEvent ev, out string templateId, out string target, out string urgency)
    {
        templateId = string.Empty;
        target = string.Empty;
        urgency = "low";

        var severity = Math.Clamp(ev.Severity, 1, 5);
        urgency = severity >= 4 ? "high" : severity == 3 ? "medium" : "low";

        var cropTarget = ResolveCropTargetFromTownEvent(ev);
        if (!string.IsNullOrWhiteSpace(cropTarget)
            && (ev.Kind.Equals("market", StringComparison.OrdinalIgnoreCase)
                || ev.Tags.Any(tag => tag.Contains("market", StringComparison.OrdinalIgnoreCase))
                || ev.Tags.Any(tag => tag.Contains("harvest", StringComparison.OrdinalIgnoreCase))))
        {
            templateId = "gather_crop";
            target = cropTarget;
            urgency = TuneEventQuestUrgency(templateId, target, urgency);
            return true;
        }

        if (ev.Kind.Equals("incident", StringComparison.OrdinalIgnoreCase)
            || ev.Tags.Any(tag =>
                tag.Contains("mine", StringComparison.OrdinalIgnoreCase)
                || tag.Contains("cave", StringComparison.OrdinalIgnoreCase)
                || tag.Contains("ore", StringComparison.OrdinalIgnoreCase)))
        {
            templateId = "mine_resource";
            target = ev.Tags.Any(tag => tag.Contains("iron", StringComparison.OrdinalIgnoreCase))
                ? "iron_ore"
                : "copper_ore";
            urgency = TuneEventQuestUrgency(templateId, target, urgency);
            return true;
        }

        var visitTarget = ResolveNpcTargetFromTownEvent(ev);
        if (!string.IsNullOrWhiteSpace(visitTarget)
            && (ev.Kind.Equals("social", StringComparison.OrdinalIgnoreCase)
                || ev.Kind.Equals("community", StringComparison.OrdinalIgnoreCase)))
        {
            templateId = "social_visit";
            target = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(visitTarget);
            urgency = TuneEventQuestUrgency(templateId, target, urgency);
            return true;
        }

        return false;
    }

    private string TuneEventQuestUrgency(string templateId, string target, string baseUrgency)
    {
        var score = baseUrgency.ToLowerInvariant() switch
        {
            "high" => 3,
            "medium" => 2,
            _ => 1
        };

        if (templateId.Equals("gather_crop", StringComparison.OrdinalIgnoreCase)
            && _state.Economy.Crops.TryGetValue(target, out var crop))
        {
            var unitValue = crop.PriceToday > 0 ? crop.PriceToday : crop.BasePrice;
            if (unitValue >= 220)
                score = Math.Max(score, 3);
            else if (unitValue >= 140)
                score = Math.Max(score, 2);
        }
        else if (templateId.Equals("mine_resource", StringComparison.OrdinalIgnoreCase))
        {
            if (target.Equals("gold_ore", StringComparison.OrdinalIgnoreCase))
                score = Math.Max(score, 3);
            else if (target.Equals("iron_ore", StringComparison.OrdinalIgnoreCase))
                score = Math.Max(score, 2);
        }

        return score switch
        {
            >= 3 => "high",
            2 => "medium",
            _ => "low"
        };
    }

    private bool IsQuestCandidateEligible(string templateId, string target)
    {
        if (string.IsNullOrWhiteSpace(templateId) || string.IsNullOrWhiteSpace(target))
            return false;

        return !_state.Quests.Available.Any(q =>
                   q.TemplateId.Equals(templateId, StringComparison.OrdinalIgnoreCase)
                   && q.TargetItem.Equals(target, StringComparison.OrdinalIgnoreCase))
               && !_state.Quests.Active.Any(q =>
                   q.TemplateId.Equals(templateId, StringComparison.OrdinalIgnoreCase)
                   && q.TargetItem.Equals(target, StringComparison.OrdinalIgnoreCase));
    }

    private string ResolveCropTargetFromTownEvent(TownMemoryEvent ev)
    {
        var supplyKeys = BuildQuestSupplyCandidateList()
            .OrderByDescending(key => key.Length)
            .ThenBy(key => key, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (supplyKeys.Count == 0)
            return string.Empty;

        foreach (var tag in ev.Tags ?? Array.Empty<string>())
        {
            var normalizedTag = NormalizeTargetToken(tag);
            var match = supplyKeys.FirstOrDefault(item => item.Equals(normalizedTag, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(match))
                return match;
        }

        foreach (var item in supplyKeys)
        {
            if (ev.Summary.Contains(item.Replace("_", " ", StringComparison.Ordinal), StringComparison.OrdinalIgnoreCase)
                || ev.Summary.Contains(item, StringComparison.OrdinalIgnoreCase))
            {
                return item;
            }
        }

        return string.Empty;
    }

    private string ResolveNpcTargetFromTownEvent(TownMemoryEvent ev)
    {
        foreach (var tag in ev.Tags ?? Array.Empty<string>())
        {
            var match = VanillaNpcRoster.FirstOrDefault(name => string.Equals(name, tag, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(match))
                return match.ToLowerInvariant();
        }

        foreach (var name in VanillaNpcRoster)
        {
            if (ev.Summary.Contains(name, StringComparison.OrdinalIgnoreCase)
                || ev.Location.Contains(name, StringComparison.OrdinalIgnoreCase))
            {
                return name.ToLowerInvariant();
            }
        }

        if (_config.EnableCustomNpcFramework
            && _customNpcRegistry is not null
            && _customNpcRegistry.TryResolveNpcTokenFromTownEvent(ev, out var customToken))
        {
            return customToken;
        }

        return string.Empty;
    }

    private string ResolveQuestIssuerById(string questId)
    {
        if (string.IsNullOrWhiteSpace(questId))
            return string.Empty;

        var quest = _state.Quests.Active.FirstOrDefault(q => q.QuestId.Equals(questId, StringComparison.OrdinalIgnoreCase))
            ?? _state.Quests.Completed.FirstOrDefault(q => q.QuestId.Equals(questId, StringComparison.OrdinalIgnoreCase))
            ?? _state.Quests.Failed.FirstOrDefault(q => q.QuestId.Equals(questId, StringComparison.OrdinalIgnoreCase))
            ?? _state.Quests.Available.FirstOrDefault(q => q.QuestId.Equals(questId, StringComparison.OrdinalIgnoreCase));
        return (quest?.Issuer ?? string.Empty).Trim().ToLowerInvariant();
    }

    private static bool TryParseQuestLifecycleFact(string key, out string questId, out string lifecycle)
    {
        questId = string.Empty;
        lifecycle = string.Empty;
        if (string.IsNullOrWhiteSpace(key))
            return false;

        var parts = key.Split(':', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3 || !parts[0].Equals("quest", StringComparison.OrdinalIgnoreCase))
            return false;

        var status = parts[2].Trim().ToLowerInvariant();
        if (status is not ("accepted" or "completed" or "failed"))
            return false;

        questId = parts[1].Trim();
        lifecycle = status;
        return !string.IsNullOrWhiteSpace(questId);
    }

    private bool WasAutoCommandTargetUsedYesterday(string command, string target)
    {
        if (string.IsNullOrWhiteSpace(command) || string.IsNullOrWhiteSpace(target))
            return false;

        var yesterday = _state.Calendar.Day - 1;
        if (yesterday < 1)
            return false;

        var normalizedCommand = command.Trim().ToLowerInvariant();
        var targetToken = NormalizeAutoTargetToken(target);
        if (string.IsNullOrWhiteSpace(targetToken))
            return false;

        foreach (var entry in _state.Facts.ProcessedIntents)
        {
            var processed = entry.Value;
            if (processed is null)
                continue;
            if (processed.Day != yesterday)
                continue;
            if (!processed.Status.Equals("applied", StringComparison.OrdinalIgnoreCase))
                continue;
            if (!processed.Command.Equals(normalizedCommand, StringComparison.OrdinalIgnoreCase))
                continue;
            if (!entry.Key.StartsWith("auto_", StringComparison.OrdinalIgnoreCase))
                continue;
            if (!entry.Key.Contains(targetToken, StringComparison.OrdinalIgnoreCase))
                continue;

            return true;
        }

        return false;
    }

    private bool WasAutoQuestMotifUsedYesterday(string templateId, string target)
    {
        if (string.IsNullOrWhiteSpace(templateId) || string.IsNullOrWhiteSpace(target))
            return false;

        var yesterday = _state.Calendar.Day - 1;
        if (yesterday < 1)
            return false;

        var templateToken = NormalizeAutoTargetToken(templateId);
        var targetToken = NormalizeAutoTargetToken(target);
        if (string.IsNullOrWhiteSpace(templateToken) || string.IsNullOrWhiteSpace(targetToken))
            return false;

        foreach (var entry in _state.Facts.ProcessedIntents)
        {
            var processed = entry.Value;
            if (processed is null)
                continue;
            if (processed.Day != yesterday)
                continue;
            if (!processed.Status.Equals("applied", StringComparison.OrdinalIgnoreCase))
                continue;
            if (!processed.Command.Equals("propose_quest", StringComparison.OrdinalIgnoreCase))
                continue;
            if (!entry.Key.StartsWith("auto_evt_q_", StringComparison.OrdinalIgnoreCase))
                continue;
            if (!entry.Key.Contains(templateToken, StringComparison.OrdinalIgnoreCase))
                continue;
            if (!entry.Key.Contains(targetToken, StringComparison.OrdinalIgnoreCase))
                continue;

            return true;
        }

        return false;
    }

    private static string NormalizeAutoTargetToken(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var normalized = Regex.Replace(value.Trim().ToLowerInvariant(), @"[^a-z0-9]+", "_");
        return normalized.Trim('_');
    }

    private static bool TryResolveInterestFromRepeatedSignals(AmbientEventSignalSnapshot snapshot, out string interest, out int signalCount)
    {
        interest = string.Empty;
        signalCount = 0;
        if (snapshot is null || snapshot.TotalEvents <= 0)
            return false;

        var marketSignals = GetSignalCount(snapshot.KindCounts, "market")
            + GetSignalCount(snapshot.TagCounts, "market", "shop", "trade", "scarcity", "oversupply");
        var natureSignals = GetSignalCount(snapshot.KindCounts, "nature")
            + GetSignalCount(snapshot.TagCounts, "nature", "forest", "river", "wildlife", "forage");
        var incidentSignals = GetSignalCount(snapshot.KindCounts, "incident")
            + GetSignalCount(snapshot.TagCounts, "mine", "monster", "danger", "accident", "cave", "skull");
        var socialSignals = GetSignalCount(snapshot.KindCounts, "social", "community")
            + GetSignalCount(snapshot.TagCounts, "community", "social", "farm", "harvest", "neighbors", "volunteer");

        var candidates = new (string Interest, int Signals)[]
        {
            ("shopkeepers_guild", marketSignals),
            ("nature_keepers", natureSignals),
            ("adventurers_club", incidentSignals),
            ("farmers_circle", socialSignals)
        };

        var best = candidates
            .OrderByDescending(c => c.Signals)
            .ThenBy(c => c.Interest, StringComparer.OrdinalIgnoreCase)
            .First();
        if (best.Signals < 2)
            return false;

        interest = best.Interest;
        signalCount = best.Signals;
        return true;
    }

    private static int GetSignalCount(Dictionary<string, int> counters, params string[] keys)
    {
        if (counters is null || keys is null || keys.Length == 0)
            return 0;

        var total = 0;
        foreach (var key in keys)
        {
            if (string.IsNullOrWhiteSpace(key))
                continue;
            if (counters.TryGetValue(key.Trim().ToLowerInvariant(), out var value))
                total += value;
        }

        return total;
    }

    private void TryApplyFallbackQuestFromPlayerChatLine(string line)
    {
        if (_intentResolver is null)
            return;

        if (!TryExtractNpcIdAndMessage(line, out var npcId, out var message))
            return;

        if (!_npcLastPlayerChatRequestUtcById.TryGetValue(npcId, out var lastPlayerChatUtc))
            return;

        if (DateTime.UtcNow - lastPlayerChatUtc > TimeSpan.FromSeconds(45))
            return;

        if (LooksLikeQuestDecline(message))
        {
            _npcPendingFallbackQuestOfferById.TryRemove(npcId, out _);
            return;
        }

        PendingFallbackQuestOffer? offerFromCurrentLine = null;
        if (TryBuildPendingQuestOfferFromMessage(npcId, message, out var parsedOffer))
        {
            offerFromCurrentLine = parsedOffer;
            _npcPendingFallbackQuestOfferById[npcId] = parsedOffer;
        }

        var playerAskedForQuestRecently = _npcLastPlayerQuestAskUtcById.TryGetValue(npcId, out var questAskUtc)
            && DateTime.UtcNow - questAskUtc <= TimeSpan.FromSeconds(60);

        _npcLastPlayerPromptById.TryGetValue(npcId, out var lastPlayerPrompt);
        var playerAcceptedQuest = IsPlayerAcceptingQuest(lastPlayerPrompt);

        // Only synthesize propose_quest when the player either asked for work or explicitly accepted.
        if (!playerAskedForQuestRecently && !playerAcceptedQuest)
            return;

        PendingFallbackQuestOffer? offerToApply = offerFromCurrentLine;
        var usedPendingOffer = false;
        if (offerToApply is null
            && playerAcceptedQuest
            && TryGetPendingFallbackQuestOfferForAcceptance(npcId, message, out var pendingOffer))
        {
            offerToApply = pendingOffer;
            usedPendingOffer = true;
        }

        if (offerToApply is null)
            return;

        var intentId = BuildSyntheticQuestIntentId(npcId, offerToApply.TemplateId, offerToApply.Target, _state.Calendar.Day);
        if (_state.Facts.ProcessedIntents.ContainsKey(intentId))
        {
            _npcPendingFallbackQuestOfferById.TryRemove(npcId, out _);
            return;
        }

        object arguments = offerToApply.RequestedCount > 0
            ? new
            {
                template_id = offerToApply.TemplateId,
                target = offerToApply.Target,
                urgency = offerToApply.Urgency,
                count = offerToApply.RequestedCount
            }
            : new
            {
                template_id = offerToApply.TemplateId,
                target = offerToApply.Target,
                urgency = offerToApply.Urgency
            };

        var payload = JsonSerializer.Serialize(new
        {
            intent_id = intentId,
            npc_id = npcId,
            command = "propose_quest",
            arguments
        });

        if (TryApplyNpcCommandFromLine(payload))
        {
            _npcPendingFallbackQuestOfferById.TryRemove(npcId, out _);
            Monitor.Log(
                $"Applied fallback propose_quest from plain chat text: template={offerToApply.TemplateId} target={offerToApply.Target} urgency={offerToApply.Urgency} count={(offerToApply.RequestedCount > 0 ? offerToApply.RequestedCount.ToString(CultureInfo.InvariantCulture) : "default")} source={(usedPendingOffer ? "pending_offer" : "current_message")}.",
                LogLevel.Warn);
        }
    }

    private static bool TryExtractNpcIdAndMessage(string line, out string npcId, out string message)
    {
        npcId = string.Empty;
        message = string.Empty;

        try
        {
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;
            if (!root.TryGetProperty("npc_id", out var npcEl) || npcEl.ValueKind != JsonValueKind.String)
                return false;
            if (!root.TryGetProperty("message", out var msgEl) || msgEl.ValueKind != JsonValueKind.String)
                return false;

            npcId = npcEl.GetString() ?? string.Empty;
            message = msgEl.GetString() ?? string.Empty;
            return !string.IsNullOrWhiteSpace(npcId) && !string.IsNullOrWhiteSpace(message);
        }
        catch
        {
            return false;
        }
    }

    private static bool LooksLikeQuestDecline(string message)
    {
        var text = message.Trim().ToLowerInvariant();
        return text.Contains("no request", StringComparison.Ordinal)
            || text.Contains("nothing right now", StringComparison.Ordinal)
            || text.Contains("not right now", StringComparison.Ordinal)
            || text.Contains("don't have anything", StringComparison.Ordinal)
            || text.Contains("do not have anything", StringComparison.Ordinal);
    }

    private static bool LooksLikeQuestOffer(string message)
    {
        var text = message.Trim().ToLowerInvariant();
        if (ContainsAny(
                text,
                "could you",
                "can you",
                "would you",
                "please",
                "bring me",
                "bring back",
                "drop off",
                "deliver",
                "gather",
                "collect",
                "harvest",
                "supply",
                "visit",
                "talk to",
                "speak with",
                "check on",
                "check in with",
                "stop by",
                "drop by",
                "swing by",
                "town market",
                "i'll reward",
                "i will reward",
                "reward you",
                "payout"))
        {
            return true;
        }

        if (Regex.IsMatch(text, @"\b(?:bring|gather|collect|deliver|harvest)\s+\d+\b", RegexOptions.CultureInvariant))
            return true;

        return false;
    }

    private bool TryBuildPendingQuestOfferFromMessage(string npcId, string message, out PendingFallbackQuestOffer offer)
    {
        offer = null!;
        if (!LooksLikeQuestOffer(message))
            return false;

        if (!TryInferQuestProposalFromMessage(npcId, message, out var templateId, out var target, out var urgency, out var requestedCount))
            return false;

        offer = new PendingFallbackQuestOffer(templateId, target, urgency, requestedCount, DateTime.UtcNow);
        return true;
    }

    private bool TryGetPendingFallbackQuestOfferForAcceptance(string npcId, string message, out PendingFallbackQuestOffer offer)
    {
        offer = null!;
        if (!_npcPendingFallbackQuestOfferById.TryGetValue(npcId, out var pending))
            return false;

        if (DateTime.UtcNow - pending.OfferedUtc > PendingFallbackQuestOfferMaxAge)
        {
            _npcPendingFallbackQuestOfferById.TryRemove(npcId, out _);
            return false;
        }

        offer = pending;
        return true;
    }

    private static bool IsPlayerAcceptingQuest(string? playerText)
    {
        if (string.IsNullOrWhiteSpace(playerText))
            return false;

        var text = playerText.Trim().ToLowerInvariant();
        return text == "yes"
            || text == "yep"
            || text == "yeah"
            || text == "sure"
            || text == "ok"
            || text == "okay"
            || text.Contains("i can help", StringComparison.Ordinal)
            || text.Contains("i'll help", StringComparison.Ordinal)
            || text.Contains("i will help", StringComparison.Ordinal)
            || text.Contains("count me in", StringComparison.Ordinal)
            || text.Contains("i accept", StringComparison.Ordinal)
            || text.Contains("let's do it", StringComparison.Ordinal)
            || text.Contains("lets do it", StringComparison.Ordinal)
            || text.Contains("i can do that", StringComparison.Ordinal)
            || text.Contains("i'll do it", StringComparison.Ordinal)
            || text.Contains("i will do it", StringComparison.Ordinal)
            || text.Contains("sounds good", StringComparison.Ordinal)
            || text.Contains("i can take it", StringComparison.Ordinal)
            || text.Contains("i'll take it", StringComparison.Ordinal)
            || text.Contains("i will take it", StringComparison.Ordinal);
    }

    private bool TryInferQuestProposalFromMessage(string npcId, string message, out string templateId, out string target, out string urgency, out int requestedCount)
    {
        templateId = string.Empty;
        target = string.Empty;
        urgency = "low";
        requestedCount = 0;

        var text = message.ToLowerInvariant();
        if (ContainsAny(text, "visit", "talk to", "speak with", "check on", "check in with", "stop by", "drop by", "swing by", "go see", "see if"))
        {
            templateId = "social_visit";
            target = TryFindNpcTargetInText(text) ?? GetFallbackVisitTargetForInference(npcId);
            urgency = InferUrgency(text);
            requestedCount = 1;
            return true;
        }

        if (ContainsAny(text, "mine", "mining", "ore", "coal", "quartz", "geode", "stone"))
        {
            templateId = "mine_resource";
            target = TryFindResourceTargetInText(text) ?? "copper_ore";
            urgency = InferUrgency(text);
            requestedCount = TryFindRequestedQuestCountInText(text) ?? 0;
            return true;
        }

        if (ContainsAny(text, "deliver", "bring", "drop off", "supply"))
        {
            templateId = "deliver_item";
            target = TryFindCropTargetInText(text) ?? GetFallbackQuestCropTargetForInference($"{npcId}:{text}");
            urgency = InferUrgency(text);
            requestedCount = TryFindRequestedQuestCountInText(text) ?? 0;
            return true;
        }

        if (ContainsAny(text, "gather", "gathering", "collect", "collecting", "harvest", "harvesting", "pick"))
        {
            templateId = "gather_crop";
            target = TryFindCropTargetInText(text) ?? GetFallbackQuestCropTargetForInference($"{npcId}:{text}");
            urgency = InferUrgency(text);
            requestedCount = TryFindRequestedQuestCountInText(text) ?? 0;
            return true;
        }

        return false;
    }

    private static bool ContainsAny(string text, params string[] terms)
    {
        foreach (var term in terms)
        {
            if (text.Contains(term, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private string? TryFindCropTargetInText(string text)
    {
        var cropCandidates = BuildQuestSupplyCandidateList()
            .OrderByDescending(k => k.Length)
            .ToList();

        foreach (var crop in cropCandidates)
        {
            if (ContainsTargetToken(text, crop))
                return NormalizeTargetToken(crop);
        }

        var match = Regex.Match(
            text,
            @"\b(?:gather|gathering|collect|collecting|harvest|harvesting|pick|deliver|delivering|bring|supply|supplying)\s+(?:some\s+|a\s+|an\s+)?(?:x?\s*\d+\s+)?([a-z_]+)(?:\s*x?\s*\d+)?\b",
            RegexOptions.CultureInvariant);
        if (!match.Success)
            return null;

        return NormalizeTargetToken(match.Groups[1].Value);
    }

    private static string? TryFindResourceTargetInText(string text)
    {
        foreach (var resource in MiningNpcResourcePool)
        {
            if (ContainsTargetToken(text, resource))
                return NormalizeTargetToken(resource);
        }

        return null;
    }

    private string? TryFindNpcTargetInText(string text)
    {
        foreach (var shortName in _player2NpcIdsByShortName.Keys.OrderByDescending(n => n.Length))
        {
            if (ContainsTargetToken(text, shortName))
                return NormalizeTargetToken(shortName);
        }

        if (_config.EnableCustomNpcFramework
            && _customNpcRegistry is not null
            && _customNpcRegistry.TryResolveNpcTokenInText(text, out var customToken))
        {
            return customToken;
        }

        var canonNpcTargets = new[]
        {
            "lewis", "pierre", "robin",
            "abigail", "alex", "caroline", "clint", "demetrius",
            "dwarf", "elliott", "emily", "evelyn", "george", "gil", "gunther",
            "gus", "haley", "harvey", "jas", "jodi", "kent",
            "krobus", "leah", "leo", "linus", "marnie", "marlon", "maru", "morris",
            "pam", "penny", "qi", "sam", "sandy", "sebastian", "shane",
            "vincent", "willy", "wizard"
        };
        foreach (var npc in canonNpcTargets)
        {
            if (ContainsTargetToken(text, npc))
                return npc;
        }

        return null;
    }

    private static bool ContainsTargetToken(string text, string target)
    {
        var normalizedTarget = NormalizeTargetToken(target).Replace("_", " ", StringComparison.Ordinal);
        if (string.IsNullOrWhiteSpace(normalizedTarget))
            return false;

        var escaped = Regex.Escape(normalizedTarget);
        var pattern = normalizedTarget.EndsWith("y", StringComparison.Ordinal) && normalizedTarget.Length > 1
            ? $@"\b(?:{Regex.Escape(normalizedTarget[..^1] + "y")}|{Regex.Escape(normalizedTarget[..^1] + "ies")})\b"
            : $@"\b{escaped}s?\b";
        return Regex.IsMatch(text, pattern, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    }

    private static string NormalizeTargetToken(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        var t = raw.Trim().ToLowerInvariant().Replace(" ", "_", StringComparison.Ordinal);
        t = Regex.Replace(t, @"[^a-z0-9_]+", string.Empty, RegexOptions.CultureInvariant);
        if (t.EndsWith("ies", StringComparison.Ordinal) && t.Length > 3)
            t = t[..^3] + "y";
        else if (t.EndsWith("s", StringComparison.Ordinal) && t.Length > 3)
            t = t[..^1];
        return t;
    }

    private string GetFallbackQuestCropTargetForInference(string? seed = null)
    {
        var ordered = BuildQuestSupplyCandidateList()
            .ToList();

        if (ordered.Count > 0)
        {
            var topCandidates = ordered.Take(Math.Min(8, ordered.Count)).ToList();
            var index = GetDiversifiedFallbackIndex(topCandidates.Count, seed, _state.Calendar.Day);
            return topCandidates[index];
        }

        return "parsnip";
    }

    private string GetFallbackVisitTargetForInference(string? seed = null)
    {
        var candidates = GetExpandedNpcRoster()
            .Select(n => NormalizeTargetToken(n))
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (candidates.Count == 0)
            return "lewis";

        var index = GetDiversifiedFallbackIndex(candidates.Count, seed, _state.Calendar.Day);
        return candidates[index];
    }

    private static int GetDiversifiedFallbackIndex(int count, string? seed, int day)
    {
        if (count <= 1)
            return 0;

        if (!string.IsNullOrWhiteSpace(seed))
        {
            var hash = Math.Abs(seed.GetHashCode());
            return hash % count;
        }

        return Math.Abs(day) % count;
    }

    private static int? TryFindRequestedQuestCountInText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var matches = Regex.Matches(
            text,
            @"\b(?:x\s*)?(\d{1,3})(?:\s*x)?\b",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        foreach (Match match in matches)
        {
            if (!match.Success)
                continue;
            if (!int.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                continue;
            if (parsed <= 0)
                continue;
            if (parsed > 30)
                continue;

            return parsed;
        }

        return null;
    }

    private static string InferUrgency(string text)
    {
        if (ContainsAny(text, "urgent", "asap", "immediately", "right away", "critical", "high demand"))
            return "high";
        if (ContainsAny(text, "soon", "need", "needed", "please", "could use"))
            return "medium";
        return "low";
    }

    private static string BuildSyntheticQuestIntentId(string npcId, string templateId, string target, int day)
    {
        var baseText = $"synth_qchat_{day}_{npcId}_{templateId}_{target}".ToLowerInvariant();
        var sanitized = Regex.Replace(baseText, @"[^a-z0-9_]+", "_", RegexOptions.CultureInvariant).Trim('_');
        return sanitized.Length <= 120 ? sanitized : sanitized[..120];
    }

    private void EnsurePlayer2StreamReadyForChat()
    {
        if (_player2Client is null || string.IsNullOrWhiteSpace(_player2Key))
            return;

        _player2StreamDesired = true;
        if (Interlocked.CompareExchange(ref _player2StreamRunning, 0, 0) == 1)
            return;

        _player2NextReconnectUtc = DateTime.UtcNow;
        _player2StreamBackoffSec = 1;
        StartPlayer2StreamListenerAttempt();
    }

    private bool IsPlayer2StreamReadyForChat()
    {
        if (Interlocked.CompareExchange(ref _player2StreamRunning, 0, 0) == 0)
            return false;
        if (_player2StreamConnectedUtc == default)
            return false;

        // Small guard window so sends don't race stream handler startup.
        return DateTime.UtcNow - _player2StreamConnectedUtc >= TimeSpan.FromMilliseconds(250);
    }

    private void QueuePendingStreamReplay(
        string message,
        string? targetNpcId,
        string? requesterShortName,
        string? senderNameOverride,
        string? contextTag,
        string reason)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        _pendingStreamReplayMessage = message;
        _pendingStreamReplayTargetNpcId = targetNpcId;
        _pendingStreamReplayRequesterShortName = requesterShortName;
        _pendingStreamReplaySenderNameOverride = senderNameOverride;
        _pendingStreamReplayContextTag = contextTag;
        _pendingStreamReplayQueuedUtc = DateTime.UtcNow;
        Monitor.Log($"Queued pending stream replay ({reason}).", LogLevel.Trace);
    }

    private void ClearPendingStreamReplay()
    {
        _pendingStreamReplayMessage = null;
        _pendingStreamReplayTargetNpcId = null;
        _pendingStreamReplayRequesterShortName = null;
        _pendingStreamReplaySenderNameOverride = null;
        _pendingStreamReplayContextTag = null;
        _pendingStreamReplayQueuedUtc = default;
    }

    private void ResetNpcResponseTracking()
    {
        _npcResponseRoutingById.Clear();
        _npcUiPendingById.Clear();
        _npcLastReceivedMessageById.Clear();
        _npcLastNonPlayerMessageById.Clear();
        _npcLastNonPlayerMessageUtcById.Clear();
        _npcLastPlayerPromptById.Clear();
        _npcLastContextTagById.Clear();
        _npcPendingFallbackQuestOfferById.Clear();
    }

    private void StartPlayerChatHistoryFallback(string npcId, NpcHistorySnapshot? previousHistorySnapshot, string playerMessage)
    {
        if (_player2Client is null || string.IsNullOrWhiteSpace(_player2Key) || string.IsNullOrWhiteSpace(npcId))
            return;

        var client = _player2Client;
        var apiBaseUrl = _config.Player2ApiBaseUrl;
        var p2Key = _player2Key!;

        _ = Task.Run(async () =>
        {
            var delivered = false;
            try
            {
                using var totalCts = new CancellationTokenSource(TimeSpan.FromSeconds(18));
                while (!totalCts.Token.IsCancellationRequested)
                {
                    if (_npcResponseRoutingById.TryGetValue(npcId, out var routingQueue)
                        && routingQueue.TryPeek(out var nextRoute)
                        && !nextRoute)
                    {
                        await Task.Delay(250, totalCts.Token);
                        continue;
                    }

                    var snapshot = await client.TryGetLatestNpcHistorySnapshotAsync(apiBaseUrl, p2Key, npcId, totalCts.Token);
                    var latest = snapshot?.LatestMessage?.Trim();
                    if (!string.IsNullOrWhiteSpace(latest))
                    {
                        var hasBaselineHash = !string.IsNullOrWhiteSpace(previousHistorySnapshot?.SnapshotHash);
                        var historyChanged = hasBaselineHash
                            && !string.Equals(snapshot!.SnapshotHash, previousHistorySnapshot!.SnapshotHash, StringComparison.Ordinal);
                        var baselineMissing = !hasBaselineHash;
                        var differsFromSeen = !_npcLastReceivedMessageById.TryGetValue(npcId, out var seen)
                            || !string.Equals(seen, latest, StringComparison.Ordinal);
                        var looksLikePlayerEcho = !string.IsNullOrWhiteSpace(playerMessage)
                            && string.Equals(latest, playerMessage.Trim(), StringComparison.OrdinalIgnoreCase);

                        if (!looksLikePlayerEcho && (historyChanged || (baselineMissing && differsFromSeen)))
                        {
                            var fallbackLine = JsonSerializer.Serialize(new { npc_id = npcId, message = latest });
                            _pendingPlayer2ChatLines.Enqueue(fallbackLine);
                            delivered = true;
                            Monitor.Log("Injected history chat-response fallback for player chat.", LogLevel.Trace);
                            return;
                        }

                    }

                    await Task.Delay(400, totalCts.Token);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Monitor.Log($"History chat-response fallback failed: {ex.Message}", LogLevel.Trace);
            }
            finally
            {
                if (!delivered && _npcUiPendingById.TryGetValue(npcId, out var pending) && pending > 0)
                {
                    _npcUiPendingById.AddOrUpdate(npcId, 0, (_, v) => Math.Max(0, v - 1));
                    Monitor.Log("Player chat history poll timed out with no fresh NPC response.", LogLevel.Debug);
                }
            }
        });
    }

    private static string? TryBuildImmediateNpcResponseLine(string? payload, string npcId)
    {
        var trimmed = payload?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(trimmed);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (TryHasTopLevelCommand(root))
                    return BuildImmediateLineWithNpcId(root, npcId);

                if (TryGetTopLevelImmediateMessage(root, out var topLevelMessage))
                    return JsonSerializer.Serialize(new { npc_id = npcId, message = topLevelMessage });

                if (TryFindFirstCommandObject(root, out var nestedCommand))
                    return BuildImmediateLineWithNpcId(nestedCommand, npcId);

                if (TryExtractLatestImmediateMessage(root, out var extractedMessage))
                    return JsonSerializer.Serialize(new { npc_id = npcId, message = extractedMessage });

                return null;
            }

            if (root.ValueKind == JsonValueKind.Array)
            {
                if (TryFindFirstCommandObject(root, out var nestedCommand))
                    return BuildImmediateLineWithNpcId(nestedCommand, npcId);

                if (TryExtractLatestImmediateMessage(root, out var extractedMessage))
                    return JsonSerializer.Serialize(new { npc_id = npcId, message = extractedMessage });

                return null;
            }

            return JsonSerializer.Serialize(new { npc_id = npcId, message = trimmed });
        }
        catch
        {
            // Some deployments return plain message text from /chat.
            return JsonSerializer.Serialize(new { npc_id = npcId, message = trimmed });
        }
    }

    private static bool TryHasTopLevelCommand(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return false;

        foreach (var prop in element.EnumerateObject())
        {
            if (prop.Name.Equals("command", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static string BuildImmediateLineWithNpcId(JsonElement element, string npcId)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return JsonSerializer.Serialize(new { npc_id = npcId, message = element.ToString() });

        if (element.TryGetProperty("npc_id", out var idEl)
            && idEl.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(idEl.GetString()))
        {
            return element.GetRawText();
        }

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("npc_id", npcId);
            foreach (var prop in element.EnumerateObject())
            {
                if (prop.Name.Equals("npc_id", StringComparison.OrdinalIgnoreCase))
                    continue;

                writer.WritePropertyName(prop.Name);
                prop.Value.WriteTo(writer);
            }
            writer.WriteEndObject();
            writer.Flush();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static bool TryFindFirstCommandObject(JsonElement element, out JsonElement commandObject)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (TryHasTopLevelCommand(element))
            {
                commandObject = element;
                return true;
            }

            foreach (var prop in element.EnumerateObject())
            {
                if (prop.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array
                    && TryFindFirstCommandObject(prop.Value, out commandObject))
                {
                    return true;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                if (item.ValueKind is JsonValueKind.Object or JsonValueKind.Array
                    && TryFindFirstCommandObject(item, out commandObject))
                {
                    return true;
                }
            }
        }

        commandObject = default;
        return false;
    }

    private static bool TryGetTopLevelImmediateMessage(JsonElement element, out string message)
    {
        message = string.Empty;
        if (element.ValueKind != JsonValueKind.Object)
            return false;

        foreach (var prop in element.EnumerateObject())
        {
            if (!IsImmediateMessageProperty(prop.Name) || prop.Value.ValueKind != JsonValueKind.String)
                continue;

            var value = (prop.Value.GetString() ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(value))
                continue;

            message = value;
            return true;
        }

        return false;
    }

    private static bool TryExtractLatestImmediateMessage(JsonElement element, out string message)
    {
        message = string.Empty;
        var candidates = new List<string>();
        CollectImmediateMessageCandidates(element, candidates);
        for (var i = candidates.Count - 1; i >= 0; i--)
        {
            var candidate = candidates[i]?.Trim();
            if (string.IsNullOrWhiteSpace(candidate))
                continue;

            message = candidate;
            return true;
        }

        return false;
    }

    private static void CollectImmediateMessageCandidates(JsonElement element, List<string> candidates)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in element.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.String && IsImmediateMessageProperty(prop.Name))
                    {
                        var value = prop.Value.GetString();
                        if (!string.IsNullOrWhiteSpace(value))
                            candidates.Add(value);
                    }

                    if (prop.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                        CollectImmediateMessageCandidates(prop.Value, candidates);
                }
                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                    CollectImmediateMessageCandidates(item, candidates);
                break;
        }
    }

    private static bool IsImmediateMessageProperty(string propertyName)
    {
        var normalized = propertyName
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();

        return normalized is "message"
            or "text"
            or "response"
            or "assistantmessage"
            or "npcmessage"
            or "output"
            or "outputtext"
            or "content"
            or "player2message";
    }

    private static string? TryExtractNpcIdFromLine(string line)
    {
        try
        {
            using var doc = JsonDocument.Parse(line);
            if (!doc.RootElement.TryGetProperty("npc_id", out var npcEl) || npcEl.ValueKind != JsonValueKind.String)
                return null;

            return npcEl.GetString();
        }
        catch
        {
            return null;
        }
    }

    private static string NormalizePlayerFacingNpcMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return string.Empty;

        var trimmed = message.Trim();
        if (!TryExtractEmbeddedJsonObject(trimmed, out var payloadJson, out var prefix))
            return trimmed;

        try
        {
            using var payloadDoc = JsonDocument.Parse(payloadJson);
            if (payloadDoc.RootElement.ValueKind != JsonValueKind.Object)
                return trimmed;

            var payload = payloadDoc.RootElement;
            var looksLikeCommandPayload =
                payload.TryGetProperty("template_id", out _)
                || payload.TryGetProperty("command", out _)
                || payload.TryGetProperty("arguments", out _)
                || payload.TryGetProperty("player2_message", out _);

            if (!looksLikeCommandPayload)
                return trimmed;

            if (payload.TryGetProperty("player2_message", out var player2MessageEl)
                && player2MessageEl.ValueKind == JsonValueKind.String)
            {
                var player2Message = (player2MessageEl.GetString() ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(player2Message))
                    return player2Message;
            }

            if (!string.IsNullOrWhiteSpace(prefix))
                return prefix;
        }
        catch
        {
            // Keep original message if embedded payload parse fails.
        }

        return string.IsNullOrWhiteSpace(prefix) ? trimmed : prefix;
    }

    private static bool TryExtractEmbeddedJsonObject(string text, out string payloadJson, out string prefix)
    {
        payloadJson = string.Empty;
        prefix = string.Empty;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var trimmed = text.Trim();
        var start = trimmed.IndexOf('{');
        if (start < 0)
            return false;

        var candidate = trimmed[start..].Trim();
        if (!candidate.StartsWith("{", StringComparison.Ordinal) || !candidate.EndsWith("}", StringComparison.Ordinal))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(candidate);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return false;

            payloadJson = candidate;
            prefix = trimmed[..start].Trim();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void QueueNpcPublishHeadlineGeneration(string command, string outcomeId, string? sourceNpcId)
    {
        if (string.IsNullOrWhiteSpace(outcomeId))
            return;

        if (!CanUsePlayer2HeadlineGenerator())
            return;

        var client = _authenticatedPlayer2Client ?? _player2Client;
        if (client is null)
            return;

        var article = FindNpcPublishedArticleForOutcome(command, outcomeId, sourceNpcId);
        if (article is null)
            return;

        var articleTitle = (article.Title ?? string.Empty).Trim();
        var articleCategory = (article.Category ?? string.Empty).Trim();
        var articleContent = (article.Content ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(articleTitle) || string.IsNullOrWhiteSpace(articleContent))
            return;

        var day = article.Day;
        var apiBaseUrl = _config.Player2ApiBaseUrl;
        var player2Key = _player2Key;
        if (string.IsNullOrWhiteSpace(apiBaseUrl) || string.IsNullOrWhiteSpace(player2Key))
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                // Keep timeout bounded so delayed headline generation cannot pile up.
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var generated = await client.TryGenerateSensationalHeadlineAsync(
                    apiBaseUrl,
                    player2Key,
                    articleTitle,
                    articleCategory,
                    articleContent,
                    cts.Token);

                var normalized = NormalizeGeneratedHeadline(generated);
                if (string.IsNullOrWhiteSpace(normalized))
                {
                    Monitor.Log($"Player2 headline empty for {command}; keeping original title.", LogLevel.Trace);
                    return;
                }

                _completedNpcPublishHeadlineUpdates.Enqueue(new NpcPublishHeadlineUpdate
                {
                    Day = day,
                    Command = command,
                    OutcomeId = outcomeId,
                    SourceNpcId = sourceNpcId,
                    Headline = normalized
                });
            }
            catch (Exception ex)
            {
                Monitor.Log($"Player2 sensational headline skipped for {command}: {ex.Message}", LogLevel.Trace);
            }
        });
    }

    private bool CanUsePlayer2HeadlineGenerator()
    {
        if (!_config.EnablePlayer2)
            return false;

        if (string.IsNullOrWhiteSpace(_config.Player2ApiBaseUrl) || string.IsNullOrWhiteSpace(_player2Key))
            return false;

        return _authenticatedPlayer2Client is not null || _player2Client is not null;
    }

    private NewspaperArticle? FindNpcPublishedArticleForOutcome(string command, string outcomeId, string? sourceNpcId)
    {
        var candidates = _state.Newspaper.Articles
            .Where(a =>
                a.Day == _state.Calendar.Day
                && a.ExpirationDay >= _state.Calendar.Day
                && a.IsNpcPublished)
            .ToList();

        if (!string.IsNullOrWhiteSpace(sourceNpcId))
        {
            var sourceShortName = _player2NpcShortNameById.TryGetValue(sourceNpcId, out var shortName)
                ? shortName
                : null;
            candidates = candidates
                .Where(a =>
                    string.Equals(a.SourceNpc, sourceNpcId, StringComparison.OrdinalIgnoreCase)
                    || (!string.IsNullOrWhiteSpace(sourceShortName)
                        && string.Equals(a.SourceNpc, sourceShortName, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        var byTitle = candidates.LastOrDefault(a => string.Equals(a.Title, outcomeId, StringComparison.OrdinalIgnoreCase));
        if (byTitle is not null)
            return byTitle;

        if (command.Equals("publish_rumor", StringComparison.OrdinalIgnoreCase))
        {
            var rumorCandidate = candidates.LastOrDefault(a => a.Category.Equals("social", StringComparison.OrdinalIgnoreCase));
            if (rumorCandidate is not null)
                return rumorCandidate;
        }

        return candidates.LastOrDefault();
    }

    private void TryApplyNpcPublishSourceName(string command, string outcomeId, string? sourceNpcId)
    {
        if (string.IsNullOrWhiteSpace(sourceNpcId))
            return;

        if (!_player2NpcShortNameById.TryGetValue(sourceNpcId, out var sourceShortName)
            || string.IsNullOrWhiteSpace(sourceShortName))
        {
            return;
        }

        var article = FindNpcPublishedArticleForOutcome(command, outcomeId, sourceNpcId);
        if (article is null)
            return;

        article.SourceNpc = ResolvePublishSourceNpcName(sourceShortName);
    }

    private static string ResolvePublishSourceNpcName(string sourceShortName)
    {
        var shortName = (sourceShortName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(shortName))
            return shortName;

        // Preserve vanilla names as-is; only map non-vanilla short names through strict aliases.
        if (Game1.getCharacterFromName(shortName) is not null)
            return shortName;

        if (PublishSourceNpcFallbackMap.TryGetValue(shortName, out var mapped) && !string.IsNullOrWhiteSpace(mapped))
            return mapped;

        return shortName;
    }

    private static string NormalizeGeneratedHeadline(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var headline = value
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Trim()
            .Trim('"')
            .Trim('\'')
            .Trim();

        if (headline.StartsWith("headline:", StringComparison.OrdinalIgnoreCase))
            headline = headline["headline:".Length..].Trim();

        if (headline.StartsWith("<", StringComparison.Ordinal))
        {
            var closing = headline.IndexOf('>');
            if (closing > 0 && closing + 1 < headline.Length)
                headline = headline[(closing + 1)..].Trim();
        }

        return headline;
    }

    private bool TryReplaceTodayIssueWithNpcArticle(string command, string outcomeId, string? headlineOverride = null)
    {
        if (string.IsNullOrWhiteSpace(outcomeId))
            return false;

        var todayIssue = _state.Newspaper.Issues.FirstOrDefault(i => i.Day == _state.Calendar.Day);
        if (todayIssue is null)
            return false;

        var article = _state.Newspaper.Articles
            .LastOrDefault(a =>
                a.Day == _state.Calendar.Day
                && a.ExpirationDay >= _state.Calendar.Day
                && (string.Equals(a.Title, outcomeId, StringComparison.OrdinalIgnoreCase)
                    || (command.Equals("publish_rumor", StringComparison.OrdinalIgnoreCase)
                        && string.Equals(a.Title, $"Rumor: {outcomeId}", StringComparison.OrdinalIgnoreCase))));

        if (article is null)
            return false;

        todayIssue.Articles.Clear();
        todayIssue.Articles.Add(article);
        var cleanHeadlineOverride = (headlineOverride ?? string.Empty).Trim();
        todayIssue.Headline = string.IsNullOrWhiteSpace(cleanHeadlineOverride)
            ? article.Title
            : cleanHeadlineOverride;
        return true;
    }

    private void ShowNewspaperCommandNotification(string command, string outcomeId, string? sourceNpcId)
    {
        if (!Context.IsWorldReady)
            return;

        var sourceName = I18n.Get("hud.newspaper.source.fallback", "Town");
        if (!string.IsNullOrWhiteSpace(sourceNpcId)
            && _player2NpcShortNameById.TryGetValue(sourceNpcId, out var shortName)
            && !string.IsNullOrWhiteSpace(shortName))
        {
            sourceName = ResolvePublishSourceNpcName(shortName);
        }

        string message;
        if (command.Equals("publish_rumor", StringComparison.OrdinalIgnoreCase))
        {
            message = I18n.Get(
                "hud.newspaper.publish_rumor",
                $"{sourceName} spread a rumor. Check today's newspaper.",
                new { source = sourceName });
        }
        else if (command.Equals("publish_article", StringComparison.OrdinalIgnoreCase))
        {
            var title = TrimForHud(outcomeId, 28);
            message = I18n.Get(
                "hud.newspaper.publish_article",
                $"{sourceName} filed a story: {title}",
                new { source = sourceName, title });
        }
        else
        {
            return;
        }

        Game1.addHUDMessage(new HUDMessage(message, HUDMessage.newQuest_type));
    }

    private static string TrimForHud(string text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text))
            return I18n.Get("hud.newspaper.title.fallback", "New article");

        var value = text.Trim();
        if (value.Length <= maxLength)
            return value;

        return value[..Math.Max(1, maxLength - 3)] + "...";
    }

    private void InitializeCustomNpcFramework(IModHelper helper)
    {
        if (!_config.EnableCustomNpcFramework)
        {
            Monitor.Log("Integrated custom-NPC framework is disabled by config.", LogLevel.Info);
            return;
        }

        _customNpcCanonBaselineService = new CanonBaselineService(helper, Monitor);
        _customNpcRegistry = new NpcRegistry();
        _customNpcCanonBaselineService.Load();
        ReloadCustomNpcPacks();
        InjectCustomNpcTargetsIntoRumorBoard("Entry");
    }

    private void ReloadCustomNpcPacks()
    {
        if (!_config.EnableCustomNpcFramework
            || _customNpcCanonBaselineService is null
            || _customNpcRegistry is null)
        {
            return;
        }

        _customNpcPackLoader = new NpcPackLoader(
            Helper,
            Monitor,
            ModManifest.Version.ToString(),
            _customNpcCanonBaselineService,
            ResolveCustomNpcLocale,
            _config.EnableStrictCustomNpcCanonValidation);

        _customNpcLoadedPacks = _customNpcPackLoader.Load(out _customNpcValidationIssues);
        var registryIssues = _customNpcRegistry.BuildFromPacks(_customNpcLoadedPacks);
        if (registryIssues.Count > 0)
            _customNpcValidationIssues = _customNpcValidationIssues.Concat(registryIssues).ToArray();

        Monitor.Log(
            $"Integrated custom-NPC framework loaded packs={_customNpcLoadedPacks.Count}, npcs={_customNpcRegistry.NpcsByToken.Count}.",
            LogLevel.Info);
        LogCustomNpcValidationIssues(verbose: false);
        InjectCustomNpcTargetsIntoRumorBoard("Reload");
    }

    private void LogCustomNpcValidationIssues(bool verbose)
    {
        var errors = _customNpcValidationIssues.Where(i => i.Severity == ValidationSeverity.Error).ToArray();
        var warnings = _customNpcValidationIssues.Where(i => i.Severity == ValidationSeverity.Warning).ToArray();
        Monitor.Log(
            $"Custom-NPC pack validation summary: errors={errors.Length}, warnings={warnings.Length}.",
            errors.Length > 0 ? LogLevel.Warn : LogLevel.Info);

        if (!verbose)
        {
            foreach (var sample in errors.Take(5))
                Monitor.Log($"[ERROR:{sample.Code}] pack={sample.PackId} npc={sample.NpcId} {sample.Message}", LogLevel.Warn);
            foreach (var sample in warnings.Take(5))
                Monitor.Log($"[WARN:{sample.Code}] pack={sample.PackId} npc={sample.NpcId} {sample.Message}", LogLevel.Debug);
            return;
        }

        foreach (var issue in _customNpcValidationIssues
                     .OrderByDescending(i => i.Severity)
                     .ThenBy(i => i.PackId, StringComparer.OrdinalIgnoreCase))
        {
            var level = issue.Severity == ValidationSeverity.Error ? LogLevel.Warn : LogLevel.Info;
            Monitor.Log(
                $"[{issue.Severity}:{issue.Code}] pack={issue.PackId} npc={issue.NpcId} file={issue.SourcePath} -> {issue.Message}",
                level);
        }
    }

    private string ResolveCustomNpcLocale()
    {
        var overrideLocale = (_config.CustomNpcLoreLocaleOverride ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(overrideLocale))
            return NormalizeCustomNpcLocaleCode(overrideLocale);

        try
        {
            var localeProp = Helper.Translation.GetType().GetProperty("Locale", BindingFlags.Public | BindingFlags.Instance);
            var locale = localeProp?.GetValue(Helper.Translation)?.ToString();
            return NormalizeCustomNpcLocaleCode(locale);
        }
        catch
        {
            return "en";
        }
    }

    private static string NormalizeCustomNpcLocaleCode(string? locale)
    {
        if (string.IsNullOrWhiteSpace(locale))
            return "en";
        return locale.Trim().Replace('_', '-').ToLowerInvariant();
    }

    private void InjectCustomNpcTargetsIntoRumorBoard(string source)
    {
        if (!_config.EnableCustomNpcFramework || _customNpcRegistry is null || _customNpcRegistry.NpcsByToken.Count == 0)
            return;

        var rumorBoardType = typeof(RumorBoardService);
        var validNpcTargetsField = rumorBoardType.GetField("ValidNpcTargets", BindingFlags.NonPublic | BindingFlags.Static);
        if (validNpcTargetsField?.GetValue(null) is not HashSet<string> validTargets)
            return;

        var added = 0;
        foreach (var token in _customNpcRegistry.NpcsByToken.Keys)
        {
            if (validTargets.Add(token))
                added++;
        }

        if (added > 0)
            Monitor.Log($"[{source}] Added {added} custom NPC targets to RumorBoardService social_visit validation.", LogLevel.Info);
    }

    private void OnCustomNpcValidatePacksCommand(string command, string[] args)
    {
        LogCustomNpcValidationIssues(verbose: true);
    }

    private void OnCustomNpcListCommand(string command, string[] args)
    {
        if (_customNpcRegistry is null || _customNpcRegistry.NpcsByToken.Count == 0)
        {
            Monitor.Log("No integrated custom NPCs are currently loaded.", LogLevel.Info);
            return;
        }

        Monitor.Log($"Integrated custom NPCs loaded: {_customNpcRegistry.NpcsByToken.Count}", LogLevel.Info);
        foreach (var npc in _customNpcRegistry.NpcsByToken.Values.OrderBy(n => n.DisplayName, StringComparer.OrdinalIgnoreCase))
        {
            Monitor.Log(
                $"- {npc.DisplayName} [{npc.NpcToken}] pack={npc.PackId} modules(quest={npc.Modules.EnableQuestProposals}, rumors={npc.Modules.EnableRumors}, articles={npc.Modules.EnableArticles}, events={npc.Modules.EnableTownEvents})",
                LogLevel.Info);
        }
    }

    private void OnCustomNpcDumpCommand(string command, string[] args)
    {
        if (_customNpcRegistry is null)
            return;

        if (args.Length == 0)
        {
            Monitor.Log("Usage: slrpg_customnpc_dump <npc>", LogLevel.Info);
            return;
        }

        var raw = string.Join(" ", args);
        if (!_customNpcRegistry.TryGetNpcByName(raw, out var npc))
        {
            Monitor.Log($"Integrated custom NPC '{raw}' was not found.", LogLevel.Warn);
            return;
        }

        Monitor.Log(_customNpcRegistry.BuildLoreDebugDump(npc), LogLevel.Info);
        Monitor.Log($"KnownLocations: {string.Join(", ", npc.Lore.KnownLocations)}", LogLevel.Info);
        Monitor.Log($"TimelineAnchors: {string.Join(", ", npc.Lore.TimelineAnchors)}", LogLevel.Info);
        Monitor.Log($"TiesToNpcs: {string.Join(", ", npc.Lore.TiesToNpcs)}", LogLevel.Info);
        Monitor.Log($"ForbiddenClaims: {string.Join(", ", npc.Lore.ForbiddenClaims)}", LogLevel.Info);
    }

    private void OnCustomNpcReloadCommand(string command, string[] args)
    {
        ReloadCustomNpcPacks();
        InjectCustomNpcTargetsIntoRumorBoard("ReloadCommand");
    }
}


