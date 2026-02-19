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

public sealed class RumorBoardMenu : IClickableMenu
{
    private const string DefaultDetailMessage = "Select a request to view details.";
    private const string SearchingDetailMessage = "Searching the board for new requests...";
    private const string DailyCapDetailMessage = "No new posting right now. Check back tomorrow.";
    private const string NoRequestDetailMessage = "No one has a new posting right now.";
    private const string NoPostingCreatedDetailMessage = "No posting was added from that reply.";

    private readonly SaveState _state;
    private readonly RumorBoardService _rumorBoardService;
    private readonly IMonitor _monitor;
    private readonly Action _onAskMayorForWork;
    private readonly Func<string>? _getExternalStatus;

    private readonly List<(QuestEntry Quest, Rectangle Rect)> _availableRows = new();
    private readonly List<(QuestEntry Quest, Rectangle Rect)> _activeRows = new();
    private int _availableScrollOffset;
    private int _activeScrollOffset;

    private QuestEntry? _selectedQuest;
    private string _statusMessage = DefaultDetailMessage;
    private bool _awaitingBoardSearchResult;
    private int _searchStartAvailableCount;
    private int _lastAvailableCount;
    private int _lastActiveCount;
    private int _lastCalendarDay;

    private Rectangle _acceptButton;
    private Rectangle _completeButton;
    private Rectangle _askWorkButton;
    private readonly ClickableTextureComponent _closeButton;

    private const int VisibleQuestRows = 7;
    private const int SectionLeftMargin = 36;
    private const int SectionTopY = 116;
    private const int SectionWidth = 460;
    private const int SectionRowHeight = 44;
    private const int SectionRowGap = 8;
    private const int SectionRowsStartOffset = 36;
    private const int DetailPanelHeight = 190;
    private const int DetailBottomPadding = 36;

    public RumorBoardMenu(SaveState state, RumorBoardService rumorBoardService, IMonitor monitor, Action onAskMayorForWork, Func<string>? getExternalStatus = null)
        : base(
            Game1.uiViewport.Width / 2 - 520,
            Game1.uiViewport.Height / 2 - 300,
            1040,
            600,
            true)
    {
        _state = state;
        _rumorBoardService = rumorBoardService;
        _monitor = monitor;
        _onAskMayorForWork = onAskMayorForWork;
        _getExternalStatus = getExternalStatus;
        _closeButton = new ClickableTextureComponent(
            new Rectangle(xPositionOnScreen + width - 68, yPositionOnScreen + 20, 48, 48),
            Game1.mouseCursors,
            new Rectangle(337, 494, 12, 12),
            4f);
        _lastAvailableCount = _state.Quests.Available.Count;
        _lastActiveCount = _state.Quests.Active.Count;
        _lastCalendarDay = _state.Calendar.Day;
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

        var ay = topY + SectionRowsStartOffset;
        foreach (var q in _state.Quests.Available
                     .Where(q => q.Status.Equals("available", StringComparison.OrdinalIgnoreCase))
                     .Where(q => q.ExpiresDay <= 0 || q.ExpiresDay >= _state.Calendar.Day)
                     .Skip(_availableScrollOffset)
                     .Take(VisibleQuestRows))
        {
            _availableRows.Add((q, new Rectangle(leftX, ay, SectionWidth, SectionRowHeight)));
            ay += SectionRowHeight + SectionRowGap;
        }

        var rightX = xPositionOnScreen + width - SectionWidth - SectionLeftMargin  - 20;
        var ry = topY + SectionRowsStartOffset;
        foreach (var q in _state.Quests.Active
                     .Where(q => q.Status.Equals("active", StringComparison.OrdinalIgnoreCase))
                     .Where(q => q.ExpiresDay <= 0 || q.ExpiresDay >= _state.Calendar.Day)
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
    }

    private void ClampScrollOffsets()
    {
        var visibleAvailableCount = _state.Quests.Available.Count(q =>
            q.Status.Equals("available", StringComparison.OrdinalIgnoreCase)
            && (q.ExpiresDay <= 0 || q.ExpiresDay >= _state.Calendar.Day));
        var visibleActiveCount = _state.Quests.Active.Count(q =>
            q.Status.Equals("active", StringComparison.OrdinalIgnoreCase)
            && (q.ExpiresDay <= 0 || q.ExpiresDay >= _state.Calendar.Day));
        var maxAvailableOffset = Math.Max(0, visibleAvailableCount - VisibleQuestRows);
        var maxActiveOffset = Math.Max(0, visibleActiveCount - VisibleQuestRows);
        _availableScrollOffset = Math.Clamp(_availableScrollOffset, 0, maxAvailableOffset);
        _activeScrollOffset = Math.Clamp(_activeScrollOffset, 0, maxActiveOffset);
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

    public override void receiveScrollWheelAction(int direction)
    {
        base.receiveScrollWheelAction(direction);
        if (direction == 0)
            return;

        var delta = direction > 0 ? -1 : 1;
        var mouseX = Game1.getMouseX();
        var mouseY = Game1.getMouseY();

        if (GetAvailableListBounds().Contains(mouseX, mouseY))
        {
            var maxAvailableOffset = Math.Max(0, _state.Quests.Available.Count - VisibleQuestRows);
            var next = Math.Clamp(_availableScrollOffset + delta, 0, maxAvailableOffset);
            if (next != _availableScrollOffset)
            {
                _availableScrollOffset = next;
                BuildLayout();
            }
            return;
        }

        if (GetActiveListBounds().Contains(mouseX, mouseY))
        {
            var maxActiveOffset = Math.Max(0, _state.Quests.Active.Count - VisibleQuestRows);
            var next = Math.Clamp(_activeScrollOffset + delta, 0, maxActiveOffset);
            if (next != _activeScrollOffset)
            {
                _activeScrollOffset = next;
                BuildLayout();
            }
        }
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);

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
            Game1.playSound("smallSelect");
            return;
        }

        foreach (var (quest, rect) in _activeRows)
        {
            if (!rect.Contains(x, y))
                continue;

            _selectedQuest = quest;
            Game1.playSound("smallSelect");
            return;
        }

        if (_askWorkButton.Contains(x, y))
        {
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
            _statusMessage = ok ? $"Accepted Town Request: {title}" : "Could not accept request.";
            if (ok)
            {
                _monitor.Log(_statusMessage, LogLevel.Info);
                Game1.playSound("coin");
            }
            else
            {
                Game1.playSound("cancel");
            }

            _selectedQuest = null;
            BuildLayout();
            return;
        }

        if (_completeButton.Contains(x, y) && _selectedQuest.Status.Equals("active", StringComparison.OrdinalIgnoreCase))
        {
            var result = _rumorBoardService.CompleteQuestWithChecks(_state, _selectedQuest.QuestId, Game1.player, consumeItems: true);
            _statusMessage = result.Message;
            if (result.Success)
            {
                _monitor.Log(result.Message, LogLevel.Info);
                Game1.playSound("reward");
            }
            else
            {
                Game1.playSound("cancel");
            }

            _selectedQuest = null;
            BuildLayout();
        }
    }

