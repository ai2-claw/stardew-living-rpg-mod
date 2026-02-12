using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using StardewLivingRPG.State;
using StardewLivingRPG.Utils;

namespace StardewLivingRPG.UI;

public sealed class MarketBoardMenu : IClickableMenu
{
    private readonly SaveState _state;
    private readonly ClickableTextureComponent _closeButton;

    // Layout constants
    private const int MastheadHeight = 100;
    private const int ColumnCount = 2;
    private const int RowCount = 4;
    private const int CropEntryWidth = 270;
    private const int CropEntryHeight = 100;
    private const int ColumnGap = 20;
    private const int RowGap = 16;

    public MarketBoardMenu(SaveState state)
        : base(
            Game1.uiViewport.Width / 2 - 320,
            Game1.uiViewport.Height / 2 - 391,
            640,
            782,
            true)
    {
        _state = state;

        _closeButton = new ClickableTextureComponent(
            new Rectangle(xPositionOnScreen + width - 48, yPositionOnScreen + 68, 48, 48),
            Game1.mouseCursors,
            new Rectangle(337, 494, 12, 12),
            4f);
    }

    public override void performHoverAction(int x, int y)
    {
        base.performHoverAction(x, y);
        _closeButton.tryHover(x, y);
    }

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
        // 1. Draw Standard Menu Background
        Game1.drawDialogueBox(xPositionOnScreen, yPositionOnScreen, width, height, false, true);

        // 2. Draw Paper Background
        var paperRect = new Rectangle(
            xPositionOnScreen + 32,
            yPositionOnScreen + 96,
            width - 64,
            height - 128
        );
        b.Draw(Game1.staminaRect, paperRect, new Color(250, 228, 187));

        // 3. Draw Masthead
        DrawMasthead(b, paperRect);

        // 4. Draw Crops Grid
        var contentY = paperRect.Y + MastheadHeight + 20;
        DrawCropsGrid(b, paperRect, contentY);

