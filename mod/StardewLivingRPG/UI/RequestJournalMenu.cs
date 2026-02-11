using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewLivingRPG.State;
using StardewLivingRPG.Systems;
using StardewLivingRPG.Utils;

namespace StardewLivingRPG.UI;

public sealed class RequestJournalMenu : IClickableMenu
{
    private readonly SaveState _state;
    private readonly RumorBoardService _rumorBoardService;
    private readonly IMonitor _monitor;

    private readonly Rectangle _tabActive;
    private readonly Rectangle _tabCompleted;
    private readonly Rectangle _completeButton;

    private bool _showCompleted;
    private QuestEntry? _selectedQuest;
    private string _status = "Select a request.";

    public RequestJournalMenu(SaveState state, RumorBoardService rumorBoardService, IMonitor monitor)
        : base(
            Game1.uiViewport.Width / 2 - 460,
            Game1.uiViewport.Height / 2 - 290,
            920,
            580,
            true)
    {
        _state = state;
        _rumorBoardService = rumorBoardService;
        _monitor = monitor;

        _tabActive = new Rectangle(xPositionOnScreen + 24, yPositionOnScreen + 20, 140, 36);
        _tabCompleted = new Rectangle(xPositionOnScreen + 172, yPositionOnScreen + 20, 170, 36);
        _completeButton = new Rectangle(xPositionOnScreen + width - 220, yPositionOnScreen + height - 64, 180, 40);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);

        if (_tabActive.Contains(x, y))
        {
            _showCompleted = false;
            _selectedQuest = null;
            Game1.playSound("smallSelect");
            return;
        }

        if (_tabCompleted.Contains(x, y))
        {
            _showCompleted = true;
            _selectedQuest = null;
            Game1.playSound("smallSelect");
            return;
        }

        var list = _showCompleted ? _state.Quests.Completed : _state.Quests.Active;
        var rows = BuildRows(list);
        foreach (var (q, rect) in rows)
        {
            if (!rect.Contains(x, y))
                continue;
            _selectedQuest = q;
            Game1.playSound("smallSelect");
            return;
        }

