using StardewLivingRPG.State;

namespace StardewLivingRPG.Systems;

public sealed class NewspaperService
{
    private const int MinDailyArticles = 2;
    private const int MaxDailyArticles = 5;

    public NewspaperIssue BuildIssue(SaveState state, string? anchorNote = null)
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

        // 3. Add NPC-generated articles for today (already in state.Newspaper.Articles)
        var todayNpcArticles = state.Newspaper.Articles
            .Where(a => a.Day == state.Calendar.Day && a.ExpirationDay >= state.Calendar.Day)
            .ToList();
        foreach (var article in todayNpcArticles)
        {
            if (issue.Articles.Count < MaxDailyArticles)
                issue.Articles.Add(article);
        }

        // 4. Select headline from most important article
        issue.Headline = SelectHeadline(issue.Articles, state);

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
                    Content = $"A local farmer was found unconscious in the mines and was brought to safety by a rescue operation. The incident occurred on day {ev.Day}.",
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
                SourceNpc = "The Pelican Times",
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
    private static string SelectHeadline(List<NewspaperArticle> articles, SaveState state)
    {
        if (articles.Count == 0)
            return "Quiet Day at Pelican Town";

        // Priority 1: High-severity incidents (severity 4-5)
        var highSeverityArticle = articles
            .Where(a => a.SourceNpc == "Town Report")
            .FirstOrDefault(a => a.Content.Contains("incident", StringComparison.OrdinalIgnoreCase)
                || a.Content.Contains("Rescue", StringComparison.OrdinalIgnoreCase)
                || a.Content.Contains("Storm", StringComparison.OrdinalIgnoreCase));

        if (highSeverityArticle is not null)
            return GenerateSensationalHeadline(highSeverityArticle);

        // Priority 2: Market/nature articles
        var marketOrNatureArticle = articles
            .FirstOrDefault(a => a.Category.Equals("market", StringComparison.OrdinalIgnoreCase)
                || a.Category.Equals("nature", StringComparison.OrdinalIgnoreCase));

        if (marketOrNatureArticle is not null)
            return GenerateSensationalHeadline(marketOrNatureArticle);

        // Priority 3: Any article
        return GenerateSensationalHeadline(articles[0]);
    }

    /// <summary>
    /// Generate tabloid-style sensational headline from article.
    /// Different from article title - exaggerated and exciting.
    /// </summary>
    private static string GenerateSensationalHeadline(NewspaperArticle article)
    {
        if (article.Content.Contains("faint", StringComparison.OrdinalIgnoreCase)
            || article.Content.Contains("unconscious", StringComparison.OrdinalIgnoreCase))
        {
            var prefixes = new[] { "DRAMA IN THE DEPTHS", "MINE RESCUE", "DARING OPERATION" };
            var suffixes = new[] { "FARMER COLLAPSES IN MINE SHAFT!", "FARMER PULLED FROM DARKNESS!", "HEROIC RESCUE AT MINES!" };
            return $"{GetRandom(prefixes)}: {GetRandom(suffixes)}";
        }

        if (article.Content.Contains("rescue", StringComparison.OrdinalIgnoreCase))
        {
            var prefixes = new[] { "DARING RESCUE", "HEROIC EFFORT", "TOWN RALLIES" };
            var suffixes = new[] { "LOCAL FARMER PULLED FROM DARKNESS!", "COMMUNITY SAVES THE DAY!", "BRAVE RESIDENTS RISK IT ALL!" };
            return $"{GetRandom(prefixes)}: {GetRandom(suffixes)}";
        }

        if (article.Content.Contains("storm", StringComparison.OrdinalIgnoreCase))
        {
            var prefixes = new[] { "NATURE'S FURY", "STORM CHAOS", "WEATHER ALERT" };
            var suffixes = new[] { "TOWN BATTERED BY HIGH WINDS!", "DAMAGE REPORTED ACROSS PELICAN TOWN!", "RESIDENTS SEEK SHELTER!" };
            return $"{GetRandom(prefixes)}: {GetRandom(suffixes)}";
        }

        if (article.Category.Equals("market", StringComparison.OrdinalIgnoreCase)
            || article.Content.Contains("price", StringComparison.OrdinalIgnoreCase)
            || article.Content.Contains("stock", StringComparison.OrdinalIgnoreCase))
        {
            var prefixes = new[] { "MARKET SHOCK", "PRICES SEND TRADERS INTO FRENZY", "ECONOMIC DRAMA" };
            var suffixes = new[] { "PRICES SPIKE ACROSS THE BOARD!", "TRADERS SCRAMBLE FOR GOODS!", "MARKET VOLATILITY STUNS TOWN!" };
            return $"{GetRandom(prefixes)}: {GetRandom(suffixes)}";
        }

        if (article.Category.Equals("nature", StringComparison.OrdinalIgnoreCase)
            || article.Content.Contains("salmon", StringComparison.OrdinalIgnoreCase)
            || article.Content.Contains("fish", StringComparison.OrdinalIgnoreCase))
        {
            var prefixes = new[] { "SILVER RUSH", "NATURE'S BOUNTY", "RIVER TEEMING" };
            var suffixes = new[] { "RIVERS TEEMING WITH MIGRATING SALMON!", "FISHERMEN REEL IN RECORD CATCH!", "ANNUAL SALMON RUN BEGINS!" };
            return $"{GetRandom(prefixes)}: {GetRandom(suffixes)}";
        }

        if (article.Content.Contains("festival", StringComparison.OrdinalIgnoreCase))
        {
            var prefixes = new[] { "FAVORITE FESTIVAL RETURNS", "TOWN PREPARES", "EXCITEMENT BUILDS" };
            var suffixes = new[] { "VENDORS PREPARE FOR INFLUX!", "RESIDENTS GEAR UP FOR CELEBRATION!", "SPECIAL EVENT DRAWS CROWDS!" };
            return $"{GetRandom(prefixes)}: {GetRandom(suffixes)}";
        }

        // Generic fallback
        var genericPrefixes = new[] { "SHOCKING", "ALARMING", "BREAKING", "DEVELOPING" };
        return $"{GetRandom(genericPrefixes)}: {article.Title.ToUpperInvariant()}!";
    }

