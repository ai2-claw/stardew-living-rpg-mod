using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using StardewLivingRPG.Config;
using StardewLivingRPG.State;
using StardewLivingRPG.Utils;

namespace StardewLivingRPG.UI;

public sealed class NewspaperMenu : IClickableMenu
{
    private readonly NewspaperIssue? _issue;
    private readonly ClickableTextureComponent _closeButton;
    private Rectangle _paperRect;
    private Rectangle _contentViewport;
    private Rectangle _scrollTrackRegion;
    private Rectangle _scrollThumbRegion;
    private int _contentScrollOffset;
    private int _contentHeight;
    private bool _scrollThumbHeld;
    private int _scrollThumbDragOffset;

    // Layout constants
    private const int MenuWidth = 640;
    private const int MenuHeight = 822;
    private const int MastheadHeight = 100;
    private const int PaperInsetX = 32;
    private const int PaperInsetTop = 96;
    private const int PaperInsetBottom = 32;
    private const int ContentHorizontalPadding = 30;
    private const int ContentBottomPadding = 20;
    private const int ContentTopSpacingBelowMasthead = 20;
    private const int ScrollBarWidth = 24;
    private const int ScrollBarGap = 8;

    public NewspaperMenu(NewspaperIssue? issue)
        : base(
            Game1.uiViewport.Width / 2 - (MenuWidth / 2),
            Game1.uiViewport.Height / 2 - (MenuHeight / 2),
            MenuWidth,
            MenuHeight,
            true)
    {
        _issue = issue;
        _closeButton = new ClickableTextureComponent(
            Rectangle.Empty,
            Game1.mouseCursors,
            new Rectangle(337, 494, 12, 12),
            4f);
        RecalculateLayout();
    }

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
    {
        base.gameWindowSizeChanged(oldBounds, newBounds);
        RecalculateLayout();
    }

    private void RecalculateLayout()
    {
        xPositionOnScreen = Game1.uiViewport.Width / 2 - (MenuWidth / 2);
        yPositionOnScreen = Game1.uiViewport.Height / 2 - (MenuHeight / 2);
        width = MenuWidth;
        height = MenuHeight;

        _paperRect = new Rectangle(
            xPositionOnScreen + PaperInsetX,
            yPositionOnScreen + PaperInsetTop,
            width - (PaperInsetX * 2),
            height - (PaperInsetTop + PaperInsetBottom));

        var contentY = _paperRect.Y + MastheadHeight + ContentTopSpacingBelowMasthead;
        _contentViewport = new Rectangle(
            _paperRect.X + ContentHorizontalPadding,
            contentY,
            _paperRect.Width - (ContentHorizontalPadding * 2) - ScrollBarGap - ScrollBarWidth,
            _paperRect.Bottom - contentY - ContentBottomPadding);

        _scrollTrackRegion = new Rectangle(
            _contentViewport.Right + ScrollBarGap,
            _contentViewport.Y,
            ScrollBarWidth,
            _contentViewport.Height);

        _closeButton.bounds = new Rectangle(
            xPositionOnScreen + width - 48,
            yPositionOnScreen + 68,
            48,
            48);

        ClampScrollOffset();
        UpdateScrollThumbRegion();
    }

    public override void performHoverAction(int x, int y)
    {
        base.performHoverAction(x, y);
        _closeButton.tryHover(x, y);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);

        if (_scrollThumbRegion.Contains(x, y) && CanScroll())
        {
            _scrollThumbHeld = true;
            _scrollThumbDragOffset = y - _scrollThumbRegion.Y;
            return;
        }

        if (_scrollTrackRegion.Contains(x, y) && CanScroll())
        {
            if (y < _scrollThumbRegion.Y)
                ScrollBy(-_contentViewport.Height / 2);
            else if (y > _scrollThumbRegion.Bottom)
                ScrollBy(_contentViewport.Height / 2);
            return;
        }

