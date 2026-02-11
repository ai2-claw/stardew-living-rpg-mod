using StardewLivingRPG.State;

namespace StardewLivingRPG.Utils;

public static class QuestTextHelper
{
    public static string BuildQuestTitle(QuestEntry q)
    {
        var target = PrettyTarget(q.TargetItem);

        var titles = q.TemplateId.ToLowerInvariant() switch
        {
            "gather_crop" => new[]
            {
                $"Gather {target}",
                $"Harvest Help: {target}",
                $"Field Run: {target}"
            },
            "deliver_item" => new[]
            {
                $"Supply Drop: {target}",
                $"Market Delivery: {target}",
                $"Town Delivery: {target}"
            },
            "mine_resource" => new[]
            {
                $"Mine Run: {target}",
                $"Prospector's Call: {target}",
                $"Ore Request: {target}"
            },
            "social_visit" => new[]
            {
                $"Friendly Visit: {target}",
                $"Check-In with {target}",
                $"Neighborly Errand: {target}"
            },
            _ => new[]
            {
                $"Town Request: {target}",
                $"Community Task: {target}",
                $"Mayor's Request: {target}"
            }
        };

        var idx = Math.Abs(q.QuestId.GetHashCode()) % titles.Length;
        return titles[idx];
    }

    public static string PrettyTarget(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Supplies";

        var normalized = value.Replace("_", " ").Trim();
        var words = normalized
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(w => char.ToUpperInvariant(w[0]) + w[1..].ToLowerInvariant());

        return string.Join(' ', words);
    }
}