    /// <summary>
    /// Get 32 seasonal article templates (8 per season).
    /// </summary>
    private static List<ArticleTemplate> GetSeasonalTemplates()
    {
        return new()
        {
            // Spring (8 articles)
            new ArticleTemplate("Pierre's Stock Alert", "Pierre announces limited stock of parsnip and potato seeds due to high demand. Farmers encouraged to buy early.", "market"),
            new ArticleTemplate("Spring Planting Rush", "Local farmers are seen busily preparing their fields for the spring planting season. Cauliflower and potatoes are popular choices.", "nature"),
            new ArticleTemplate("Community Cleanup", "Lewis announces a community cleanup day for this Saturday. Residents are asked to gather at the town square.", "community"),
            new ArticleTemplate("New Seed Arrivals", "Pierre has received a shipment of rare strawberry seeds. Limited quantities available.", "market"),
            new ArticleTemplate("Flower Blooms Early", "An unusual warm spell has caused flowers around town to bloom earlier than expected this year.", "nature"),
            new ArticleTemplate("Youth Fishing Contest", "The annual youth fishing contest will be held at the beach this Sunday. All are welcome to attend.", "community"),
            new ArticleTemplate("Crop Quality Concerns", "Agricultural experts warn about soil quality this spring. Fertilizer recommended for optimal yields.", "market"),
            new ArticleTemplate("Spring Festival Preview", "Organisers are putting final touches on the upcoming Egg Festival. Vendors are preparing for crowds.", "community"),

            // Summer (8 articles)
            new ArticleTemplate("Salmon Run Begins", "The annual salmon run has started in the rivers. Fishermen report excellent catches.", "nature"),
            new ArticleTemplate("Heat Wave Alert", "Temperatures are rising. Residents are advised to stay hydrated and avoid strenuous activities.", "nature"),
            new ArticleTemplate("Beach Volleyball", "The summer beach volleyball tournament is open for registration. Teams of two can sign up at the beach.", "community"),
            new ArticleTemplate("Blueberry Boom", "Local blueberry patches are producing exceptional yields this year. Prices may drop.", "market"),
            new ArticleTemplate("Ice Cream Social", "Gus and the Saloon are hosting an ice cream social this weekend. Free samples for children.", "community"),
            new ArticleTemplate("Corn Concerns", "Corn crops are showing signs of stress due to the heat. Farmers are monitoring closely.", "nature"),
            new ArticleTemplate("Lucky Mineral", "A lucky prospector found a prismatic shard in the mines. Scientists are intrigued.", "community"),
            new ArticleTemplate("Summer Tourism", "Tourists from the city are visiting Pelican Town in record numbers. Local businesses see increased sales.", "market"),

            // Fall (8 articles)
            new ArticleTemplate("Harvest Festival Prep", "Stardew Valley's annual Harvest Festival is approaching. Farmers are selecting their best crops for competition.", "community"),
            new ArticleTemplate("Pumpkin Patch Report", "Pumpkin patches are producing excellent specimens this year. Carving enthusiasts delighted.", "nature"),
            new ArticleTemplate("Cranberry Season", "The cranberry bog is ready for harvest. Residents are preparing for the annual cranberry contest.", "nature"),
            new ArticleTemplate("Spirits' Eve", "The annual Spirits' Eve festival will feature unusual activities this year. Mayor Lewis remains tight-lipped.", "community"),
            new ArticleTemplate("Falling Prices", "Crop prices are trending downward as harvest floods the market. Buyers have excellent bargaining power.", "market"),
            new ArticleTemplate("Foliage Season", "The valley's leaves are turning spectacular colors. It's an ideal time for hiking.", "nature"),
            new ArticleTemplate("Soup Kitchen", "The community kitchen is seeking volunteers for the upcoming fall feast. All contributions welcome.", "community"),
            new ArticleTemplate("Winter Prep", "Residents are stocking up on firewood and preserves. The coming winter is predicted to be harsh.", "market"),

            // Winter (8 articles)
            new ArticleTemplate("Feast of Winter Star", "Planning for the Feast of the Winter Star is underway. The potluck dish signup is open.", "community"),
            new ArticleTemplate("Ice Fishing", "Ice fishing shacks are set up on the lake. Early reports show slow catches.", "nature"),
            new ArticleTemplate("Coal Shortage", "Coal prices are rising as residents heat their homes. Miners are working to meet demand.", "market"),
            new ArticleTemplate("Snow Sculptures", "The annual ice sculpture contest will feature a mystery prize this year.", "community"),
            new ArticleTemplate("Winter Foraging", "Foraging options are limited in winter. Residents rely on preserved foods and winter roots.", "nature"),
            new ArticleTemplate("Gift Exchange", "The community gift exchange is accepting donations. Please wrap gifts and label with intended recipient.", "community"),
            new ArticleTemplate("Market Slowdown", "Winter has slowed trading. Pierre reports minimal sales except for staples and wheat.", "market"),
            new ArticleTemplate("Stardew Valley Inn", "The inn is offering warm drinks and lodging for travelers caught in the snow.", "community")
        };
    }

