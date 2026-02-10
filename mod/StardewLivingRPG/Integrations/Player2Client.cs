using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

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
