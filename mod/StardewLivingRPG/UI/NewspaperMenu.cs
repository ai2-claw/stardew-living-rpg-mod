using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using StardewLivingRPG.State;

namespace StardewLivingRPG.UI;

public sealed class NewspaperMenu : IClickableMenu
{
    private readonly NewspaperIssue? _issue;

    public NewspaperMenu(NewspaperIssue? issue)
        : base(
            xPositionOnScreen: Game1.uiViewport.Width / 2 - 420,
            yPositionOnScreen: Game1.uiViewport.Height / 2 - 280,
            width: 840,
            height: 560,
            showUpperRightCloseButton: true)
    {
        _issue = issue;
    }

    public override void draw(SpriteBatch b)
    {
        Game1.drawDialogueBox(xPositionOnScreen, yPositionOnScreen, width, height, false, true);

        var x = xPositionOnScreen + 36;
        var y = yPositionOnScreen + 24;

        SpriteText.drawString(b, "Pelican Times", x, y, 999, width: width - 72);
        y += 56;

        if (_issue is null)
        {
            SpriteText.drawString(b, "No issue published yet. Check again tomorrow morning.", x, y, 999, width: width - 72);
            drawMouse(b);
            return;
        }

        SpriteText.drawString(b, $"Day {_issue.Day}: {_issue.Headline}", x, y, 999, width: width - 72);
        y += 52;

        foreach (var s in _issue.Sections)
        {
            SpriteText.drawString(b, s, x, y, 999, width: width - 72);
            y += 44;
        }

        y += 8;
        SpriteText.drawString(b, "Tomorrow Outlook:", x, y, 999, width: width - 72);
        y += 40;

        foreach (var h in _issue.PredictiveHints)
        {
            SpriteText.drawString(b, $"- {h}", x, y, 999, width: width - 72);
            y += 38;
        }

        drawMouse(b);
    }
}
