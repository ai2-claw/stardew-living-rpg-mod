using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;
using StardewLivingRPG.Config;
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
    private const int MaxNpcPublishCombinedCharacters = 100;
    private const double DefaultMsPerNpcChatClockStep = 7000d;
    private const double NpcChatClockSlowdownMultiplier = 2d;

    private static readonly MethodInfo? PerformTenMinuteClockUpdateMethod = typeof(Game1).GetMethod(
        "performTenMinuteClockUpdate",
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
        binder: null,
        types: Type.EmptyTypes,
        modifiers: null);
    private static readonly FieldInfo? RealMsPerGameMinuteField = typeof(Game1).GetField(
        "realMilliSecondsPerGameMinute",
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

    private sealed class NpcPublishHeadlineUpdate
    {
        public int Day { get; init; }
        public string Command { get; init; } = string.Empty;
        public string OutcomeId { get; init; } = string.Empty;
        public string? SourceNpcId { get; init; }
        public string Headline { get; init; } = string.Empty;
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

    private ModConfig _config = new();
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
    private NpcSpeechStyleService? _npcSpeechStyleService;

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
    private DateTime _player2LastAutoConnectAttemptUtc;
    private string? _pendingUiMayorWorkRequest;
    private string? _pendingUiRequesterShortName;
    private string? _lastUiWorkPrompt;
    private string? _lastUiWorkRequesterShortName;
    private DateTime _lastUiWorkRequestUtc;
    private int _uiWorkRequestInFlight;
    private int _uiRequesterRoundRobinIndex;
    private readonly Dictionary<string, string> _player2NpcIdsByShortName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _player2NpcShortNameById = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, ConcurrentQueue<string>> _npcUiMessagesById = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, ConcurrentQueue<bool>> _npcResponseRoutingById = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _npcLastReceivedMessageById = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, DateTime> _npcLastPlayerChatRequestUtcById = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _npcLastPlayerPromptById = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, DateTime> _npcLastPlayerQuestAskUtcById = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, int> _npcUiPendingById = new(StringComparer.OrdinalIgnoreCase);
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
    private int _pendingLateNightPassOutDay = -1;
    private string _pendingLateNightPassOutLocation = "Town";
    private readonly Random _ambientNpcRandom = new();
    private int _ambientNpcConversationDay = -1;
    private int _ambientNpcConversationsToday;
    private DateTime _nextAmbientNpcConversationUtc;
    private int _ambientNpcConversationInFlight;

    private string? _pendingNpcDialogueHookName;
    private bool _npcDialogueHookArmed;
    private bool _npcDialogueHookMenuOpened;
    private DateTime _npcDialogueHookArmedUtc;
    private DateTime _npcChatClockLastTickUtc;
    private double _npcChatClockAccumulatorMs;
    private bool _npcChatClockMethodMissingLogged;

    public override void Entry(IModHelper helper)
    {
        _config = helper.ReadConfig<ModConfig>();
        _dailyTickService = new DailyTickService(Monitor, _config);
        _economyService = new EconomyService();
        _marketBoardService = new MarketBoardService();
        _salesIngestionService = new SalesIngestionService();
        _newspaperService = new NewspaperService(Monitor, _player2Client);
        _rumorBoardService = new RumorBoardService();
        _intentResolver = new NpcIntentResolver(_rumorBoardService, _config.StrictNpcTemplateValidation);
        _anchorEventService = new AnchorEventService();
        _npcMemoryService = new NpcMemoryService();
        _townMemoryService = new TownMemoryService();
        var speechStyleConfig = helper.Data.ReadJsonFile<NpcSpeechStyleConfig>("npc_speech_profiles.json")
            ?? NpcSpeechStyleConfig.CreateDefault();
        _npcSpeechStyleService = new NpcSpeechStyleService(speechStyleConfig);
        _player2Client = new Player2Client();

        helper.ConsoleCommands.Add("slrpg_sell", "Record simulated crop sale: slrpg_sell <crop> <count>", OnSellCommand);
        helper.ConsoleCommands.Add("slrpg_board", "Print text market board preview.", OnBoardCommand);
        helper.ConsoleCommands.Add("slrpg_open_board", "Open Market Board menu.", OnOpenBoardCommand);
        helper.ConsoleCommands.Add("slrpg_open_news", "Open latest newspaper issue.", OnOpenNewsCommand);
        helper.ConsoleCommands.Add("slrpg_open_rumors", "Open rumor board menu.", OnOpenRumorsCommand);
        helper.ConsoleCommands.Add("slrpg_accept_quest", "Accept rumor quest: slrpg_accept_quest <questId>", OnAcceptQuestCommand);
        helper.ConsoleCommands.Add("slrpg_quest_progress", "Show active quest progress: slrpg_quest_progress <questId>", OnQuestProgressCommand);
        helper.ConsoleCommands.Add("slrpg_quest_progress_all", "Show progress for all active quests.", OnQuestProgressAllCommand);
        helper.ConsoleCommands.Add("slrpg_complete_quest", "Complete active quest: slrpg_complete_quest <questId>", OnCompleteQuestCommand);
        helper.ConsoleCommands.Add("slrpg_set_sentiment", "Set sentiment: slrpg_set_sentiment economy <value>", OnSetSentimentCommand);
        helper.ConsoleCommands.Add("slrpg_debug_state", "Print compact state snapshot for QA.", OnDebugStateCommand);
        helper.ConsoleCommands.Add("slrpg_intent_inject", "Inject raw NPC intent envelope JSON for resolver QA.", OnIntentInjectCommand);
        helper.ConsoleCommands.Add("slrpg_debug_news_toast", "Inject debug publish_article/publish_rumor intent and trigger HUD toast: slrpg_debug_news_toast <article|rumor> [text]", OnDebugNewsToastCommand);
        helper.ConsoleCommands.Add("slrpg_intent_smoketest", "Run mini automated intent resolver smoke tests.", OnIntentSmokeTestCommand);
        helper.ConsoleCommands.Add("slrpg_anchor_smoketest", "Run deterministic anchor trigger/resolution smoke test.", OnAnchorSmokeTestCommand);
        helper.ConsoleCommands.Add("slrpg_demo_bootstrap", "Seed reproducible vertical-slice scenario.", OnDemoBootstrapCommand);
        helper.ConsoleCommands.Add("slrpg_memory_debug", "Dump NPC memory summary: slrpg_memory_debug <npc>", OnMemoryDebugCommand);
        helper.ConsoleCommands.Add("slrpg_town_memory_dump", "Dump town-memory event count.", OnTownMemoryDumpCommand);
        helper.ConsoleCommands.Add("slrpg_town_memory_npc", "Dump town-memory knowledge for npc: slrpg_town_memory_npc <npc>", OnTownMemoryNpcCommand);

        helper.ConsoleCommands.Add("slrpg_p2_login", "Player2 local app login using configured game client id.", OnPlayer2LoginCommand);
        helper.ConsoleCommands.Add("slrpg_p2_spawn", "Spawn one Player2 NPC session.", OnPlayer2SpawnNpcCommand);
        helper.ConsoleCommands.Add("slrpg_p2_chat", "Send chat to active Player2 NPC: slrpg_p2_chat <message>", OnPlayer2ChatCommand);
        helper.ConsoleCommands.Add("slrpg_p2_read_once", "Read one line from /npcs/responses stream.", OnPlayer2ReadOnceCommand);
        helper.ConsoleCommands.Add("slrpg_p2_read_reset", "Reset/cancel stuck Player2 read_once.", OnPlayer2ReadResetCommand);
        helper.ConsoleCommands.Add("slrpg_p2_stream_start", "Start persistent Player2 response stream listener.", OnPlayer2StreamStartCommand);
        helper.ConsoleCommands.Add("slrpg_p2_stream_stop", "Stop persistent Player2 response stream listener.", OnPlayer2StreamStopCommand);
        helper.ConsoleCommands.Add("slrpg_p2_status", "Show Player2 session + joules + stream status.", OnPlayer2StatusCommand);
        helper.ConsoleCommands.Add("slrpg_p2_health", "Compact Player2 health summary line.", OnPlayer2HealthCommand);

        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        helper.Events.GameLoop.Saving += OnSaving;
        helper.Events.GameLoop.DayEnding += OnDayEnding;
        helper.Events.GameLoop.DayStarted += OnDayStarted;
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        helper.Events.Display.RenderedHud += OnRenderedHud;
        helper.Events.Display.MenuChanged += OnMenuChanged;
        helper.Events.Input.ButtonPressed += OnButtonPressed;

        Monitor.Log("Stardew Living RPG loaded.", LogLevel.Info);
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        _state = StateStore.LoadOrCreate(Helper, Monitor);
        _state.ApplyConfig(_config);
        _economyService?.EnsureInitialized(_state.Economy);
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

        _pendingDayStartStreamRecycleDay = -1;
        TryCapturePendingLateNightPassOut();

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

        var sold = _salesIngestionService?.DrainPendingSales() ?? new Dictionary<string, int>();
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
            if (GetPlayer2HudRect().Contains(point))
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

        var playerTile = Game1.player.Tile;
        var nearbyRequester = loc.characters
            .FirstOrDefault(npc =>
                npc is not null
                && !string.IsNullOrWhiteSpace(npc.Name)
                && IsRosterNpc(npc.Name)
                && Vector2.Distance(npc.Tile, playerTile) <= 2.25f);

        if (nearbyRequester is null)
            return false;

        // Additive hook: don't block vanilla interaction.
        // We arm a follow-up question shown after vanilla dialogue/menu closes.
        _pendingNpcDialogueHookName = nearbyRequester.Name;
        _npcDialogueHookArmed = true;
        _npcDialogueHookMenuOpened = false;
        _npcDialogueHookArmedUtc = DateTime.UtcNow;
        return false;
    }

    private bool IsRosterNpc(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        var roster = GetExpandedNpcRoster();

        return roster.Any(n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase));
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

        return merged;
    }

    private bool TryCreateRosterTalkDialogue(GameLocation loc, NPC npc)
    {
        var name = npc.Name ?? string.Empty;
        if (!IsRosterNpc(name))
            return false;

        var prompt = BuildNpcFollowUpGreeting(npc);
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
                    OpenNpcChatMenu(npc);
            },
            npc);

        return true;
    }

    private void OpenNpcChatMenu(NPC npc)
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
                    SendPlayer2ChatInternal(text, npcIdForChat, npcName);
                else
                    SendPlayer2ChatInternal(text);
            },
            () => string.IsNullOrWhiteSpace(npcIdForChat) ? null : DequeueNpcUiMessage(npcIdForChat),
            () => !string.IsNullOrWhiteSpace(npcIdForChat) && IsNpcThinking(npcIdForChat),
            heartLevel);
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

        if (!_state.NpcMemory.Profiles.TryGetValue(npcName, out var profile))
            return true;

        return profile.RecentTurns.Count == 0 && profile.Facts.Count == 0;
    }

    private string BuildNpcFollowUpGreeting(NPC npc)
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

        if (IsFirstInteractionWithNpc(npcName))
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
        if (TryBuildTownPulseGreeting(out var townGreeting))
            greetings.Add(townGreeting);
        if (TryBuildEconomySignalGreeting(out var economyGreeting))
            greetings.Add(economyGreeting);

        var baseGreeting = profile switch
        {
            NpcVerbalProfile.Professional => isReserved
                ? $"Good {dayPeriod}. Is there something specific you needed?"
                : $"Good {dayPeriod}. Keeping things steady around town today.",
            NpcVerbalProfile.Intellectual => isReserved
                ? $"Good {dayPeriod}. Have you come by for something specific?"
                : $"Good {dayPeriod}. I have been making a few observations around town.",
            NpcVerbalProfile.Enthusiast => isReserved
                ? $"Good {dayPeriod}. Need anything around town?"
                : $"Hey! Good {dayPeriod}! The town feels lively today.",
            NpcVerbalProfile.Recluse => isReserved
                ? $"...{dayPeriod}. What do you need?"
                : $"...{dayPeriod} then. Town is quieter than usual.",
            _ => isReserved
                ? $"Good {dayPeriod}, {playerAddress}. What can I do for you?"
                : $"Good {dayPeriod}, {playerAddress}. How are things on your side?"
        };
        greetings.Add(baseGreeting);

        if (weather == "rain")
        {
            if (isReserved)
                greetings.Add("Rain's coming down steadily today.");
            else
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

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        TryAdvanceClockWhileNpcChatOpen();
        TrackLateNightPassOutWindow();
        TryCaptureTownIncidents();
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
                    SendPlayer2ChatInternal(req, pendingNpcId, requester, contextTag: "player_request_board", captureForPlayerChat: false);
                else
                    SendPlayer2ChatInternal(req, contextTag: "player_request_board", captureForPlayerChat: false);
            }
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
                Monitor.Log("No player chat response line received (timeout/empty).", LogLevel.Warn);
                continue;
            }

            Monitor.Log($"Player2 chat line: {line}", LogLevel.Info);
            CaptureNpcUiMessage(line, allowPlayerChatRouting: true);
            var appliedNpcCommand = TryApplyNpcCommandFromLine(line);
            if (!appliedNpcCommand)
                TryApplyFallbackQuestFromPlayerChatLine(line);
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
                Monitor.Log("No response line received (timeout/empty).", LogLevel.Warn);
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

            Monitor.Log($"Player2 stream line: {line}", LogLevel.Info);
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
            TryApplyNpcCommandFromLine(line);
        }
    }

    private void ResetAmbientNpcConversationScheduleForDay()
    {
        _ambientNpcConversationDay = _state.Calendar.Day;
        _ambientNpcConversationsToday = 0;
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

            var prompt =
                $"{speakerShortName}, you had a brief offscreen conversation with {listenerShortName} about today's town happenings. " +
                "Stay in-character and reply naturally. " +
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
            Monitor.Log($"Ambient NPC conversation triggered: {speakerShortName} -> {listenerShortName}.", LogLevel.Trace);
        }
        finally
        {
            Interlocked.Exchange(ref _ambientNpcConversationInFlight, 0);
            _nextAmbientNpcConversationUtc = DateTime.UtcNow.AddSeconds(_ambientNpcRandom.Next(180, 480));
        }
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

        if (_pendingLateNightPassOutDay != _state.Calendar.Day)
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

        if (!_npcDialogueHookArmed)
            return;

        // Wait until dialogue/menu closes, then append our choice as a follow-up question.
        if (e.NewMenu is not null)
        {
            _npcDialogueHookMenuOpened = true;
            return;
        }

        if (string.IsNullOrWhiteSpace(_pendingNpcDialogueHookName))
        {
            ClearNpcDialogueHook();
            return;
        }

        var requesterName = _pendingNpcDialogueHookName;
        var loc = Game1.currentLocation;
        var npc = loc?.characters?.FirstOrDefault(c => string.Equals(c?.Name, requesterName, StringComparison.OrdinalIgnoreCase));
        ClearNpcDialogueHook();
        if (npc is null)
            return;

        OpenNpcFollowUpDialogue(loc!, npc);
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
        var npc = loc?.characters?.FirstOrDefault(c => string.Equals(c?.Name, requesterName, StringComparison.OrdinalIgnoreCase));
        if (npc is null || Vector2.Distance(npc.Tile, Game1.player.Tile) > 2.5f)
        {
            ClearNpcDialogueHook();
            return;
        }

        ClearNpcDialogueHook();
        OpenNpcFollowUpDialogue(loc!, npc);
    }

    private void OpenNpcFollowUpDialogue(GameLocation loc, NPC npc)
    {
        if (TryCreateRosterTalkDialogue(loc, npc))
            return;

        var responses = new List<Response>();
        if (HasPendingQuestForNpc(npc.Name ?? npc.displayName))
            responses.Add(new Response("town_word", "What's the word around town?"));
        responses.Add(new Response("talk", "Got a minute to chat?"));
        responses.Add(new Response("later", "Catch you later!"));

        loc.createQuestionDialogue(
            $"{npc.displayName}: {BuildNpcFollowUpGreeting(npc)}",
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
                    OpenNpcChatMenu(npc);
            },
            npc);
    }

    private void ClearNpcDialogueHook()
    {
        _pendingNpcDialogueHookName = null;
        _npcDialogueHookArmed = false;
        _npcDialogueHookMenuOpened = false;
        _npcDialogueHookArmedUtc = default;
    }

    private void OnRenderedHud(object? sender, RenderedHudEventArgs e)
    {
        if (!Context.IsWorldReady || Game1.eventUp || Game1.activeClickableMenu is not null)
            return;

        var connected = IsLocalInsightHudActive();

        var label = connected ? "Local Insight: Active" : "Local Insight: Dormant";
        var textSize = Game1.smallFont.MeasureString(label);
        const int paddingX = 12;
        const int paddingY = 6;

        var rect = new Rectangle(
            16,
            16,
            (int)textSize.X + (paddingX * 2),
            (int)textSize.Y + (paddingY * 2));

        var bg = connected ? new Color(116, 81, 46) * 0.95f : new Color(82, 65, 50) * 0.95f;
        e.SpriteBatch.Draw(Game1.staminaRect, rect, bg);

        var textPos = new Vector2(rect.X + paddingX, rect.Y + paddingY);
        e.SpriteBatch.DrawString(Game1.smallFont, label, textPos + new Vector2(2f, 2f), Color.Black * 0.6f);
        e.SpriteBatch.DrawString(Game1.smallFont, label, textPos, connected ? Color.PaleGoldenrod : new Color(180, 160, 130));
    }

    private Rectangle GetPlayer2HudRect()
    {
        var connected = IsLocalInsightHudActive();

        var label = connected ? "Local Insight: Active" : "Local Insight: Dormant";
        var textSize = Game1.smallFont.MeasureString(label);
        return new Rectangle(16, 16, (int)textSize.X + 24, (int)textSize.Y + 12);
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

        if (string.IsNullOrWhiteSpace(_config.Player2GameClientId))
        {
            _player2UiStatus = "Player2 disabled: missing game client id.";
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
            }
            finally
            {
                Interlocked.Exchange(ref _player2ConnectInFlight, 0);
            }
        });
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

        Game1.activeClickableMenu = new MarketBoardMenu(_state);
        _state.Telemetry.Daily.MarketBoardOpens += 1;
    }

    private void OpenNewspaper()
    {
        TryApplyCompletedNewspaperIssues();
        TryRefreshPendingNewspaperIssue("open-newspaper");

        var issue = _state.Newspaper.Issues.LastOrDefault();
        Game1.activeClickableMenu = new NewspaperMenu(issue);
    }

    private void OpenRumorBoard()
    {
        if (_rumorBoardService is null)
            return;

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

        Monitor.Log($"=== Pierre's Market Board — Day {_state.Calendar.Day} ({_state.Calendar.Season}) ===", LogLevel.Info);

        foreach (var (crop, entry) in _state.Economy.Crops.OrderByDescending(kv => kv.Value.TrendEma).Take(8))
        {
            var arrow = entry.PriceToday > entry.PriceYesterday ? "↑" : entry.PriceToday < entry.PriceYesterday ? "↓" : "→";
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
                "unknown command",
                "{\"intent_id\":\"smoke_unk_001\",\"npc_id\":\"lewis\",\"command\":\"launch_rocket\",\"arguments\":{}}",
                "rejected"
            )
        };

        var pass = 0;
        var fail = 0;

        Monitor.Log("Running slrpg_intent_smoketest...", LogLevel.Info);

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

        Monitor.Log($"Intent smoketest complete: pass={pass} fail={fail} total={tests.Length}", fail == 0 ? LogLevel.Info : LogLevel.Warn);
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

        if (!_config.EnablePlayer2)
        {
            Monitor.Log("Player2 integration disabled. Set EnablePlayer2=true in config.json.", LogLevel.Warn);
            return;
        }

        if (string.IsNullOrWhiteSpace(_config.Player2GameClientId))
        {
            Monitor.Log("Missing Player2GameClientId in config.json.", LogLevel.Warn);
            return;
        }

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            _player2Key = _player2Client
                .LoginViaLocalAppAsync(_config.Player2LocalAuthBaseUrl, _config.Player2GameClientId, cts.Token)
                .GetAwaiter().GetResult();

            // CRITICAL: SetCredentials on Player2Client and store authenticated client
            _player2Client.SetCredentials(_config.Player2LocalAuthBaseUrl, _player2Key);
            _authenticatedPlayer2Client = _player2Client;
            Monitor.Log("Player2 login successful (local app).", LogLevel.Info);

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
            using var authCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSec));

            var start = _player2Client
                .StartDeviceAuthAsync(_config.Player2DeviceAuthBaseUrl, _config.Player2GameClientId, authCts.Token)
                .GetAwaiter().GetResult();

            var verifyUrl = start.GetVerificationUrlOrFallback();
            var intervalSec = Math.Clamp(start.IntervalSeconds <= 0 ? 5 : start.IntervalSeconds, 2, 15);

            Monitor.Log($"Player2 device login started. Open: {verifyUrl}", LogLevel.Info);
            Monitor.Log($"Enter code: {start.UserCode} (expires in ~{start.ExpiresIn}s).", LogLevel.Info);
            Monitor.Log("Waiting for device authorization…", LogLevel.Info);

            while (!authCts.IsCancellationRequested)
            {
                Thread.Sleep(TimeSpan.FromSeconds(intervalSec));

                var poll = _player2Client
                    .PollDeviceAuthTokenAsync(_config.Player2DeviceAuthBaseUrl, start.DeviceCode, authCts.Token)
                    .GetAwaiter().GetResult();

                if (poll.IsAuthorized && !string.IsNullOrWhiteSpace(poll.P2Key))
                {
                    _player2Key = poll.P2Key;

                    // CRITICAL: SetCredentials on Player2Client and store authenticated client
                    _player2Client.SetCredentials(_config.Player2DeviceAuthBaseUrl, _player2Key);
                    _authenticatedPlayer2Client = _player2Client;
                    Monitor.Log("Player2 login successful (device flow).", LogLevel.Info);

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
                    Monitor.Log($"Player2 device login failed: {poll.Status} {poll.ErrorMessage}".Trim(), LogLevel.Error);
                    return;
                }
            }

            Monitor.Log("Player2 device login timed out waiting for authorization.", LogLevel.Error);
        }
        catch (Exception ex)
        {
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
            var req = new SpawnNpcRequest
            {
                ShortName = "Lewis",
                Name = "Mayor Lewis",
                CharacterDescription = "Mayor Lewis of Pelican Town in Stardew Valley. Canon-grounded, practical, cooperative, and non-fabricating.",
                SystemPrompt = "You are Mayor Lewis from Stardew Valley (Pelican Town). Stay fully in-character as an NPC, not an AI assistant. Tone: warm, practical, brief. Prefer 1-3 short sentences and natural townfolk phrasing. Avoid bullet lists unless explicitly requested. Never say phrases like 'as an AI', 'canon list', 'provided context', or 'feel free to ask'. Strict canon mode: never invent town names, regions, NPCs, or lore. Use only game_state_info facts. If uncertain, say you are unsure in-character. When asked about the market, mention at least one concrete current market signal from game_state_info (movers, oversupply, scarcity, or recommendation). For quest asks, use the propose_quest command with template_id EXACTLY one of [gather_crop, deliver_item, mine_resource, social_visit] (never quest IDs). Use target types by template: gather/deliver=item or crop, mine=resource, social_visit=NPC name. Never offer or describe a concrete player task without emitting propose_quest in the same reply. If no suitable request exists, say so in-character and do not invent a task. For publish_article and publish_rumor, keep title+content within 100 characters total. IMPORTANT: do not promise exact gold amounts unless they match REWARD_RULES in game_state_info; prefer wording like modest/solid/high payout band.",
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
                                urgency = new { type = "string", @enum = new[] { "low", "medium", "high" } }
                            },
                            required = new[] { "template_id", "target", "urgency" },
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

        var roster = GetExpandedNpcRoster();

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
                    _ => $"You are {shortName} of Pelican Town in Stardew Valley. Never claim to be another NPC."
                };

                var req = new SpawnNpcRequest
                {
                    ShortName = shortName,
                    Name = shortName,
                    CharacterDescription = $"{shortName} in Pelican Town, practical and grounded.",
                    SystemPrompt = identityPrompt + " Stay in-character, grounded in Stardew canon. Never impersonate another NPC. For quest asks, and whenever you offer a task/request, you must emit propose_quest with template_id EXACTLY one of [gather_crop, deliver_item, mine_resource, social_visit] and valid target/urgency. Never give a text-only task offer without propose_quest in the same reply. If no suitable request exists, say no request is available in-character. For publish_article and publish_rumor, keep title+content within 100 characters total.",
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
                                    urgency = new { type = "string", @enum = new[] { "low", "medium", "high" } }
                                },
                                required = new[] { "template_id", "target", "urgency" },
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

            Monitor.Log($"Sent chat to Player2 NPC ({who}) id={npcId}. Keep stream listener running to receive response lines.", LogLevel.Info);
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
            var playerAskedForQuest = IsPlayerAskingForQuest(message);
            if (string.IsNullOrWhiteSpace(effectiveContextTag) && playerAskedForQuest)
                effectiveContextTag = "player_chat_quest_request";
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

            Monitor.Log($"Sent player chat via per-message flow to Player2 NPC ({who}) id={npcId}.", LogLevel.Info);
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

        Monitor.Log("Reading one Player2 stream line in background…", LogLevel.Info);
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

            if (TryReplaceTodayIssueWithNpcArticle(update.Command, update.OutcomeId, update.Headline))
            {
                Monitor.Log($"Applied queued Player2 sensational headline for {update.Command}: {update.Headline}", LogLevel.Trace);
                continue;
            }

            var existingIndex = _pendingNpcPublishHeadlineUpdates.FindIndex(pending =>
                IsSameNpcPublishHeadlineTarget(pending, update));
            if (existingIndex >= 0)
                _pendingNpcPublishHeadlineUpdates[existingIndex] = update;
            else
                _pendingNpcPublishHeadlineUpdates.Add(update);
        }

        for (var i = _pendingNpcPublishHeadlineUpdates.Count - 1; i >= 0; i--)
        {
            var pending = _pendingNpcPublishHeadlineUpdates[i];
            if (pending.Day != _state.Calendar.Day)
            {
                _pendingNpcPublishHeadlineUpdates.RemoveAt(i);
                continue;
            }

            if (!TryReplaceTodayIssueWithNpcArticle(pending.Command, pending.OutcomeId, pending.Headline))
                continue;

            Monitor.Log($"Applied deferred Player2 sensational headline for {pending.Command}: {pending.Headline}", LogLevel.Trace);
            _pendingNpcPublishHeadlineUpdates.RemoveAt(i);
        }
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
        var message = $"Morning edition ready: {headline}";
        Game1.addHUDMessage(new HUDMessage(message, HUDMessage.newQuest_type));
    }

    private static SaveState CloneSaveState(SaveState state)
    {
        var json = JsonSerializer.Serialize(state);
        return JsonSerializer.Deserialize<SaveState>(json) ?? SaveState.CreateDefault();
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

            var routeToPlayerChat = allowPlayerChatRouting
                && _npcUiPendingById.TryGetValue(npcId, out var pending)
                && pending > 0;
            if (_npcResponseRoutingById.TryGetValue(npcId, out var routingQueue) && routingQueue.TryDequeue(out var routed))
                routeToPlayerChat = allowPlayerChatRouting && routed;

            if (routeToPlayerChat)
            {
                _npcUiPendingById.AddOrUpdate(npcId, 0, (_, v) => Math.Max(0, v - 1));
            }

            if (!root.TryGetProperty("message", out var msgEl) || msgEl.ValueKind != JsonValueKind.String)
                return routeToPlayerChat;

            var msg = msgEl.GetString();
            if (string.IsNullOrWhiteSpace(msg))
                return routeToPlayerChat;
            var playerFacingMsg = NormalizePlayerFacingNpcMessage(msg);
            if (string.IsNullOrWhiteSpace(playerFacingMsg))
                playerFacingMsg = msg.Trim();

            if (routeToPlayerChat)
            {
                playerFacingMsg = NormalizeNpcTimeReply(npcId, playerFacingMsg);
                var q = _npcUiMessagesById.GetOrAdd(npcId, _ => new ConcurrentQueue<string>());
                q.Enqueue(playerFacingMsg);
                _npcLastReceivedMessageById[npcId] = msg;
            }

            if (_npcMemoryService is not null && routeToPlayerChat)
            {
                var npcName = GetNpcShortNameById(npcId);
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

    private string? DequeueNpcUiMessage(string npcId)
    {
        if (!_npcUiMessagesById.TryGetValue(npcId, out var q))
            return null;

        return q.TryDequeue(out var msg) ? msg : null;
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

        Monitor.Log($"Starting Player2 stream listener… (backoff={_player2StreamBackoffSec}s)", LogLevel.Info);

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
        var weather = GetCurrentWeatherLabel();
        var dayOfWeek = GetCurrentDayOfWeekLabel();
        var timeOfDay = GetCurrentTimeOfDayLabel(out var hour24, out var minute);
        var heartLevel = GetNpcHeartLevel(npcName);
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

        var canonNpcs = "Lewis, Robin, Pierre, Linus, Haley, Alex, Demetrius, Wizard";
        var activeQuestIds = _state.Quests.Active.Take(3).Select(q => q.QuestId).ToArray();
        var availableQuestIds = _state.Quests.Available.Take(3).Select(q => q.QuestId).ToArray();

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
        var eventsContext = BuildRecentEventAwarenessBlock();
        var playerAskedForRequest = IsPlayerAskingForQuest(playerText);
        var parsnipCrisis = IsParsnipQuestCrisisActive();
        if (!string.IsNullOrWhiteSpace(npcName))
        {
            if (_npcMemoryService is not null)
                npcMemory = _npcMemoryService.BuildMemoryBlock(_state, npcName, playerText ?? string.Empty, _state.Calendar.Day);
            if (_townMemoryService is not null)
                townMemory = _townMemoryService.BuildTownMemoryBlock(_state, npcName, playerText ?? string.Empty, _state.Calendar.Day);
        }

        return string.Join(" ",
            "CANON_WORLD: Stardew Valley.",
            "CANON_TOWN: Pelican Town.",
            $"CANON_NPCS: [{canonNpcs}].",
            $"CONTEXT: {(string.IsNullOrWhiteSpace(contextTag) ? "player_chat" : contextTag)}.",
            "RULE: Never invent towns, regions, or citizens outside this canon list.",
            $"STYLE: Reply strictly in-character as {(string.IsNullOrWhiteSpace(npcName) ? "the addressed NPC" : npcName)}, concise, natural, no assistant-speak.",
            "STYLE: Prefer 1-3 short sentences; avoid bullet lists unless explicitly requested.",
            "STYLE: Do not mention 'canon list', 'context', or other meta-AI framing.",
            "RULE: If unsure, say unsure in-character and ask a short follow-up.",
            speechStyleBlock,
            "RELATIONSHIP_RULE: Match familiarity to STATE: RelationshipHearts. At 0-2 hearts, keep distance and avoid affectionate language.",
            "PLAYER_NAME_RULE: Use PLAYER_KNOWLEDGE to decide how to address the player. If NpcHasMetPlayer is false, do not call the player by name.",
            "TIME_RULE: If asked for time, answer with hour and minute plus AM/PM (for example: 6:30 AM). Never answer with minutes only.",
            "QUEST_RULE: If you offer or describe a concrete task/request/quest, include propose_quest in the same reply.",
            "QUEST_RULE: Never give text-only task offers without propose_quest.",
            "QUEST_RULE: If no suitable request exists, explicitly say none is available in-character.",
            "QUEST_RULE: Do not proactively offer a new quest during normal small talk. Offer quests only when the player asks for work/request or explicitly agrees to help.",
            "QUEST_TARGET_RULE: Do not default to parsnip. Use parsnip only when STATE: ParsnipCrisis is true; otherwise choose another valid crop target or decline.",
            playerAskedForRequest
                ? "QUEST_CONTEXT: Player explicitly asked for work/request now. You must either emit propose_quest or decline clearly; no text-only task offers."
                : string.Empty,
            "NEWS_RULE: If asked about news, rumors, or recent events, answer using NEWS_CONTEXT and RECENT_EVENTS first.",
            "RULE: For publish_article/publish_rumor commands, keep title+content within 100 characters total.",
            "MARKET_RULE: For market questions, mention at least one live signal from MARKET_SIGNALS.",
            "REWARD_RULE: Never promise arbitrary gold numbers; follow REWARD_RULES bands.",
            "REWARD_RULES: Rewards are dynamic from target value x count with urgency bands (low=modest, medium=solid, high=premium). Social visits stay in a small fixed band.",
            $"STATE: CurrentSeason {_state.Calendar.Season}.",
            $"STATE: CurrentWeather {weather}.",
            $"STATE: CurrentDayOfWeek {dayOfWeek}.",
            $"STATE: CurrentTimeOfDay {timeOfDay}.",
            $"STATE: RelationshipHearts {(string.IsNullOrWhiteSpace(npcName) ? 0 : heartLevel)}.",
            $"STATE: CurrentHour24 {hour24:00}.",
            $"STATE: CurrentMinute {minute:00}.",
            $"STATE: ParsnipCrisis {parsnipCrisis}.",
            $"PLAYER_KNOWLEDGE: PlayerName='{playerName}' NpcHasMetPlayer={npcHasMetPlayer} PreferredAddress='{preferredAddress}'.",
            $"STATE: PlayerStats Charisma={charismaStat} Social={socialStat}.",
            $"STATE: Day {_state.Calendar.Day} {_state.Calendar.Season}.",
            $"STATE: EconomySentiment {_state.Social.TownSentiment.Economy}.",
            $"MARKET_SIGNALS: TopMovers [{string.Join(", ", movers)}]. Oversupply {oversupplyText}. Scarcity {scarcityText}. RecommendedAlternative {recText}.",
            newsContext,
            eventsContext,
            $"STATE: AvailableTownRequests {_state.Quests.Available.Count} ids=[{string.Join(",", availableQuestIds)}].",
            $"STATE: ActiveTownRequests {_state.Quests.Active.Count} ids=[{string.Join(",", activeQuestIds)}].",
            npcMemory,
            townMemory
        );
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

    private string BuildRecentEventAwarenessBlock()
    {
        var day = _state.Calendar.Day;
        var events = _state.TownMemory.Events
            .Where(ev =>
                ev.Day <= day
                && ev.Day >= day - 3
                && !string.IsNullOrWhiteSpace(ev.Summary))
            .OrderByDescending(ev => ev.Day)
            .ThenByDescending(ev => ev.Severity)
            .Take(3)
            .Select(ev => $"{ev.Kind}:{TrimForContext(ev.Summary, 44, "event")}")
            .ToArray();

        return $"RECENT_EVENTS: [{JoinContextItems(events)}].";
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

            var result = _intentResolver.ResolveFromStreamLine(_state, line);
            if (!result.HasIntent)
                return false;

            if (result.IsRejected)
            {
                _state.Telemetry.Daily.NpcIntentsRejected += 1;
                Monitor.Log($"NPC intent rejected [{result.ReasonCode}]: {result.Reason}", LogLevel.Warn);
                return false;
            }

            if (result.IsDuplicate)
            {
                _state.Telemetry.Daily.NpcIntentsDuplicate += 1;
                Monitor.Log($"NPC intent duplicate ignored: {result.IntentId}", LogLevel.Debug);
                return false;
            }

            if (!result.AppliedOk)
                return false;

            _state.Telemetry.Daily.NpcIntentsApplied += 1;
            _state.Telemetry.Daily.NpcCommandAppliedByType.TryGetValue(result.Command, out var cmdCount);
            _state.Telemetry.Daily.NpcCommandAppliedByType[result.Command] = cmdCount + 1;

            Monitor.Log($"Applied NPC command: {result.Command} -> outcome {result.OutcomeId} (intent={result.IntentId})", LogLevel.Info);

            if (result.Proposal is not null)
            {
                var p = result.Proposal;
                Monitor.Log($"Quest mapping | requested: template={p.RequestedTemplate}, target={p.RequestedTarget}, urgency={p.RequestedUrgency} | applied: template={p.AppliedTemplate}, target={p.AppliedTarget}, urgency={p.AppliedUrgency}, count={p.Count}, reward={p.RewardGold}, expires+{p.ExpiresDelta}d | fallback={result.FallbackUsed}", LogLevel.Info);

                if (!string.IsNullOrWhiteSpace(sourceNpcId)
                    && _player2NpcShortNameById.TryGetValue(sourceNpcId, out var issuerShortName))
                {
                    var q = _state.Quests.Available.FirstOrDefault(x => x.QuestId.Equals(result.OutcomeId, StringComparison.OrdinalIgnoreCase));
                    if (q is not null)
                        q.Issuer = issuerShortName.ToLowerInvariant();
                }
            }

            _player2LastCommandApplied = $"{result.Command}:{result.OutcomeId}";
            _player2LastCommandAppliedUtc = DateTime.UtcNow;

            if (result.Command.Equals("publish_rumor", StringComparison.OrdinalIgnoreCase)
                || result.Command.Equals("publish_article", StringComparison.OrdinalIgnoreCase))
            {
                var outcomeId = result.OutcomeId;
                TryApplyNpcPublishSourceName(result.Command, outcomeId, sourceNpcId);
                outcomeId = TryClampNpcPublishArticleAsLastResort(result.Command, outcomeId, sourceNpcId);
                QueueNpcPublishHeadlineGeneration(result.Command, outcomeId, sourceNpcId);
                ShowNewspaperCommandNotification(result.Command, outcomeId, sourceNpcId);

                // Avoid rebuilding today's issue after it has already been generated, because rebuilds can
                // replace dynamic editor stories with fallback fillers. Replace today's visible content/headline
                // with the latest NPC article directly when possible.
                if (!TryReplaceTodayIssueWithNpcArticle(result.Command, outcomeId))
                {
                    var hasTodayIssue = _state.Newspaper.Issues.Any(i => i.Day == _state.Calendar.Day);
                    if (!hasTodayIssue)
                    {
                        _pendingNewspaperRefreshDay = _state.Calendar.Day;
                        TryRefreshPendingNewspaperIssue($"npc-command:{result.Command}");
                    }
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Monitor.Log($"NPC command parse skipped: {ex.Message}", LogLevel.Trace);
            return false;
        }
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

        var playerAskedForQuestRecently = _npcLastPlayerQuestAskUtcById.TryGetValue(npcId, out var questAskUtc)
            && DateTime.UtcNow - questAskUtc <= TimeSpan.FromSeconds(60);

        _npcLastPlayerPromptById.TryGetValue(npcId, out var lastPlayerPrompt);
        var playerAcceptedQuest = IsPlayerAcceptingQuest(lastPlayerPrompt);

        // Only synthesize propose_quest when the player either asked for work or explicitly accepted.
        if (!playerAskedForQuestRecently && !playerAcceptedQuest)
            return;

        if (!LooksLikeQuestOffer(message))
            return;

        if (LooksLikeQuestDecline(message))
            return;

        if (!TryInferQuestProposalFromMessage(message, out var templateId, out var target, out var urgency))
            return;

        var intentId = BuildSyntheticQuestIntentId(npcId, templateId, target, _state.Calendar.Day);
        if (_state.Facts.ProcessedIntents.ContainsKey(intentId))
            return;

        var payload = JsonSerializer.Serialize(new
        {
            intent_id = intentId,
            npc_id = npcId,
            command = "propose_quest",
            arguments = new
            {
                template_id = templateId,
                target,
                urgency
            }
        });

        if (TryApplyNpcCommandFromLine(payload))
        {
            Monitor.Log(
                $"Applied fallback propose_quest from plain chat text: template={templateId} target={target} urgency={urgency}.",
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

    private bool TryInferQuestProposalFromMessage(string message, out string templateId, out string target, out string urgency)
    {
        templateId = string.Empty;
        target = string.Empty;
        urgency = "low";

        var text = message.ToLowerInvariant();
        if (ContainsAny(text, "visit", "talk to", "speak with", "check on"))
        {
            templateId = "social_visit";
            target = TryFindNpcTargetInText(text) ?? "lewis";
            urgency = InferUrgency(text);
            return true;
        }

        if (ContainsAny(text, "mine", "mining", "ore", "coal", "quartz", "geode", "stone"))
        {
            templateId = "mine_resource";
            target = TryFindResourceTargetInText(text) ?? "copper_ore";
            urgency = InferUrgency(text);
            return true;
        }

        if (ContainsAny(text, "deliver", "bring", "drop off", "supply"))
        {
            templateId = "deliver_item";
            target = TryFindCropTargetInText(text) ?? GetFallbackQuestCropTargetForInference();
            urgency = InferUrgency(text);
            return true;
        }

        if (ContainsAny(text, "gather", "gathering", "collect", "collecting", "harvest", "harvesting", "pick"))
        {
            templateId = "gather_crop";
            target = TryFindCropTargetInText(text) ?? GetFallbackQuestCropTargetForInference();
            urgency = InferUrgency(text);
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
        var cropCandidates = _state.Economy.Crops.Keys
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .OrderByDescending(k => k.Length)
            .ToList();

        foreach (var crop in cropCandidates)
        {
            if (ContainsTargetToken(text, crop))
                return NormalizeTargetToken(crop);
        }

        var match = Regex.Match(
            text,
            @"\b(?:gather|gathering|collect|collecting|harvest|harvesting|pick|deliver|delivering|bring|supply|supplying)\s+(?:some\s+|a\s+|an\s+)?([a-z_]+)",
            RegexOptions.CultureInvariant);
        if (!match.Success)
            return null;

        return NormalizeTargetToken(match.Groups[1].Value);
    }

    private static string? TryFindResourceTargetInText(string text)
    {
        var resources = new[]
        {
            "copper_ore", "iron_ore", "gold_ore", "coal", "quartz", "stone"
        };

        foreach (var resource in resources)
        {
            if (ContainsTargetToken(text, resource))
                return resource;
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

        var canonNpcTargets = new[]
        {
            "lewis", "robin", "pierre", "linus", "haley", "alex", "demetrius", "wizard", "elliott"
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

        var pattern = $@"\b{Regex.Escape(normalizedTarget)}s?\b";
        return Regex.IsMatch(text, pattern, RegexOptions.CultureInvariant);
    }

    private static string NormalizeTargetToken(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        var t = raw.Trim().ToLowerInvariant().Replace(" ", "_", StringComparison.Ordinal);
        t = Regex.Replace(t, @"[^a-z0-9_]+", string.Empty, RegexOptions.CultureInvariant);
        if (t.EndsWith("s", StringComparison.Ordinal) && t.Length > 3)
            t = t[..^1];
        return t;
    }

    private string GetFallbackQuestCropTargetForInference()
    {
        var allowParsnip = IsParsnipQuestCrisisActive();
        var ordered = _state.Economy.Crops
            .Where(kv => !string.IsNullOrWhiteSpace(kv.Key))
            .OrderByDescending(kv => kv.Value.ScarcityBonus)
            .ThenByDescending(kv => kv.Value.DemandFactor)
            .ThenByDescending(kv => kv.Value.SupplyPressureFactor)
            .Select(kv => kv.Key.Trim().ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!allowParsnip)
            ordered = ordered.Where(c => !c.Equals("parsnip", StringComparison.OrdinalIgnoreCase)).ToList();

        var best = ordered.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(best))
            return best;

        var season = (_state.Calendar.Season ?? string.Empty).Trim().ToLowerInvariant();
        if (allowParsnip)
        {
            return season switch
            {
                "spring" => "parsnip",
                "summer" => "tomato",
                "fall" => "pumpkin",
                _ => "wheat"
            };
        }

        return season switch
        {
            "spring" => "potato",
            "summer" => "tomato",
            "fall" => "pumpkin",
            _ => "wheat"
        };
    }

    private bool IsParsnipQuestCrisisActive()
    {
        if (!_state.Facts.Facts.TryGetValue("anchor:town_hall_crisis:status:triggered", out var triggered) || !triggered.Value)
            return false;

        if (_state.Facts.Facts.TryGetValue("anchor:town_hall_crisis:status:resolved", out var resolved) && resolved.Value)
            return false;

        if (!_state.Economy.Crops.TryGetValue("parsnip", out var parsnip))
            return false;

        var topByScarcity = _state.Economy.Crops
            .OrderByDescending(kv => kv.Value.ScarcityBonus)
            .ThenByDescending(kv => kv.Value.DemandFactor)
            .Select(kv => kv.Key)
            .FirstOrDefault();

        if (!string.Equals(topByScarcity, "parsnip", StringComparison.OrdinalIgnoreCase))
            return false;

        var secondScarcity = _state.Economy.Crops
            .Where(kv => !kv.Key.Equals("parsnip", StringComparison.OrdinalIgnoreCase))
            .Select(kv => kv.Value.ScarcityBonus)
            .DefaultIfEmpty(0f)
            .Max();

        var scarcityLead = parsnip.ScarcityBonus - secondScarcity;
        return parsnip.DemandFactor >= 1.04f
            && parsnip.ScarcityBonus >= 0.04f
            && scarcityLead >= 0.01f;
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
        _npcLastPlayerPromptById.Clear();
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
                    Monitor.Log("Player chat history poll timed out with no fresh NPC response.", LogLevel.Warn);
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

    private string TryClampNpcPublishArticleAsLastResort(string command, string outcomeId, string? sourceNpcId)
    {
        var article = FindNpcPublishedArticleForOutcome(command, outcomeId, sourceNpcId);
        if (article is null)
            return outcomeId;

        if (!TryClampNpcPublishArticleInPlace(article))
            return article.Title;

        Monitor.Log(
            $"Applied fallback clamp for {command} to keep title+content <= {MaxNpcPublishCombinedCharacters}: '{article.Title}'",
            LogLevel.Trace);

        return article.Title;
    }

    private static bool TryClampNpcPublishArticleInPlace(NewspaperArticle article)
    {
        var title = (article.Title ?? string.Empty).Trim();
        var content = (article.Content ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
            return false;

        if (title.Length + content.Length <= MaxNpcPublishCombinedCharacters)
        {
            article.Title = title;
            article.Content = content;
            return false;
        }

        var maxTitleLength = Math.Max(1, MaxNpcPublishCombinedCharacters - 1);
        if (title.Length > maxTitleLength)
            title = title[..maxTitleLength].TrimEnd();

        if (string.IsNullOrWhiteSpace(title))
            return false;

        var maxContentLength = MaxNpcPublishCombinedCharacters - title.Length;
        if (maxContentLength <= 0)
        {
            title = title[..Math.Max(1, MaxNpcPublishCombinedCharacters - 1)].TrimEnd();
            maxContentLength = MaxNpcPublishCombinedCharacters - title.Length;
        }

        if (maxContentLength <= 0)
            return false;

        if (content.Length > maxContentLength)
            content = content[..maxContentLength].TrimEnd();

        if (string.IsNullOrWhiteSpace(content))
            return false;

        var changed = !string.Equals(article.Title, title, StringComparison.Ordinal)
            || !string.Equals(article.Content, content, StringComparison.Ordinal);

        article.Title = title;
        article.Content = content;
        return changed;
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

        var sourceName = "Town";
        if (!string.IsNullOrWhiteSpace(sourceNpcId)
            && _player2NpcShortNameById.TryGetValue(sourceNpcId, out var shortName)
            && !string.IsNullOrWhiteSpace(shortName))
        {
            sourceName = ResolvePublishSourceNpcName(shortName);
        }

        string message;
        if (command.Equals("publish_rumor", StringComparison.OrdinalIgnoreCase))
        {
            message = $"{sourceName} spread a rumor. Check today's newspaper.";
        }
        else if (command.Equals("publish_article", StringComparison.OrdinalIgnoreCase))
        {
            message = $"{sourceName} filed a story: {TrimForHud(outcomeId, 28)}";
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
            return "New article";

        var value = text.Trim();
        if (value.Length <= maxLength)
            return value;

        return value[..Math.Max(1, maxLength - 3)] + "...";
    }
}
