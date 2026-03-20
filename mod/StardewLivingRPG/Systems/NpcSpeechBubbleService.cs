using System.Text.RegularExpressions;
using StardewLivingRPG.Config;
using StardewLivingRPG.Utils;
using StardewValley;

namespace StardewLivingRPG.Systems;

public sealed class NpcSpeechBubbleService
{
    private const int EncounterBubbleMinDurationMs = 900;
    private const int EncounterBubbleMaxDurationMs = 3000;
    private const int EncounterBubblePauseBetweenMs = 120;
    private const int EncounterBubbleCharDurationMs = 22;

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
    private readonly Dictionary<string, DateTime> _encounterLastBubbleEndUtc = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _encounterBubblesEverQueued = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _encounterBubblesDisplayed = new(StringComparer.OrdinalIgnoreCase);
    private long _encounterBubbleOrderCounter;

    public NpcSpeechBubbleService(ModConfig config)
    {
        _config = config;
    }

    public void QueueTranscriptLine(string npcId, string text)
    {
        var chunks = ChunkText(SanitizeBubbleText(text), _config.BubbleMaxChars);
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
        text = SanitizeEncounterBubbleText(text);
        if (string.IsNullOrWhiteSpace(encounterId) || string.IsNullOrWhiteSpace(text))
            return;

        if (!_encounterBubbles.TryGetValue(encounterId, out var list))
        {
            list = new List<EncounterBubble>();
            _encounterBubbles[encounterId] = list;
            _encounterDisplayIndex[encounterId] = 0;
            _encounterNextDisplayUtc[encounterId] = DateTime.UtcNow;
        }

        _encounterBubblesEverQueued.Add(encounterId);

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
        ForgetEncounter(encounterId);
    }

    public void ForgetEncounter(string encounterId)
    {
        ClearEncounterActiveState(encounterId);
        _encounterBubblesEverQueued.Remove(encounterId);
        _encounterBubblesDisplayed.Remove(encounterId);
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

    public bool IsLastBubbleFinished(string encounterId)
    {
        return !_encounterLastBubbleEndUtc.TryGetValue(encounterId, out var endUtc) || DateTime.UtcNow >= endUtc;
    }

    public int Tick(Func<string, NPC?> resolveNpc, Func<string, string, bool>? validateEncounterSpeaker = null)
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

            var chunk = SanitizeBubbleText(pending.Chunks.Dequeue());
            if (string.IsNullOrWhiteSpace(chunk))
            {
                if (pending.Chunks.Count == 0)
                    _pendingByNpcId.Remove(key);
                continue;
            }

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
            if (!_encounterBubbles.TryGetValue(encId, out var list))
            {
                ClearEncounterActiveState(encId);
                continue;
            }
            if (idx >= list.Count)
            {
                if (!IsLastBubbleFinished(encId))
                    continue;

                ClearEncounterActiveState(encId);
                continue;
            }

            if (_encounterNextDisplayUtc.TryGetValue(encId, out var nextUtc) && DateTime.UtcNow < nextUtc)
                continue;

            var bubble = list[idx];
            if (validateEncounterSpeaker is not null && !validateEncounterSpeaker(encId, bubble.SpeakerNpcId))
            {
                ClearEncounterActiveState(encId);
                continue;
            }

            var npc = resolveNpc(bubble.SpeakerNpcId);
            if (npc is null)
            {
                // Cancel encounter bubbles if speaker unavailable
                ClearEncounterActiveState(encId);
                continue;
            }

            var sanitized = SanitizeEncounterBubbleText(bubble.Text);
            if (string.IsNullOrWhiteSpace(sanitized))
            {
                _encounterDisplayIndex[encId] = idx + 1;
                continue;
            }

            var durationMs = GetEncounterBubbleDurationMs(sanitized);
            npc.showTextAboveHead(sanitized);
            TrySetTextAboveHeadTimer(npc, durationMs);
            _encounterBubblesDisplayed.Add(encId);
            _encounterDisplayIndex[encId] = idx + 1;
            _encounterNextDisplayUtc[encId] = DateTime.UtcNow.AddMilliseconds(durationMs + EncounterBubblePauseBetweenMs);
            if (idx + 1 >= list.Count)
                _encounterLastBubbleEndUtc[encId] = _encounterNextDisplayUtc[encId];
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
        _encounterLastBubbleEndUtc.Clear();
        _encounterBubblesEverQueued.Clear();
        _encounterBubblesDisplayed.Clear();
    }

