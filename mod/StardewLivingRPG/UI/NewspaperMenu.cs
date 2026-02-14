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
        _closeButton.draw(b);
        base.drawMouse(b);
    }

    private int DrawArticles(SpriteBatch b, Rectangle paperRect, int startY)
    {
        if (_issue?.Articles == null || _issue.Articles.Count == 0)
            return startY;

        var y = startY;
        y += 10;

        // Horizontal separator line (match Market Outlook separator style)
        b.Draw(Game1.staminaRect, new Rectangle(paperRect.X + 20, y, paperRect.Width - 40, 1), Color.Black * 0.3f);
        y += 15;

        // Section header (smaller)
        b.DrawString(Game1.smallFont, "Community News", new Vector2(paperRect.X + 30, y), new Color(50, 30, 20));
        y += 50; // Extra spacing for portrait (portrait is 40px tall, category badge is ~14px)

        var useSingleColumn = _issue.Articles.Any(a => a.IsNpcPublished);
        if (useSingleColumn)
        {
            var singleColumnX = paperRect.X + 30;
            var singleColumnWidth = paperRect.Width - 60;
            var singleColumnY = y;
            foreach (var article in _issue.Articles)
            {
                DrawArticle(b, article, singleColumnX, ref singleColumnY, singleColumnWidth);
                singleColumnY += 16;
            }

            return singleColumnY;
        }

        // Two-column layout
        var columnGap = 30;
        var sideMargin = 30;
        var columnWidth = (paperRect.Width - sideMargin * 2 - columnGap) / 2;
        var leftColumnX = paperRect.X + sideMargin;
        var rightColumnX = leftColumnX + columnWidth + columnGap;

        var leftColumnY = y;
        var rightColumnY = y;

        for (int i = 0; i < _issue.Articles.Count; i++)
        {
            var article = _issue.Articles[i];
            var isLeftColumn = i % 2 == 0;
            var columnX = isLeftColumn ? leftColumnX : rightColumnX;
            var columnY = isLeftColumn ? ref leftColumnY : ref rightColumnY;

            DrawArticle(b, article, columnX, ref columnY, columnWidth);
            columnY += 12; // Spacing after article
        }

        // Return the height of the taller column
        return Math.Max(leftColumnY, rightColumnY);
    }

    private void DrawArticle(SpriteBatch b, NewspaperArticle article, int x, ref int y, int maxWidth)
    {
        var startY = y;

        // Category badge (smaller)
        var categoryColor = article.Category.ToLowerInvariant() switch
        {
            "community" => new Color(60, 140, 60),
            "market" => new Color(180, 120, 40),
            "social" => new Color(100, 80, 180),
            "nature" => new Color(40, 140, 80),
            _ => new Color(80, 80, 80)
        };

        string categoryText;
        if (article.Category.Equals("community", StringComparison.OrdinalIgnoreCase))
            categoryText = "Community";
        else if (article.Category.Equals("market", StringComparison.OrdinalIgnoreCase))
            categoryText = "Market";
        else if (article.Category.Equals("social", StringComparison.OrdinalIgnoreCase))
            categoryText = "Social";
        else if (article.Category.Equals("nature", StringComparison.OrdinalIgnoreCase))
            categoryText = "Nature";
        else
            categoryText = "News";

        // Portrait and header row
        var portraitSize = 40;
        var portraitX = x;
        var portraitY = y + 4;

        // Load and draw NPC portrait if available
        if (!string.IsNullOrEmpty(article.SourceNpc) && article.SourceNpc != "Debug")
        {
            try
            {
                // Placeholder portrait mappings for non-vanilla bylines.
                var npcName = article.SourceNpc switch
                {
                    "The Pelican Times" => "Lewis",
                    "Pelican Times Editor" => "Elliott",
                    _ => article.SourceNpc
                };
                var npc = Game1.getCharacterFromName(npcName);
                if (npc?.Portrait != null)
                {
                    // NPC portraits are typically 64x64 per frame, draw the default neutral one (frame 0)
                    var sourceRect = new Rectangle(0, 0, 64, 64);
                    b.Draw(npc.Portrait, new Rectangle(portraitX, portraitY, portraitSize, portraitSize), sourceRect, Color.White * 0.9f);
                }
            }
            catch
            {
                // Silently fallback if portrait can't be loaded
            }
        }

        // Draw category and name to the right of portrait
        var textX = x + portraitSize + 8;
        var textMaxWidth = maxWidth - portraitSize - 8;

        b.DrawString(Game1.smallFont, $"[{categoryText}]", new Vector2(textX, y), categoryColor * 0.8f);
        y += 28;

        // NPC name
        if (!string.IsNullOrEmpty(article.SourceNpc))
        {
            var displayName = article.SourceNpc switch
            {
                "Debug" => "Anonymous",
                "Pelican Times Editor" => "Editor",
                _ => article.SourceNpc
            };
            b.DrawString(Game1.smallFont, displayName, new Vector2(textX, y), new Color(40, 20, 10));
        }
        y += 30; // Spacing after portrait row

        // Title
        var wrappedTitle = TextWrapHelper.WrapText(Game1.smallFont, article.Title, maxWidth);
        foreach (var line in wrappedTitle)
        {
            b.DrawString(Game1.smallFont, line, new Vector2(x, y), new Color(40, 20, 10));
            y += 26;
        }
        y += 8; // Spacing between title and content

        // Content
        var wrappedContent = TextWrapHelper.WrapText(Game1.smallFont, article.Content, maxWidth);
        foreach (var line in wrappedContent)
        {
            b.DrawString(Game1.smallFont, line, new Vector2(x, y), new Color(60, 50, 40));
            y += 22;
        }
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
        b.Draw(Game1.staminaRect, new Rectangle(paperRect.X + 20, y, paperRect.Width - 40, 1), Color.Black * 0.3f);
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

        // --- AI-GENERATED ARTICLES SECTION ---
        y = DrawArticles(b, paperRect, y);
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
