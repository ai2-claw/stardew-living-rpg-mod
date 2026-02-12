using StardewLivingRPG.Config;
using StardewLivingRPG.State;
using StardewLivingRPG.Integrations;

namespace StardewLivingRPG.Systems;

public sealed class NewspaperService
{
    private const int MinDailyArticles = 2;
    private const int MaxDailyArticles = 5;
    private readonly Player2Client? _player2;

    public NewspaperService(Player2Client? player2 = null)
    {
        _player2 = player2;
    }

    public async Task<NewspaperIssue> BuildIssueAsync(SaveState state, string? anchorNote = null)
    {
        // Start with empty issue
        var issue = new NewspaperIssue
        {
            Day = state.Calendar.Day,
            Headline = "Quiet Day at Pelican Town", // Placeholder, will be replaced by SelectHeadline
            Sections = new List<string>(),
            PredictiveHints = new List<string>(),
            Articles = new List<NewspaperArticle>()
        };

        // 1. Generate event articles from town memory (runs FIRST)
        var eventArticles = GenerateEventArticles(state, yesterday: state.Calendar.Day - 1);
        foreach (var article in eventArticles)
        {
            if (issue.Articles.Count < MaxDailyArticles)
                issue.Articles.Add(article);
        }

        // 2. Fill remaining slots with seasonal filler articles
        var fillerCount = Math.Max(0, MinDailyArticles - issue.Articles.Count);
        var fillerArticles = GenerateFillerArticles(state, count: fillerCount);
        foreach (var article in fillerArticles)
        {
            if (issue.Articles.Count < MaxDailyArticles)
                issue.Articles.Add(article);
        }

        // 3. Add NPC-generated articles for today
        var todayNpcArticles = state.Newspaper.Articles
            .Where(a => a.Day == state.Calendar.Day && a.ExpirationDay >= state.Calendar.Day)
            .ToList();
        foreach (var article in todayNpcArticles)
        {
            if (issue.Articles.Count < MaxDailyArticles)
                issue.Articles.Add(article);
        }

        // 4. Select headline from most important article
        issue.Headline = await SelectHeadlineAsync(issue.Articles, state);

        // 5. Add market sections
        var topDown = GetTopDown(state);
        var topUp = GetTopUp(state);

        if (topDown is not null)
            issue.Sections.Add($"Market: {Cap(topDown.Crop)} softened {(Math.Abs(topDown.DeltaPct) * 100):0.#}% to {topDown.Today}g.");

        if (topUp is not null)
            issue.Sections.Add($"Opportunity: {Cap(topUp.Crop)} rose {(topUp.DeltaPct * 100):0.#}% to {topUp.Today}g.");

        if (!string.IsNullOrWhiteSpace(anchorNote))
            issue.Sections.Add(anchorNote);

        // 6. Predictive hints for planner players
        var scarcityCandidate = state.Economy.Crops
            .Where(kv => kv.Value.ScarcityBonus > 0.0f)
            .OrderByDescending(kv => kv.Value.ScarcityBonus)
            .FirstOrDefault();

        if (!string.IsNullOrEmpty(scarcityCandidate.Key))
        {
            issue.PredictiveHints.Add($"Supply outlook: {Cap(scarcityCandidate.Key)} may stay strong if oversupply persists elsewhere.");
        }

        var seasonHint = state.Calendar.Season.ToLowerInvariant() switch
        {
            "spring" => "Season watch: Cauliflower and Parsnip demand tends to hold in spring.",
            "summer" => "Season watch: Melon and Blueberry demand remains healthy in summer.",
            "fall" => "Season watch: Pumpkin and Cranberry demand tends to strengthen in fall.",
            "winter" => "Season watch: Snow and cold slows farm activity.",
            _ => "Season watch: mixed demand expected tomorrow."
        };
        issue.PredictiveHints.Add(seasonHint);

        return issue;
    }

    /// <summary>
    /// Synchronous wrapper for async headline generation. Use BuildIssueAsync for Player2 integration.
    /// Requires SetCredentials to be called before use.
    /// </summary>
    public NewspaperIssue BuildIssue(SaveState state, ModConfig config, Player2Client player2)
    {
        // Set Player2 credentials for headline generation
        if (!string.IsNullOrEmpty(config.Player2ApiBaseUrl) && !string.IsNullOrEmpty(config.Player2GameClientId))
            player2.SetCredentials(config.Player2ApiBaseUrl, config.Player2GameClientId);

        // Run synchronously, blocking on async headline generation
        try
        {
            var task = BuildIssueAsync(state, null);
            task.Wait();
            return task.Result;
        }
        catch
        {
            // Fallback if Player2 fails
            return new NewspaperIssue
            {
                Day = state.Calendar.Day,
                Headline = "Quiet Day at Pelican Town",
                Sections = new List<string>(),
                PredictiveHints = new List<string>(),
                Articles = new List<NewspaperArticle>()
            };
        }
    }