        // 5. Draw Close Button
        _closeButton.draw(b);
        drawMouse(b);
    }

    private void DrawMasthead(SpriteBatch b, Rectangle paperRect)
    {
        var centerX = paperRect.Center.X;
        var topY = paperRect.Y + 20;

        // Title
        string title = "Pelican Market Board";
        Vector2 titleSize = Game1.dialogueFont.MeasureString(title);
        Vector2 titlePos = new Vector2(centerX - titleSize.X / 2f, topY);

        b.DrawString(Game1.dialogueFont, title, titlePos + new Vector2(2, 2), new Color(100, 80, 60, 100));
        b.DrawString(Game1.dialogueFont, title, titlePos, new Color(60, 40, 20));

        // Subtitle with date
        string season = Game1.currentSeason;
        if (!string.IsNullOrEmpty(season))
            season = char.ToUpper(season[0]) + season.Substring(1);
        else
            season = "Spring";

        string dateStr = $"Day {_state.Calendar.Day} ({season}) • Price: Free";
        Vector2 dateSize = Game1.smallFont.MeasureString(dateStr);
        b.DrawString(Game1.smallFont, dateStr, new Vector2(centerX - dateSize.X / 2f, topY + 45), new Color(80, 60, 40));

        // Separator Lines
        int lineY = topY + 80;
        b.Draw(Game1.staminaRect, new Rectangle(paperRect.X + 20, lineY, paperRect.Width - 40, 2), Color.Black * 0.6f);
        b.Draw(Game1.staminaRect, new Rectangle(paperRect.X + 20, lineY + 4, paperRect.Width - 40, 1), Color.Black * 0.4f);
    }

    private void DrawCropsGrid(SpriteBatch b, Rectangle paperRect, int startY)
    {
        // Get top crops sorted by TrendEma
        var topCrops = _state.Economy.Crops
            .OrderByDescending(kv => kv.Value.TrendEma)
            .Take(ColumnCount * RowCount)
            .ToList();

        var gridStartX = paperRect.X + (paperRect.Width - (ColumnCount * CropEntryWidth + (ColumnCount - 1) * ColumnGap)) / 2;

        for (int i = 0; i < topCrops.Count; i++)
        {
            int column = i % ColumnCount;
            int row = i / ColumnCount;

            var cropX = gridStartX + column * (CropEntryWidth + ColumnGap);
            var cropY = startY + row * (CropEntryHeight + RowGap);

            var cropName = topCrops[i].Key;
            var cropEntry = topCrops[i].Value;

            DrawCropEntry(b, cropName, cropEntry, new Rectangle(cropX, cropY, CropEntryWidth, CropEntryHeight));
        }
    }

    private void DrawCropEntry(SpriteBatch b, string cropName, CropEconomyEntry entry, Rectangle bounds)
    {
        var x = bounds.X;
        var y = bounds.Y;

        // Background card
        b.Draw(Game1.staminaRect, bounds, new Color(245, 223, 182) * 0.5f);
        b.Draw(Game1.staminaRect, new Rectangle(x, y, bounds.Width, 1), Color.Black * 0.2f);

        // Draw crop sprite with border
        var spritePos = new Vector2(x + 10, y + 12);
        CropSpriteHelper.DrawCropSpriteWithBorder(b, cropName, spritePos, scale: 2f);

        // Crop name and price
        var textX = x + 60;
        var nameY = y + 8;

        // Name
        var displayName = char.ToUpper(cropName[0]) + cropName.Substring(1);
        b.DrawString(Game1.smallFont, displayName, new Vector2(textX, nameY), new Color(60, 40, 20));

        // Price with trend indicator
        var priceText = $"{entry.PriceToday}g";
        var priceColor = GetPriceColor(entry);
        b.DrawString(Game1.dialogueFont, priceText, new Vector2(textX, nameY + 24), priceColor);

        // Trend arrows
        var trendArrow = GetTrendArrow(entry);
        b.DrawString(Game1.smallFont, trendArrow, new Vector2(textX + 70, nameY + 30), GetTrendColor(entry));

        // Mini bar chart (7-day price history)
        var chartX = textX;
        var chartY = nameY + 58;
        var chartWidth = 100;
        var chartHeight = 18;
        DrawPriceBarChart(b, entry, new Rectangle(chartX, chartY, chartWidth, chartHeight));

        // Stats (small, below chart)
        var statsY = chartY + chartHeight + 4;
        var statsText = $"d:{entry.DemandFactor:F2} s:{entry.SupplyPressureFactor:F2}";
        b.DrawString(Game1.smallFont, statsText, new Vector2(chartX, statsY), new Color(100, 80, 70) * 0.8f);

        // Scarcity bonus (if any)
        if (entry.ScarcityBonus > 0)
        {
            var scarcityText = $"sc:+{entry.ScarcityBonus:P0}";
            var scarcityX = chartX + chartWidth - Game1.smallFont.MeasureString(scarcityText).X;
            b.DrawString(Game1.smallFont, scarcityText, new Vector2(scarcityX, statsY), new Color(180, 100, 50) * 0.9f);
        }
    }

    private void DrawPriceBarChart(SpriteBatch b, CropEconomyEntry entry, Rectangle bounds)
    {
        if (entry.PriceHistory7D.Count == 0)
            return;

        var history = entry.PriceHistory7D.ToList();
        var maxPrice = history.Max();
        var minPrice = history.Min();
        var priceRange = maxPrice - minPrice;
        var barWidth = (bounds.Width - 6f) / Math.Max(7, history.Count); // 6px gap total

        for (int i = 0; i < history.Count; i++)
        {
            var price = history[i];
            var barHeight = priceRange > 0
                ? (float)((price - minPrice) / priceRange) * bounds.Height
                : bounds.Height / 2f;

            // Clamp bar height
            barHeight = MathHelper.Clamp(barHeight, 2, bounds.Height);

            var barX = bounds.X + 3 + i * barWidth;
            var barY = bounds.Y + bounds.Height - barHeight;

            // Color: green if up from previous, red if down, neutral otherwise
            var barColor = i == 0 ? Color.Gray :
                          price > history[i - 1] ? new Color(80, 160, 80) :
                          price < history[i - 1] ? new Color(180, 80, 80) : Color.Gray;

            b.Draw(Game1.staminaRect, new Rectangle((int)barX, (int)barY, (int)(barWidth - 1), (int)barHeight), barColor);
        }

        // Chart border
        b.Draw(Game1.staminaRect, bounds, Color.Black * 0.1f);
    }

    private static Color GetPriceColor(CropEconomyEntry entry)
    {
        if (entry.PriceToday > entry.PriceYesterday)
            return new Color(60, 140, 60); // Green
        if (entry.PriceToday < entry.PriceYesterday)
            return new Color(180, 60, 60); // Red
        return new Color(60, 40, 20); // Neutral dark brown
    }

    private static Color GetTrendColor(CropEconomyEntry entry)
    {
        if (entry.TrendEma > 0.02f)
            return new Color(60, 140, 60); // Strong up
        if (entry.TrendEma < -0.02f)
            return new Color(180, 60, 60); // Strong down
        return new Color(100, 80, 70); // Neutral
    }

    private static string GetTrendArrow(CropEconomyEntry entry)
    {
        if (entry.TrendEma > 0.05f)
            return "↑↑↑";
        if (entry.TrendEma > 0.02f)
            return "↑↑";
        if (entry.TrendEma > 0f)
            return "↑";
        if (entry.TrendEma < -0.05f)
            return "↓↓↓";
        if (entry.TrendEma < -0.02f)
            return "↓↓";
        if (entry.TrendEma < 0f)
            return "↓";
        return "→";
    }
}
