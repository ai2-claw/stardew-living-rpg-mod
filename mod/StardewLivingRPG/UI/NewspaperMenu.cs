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
    
    // --- ADDED: Close button component ---
    private readonly ClickableTextureComponent _closeButton;

    // Layout constants
    private const int MastheadHeight = 100;

    public NewspaperMenu(NewspaperIssue? issue)
        : base(
            Game1.uiViewport.Width / 2 - 320,
            Game1.uiViewport.Height / 2 - 391,
            640,
            782,
            true)
    {
        _issue = issue;

        // --- ADDED: Initialize the Red X Close Button ---
        // Positioned top-right, slightly overlapping the border
        _closeButton = new ClickableTextureComponent(
            new Rectangle(xPositionOnScreen + width - 48, yPositionOnScreen + 68, 48, 48),
            Game1.mouseCursors,
            new Rectangle(337, 494, 12, 12),
            4f);
    }

    // --- ADDED: Handle Hover Effects ---
    public override void performHoverAction(int x, int y)
    {
        base.performHoverAction(x, y);
        _closeButton.tryHover(x, y);
    }

    // --- ADDED: Handle Clicks ---
    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);

        if (_closeButton.containsPoint(x, y))
        {
            exitThisMenu(playSound);
        }
    }

    public override void draw(SpriteBatch b)
    {
        // 1. Draw Standard Menu Background (The outer frame)
        Game1.drawDialogueBox(xPositionOnScreen, yPositionOnScreen, width, height, false, true);

        // 2. Draw "Paper" Background (White/Cream rectangle inside)
        var paperRect = new Rectangle(
            xPositionOnScreen + 32, 
            yPositionOnScreen + 96, 
            width - 64, 
            height - 128
        );
        b.Draw(Game1.staminaRect, paperRect, new Color(250, 228, 187)); // Cream/Paper white

        if (_issue is null)
        {
            DrawError(b, paperRect);
            
            // Draw close button even if there is an error
            _closeButton.draw(b);
            drawMouse(b);
            return;
        }

        // 3. Draw Masthead ("The Pelican Times")
        DrawMasthead(b, paperRect);

        // 4. Draw Content
        var contentY = paperRect.Y + MastheadHeight + 20;
        DrawContent(b, paperRect, contentY);

        // 5. Draw Close Button (Standard upper right)
        // --- ADDED: Explicit draw call for the button ---
        _closeButton.draw(b);

        base.drawMouse(b);
    }

    private void DrawMasthead(SpriteBatch b, Rectangle paperRect)
    {
        var centerX = paperRect.Center.X;
        var topY = paperRect.Y + 20;

        // Title: "The Pelican Times"
        string title = "The Pelican Times";
        Vector2 titleSize = Game1.dialogueFont.MeasureString(title);
        Vector2 titlePos = new Vector2(centerX - titleSize.X / 2f, topY);
        
        // Shadow then Text
        b.DrawString(Game1.dialogueFont, title, titlePos + new Vector2(2, 2), new Color(100, 80, 60, 100));
        b.DrawString(Game1.dialogueFont, title, titlePos, new Color(60, 40, 20)); // Dark Brown Ink

        // Subtitle: Date & Season
        string season = Game1.currentSeason;
        if (!string.IsNullOrEmpty(season))
        {
            season = char.ToUpper(season[0]) + season.Substring(1);
        }
        else 
        {
            season = "Spring";
        }

        string dateStr = $"Vol. 1 • {season} {_issue?.Day ?? 1}, Year 1 • Price: Free";
        Vector2 dateSize = Game1.smallFont.MeasureString(dateStr);
        b.DrawString(Game1.smallFont, dateStr, new Vector2(centerX - dateSize.X / 2f, topY + 45), new Color(80, 60, 40));

        // Separator Lines
        int lineY = topY + 80;
        b.Draw(Game1.staminaRect, new Rectangle(paperRect.X + 20, lineY, paperRect.Width - 40, 2), Color.Black * 0.6f);
        b.Draw(Game1.staminaRect, new Rectangle(paperRect.X + 20, lineY + 4, paperRect.Width - 40, 1), Color.Black * 0.4f);
    }

    private void DrawContent(SpriteBatch b, Rectangle paperRect, int startY)
    {
        if (_issue is null) return;

        var x = paperRect.X + 30;
        var y = startY;
        var maxWidth = paperRect.Width - 60; // Margin for text wrapping

        // --- HEADLINE ---
        string headline = _issue.Headline ?? "Breaking News";
        
        // Use TextWrapHelper for Headline
        var wrappedHeadline = TextWrapHelper.WrapText(Game1.dialogueFont, headline, maxWidth);
        
        foreach (var line in wrappedHeadline)
        {
            b.DrawString(Game1.dialogueFont, line, new Vector2(x, y), new Color(40, 20, 10));
            y += 48;
        }
        
        y += 10; 

        // --- MAIN BODY SECTIONS ---
        if (_issue.Sections != null)
        {
            foreach (var section in _issue.Sections) 
            {
                // Use TextWrapHelper for Body Text
                var wrappedBody = TextWrapHelper.WrapText(Game1.smallFont, section, maxWidth);
                
                foreach (var line in wrappedBody)
                {
                    b.DrawString(Game1.smallFont, line, new Vector2(x, y), new Color(60, 50, 40));
                    y += 28;
                }
                y += 16; // Paragraph spacing
            }
        }

        // --- SEPARATOR ---
        y += 10;
        b.Draw(Game1.staminaRect, new Rectangle(x + 40, y, maxWidth - 80, 1), Color.Black * 0.3f);
        y += 20;

        // --- FORECAST / OUTLOOK ---
        if (_issue.PredictiveHints != null && _issue.PredictiveHints.Any())
        {
            b.DrawString(Game1.smallFont, "Market Outlook:", new Vector2(x, y), new Color(50, 30, 20)); 
            y += 30;

            foreach (var hint in _issue.PredictiveHints)
            {
                var bulletText = $"- {hint}";
                
                // Use TextWrapHelper for Hints
                var wrappedHint = TextWrapHelper.WrapText(Game1.smallFont, bulletText, maxWidth);
                
                foreach (var line in wrappedHint)
                {
                    b.DrawString(Game1.smallFont, line, new Vector2(x + 10, y), new Color(70, 60, 50));
                    y += 24;
                }
                y += 4;
            }
        }
    }

    private void DrawError(SpriteBatch b, Rectangle paperRect)
    {
        string text = "No issue available today.";
        var size = Game1.dialogueFont.MeasureString(text);
        var pos = new Vector2(
            paperRect.Center.X - size.X / 2f,
            paperRect.Center.Y - size.Y / 2f
        );
        b.DrawString(Game1.dialogueFont, text, pos, Color.Black);
    }
}