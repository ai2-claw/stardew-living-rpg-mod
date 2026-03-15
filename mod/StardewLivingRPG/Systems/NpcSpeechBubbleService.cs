using StardewLivingRPG.Config;
using StardewValley;

namespace StardewLivingRPG.Systems;

public sealed class NpcSpeechBubbleService
{
    private sealed class PendingBubble
    {
        public string NpcId { get; init; } = string.Empty;
        public Queue<string> Chunks { get; init; } = new();
        public DateTime NextDisplayUtc { get; set; }
    }

    private readonly ModConfig _config;
    private readonly Dictionary<string, PendingBubble> _pendingByNpcId = new(StringComparer.OrdinalIgnoreCase);

    public NpcSpeechBubbleService(ModConfig config)
    {
        _config = config;
    }

    public void QueueTranscriptLine(string npcId, string text)
    {
        var chunks = ChunkText(text, _config.BubbleMaxChars);
        if (chunks.Count == 0)
            return;

        if (!_pendingByNpcId.TryGetValue(npcId, out var pending))
        {
            pending = new PendingBubble
            {
                NpcId = npcId,
                NextDisplayUtc = DateTime.UtcNow
            };
            _pendingByNpcId[npcId] = pending;
        }

        foreach (var chunk in chunks)
            pending.Chunks.Enqueue(chunk);
    }

    public int Tick(Func<string, NPC?> resolveNpc)
    {
        var displayed = 0;
        foreach (var key in _pendingByNpcId.Keys.ToArray())
        {
            var pending = _pendingByNpcId[key];
            if (pending.Chunks.Count == 0)
            {
                _pendingByNpcId.Remove(key);
                continue;
            }

            if (DateTime.UtcNow < pending.NextDisplayUtc)
                continue;

            var npc = resolveNpc(pending.NpcId);
            if (npc is null)
            {
                _pendingByNpcId.Remove(key);
                continue;
            }

            var chunk = pending.Chunks.Dequeue();
            npc.showTextAboveHead(chunk);
            TrySetTextAboveHeadTimer(npc, GetBubbleDurationMs(chunk));
            pending.NextDisplayUtc = DateTime.UtcNow.AddMilliseconds(GetBubbleDurationMs(chunk) + _config.BubblePauseBetweenMs);
            displayed += 1;

            if (pending.Chunks.Count == 0)
                _pendingByNpcId.Remove(key);
        }

        return displayed;
    }

    public void CancelAll()
    {
        _pendingByNpcId.Clear();
    }

    public static List<string> ChunkText(string? text, int maxChars)
    {
        var clean = Normalize(text);
        var chunks = new List<string>();
        if (string.IsNullOrWhiteSpace(clean) || maxChars <= 4)
            return chunks;

        var remaining = clean;
        while (remaining.Length > maxChars)
        {
            var splitIndex = FindSplitIndex(remaining, maxChars);
            chunks.Add(remaining[..splitIndex].Trim());
            remaining = remaining[splitIndex..].Trim();
        }

        if (!string.IsNullOrWhiteSpace(remaining))
            chunks.Add(remaining.Trim());

        return chunks;
    }

    public int GetBubbleDurationMs(string? text)
    {
        var clean = Normalize(text);
        return Math.Clamp(_config.BubbleMinDurationMs + (clean.Length * 35), _config.BubbleMinDurationMs, _config.BubbleMaxDurationMs);
    }

    private static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        return string.Join(" ",
            text.Trim()
                .Replace("\r", " ", StringComparison.Ordinal)
                .Replace("\n", " ", StringComparison.Ordinal)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static int FindSplitIndex(string text, int maxChars)
    {
        var window = text[..Math.Min(text.Length, maxChars)];
        var punctuation = new[] { '.', '!', '?', ';', ',' };
        var split = window.LastIndexOfAny(punctuation);
        if (split >= maxChars / 2)
            return split + 1;

        split = window.LastIndexOf(' ');
        if (split >= maxChars / 2)
            return split;

        return Math.Min(text.Length, maxChars);
    }

    private static void TrySetTextAboveHeadTimer(NPC npc, int durationMs)
    {
        var field = npc.GetType().GetField("textAboveHeadTimer", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        if (field is not null && field.FieldType == typeof(int))
        {
            field.SetValue(npc, durationMs);
            return;
        }

        var property = npc.GetType().GetProperty("textAboveHeadTimer", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        if (property?.CanWrite == true && property.PropertyType == typeof(int))
            property.SetValue(npc, durationMs);
    }
}
