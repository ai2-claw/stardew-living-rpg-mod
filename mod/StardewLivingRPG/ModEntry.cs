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
using System.Threading;

namespace StardewLivingRPG;

public sealed class ModEntry : Mod
{
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

    // Player2 M2 runtime session state
    private Player2Client? _player2Client;
    private string? _player2Key;
    private string? _activeNpcId;
    private readonly ConcurrentQueue<string> _pendingPlayer2Lines = new();
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
    private DateTime _lastUiWorkRequestUtc;
    private int _uiWorkRequestInFlight;
    private readonly Dictionary<string, string> _player2NpcIdsByShortName = new(StringComparer.OrdinalIgnoreCase);

    public override void Entry(IModHelper helper)
    {
        _config = helper.ReadConfig<ModConfig>();
        _dailyTickService = new DailyTickService(Monitor, _config);
        _economyService = new EconomyService();
        _marketBoardService = new MarketBoardService();
        _salesIngestionService = new SalesIngestionService();
        _newspaperService = new NewspaperService();
        _rumorBoardService = new RumorBoardService();
        _intentResolver = new NpcIntentResolver(_rumorBoardService, _config.StrictNpcTemplateValidation);
        _anchorEventService = new AnchorEventService();
        _player2Client = new Player2Client();

        helper.ConsoleCommands.Add("slrpg_sell", "Record simulated crop sale: slrpg_sell <crop> <count>", OnSellCommand);
        helper.ConsoleCommands.Add("slrpg_board", "Print text market board preview.", OnBoardCommand);
        helper.ConsoleCommands.Add("slrpg_open_board", "Open Market Board menu.", OnOpenBoardCommand);
        helper.ConsoleCommands.Add("slrpg_open_news", "Open latest newspaper issue.", OnOpenNewsCommand);
        helper.ConsoleCommands.Add("slrpg_open_rumors", "Open rumor board menu.", OnOpenRumorsCommand);
        helper.ConsoleCommands.Add("slrpg_open_journal", "Open request journal menu.", OnOpenJournalCommand);
        helper.ConsoleCommands.Add("slrpg_accept_quest", "Accept rumor quest: slrpg_accept_quest <questId>", OnAcceptQuestCommand);
        helper.ConsoleCommands.Add("slrpg_quest_progress", "Show active quest progress: slrpg_quest_progress <questId>", OnQuestProgressCommand);
        helper.ConsoleCommands.Add("slrpg_quest_progress_all", "Show progress for all active quests.", OnQuestProgressAllCommand);
        helper.ConsoleCommands.Add("slrpg_complete_quest", "Complete active quest: slrpg_complete_quest <questId>", OnCompleteQuestCommand);
        helper.ConsoleCommands.Add("slrpg_set_sentiment", "Set sentiment: slrpg_set_sentiment economy <value>", OnSetSentimentCommand);
        helper.ConsoleCommands.Add("slrpg_debug_state", "Print compact state snapshot for QA.", OnDebugStateCommand);
        helper.ConsoleCommands.Add("slrpg_intent_inject", "Inject raw NPC intent envelope JSON for resolver QA.", OnIntentInjectCommand);
        helper.ConsoleCommands.Add("slrpg_intent_smoketest", "Run mini automated intent resolver smoke tests.", OnIntentSmokeTestCommand);
        helper.ConsoleCommands.Add("slrpg_anchor_smoketest", "Run deterministic anchor trigger/resolution smoke test.", OnAnchorSmokeTestCommand);
        helper.ConsoleCommands.Add("slrpg_demo_bootstrap", "Seed reproducible vertical-slice scenario.", OnDemoBootstrapCommand);

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
            var issue = _newspaperService.BuildIssue(_state, anchorNote);
            _state.Newspaper.Issues.Add(issue);
        }

