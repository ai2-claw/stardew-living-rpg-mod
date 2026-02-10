using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using StardewLivingRPG.State;
using StardewLivingRPG.Utils;

namespace StardewLivingRPG.UI;

public sealed class NewspaperMenu : IClickableMenu
{
    private readonly NewspaperIssue? _issue;

    public NewspaperMenu(NewspaperIssue? issue)
        : base(
            Game1.uiViewport.Width / 2 - 420,
            Game1.uiViewport.Height / 2 - 280,
            840,
            560,
            true)
    {
        _issue = issue;
    }

    public override void draw(SpriteBatch b)
    {
        Game1.drawDialogueBox(xPositionOnScreen, yPositionOnScreen, width, height, false, true);

        var x = xPositionOnScreen + 36;
        var y = yPositionOnScreen + 24;

        b.DrawString(Game1.dialogueFont, "Pelican Times", new Vector2(x, y), Game1.textColor);
        y += 56;

        if (_issue is null)
        {
            b.DrawString(Game1.smallFont, "No issue published yet. Check again tomorrow morning.", new Vector2(x, y), Game1.textColor * 0.8f);
            drawMouse(b);
            return;
        }

        var headline = $"Day {_issue.Day}: {_issue.Headline}";
        var wrappedHeadline = TextWrapHelper.WrapText(Game1.dialogueFont, headline, width - 72);
        foreach (var line in wrappedHeadline)
        {
            b.DrawString(Game1.dialogueFont, line, new Vector2(x, y), Game1.textColor);
            y += 52;
        }

        foreach (var s in _issue.Sections)
        {
            var wrappedSection = TextWrapHelper.WrapText(Game1.smallFont, s, width - 72);
            foreach (var line in wrappedSection)
            {
                b.DrawString(Game1.smallFont, line, new Vector2(x, y), Game1.textColor);
                y += 44;
            }
        }

        y += 8;
        b.DrawString(Game1.dialogueFont, "Tomorrow Outlook:", new Vector2(x, y), Game1.textColor);
        y += 40;

        foreach (var h in _issue.PredictiveHints)
        {
            b.DrawString(Game1.smallFont, $"- {h}", new Vector2(x, y), Game1.textColor);
            y += 38;
        }

        drawMouse(b);
    }
}
