using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
using StardewLivingRPG.State;
using StardewLivingRPG.Systems;
using StardewLivingRPG.Utils;

namespace StardewLivingRPG.UI;

public sealed class RumorBoardFocusContext
{
    public RumorBoardFocusContext(string npcName, string npcDisplayName)
    {
        NpcName = (npcName ?? string.Empty).Trim();
        NpcDisplayName = string.IsNullOrWhiteSpace(npcDisplayName) ? NpcName : npcDisplayName.Trim();
    }

    public string NpcName { get; }
    public string NpcDisplayName { get; }
}

public sealed class RumorBoardMenu : IClickableMenu
{
    private const int BoardWidth = 1040;
    private const int BoardHeight = 698;
    private static string DefaultDetailMessage => I18n.Get("rumor_board.detail.default", "Select a request to view details.");
    private static string SearchingDetailMessage => I18n.Get("rumor_board.detail.searching", "Searching the board for new requests...");
    private static string DailyCapDetailMessage => I18n.Get("rumor_board.detail.daily_cap", "No new posting right now. Check back tomorrow.");
    private static string NoRequestDetailMessage => I18n.Get("rumor_board.detail.none_available", "No one has a new posting right now.");
    private static string NoPostingCreatedDetailMessage => I18n.Get("rumor_board.detail.none_created", "No posting was added from that reply.");

    private readonly SaveState _state;
    private readonly RumorBoardService _rumorBoardService;
    private readonly IMonitor _monitor;
    private readonly Action _onAskMayorForWork;
    private readonly Func<string>? _getExternalStatus;
    private readonly RumorBoardFocusContext? _focusContext;

    private readonly List<(QuestEntry Quest, Rectangle Rect)> _availableRows = new();
    private readonly List<(QuestEntry Quest, Rectangle Rect)> _activeRows = new();
    private int _availableScrollOffset;
    private int _activeScrollOffset;
    private int _detailScrollOffset;
    private int _detailContentHeight;
    private Rectangle _detailTextViewport;
    private Rectangle _detailScrollTrackRegion;
    private Rectangle _detailScrollThumbRegion;
    private bool _detailScrollThumbHeld;
    private int _detailScrollThumbDragOffset;

    private QuestEntry? _selectedQuest;
    private string _statusMessage = string.Empty;
    private bool _awaitingBoardSearchResult;
    private int _searchStartAvailableCount;
    private int _lastAvailableCount;
    private int _lastActiveCount;
    private int _lastCalendarDay;
    private bool _showAllTownRequestsFromFocus;

    private Rectangle _acceptButton;
    private Rectangle _completeButton;
    private Rectangle _askWorkButton;
    private readonly ClickableTextureComponent _closeButton;

    private const int VisibleQuestRows = 4;
    private const int SectionLeftMargin = 36;
    private const int SectionTopY = 116;
    private const int SectionWidth = 460;
    private const int SectionRowHeight = 44;
    private const int SectionRowGap = 8;
    private const int SectionRowsStartOffset = 36;
    private const int DetailPanelHeight = 288;
    private const int DetailBottomPadding = 36;
    private const int DetailLineHeight = 30;
    private const int DetailHeaderHeight = 58;
    private const int DetailProgressSectionHeight = 48;
    private const int DetailProgressBarHeight = 38;
    private const int DetailInterestFlavorTopPadding = 6;
    private const int DetailScrollbarWidth = 14;
    private const int DetailScrollbarGap = 8;

    public RumorBoardMenu(SaveState state, RumorBoardService rumorBoardService, IMonitor monitor, Action onAskMayorForWork, Func<string>? getExternalStatus = null, RumorBoardFocusContext? focusContext = null)
        : base(
            Game1.uiViewport.Width / 2 - (BoardWidth / 2),
            Game1.uiViewport.Height / 2 - (BoardHeight / 2),
            BoardWidth,
            BoardHeight,
            true)
    {
        _state = state;
        _rumorBoardService = rumorBoardService;
        _monitor = monitor;
        _onAskMayorForWork = onAskMayorForWork;
        _getExternalStatus = getExternalStatus;
        _focusContext = focusContext;
        _closeButton = new ClickableTextureComponent(
            new Rectangle(xPositionOnScreen + width - 68, yPositionOnScreen + 20, 48, 48),
            Game1.mouseCursors,
            new Rectangle(337, 494, 12, 12),
            4f);
        _lastAvailableCount = _state.Quests.Available.Count;
        _lastActiveCount = _state.Quests.Active.Count;
        _lastCalendarDay = _state.Calendar.Day;
        _statusMessage = IsNpcFocusActive ? BuildNpcFocusStatusMessage() : DefaultDetailMessage;
        BuildLayout();
    }

    private void BuildLayout()
    {
        _rumorBoardService.ExpireOverdueQuests(_state);
        _availableRows.Clear();
        _activeRows.Clear();
        ClampScrollOffsets();

        var leftX = xPositionOnScreen + SectionLeftMargin;
        var topY = yPositionOnScreen + SectionTopY;
        var availableQuests = GetAvailableQuestsForCurrentView().ToList();
        var activeQuests = GetActiveQuestsForCurrentView().ToList();

        var ay = topY + SectionRowsStartOffset;
        foreach (var q in availableQuests
                     .Skip(_availableScrollOffset)
                     .Take(VisibleQuestRows))
        {
            _availableRows.Add((q, new Rectangle(leftX, ay, SectionWidth, SectionRowHeight)));
            ay += SectionRowHeight + SectionRowGap;
        }

        var rightX = xPositionOnScreen + width - SectionWidth - SectionLeftMargin  - 20;
        var ry = topY + SectionRowsStartOffset;
        foreach (var q in activeQuests
                     .Skip(_activeScrollOffset)
                     .Take(VisibleQuestRows))
        {
            _activeRows.Add((q, new Rectangle(rightX, ry, SectionWidth, SectionRowHeight)));
            ry += SectionRowHeight + SectionRowGap;
        }

        var detailPanel = GetDetailPanelBounds();
        var buttonY = detailPanel.Bottom - 52;
        _acceptButton = new Rectangle(xPositionOnScreen + 50, buttonY, 160, 44);
        _completeButton = new Rectangle(xPositionOnScreen + 225, buttonY, 190, 44);
        _askWorkButton = new Rectangle(xPositionOnScreen + width - 265, buttonY, 210, 44);
        SyncSelectionForCurrentView(availableQuests, activeQuests);
    }

