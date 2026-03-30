using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using StardewLivingRPG.State;

namespace StardewLivingRPG.Systems;

public sealed class NpcTranscriptArchiveService
{
    private const int HotRawDays = 14;
    private const int MinHotRawExchanges = 120;
    private const int ChunkTargetSize = 24;
    private const int MaxCandidateChunksToDecompress = 2;
    private const int PerNpcCompressedPayloadCapBytes = 512 * 1024;
    private const int PerNpcPayloadExchangeCap = 1000;
    private const int GlobalCompressedPayloadCapBytes = 8 * 1024 * 1024;

    private static readonly string[] SecretPhrases =
    {
        "secret", "between us", "don't tell", "dont tell", "keep this private", "keep it private", "confidential"
    };

    private static readonly string[] PromisePhrases =
    {
        "promise", "promised", "i will", "i'll", "ill", "count on me", "deal", "agreed", "swore"
    };

    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.General);
    private readonly Dictionary<string, Dictionary<string, HashSet<string>>> _rawKeywordPostingsByNpc = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Dictionary<string, HashSet<string>>> _chunkKeywordPostingsByNpc = new(StringComparer.OrdinalIgnoreCase);

    public string BeginPendingExchange(
        SaveState state,
        string npcName,
        string npcDisplayName,
        string playerText,
        int day,
        int timeOfDay,
        string season,
        int year,
        string locationName,
        string contextTag)
    {
        if (string.IsNullOrWhiteSpace(npcName))
            return string.Empty;

        var archive = GetArchive(state, npcName);
        var requestToken = Guid.NewGuid().ToString("N");
        archive.PendingExchanges.Add(new PendingTranscriptExchange
        {
            ExchangeId = Guid.NewGuid().ToString("N"),
            RequestToken = requestToken,
            NpcId = npcName.Trim(),
            NpcDisplayName = string.IsNullOrWhiteSpace(npcDisplayName) ? npcName.Trim() : npcDisplayName.Trim(),
            Day = day,
            TimeOfDay = timeOfDay,
            Season = string.IsNullOrWhiteSpace(season) ? "spring" : season.Trim(),
            Year = Math.Max(1, year),
            LocationName = (locationName ?? string.Empty).Trim(),
            ContextTag = string.IsNullOrWhiteSpace(contextTag) ? "player_chat" : contextTag.Trim(),
            PlayerText = (playerText ?? string.Empty).Trim(),
            Visibility = ComputeVisibility(playerText),
            SourceRefKind = "chat",
            SourceRefId = requestToken
        });
        archive.LastUpdatedDay = day;
        return requestToken;
    }

    public TranscriptExchange? CompletePendingExchange(
        SaveState state,
        string npcName,
        string npcText,
        int day,
        string completionState = "complete")
    {
        if (string.IsNullOrWhiteSpace(npcName))
            return null;

        var archive = GetArchive(state, npcName);
        if (archive.PendingExchanges.Count == 0)
            return null;

        var pending = archive.PendingExchanges[0];
        archive.PendingExchanges.RemoveAt(0);
        var combined = $"{pending.PlayerText} {npcText}".Trim();
        var exchange = new TranscriptExchange
        {
            ExchangeId = pending.ExchangeId,
            RequestToken = pending.RequestToken,
            NpcId = pending.NpcId,
            NpcDisplayName = pending.NpcDisplayName,
            Day = pending.Day,
            TimeOfDay = pending.TimeOfDay,
            Season = pending.Season,
            Year = pending.Year,
            LocationName = pending.LocationName,
            ContextTag = pending.ContextTag,
            PlayerText = pending.PlayerText,
            NpcText = (npcText ?? string.Empty).Trim(),
            Keywords = ExtractKeywords(combined),
            Importance = ComputeImportance(combined, pending.ContextTag),
            Visibility = NormalizeVisibility(pending.Visibility, combined),
            CompletionState = NormalizeCompletionState(completionState),
            SourceRefKind = pending.SourceRefKind,
            SourceRefId = pending.SourceRefId,
            LinkedImportantMemoryIds = new List<string>()
        };

        archive.RawExchanges.Add(exchange);
        archive.LastUpdatedDay = Math.Max(day, pending.Day);
        IndexRawExchange(exchange);
        return exchange;
    }

    public int FinalizePendingExchanges(SaveState state, int day)
    {
        var finalized = 0;
        foreach (var archive in state.TranscriptArchive.Archives.Values)
        {
            while (archive.PendingExchanges.Count > 0)
            {
                var pending = archive.PendingExchanges[0];
                archive.PendingExchanges.RemoveAt(0);
                archive.RawExchanges.Add(new TranscriptExchange
                {
                    ExchangeId = pending.ExchangeId,
                    RequestToken = pending.RequestToken,
                    NpcId = pending.NpcId,
                    NpcDisplayName = pending.NpcDisplayName,
                    Day = pending.Day,
                    TimeOfDay = pending.TimeOfDay,
                    Season = pending.Season,
                    Year = pending.Year,
                    LocationName = pending.LocationName,
                    ContextTag = pending.ContextTag,
                    PlayerText = pending.PlayerText,
                    NpcText = string.Empty,
                    Keywords = ExtractKeywords(pending.PlayerText),
                    Importance = ComputeImportance(pending.PlayerText, pending.ContextTag),
                    Visibility = pending.Visibility,
                    CompletionState = "timed_out",
                    SourceRefKind = pending.SourceRefKind,
                    SourceRefId = pending.SourceRefId,
                    LinkedImportantMemoryIds = new List<string>()
                });
                archive.LastUpdatedDay = Math.Max(archive.LastUpdatedDay, day);
                finalized++;
            }
        }

        if (finalized > 0)
            RebuildTransientIndexes(state);

        return finalized;
    }

    public bool HasTranscriptHistory(SaveState state, string npcName)
    {
        return !string.IsNullOrWhiteSpace(npcName)
            && state.TranscriptArchive.Archives.TryGetValue(npcName, out var archive)
            && ((archive.RawExchanges?.Count ?? 0) > 0 || (archive.Chunks?.Count ?? 0) > 0);
    }

    public bool TryBuildGreetingCue(SaveState state, string npcName, out string greeting)
    {
        greeting = string.Empty;
        if (!state.TranscriptArchive.Archives.TryGetValue(npcName, out var archive))
            return false;

        var latest = archive.RawExchanges
            .Where(exchange => !string.IsNullOrWhiteSpace(exchange.PlayerText) || !string.IsNullOrWhiteSpace(exchange.NpcText))
            .OrderByDescending(exchange => exchange.Day)
            .ThenByDescending(exchange => exchange.TimeOfDay)
            .FirstOrDefault();
        if (latest is not null)
        {
            var topic = latest.Keywords.FirstOrDefault(keyword => keyword.Length >= 4);
            greeting = string.IsNullOrWhiteSpace(topic)
                ? "I still remember our last conversation."
                : $"I still remember our last talk about {topic}.";
            return true;
        }

        if (!archive.Chunks.Any(chunk => !string.IsNullOrWhiteSpace(chunk.Summary)))
            return false;

        greeting = "I still remember the shape of our earlier conversations.";
        return true;
    }

    public bool TryBuildGroundedReply(SaveState state, string npcName, out string reply)
    {
        reply = string.Empty;
        if (!state.TranscriptArchive.Archives.TryGetValue(npcName, out var archive))
            return false;

        var latest = archive.RawExchanges
            .Where(exchange => !string.IsNullOrWhiteSpace(exchange.NpcText))
            .OrderByDescending(exchange => exchange.Day)
            .ThenByDescending(exchange => exchange.TimeOfDay)
            .FirstOrDefault();
        if (latest is not null)
        {
            reply = $"Last time we left off here: {BuildReplySnippet(latest)}";
            return true;
        }

        var latestChunk = archive.Chunks
            .OrderByDescending(chunk => chunk.DayRangeEnd)
            .FirstOrDefault(chunk => !string.IsNullOrWhiteSpace(chunk.Summary));
        if (latestChunk is null)
            return false;

        reply = $"We've talked about this before: {EnsureSentenceTerminal(TrimForPrompt(latestChunk.Summary, 96))}";
        return true;
    }

    public string BuildTranscriptRecallBlock(SaveState state, string npcName, string playerText, int day, int topK = 3, int charCap = 420)
    {
        var results = QueryRelevantTranscriptSnippets(state, npcName, playerText, day, topK);
        if (results.Count == 0)
            return string.Empty;

        var parts = results.Select(result => $"Day {result.Day}: {result.Snippet}");
        return JoinWithinCap($"NPC_TRANSCRIPT_RECALL[{npcName}]: ", parts, charCap);
    }

    public void RollWarmChunks(SaveState state, int currentDay)
    {
        var changed = false;
        foreach (var archive in state.TranscriptArchive.Archives.Values)
        {
            var raw = archive.RawExchanges
                .OrderBy(exchange => exchange.Day)
                .ThenBy(exchange => exchange.TimeOfDay)
                .ToList();
            if (raw.Count <= ChunkTargetSize)
                continue;

            var hotStartByCount = Math.Max(0, raw.Count - MinHotRawExchanges);
            var hotCutoffDay = currentDay - HotRawDays;
            var hotStartByDay = raw.FindIndex(exchange => exchange.Day >= hotCutoffDay);
            if (hotStartByDay < 0)
                hotStartByDay = raw.Count;

            var keepStart = Math.Min(hotStartByCount, hotStartByDay);
            if (keepStart < ChunkTargetSize)
                continue;

            var compressible = raw.Take(keepStart).ToList();
            if (compressible.Count < ChunkTargetSize)
                continue;

            foreach (var batch in Batch(compressible, ChunkTargetSize))
            {
                var exchanges = batch.ToList();
                if (exchanges.Count == 0)
                    continue;

                archive.Chunks.Add(BuildChunkHeader(exchanges));
                changed = true;
            }

            archive.RawExchanges = raw.Skip(keepStart).ToList();
        }

        if (changed)
            RebuildTransientIndexes(state);
    }

    public void PruneArchiveIfNeeded(SaveState state)
    {
        foreach (var archive in state.TranscriptArchive.Archives.Values)
            PruneNpcArchive(archive);

        var globalPayloadBytes = state.TranscriptArchive.Archives.Values.Sum(GetPayloadBytes);
        if (globalPayloadBytes <= GlobalCompressedPayloadCapBytes)
            return;

        foreach (var chunk in state.TranscriptArchive.Archives.Values
                     .SelectMany(archive => archive.Chunks)
                     .Where(chunk => !string.IsNullOrWhiteSpace(chunk.CompressedPayloadBase64))
                     .OrderBy(chunk => chunk.DayRangeEnd)
                     .ThenBy(chunk => chunk.DayRangeStart))
        {
            chunk.CompressionCodec = "summary_only";
            chunk.CompressedPayloadBase64 = string.Empty;
            globalPayloadBytes = state.TranscriptArchive.Archives.Values.Sum(GetPayloadBytes);
            if (globalPayloadBytes <= GlobalCompressedPayloadCapBytes)
                break;
        }
    }

    public void RebuildTransientIndexes(SaveState state)
    {
        _rawKeywordPostingsByNpc.Clear();
        _chunkKeywordPostingsByNpc.Clear();

        foreach (var (npcName, archive) in state.TranscriptArchive.Archives)
        {
            foreach (var exchange in archive.RawExchanges ?? Enumerable.Empty<TranscriptExchange>())
                IndexRawExchange(exchange, npcName);

            foreach (var chunk in archive.Chunks ?? Enumerable.Empty<TranscriptChunkHeader>())
                IndexChunk(chunk, npcName);
        }
    }

    public string DescribeArchive(SaveState state, string npcName)
    {
        if (!state.TranscriptArchive.Archives.TryGetValue(npcName, out var archive))
            return $"Transcript archive {npcName}: raw=0, chunks=0, payload_chunks=0, pending=0";

        var payloadChunks = archive.Chunks.Count(chunk => !string.IsNullOrWhiteSpace(chunk.CompressedPayloadBase64));
        var allDays = archive.RawExchanges.Select(exchange => exchange.Day)
            .Concat(archive.Chunks.SelectMany(chunk => new[] { chunk.DayRangeStart, chunk.DayRangeEnd }))
            .ToList();
        var oldestDay = allDays.Count == 0 ? 0 : allDays.Min();
        var newestDay = allDays.Count == 0 ? 0 : allDays.Max();
        return $"Transcript archive {npcName}: raw={archive.RawExchanges.Count}, chunks={archive.Chunks.Count}, payload_chunks={payloadChunks}, pending={archive.PendingExchanges.Count}, oldest_day={oldestDay}, newest_day={newestDay}";
    }

    private List<TranscriptQueryResult> QueryRelevantTranscriptSnippets(SaveState state, string npcName, string playerText, int day, int topK)
    {
        if (string.IsNullOrWhiteSpace(npcName)
            || !state.TranscriptArchive.Archives.TryGetValue(npcName, out var archive))
        {
            return new List<TranscriptQueryResult>();
        }

        var queryTokens = ExtractKeywords(playerText).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var promptLower = (playerText ?? string.Empty).Trim().ToLowerInvariant();
        var results = new List<TranscriptQueryResult>();

        foreach (var exchange in SelectCandidateRawExchanges(archive, npcName, queryTokens))
        {
            var score = ScoreExchange(exchange, queryTokens, promptLower, day);
            if (score <= 0)
                continue;

            results.Add(new TranscriptQueryResult
            {
                ExchangeId = exchange.ExchangeId,
                Day = exchange.Day,
                Score = score,
                Snippet = BuildPromptSnippet(exchange)
            });
        }

        foreach (var chunk in SelectCandidateChunks(archive, npcName, queryTokens))
        {
            if (string.IsNullOrWhiteSpace(chunk.CompressedPayloadBase64))
            {
                var summaryScore = ScoreChunkSummary(chunk, queryTokens, promptLower, day);
                if (summaryScore > 0)
                {
                    results.Add(new TranscriptQueryResult
                    {
                        ExchangeId = chunk.ChunkId,
                        Day = chunk.DayRangeEnd,
                        Score = summaryScore,
                        Snippet = EnsureSentenceTerminal(TrimForPrompt(chunk.Summary, 130))
                    });
                }

                continue;
            }

            foreach (var exchange in RestoreChunkExchanges(chunk)
                         .OrderByDescending(exchange => exchange.Day)
                         .ThenByDescending(exchange => exchange.TimeOfDay))
            {
                var score = ScoreExchange(exchange, queryTokens, promptLower, day);
                if (score <= 0)
                    continue;

                results.Add(new TranscriptQueryResult
                {
                    ExchangeId = exchange.ExchangeId,
                    Day = exchange.Day,
                    Score = score,
                    Snippet = BuildPromptSnippet(exchange)
                });
            }
        }

        return results
            .OrderByDescending(result => result.Score)
            .ThenByDescending(result => result.Day)
            .GroupBy(result => result.ExchangeId, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Take(topK)
            .ToList();
    }

    private void IndexRawExchange(TranscriptExchange exchange, string? npcNameOverride = null)
    {
        var npcName = string.IsNullOrWhiteSpace(npcNameOverride) ? exchange.NpcId : npcNameOverride;
        if (string.IsNullOrWhiteSpace(npcName))
            return;

        var postings = GetOrCreatePostings(_rawKeywordPostingsByNpc, npcName);
        foreach (var keyword in exchange.Keywords ?? Array.Empty<string>())
        {
            if (string.IsNullOrWhiteSpace(keyword))
                continue;

            if (!postings.TryGetValue(keyword, out var ids))
            {
                ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                postings[keyword] = ids;
            }

            ids.Add(exchange.ExchangeId);
        }
    }

    private void IndexChunk(TranscriptChunkHeader chunk, string? npcNameOverride = null)
    {
        var npcName = string.IsNullOrWhiteSpace(npcNameOverride) ? chunk.NpcId : npcNameOverride;
        if (string.IsNullOrWhiteSpace(npcName))
            return;

        var postings = GetOrCreatePostings(_chunkKeywordPostingsByNpc, npcName);
        foreach (var keyword in chunk.TopKeywords ?? Array.Empty<string>())
        {
            if (string.IsNullOrWhiteSpace(keyword))
                continue;

            if (!postings.TryGetValue(keyword, out var ids))
            {
                ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                postings[keyword] = ids;
            }

            ids.Add(chunk.ChunkId);
        }
    }

    private static NpcTranscriptArchive GetArchive(SaveState state, string npcName)
    {
        if (!state.TranscriptArchive.Archives.TryGetValue(npcName, out var archive))
        {
            var existingKey = state.TranscriptArchive.Archives.Keys.FirstOrDefault(key =>
                string.Equals(key, npcName, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(existingKey) && state.TranscriptArchive.Archives.TryGetValue(existingKey, out archive))
                return archive;

            archive = new NpcTranscriptArchive();
            state.TranscriptArchive.Archives[npcName] = archive;
        }

        return archive;
    }

    private IEnumerable<TranscriptExchange> SelectCandidateRawExchanges(
        NpcTranscriptArchive archive,
        string npcName,
        HashSet<string> queryTokens)
    {
        if (archive.RawExchanges.Count == 0)
            return Enumerable.Empty<TranscriptExchange>();

        if (queryTokens.Count == 0 || !_rawKeywordPostingsByNpc.TryGetValue(npcName, out var postings))
            return archive.RawExchanges.TakeLast(12);

        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var token in queryTokens)
        {
            if (postings.TryGetValue(token, out var exchangeIds))
                ids.UnionWith(exchangeIds);
        }

        return ids.Count == 0
            ? archive.RawExchanges.TakeLast(12)
            : archive.RawExchanges.Where(exchange => ids.Contains(exchange.ExchangeId)).TakeLast(24);
    }

    private IEnumerable<TranscriptChunkHeader> SelectCandidateChunks(
        NpcTranscriptArchive archive,
        string npcName,
        HashSet<string> queryTokens)
    {
        if (archive.Chunks.Count == 0)
            return Enumerable.Empty<TranscriptChunkHeader>();

        if (queryTokens.Count == 0 || !_chunkKeywordPostingsByNpc.TryGetValue(npcName, out var postings))
            return archive.Chunks.OrderByDescending(chunk => chunk.DayRangeEnd).Take(MaxCandidateChunksToDecompress);

        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var token in queryTokens)
        {
            if (postings.TryGetValue(token, out var chunkIds))
                ids.UnionWith(chunkIds);
        }

        var candidates = archive.Chunks
            .Where(chunk => ids.Count == 0 || ids.Contains(chunk.ChunkId))
            .OrderByDescending(chunk => CountOverlap(chunk.TopKeywords, queryTokens))
            .ThenByDescending(chunk => chunk.DayRangeEnd)
            .Take(MaxCandidateChunksToDecompress)
            .ToList();

        return candidates.Count == 0
            ? archive.Chunks.OrderByDescending(chunk => chunk.DayRangeEnd).Take(MaxCandidateChunksToDecompress)
            : candidates;
    }

    private void PruneNpcArchive(NpcTranscriptArchive archive)
    {
        var payloadBytes = GetPayloadBytes(archive);
        var payloadExchangeCount = archive.RawExchanges.Count
            + archive.Chunks.Where(chunk => !string.IsNullOrWhiteSpace(chunk.CompressedPayloadBase64)).Sum(chunk => chunk.ExchangeCount);

        if (payloadBytes <= PerNpcCompressedPayloadCapBytes && payloadExchangeCount <= PerNpcPayloadExchangeCap)
            return;

        foreach (var chunk in archive.Chunks
                     .Where(chunk => !string.IsNullOrWhiteSpace(chunk.CompressedPayloadBase64))
                     .OrderBy(chunk => chunk.DayRangeEnd)
                     .ThenBy(chunk => chunk.DayRangeStart))
        {
            chunk.CompressionCodec = "summary_only";
            chunk.CompressedPayloadBase64 = string.Empty;

            payloadBytes = GetPayloadBytes(archive);
            payloadExchangeCount = archive.RawExchanges.Count
                + archive.Chunks.Where(c => !string.IsNullOrWhiteSpace(c.CompressedPayloadBase64)).Sum(c => c.ExchangeCount);
            if (payloadBytes <= PerNpcCompressedPayloadCapBytes && payloadExchangeCount <= PerNpcPayloadExchangeCap)
                break;
        }
    }

    private TranscriptChunkHeader BuildChunkHeader(List<TranscriptExchange> exchanges)
    {
        var payloadJson = JsonSerializer.Serialize(exchanges, _jsonOptions);
        var topKeywords = exchanges
            .SelectMany(exchange => exchange.Keywords ?? Array.Empty<string>())
            .Where(keyword => !string.IsNullOrWhiteSpace(keyword))
            .GroupBy(keyword => keyword, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Take(18)
            .Select(group => group.Key)
            .ToArray();

        return new TranscriptChunkHeader
        {
            ChunkId = Guid.NewGuid().ToString("N"),
            NpcId = exchanges[0].NpcId,
            DayRangeStart = exchanges.Min(exchange => exchange.Day),
            DayRangeEnd = exchanges.Max(exchange => exchange.Day),
            ExchangeCount = exchanges.Count,
            Summary = BuildChunkSummary(exchanges),
            TopKeywords = topKeywords,
            ImportanceMax = exchanges.Max(exchange => exchange.Importance),
            CompressionCodec = "gzip",
            CompressedPayloadBase64 = CompressToBase64(payloadJson)
        };
    }

    private static string BuildChunkSummary(List<TranscriptExchange> exchanges)
    {
        var standout = exchanges
            .OrderByDescending(exchange => exchange.Importance)
            .ThenByDescending(exchange => exchange.Day)
            .FirstOrDefault();
        if (standout is null)
            return string.Empty;

        if (!string.IsNullOrWhiteSpace(standout.PlayerText) && !string.IsNullOrWhiteSpace(standout.NpcText))
            return $"Player said '{TrimForPrompt(standout.PlayerText, 50)}' and {standout.NpcDisplayName} replied '{TrimForPrompt(standout.NpcText, 50)}'.";

        return !string.IsNullOrWhiteSpace(standout.PlayerText)
            ? $"Player said '{TrimForPrompt(standout.PlayerText, 90)}'."
            : $"{standout.NpcDisplayName} said '{TrimForPrompt(standout.NpcText, 90)}'.";
    }

    private static Dictionary<string, HashSet<string>> GetOrCreatePostings(
        Dictionary<string, Dictionary<string, HashSet<string>>> postingsByNpc,
        string npcName)
    {
        if (!postingsByNpc.TryGetValue(npcName, out var postings))
        {
            postings = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            postingsByNpc[npcName] = postings;
        }

        return postings;
    }

    private static IEnumerable<IEnumerable<T>> Batch<T>(IEnumerable<T> source, int size)
    {
        var batch = new List<T>(size);
        foreach (var item in source)
        {
            batch.Add(item);
            if (batch.Count < size)
                continue;

            yield return batch;
            batch = new List<T>(size);
        }

        if (batch.Count > 0)
            yield return batch;
    }

    private static IEnumerable<TranscriptExchange> RestoreChunkExchanges(TranscriptChunkHeader chunk)
    {
        if (string.IsNullOrWhiteSpace(chunk.CompressedPayloadBase64))
            return Enumerable.Empty<TranscriptExchange>();

        try
        {
            var json = DecompressFromBase64(chunk.CompressedPayloadBase64);
            return JsonSerializer.Deserialize<List<TranscriptExchange>>(json) ?? Enumerable.Empty<TranscriptExchange>();
        }
        catch
        {
            return Enumerable.Empty<TranscriptExchange>();
        }
    }

    private static string CompressToBase64(string text)
    {
        var input = Encoding.UTF8.GetBytes(text);
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.SmallestSize, leaveOpen: true))
            gzip.Write(input, 0, input.Length);
        return Convert.ToBase64String(output.ToArray());
    }

    private static string DecompressFromBase64(string base64)
    {
        var input = Convert.FromBase64String(base64);
        using var inputStream = new MemoryStream(input);
        using var gzip = new GZipStream(inputStream, CompressionMode.Decompress);
        using var output = new MemoryStream();
        gzip.CopyTo(output);
        return Encoding.UTF8.GetString(output.ToArray());
    }

    private static string[] ExtractKeywords(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<string>();

        var keywords = new List<string>();
        foreach (Match match in Regex.Matches(text.ToLowerInvariant(), @"[\p{L}\p{N}][\p{L}\p{N}_'-]{2,}", RegexOptions.CultureInvariant))
        {
            var token = match.Value.Trim('\'', '"', '.', ',', '!', '?', ';', ':');
            if (token.Length >= 3)
                keywords.Add(token);
        }

        foreach (var phrase in SecretPhrases.Concat(PromisePhrases).Concat(new[] { "last time", "you said" }))
        {
            if (text.Contains(phrase, StringComparison.OrdinalIgnoreCase))
                keywords.Add(phrase);
        }

        if (keywords.Count < 3)
            keywords.AddRange(BuildCharacterNGrams(text, 3, 12));

        return keywords
            .Where(keyword => !string.IsNullOrWhiteSpace(keyword))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(24)
            .ToArray();
    }

    private static int ComputeImportance(string? text, string? contextTag)
    {
        var combined = (text ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(combined))
            return 1;

        var importance = 1;
        if (ContainsAny(combined, SecretPhrases))
            importance = Math.Max(importance, 5);
        if (ContainsAny(combined, PromisePhrases))
            importance = Math.Max(importance, 4);
        if (combined.Contains("favorite", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("prefer", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("like", StringComparison.OrdinalIgnoreCase))
        {
            importance = Math.Max(importance, 3);
        }
        if (!string.IsNullOrWhiteSpace(contextTag)
            && contextTag.Contains("quest", StringComparison.OrdinalIgnoreCase))
        {
            importance = Math.Max(importance, 4);
        }

        return Math.Clamp(importance, 1, 5);
    }

    private static string ComputeVisibility(string? text)
    {
        return ContainsAny(text ?? string.Empty, SecretPhrases) ? "private" : "npc_only";
    }

    private static string NormalizeVisibility(string visibility, string text)
    {
        if (string.Equals(visibility, "private", StringComparison.OrdinalIgnoreCase))
            return "private";
        if (string.Equals(visibility, "shareable", StringComparison.OrdinalIgnoreCase))
            return "shareable";
        return ComputeVisibility(text);
    }

    private static string NormalizeCompletionState(string? completionState)
    {
        var value = (completionState ?? string.Empty).Trim().ToLowerInvariant();
        return value switch
        {
            "fallback_completed" => "fallback_completed",
            "player_only" => "player_only",
            "timed_out" => "timed_out",
            _ => "complete"
        };
    }

    private static string BuildReplySnippet(TranscriptExchange exchange)
    {
        if (!string.IsNullOrWhiteSpace(exchange.NpcText))
            return EnsureSentenceTerminal(TrimForPrompt(exchange.NpcText, 96));
        if (!string.IsNullOrWhiteSpace(exchange.PlayerText))
            return EnsureSentenceTerminal(TrimForPrompt(exchange.PlayerText, 96));
        return "we were talking before.";
    }

    private static string JoinWithinCap(string prefix, IEnumerable<string> parts, int charCap)
    {
        var builder = new StringBuilder(prefix ?? string.Empty);
        var prefixLength = builder.Length;
        foreach (var part in parts.Where(part => !string.IsNullOrWhiteSpace(part)))
        {
            var separator = builder.Length == prefixLength ? string.Empty : " ";
            if (builder.Length + separator.Length + part.Length > charCap)
                break;

            builder.Append(separator);
            builder.Append(part);
        }

        return builder.Length == prefixLength ? string.Empty : builder.ToString();
    }

    private static string TrimForPrompt(string? text, int max)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var normalized = Regex.Replace(text.Trim(), @"\s+", " ", RegexOptions.CultureInvariant);
        return normalized.Length > max ? normalized[..max] + "..." : normalized;
    }

    private static string EnsureSentenceTerminal(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        return text.EndsWith(".", StringComparison.Ordinal)
            || text.EndsWith("!", StringComparison.Ordinal)
            || text.EndsWith("?", StringComparison.Ordinal)
            ? text
            : text + ".";
    }

    private static int ScoreExchange(TranscriptExchange exchange, HashSet<string> queryTokens, string promptLower, int day)
    {
        var age = Math.Max(0, day - exchange.Day);
        var score = exchange.Importance * 10 + Math.Max(0, 28 - age);
        score += CountOverlap(exchange.Keywords, queryTokens) * 8;

        if (promptLower.Contains("remember", StringComparison.Ordinal))
            score += 4;
        if (ContainsAny(promptLower, PromisePhrases) && ContainsAny(exchange.PlayerText + " " + exchange.NpcText, PromisePhrases))
            score += 14;
        if (ContainsAny(promptLower, SecretPhrases) && ContainsAny(exchange.PlayerText + " " + exchange.NpcText, SecretPhrases))
            score += 16;
        if (exchange.CompletionState.Equals("player_only", StringComparison.OrdinalIgnoreCase)
            || exchange.CompletionState.Equals("timed_out", StringComparison.OrdinalIgnoreCase))
        {
            score -= 3;
        }

        return score;
    }

    private static int ScoreChunkSummary(TranscriptChunkHeader chunk, HashSet<string> queryTokens, string promptLower, int day)
    {
        if (string.IsNullOrWhiteSpace(chunk.Summary))
            return 0;

        var age = Math.Max(0, day - chunk.DayRangeEnd);
        var score = chunk.ImportanceMax * 8 + Math.Max(0, 20 - age);
        score += CountOverlap(chunk.TopKeywords, queryTokens) * 6;
        if (promptLower.Contains("remember", StringComparison.Ordinal))
            score += 2;

        return score;
    }

    private static string BuildPromptSnippet(TranscriptExchange exchange)
    {
        if (!string.IsNullOrWhiteSpace(exchange.PlayerText) && !string.IsNullOrWhiteSpace(exchange.NpcText))
            return $"Player '{TrimForPrompt(exchange.PlayerText, 48)}' / NPC '{TrimForPrompt(exchange.NpcText, 48)}'.";
        if (!string.IsNullOrWhiteSpace(exchange.PlayerText))
            return $"Player '{TrimForPrompt(exchange.PlayerText, 88)}'.";
        return $"NPC '{TrimForPrompt(exchange.NpcText, 88)}'.";
    }

    private static IEnumerable<string> BuildCharacterNGrams(string text, int gramSize, int maxCount)
    {
        var normalized = new string(text.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
        if (normalized.Length < gramSize)
            yield break;

        var count = 0;
        for (var i = 0; i <= normalized.Length - gramSize && count < maxCount; i++)
        {
            yield return normalized.Substring(i, gramSize);
            count++;
        }
    }

    private static bool ContainsAny(string text, IEnumerable<string> phrases)
    {
        foreach (var phrase in phrases)
        {
            if (!string.IsNullOrWhiteSpace(phrase) && text.Contains(phrase, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static int CountOverlap(IEnumerable<string> values, HashSet<string> queryTokens)
    {
        var count = 0;
        foreach (var value in values ?? Enumerable.Empty<string>())
        {
            if (!string.IsNullOrWhiteSpace(value) && queryTokens.Contains(value))
                count++;
        }

        return count;
    }

    private static int GetPayloadBytes(NpcTranscriptArchive archive)
    {
        return archive.Chunks.Sum(chunk => string.IsNullOrWhiteSpace(chunk.CompressedPayloadBase64) ? 0 : chunk.CompressedPayloadBase64.Length);
    }

    private sealed class TranscriptQueryResult
    {
        public string ExchangeId { get; init; } = string.Empty;
        public int Day { get; init; }
        public int Score { get; init; }
        public string Snippet { get; init; } = string.Empty;
    }
}