    private void ClearEncounterActiveState(string encounterId)
    {
        _encounterBubbles.Remove(encounterId);
        _encounterDisplayIndex.Remove(encounterId);
        _encounterNextDisplayUtc.Remove(encounterId);
        _encounterLastBubbleEndUtc.Remove(encounterId);
    }

    public bool WereEncounterBubblesEverQueued(string encounterId)
    {
        return _encounterBubblesEverQueued.Contains(encounterId);
    }

    public bool WereEncounterBubblesDisplayed(string encounterId)
    {
        return _encounterBubblesDisplayed.Contains(encounterId);
    }

    public static List<string> ChunkText(string? text, int maxChars)
    {
        var clean = Normalize(text);
        var chunks = new List<string>();
        if (string.IsNullOrWhiteSpace(clean) || maxChars <= 4)
            return chunks;

        var pendingChunk = string.Empty;
        foreach (var unit in SplitSentenceUnits(clean))
        {
            if (string.IsNullOrWhiteSpace(unit))
                continue;

            if (unit.Length <= maxChars)
            {
                if (string.IsNullOrWhiteSpace(pendingChunk))
                {
                    pendingChunk = unit;
                    continue;
                }

                var candidate = $"{pendingChunk} {unit}";
                if (candidate.Length <= maxChars)
                {
                    pendingChunk = candidate;
                    continue;
                }

                chunks.Add(pendingChunk);
                pendingChunk = unit;
                continue;
            }

            if (!string.IsNullOrWhiteSpace(pendingChunk))
            {
                chunks.Add(pendingChunk);
                pendingChunk = string.Empty;
            }

            chunks.AddRange(SplitLongUnit(unit));
        }

        if (!string.IsNullOrWhiteSpace(pendingChunk))
            chunks.Add(pendingChunk);

        return chunks;
    }

    public int GetBubbleDurationMs(string? text)
    {
        var clean = Normalize(text);
        var multiplier = Math.Clamp(_config.BubbleDurationMultiplier, 0.5f, 3.0f);
        var minDuration = (int)Math.Round(_config.BubbleMinDurationMs * multiplier);
        var maxDuration = (int)Math.Round(_config.BubbleMaxDurationMs * multiplier);
        var scaledDuration = (int)Math.Round((_config.BubbleMinDurationMs + (clean.Length * 35)) * multiplier);
        return Math.Clamp(scaledDuration, minDuration, maxDuration);
    }

    public int GetEncounterBubbleDurationMs(string? text)
    {
        var clean = Normalize(text);
        var multiplier = Math.Clamp(_config.BubbleDurationMultiplier, 0.5f, 3.0f);
        var minDuration = (int)Math.Round(EncounterBubbleMinDurationMs * multiplier);
        var maxDuration = (int)Math.Round(EncounterBubbleMaxDurationMs * multiplier);
        var scaledDuration = (int)Math.Round((EncounterBubbleMinDurationMs + (clean.Length * EncounterBubbleCharDurationMs)) * multiplier);
        return Math.Clamp(
            scaledDuration,
            minDuration,
            maxDuration);
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

    private static List<string> SplitSentenceUnits(string text)
    {
        var units = new List<string>();
        var start = 0;

        for (var i = 0; i < text.Length; i++)
        {
            if (!IsTerminalPunctuation(text[i]))
                continue;

            var next = i + 1;
            while (next < text.Length && char.IsWhiteSpace(text[next]))
                next++;

            var unit = text[start..next].Trim();
            if (!string.IsNullOrWhiteSpace(unit))
                units.Add(unit);
            start = next;
        }

        if (start < text.Length)
        {
            var trailing = text[start..].Trim();
            if (!string.IsNullOrWhiteSpace(trailing))
                units.Add(trailing);
        }

        return units.Count > 0 ? units : new List<string> { text };
    }

    private static List<string> SplitLongUnit(string text)
    {
        var chunks = new List<string>();
        var remaining = Normalize(text).Trim();
        if (string.IsNullOrWhiteSpace(remaining))
            return chunks;

        var words = remaining.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length <= 1)
            return SplitLongToken(remaining);

        var bestIndex = FindBestBalancedSplitIndex(words);
        if (bestIndex <= 0 || bestIndex >= words.Length)
            return SplitLongToken(remaining);

        var firstHalf = string.Join(" ", words[..bestIndex]).Trim();
        var secondHalf = string.Join(" ", words[bestIndex..]).Trim();
        if (string.IsNullOrWhiteSpace(firstHalf) || string.IsNullOrWhiteSpace(secondHalf))
            return SplitLongToken(remaining);

        chunks.Add($"{firstHalf}...");
        chunks.Add(secondHalf);

        return chunks;
    }

