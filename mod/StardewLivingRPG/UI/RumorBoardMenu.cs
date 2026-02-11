using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewLivingRPG.State;
using StardewLivingRPG.Systems;
using StardewLivingRPG.Utils;

namespace StardewLivingRPG.UI;

public sealed class RumorBoardMenu : IClickableMenu
{
    private readonly SaveState _state;
    private readonly RumorBoardService _rumorBoardService;
    private readonly IMonitor _monitor;
    private readonly Action _onAskMayorForWork;

    private readonly List<(QuestEntry Quest, Rectangle Rect)> _availableRows = new();
    private readonly List<(QuestEntry Quest, Rectangle Rect)> _activeRows = new();

    private QuestEntry? _selectedQuest;
    private string _statusMessage = "Select a Town Request to view details.";

    private Rectangle _acceptButton;
    private Rectangle _completeButton;
    private Rectangle _askWorkButton;

    public RumorBoardMenu(SaveState state, RumorBoardService rumorBoardService, IMonitor monitor, Action onAskMayorForWork)
        : base(
            Game1.uiViewport.Width / 2 - 480,
            Game1.uiViewport.Height / 2 - 300,
            960,
            600,
            true)
    {
        _state = state;
        _rumorBoardService = rumorBoardService;
        _monitor = monitor;
        _onAskMayorForWork = onAskMayorForWork;
        BuildLayout();
    }

    private void BuildLayout()
    {
        _availableRows.Clear();
        _activeRows.Clear();

        var leftX = xPositionOnScreen + 24;
        var topY = yPositionOnScreen + 72;
        var sectionWidth = 430;

        var rowHeight = 36;
        var rowGap = 6;

        var ay = topY + 34;
        foreach (var q in _state.Quests.Available.Take(8))
        {
            _availableRows.Add((q, new Rectangle(leftX, ay, sectionWidth, rowHeight)));
            ay += rowHeight + rowGap;
        }

        var rightX = xPositionOnScreen + width - sectionWidth - 24;
        var ry = topY + 34;
        foreach (var q in _state.Quests.Active.Take(8))
        {
            _activeRows.Add((q, new Rectangle(rightX, ry, sectionWidth, rowHeight)));
            ry += rowHeight + rowGap;
        }

        var detailY = yPositionOnScreen + height - 188;
        _acceptButton = new Rectangle(xPositionOnScreen + 36, detailY + 128, 160, 40);
        _completeButton = new Rectangle(xPositionOnScreen + 212, detailY + 128, 190, 40);
        _askWorkButton = new Rectangle(xPositionOnScreen + width - 250, detailY + 128, 210, 40);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);

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
            _onAskMayorForWork();
            _statusMessage = "Looking over the board for new postings...";
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

    public override void draw(SpriteBatch b)
    {
        Game1.drawDialogueBox(xPositionOnScreen, yPositionOnScreen, width, height, false, true);

        var titleX = xPositionOnScreen + 24;
        var titleY = yPositionOnScreen + 20;
        b.DrawString(Game1.dialogueFont, "Town Request Board", new Vector2(titleX, titleY), Game1.textColor);

        DrawSection(b, "Available", _availableRows, isActiveSection: false);
        DrawSection(b, "Active", _activeRows, isActiveSection: true);

        DrawDetailPanel(b);

        b.DrawString(Game1.smallFont, _statusMessage, new Vector2(xPositionOnScreen + 24, yPositionOnScreen + height - 22), Game1.textColor * 0.75f);

        drawMouse(b);
    }

    private void DrawSection(SpriteBatch b, string label, List<(QuestEntry Quest, Rectangle Rect)> rows, bool isActiveSection)
    {
        var labelX = rows.Count > 0 ? rows[0].Rect.X : (isActiveSection ? xPositionOnScreen + width - 454 : xPositionOnScreen + 24);
        var labelY = yPositionOnScreen + 72;

        b.DrawString(Game1.smallFont, label + ":", new Vector2(labelX, labelY), Game1.textColor);

        if (rows.Count > 0)
        {
            foreach (var (quest, rect) in rows)
            {
                var bg = _selectedQuest?.QuestId == quest.QuestId ? Color.CornflowerBlue * 0.35f : Color.Black * 0.22f;
                b.Draw(Game1.staminaRect, rect, bg);

                var title = QuestTextHelper.BuildQuestTitle(quest);
                var text = isActiveSection
                    ? $"{title} (day {quest.ExpiresDay})"
                    : $"{title}  +{quest.RewardGold}g";

                b.DrawString(Game1.smallFont, text, new Vector2(rect.X + 8, rect.Y + 8), Game1.textColor);
            }
        }
        else
        {
            var emptyX = labelX;
            var emptyY = labelY + 38;
            var text = isActiveSection ? "No active requests." : "No requests posted today.";
            b.DrawString(Game1.smallFont, text, new Vector2(emptyX, emptyY), Game1.textColor * 0.8f);
        }
    }

    private void DrawDetailPanel(SpriteBatch b)
    {
        var panel = new Rectangle(xPositionOnScreen + 24, yPositionOnScreen + height - 190, width - 48, 160);
        b.Draw(Game1.staminaRect, panel, Color.Black * 0.18f);

        if (_selectedQuest is null)
        {
            b.DrawString(Game1.smallFont, "Select a request to view details and actions.", new Vector2(panel.X + 12, panel.Y + 12), Game1.textColor * 0.8f);
            return;
        }

        var q = _selectedQuest;
        var progress = _rumorBoardService.GetQuestProgress(_state, q.QuestId, Game1.player);

        var lines = new List<string>
        {
            $"Request: {QuestTextHelper.BuildQuestTitle(q)} ({q.Status})",
            $"From: {q.Issuer} | Reward: +{q.RewardGold}g | Expires day {q.ExpiresDay}",
            q.Summary,
            $"Reference: {q.QuestId}"
        };

        if (progress.Exists && progress.RequiresItems)
            lines.Add($"Progress: {progress.HaveCount}/{progress.NeedCount} {q.TargetItem} (ready={progress.IsReadyToComplete})");

        var y = panel.Y + 12;
        foreach (var line in lines)
        {
            var wrapped = TextWrapHelper.WrapText(Game1.smallFont, line, panel.Width - 20);
            foreach (var w in wrapped)
            {
                b.DrawString(Game1.smallFont, w, new Vector2(panel.X + 12, y), Game1.textColor);
                y += 24;
            }
        }

        DrawButton(b, _acceptButton, "Accept", enabled: q.Status.Equals("available", StringComparison.OrdinalIgnoreCase));
        DrawButton(b, _completeButton, "Complete", enabled: q.Status.Equals("active", StringComparison.OrdinalIgnoreCase));
        DrawButton(b, _askWorkButton, "New Postings", enabled: true);
    }

    private static void DrawButton(SpriteBatch b, Rectangle rect, string text, bool enabled)
    {
        var bg = enabled ? Color.DarkSlateBlue * 0.65f : Color.DimGray * 0.45f;
        b.Draw(Game1.staminaRect, rect, bg);

        var size = Game1.smallFont.MeasureString(text);
        var tx = rect.X + (rect.Width - size.X) / 2f;
        var ty = rect.Y + (rect.Height - size.Y) / 2f;
        b.DrawString(Game1.smallFont, text, new Vector2(tx, ty), Color.White * (enabled ? 1f : 0.7f));
    }

}
