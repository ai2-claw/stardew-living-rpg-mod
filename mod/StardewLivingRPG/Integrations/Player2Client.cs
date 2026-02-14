using System.Net.Http.Headers;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using StardewLivingRPG.State;

namespace StardewLivingRPG.Integrations;

public sealed class Player2Client
{
    private const int MaxGeneratedArticleCharacters = 80;
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };
    private string? _apiBaseUrl;
    private string? _p2Key;
    private string? _headlineEditorNpcId;
    private readonly SemaphoreSlim _headlineEditorNpcLock = new(1, 1);

    public Player2Client(HttpClient? httpClient = null)
    {
        _http = httpClient ?? new HttpClient();
        _http.Timeout = TimeSpan.FromSeconds(20);
    }

    public void SetCredentials(string apiBaseUrl, string p2Key)
    {
        _apiBaseUrl = apiBaseUrl;
        _p2Key = p2Key;
    }

    public async Task<string> LoginViaLocalAppAsync(string localAuthBaseUrl, string gameClientId, CancellationToken ct)
    {
        var url = $"{localAuthBaseUrl.TrimEnd('/')}/login/web/{Uri.EscapeDataString(gameClientId)}";
        using var res = await _http.PostAsync(url, content: null, ct);
        var body = await res.Content.ReadAsStringAsync(ct);
        res.EnsureSuccessStatusCode();

        var data = JsonSerializer.Deserialize<LoginWebResponse>(body, _jsonOptions);
        if (string.IsNullOrWhiteSpace(data?.P2Key))
            throw new InvalidOperationException("Player2 local login response missing p2Key.");

        return data.P2Key;
    }

    public async Task<DeviceAuthStartResponse> StartDeviceAuthAsync(string authBaseUrl, string gameClientId, CancellationToken ct)
    {
        var url = $"{authBaseUrl.TrimEnd('/')}/login/device/new";
        var payload = new Dictionary<string, string>
        {
            ["game_client_id"] = gameClientId
        };

        using var msg = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, _jsonOptions), Encoding.UTF8, "application/json")
        };

        using var res = await _http.SendAsync(msg, ct);
        var body = await res.Content.ReadAsStringAsync(ct);
        res.EnsureSuccessStatusCode();

        var data = JsonSerializer.Deserialize<DeviceAuthStartResponse>(body, _jsonOptions) ?? new DeviceAuthStartResponse();
        if (string.IsNullOrWhiteSpace(data.DeviceCode))
            throw new InvalidOperationException("Player2 device auth start missing device_code.");

        return data;
    }

    public async Task<DeviceAuthTokenPollResult> PollDeviceAuthTokenAsync(string authBaseUrl, string deviceCode, CancellationToken ct)
    {
        var url = $"{authBaseUrl.TrimEnd('/')}/login/device/token";
        var payload = new Dictionary<string, string>
        {
            ["device_code"] = deviceCode
        };

        using var msg = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, _jsonOptions), Encoding.UTF8, "application/json")
        };

        using var res = await _http.SendAsync(msg, ct);
        var body = await res.Content.ReadAsStringAsync(ct);

        if (res.IsSuccessStatusCode)
        {
            var ok = JsonSerializer.Deserialize<DeviceAuthTokenSuccessResponse>(body, _jsonOptions);
            if (string.IsNullOrWhiteSpace(ok?.P2Key))
                return new DeviceAuthTokenPollResult { Status = "pending" };

            return new DeviceAuthTokenPollResult
            {
                Status = "authorized",
                P2Key = ok.P2Key
            };
        }

        var err = JsonSerializer.Deserialize<DeviceAuthTokenErrorResponse>(body, _jsonOptions);
        var status = (err?.Status ?? err?.Error ?? "pending").Trim().ToLowerInvariant();
        return new DeviceAuthTokenPollResult
        {
            Status = status,
            ErrorMessage = err?.Message
        };
    }

    public async Task<string> SpawnNpcAsync(string apiBaseUrl, string p2Key, SpawnNpcRequest req, CancellationToken ct)
    {
        var url = $"{apiBaseUrl.TrimEnd('/')}/npcs/spawn";
        using var msg = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(req, _jsonOptions), Encoding.UTF8, "application/json")
        };
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", p2Key);

        using var res = await _http.SendAsync(msg, ct);
        var body = await res.Content.ReadAsStringAsync(ct);
        res.EnsureSuccessStatusCode();

        var npcId = JsonSerializer.Deserialize<string>(body, _jsonOptions);
        if (string.IsNullOrWhiteSpace(npcId))
            throw new InvalidOperationException("Player2 spawn response missing npc id.");

        return npcId;
    }

    public async Task SendNpcChatAsync(string apiBaseUrl, string p2Key, string npcId, NpcChatRequest req, CancellationToken ct)
    {
        var url = $"{apiBaseUrl.TrimEnd('/')}/npcs/{Uri.EscapeDataString(npcId)}/chat";
        using var msg = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(req, _jsonOptions), Encoding.UTF8, "application/json")
        };
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", p2Key);

        using var res = await _http.SendAsync(msg, ct);
        res.EnsureSuccessStatusCode();
    }

    public async Task<JoulesResponse> GetJoulesAsync(string apiBaseUrl, string p2Key, CancellationToken ct)
    {
        var url = $"{apiBaseUrl.TrimEnd('/')}/joules";
        using var msg = new HttpRequestMessage(HttpMethod.Get, url);
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", p2Key);

        using var res = await _http.SendAsync(msg, ct);
        var body = await res.Content.ReadAsStringAsync(ct);
        res.EnsureSuccessStatusCode();

        var data = JsonSerializer.Deserialize<JoulesResponse>(body, _jsonOptions);
        return data ?? new JoulesResponse();
    }

    public async Task<string> GenerateSensationalHeadlineAsync(string apiBaseUrl, string p2Key, string articleTitle, string articleCategory, string articleContent, CancellationToken ct)
    {
        var headline = await TryGenerateSensationalHeadlineAsync(apiBaseUrl, p2Key, articleTitle, articleCategory, articleContent, ct);
        return string.IsNullOrWhiteSpace(headline) ? FallbackHeadline(articleTitle) : headline;
    }

    /// <summary>
    /// Generate sensational headline using stored credentials (SetCredentials must be called first).
    /// </summary>
    public async Task<string> GenerateSensationalHeadlineAsync(string articleTitle, string articleCategory, string articleContent, CancellationToken ct)
    {
        var headline = await TryGenerateSensationalHeadlineAsync(articleTitle, articleCategory, articleContent, ct);
        return string.IsNullOrWhiteSpace(headline) ? FallbackHeadline(articleTitle) : headline;
    }

    public async Task<string?> TryGenerateSensationalHeadlineAsync(string articleTitle, string articleCategory, string articleContent, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(_apiBaseUrl) || string.IsNullOrEmpty(_p2Key))
            return null;

        return await TryGenerateSensationalHeadlineCoreAsync(_apiBaseUrl, _p2Key, articleTitle, articleCategory, articleContent, ct);
    }

    public async Task<string?> TryGenerateSensationalHeadlineAsync(string apiBaseUrl, string p2Key, string articleTitle, string articleCategory, string articleContent, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(apiBaseUrl) || string.IsNullOrWhiteSpace(p2Key))
            return null;

        return await TryGenerateSensationalHeadlineCoreAsync(apiBaseUrl, p2Key, articleTitle, articleCategory, articleContent, ct);
    }

    private async Task<string?> TryGenerateSensationalHeadlineCoreAsync(string apiBaseUrl, string p2Key, string articleTitle, string articleCategory, string articleContent, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(apiBaseUrl) || string.IsNullOrWhiteSpace(p2Key))
            return null;

        for (var attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                var npcId = await EnsureHeadlineEditorNpcIdAsync(apiBaseUrl, p2Key, ct);
                var previousHistoryMessage = await TryGetLatestNpcMessageFromHistoryAsync(apiBaseUrl, p2Key, npcId, ct);

                var prompt = BuildHeadlinePrompt(articleTitle, articleCategory, articleContent);
                var url = $"{apiBaseUrl.TrimEnd('/')}/npcs/{Uri.EscapeDataString(npcId)}/chat";
                using var msg = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(JsonSerializer.Serialize(new NpcChatRequest
                    {
                        SenderName = "Pelican Times Desk",
                        SenderMessage = prompt,
                        GameStateInfo = $"headline_request:{articleTitle}"
                    }, _jsonOptions), Encoding.UTF8, "application/json")
                };
                msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", p2Key);

                using var res = await _http.SendAsync(msg, ct);
                var body = await res.Content.ReadAsStringAsync(ct);
                res.EnsureSuccessStatusCode();

                var immediateHeadline = SanitizeHeadline(TryExtractLatestMessage(body), articleTitle);
                if (!string.IsNullOrWhiteSpace(immediateHeadline))
                    return immediateHeadline;

                var fromHistory = await WaitForFreshHeadlineFromHistoryAsync(apiBaseUrl, p2Key, npcId, previousHistoryMessage, ct);
                var historyHeadline = SanitizeHeadline(fromHistory, articleTitle);
                if (!string.IsNullOrWhiteSpace(historyHeadline))
                    return historyHeadline;
            }
            catch (HttpRequestException ex) when (attempt == 0 && IsNpcMissing(ex))
            {
                _headlineEditorNpcId = null;
                continue;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Player2Client] GenerateSensationalHeadlineAsync EXCEPTION: {ex.GetType().Name}: {ex.Message}");
                break;
            }
        }

        return null;
    }

    private async Task<string> EnsureHeadlineEditorNpcIdAsync(string apiBaseUrl, string p2Key, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(_headlineEditorNpcId))
            return _headlineEditorNpcId;

        await _headlineEditorNpcLock.WaitAsync(ct);
        try
        {
            if (!string.IsNullOrWhiteSpace(_headlineEditorNpcId))
                return _headlineEditorNpcId;

            var req = new SpawnNpcRequest
            {
                ShortName = "Editor",
                Name = "Pelican Times Editor",
                CharacterDescription = "Town newspaper editor writing short, dramatic headlines.",
                SystemPrompt = "Write one sensational newspaper headline per request. Reply with headline text only. Max 30 characters.",
                KeepGameState = true,
                Commands = new List<SpawnNpcCommand>()
            };

            _headlineEditorNpcId = await SpawnNpcAsync(apiBaseUrl, p2Key, req, ct);
            return _headlineEditorNpcId;
        }
        finally
        {
            _headlineEditorNpcLock.Release();
        }
    }

    private async Task<string?> WaitForFreshHeadlineFromHistoryAsync(string apiBaseUrl, string p2Key, string npcId, string? previousHistoryMessage, CancellationToken ct)
    {
        const int maxAttempts = 20;
        for (var i = 0; i < maxAttempts; i++)
        {
            ct.ThrowIfCancellationRequested();

            var latest = await TryGetLatestNpcMessageFromHistoryAsync(apiBaseUrl, p2Key, npcId, ct);
            if (!string.IsNullOrWhiteSpace(latest)
                && !string.Equals(latest, previousHistoryMessage, StringComparison.Ordinal))
            {
                return latest;
            }

            if (i < maxAttempts - 1)
                await Task.Delay(300, ct);
        }

        return null;
    }

    private async Task<string?> TryGetLatestNpcMessageFromHistoryAsync(string apiBaseUrl, string p2Key, string npcId, CancellationToken ct)
    {
        try
        {
            var url = $"{apiBaseUrl.TrimEnd('/')}/npcs/{Uri.EscapeDataString(npcId)}/history";
            using var msg = new HttpRequestMessage(HttpMethod.Get, url);
            msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", p2Key);

            using var res = await _http.SendAsync(msg, ct);
            if (!res.IsSuccessStatusCode)
                return null;

            var body = await res.Content.ReadAsStringAsync(ct);
            return TryExtractLatestMessage(body);
        }
        catch
        {
            return null;
        }
    }

    private static string BuildHeadlinePrompt(string articleTitle, string articleCategory, string articleContent)
    {
        return $"You are a tabloid newspaper editor. Convert this article into a sensational 30-char headline.\n\nTitle: {articleTitle}\nCategory: {articleCategory}\nContent: {articleContent}\n\nRespond with ONLY the headline, max 30 characters. Make it exciting and exaggerated like a small-town tabloid.";
    }

    private static bool IsNpcMissing(HttpRequestException ex)
    {
        return ex.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone;
    }

    private static string? SanitizeHeadline(string? rawHeadline, string articleTitle)
    {
        if (string.IsNullOrWhiteSpace(rawHeadline))
            return null;

        var headline = rawHeadline
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Trim()
            .Trim('"')
            .Trim('\'')
            .Trim();

        if (headline.StartsWith("headline:", StringComparison.OrdinalIgnoreCase))
            headline = headline["headline:".Length..].Trim();

        // Common Player2 format: "<Editor> Headline text"
        if (headline.StartsWith("<", StringComparison.Ordinal))
        {
            var closing = headline.IndexOf('>');
            if (closing > 0 && closing + 1 < headline.Length)
                headline = headline[(closing + 1)..].Trim();
        }

        if (LooksLikePromptEcho(headline, articleTitle))
            return null;

        // Guard against metadata tokens being mistaken as headlines.
        if (!headline.Any(char.IsLetter))
            return null;

        if (headline.Length > 30)
            headline = headline.Substring(0, 27) + "...";

        return string.IsNullOrWhiteSpace(headline) ? null : headline;
    }

    private static bool LooksLikePromptEcho(string text, string articleTitle)
    {
        if (text.Contains("Convert this article into", StringComparison.OrdinalIgnoreCase)
            || text.Contains("Respond with ONLY the headline", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (text.Contains("Title:", StringComparison.OrdinalIgnoreCase)
            && text.Contains(articleTitle, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static string? TryExtractLatestMessage(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return null;

        var trimmed = payload.Trim();
        var looksLikeJsonObjectOrArray = trimmed.StartsWith("{", StringComparison.Ordinal) || trimmed.StartsWith("[", StringComparison.Ordinal);

        try
        {
            using var doc = JsonDocument.Parse(trimmed);
            var candidates = new List<string>();
            CollectMessageCandidates(doc.RootElement, candidates);

            for (var i = candidates.Count - 1; i >= 0; i--)
            {
                var candidate = candidates[i]?.Trim();
                if (!string.IsNullOrWhiteSpace(candidate))
                    return candidate;
            }
        }
        catch
        {
            // Fall back to plain text handling.
        }

        return looksLikeJsonObjectOrArray ? null : trimmed.Trim('"').Trim('\'');
    }

    private static void CollectMessageCandidates(JsonElement element, List<string> candidates)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
            {
                var value = element.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                    candidates.Add(value);
                break;
            }
            case JsonValueKind.Object:
            {
                foreach (var prop in element.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.String && IsMessageCandidateProperty(prop.Name))
                    {
                        var value = prop.Value.GetString();
                        if (!string.IsNullOrWhiteSpace(value))
                            candidates.Add(value);
                    }

                    if (prop.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                        CollectMessageCandidates(prop.Value, candidates);
                }
                break;
            }
            case JsonValueKind.Array:
            {
                foreach (var item in element.EnumerateArray())
                    CollectMessageCandidates(item, candidates);
                break;
            }
        }
    }

    private static bool IsMessageCandidateProperty(string propertyName)
    {
        var normalized = propertyName
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();

        return normalized is "message"
            or "headline"
            or "content"
            or "text"
            or "response"
            or "sendermessage"
            or "assistantmessage"
            or "npcmessage"
            or "outputtext";
    }

    private static string FallbackHeadline(string title)
    {
        var prefixes = new[] { "BREAKING:", "SHOCKING:", "URGENT:", "ALERT:" };
        var hash = Math.Abs(title.GetHashCode());
        var prefix = prefixes[hash % prefixes.Length];
        var truncated = title.Length > 22 ? title.Substring(0, 22) : title;
        return $"{prefix} {truncated}!";
    }

    public async Task<List<NewspaperArticle>> GenerateArticlesAsync(GenerateArticlesRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_apiBaseUrl) || string.IsNullOrWhiteSpace(_p2Key))
            return new List<NewspaperArticle>();

        return await GenerateArticlesAsync(_apiBaseUrl, _p2Key, request, ct);
    }

    public async Task<List<NewspaperArticle>> GenerateArticlesAsync(string apiBaseUrl, string p2Key, GenerateArticlesRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(apiBaseUrl) || string.IsNullOrWhiteSpace(p2Key))
            return new List<NewspaperArticle>();

        var requestedCount = Math.Clamp(request.Context?.Count ?? 1, 1, 2);
        for (var attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                var npcId = await EnsureHeadlineEditorNpcIdAsync(apiBaseUrl, p2Key, ct);
                var previousHistoryMessage = await TryGetLatestNpcMessageFromHistoryAsync(apiBaseUrl, p2Key, npcId, ct);

                var prompt = BuildArticleGenerationPrompt(request, requestedCount);
                var url = $"{apiBaseUrl.TrimEnd('/')}/npcs/{Uri.EscapeDataString(npcId)}/chat";
                using var msg = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(JsonSerializer.Serialize(new NpcChatRequest
                    {
                        SenderName = "Pelican Times Desk",
                        SenderMessage = prompt,
                        GameStateInfo = BuildArticleGenerationStateInfo(request.Context)
                    }, _jsonOptions), Encoding.UTF8, "application/json")
                };
                msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", p2Key);

                using var res = await _http.SendAsync(msg, ct);
                var body = await res.Content.ReadAsStringAsync(ct);
                res.EnsureSuccessStatusCode();

                // Some Player2 deployments send immediate message payloads.
                var immediate = ParseGeneratedArticlesPayload(body, requestedCount);
                if (immediate.Count > 0)
                    return immediate;

                var immediateMessage = TryExtractLatestMessage(body);
                var immediateFromMessage = ParseGeneratedArticlesPayload(immediateMessage, requestedCount);
                if (immediateFromMessage.Count > 0)
                    return immediateFromMessage;

                // Fallback to history polling (same pattern used for headline generation).
                var fromHistory = await WaitForFreshHeadlineFromHistoryAsync(apiBaseUrl, p2Key, npcId, previousHistoryMessage, ct);
                var historyArticles = ParseGeneratedArticlesPayload(fromHistory, requestedCount);
                if (historyArticles.Count > 0)
                    return historyArticles;
            }
            catch (HttpRequestException ex) when (attempt == 0 && IsNpcMissing(ex))
            {
                _headlineEditorNpcId = null;
                continue;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Player2Client] GenerateArticlesAsync EXCEPTION: {ex.GetType().Name}: {ex.Message}");
                break;
            }
        }

        return new List<NewspaperArticle>();
    }

    private static string BuildArticleGenerationPrompt(GenerateArticlesRequest request, int count)
    {
        var season = request.Context?.Season ?? "spring";
        var day = request.Context?.Day ?? 1;
        var year = request.Context?.Year ?? 1;
        var existing = request.Context?.ExistingArticles ?? new List<string>();
        var existingList = existing.Count == 0 ? "(none)" : string.Join(", ", existing.Take(20));

        return
            $"You are the Pelican Times editor in Stardew Valley. " +
            $"Generate {count} fresh short newspaper stories for season {season}, day {day}, year {year}. " +
            $"Avoid repeating these recent titles: {existingList}. " +
            "Stories should feel like town progression for this point in the year. " +
            "Each story must fit within 80 characters total (title + content). " +
            "Return STRICT JSON only with this schema: " +
            "{\"articles\":[{\"title\":\"...\",\"content\":\"...\",\"category\":\"community|market|social|nature\"}]}. " +
            "No markdown, no prose outside JSON.";
    }

    private static string BuildArticleGenerationStateInfo(ArticleGenerationContext? context)
    {
        if (context is null)
            return "article_generation";

        return $"article_generation: season={context.Season}, day={context.Day}, year={context.Year}";
    }

    private List<NewspaperArticle> ParseGeneratedArticlesPayload(string? payload, int maxCount)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return new List<NewspaperArticle>();

        var candidates = BuildJsonCandidates(payload);
        foreach (var candidate in candidates)
        {
            if (!TryParseGeneratedArticlesJson(candidate, maxCount, out var articles))
                continue;

            if (articles.Count > 0)
                return articles;
        }

        return new List<NewspaperArticle>();
    }

    private static List<string> BuildJsonCandidates(string payload)
    {
        var raw = payload.Trim();
        var candidates = new List<string> { raw };

        if (raw.StartsWith("<", StringComparison.Ordinal))
        {
            var close = raw.IndexOf('>');
            if (close > 0 && close + 1 < raw.Length)
                candidates.Add(raw[(close + 1)..].Trim());
        }

        // Handle fenced blocks: ```json ... ```
        var fenceStart = raw.IndexOf("```", StringComparison.Ordinal);
        if (fenceStart >= 0)
        {
            var contentStart = raw.IndexOf('\n', fenceStart);
            if (contentStart > fenceStart)
            {
                var fenceEnd = raw.IndexOf("```", contentStart, StringComparison.Ordinal);
                if (fenceEnd > contentStart)
                    candidates.Add(raw[(contentStart + 1)..fenceEnd].Trim());
            }
        }

        // Handle mixed text with embedded JSON object/array.
        var objStart = raw.IndexOf('{');
        var objEnd = raw.LastIndexOf('}');
        if (objStart >= 0 && objEnd > objStart)
            candidates.Add(raw[objStart..(objEnd + 1)]);

        var arrStart = raw.IndexOf('[');
        var arrEnd = raw.LastIndexOf(']');
        if (arrStart >= 0 && arrEnd > arrStart)
            candidates.Add(raw[arrStart..(arrEnd + 1)]);

        return candidates
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private bool TryParseGeneratedArticlesJson(string json, int maxCount, out List<NewspaperArticle> articles)
    {
        articles = new List<NewspaperArticle>();
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            JsonElement articleArray;
            if (root.ValueKind == JsonValueKind.Object
                && TryGetPropertyCaseInsensitive(root, "articles", out var articlesProp)
                && articlesProp.ValueKind == JsonValueKind.Array)
            {
                articleArray = articlesProp;
            }
            else if (root.ValueKind == JsonValueKind.Array)
            {
                articleArray = root;
            }
            else
            {
                return false;
            }

            var seenTitles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in articleArray.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                    continue;

                if (!TryGetStringProperty(item, "title", out var title) || string.IsNullOrWhiteSpace(title))
                    continue;

                if (!TryGetStringProperty(item, "content", out var content) || string.IsNullOrWhiteSpace(content))
                    continue;

                if (!TryClampGeneratedArticle(title, content, out var clampedTitle, out var clampedContent))
                    continue;

                if (!seenTitles.Add(clampedTitle))
                    continue;

                var category = TryGetStringProperty(item, "category", out var categoryValue)
                    ? NormalizeArticleCategory(categoryValue)
                    : "community";

                articles.Add(new NewspaperArticle
                {
                    Title = clampedTitle,
                    Content = clampedContent,
                    Category = category
                });

                if (articles.Count >= maxCount)
                    break;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryClampGeneratedArticle(string title, string content, out string clampedTitle, out string clampedContent)
    {
        clampedTitle = (title ?? string.Empty).Trim();
        clampedContent = (content ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(clampedTitle) || string.IsNullOrWhiteSpace(clampedContent))
            return false;

        // Reserve at least one character for content while enforcing title+content <= 80.
        var maxTitleLength = Math.Max(1, MaxGeneratedArticleCharacters - 1);
        if (clampedTitle.Length > maxTitleLength)
            clampedTitle = clampedTitle[..maxTitleLength].Trim();

        if (string.IsNullOrWhiteSpace(clampedTitle))
            return false;

        var maxContentLength = MaxGeneratedArticleCharacters - clampedTitle.Length;
        if (maxContentLength <= 0)
            return false;

        if (clampedContent.Length > maxContentLength)
            clampedContent = clampedContent[..maxContentLength].Trim();

        return !string.IsNullOrWhiteSpace(clampedContent);
    }

    private static bool TryGetStringProperty(JsonElement obj, string propertyName, out string value)
    {
        value = string.Empty;
        if (!TryGetPropertyCaseInsensitive(obj, propertyName, out var prop))
            return false;

        if (prop.ValueKind != JsonValueKind.String)
            return false;

        value = prop.GetString() ?? string.Empty;
        return true;
    }

    private static bool TryGetPropertyCaseInsensitive(JsonElement obj, string propertyName, out JsonElement value)
    {
        foreach (var prop in obj.EnumerateObject())
        {
            if (prop.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = prop.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static string NormalizeArticleCategory(string? category)
    {
        if (string.Equals(category, "market", StringComparison.OrdinalIgnoreCase))
            return "market";
        if (string.Equals(category, "social", StringComparison.OrdinalIgnoreCase))
            return "social";
        if (string.Equals(category, "nature", StringComparison.OrdinalIgnoreCase))
            return "nature";
        return "community";
    }

    /// <summary>
    /// Reads one JSON line from /npcs/responses stream (Accept: application/json NDJSON).
    /// </summary>
    public async Task<string?> ReadOneNpcResponseLineAsync(string apiBaseUrl, string p2Key, CancellationToken ct)
    {
        var url = $"{apiBaseUrl.TrimEnd('/')}/npcs/responses";
        using var msg = new HttpRequestMessage(HttpMethod.Get, url);
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", p2Key);
        msg.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var res = await _http.SendAsync(msg, HttpCompletionOption.ResponseHeadersRead, ct);
        res.EnsureSuccessStatusCode();

        await using var stream = await res.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        // Read first non-empty line
        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync().WaitAsync(ct);
            if (!string.IsNullOrWhiteSpace(line))
                return line;
        }

        return null;
    }

    /// <summary>
    /// Long-lived NDJSON stream reader for /npcs/responses.
    /// Calls onLine for each non-empty line until cancelled or stream closes.
    /// </summary>
    public async Task StreamNpcResponsesAsync(string apiBaseUrl, string p2Key, Func<string, Task> onLine, CancellationToken ct)
    {
        var url = $"{apiBaseUrl.TrimEnd('/')}/npcs/responses";
        using var msg = new HttpRequestMessage(HttpMethod.Get, url);
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", p2Key);
        msg.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var res = await _http.SendAsync(msg, HttpCompletionOption.ResponseHeadersRead, ct);
        res.EnsureSuccessStatusCode();

        await using var stream = await res.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            string? line;
            try
            {
                line = await reader.ReadLineAsync().WaitAsync(ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(line))
                continue;

            await onLine(line);
        }
    }

    private sealed class LoginWebResponse
    {
        public string P2Key { get; set; } = "";
    }

    private sealed class DeviceAuthTokenSuccessResponse
    {
        [JsonPropertyName("p2key")]
        public string P2Key { get; set; } = "";
    }

    private sealed class DeviceAuthTokenErrorResponse
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }
}

public sealed class DeviceAuthStartResponse
{
    [JsonPropertyName("device_code")]
    public string DeviceCode { get; set; } = string.Empty;

    [JsonPropertyName("user_code")]
    public string UserCode { get; set; } = string.Empty;

    [JsonPropertyName("verification_uri")]
    public string VerificationUri { get; set; } = string.Empty;

    [JsonPropertyName("verification_url")]
    public string VerificationUrl { get; set; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; } = 120;

    [JsonPropertyName("interval")]
    public int IntervalSeconds { get; set; } = 5;

    public string GetVerificationUrlOrFallback()
    {
        if (!string.IsNullOrWhiteSpace(VerificationUri))
            return VerificationUri;

        if (!string.IsNullOrWhiteSpace(VerificationUrl))
            return VerificationUrl;

        return "https://player2.game/login/device";
    }
}

public sealed class DeviceAuthTokenPollResult
{
    public string Status { get; set; } = "pending";
    public string? P2Key { get; set; }
    public string? ErrorMessage { get; set; }

    public bool IsAuthorized => string.Equals(Status, "authorized", StringComparison.OrdinalIgnoreCase)
                              || !string.IsNullOrWhiteSpace(P2Key);

    public bool IsTerminalFailure => string.Equals(Status, "expired", StringComparison.OrdinalIgnoreCase)
                                  || string.Equals(Status, "denied", StringComparison.OrdinalIgnoreCase)
                                  || string.Equals(Status, "invalid", StringComparison.OrdinalIgnoreCase)
                                  || string.Equals(Status, "error", StringComparison.OrdinalIgnoreCase);
}

public sealed class SpawnNpcRequest
{
    [JsonPropertyName("short_name")]
    public string ShortName { get; set; } = "Lewis";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "Mayor Lewis";

    [JsonPropertyName("character_description")]
    public string CharacterDescription { get; set; } = "A pragmatic mayor focused on town stability and cooperation.";

    [JsonPropertyName("system_prompt")]
    public string SystemPrompt { get; set; } = "You are Mayor Lewis. Prioritize practical, cozy town-stabilizing advice and actions.";

    [JsonPropertyName("tts")]
    public SpawnNpcTts Tts { get; set; } = new();

    [JsonPropertyName("keep_game_state")]
    public bool KeepGameState { get; set; } = true;

    [JsonPropertyName("commands")]
    public List<SpawnNpcCommand> Commands { get; set; } = new();
}

public sealed class SpawnNpcTts
{
    [JsonPropertyName("speed")]
    public float Speed { get; set; } = 1.0f;

    [JsonPropertyName("audio_format")]
    public string AudioFormat { get; set; } = "mp3";
}

public sealed class SpawnNpcCommand
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "propose_quest";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "Propose a town rumor quest from safe templates.";

    [JsonPropertyName("parameters")]
    public object Parameters { get; set; } = new
    {
        type = "object",
        properties = new
        {
            template_id = new { type = "string" },
            target = new { type = "string" },
            urgency = new { type = "string" }
        },
        required = new[] { "template_id", "target", "urgency" }
    };

    [JsonPropertyName("never_respond_with_message")]
    public bool NeverRespondWithMessage { get; set; } = false;
}

public sealed class NpcChatRequest
{
    [JsonPropertyName("sender_name")]
    public string SenderName { get; set; } = "Player";

    [JsonPropertyName("sender_message")]
    public string SenderMessage { get; set; } = "Hello";

    [JsonPropertyName("game_state_info")]
    public string? GameStateInfo { get; set; }
}

public sealed class JoulesResponse
{
    [JsonPropertyName("joules")]
    public int Joules { get; set; }

    [JsonPropertyName("patron_tier")]
    public string PatronTier { get; set; } = string.Empty;

    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;
}

public sealed class GenerateArticlesRequest
{
    [JsonPropertyName("short_name")]
    public string ShortName { get; set; } = "Editor";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "Newspaper Editor";

    [JsonPropertyName("character_description")]
    public string CharacterDescription { get; set; } = "You are the newspaper editor for the Pelican Times. Generate seasonal filler articles based on game progress.";

    [JsonPropertyName("system_prompt")]
    public string SystemPrompt { get; set; } = "You are the newspaper editor. Generate 1-2 short newspaper articles (title + 2-3 sentences) based on the current game state.";

    [JsonPropertyName("commands")]
    public List<SpawnNpcCommand> Commands { get; set; } = new();

    [JsonPropertyName("keep_game_state")]
    public bool KeepGameState { get; set; } = false;

    [JsonPropertyName("context")]
    public ArticleGenerationContext? Context { get; set; }
}

public sealed class ArticleGenerationContext
{
    [JsonPropertyName("season")]
    public string Season { get; set; } = "spring";

    [JsonPropertyName("day")]
    public int Day { get; set; }

    [JsonPropertyName("year")]
    public int Year { get; set; } = 1;

    [JsonPropertyName("existing_articles")]
    public List<string> ExistingArticles { get; set; } = new();

    [JsonPropertyName("count")]
    public int Count { get; set; } = 1;
}

public sealed class GenerateArticlesResponse
{
    [JsonPropertyName("articles")]
    public List<NewspaperArticle> Articles { get; set; } = new();
}

public sealed class GeneratedArticle
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = "community";
}

