using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using StardewLivingRPG.State;

namespace StardewLivingRPG.UI;

public sealed class RumorBoardMenu : IClickableMenu
{
    private readonly SaveState _state;

    public RumorBoardMenu(SaveState state)
        : base(
            xPositionOnScreen: Game1.uiViewport.Width / 2 - 420,
            yPositionOnScreen: Game1.uiViewport.Height / 2 - 280,
            width: 840,
            height: 560,
            showUpperRightCloseButton: true)
    {
        _state = state;
    }

    public override void draw(SpriteBatch b)
    {
        Game1.drawDialogueBox(xPositionOnScreen, yPositionOnScreen, width, height, false, true);

        var x = xPositionOnScreen + 36;
        var y = yPositionOnScreen + 24;

        SpriteText.drawString(b, "Community Board 2.0 (Rumor Mill)", x, y, 999, width: width - 72);
        y += 52;

        SpriteText.drawString(b, "Available:", x, y, 999, width: width - 72);
        y += 40;

        if (_state.Quests.Available.Count == 0)
        {
            SpriteText.drawString(b, "No rumors posted today.", x, y, 999, width: width - 72);
            y += 36;
        }
        else
        {
            foreach (var q in _state.Quests.Available.Take(5))
            {
                SpriteText.drawString(b, $"- {q.QuestId}: {q.Summary}", x, y, 999, width: width - 72);
                y += 34;
            }
        }

        y += 10;
        SpriteText.drawString(b, "Active:", x, y, 999, width: width - 72);
        y += 40;

        if (_state.Quests.Active.Count == 0)
        {
            SpriteText.drawString(b, "No active rumor quests.", x, y, 999, width: width - 72);
            y += 36;
        }
        else
        {
            foreach (var q in _state.Quests.Active.Take(4))
            {
                SpriteText.drawString(b, $"- {q.QuestId} (expires day {q.ExpiresDay})", x, y, 999, width: width - 72);
                y += 34;
            }
        }

        y += 12;
        SpriteText.drawString(b, "Use console: slrpg_accept_quest <questId>", x, y, 999, width: width - 72);
        y += 34;
        SpriteText.drawString(b, "Use console: slrpg_complete_quest <questId>", x, y, 999, width: width - 72);

        drawMouse(b);
    }
}