        if (_closeButton.containsPoint(x, y))
            exitThisMenu(playSound);
    }

    public override void leftClickHeld(int x, int y)
    {
        base.leftClickHeld(x, y);
        if (!_scrollThumbHeld || !CanScroll())
            return;

        var trackHeight = _scrollTrackRegion.Height - _scrollThumbRegion.Height;
        var newThumbY = Math.Clamp(y - _scrollThumbDragOffset, _scrollTrackRegion.Y, _scrollTrackRegion.Y + trackHeight);
        var scrollPercent = trackHeight > 0 ? (float)(newThumbY - _scrollTrackRegion.Y) / trackHeight : 0f;
        var maxScroll = Math.Max(0, _contentHeight - _contentViewport.Height);
        _contentScrollOffset = (int)(scrollPercent * maxScroll);
    }

    public override void releaseLeftClick(int x, int y)
    {
        base.releaseLeftClick(x, y);
        _scrollThumbHeld = false;
    }

    public override void receiveScrollWheelAction(int direction)
    {
        base.receiveScrollWheelAction(direction);
        if (_contentViewport.Contains(Game1.getMouseX(), Game1.getMouseY()) || _scrollTrackRegion.Contains(Game1.getMouseX(), Game1.getMouseY()))
            ScrollBy(-direction * Game1.smallFont.LineSpacing * 2);
    }

    public override void draw(SpriteBatch b)
    {
        Game1.drawDialogueBox(xPositionOnScreen, yPositionOnScreen, width, height, false, true);

        b.Draw(Game1.staminaRect, _paperRect, new Color(250, 228, 187));

        if (_issue is null)
        {
            DrawError(b, _paperRect);
            _closeButton.draw(b);
            drawMouse(b);
            return;
        }

        DrawMasthead(b, _paperRect);

        var contentStartY = _contentViewport.Y;
        var contentBottom = DrawContent(
            b: null,
            paperRect: _paperRect,
            contentX: _contentViewport.X,
            contentMaxWidth: _contentViewport.Width,
            startY: contentStartY,
            draw: false);
        _contentHeight = Math.Max(0, contentBottom - contentStartY);
        ClampScrollOffset();
        UpdateScrollThumbRegion();

        DrawContentClipped(b);
        DrawScrollbar(b);

        _closeButton.draw(b);
        base.drawMouse(b);
    }

    private bool CanScroll()
    {
        return _contentHeight > _contentViewport.Height;
    }

    private void ScrollBy(int delta)
    {
        var maxScroll = Math.Max(0, _contentHeight - _contentViewport.Height);
        _contentScrollOffset = Math.Clamp(_contentScrollOffset + delta, 0, maxScroll);
        UpdateScrollThumbRegion();
    }

    private void ClampScrollOffset()
    {
        var maxScroll = Math.Max(0, _contentHeight - _contentViewport.Height);
        _contentScrollOffset = Math.Clamp(_contentScrollOffset, 0, maxScroll);
    }

    private void UpdateScrollThumbRegion()
    {
        if (!CanScroll())
        {
            _scrollThumbRegion = Rectangle.Empty;
            return;
        }

        var maxScroll = _contentHeight - _contentViewport.Height;
        var thumbRatio = (float)_contentViewport.Height / _contentHeight;
        var thumbHeight = Math.Max(20, (int)(_scrollTrackRegion.Height * thumbRatio));
        var scrollPercent = maxScroll > 0 ? (float)_contentScrollOffset / maxScroll : 0f;
        var thumbY = _scrollTrackRegion.Y + (int)((_scrollTrackRegion.Height - thumbHeight) * scrollPercent);

        _scrollThumbRegion = new Rectangle(
            _scrollTrackRegion.X + 4,
            thumbY,
            _scrollTrackRegion.Width - 8,
            thumbHeight);
    }

    private void DrawContentClipped(SpriteBatch b)
    {
        var oldScissor = Game1.graphics.GraphicsDevice.ScissorRectangle;
        var rasterizer = new RasterizerState { ScissorTestEnable = true };

        b.End();
        b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, rasterizer);
        Game1.graphics.GraphicsDevice.ScissorRectangle = _contentViewport;

        DrawContent(
            b,
            _paperRect,
            _contentViewport.X,
            _contentViewport.Width,
            _contentViewport.Y - _contentScrollOffset,
            draw: true);

        b.End();
        b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
        Game1.graphics.GraphicsDevice.ScissorRectangle = oldScissor;
    }

    private void DrawScrollbar(SpriteBatch b)
    {
        if (!CanScroll())
            return;

        b.Draw(Game1.staminaRect, _scrollTrackRegion, new Color(139, 90, 43) * 0.3f);
        var thumbColor = _scrollThumbHeld ? new Color(221, 148, 25) : new Color(191, 118, 15);
        b.Draw(Game1.staminaRect, _scrollThumbRegion, thumbColor);
    }

    private int DrawArticles(SpriteBatch? b, Rectangle paperRect, int contentX, int contentWidth, int startY, bool draw)
    {
        if (_issue?.Articles == null || _issue.Articles.Count == 0)
            return startY;

        var y = startY;
        y += 10;

        if (draw && b is not null)
            b.Draw(Game1.staminaRect, new Rectangle(paperRect.X + 20, y, paperRect.Width - 40, 1), Color.Black * 0.3f);
        y += 15;

        if (draw && b is not null)
        {
            b.DrawString(
                Game1.smallFont,
                I18n.Get("newspaper.section.community_news", "Community News"),
                new Vector2(contentX, y),
                new Color(63, 78, 111));
        }
        y += 40;

        var singleColumnY = y;
        foreach (var article in _issue.Articles)
        {
            DrawArticle(b, article, contentX, ref singleColumnY, contentWidth, draw);
            singleColumnY += 16;
        }

        return singleColumnY;
    }

    private void DrawArticle(SpriteBatch? b, NewspaperArticle article, int x, ref int y, int maxWidth, bool draw)
    {
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
            categoryText = I18n.Get("newspaper.category.community", "Community");
        else if (article.Category.Equals("market", StringComparison.OrdinalIgnoreCase))
            categoryText = I18n.Get("newspaper.category.market", "Market");
        else if (article.Category.Equals("social", StringComparison.OrdinalIgnoreCase))
            categoryText = I18n.Get("newspaper.category.social", "Social");
        else if (article.Category.Equals("nature", StringComparison.OrdinalIgnoreCase))
            categoryText = I18n.Get("newspaper.category.nature", "Nature");
        else
            categoryText = I18n.Get("newspaper.category.news", "News");

        var portraitSize = 40;
        var portraitX = x;
        var portraitY = y + 4;

        if (draw && b is not null && !string.IsNullOrEmpty(article.SourceNpc) && article.SourceNpc != "Debug")
        {
            try
            {
                var townProfile = TownProfileResolver.ResolveForLocation(Game1.currentLocation?.Name);
                var npcName = ResolveBylinePortraitNpc(article.SourceNpc, townProfile);
                var npc = Game1.getCharacterFromName(npcName);
                if (npc?.Portrait != null)
                {
                    var sourceRect = new Rectangle(0, 0, 64, 64);
                    b.Draw(npc.Portrait, new Rectangle(portraitX, portraitY, portraitSize, portraitSize), sourceRect, Color.White * 0.9f);
                }
            }
            catch
            {
            }
        }

        var textX = x + portraitSize + 8;
        if (draw && b is not null)
            b.DrawString(Game1.smallFont, $"[{categoryText}]", new Vector2(textX, y), categoryColor * 0.8f);
        y += 28;

        if (draw && b is not null && !string.IsNullOrEmpty(article.SourceNpc))
        {
            var townProfile = TownProfileResolver.ResolveForLocation(Game1.currentLocation?.Name);
            var displayName = article.SourceNpc switch
            {
                "Debug" => I18n.Get("newspaper.byline.anonymous", "Anonymous"),
                _ when IsEditorByline(article.SourceNpc, townProfile) => I18n.Get("newspaper.byline.editor", "Editor"),
                _ => article.SourceNpc
            };
            b.DrawString(Game1.smallFont, displayName, new Vector2(textX, y), new Color(40, 20, 10));
        }
        y += 30;

        var wrappedTitle = TextWrapHelper.WrapText(Game1.smallFont, article.Title, maxWidth);
        foreach (var line in wrappedTitle)
        {
            if (draw && b is not null)
                b.DrawString(Game1.smallFont, line, new Vector2(x, y), new Color(40, 20, 10));
            y += 26;
        }
        y += 8;

        var wrappedContent = TextWrapHelper.WrapText(Game1.smallFont, article.Content, maxWidth);
        foreach (var line in wrappedContent)
        {
            if (draw && b is not null)
                b.DrawString(Game1.smallFont, line, new Vector2(x, y), new Color(60, 50, 40));
            y += 22;
        }
    }

    private void DrawMasthead(SpriteBatch b, Rectangle paperRect)
    {
        var centerX = paperRect.Center.X;
        var topY = paperRect.Y + 20;

        var townProfile = TownProfileResolver.ResolveForLocation(Game1.currentLocation?.Name);
        string title = I18n.Get("newspaper.title", townProfile.NewspaperTitle);
        Vector2 titleSize = Game1.dialogueFont.MeasureString(title);
        Vector2 titlePos = new Vector2(centerX - titleSize.X / 2f, topY);
        
        // Shadow then Text
        b.DrawString(Game1.dialogueFont, title, titlePos + new Vector2(2, 2), new Color(100, 80, 60, 100));
        b.DrawString(Game1.dialogueFont, title, titlePos, new Color(60, 40, 20)); // Dark Brown Ink

        // Subtitle: Date
        var issueDay = _issue?.Day ?? 1;
        var displayDate = CalendarDisplayHelper.FormatSeasonYearWeekdayDay(issueDay);
        string dateStr = I18n.Get(
            "newspaper.subtitle",
            $"Vol. 1 - {displayDate} - Price: Free",
            new { date = displayDate });
        Vector2 dateSize = Game1.smallFont.MeasureString(dateStr);
        b.DrawString(Game1.smallFont, dateStr, new Vector2(centerX - dateSize.X / 2f, topY + 45), new Color(80, 60, 40));

        // Separator Lines
        int lineY = topY + 80;
        b.Draw(Game1.staminaRect, new Rectangle(paperRect.X + 20, lineY, paperRect.Width - 40, 2), Color.Black * 0.6f);
        b.Draw(Game1.staminaRect, new Rectangle(paperRect.X + 20, lineY + 4, paperRect.Width - 40, 1), Color.Black * 0.4f);
    }

    private static string ResolveBylinePortraitNpc(string sourceNpc, TownProfile townProfile)
    {
        if (string.IsNullOrWhiteSpace(sourceNpc))
            return sourceNpc;

        if (sourceNpc.Equals(townProfile.NewspaperTitle, StringComparison.OrdinalIgnoreCase)
            || sourceNpc.Equals(TownProfileResolver.ResolveForLocation("Town").NewspaperTitle, StringComparison.OrdinalIgnoreCase)
            || sourceNpc.Equals(TownProfileResolver.ResolveForLocation("Custom_Ridgeside_RidgesideVillage").NewspaperTitle, StringComparison.OrdinalIgnoreCase))
        {
            return "Lewis";
        }

        if (townProfile.IsEditorSource(sourceNpc)
            || TownProfileResolver.ResolveForLocation("Town").IsEditorSource(sourceNpc)
            || TownProfileResolver.ResolveForLocation("Custom_Ridgeside_RidgesideVillage").IsEditorSource(sourceNpc))
        {
            return "Elliott";
        }

        return sourceNpc;
    }

    private static bool IsEditorByline(string sourceNpc, TownProfile townProfile)
    {
        if (string.IsNullOrWhiteSpace(sourceNpc))
            return false;

        return townProfile.IsEditorSource(sourceNpc)
            || TownProfileResolver.ResolveForLocation("Town").IsEditorSource(sourceNpc)
            || TownProfileResolver.ResolveForLocation("Custom_Ridgeside_RidgesideVillage").IsEditorSource(sourceNpc);
    }

    private int DrawContent(
        SpriteBatch? b,
        Rectangle paperRect,
        int contentX,
        int contentMaxWidth,
        int startY,
        bool draw)
    {
        if (_issue is null)
            return startY;

        var y = startY;

        string headline = _issue.Headline ?? I18n.Get("newspaper.headline.fallback", "Breaking News");
        var wrappedHeadline = TextWrapHelper.WrapText(Game1.dialogueFont, headline, contentMaxWidth);
        foreach (var line in wrappedHeadline)
        {
            if (draw && b is not null)
                b.DrawString(Game1.dialogueFont, line, new Vector2(contentX, y), new Color(40, 20, 10));
            y += 48;
        }
        y += 10;

        if (_issue.Sections != null)
        {
            foreach (var section in _issue.Sections)
            {
                var wrappedBody = TextWrapHelper.WrapText(Game1.smallFont, section, contentMaxWidth);
                foreach (var line in wrappedBody)
                {
                    if (draw && b is not null)
                        b.DrawString(Game1.smallFont, line, new Vector2(contentX, y), new Color(60, 50, 40));
                    y += 28;
                }
                y += 16;
            }
        }

        y += 10;
        if (draw && b is not null)
            b.Draw(Game1.staminaRect, new Rectangle(paperRect.X + 20, y, paperRect.Width - 40, 1), Color.Black * 0.3f);
        y += 20;

        if (_issue.PredictiveHints != null && _issue.PredictiveHints.Any())
        {
            if (draw && b is not null)
            {
                b.DrawString(
                    Game1.smallFont,
                    I18n.Get("newspaper.section.market_outlook", "Market Outlook"),
                    new Vector2(contentX, y),
                    new Color(200, 74, 67));
            }
            y += 30;

            foreach (var hint in _issue.PredictiveHints)
            {
                var bulletText = $"- {hint}";
                var wrappedHint = TextWrapHelper.WrapText(Game1.smallFont, bulletText, contentMaxWidth);
                foreach (var line in wrappedHint)
                {
                    if (draw && b is not null)
                        b.DrawString(Game1.smallFont, line, new Vector2(contentX + 10, y), new Color(70, 60, 50));
                    y += 24;
                }
                y += 4;
            }
        }

        y = DrawArticles(b, paperRect, contentX, contentMaxWidth, y, draw);
        return y;
    }

    private void DrawError(SpriteBatch b, Rectangle paperRect)
    {
        string text = I18n.Get("newspaper.empty.no_issue", "No issue available today.");
        var size = Game1.dialogueFont.MeasureString(text);
        var pos = new Vector2(
            paperRect.Center.X - size.X / 2f,
            paperRect.Center.Y - size.Y / 2f
        );
        b.DrawString(Game1.dialogueFont, text, pos, Color.Black);
    }
}