    private void ClampScrollOffsets()
    {
        var visibleAvailableCount = GetAvailableQuestsForCurrentView().Count();
        var visibleActiveCount = GetActiveQuestsForCurrentView().Count();
        var maxAvailableOffset = Math.Max(0, visibleAvailableCount - VisibleQuestRows);
        var maxActiveOffset = Math.Max(0, visibleActiveCount - VisibleQuestRows);
        _availableScrollOffset = Math.Clamp(_availableScrollOffset, 0, maxAvailableOffset);
        _activeScrollOffset = Math.Clamp(_activeScrollOffset, 0, maxActiveOffset);
    }

    private bool IsNpcFocusActive => _focusContext is not null && !_showAllTownRequestsFromFocus;

    private IEnumerable<QuestEntry> GetAvailableQuestsForCurrentView()
    {
        var available = _state.Quests.Available
            .Where(q => q.Status.Equals("available", StringComparison.OrdinalIgnoreCase))
            .Where(q => q.ExpiresDay <= 0 || q.ExpiresDay >= _state.Calendar.Day);
        return IsNpcFocusActive
            ? available.Where(IsQuestRelevantToFocusedNpc)
            : available;
    }

    private IEnumerable<QuestEntry> GetActiveQuestsForCurrentView()
    {
        var active = _state.Quests.Active
            .Where(q => q.Status.Equals("active", StringComparison.OrdinalIgnoreCase))
            .Where(q => q.ExpiresDay <= 0 || q.ExpiresDay >= _state.Calendar.Day);
        return IsNpcFocusActive
            ? active.Where(IsQuestRelevantToFocusedNpc)
            : active;
    }

    private bool IsQuestRelevantToFocusedNpc(QuestEntry quest)
    {
        if (_focusContext is null)
            return true;

        if (MatchesFocusedNpc(quest.Issuer))
            return true;

        return quest.Status.Equals("active", StringComparison.OrdinalIgnoreCase)
            && quest.TemplateId.Equals("social_visit", StringComparison.OrdinalIgnoreCase)
            && MatchesFocusedNpc(quest.TargetItem);
    }

    private bool MatchesFocusedNpc(string? rawValue)
    {
        if (_focusContext is null || string.IsNullOrWhiteSpace(rawValue))
            return false;

        var value = rawValue.Trim();
        return value.Equals(_focusContext.NpcName, StringComparison.OrdinalIgnoreCase)
            || value.Equals(_focusContext.NpcDisplayName, StringComparison.OrdinalIgnoreCase)
            || QuestTextHelper.PrettyName(value).Equals(_focusContext.NpcDisplayName, StringComparison.OrdinalIgnoreCase);
    }

    private void SyncSelectionForCurrentView(IReadOnlyList<QuestEntry> availableQuests, IReadOnlyList<QuestEntry> activeQuests)
    {
        var visibleQuestIds = new HashSet<string>(
            activeQuests.Select(q => q.QuestId).Concat(availableQuests.Select(q => q.QuestId)),
            StringComparer.OrdinalIgnoreCase);

        if (_selectedQuest is not null && !visibleQuestIds.Contains(_selectedQuest.QuestId))
            _selectedQuest = null;

        if (_selectedQuest is null && IsNpcFocusActive)
            _selectedQuest = activeQuests.FirstOrDefault() ?? availableQuests.FirstOrDefault();

        if (IsNpcFocusActive && _selectedQuest is null)
            _statusMessage = BuildNpcFocusStatusMessage();
        else if (!IsNpcFocusActive && string.IsNullOrWhiteSpace(_statusMessage))
            _statusMessage = DefaultDetailMessage;
    }

    private string BuildNpcFocusStatusMessage()
    {
        if (_focusContext is null)
            return DefaultDetailMessage;

        return I18n.Get(
            "rumor_board.detail.focus_none",
            $"Nothing from {_focusContext.NpcDisplayName} right now. You can still open all town requests from here.",
            new { npc = _focusContext.NpcDisplayName });
    }

    private string GetBoardTitle()
    {
        if (!IsNpcFocusActive || _focusContext is null)
            return I18n.Get("rumor_board.title", "Town Request Board");

        return I18n.Get(
            "rumor_board.title.focused",
            $"Town Requests: {_focusContext.NpcDisplayName}",
            new { npc = _focusContext.NpcDisplayName });
    }

    private string GetViewToggleLabel()
    {
        if (_focusContext is null)
            return I18n.Get("rumor_board.button.new_postings", "New Postings");

        return IsNpcFocusActive
            ? I18n.Get("rumor_board.button.show_all", "Show All")
            : I18n.Get("rumor_board.button.back_to_npc", $"Back to {_focusContext.NpcDisplayName}", new { npc = _focusContext.NpcDisplayName });
    }

