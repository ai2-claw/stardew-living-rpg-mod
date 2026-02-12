using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using StardewLivingRPG.State;

namespace StardewLivingRPG.Integrations;

public sealed class Player2Client
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public Player2Client(HttpClient? httpClient = null)
    {
        _http = httpClient ?? new HttpClient();
        _http.Timeout = TimeSpan.FromSeconds(20);
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

    public async Task<List<NewspaperArticle>> GenerateArticlesAsync(string apiBaseUrl, string p2Key, GenerateArticlesRequest request, CancellationToken ct)
    {
        var url = $"{apiBaseUrl.TrimEnd('/')}/npcs/spawn";
        using var msg = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(request, _jsonOptions), Encoding.UTF8, "application/json")
        };
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", p2Key);

        using var res = await _http.SendAsync(msg, ct);
        var body = await res.Content.ReadAsStringAsync(ct);
        res.EnsureSuccessStatusCode();

        var data = JsonSerializer.Deserialize<GenerateArticlesResponse>(body, _jsonOptions);
        return data?.Articles ?? new List<NewspaperArticle>();
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

