using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using StardewLivingRPG.State;
using StardewLivingRPG.Systems;

namespace StardewLivingRPG.UI;

public sealed class MarketBoardMenu : IClickableMenu
{
    private readonly SaveState _state;
    private readonly MarketBoardService _boardService;

    public MarketBoardMenu(SaveState state, MarketBoardService boardService)
        : base(
            xPositionOnScreen: Game1.uiViewport.Width / 2 - 420,
            yPositionOnScreen: Game1.uiViewport.Height / 2 - 280,
            width: 840,
            height: 560,
            showUpperRightCloseButton: true)
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
        SpriteText.drawString(b, header, x, y, 999, width: width - 72);

        y += 56;
        SpriteText.drawString(b, "Top movers:", x, y);

        y += 44;
        foreach (var line in _boardService.BuildTopRows(_state, 8))
        {
            SpriteText.drawString(b, line, x, y, 999, width: width - 72);
            y += 40;
        }

        y += 10;
        SpriteText.drawString(b, "Tip: Check this each morning before selling.", x, y, 999, width: width - 72);

        drawMouse(b);
    }
}