        Monitor.Log($"Daily tick complete for day {_state.Calendar.Day} ({_state.Calendar.Season} Y{_state.Calendar.Year}).", LogLevel.Debug);
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        if (Game1.activeClickableMenu is not null)
            return;

        if (e.Button == _config.OpenBoardKey)
            OpenMarketBoard();
        else if (e.Button == _config.OpenNewspaperKey)
            OpenNewspaper();
        else if (e.Button == _config.OpenRumorBoardKey)
            OpenRumorBoard();
        else if (e.Button == _config.OpenRequestJournalKey)
            OpenRequestJournal();
        else if (e.Button == SButton.MouseLeft)
        {
            var point = new Point(Game1.getMouseX(), Game1.getMouseY());
            if (GetPlayer2HudRect().Contains(point))
                StartPlayer2AutoConnect("hud-button", force: true);
        }
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        if (_config.EnablePlayer2 && _config.AutoConnectPlayer2OnLoad)
        {
            var shouldAttempt = string.IsNullOrWhiteSpace(_player2Key) || string.IsNullOrWhiteSpace(_activeNpcId) || !_player2StreamDesired;
            if (shouldAttempt && DateTime.UtcNow - _player2LastAutoConnectAttemptUtc > TimeSpan.FromSeconds(20))
                StartPlayer2AutoConnect("auto-retry", force: false);
        }

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

