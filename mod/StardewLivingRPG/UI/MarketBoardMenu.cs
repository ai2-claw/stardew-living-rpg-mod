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
    private const int CropEntryHeight = 112;
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

        string dateStr = $"Day {_state.Calendar.Day} ({season}) - Price: Free";
        Vector2 dateSize = Game1.smallFont.MeasureString(dateStr);
        b.DrawString(Game1.smallFont, dateStr, new Vector2(centerX - dateSize.X / 2f, topY + 45), new Color(80, 60, 40));

        // Separator Lines
        int lineY = topY + 80;
        b.Draw(Game1.staminaRect, new Rectangle(paperRect.X + 20, lineY, paperRect.Width - 40, 2), Color.Black * 0.6f);
        b.Draw(Game1.staminaRect, new Rectangle(paperRect.X + 20, lineY + 4, paperRect.Width - 40, 1), Color.Black * 0.4f);
    }

    private void DrawCropsGrid(SpriteBatch b, Rectangle paperRect, int startY)
    {
        var topCrops = SelectTopBoardEntries();

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
        var displayName = QuestTextHelper.PrettyName(cropName);
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

        // Public cause text (small, below chart) - avoids exposing internal tuning values.
        var statsY = chartY + chartHeight + 4;
        var causeText = BuildPublicMarketCause(cropName, entry);
        var wrappedCause = TextWrapHelper.WrapText(Game1.smallFont, causeText, bounds.Width - 68);
        for (int i = 0; i < Math.Min(2, wrappedCause.Length); i++)
        {
            b.DrawString(
                Game1.smallFont,
                wrappedCause[i],
                new Vector2(chartX, statsY + i * 14),
                new Color(100, 80, 70) * 0.85f);
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
            return "^^^";
        if (entry.TrendEma > 0.02f)
            return "^^";
        if (entry.TrendEma > 0f)
            return "^";
        if (entry.TrendEma < -0.05f)
            return "vvv";
        if (entry.TrendEma < -0.02f)
            return "vv";
        if (entry.TrendEma < 0f)
            return "v";
        return "->";
    }

    private string BuildPublicMarketCause(string cropName, CropEconomyEntry entry)
    {
        var activeEvent = _state.Economy.MarketEvents
            .Where(ev =>
                ev.Crop.Equals(cropName, StringComparison.OrdinalIgnoreCase)
                && _state.Calendar.Day >= ev.StartDay
                && _state.Calendar.Day <= ev.EndDay)
            .OrderByDescending(ev => Math.Abs(ev.DeltaPct))
            .FirstOrDefault();
        if (activeEvent is not null)
            return DescribeMarketEventCause(activeEvent);

        if (entry.PriceToday > entry.PriceYesterday && entry.ScarcityBonus >= 0.03f)
            return "Cause: short supply raised demand.";
        if (entry.PriceToday < entry.PriceYesterday && entry.SupplyPressureFactor <= 0.95f)
            return "Cause: extra stock softened prices.";
        if (entry.TrendEma > 0.03f)
            return "Cause: steady local buying.";
        if (entry.TrendEma < -0.03f)
            return "Cause: slower buying this week.";

        return "Cause: normal market chatter.";
    }

    private static string DescribeMarketEventCause(MarketEventEntry marketEvent)
    {
        var type = (marketEvent.Type ?? string.Empty).Trim().ToLowerInvariant();
        return type switch
        {
            "npc_market_modifier" => marketEvent.DeltaPct >= 0f
                ? "Cause: town request boosted demand."
                : "Cause: oversupply rumor eased prices.",
            _ => marketEvent.DeltaPct >= 0f
                ? "Cause: market buzz pushed prices up."
                : "Cause: market buzz pushed prices down."
        };
    }

    private List<KeyValuePair<string, CropEconomyEntry>> SelectTopBoardEntries()
    {
        return _state.Economy.Crops
            .Select(kv => new
            {
                kv.Key,
                kv.Value,
                Score = ComputeBoardRelevance(kv.Value),
                TieBreaker = ComputeDailyTieBreaker(kv.Key, _state.Calendar.Day)
            })
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => Math.Abs(x.Value.PriceToday - x.Value.PriceYesterday))
            .ThenByDescending(x => Math.Abs(x.Value.TrendEma))
            .ThenByDescending(x => x.TieBreaker)
            .Take(ColumnCount * RowCount)
            .Select(x => new KeyValuePair<string, CropEconomyEntry>(x.Key, x.Value))
            .ToList();
    }

    private static float ComputeBoardRelevance(CropEconomyEntry entry)
    {
        var yesterday = Math.Max(1, entry.PriceYesterday);
        var dailyDeltaPct = Math.Abs(entry.PriceToday - entry.PriceYesterday) / (float)yesterday;
        var trend = Math.Abs(entry.TrendEma);
        var demandShift = Math.Abs(entry.DemandFactor - 1f);
        var supplyShift = Math.Abs(entry.SupplyPressureFactor - 1f);
        var scarcity = Math.Abs(entry.ScarcityBonus);

        // Prioritize true movers (price + trend), then pressure/scarcity context.
        var score = (dailyDeltaPct * 6f)
            + (trend * 4f)
            + (demandShift * 2f)
            + (supplyShift * 2f)
            + (scarcity * 2f);

        return score;
    }

    private static int ComputeDailyTieBreaker(string value, int day)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0;

        unchecked
        {
            uint hash = 2166136261u; // FNV-1a seed
            foreach (var ch in value)
            {
                hash ^= ch;
                hash *= 16777619u;
            }

            hash ^= (uint)day * 16777619u;
            hash *= 2246822519u;
            return (int)(hash & int.MaxValue);
        }
    }
}