    private static string GetRandom(string[] options)
    {
        // Deterministic random based on current time (changes each call, keeps variety)
        var index = DateTime.Now.Millisecond % options.Length;
        return options[index];
    }

    private static string Cap(string s)
        => string.IsNullOrWhiteSpace(s) ? s : char.ToUpperInvariant(s[0]) + s[1..];

    private static CropPriceChange? GetTopDown(SaveState state)
    {
        if (state.Economy.Crops is null) return null;
        return state.Economy.Crops
            .Where(kv => kv.Value.PriceYesterday > 0)
            .Select(kv => new CropPriceChange
            {
                Crop = kv.Key,
                Today = kv.Value.PriceToday,
                Yesterday = kv.Value.PriceYesterday,
                DeltaPct = (double)(kv.Value.PriceToday - kv.Value.PriceYesterday) / kv.Value.PriceYesterday
            })
            .Where(x => x.DeltaPct < 0)
            .OrderByDescending(x => Math.Abs(x.DeltaPct))
            .FirstOrDefault();
    }

    private static CropPriceChange? GetTopUp(SaveState state)
    {
        if (state.Economy.Crops is null) return null;
        return state.Economy.Crops
            .Where(kv => kv.Value.PriceYesterday > 0)
            .Select(kv => new CropPriceChange
            {
                Crop = kv.Key,
                Today = kv.Value.PriceToday,
                Yesterday = kv.Value.PriceYesterday,
                DeltaPct = (double)(kv.Value.PriceToday - kv.Value.PriceYesterday) / kv.Value.PriceYesterday
            })
            .Where(x => x.DeltaPct > 0)
            .OrderByDescending(x => x.DeltaPct)
            .FirstOrDefault();
    }

    private class ArticleTemplate
    {
        public string Title { get; }
        public string Content { get; }
        public string Category { get; }

        public ArticleTemplate(string title, string content, string category)
        {
            Title = title;
            Content = content;
            Category = category;
        }
    }

    private class CropPriceChange
    {
        public string Crop { get; set; } = string.Empty;
        public int Today { get; set; }
        public int Yesterday { get; set; }
        public double DeltaPct { get; set; }
    }
}