    /// <summary>
    /// Generate event articles from town memory (yesterday's events).
    /// Max 2 event articles per day.
    /// </summary>
    private static List<NewspaperArticle> GenerateEventArticles(SaveState state, int yesterday)
    {
        var articles = new List<NewspaperArticle>();

        // Get yesterday's events from town memory
        var yesterdayEvents = state.TownMemory.Events
            .Where(e => e.Day == yesterday)
            .OrderByDescending(e => e.Severity)
            .Take(2)
            .ToList();

        foreach (var ev in yesterdayEvents)
        {
            var article = ev.Kind.ToLowerInvariant() switch
            {
                "fainting" => new NewspaperArticle
                {
                    Title = "Rescue at Mines",
                    Content = $"A local farmer was found unconscious in mines and was brought to safety by a rescue operation. The incident occurred on day {ev.Day}.",
                    Category = "community",
                    SourceNpc = "Town Report",
                    Day = state.Calendar.Day,
                    ExpirationDay = state.Calendar.Day + 3
                },
                "achievement" => new NewspaperArticle
                {
                    Title = "Community Achievement",
                    Content = $"{ev.Summary}. Residents are celebrating this milestone as a sign of the town's growing prosperity.",
                    Category = "community",
                    SourceNpc = "Town Report",
                    Day = state.Calendar.Day,
                    ExpirationDay = state.Calendar.Day + 3
                },
                "storm" => new NewspaperArticle
                {
                    Title = "Storm Damage Reported",
                    Content = $"{ev.Summary}. Repair efforts are underway across affected areas.",
                    Category = "nature",
                    SourceNpc = "Town Report",
                    Day = state.Calendar.Day,
                    ExpirationDay = state.Calendar.Day + 3
                },
                _ => null
            };

            if (article is not null && articles.Count < 2)
                articles.Add(article);
        }

        return articles;
    }

    /// <summary>
    /// Generate seasonal filler articles to reach minimum article count.
    /// Deterministic based on day number and season.
    /// </summary>
    private static List<NewspaperArticle> GenerateFillerArticles(SaveState state, int count)
    {
        if (count <= 0)
            return new List<NewspaperArticle>();

        var articles = new List<NewspaperArticle>();
        var season = state.Calendar.Season.ToLowerInvariant();
        var dayInSeason = state.Calendar.Day;

        // Get seasonal templates (32 total: 8 per season)
        var templates = GetSeasonalTemplates();

        // Deterministic selection based on day number
        for (int i = 0; i < count; i++)
        {
            var templateIndex = (dayInSeason + i * 7) % templates.Count;
            var template = templates[templateIndex];

            articles.Add(new NewspaperArticle
            {
                Title = template.Title,
                Content = template.Content,
                Category = template.Category,
                SourceNpc = "Lewis", // Attributed to mayor for filler content
                Day = state.Calendar.Day,
                ExpirationDay = state.Calendar.Day + 2
            });
        }

        return articles;
    }

    /// <summary>
    /// Select the most important article as the headline.
    /// Priority: high-severity incidents > market/nature > any > fallback.
    /// </summary>
    private async Task<string> SelectHeadlineAsync(List<NewspaperArticle> articles, SaveState state)
    {
        if (articles.Count == 0)
            return "Quiet Day at Pelican Town";

        // Priority 1: High-severity incidents
        var highSeverityArticle = articles
            .FirstOrDefault(a => a.SourceNpc == "Town Report" &&
                (a.Content.Contains("incident", StringComparison.OrdinalIgnoreCase)
                || a.Content.Contains("Rescue", StringComparison.OrdinalIgnoreCase)
                || a.Content.Contains("Storm", StringComparison.OrdinalIgnoreCase)));

        if (highSeverityArticle is not null)
        {
            if (_player2 != null)
                return FallbackHeadline(highSeverityArticle.Title);
            return await _player2.GenerateSensationalHeadlineAsync(
                    highSeverityArticle.Title,
                    highSeverityArticle.Category,
                    highSeverityArticle.Content,
                    default);
        }

        // Priority 2: Market/nature articles
        var marketOrNatureArticle = articles
            .FirstOrDefault(a => a.Category.Equals("market", StringComparison.OrdinalIgnoreCase)
                || a.Category.Equals("nature", StringComparison.OrdinalIgnoreCase));

        if (marketOrNatureArticle is not null)
        {
            if (_player2 == null)
                return FallbackHeadline(marketOrNatureArticle.Title);
            return await _player2.GenerateSensationalHeadlineAsync(
                    marketOrNatureArticle.Title,
                    marketOrNatureArticle.Category,
                    marketOrNatureArticle.Content,
                    default);
        }

        // Priority 3: Any article
        {
            if (_player2 == null)
                return FallbackHeadline(articles[0].Title);
            return await _player2.GenerateSensationalHeadlineAsync(
                    articles[0].Title,
                    articles[0].Category,
                    articles[0].Content,
                    default);
        }
    }