    private string GetEmptySectionText(bool isActiveSection)
    {
        if (!IsNpcFocusActive || _focusContext is null)
        {
            return isActiveSection
                ? I18n.Get("rumor_board.empty.active", "No active requests.")
                : I18n.Get("rumor_board.empty.available", "No requests posted today.");
        }

        return isActiveSection
            ? I18n.Get("rumor_board.empty.focus_active", $"No active tasks tied to {_focusContext.NpcDisplayName}.", new { npc = _focusContext.NpcDisplayName })
            : I18n.Get("rumor_board.empty.focus_available", $"No requests from {_focusContext.NpcDisplayName} right now.", new { npc = _focusContext.NpcDisplayName });
    }

    private Rectangle GetAvailableListBounds()
    {
        var x = xPositionOnScreen + SectionLeftMargin;
        var y = yPositionOnScreen + SectionTopY + SectionRowsStartOffset;
        var listHeight = (SectionRowHeight * VisibleQuestRows) + (SectionRowGap * (VisibleQuestRows - 1));
        return new Rectangle(x, y, SectionWidth, listHeight);
    }

    private Rectangle GetActiveListBounds()
    {
        var x = xPositionOnScreen + width - SectionWidth - SectionLeftMargin;
        var y = yPositionOnScreen + SectionTopY + SectionRowsStartOffset;
        var listHeight = (SectionRowHeight * VisibleQuestRows) + (SectionRowGap * (VisibleQuestRows - 1));
        return new Rectangle(x, y, SectionWidth, listHeight);
    }

    private Rectangle GetDetailPanelBounds()
    {
        return new Rectangle(
            xPositionOnScreen + 32,
            yPositionOnScreen + height - DetailPanelHeight - DetailBottomPadding,
            width - 64,
            DetailPanelHeight);
    }

    private Rectangle GetDetailTextViewportBounds(Rectangle detailPanel, bool reserveScrollbarSpace)
    {
        var viewportY = detailPanel.Y + 16 + DetailHeaderHeight;
        var viewportBottom = _acceptButton.Y - DetailProgressSectionHeight - 14;
        var viewportHeight = Math.Max(40, viewportBottom - viewportY);
        var viewportWidth = detailPanel.Width - 48; // Increased padding
        if (reserveScrollbarSpace)
            viewportWidth = Math.Max(120, viewportWidth - DetailScrollbarWidth - DetailScrollbarGap);

        return new Rectangle(detailPanel.X + 24, viewportY, viewportWidth, viewportHeight); // Shifted right
    }

    private int GetMaxDetailScroll()
    {
        return Math.Max(0, _detailContentHeight - _detailTextViewport.Height);
    }

    private bool CanScrollDetailContent()
    {
        return _detailContentHeight > _detailTextViewport.Height;
    }

    private void ClampDetailScrollOffset()
    {
        _detailScrollOffset = Math.Clamp(_detailScrollOffset, 0, GetMaxDetailScroll());
    }

    private void ScrollDetailBy(int delta)
    {
        _detailScrollOffset = Math.Clamp(_detailScrollOffset + delta, 0, GetMaxDetailScroll());
        UpdateDetailScrollThumbRegion();
    }

    private void UpdateDetailScrollThumbRegion()
    {
        if (!CanScrollDetailContent())
        {
            _detailScrollThumbRegion = Rectangle.Empty;
            return;
        }

        var maxScroll = GetMaxDetailScroll();
        var thumbRatio = (float)_detailTextViewport.Height / _detailContentHeight;
        var thumbHeight = Math.Max(20, (int)(_detailScrollTrackRegion.Height * thumbRatio));
        var scrollPercent = maxScroll > 0 ? (float)_detailScrollOffset / maxScroll : 0f;
        var thumbY = _detailScrollTrackRegion.Y + (int)((_detailScrollTrackRegion.Height - thumbHeight) * scrollPercent);

        _detailScrollThumbRegion = new Rectangle(
            _detailScrollTrackRegion.X,
            thumbY,
            _detailScrollTrackRegion.Width,
            thumbHeight);
    }

    public override void receiveScrollWheelAction(int direction)
    {
        base.receiveScrollWheelAction(direction);
        if (direction == 0)
            return;

        var rowDelta = direction > 0 ? -1 : 1;
        var mouseX = Game1.getMouseX();
        var mouseY = Game1.getMouseY();

        if (GetAvailableListBounds().Contains(mouseX, mouseY))
        {
            var visibleAvailableCount = _state.Quests.Available.Count(q =>
                q.Status.Equals("available", StringComparison.OrdinalIgnoreCase)
                && (q.ExpiresDay <= 0 || q.ExpiresDay >= _state.Calendar.Day));
            var maxAvailableOffset = Math.Max(0, visibleAvailableCount - VisibleQuestRows);
            var next = Math.Clamp(_availableScrollOffset + rowDelta, 0, maxAvailableOffset);
            if (next != _availableScrollOffset)
            {
                _availableScrollOffset = next;
                BuildLayout();
            }
            return;
        }

        if (GetActiveListBounds().Contains(mouseX, mouseY))
        {
            var visibleActiveCount = _state.Quests.Active.Count(q =>
                q.Status.Equals("active", StringComparison.OrdinalIgnoreCase)
                && (q.ExpiresDay <= 0 || q.ExpiresDay >= _state.Calendar.Day));
            var maxActiveOffset = Math.Max(0, visibleActiveCount - VisibleQuestRows);
            var next = Math.Clamp(_activeScrollOffset + rowDelta, 0, maxActiveOffset);
            if (next != _activeScrollOffset)
            {
                _activeScrollOffset = next;
                BuildLayout();
            }
            return;
        }

        if (_selectedQuest is not null
            && (_detailTextViewport.Contains(mouseX, mouseY) || _detailScrollTrackRegion.Contains(mouseX, mouseY)))
        {
            var pixelDelta = direction > 0 ? -DetailLineHeight * 2 : DetailLineHeight * 2;
            ScrollDetailBy(pixelDelta);
        }
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);