        if (!string.IsNullOrWhiteSpace(_pendingUiMayorWorkRequest)
            && !string.IsNullOrWhiteSpace(_player2Key)
            && !string.IsNullOrWhiteSpace(_activeNpcId))
        {
            var req = _pendingUiMayorWorkRequest!;
            _pendingUiMayorWorkRequest = null;
            SendPlayer2ChatInternal(req);
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

            Monitor.Log($"Player2 stream line: {line}", LogLevel.Info);
            _player2LastLineUtc = DateTime.UtcNow;
            _player2StreamBackoffSec = 1;
            TryApplyNpcCommandFromLine(line);
        }
    }

    private void OnRenderedHud(object? sender, RenderedHudEventArgs e)
    {
        if (!Context.IsWorldReady || Game1.eventUp || Game1.activeClickableMenu is not null)
            return;

        var rect = GetPlayer2HudRect();
        var connected = !string.IsNullOrWhiteSpace(_player2Key)
            && !string.IsNullOrWhiteSpace(_activeNpcId)
            && (_player2StreamDesired || Interlocked.CompareExchange(ref _player2StreamRunning, 0, 0) == 1);

        var bg = connected ? Color.DarkGreen * 0.75f : Color.DarkRed * 0.75f;
        e.SpriteBatch.Draw(Game1.staminaRect, rect, bg);

        var label = connected ? "Town AI: Connected" : "Town AI: Reconnect";
        var size = Game1.smallFont.MeasureString(label);
        e.SpriteBatch.DrawString(Game1.smallFont, label, new Vector2(rect.X + (rect.Width - size.X) / 2f, rect.Y + 6), Color.White);

        if (!string.IsNullOrWhiteSpace(_player2UiStatus))
        {
            var status = _player2UiStatus.Length > 52 ? _player2UiStatus[..52] + "..." : _player2UiStatus;
            e.SpriteBatch.DrawString(Game1.smallFont, status, new Vector2(rect.X, rect.Bottom + 4), Game1.textColor * 0.85f);
        }
    }

    private Rectangle GetPlayer2HudRect()
    {
        return new Rectangle(16, 16, 220, 30);
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
                && (_player2StreamDesired || Interlocked.CompareExchange(ref _player2StreamRunning, 0, 0) == 1);
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

                if (!string.IsNullOrWhiteSpace(_player2Key) && !_player2StreamDesired)
                    OnPlayer2StreamStartCommand("slrpg_p2_stream_start", Array.Empty<string>());

                var ok = !string.IsNullOrWhiteSpace(_player2Key)
                    && !string.IsNullOrWhiteSpace(_activeNpcId)
                    && (_player2StreamDesired || Interlocked.CompareExchange(ref _player2StreamRunning, 0, 0) == 1);

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

    private void OpenMarketBoard()
    {
        if (_marketBoardService is null)
            return;

        Game1.activeClickableMenu = new MarketBoardMenu(_state, _marketBoardService);
        _state.Telemetry.Daily.MarketBoardOpens += 1;
    }

    private void OpenNewspaper()
    {
        var issue = _state.Newspaper.Issues.LastOrDefault();
        Game1.activeClickableMenu = new NewspaperMenu(issue);
    }

    private void OpenRumorBoard()
    {
        if (_rumorBoardService is null)
            return;

        Game1.activeClickableMenu = new RumorBoardMenu(_state, _rumorBoardService, Monitor, OnUiAskMayorForWork);
    }

    private void OpenRequestJournal()
    {
        if (_rumorBoardService is null)
            return;

        Game1.activeClickableMenu = new RequestJournalMenu(_state, _rumorBoardService, Monitor);
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

        Monitor.Log("=== Pierre's Market Board (text preview) ===", LogLevel.Info);
        foreach (var line in _marketBoardService.BuildTopRows(_state))
            Monitor.Log(line, LogLevel.Info);
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

    private void OnOpenJournalCommand(string name, string[] args)
    {
        if (!Context.IsWorldReady)
            return;

        OpenRequestJournal();
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
            Monitor.Log(result.Message, LogLevel.Info);
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
            Monitor.Log("Player2 login successful (local app).", LogLevel.Info);
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
                    Monitor.Log("Player2 login successful (device flow).", LogLevel.Info);
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
                SystemPrompt = "You are Mayor Lewis from Stardew Valley (Pelican Town). Stay fully in-character as an NPC, not an AI assistant. Tone: warm, practical, brief. Prefer 1-3 short sentences and natural townfolk phrasing. Avoid bullet lists unless explicitly requested. Never say phrases like 'as an AI', 'canon list', 'provided context', or 'feel free to ask'. Strict canon mode: never invent town names, regions, NPCs, or lore. Use only game_state_info facts. If uncertain, say you are unsure in-character. When asked about the market, mention at least one concrete current market signal from game_state_info (movers, oversupply, scarcity, or recommendation). For quest asks, use the propose_quest command with template_id EXACTLY one of [gather_crop, deliver_item, mine_resource, social_visit] (never quest IDs). Use target types by template: gather/deliver=item or crop, mine=resource, social_visit=NPC name. IMPORTANT: do not promise exact gold amounts unless they match REWARD_RULES in game_state_info; prefer wording like modest/solid/high payout band.",
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
                    }
                }
            };

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            _activeNpcId = _player2Client
                .SpawnNpcAsync(_config.Player2ApiBaseUrl, _player2Key!, req, cts.Token)
                .GetAwaiter().GetResult();

            _player2NpcIdsByShortName[req.ShortName] = _activeNpcId;
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

        var roster = (_config.Player2NpcRosterCsv ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var shortName in roster)
        {
            if (string.IsNullOrWhiteSpace(shortName))
                continue;

            if (_player2NpcIdsByShortName.ContainsKey(shortName))
                continue;

            try
            {
                var req = new SpawnNpcRequest
                {
                    ShortName = shortName,
                    Name = shortName,
                    CharacterDescription = $"{shortName} in Pelican Town, practical and grounded.",
                    SystemPrompt = "Stay in-character for Stardew Valley and use safe command schema when proposing town requests.",
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
                        }
                    }
                };

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                var npcId = _player2Client.SpawnNpcAsync(_config.Player2ApiBaseUrl, _player2Key!, req, cts.Token)
                    .GetAwaiter().GetResult();

                _player2NpcIdsByShortName[shortName] = npcId;
                Monitor.Log($"Player2 NPC spawned (roster): {shortName} -> {npcId}", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Monitor.Log($"Player2 additional NPC spawn failed ({shortName}): {ex.Message}", LogLevel.Warn);
            }
        }
    }

    private void SendPlayer2ChatInternal(string message)
    {
        if (_player2Client is null || string.IsNullOrWhiteSpace(_player2Key) || string.IsNullOrWhiteSpace(_activeNpcId))
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

            var req = new NpcChatRequest
            {
                SenderName = Game1.player?.Name ?? "Player",
                SenderMessage = message,
                GameStateInfo = BuildCompactGameStateInfo()
            };

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            _player2Client.SendNpcChatAsync(_config.Player2ApiBaseUrl, _player2Key!, _activeNpcId!, req, cts.Token)
                .GetAwaiter().GetResult();

            Monitor.Log("Sent chat to Player2 NPC. Keep stream listener running to receive response lines.", LogLevel.Info);
        }
        catch (Exception ex)
        {
            Monitor.Log($"Player2 chat failed: {ex.Message}", LogLevel.Error);
        }
    }

    private void OnUiAskMayorForWork()
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
            _player2UiStatus = $"Please wait {wait}s before requesting work again.";
            return;
        }

        Interlocked.Exchange(ref _uiWorkRequestInFlight, 1);

        try
        {
            var requester = GetDefaultNpcRequesterName();
            var prompt = $"{requester}, do you have a practical town request for me today? Use propose_quest with a safe template and parameters.";

            var connected = !string.IsNullOrWhiteSpace(_player2Key)
                && !string.IsNullOrWhiteSpace(_activeNpcId)
                && (_player2StreamDesired || Interlocked.CompareExchange(ref _player2StreamRunning, 0, 0) == 1);

            if (!connected)
            {
                _pendingUiMayorWorkRequest = prompt;
                _player2UiStatus = "Connecting Town AI to request work...";
                _lastUiWorkRequestUtc = DateTime.UtcNow;
                StartPlayer2AutoConnect("ui-ask-work", force: true);
                return;
            }

            SendPlayer2ChatInternal(prompt);
            _lastUiWorkRequestUtc = DateTime.UtcNow;
            _player2UiStatus = $"Asked {requester} for a new town request.";
        }
        finally
        {
            Interlocked.Exchange(ref _uiWorkRequestInFlight, 0);
        }
    }

    private string GetDefaultNpcRequesterName()
    {
        var first = (_config.Player2NpcRosterCsv ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();

        return string.IsNullOrWhiteSpace(first) ? "Mayor Lewis" : first;
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

    private void OnPlayer2StreamStopCommand(string name, string[] args)
    {
        _player2StreamDesired = false;
        _player2StreamCts?.Cancel();
        _player2StreamCts = null;
        Interlocked.Exchange(ref _player2StreamRunning, 0);
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

        Monitor.Log($"P2 health | login={loggedIn} npc={npc} stream={running}/{_player2StreamDesired} joules={joulesText} lastLineAgo={lineAgo} lastCmd={_player2LastCommandApplied} lastCmdAgo={cmdAgo}", LogLevel.Info);
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

        _player2StreamCts?.Cancel();
        _player2StreamCts = new CancellationTokenSource();
        var ct = _player2StreamCts.Token;

        Monitor.Log($"Starting Player2 stream listener… (backoff={_player2StreamBackoffSec}s)", LogLevel.Info);

        _ = Task.Run(async () =>
        {
            try
            {
                await _player2Client.StreamNpcResponsesAsync(_config.Player2ApiBaseUrl, _player2Key!, async line =>
                {
                    _pendingPlayer2Lines.Enqueue(line);
                    await Task.CompletedTask;
                }, ct);

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

                if (_player2StreamDesired)
                {
                    _player2NextReconnectUtc = DateTime.UtcNow.AddSeconds(_player2StreamBackoffSec);
                    _player2StreamBackoffSec = Math.Min(_player2StreamBackoffSec * 2, 30);
                }
            }
        });
    }

    private string BuildCompactGameStateInfo()
    {
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

        return string.Join(" ",
            "CANON_WORLD: Stardew Valley.",
            "CANON_TOWN: Pelican Town.",
            $"CANON_NPCS: [{canonNpcs}].",
            "RULE: Never invent towns, regions, or citizens outside this canon list.",
            "STYLE: Reply strictly in-character as Mayor Lewis, concise, natural, no assistant-speak.",
            "STYLE: Prefer 1-3 short sentences; avoid bullet lists unless explicitly requested.",
            "STYLE: Do not mention 'canon list', 'context', or other meta-AI framing.",
            "RULE: If unsure, say unsure in-character and ask a short follow-up.",
            "MARKET_RULE: For market questions, mention at least one live signal from MARKET_SIGNALS.",
            "REWARD_RULE: Never promise arbitrary gold numbers; follow REWARD_RULES bands.",
            "REWARD_RULES: gather_crop low=350 medium=500 high=700; deliver_item low=360 medium=500 high=650; mine_resource low=450 medium=600 high=800; social_visit low=220 medium=300 high=400.",
            $"STATE: Day {_state.Calendar.Day} {_state.Calendar.Season}.",
            $"STATE: EconomySentiment {_state.Social.TownSentiment.Economy}.",
            $"MARKET_SIGNALS: TopMovers [{string.Join(", ", movers)}]. Oversupply {oversupplyText}. Scarcity {scarcityText}. RecommendedAlternative {recText}.",
            $"STATE: AvailableTownRequests {_state.Quests.Available.Count} ids=[{string.Join(",", availableQuestIds)}].",
            $"STATE: ActiveTownRequests {_state.Quests.Active.Count} ids=[{string.Join(",", activeQuestIds)}]."
        );
    }

    private void TryApplyNpcCommandFromLine(string line)
    {
        try
        {
            if (_intentResolver is null)
                return;

            var result = _intentResolver.ResolveFromStreamLine(_state, line);
            if (!result.HasIntent)
                return;

            if (result.IsRejected)
            {
                _state.Telemetry.Daily.NpcIntentsRejected += 1;
                Monitor.Log($"NPC intent rejected [{result.ReasonCode}]: {result.Reason}", LogLevel.Warn);
                return;
            }

            if (result.IsDuplicate)
            {
                _state.Telemetry.Daily.NpcIntentsDuplicate += 1;
                Monitor.Log($"NPC intent duplicate ignored: {result.IntentId}", LogLevel.Debug);
                return;
            }

            if (!result.AppliedOk)
                return;

            _state.Telemetry.Daily.NpcIntentsApplied += 1;
            _state.Telemetry.Daily.NpcCommandAppliedByType.TryGetValue(result.Command, out var cmdCount);
            _state.Telemetry.Daily.NpcCommandAppliedByType[result.Command] = cmdCount + 1;

            Monitor.Log($"Applied NPC command: {result.Command} -> outcome {result.OutcomeId} (intent={result.IntentId})", LogLevel.Info);

            if (result.Proposal is not null)
            {
                var p = result.Proposal;
                Monitor.Log($"Quest mapping | requested: template={p.RequestedTemplate}, target={p.RequestedTarget}, urgency={p.RequestedUrgency} | applied: template={p.AppliedTemplate}, target={p.AppliedTarget}, urgency={p.AppliedUrgency}, count={p.Count}, reward={p.RewardGold}, expires+{p.ExpiresDelta}d | fallback={result.FallbackUsed}", LogLevel.Info);
            }

            _player2LastCommandApplied = $"{result.Command}:{result.OutcomeId}";
            _player2LastCommandAppliedUtc = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            Monitor.Log($"NPC command parse skipped: {ex.Message}", LogLevel.Trace);
        }
    }
}