    private async Task<string> GenerateSensationalHeadlineAsync(NewspaperArticle article)
    {
        if (_player2 == null)
            return FallbackHeadline(article.Title);

        try
        {
            return await _player2.GenerateSensationalHeadlineAsync(
                "", // apiBaseUrl - will use default from config
                "", // p2Key - will use default from config
                article.Title,
                article.Category,
                article.Content,
                default);
        }
        catch
        {
            return FallbackHeadline(article.Title);
        }
    }

    private static string FallbackHeadline(string title)
    {
        var prefixes = new[] { "BREAKING:", "SHOCKING:", "URGENT:", "ALERT:" };
        var hash = Math.Abs(title.GetHashCode());
        var prefix = prefixes[hash % prefixes.Length];
        var truncated = title.Length > 22 ? title.Substring(0, 22) : title;
        return $"{prefix} {truncated}!";
    }

    private static string TruncateHeadline(string headline)
    {
        if (headline.Length <= 30)
            return headline;

        return headline.Substring(0, 27) + "...";
    }

    private static string Cap(string value) => string.IsNullOrEmpty(value) ? value : char.ToUpper(value[0]) + value.Substring(1);

    private static CropTrendEntry? GetTopDown(SaveState state)
    {
        if (state.Economy.Crops.Count == 0)
            return null;

        return state.Economy.Crops
            .Where(kv => kv.Value.PriceToday < kv.Value.PriceYesterday)
            .OrderByDescending(kv => (kv.Value.PriceYesterday - kv.Value.PriceToday) / kv.Value.PriceYesterday)
            .Select(kv => new CropTrendEntry
            {
                Crop = kv.Key,
                Today = kv.Value.PriceToday,
                DeltaPct = (kv.Value.PriceToday - kv.Value.PriceYesterday) / kv.Value.PriceYesterday
            })
            .FirstOrDefault();
    }

    private static CropTrendEntry? GetTopUp(SaveState state)
    {
        if (state.Economy.Crops.Count == 0)
            return null;

        return state.Economy.Crops
            .Where(kv => kv.Value.PriceToday > kv.Value.PriceYesterday)
            .OrderByDescending(kv => (kv.Value.PriceToday - kv.Value.PriceYesterday) / kv.Value.PriceYesterday)
            .Select(kv => new CropTrendEntry
            {
                Crop = kv.Key,
                Today = kv.Value.PriceToday,
                DeltaPct = (kv.Value.PriceToday - kv.Value.PriceYesterday) / kv.Value.PriceYesterday
            })
            .FirstOrDefault();
    }