    public override void performHoverAction(int x, int y)
    {
        base.performHoverAction(x, y);
        _closeButton.tryHover(x, y);
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
        SpriteText.drawStringWithScrollCenteredAt(b, "Town Request Board", xPositionOnScreen + (width / 2), yPositionOnScreen + 16);

        DrawSection(b, "Available", _availableRows, isActiveSection: false);
        DrawSection(b, "Active", _activeRows, isActiveSection: true);
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

        var labelY = rows.Count > 0 ? rows[0].Rect.Y - 24 : yPositionOnScreen + SectionTopY + 6;
        var labelPos = new Vector2(labelX, labelY);
        const float labelScale = 0.8f;
        b.DrawString(Game1.smallFont, label, labelPos, new Color(70, 110, 185), 0f, Vector2.Zero, labelScale, SpriteEffects.None, 0f);

        if (rows.Count > 0)
        {
            foreach (var (quest, rect) in rows)
            {
                // Row container drop shadow.
                b.Draw(Game1.staminaRect, new Rectangle(rect.X + 2, rect.Y + 2, rect.Width, rect.Height), Color.Black * 0.18f);

                if (_selectedQuest?.QuestId == quest.QuestId)
                {
                    IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(375, 357, 3, 3), rect.X, rect.Y, rect.Width, rect.Height, Color.White, 4f, false);
                }
                else
                {
                    // Cream parchment card background.
                    b.Draw(Game1.staminaRect, rect, new Color(255, 238, 191));

                    // 2px dark brown border to match Stardew's chunky UI framing.
                    var borderColor = new Color(85, 45, 20);
                    const int borderThickness = 2;

                    // Top
                    b.Draw(Game1.staminaRect, new Rectangle(rect.X, rect.Y, rect.Width, borderThickness), borderColor);
                    // Bottom
                    b.Draw(Game1.staminaRect, new Rectangle(rect.X, rect.Y + rect.Height - borderThickness, rect.Width, borderThickness), borderColor);
                    // Left
                    b.Draw(Game1.staminaRect, new Rectangle(rect.X, rect.Y, borderThickness, rect.Height), borderColor);
                    // Right
                    b.Draw(Game1.staminaRect, new Rectangle(rect.X + rect.Width - borderThickness, rect.Y, borderThickness, rect.Height), borderColor);
                }

                var title = QuestTextHelper.BuildQuestTitle(quest);
                var dueDateText = quest.ExpiresDay > 0
                    ? CalendarDisplayHelper.FormatWeekdayDay(quest.ExpiresDay)
                    : "No deadline";
                var text = isActiveSection
                    ? $"{title} ({dueDateText})"
                    : $"{title}  +{quest.RewardGold}g";

                if (isActiveSection)
                    DrawClockIcon(b, rect.X + 12, rect.Y + 14);
                else
                    DrawCoinIcon(b, rect.X + 12, rect.Y + 14);

                b.DrawString(Game1.smallFont, text, new Vector2(rect.X + 45, rect.Y + 10), Game1.textColor);
            }
        }
        else
        {
            var text = isActiveSection ? "No active requests." : "No requests posted today.";
            b.DrawString(Game1.smallFont, text, new Vector2(labelX, labelY + 45), Game1.textColor * 0.7f);
        }
    }

    private void DrawDetailPanel(SpriteBatch b)
    {
        var panel = GetDetailPanelBounds();
        IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 396, 15, 15), panel.X, panel.Y, panel.Width, panel.Height, Color.White, 4f, false);

        if (_selectedQuest is null)
        {
            var msgSize = Game1.smallFont.MeasureString(_statusMessage);
            b.DrawString(Game1.smallFont, _statusMessage, new Vector2(panel.X + (panel.Width / 2f) - (msgSize.X / 2f), panel.Y + 62), Game1.textColor * 0.5f);
            DrawButton(b, _askWorkButton, "New Postings", enabled: true);
            return;
        }

        var q = _selectedQuest;
        var progress = _rumorBoardService.GetQuestProgress(_state, q.QuestId, Game1.player);

        var lines = new List<string>
        {
            $"Request: {QuestTextHelper.BuildQuestTitle(q)} ({q.Status})",
            $"From: {QuestTextHelper.PrettyName(q.Issuer)} | Reward: +{q.RewardGold}g | Expires {(q.ExpiresDay > 0 ? CalendarDisplayHelper.FormatWeekdayDayWithSeasonYear(q.ExpiresDay) : "No deadline")}",
            q.Summary
        };

        if (progress.Exists && progress.RequiresItems)
            lines.Add($"Progress: {progress.HaveCount}/{progress.NeedCount} {q.TargetItem} (ready={progress.IsReadyToComplete})");
        else if (progress.Exists && q.TemplateId.Equals("social_visit", StringComparison.OrdinalIgnoreCase))
            lines.Add($"Progress: visit {QuestTextHelper.PrettyName(q.TargetItem)} ({progress.HaveCount}/{progress.NeedCount})");

        var y = panel.Y + 16;
        var maxY = _acceptButton.Y - 8;
        foreach (var line in lines)
        {
            var wrapped = TextWrapHelper.WrapText(Game1.smallFont, line, panel.Width - 32);
            foreach (var w in wrapped)
            {
                if (y + 24 > maxY)
                    break;

                b.DrawString(Game1.smallFont, w, new Vector2(panel.X + 16, y), Game1.textColor);
                y += 26;
            }

            if (y + 24 > maxY)
                break;
        }

        DrawButton(b, _acceptButton, "Accept", enabled: q.Status.Equals("available", StringComparison.OrdinalIgnoreCase));
        DrawButton(b, _completeButton, "Complete", enabled: q.Status.Equals("active", StringComparison.OrdinalIgnoreCase));
        DrawButton(b, _askWorkButton, "Refresh", enabled: true);
    }

    private void SyncDetailMessageFromExternalStatus()
    {
        var external = (_getExternalStatus?.Invoke() ?? string.Empty).Trim();
        if (external.IndexOf("No new postings right now", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            _statusMessage = DailyCapDetailMessage;
            _awaitingBoardSearchResult = false;
            return;
        }

        if (_awaitingBoardSearchResult && _state.Quests.Available.Count > _searchStartAvailableCount)
        {
            _statusMessage = DefaultDetailMessage;
            _awaitingBoardSearchResult = false;
            return;
        }

        if (!_awaitingBoardSearchResult || string.IsNullOrWhiteSpace(external))
            return;

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
        IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(432, 439, 9, 9), rect.X, rect.Y, rect.Width, rect.Height, enabled ? Color.White : Color.Gray, 4f, false);

        var size = Game1.smallFont.MeasureString(text);
        var pos = new Vector2(rect.X + (rect.Width - size.X) / 2f, rect.Y + (rect.Height - size.Y) / 2f);
        b.DrawString(Game1.smallFont, text, pos, enabled ? Game1.textColor : Game1.textColor * 0.5f);
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