    private static int FindBestBalancedSplitIndex(string[] words)
    {
        var bestIndex = -1;
        var bestDelta = int.MaxValue;
        var bestLongestSide = int.MaxValue;

        for (var i = 1; i < words.Length; i++)
        {
            var left = string.Join(" ", words[..i]).Trim();
            var right = string.Join(" ", words[i..]).Trim();
            if (left.Length == 0 || right.Length == 0)
                continue;

            var leftLengthWithContinuation = left.Length + 3;
            var delta = Math.Abs(leftLengthWithContinuation - right.Length);
            var longestSide = Math.Max(leftLengthWithContinuation, right.Length);
            if (delta < bestDelta || (delta == bestDelta && longestSide < bestLongestSide))
            {
                bestDelta = delta;
                bestLongestSide = longestSide;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private static List<string> SplitLongToken(string text)
    {
        var midpoint = text.Length / 2;
        if (midpoint <= 0 || midpoint >= text.Length)
            return new List<string> { text };

        var firstHalf = text[..midpoint].TrimEnd();
        var secondHalf = text[midpoint..].TrimStart();
        if (firstHalf.Length == 0 || secondHalf.Length == 0)
            return new List<string> { text };

        return new List<string>
        {
            $"{firstHalf}...",
            secondHalf
        };
    }

    private static bool IsTerminalPunctuation(char value)
    {
        return value is '.' or '!' or '?';
    }

    private static readonly Regex LeadingMetadataRegex = new(
        @"^[^\w]*(?:(?:<\s*(?:emotion|mood|state)\s*:\s*[^>]+>\s*)|(?:\[\s*(?:emotion|mood|state)\s*:\s*[^\]]+\]\s*)|(?:\(\s*(?:emotion|mood|state)\s*:\s*[^)]+\)\s*)|(?:\*?\s*(?:emotion|mood|state)\s*[:=]\s*[a-z_]+\*?\s*))+\s*",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex InlineMetadataRegex = new(
        @"(?:<\s*(?:emotion|mood|state)\s*:\s*[^>]+>)|(?:\[\s*(?:emotion|mood|state)\s*:\s*[^\]]+\])|(?:\(\s*(?:emotion|mood|state)\s*:\s*[^)]+\))",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex BubbleRolePrefixRegex = new(
        @"^\s*/?(?:assistant|npc|bot|system)\b(?:\s*[:>\-./\\|]+\s*|\s+|$)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex BubbleNoteWrapperRegex = new(
        @"(?:\(\s*(?:note|ooc|meta|instruction)\b[^)]*\))|(?:\[\s*(?:note|ooc|meta|instruction)\b[^\]]*\])",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex BubbleInstructionRegex = new(
        @"\b(?:note|ooc|meta|instruction|reply in|no further response needed|do not|bubble-ready|in-character sentences only|emit commands?|continue naturally from|face-to-face encounter turn|keep command names and argument keys in english|localize only player-facing values|structured command outputs?|json|schema|mutation names?)\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex EncounterPromptScaffoldRegex = new(
        @"(?:^LANGUAGE_RULE\b|^Face-to-face encounter turn\b|^Current time:\s*|^Context:\s*|^Continuation context:\s*|^Opener style:\s*|\bReply in\s+1-2\s+short\b|\bDo not emit commands\b|\bSpeak directly to\b|\bContinue naturally from:\s*|bubble-ready\b|in-character sentences only\b|^You are\s+[^,]+,\s|^Wrap up the conversation naturally\b|^Keep it grounded and specific\b|^The previous line already sounded like a goodbye\b|^If you already said goodbye\b|^You already spoke with\b|^Greet them naturally\b|\bDo not repeat the same farewell\b|\bKeep command names and argument keys in English\b|\blocalize only player-facing values\b|^QUEST_RULE\b|^EVENT_QUALITY_RULE\b|^RUMOR_CMD_RULE\b|^SOCIAL_RULE\b|^INTEREST_RULE\b|^MARKET_MOD_RULE\b|^LANGUAGE_RULE\b|^STYLE:\s*For structured command outputs\b)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex EncounterCommandLeakRegex = new(
        @"(?:^\s*(?:adjust[_\s]+reputation|shift[_\s]+interest[_\s]+influence|apply[_\s]+market[_\s]+modifier|spread[_\s]+rumor|publish[_\s]+rumor|publish[_\s]+article|propose[_\s]+quest|record[_\s]+town[_\s]+event|record[_\s]+memory[_\s]+fact|adjust[_\s]+town[_\s]+sentiment|update[_\s]+romance[_\s]+profile|propose[_\s]+micro[_\s]+date)\b|""(?:command|arguments|npc_id|intent_id)""\s*:)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex AsciiWordPattern = new(
        @"[A-Za-z]{4,}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    internal static string StripEmotionMetadata(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;
        return LeadingMetadataRegex.Replace(text, string.Empty).TrimStart();
    }

    internal static string SanitizeBubbleText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var cleaned = StripEmotionMetadata(text);
        cleaned = InlineMetadataRegex.Replace(cleaned, string.Empty);
        cleaned = BubbleNoteWrapperRegex.Replace(cleaned, string.Empty);

        var keptUnits = new List<string>();
        foreach (var unit in SplitSentenceUnits(Normalize(cleaned)))
        {
            var sanitizedUnit = BubbleRolePrefixRegex.Replace(unit.Trim(), string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(sanitizedUnit))
                continue;
            if (BubbleInstructionRegex.IsMatch(sanitizedUnit))
                continue;

            keptUnits.Add(sanitizedUnit);
        }

        return Normalize(string.Join(" ", keptUnits));
    }

    internal static string SanitizeEncounterBubbleText(string? text)
    {
        if (LooksLikeEncounterCommandLeak(text))
            return string.Empty;

        var cleaned = SanitizeBubbleText(text);
        if (string.IsNullOrWhiteSpace(cleaned))
            return string.Empty;

        var locale = NormalizeLocale(I18n.GetCurrentLocaleCode());
        var keptUnits = new List<string>();
        foreach (var unit in SplitSentenceUnits(cleaned))
        {
            var trimmed = Normalize(unit).Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                continue;
            if (EncounterPromptScaffoldRegex.IsMatch(trimmed))
                continue;
            if (IsEnglishLeakSensitiveLocale(locale) && LooksLikeEnglishPromptLeak(trimmed))
                continue;

            keptUnits.Add(trimmed);
        }

        return Normalize(string.Join(" ", keptUnits));
    }

    internal static bool LooksLikeEncounterCommandLeak(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        return EncounterCommandLeakRegex.IsMatch(text);
    }

    internal static bool LooksLikeEncounterPromptLeak(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        return EncounterPromptScaffoldRegex.IsMatch(text)
            || text.Contains("Keep command names and argument keys in English", StringComparison.OrdinalIgnoreCase)
            || text.Contains("localize only player-facing values", StringComparison.OrdinalIgnoreCase)
            || text.Contains("structured command outputs", StringComparison.OrdinalIgnoreCase)
            || text.Contains("Face-to-face encounter turn", StringComparison.OrdinalIgnoreCase)
            || text.Contains("bubble-ready", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeEnglishPromptLeak(string text)
    {
        return AsciiWordPattern.IsMatch(text) && EncounterPromptScaffoldRegex.IsMatch(text);
    }

    private static bool IsEnglishLeakSensitiveLocale(string locale)
    {
        return locale == "ja"
            || locale == "ko"
            || locale == "ru"
            || locale.StartsWith("zh", StringComparison.Ordinal);
    }

    private static string NormalizeLocale(string? locale)
    {
        if (string.IsNullOrWhiteSpace(locale))
            return "en";

        return locale.Trim().Replace('_', '-').ToLowerInvariant();
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