    private static List<ArticleTemplate> GetSeasonalTemplates()
    {
        return new List<ArticleTemplate>
        {
            // Spring (8 templates)
            new() { Title = "Spring Awakening", Content = "As flowers bloom across the valley, farmers prepare their soil for the season's first planting.", Category = "nature", Season = "spring" },
            new() { Title = "Seed Shortage Reported", Content = "Local suppliers report higher demand for parsnip and cauliflower seeds this week.", Category = "market", Season = "spring" },
            new() { Title = "Community Garden Cleanup", Content = "Residents gathered at the community center to prepare gardens for spring planting.", Category = "community", Season = "spring" },
            new() { Title = "Rain Brings Hope", Content = "Recent rainfall has farmers optimistic about soil moisture levels for early crops.", Category = "nature", Season = "spring" },
            new() { Title = "Pierre's Spring Sale", Content = "Pierre's General Store announces seasonal discounts on starting seeds.", Category = "market", Season = "spring" },
            new() { Title = "Fishing Tournament Prep", Content = "Anglers across the valley prepare for the upcoming spring fishing tournament.", Category = "community", Season = "spring" },
            new() { Title = "Foraging Season Opens", Content = "Wild spring onions and other forageables spotted near the southern beach.", Category = "nature", Season = "spring" },
            new() { Title = "New Farm Faces", Content = "Several new farmers have arrived in Pelican Town this season.", Category = "community", Season = "spring" },

            // Summer (8 templates)
            new() { Title = "Heat Wave Continues", Content = "Unseasonably warm temperatures have crops requiring extra irrigation this week.", Category = "nature", Season = "summer" },
            new() { Title = "Blueberry Bonanza", Content = "Blueberry farms report exceptional yields as summer heat ripens berries early.", Category = "market", Season = "summer" },
            new() { Title = "Beach Festival Planning", Content = "The mayor's office confirms plans for the annual summer beach festival.", Category = "community", Season = "summer" },
            new() { Title = "Corn Prices Rising", Content = "Demand for corn remains strong as summer festivals approach.", Category = "market", Season = "summer" },
            new() { Title = "Thunderstorm Warning", Content = "Weather service predicts heavy storms for the valley later this week.", Category = "nature", Season = "summer" },
            new() { Title = "Luau Crowd Record", Content = "This year's luau drew record attendance to the beach.", Category = "community", Season = "summer" },
            new() { Title = "Melon Harvest Early", Content = "Warm weather has accelerated melon ripening across valley farms.", Category = "nature", Season = "summer" },
            new() { Title = "Ice Cream Stand Returns", Content = "The traveling cart has resumed selling ice cream in the town square.", Category = "community", Season = "summer" },

            // Fall (8 templates)
            new() { Title = "Leaves Turning", Content = "Spectacular autumn colors draw visitors to the valley this week.", Category = "nature", Season = "fall" },
            new() { Title = "Pumpkin Prices Surge", Content = "Farmers report strong demand for pumpkins as fall festivals approach.", Category = "market", Season = "fall" },
            new() { Title = "Stardew Valley Fair Prep", Content = "Exhibitors prepare their best produce and livestock for the fair.", Category = "community", Season = "fall" },
            new() { Title = "Cranberry Season Opens", Content = "Cranberry bog owners begin their annual harvest.", Category = "nature", Season = "fall" },
            new() { Title = "Yam Prices Stabilize", Content = "After weeks of volatility, yam prices have settled at moderate levels.", Category = "market", Season = "fall" },
            new() { Title = "Spirit's Eve Plans", Content = "Town residents prepare for the annual Spirit's Eve celebration.", Category = "community", Season = "fall" },
            new() { Title = "Harvest Moon Bright", Content = "Farmers work late under the bright harvest moon.", Category = "nature", Season = "fall" },
            new() { Title = "Gift Exchange Season", Content = "Local shops report increased sales as residents prepare gifts.", Category = "community", Season = "fall" },

            // Winter (8 templates)
            new() { Title = "First Snow Falls", Content = "A blanket of white covers Pelican Town as winter officially begins.", Category = "nature", Season = "winter" },
            new() { Title = "Winter Seeds Available", Content = "Pierre announces availability of winter seeds and powder for growing indoors.", Category = "market", Season = "winter" },
            new() { Title = "Feast of the Winter Star", Content = "Planning begins for the annual winter feast celebration.", Category = "community", Season = "winter" },
            new() { Title = "Fishing slows in Ice", Content = "Icy conditions make fishing difficult across valley waterways.", Category = "nature", Season = "winter" },
            new() { Title = "Coal Demand High", Content = "Heating needs drive coal prices up this winter season.", Category = "market", Season = "winter" },
            new() { Title = "Night Market Vendors", Content = "Merchants prepare for the annual winter night market.", Category = "community", Season = "winter" },
            new() { Title = "Farm Maintenance", Content = "Farmers use the quiet season to repair fences and equipment.", Category = "nature", Season = "winter" },
            new() { Title = "Soup Kitchen Volunteers", Content = "Community members volunteer at the soup kitchen for needy residents.", Category = "community", Season = "winter" }
        };
    }
}

public sealed class ArticleTemplate
{
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public string Category { get; set; } = "community";
    public string Season { get; set; } = "spring";
}

public sealed class CropTrendEntry
{
    public string Crop { get; set; } = "";
    public int Today { get; set; }
    public float DeltaPct { get; set; }
}