        if (!_showCompleted && _selectedQuest is not null && _completeButton.Contains(x, y))
        {
            var result = _rumorBoardService.CompleteQuestWithChecks(_state, _selectedQuest.QuestId, Game1.player, consumeItems: true);
            _status = result.Message;
            if (result.Success)
            {
                _monitor.Log(result.Message, LogLevel.Info);
                Game1.playSound("reward");
                _selectedQuest = null;
            }
            else
            {
                Game1.playSound("cancel");
            }
        }
    }

    public override void draw(SpriteBatch b)
    {
        Game1.drawDialogueBox(xPositionOnScreen, yPositionOnScreen, width, height, false, true);
        b.DrawString(Game1.dialogueFont, "Request Journal", new Vector2(xPositionOnScreen + 24, yPositionOnScreen + 68), Game1.textColor);

        DrawTab(b, _tabActive, "Active", !_showCompleted);
        DrawTab(b, _tabCompleted, "Completed", _showCompleted);

        var list = _showCompleted ? _state.Quests.Completed : _state.Quests.Active;
        var rows = BuildRows(list);

        if (rows.Count == 0)
        {
            var empty = _showCompleted ? "No completed requests yet." : "No active requests.";
            b.DrawString(Game1.smallFont, empty, new Vector2(xPositionOnScreen + 24, yPositionOnScreen + 110), Game1.textColor * 0.8f);
        }
        else
        {
            foreach (var (q, rect) in rows)
            {
                var bg = _selectedQuest?.QuestId == q.QuestId ? Color.CadetBlue * 0.35f : Color.Black * 0.2f;
                b.Draw(Game1.staminaRect, rect, bg);

                var title = QuestTextHelper.BuildQuestTitle(q);
                var progress = _rumorBoardService.GetQuestProgress(_state, q.QuestId, Game1.player);
                var line = _showCompleted
                    ? $"{title}  +{q.RewardGold}g"
                    : progress.RequiresItems
                        ? $"{title}  {progress.HaveCount}/{progress.NeedCount}  (day {q.ExpiresDay})"
                        : $"{title}  (day {q.ExpiresDay})";

                var color = (!_showCompleted && q.ExpiresDay > 0 && q.ExpiresDay - _state.Calendar.Day <= 1)
                    ? Color.OrangeRed
                    : Game1.textColor;

                b.DrawString(Game1.smallFont, line, new Vector2(rect.X + 8, rect.Y + 8), color);
            }
        }

        DrawDetail(b);

        b.DrawString(Game1.smallFont, _status, new Vector2(xPositionOnScreen + 24, yPositionOnScreen + height - 22), Game1.textColor * 0.75f);
        drawMouse(b);
    }

    private List<(QuestEntry Quest, Rectangle Rect)> BuildRows(List<QuestEntry> source)
    {
        var rows = new List<(QuestEntry Quest, Rectangle Rect)>();
        var x = xPositionOnScreen + 24;
        var y = yPositionOnScreen + 108;
        var w = width - 48;
        var h = 34;

        foreach (var q in source.Take(10))
        {
            rows.Add((q, new Rectangle(x, y, w, h)));
            y += h + 6;
        }

        return rows;
    }

    private void DrawDetail(SpriteBatch b)
    {
        var panel = new Rectangle(xPositionOnScreen + 24, yPositionOnScreen + height - 160, width - 48, 120);
        b.Draw(Game1.staminaRect, panel, Color.Black * 0.16f);

        if (_selectedQuest is null)
        {
            b.DrawString(Game1.smallFont, "Select a request to view details.", new Vector2(panel.X + 12, panel.Y + 10), Game1.textColor * 0.8f);
            return;
        }

        var q = _selectedQuest;
        var title = QuestTextHelper.BuildQuestTitle(q);
        var progress = _rumorBoardService.GetQuestProgress(_state, q.QuestId, Game1.player);

        b.DrawString(Game1.smallFont, title, new Vector2(panel.X + 12, panel.Y + 10), Game1.textColor);
        b.DrawString(Game1.smallFont, $"Reward +{q.RewardGold}g | Expires day {q.ExpiresDay} | From {q.Issuer}", new Vector2(panel.X + 12, panel.Y + 34), Game1.textColor);
        if (progress.RequiresItems)
            b.DrawString(Game1.smallFont, $"Progress: {progress.HaveCount}/{progress.NeedCount} {q.TargetItem}", new Vector2(panel.X + 12, panel.Y + 58), Game1.textColor);

        if (!_showCompleted)
            DrawActionButton(b, _completeButton, "Complete Request", _selectedQuest.Status.Equals("active", StringComparison.OrdinalIgnoreCase));
    }

    private static void DrawActionButton(SpriteBatch b, Rectangle rect, string text, bool enabled)
    {
        var bg = enabled ? Color.DarkOliveGreen * 0.7f : Color.DimGray * 0.45f;
        b.Draw(Game1.staminaRect, rect, bg);
        var size = Game1.smallFont.MeasureString(text);
        b.DrawString(Game1.smallFont, text, new Vector2(rect.X + (rect.Width - size.X) / 2f, rect.Y + (rect.Height - size.Y) / 2f), Color.White * (enabled ? 1f : 0.7f));
    }

    private static void DrawTab(SpriteBatch b, Rectangle rect, string text, bool active)
    {
        var bg = active ? Color.SteelBlue * 0.8f : Color.Black * 0.25f;
        b.Draw(Game1.staminaRect, rect, bg);
        var size = Game1.smallFont.MeasureString(text);
        b.DrawString(Game1.smallFont, text, new Vector2(rect.X + (rect.Width - size.X) / 2f, rect.Y + (rect.Height - size.Y) / 2f), Color.White);
    }
}
