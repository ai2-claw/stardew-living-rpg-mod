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

    private sealed class EncounterBubble
    {
        public string EncounterId { get; init; } = string.Empty;
        public string SpeakerNpcId { get; init; } = string.Empty;
        public string Text { get; init; } = string.Empty;
        public int SequenceIndex { get; init; }
        public long Order { get; init; }
    }

    private readonly ModConfig _config;
    private readonly Dictionary<string, PendingBubble> _pendingByNpcId = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<EncounterBubble>> _encounterBubbles = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _encounterDisplayIndex = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DateTime> _encounterNextDisplayUtc = new(StringComparer.OrdinalIgnoreCase);
    private long _encounterBubbleOrderCounter;

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

    public void QueueEncounterBubble(string encounterId, string speakerNpcId, string text, int sequenceIndex)
    {
        if (string.IsNullOrWhiteSpace(encounterId) || string.IsNullOrWhiteSpace(text))
            return;

        if (!_encounterBubbles.TryGetValue(encounterId, out var list))
        {
            list = new List<EncounterBubble>();
            _encounterBubbles[encounterId] = list;
            _encounterDisplayIndex[encounterId] = 0;
            _encounterNextDisplayUtc[encounterId] = DateTime.UtcNow;
        }

        var chunks = ChunkText(text, _config.BubbleMaxChars);
        foreach (var chunk in chunks)
        {
            list.Add(new EncounterBubble
            {
                EncounterId = encounterId,
                SpeakerNpcId = speakerNpcId,
                Text = chunk,
                SequenceIndex = sequenceIndex,
                Order = ++_encounterBubbleOrderCounter
            });
        }

        list.Sort((a, b) =>
        {
            var sequenceCompare = a.SequenceIndex.CompareTo(b.SequenceIndex);
            return sequenceCompare != 0 ? sequenceCompare : a.Order.CompareTo(b.Order);
        });
    }

    public void CancelEncounterBubbles(string encounterId)
    {
        _encounterBubbles.Remove(encounterId);
        _encounterDisplayIndex.Remove(encounterId);
        _encounterNextDisplayUtc.Remove(encounterId);
    }

    public bool HasEncounterBubblesRemaining(string encounterId)
    {
        return _encounterBubbles.TryGetValue(encounterId, out var list)
               && _encounterDisplayIndex.TryGetValue(encounterId, out var idx)
               && idx < list.Count;
    }

    public bool IsEncounterReadyForNextBubble(string encounterId)
    {
        if (!_encounterBubbles.TryGetValue(encounterId, out var list))
            return true;
        if (!_encounterDisplayIndex.TryGetValue(encounterId, out var idx))
            return true;
        if (idx < list.Count)
            return false;

        return !_encounterNextDisplayUtc.TryGetValue(encounterId, out var nextUtc) || DateTime.UtcNow >= nextUtc;
    }

    public int Tick(Func<string, NPC?> resolveNpc)
    {
        var displayed = 0;

        // Process regular transcript bubbles
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

        // Process encounter bubbles (G22) — alternate between speakers by sequence order
        foreach (var encId in _encounterBubbles.Keys.ToArray())
        {
            if (!_encounterDisplayIndex.TryGetValue(encId, out var idx))
                continue;
            if (!_encounterBubbles.TryGetValue(encId, out var list) || idx >= list.Count)
            {
                _encounterBubbles.Remove(encId);
                _encounterDisplayIndex.Remove(encId);
                _encounterNextDisplayUtc.Remove(encId);
                continue;
            }

            if (_encounterNextDisplayUtc.TryGetValue(encId, out var nextUtc) && DateTime.UtcNow < nextUtc)
                continue;

            var bubble = list[idx];
            var npc = resolveNpc(bubble.SpeakerNpcId);
            if (npc is null)
            {
                // Cancel encounter bubbles if speaker unavailable
                _encounterBubbles.Remove(encId);
                _encounterDisplayIndex.Remove(encId);
                _encounterNextDisplayUtc.Remove(encId);
                continue;
            }

            npc.showTextAboveHead(bubble.Text);
            TrySetTextAboveHeadTimer(npc, GetBubbleDurationMs(bubble.Text));
            _encounterDisplayIndex[encId] = idx + 1;
            _encounterNextDisplayUtc[encId] = DateTime.UtcNow.AddMilliseconds(GetBubbleDurationMs(bubble.Text) + _config.BubblePauseBetweenMs);
            displayed += 1;
        }

        return displayed;
    }

    public void CancelAll()
    {
        _pendingByNpcId.Clear();
        _encounterBubbles.Clear();
        _encounterDisplayIndex.Clear();
        _encounterNextDisplayUtc.Clear();
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
