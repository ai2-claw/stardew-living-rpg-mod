using Microsoft.Xna.Framework;
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
            Game1.uiViewport.Width / 2 - 420,
            Game1.uiViewport.Height / 2 - 280,
            840,
            560,
            true)
    {
        _state = state;
    }

    public override void draw(SpriteBatch b)
    {
        Game1.drawDialogueBox(xPositionOnScreen, yPositionOnScreen, width, height, false, true);

        var x = xPositionOnScreen + 36;
        var y = yPositionOnScreen + 24;

        b.DrawString(Game1.dialogueFont, "Community Board 2.0 (Rumor Mill)", new Vector2(x, y), Game1.textColor);
        y += 52;

        b.DrawString(Game1.smallFont, "Available:", new Vector2(x, y), Game1.textColor);
        y += 40;

        if (_state.Quests.Available.Count == 0)
        {
            b.DrawString(Game1.smallFont, "No rumors posted today.", new Vector2(x, y), Game1.textColor * 0.8f);
            y += 36;
        }
        else
        {
            foreach (var q in _state.Quests.Available.Take(5))
            {
                b.DrawString(Game1.smallFont, $"- {q.QuestId}: {q.Summary}", new Vector2(x, y), Game1.textColor);
                y += 34;
            }
        }

        y += 10;
        b.DrawString(Game1.smallFont, "Active:", new Vector2(x, y), Game1.textColor);
        y += 40;

        if (_state.Quests.Active.Count == 0)
        {
            b.DrawString(Game1.smallFont, "No active rumor quests.", new Vector2(x, y), Game1.textColor * 0.8f);
            y += 36;
        }
        else
        {
            foreach (var q in _state.Quests.Active.Take(4))
            {
                b.DrawString(Game1.smallFont, $"- {q.QuestId} (expires day {q.ExpiresDay})", new Vector2(x, y), Game1.textColor);
                y += 34;
            }
        }

        y += 12;
        b.DrawString(Game1.smallFont, "Use console: slrpg_accept_quest <questId>", new Vector2(x, y), Game1.textColor * 0.7f);
        y += 34;
        b.DrawString(Game1.smallFont, "Use console: slrpg_complete_quest <questId>", new Vector2(x, y), Game1.textColor * 0.7f);

        drawMouse(b);
    }
}
