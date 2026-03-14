namespace StardewLivingRPG.State;

public sealed class NewspaperState
{
    public List<NewspaperIssue> Issues { get; set; } = new();
    public List<NewspaperArticle> Articles { get; set; } = new();
}

public sealed class NewspaperIssue
{
    public int Day { get; set; }
    public string Headline { get; set; } = string.Empty;
    public List<string> Sections { get; set; } = new();
    public List<string> PredictiveHints { get; set; } = new();
    public List<NewspaperMarketLine> MarketSections { get; set; } = new();
    public List<NewspaperMarketLine> MarketHintFallbacks { get; set; } = new();
    public List<NewspaperArticle> Articles { get; set; } = new();
}

public sealed class NewspaperMarketLine
{
    public string TemplateId { get; set; } = string.Empty;
    public string CropKey { get; set; } = string.Empty;
    public int PriceToday { get; set; }
    public float DeltaPct { get; set; }
}

public class NewspaperArticle
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // "community", "market", "social", "nature"
    public string SourceNpc { get; set; } = string.Empty;
    public bool IsNpcPublished { get; set; }
    public int Day { get; set; }
    public int ExpirationDay { get; set; } // Article expires after this many days
}