        if (_selectedQuest is not null && _detailScrollThumbRegion.Contains(x, y) && CanScrollDetailContent())
        {
            _detailScrollThumbHeld = true;
            _detailScrollThumbDragOffset = y - _detailScrollThumbRegion.Y;
            return;
        }

        if (_selectedQuest is not null && _detailScrollTrackRegion.Contains(x, y) && CanScrollDetailContent())
        {
            if (y < _detailScrollThumbRegion.Y)
                ScrollDetailBy(-Math.Max(DetailLineHeight * 2, _detailTextViewport.Height / 2));
            else if (y > _detailScrollThumbRegion.Bottom)
                ScrollDetailBy(Math.Max(DetailLineHeight * 2, _detailTextViewport.Height / 2));
            return;
        }

        if (_closeButton.containsPoint(x, y))
        {
            exitThisMenu(playSound);
            return;
        }

        foreach (var (quest, rect) in _availableRows)
        {
            if (!rect.Contains(x, y))
                continue;

            _selectedQuest = quest;
            _detailScrollOffset = 0;
            _detailScrollThumbHeld = false;
            Game1.playSound("smallSelect");
            return;
        }

        foreach (var (quest, rect) in _activeRows)
        {
            if (!rect.Contains(x, y))
                continue;

            _selectedQuest = quest;
            _detailScrollOffset = 0;
            _detailScrollThumbHeld = false;
            Game1.playSound("smallSelect");
            return;
        }

        if (_askWorkButton.Contains(x, y))
        {
            if (_focusContext is not null)
            {
                _showAllTownRequestsFromFocus = !_showAllTownRequestsFromFocus;
                _selectedQuest = null;
                _detailScrollOffset = 0;
                _detailScrollThumbHeld = false;
                _statusMessage = IsNpcFocusActive ? BuildNpcFocusStatusMessage() : DefaultDetailMessage;
                BuildLayout();
                Game1.playSound("smallSelect");
                return;
            }

            // If an active quest is selected, check its progress instead of searching for new postings
            if (_selectedQuest is not null && _selectedQuest.Status.Equals("active", StringComparison.OrdinalIgnoreCase))
            {
                CheckSelectedQuestProgress();
                Game1.playSound("smallSelect");
                return;
            }

            _searchStartAvailableCount = _state.Quests.Available.Count;
            _onAskMayorForWork();
            _statusMessage = SearchingDetailMessage;
            _awaitingBoardSearchResult = true;
            _lastAvailableCount = _state.Quests.Available.Count;
            _lastActiveCount = _state.Quests.Active.Count;
            SyncDetailMessageFromExternalStatus();
            Game1.playSound("newArtifact");
            return;
        }

        if (_selectedQuest is null)
            return;

        if (_acceptButton.Contains(x, y) && _selectedQuest.Status.Equals("available", StringComparison.OrdinalIgnoreCase))
        {
            var title = QuestTextHelper.BuildQuestTitle(_selectedQuest);
            var ok = _rumorBoardService.AcceptQuest(_state, _selectedQuest.QuestId);
            _statusMessage = ok
                ? QuestTextHelper.BuildAcceptedMessage(title)
                : I18n.Get("rumor_board.status.accept_failed", "Could not accept request.");
            if (ok)
            {
                var flavor = InterestTextHelper.BuildQuestAcceptLine(_selectedQuest);
                if (!string.IsNullOrWhiteSpace(flavor))
                    _statusMessage = $"{_statusMessage} {flavor}";
                _monitor.Log(_statusMessage, LogLevel.Info);
                Game1.playSound("coin");
            }
            else
            {
                Game1.playSound("cancel");
            }

            _selectedQuest = null;
            _detailScrollOffset = 0;
            _detailScrollThumbHeld = false;
            BuildLayout();
            return;
        }

