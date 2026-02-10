using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using StardewLivingRPG.State;
using StardewLivingRPG.Systems;
using StardewLivingRPG.Utils;

namespace StardewLivingRPG.UI;

public sealed class MarketBoardMenu : IClickableMenu
{
    private readonly SaveState _state;
    private readonly MarketBoardService _boardService;

    public MarketBoardMenu(SaveState state, MarketBoardService boardService)
        : base(
            Game1.uiViewport.Width / 2 - 420,
            Game1.uiViewport.Height / 2 - 280,
            840,
            560,
            true)
    {
        _state = state;
        _boardService = boardService;
    }

    public override void draw(SpriteBatch b)
    {
        Game1.drawDialogueBox(xPositionOnScreen, yPositionOnScreen, width, height, false, true);

        var x = xPositionOnScreen + 36;
        var y = yPositionOnScreen + 24;

        var header = $"Pelican Market Board — Day {_state.Calendar.Day} ({_state.Calendar.Season})";
        b.DrawString(Game1.dialogueFont, header, new Vector2(x, y), Game1.textColor);

        y += 56;
        b.DrawString(Game1.smallFont, "Top movers:", new Vector2(x, y), Game1.textColor);

        y += 44;
        var availableWidth = width - 72;
        foreach (var line in _boardService.BuildTopRows(_state, 8))
        {
            var wrappedLines = TextWrapHelper.WrapText(Game1.smallFont, line, availableWidth);
            foreach (var wrappedLine in wrappedLines)
            {
                b.DrawString(Game1.smallFont, wrappedLine, new Vector2(x, y), Game1.textColor);
                y += 40;
            }
        }

        y += 10;
        b.DrawString(Game1.smallFont, "Tip: Check this each morning before selling.", new Vector2(x, y), Game1.textColor * 0.8f);

        drawMouse(b);
    }
}