        if (_completeButton.Contains(x, y) && _selectedQuest.Status.Equals("active", StringComparison.OrdinalIgnoreCase))
        {
            var selectedProgress = _rumorBoardService.GetQuestProgress(_state, _selectedQuest.QuestId, Game1.player);
            if (!selectedProgress.IsReadyToComplete)
            {
                Game1.playSound("cancel");
                _statusMessage = I18n.Get("rumor_board.status.refresh_not_ready", "This request is not ready to turn in yet.");
                return;
            }

            var result = _rumorBoardService.CompleteQuestWithChecks(_state, _selectedQuest.QuestId, Game1.player, consumeItems: true);
            _statusMessage = result.Message;
            if (result.Success)
            {
                var flavor = InterestTextHelper.BuildQuestCompleteLine(_selectedQuest);
                if (!string.IsNullOrWhiteSpace(flavor))
                    _statusMessage = $"{_statusMessage} {flavor}";
                _monitor.Log(result.Message, LogLevel.Info);
                Game1.playSound("reward");
            }
            else
            {
                Game1.playSound("cancel");
            }

            _selectedQuest = null;
            _detailScrollOffset = 0;
            _detailScrollThumbHeld = false;
            BuildLayout();
        }
    }

    public override void performHoverAction(int x, int y)
    {
        base.performHoverAction(x, y);
        _closeButton.tryHover(x, y);
    }

    public override void leftClickHeld(int x, int y)
    {
        base.leftClickHeld(x, y);
        if (!_detailScrollThumbHeld || !CanScrollDetailContent())
            return;

        var trackHeight = _detailScrollTrackRegion.Height - _detailScrollThumbRegion.Height;
        if (trackHeight <= 0)
        {
            _detailScrollOffset = 0;
            return;
        }

        var newThumbY = Math.Clamp(
            y - _detailScrollThumbDragOffset,
            _detailScrollTrackRegion.Y,
            _detailScrollTrackRegion.Y + trackHeight);
        var scrollPercent = (float)(newThumbY - _detailScrollTrackRegion.Y) / trackHeight;
        _detailScrollOffset = (int)(scrollPercent * GetMaxDetailScroll());
    }

    public override void releaseLeftClick(int x, int y)
    {
        base.releaseLeftClick(x, y);
        _detailScrollThumbHeld = false;
    }

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
    {
        base.gameWindowSizeChanged(oldBounds, newBounds);

        // Recalculate menu position based on new viewport
        xPositionOnScreen = Game1.uiViewport.Width / 2 - (width / 2);
        yPositionOnScreen = Game1.uiViewport.Height / 2 - (height / 2);

        // Update close button position
        _closeButton.bounds = new Rectangle(xPositionOnScreen + width - 68, yPositionOnScreen + 20, 48, 48);

        // Rebuild all layout elements
        BuildLayout();
    }

    private void CheckSelectedQuestProgress()
    {
        if (_selectedQuest is null || !_selectedQuest.Status.Equals("active", StringComparison.OrdinalIgnoreCase))
        {
            _statusMessage = I18n.Get("rumor_board.status.refresh_requires_active", "Select an active request to check progress.");
            return;
        }

        var progress = _rumorBoardService.GetQuestProgress(_state, _selectedQuest.QuestId, Game1.player);
        if (!progress.Exists)
        {
            _statusMessage = I18n.Get("rumor_board.status.refresh_missing", "That request is no longer active.");
            return;
        }

        if (progress.IsReadyToComplete)
        {
            var title = QuestTextHelper.BuildQuestTitle(progress.Quest!);
            _statusMessage = QuestTextHelper.BuildProgressReadyMessage(title);
        }
        else if (progress.RequiresItems)
        {
            var item = QuestTextHelper.GetQuestTargetDisplayName(progress.Quest!);
            _statusMessage = QuestTextHelper.BuildProgressItemsMessage(progress.HaveCount, progress.NeedCount, item);
        }
        else // social_visit
        {
            var target = QuestTextHelper.GetQuestTargetDisplayName(progress.Quest!);
            _statusMessage = QuestTextHelper.BuildProgressVisitMessage(target);
        }
    }

    public override void draw(SpriteBatch b)
    {
        SyncDetailMessageFromExternalStatus();

        if (_lastCalendarDay != _state.Calendar.Day
            || _lastAvailableCount != _state.Quests.Available.Count
            || _lastActiveCount != _state.Quests.Active.Count)
        {
            _lastCalendarDay = _state.Calendar.Day;
            _lastAvailableCount = _state.Quests.Available.Count;
            _lastActiveCount = _state.Quests.Active.Count;
            BuildLayout();
        }

        b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.4f);
        Game1.drawDialogueBox(xPositionOnScreen, yPositionOnScreen, width, height, false, true);
        
        // Ribbon Header
        SpriteText.drawStringWithScrollCenteredAt(
            b,
            GetBoardTitle(),
            xPositionOnScreen + (width / 2),
            yPositionOnScreen + 16);

        DrawSection(b, I18n.Get("rumor_board.section.available", "Available"), _availableRows, isActiveSection: false);
        DrawSection(b, I18n.Get("rumor_board.section.active", "Active"), _activeRows, isActiveSection: true);
        DrawDetailPanel(b);
        _closeButton.draw(b);

        drawMouse(b);
    }

    private void DrawSection(SpriteBatch b, string label, List<(QuestEntry Quest, Rectangle Rect)> rows, bool isActiveSection)
    {
        var labelX = rows.Count > 0
        ? rows[0].Rect.X
        : (isActiveSection
            ? xPositionOnScreen + width - SectionWidth - SectionLeftMargin - 20
            : xPositionOnScreen + SectionLeftMargin);

        // Shift label right to align perfectly with the padded card contents
        labelX += 20;

        var labelY = rows.Count > 0 ? rows[0].Rect.Y - 32 : yPositionOnScreen + SectionTopY + 6;
        var labelPos = new Vector2(labelX, labelY);
        
        // Polished heading with text shadow instead of raw text drawing
        Utility.drawTextWithShadow(b, label, Game1.smallFont, labelPos, Game1.textColor, 1f, -1f, -1, -1, 0.5f);

        Point mouse = new Point(Game1.getMouseX(), Game1.getMouseY());

        if (rows.Count > 0)
        {
            foreach (var (quest, rect) in rows)
            {
                bool isHovered = rect.Contains(mouse);
                bool isSelected = _selectedQuest?.QuestId == quest.QuestId;

                // Row container drop shadow for depth
                b.Draw(Game1.staminaRect, new Rectangle(rect.X + 4, rect.Y + 4, rect.Width, rect.Height), Color.Black * 0.15f);

                // Highlighted background interaction
                Color bgColor = isSelected ? new Color(255, 250, 220) : (isHovered ? new Color(255, 245, 205) : new Color(255, 238, 191));
                b.Draw(Game1.staminaRect, rect, bgColor);

                // Polished 2px/3px borders 
                Color borderColor = isSelected ? new Color(220, 120, 40) : new Color(130, 80, 40);
                int borderThickness = isSelected ? 3 : 2;

                // Draw borders dynamically
                b.Draw(Game1.staminaRect, new Rectangle(rect.X, rect.Y, rect.Width, borderThickness), borderColor);
                b.Draw(Game1.staminaRect, new Rectangle(rect.X, rect.Y + rect.Height - borderThickness, rect.Width, borderThickness), borderColor);
                b.Draw(Game1.staminaRect, new Rectangle(rect.X, rect.Y, borderThickness, rect.Height), borderColor);
                b.Draw(Game1.staminaRect, new Rectangle(rect.X + rect.Width - borderThickness, rect.Y, borderThickness, rect.Height), borderColor);

                var title = QuestTextHelper.BuildQuestTitle(quest);
                
                // Truncate title correctly leaving room for right-aligned text
                int spaceForRightText = 120; // Increased to ensure right text doesn't overlap
                var clippedTitle = TrimTextToWidth(Game1.smallFont, title, rect.Width - 44 - spaceForRightText);

                if (isActiveSection)
                    DrawClockIcon(b, rect.X + 20, rect.Y + 14); // Pushed right
                else
                    DrawCoinIcon(b, rect.X + 20, rect.Y + 14); // Pushed right

                Color titleColor = isSelected ? Game1.textColor : Game1.textColor * 0.9f;
                b.DrawString(Game1.smallFont, clippedTitle, new Vector2(rect.X + 44, rect.Y + 10), titleColor); // Centered vertically & pushed right

                // Beautifully Right-Aligned Metadata (Gold/Date)
                string rightText = isActiveSection 
                    ? (quest.ExpiresDay > 0 ? CalendarDisplayHelper.FormatWeekdayDay(quest.ExpiresDay) : "No limit") 
                    : $"+{quest.RewardGold}g";
                
                var rightSize = Game1.smallFont.MeasureString(rightText);
                Vector2 rightPos = new Vector2(rect.X + rect.Width - rightSize.X - 20, rect.Y + 10); // Pushed symmetrically left, centered vertically
                
                Color rightColor = isActiveSection ? new Color(150, 50, 50) : new Color(40, 110, 40);
                b.DrawString(Game1.smallFont, rightText, rightPos, rightColor);
            }
        }
        else
        {
            var text = GetEmptySectionText(isActiveSection);
            b.DrawString(Game1.smallFont, text, new Vector2(labelX, labelY + 45), Game1.textColor * 0.7f);
        }
    }

    private void DrawDetailPanel(SpriteBatch b)
    {
        var panel = GetDetailPanelBounds();
        IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 396, 15, 15), panel.X, panel.Y, panel.Width, panel.Height, Color.White, 4f, false);

        if (_selectedQuest is null)
        {
            _detailScrollOffset = 0;
            _detailContentHeight = 0;
            _detailTextViewport = Rectangle.Empty;
            _detailScrollTrackRegion = Rectangle.Empty;
            _detailScrollThumbRegion = Rectangle.Empty;
            _detailScrollThumbHeld = false;
            DrawWrappedStatusMessage(b, panel, _statusMessage);
            DrawButton(b, _askWorkButton, GetViewToggleLabel(), enabled: true);
            return;
        }

        var q = _selectedQuest;
        var progress = _rumorBoardService.GetQuestProgress(_state, q.QuestId, Game1.player);
        var rawLines = BuildDetailLines(q, progress);
        var interestFlavor = InterestTextHelper.BuildQuestDetailLine(q);
        DrawDetailHeader(b, q, panel);

        _detailTextViewport = GetDetailTextViewportBounds(panel, reserveScrollbarSpace: false);
        var wrappedLines = WrapDetailLines(rawLines, _detailTextViewport.Width);
        _detailContentHeight = CalculateDetailContentHeight(wrappedLines.Count, interestFlavor);

        if (_detailContentHeight > _detailTextViewport.Height)
            _detailTextViewport = GetDetailTextViewportBounds(panel, reserveScrollbarSpace: true);

        wrappedLines = WrapDetailLines(rawLines, _detailTextViewport.Width);
        _detailContentHeight = CalculateDetailContentHeight(wrappedLines.Count, interestFlavor);

        _detailScrollTrackRegion = CanScrollDetailContent()
            ? new Rectangle(
                _detailTextViewport.Right + DetailScrollbarGap,
                _detailTextViewport.Y,
                DetailScrollbarWidth,
                _detailTextViewport.Height)
            : Rectangle.Empty;
        ClampDetailScrollOffset();
        UpdateDetailScrollThumbRegion();

        var oldScissor = Game1.graphics.GraphicsDevice.ScissorRectangle;
        var rasterizer = new RasterizerState { ScissorTestEnable = true };
        b.End();
        b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, rasterizer);
        Game1.graphics.GraphicsDevice.ScissorRectangle = _detailTextViewport;

        var y = _detailTextViewport.Y - _detailScrollOffset;
        foreach (var line in rawLines)
        {
            var wrapped = TextWrapHelper.WrapText(Game1.smallFont, line, _detailTextViewport.Width);
            if (wrapped.Length == 0)
                wrapped = new[] { string.Empty };

            var isInterestFlavorLine = string.Equals(line, interestFlavor, StringComparison.Ordinal);
            var color = isInterestFlavorLine
                ? new Color(150, 50, 50)
                : Game1.textColor;

            if (isInterestFlavorLine)
                y += DetailInterestFlavorTopPadding;

            foreach (var wrappedLine in wrapped)
            {
                b.DrawString(Game1.smallFont, wrappedLine, new Vector2(_detailTextViewport.X, y), color);
                y += DetailLineHeight;
            }
        }

        b.End();
        b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
        Game1.graphics.GraphicsDevice.ScissorRectangle = oldScissor;

        // Draw Polished Scrollbar if needed
        if (CanScrollDetailContent())
        {
            // Darker track to seat the thumb nicely
            b.Draw(Game1.staminaRect, _detailScrollTrackRegion, new Color(100, 70, 40) * 0.5f);
            
            // Authentic UI Texture for scrollbar thumb instead of a flat box
            Color thumbTint = _detailScrollThumbHeld ? Color.White : Color.LightGray;
            IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(432, 439, 9, 9), 
                _detailScrollThumbRegion.X, _detailScrollThumbRegion.Y, 
                _detailScrollThumbRegion.Width, _detailScrollThumbRegion.Height, thumbTint, 4f, false);
        }

        DrawProgressSection(b, q, progress);

        DrawButton(b, _acceptButton, I18n.Get("rumor_board.button.accept", "Accept"), enabled: q.Status.Equals("available", StringComparison.OrdinalIgnoreCase));
        DrawButton(b, _completeButton, I18n.Get("rumor_board.button.complete", "Complete"), enabled: q.Status.Equals("active", StringComparison.OrdinalIgnoreCase) && progress.IsReadyToComplete);
        DrawButton(
            b,
            _askWorkButton,
            _focusContext is null
                ? I18n.Get("rumor_board.button.refresh", "Refresh")
                : GetViewToggleLabel(),
            enabled: true);
    }

    private static void DrawWrappedStatusMessage(SpriteBatch b, Rectangle panel, string message)
    {
        var wrapped = TextWrapHelper.WrapText(Game1.smallFont, message, panel.Width - 48);
        var totalHeight = wrapped.Length * DetailLineHeight;
        var y = panel.Y + Math.Max(16, ((panel.Height - 56) - totalHeight) / 2);

        foreach (var line in wrapped)
        {
            var size = Game1.smallFont.MeasureString(line);
            var x = panel.X + (panel.Width / 2f) - (size.X / 2f);
            b.DrawString(Game1.smallFont, line, new Vector2(x, y), Game1.textColor * 0.5f);
            y += DetailLineHeight;
        }
    }

    private static List<string> BuildDetailLines(QuestEntry q, QuestProgressResult progress)
    {
        var lines = new List<string>
        {
            QuestTextHelper.BuildQuestSummary(q)
        };

        var interestFlavor = InterestTextHelper.BuildQuestDetailLine(q);
        if (!string.IsNullOrWhiteSpace(interestFlavor))
            lines.Add(interestFlavor);

        return lines;
    }

    private static void DrawDetailHeader(SpriteBatch b, QuestEntry q, Rectangle panel)
    {
        var contentX = panel.X + 24;
        var title = QuestTextHelper.BuildQuestTitle(q);
        b.DrawString(Game1.smallFont, title, new Vector2(contentX, panel.Y + 16), Game1.textColor);

        var rowY = panel.Y + 16 + 30;
        var clientWidth = DrawMetadataPair(
            b,
            I18n.Get("rumor_board.detail.client_label", "Client:"),
            QuestTextHelper.GetQuestIssuerDisplayName(q.Issuer),
            new Vector2(contentX, rowY),
            Game1.textColor * 0.8f,
            new Color(116, 82, 164));

        var rewardX = contentX + clientWidth + 28f;
        var rewardWidth = DrawMetadataPair(
            b,
            I18n.Get("rumor_board.detail.reward_label", "Reward:"),
            $"+{q.RewardGold}g",
            new Vector2(rewardX, rowY),
            Game1.textColor * 0.8f,
            new Color(48, 128, 52));

        var deadlineX = rewardX + rewardWidth + 28f;
        DrawMetadataPair(
            b,
            I18n.Get("rumor_board.detail.deadline_label", "Deadline:"),
            q.ExpiresDay > 0
                ? CalendarDisplayHelper.FormatWeekdayDay(q.ExpiresDay)
                : I18n.Get("rumor_board.deadline.none", "No deadline"),
            new Vector2(deadlineX, rowY),
            Game1.textColor * 0.8f,
            new Color(150, 50, 50));
    }

    private static float DrawMetadataPair(SpriteBatch b, string label, string value, Vector2 position, Color labelColor, Color valueColor)
    {
        b.DrawString(Game1.smallFont, label, position, labelColor);
        var labelWidth = Game1.smallFont.MeasureString(label).X;
        var valuePosition = new Vector2(position.X + labelWidth + 6f, position.Y);
        b.DrawString(Game1.smallFont, value, valuePosition, valueColor);
        return labelWidth + 6f + Game1.smallFont.MeasureString(value).X;
    }

    private static int CalculateDetailContentHeight(int wrappedLineCount, string? interestFlavor)
    {
        var height = wrappedLineCount * DetailLineHeight;
        if (!string.IsNullOrWhiteSpace(interestFlavor))
            height += DetailInterestFlavorTopPadding;

        return height;
    }

    private void DrawProgressSection(SpriteBatch b, QuestEntry quest, QuestProgressResult progress)
    {
        if (!progress.Exists)
            return;

        var barWidth = _detailTextViewport.Width;
        if (CanScrollDetailContent())
            barWidth = _detailScrollTrackRegion.IsEmpty
                ? barWidth
                : Math.Max(160, _detailScrollTrackRegion.X - DetailScrollbarGap - _detailTextViewport.X);

        var barRect = new Rectangle(
            _detailTextViewport.X,
            _acceptButton.Y - DetailProgressSectionHeight,
            barWidth,
            DetailProgressBarHeight);

        b.Draw(Game1.staminaRect, barRect, new Color(96, 76, 48));
        var fillInset = 3;
        var innerRect = new Rectangle(barRect.X + fillInset, barRect.Y + fillInset, Math.Max(0, barRect.Width - (fillInset * 2)), Math.Max(0, barRect.Height - (fillInset * 2)));
        b.Draw(Game1.staminaRect, innerRect, new Color(226, 214, 188));

        var ratio = progress.NeedCount <= 0
            ? (progress.IsReadyToComplete ? 1f : 0f)
            : Math.Clamp(progress.HaveCount / (float)Math.Max(1, progress.NeedCount), 0f, 1f);
        var fillWidth = (int)(innerRect.Width * ratio);
        if (fillWidth > 0)
        {
            var fillRect = new Rectangle(innerRect.X, innerRect.Y, fillWidth, innerRect.Height);
            b.Draw(Game1.staminaRect, fillRect, progress.IsReadyToComplete ? new Color(84, 156, 78) : new Color(214, 174, 70));
        }

        var progressText = QuestTextHelper.BuildProgressBarText(quest, progress);

        var textSize = Game1.smallFont.MeasureString(progressText);
        var textPos = new Vector2(
            barRect.X + Math.Max(8, (barRect.Width - textSize.X) / 2f),
            innerRect.Y + Math.Max(3f, ((innerRect.Height - textSize.Y) / 2f)));
        b.DrawString(Game1.smallFont, progressText, textPos, new Color(58, 42, 25));
    }

    private static List<string> WrapDetailLines(IEnumerable<string> lines, int width)
    {
        var wrappedLines = new List<string>();
        foreach (var line in lines)
        {
            var wrapped = TextWrapHelper.WrapText(Game1.smallFont, line, width);
            if (wrapped.Length == 0)
            {
                wrappedLines.Add(string.Empty);
                continue;
            }

            wrappedLines.AddRange(wrapped);
        }

        return wrappedLines;
    }

    private void SyncDetailMessageFromExternalStatus()
    {
        // Only sync when we're actually awaiting a board search result
        if (!_awaitingBoardSearchResult || _focusContext is not null)
            return;

        var external = (_getExternalStatus?.Invoke() ?? string.Empty).Trim();

        if (_state.Quests.Available.Count > _searchStartAvailableCount)
        {
            _statusMessage = DefaultDetailMessage;
            _awaitingBoardSearchResult = false;
            return;
        }

        if (string.IsNullOrWhiteSpace(external))
            return;

        if (external.IndexOf("No new postings right now", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            _statusMessage = DailyCapDetailMessage;
            _awaitingBoardSearchResult = false;
            return;
        }

        if (external.IndexOf("New posting added to the board", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            _statusMessage = DefaultDetailMessage;
            _awaitingBoardSearchResult = false;
            return;
        }

        if (external.IndexOf("No one has a new posting right now", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            _statusMessage = NoRequestDetailMessage;
            _awaitingBoardSearchResult = false;
            return;
        }

        if (external.IndexOf("No posting was added", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            _statusMessage = NoPostingCreatedDetailMessage;
            _awaitingBoardSearchResult = false;
            return;
        }

        if (IsImmediateBoardGateStatus(external))
        {
            _statusMessage = external;
            _awaitingBoardSearchResult = false;
        }
    }

    private static bool IsImmediateBoardGateStatus(string status)
    {
        return status.IndexOf("Work request already in progress", StringComparison.OrdinalIgnoreCase) >= 0
            || status.IndexOf("Please wait ", StringComparison.OrdinalIgnoreCase) >= 0
            || status.IndexOf("Board is full", StringComparison.OrdinalIgnoreCase) >= 0
            || status.IndexOf("connect failed", StringComparison.OrdinalIgnoreCase) >= 0
            || status.IndexOf("disabled: missing game client id", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static void DrawButton(SpriteBatch b, Rectangle rect, string text, bool enabled)
    {
        IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(432, 439, 9, 9), rect.X, rect.Y, rect.Width, rect.Height, enabled ? Color.White : Color.Gray * 0.8f, 4f, false);

        var size = Game1.smallFont.MeasureString(text);
        var pos = new Vector2(rect.X + (rect.Width - size.X) / 2f, rect.Y + (rect.Height - size.Y) / 2f);
        
        if (enabled)
            Utility.drawTextWithShadow(b, text, Game1.smallFont, pos, Game1.textColor, 1f, -1f, -1, -1, 0.5f);
        else
            b.DrawString(Game1.smallFont, text, pos, Game1.textColor * 0.4f);
    }

    private static string TrimTextToWidth(SpriteFont font, string text, int maxWidth)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        if (font.MeasureString(text).X <= maxWidth)
            return text;

        const string ellipsis = "...";
        var trimmed = text.TrimEnd();
        while (trimmed.Length > 0 && font.MeasureString(trimmed + ellipsis).X > maxWidth)
            trimmed = trimmed[..^1];

        return string.IsNullOrWhiteSpace(trimmed)
            ? ellipsis
            : trimmed + ellipsis;
    }

    private static void DrawCoinIcon(SpriteBatch b, int x, int y)
    {
        b.Draw(Game1.staminaRect, new Rectangle(x, y, 12, 12), new Color(196, 142, 32));
        b.Draw(Game1.staminaRect, new Rectangle(x + 1, y + 1, 10, 10), new Color(233, 190, 77));
        b.Draw(Game1.staminaRect, new Rectangle(x + 3, y + 3, 4, 4), new Color(255, 234, 160));
    }

    private static void DrawClockIcon(SpriteBatch b, int x, int y)
    {
        b.Draw(Game1.staminaRect, new Rectangle(x, y, 12, 12), new Color(60, 50, 40));
        b.Draw(Game1.staminaRect, new Rectangle(x + 1, y + 1, 10, 10), new Color(235, 230, 215));
        b.Draw(Game1.staminaRect, new Rectangle(x + 5, y + 3, 1, 4), new Color(90, 75, 55));
        b.Draw(Game1.staminaRect, new Rectangle(x + 5, y + 5, 3, 1), new Color(90, 75, 55));
    }
}
