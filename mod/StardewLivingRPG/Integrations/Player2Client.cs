using System.Net.Http.Headers;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using StardewLivingRPG.Config;
using StardewLivingRPG.State;
using StardewLivingRPG.Utils;
using StardewValley;

namespace StardewLivingRPG.Integrations;

public sealed class Player2Client
{
    private const int MaxMarketOutlookLineCharacters = 120;
    private const int MaxRewrittenEventTitleCharacters = 80;
    private const int MaxRewrittenEventContentCharacters = 420;
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

    private static TownProfile ResolveActiveTownProfile()
    {
        return TownProfileResolver.ResolveForLocation(Game1.currentLocation?.Name);
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
        var payloadAttempts = new[]
        {
            new Dictionary<string, string> { ["game_client_id"] = gameClientId },
            new Dictionary<string, string> { ["client_id"] = gameClientId }
        };

        DeviceAuthStartAttemptResult? lastFailure = null;
        for (var i = 0; i < payloadAttempts.Length; i++)
        {
            var attempt = await TryStartDeviceAuthAsync(url, payloadAttempts[i], ct);
            if (attempt.IsSuccessStatusCode)
            {
                var data = ParseDeviceAuthStartResponse(attempt.Body);
                if (string.IsNullOrWhiteSpace(data.DeviceCode))
                    throw new InvalidOperationException($"Player2 device auth start missing device_code. Response: {TrimForLog(attempt.Body)}");

                return data;
            }

            lastFailure = attempt;
            var canRetryWithAlternatePayload = i == 0
                && (attempt.StatusCode == HttpStatusCode.BadRequest || attempt.StatusCode == HttpStatusCode.UnprocessableEntity);
            if (!canRetryWithAlternatePayload)
                break;
        }

        if (lastFailure is null)
            throw new HttpRequestException("Player2 device auth start failed.", null, HttpStatusCode.BadRequest);

        var error = BuildDeviceAuthStartErrorMessage(lastFailure);
        throw new HttpRequestException(error, null, lastFailure.StatusCode);
    }

    public async Task<DeviceAuthTokenPollResult> PollDeviceAuthTokenAsync(
        string authBaseUrl,
        string deviceCode,
        CancellationToken ct,
        string? gameClientId = null,
        string? userCode = null)
    {
        var url = $"{authBaseUrl.TrimEnd('/')}/login/device/token";
        var payloadAttempts = BuildDeviceAuthTokenPayloadAttempts(deviceCode, gameClientId, userCode);
        if (payloadAttempts.Count == 0)
        {
            payloadAttempts.Add(new Dictionary<string, string>
            {
                ["device_code"] = deviceCode
            });
        }

        DeviceAuthTokenPollResult? bestPending = null;
        for (var i = 0; i < payloadAttempts.Count; i++)
        {
            var payload = payloadAttempts[i];
            for (var asFormUrlEncoded = 0; asFormUrlEncoded < 2; asFormUrlEncoded++)
            {
                var attempt = await TryPollDeviceAuthTokenAsync(url, payload, ct, useFormUrlEncoded: asFormUrlEncoded == 1);
                var parsed = ParseDeviceAuthTokenPollResult(attempt);

                if (parsed.IsAuthorized)
                    return parsed;

                if (parsed.IsTerminalFailure)
                    return parsed;

                if (bestPending is null || !string.IsNullOrWhiteSpace(parsed.ErrorMessage))
                    bestPending = parsed;
            }
        }

        return bestPending ?? new DeviceAuthTokenPollResult
        {
            Status = "pending"
        };
    }

    private static List<Dictionary<string, string>> BuildDeviceAuthTokenPayloadAttempts(string deviceCode, string? gameClientId, string? userCode)
    {
        var attempts = new List<Dictionary<string, string>>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddAttempt(params (string Key, string? Value)[] entries)
        {
            var payload = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var (key, value) in entries)
            {
                if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
                    continue;

                payload[key] = value.Trim();
            }

            if (payload.Count == 0)
                return;

            var signature = string.Join("|", payload.OrderBy(p => p.Key, StringComparer.Ordinal)
                .Select(p => $"{p.Key}={p.Value}"));
            if (seen.Add(signature))
                attempts.Add(payload);
        }

        AddAttempt(("device_code", deviceCode));
        AddAttempt(("deviceCode", deviceCode));
        AddAttempt(("device_code", deviceCode), ("client_id", gameClientId));
        AddAttempt(("device_code", deviceCode), ("game_client_id", gameClientId));
        AddAttempt(("deviceCode", deviceCode), ("clientId", gameClientId));
        AddAttempt(("deviceCode", deviceCode), ("gameClientId", gameClientId));
        AddAttempt(("grant_type", "urn:ietf:params:oauth:grant-type:device_code"), ("device_code", deviceCode), ("client_id", gameClientId));
        AddAttempt(("grant_type", "urn:ietf:params:oauth:grant-type:device_code"), ("device_code", deviceCode), ("game_client_id", gameClientId));
        AddAttempt(("grantType", "urn:ietf:params:oauth:grant-type:device_code"), ("deviceCode", deviceCode), ("clientId", gameClientId));
        AddAttempt(("device_code", deviceCode), ("user_code", userCode), ("client_id", gameClientId));
        AddAttempt(("deviceCode", deviceCode), ("userCode", userCode), ("clientId", gameClientId));

        return attempts;
    }

    private async Task<DeviceAuthTokenPollAttemptResult> TryPollDeviceAuthTokenAsync(string url, Dictionary<string, string> payload, CancellationToken ct, bool useFormUrlEncoded)
    {
        using var msg = new HttpRequestMessage(HttpMethod.Post, url);
        if (useFormUrlEncoded)
        {
            msg.Content = new FormUrlEncodedContent(payload);
        }
        else
        {
            msg.Content = new StringContent(JsonSerializer.Serialize(payload, _jsonOptions), Encoding.UTF8, "application/json");
        }

        using var res = await _http.SendAsync(msg, ct);
        var body = await res.Content.ReadAsStringAsync(ct);
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in res.Headers)
        {
            var value = header.Value.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(value))
                headers[header.Key] = value;
        }

        foreach (var header in res.Content.Headers)
        {
            var value = header.Value.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(value))
                headers[header.Key] = value;
        }

        return new DeviceAuthTokenPollAttemptResult
        {
            IsSuccessStatusCode = res.IsSuccessStatusCode,
            StatusCode = res.StatusCode,
            Body = body,
            Headers = headers
        };
    }

    private DeviceAuthTokenPollResult ParseDeviceAuthTokenPollResult(DeviceAuthTokenPollAttemptResult attempt)
    {
        var token = TryExtractDeviceAuthTokenKey(attempt.Body);
        if (string.IsNullOrWhiteSpace(token))
            token = TryExtractDeviceAuthTokenFromHeaders(attempt.Headers);
        if (!string.IsNullOrWhiteSpace(token))
        {
            return new DeviceAuthTokenPollResult
            {
                Status = "authorized",
                P2Key = token
            };
        }

        var status = ParseDeviceAuthStatus(attempt.Body, attempt.StatusCode);
        var message = ParseDeviceAuthMessage(attempt.Body);

        if (string.Equals(status, "authorized", StringComparison.OrdinalIgnoreCase))
        {
            return new DeviceAuthTokenPollResult
            {
                Status = "authorized",
                P2Key = token,
                ErrorMessage = message
            };
        }

        if (IsTerminalStatus(status))
        {
            return new DeviceAuthTokenPollResult
            {
                Status = status,
                ErrorMessage = string.IsNullOrWhiteSpace(message)
                    ? $"HTTP {(int)attempt.StatusCode} ({attempt.StatusCode})"
                    : message
            };
        }

        // Many OAuth-style device flows return HTTP 400 while still pending authorization.
        // Keep waiting unless status is explicitly terminal.
        if ((int)attempt.StatusCode >= 400 && IsPendingStatus(status))
        {
            return new DeviceAuthTokenPollResult
            {
                Status = "pending",
                ErrorMessage = message
            };
        }

        return new DeviceAuthTokenPollResult
        {
            Status = status,
            ErrorMessage = message
        };
    }

    private static string TryExtractDeviceAuthTokenFromHeaders(IReadOnlyDictionary<string, string> headers)
    {
        if (headers is null || headers.Count == 0)
            return string.Empty;

        var keys = new[]
        {
            "x-p2key",
            "x-p2-key",
            "x-player2-key",
            "p2key",
            "authorization"
        };

        for (var i = 0; i < keys.Length; i++)
        {
            if (!headers.TryGetValue(keys[i], out var value) || string.IsNullOrWhiteSpace(value))
                continue;

            var trimmed = value.Trim();
            if (trimmed.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                trimmed = trimmed[7..].Trim();

            if (!string.IsNullOrWhiteSpace(trimmed))
                return trimmed;
        }

        return string.Empty;
    }

    private static string ParseDeviceAuthStatus(string body, HttpStatusCode statusCode)
    {
        var fallback = "pending";
        if (string.IsNullOrWhiteSpace(body))
            return fallback;

        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return fallback;

            var rawStatus = TryGetStringByAliases(root, "status", "state", "result", "error");
            if (string.IsNullOrWhiteSpace(rawStatus))
                rawStatus = TryExtractDeviceAuthStatusFromElement(root);
            if (string.IsNullOrWhiteSpace(rawStatus))
            {
                if (TryGetPropertyCaseInsensitive(root, "authorized", out var authorizedEl)
                    && authorizedEl.ValueKind is JsonValueKind.True or JsonValueKind.False)
                {
                    rawStatus = authorizedEl.GetBoolean() ? "authorized" : fallback;
                }
            }

            if (string.IsNullOrWhiteSpace(rawStatus))
                return fallback;

            var normalized = rawStatus.Trim().ToLowerInvariant();
            return normalized switch
            {
                "approved" => "authorized",
                "authorized" => "authorized",
                "success" => "authorized",
                "ok" => "authorized",
                "authorization_pending" => "pending",
                "authorizationpending" => "pending",
                "pending" => "pending",
                "waiting" => "pending",
                "slow_down" => "pending",
                "slowdown" => "pending",
                "expired_token" => "expired",
                "access_denied" => "denied",
                "accessdenied" => "denied",
                "invalid_client" => "invalid",
                "invalidclient" => "invalid",
                "unauthorized_client" => "invalid",
                "unauthorizedclient" => "invalid",
                "invalid_request" => "pending",
                "invalidrequest" => "pending",
                "bad_request" => "pending",
                "badrequest" => "pending",
                _ => normalized
            };
        }
        catch
        {
            return fallback;
        }
    }

    private static string TryExtractDeviceAuthStatusFromElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
            {
                var direct = TryGetStringByAliases(element, "status", "state", "result", "error");
                if (!string.IsNullOrWhiteSpace(direct))
                    return direct;

                foreach (var prop in element.EnumerateObject())
                {
                    var nested = TryExtractDeviceAuthStatusFromElement(prop.Value);
                    if (!string.IsNullOrWhiteSpace(nested))
                        return nested;
                }

                return string.Empty;
            }
            case JsonValueKind.Array:
            {
                foreach (var item in element.EnumerateArray())
                {
                    var nested = TryExtractDeviceAuthStatusFromElement(item);
                    if (!string.IsNullOrWhiteSpace(nested))
                        return nested;
                }

                return string.Empty;
            }
            default:
                return string.Empty;
        }
    }

    private static string? ParseDeviceAuthMessage(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return TrimForLog(body);

            var msg = TryGetStringByAliases(root, "message", "detail", "error_description", "description");
            return string.IsNullOrWhiteSpace(msg) ? null : msg;
        }
        catch
        {
            return TrimForLog(body);
        }
    }

    private static bool IsPendingStatus(string status)
    {
        var normalized = (status ?? string.Empty).Trim().ToLowerInvariant();
        return normalized is "pending" or "authorization_pending" or "waiting" or "slow_down";
    }

    private static bool IsTerminalStatus(string status)
    {
        var normalized = (status ?? string.Empty).Trim().ToLowerInvariant();
        return normalized is "expired"
            or "denied"
            or "invalid"
            or "error"
            or "access_denied"
            or "accessdenied"
            or "expired_token"
            or "expiredtoken"
            or "invalid_client"
            or "invalidclient"
            or "unauthorized_client"
            or "unauthorizedclient";
    }

    private static string TryExtractDeviceAuthTokenKey(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return string.Empty;

        try
        {
            using var doc = JsonDocument.Parse(body);
            return TryExtractDeviceAuthTokenFromElement(doc.RootElement);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string TryExtractDeviceAuthTokenFromElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
            {
                var direct = TryGetStringByAliases(
                    element,
                    "p2key",
                    "p2Key",
                    "p2_key",
                    "access_token",
                    "accessToken",
                    "auth_token",
                    "authToken",
                    "token");
                if (!string.IsNullOrWhiteSpace(direct))
                    return direct;

                foreach (var prop in element.EnumerateObject())
                {
                    var nested = TryExtractDeviceAuthTokenFromElement(prop.Value);
                    if (!string.IsNullOrWhiteSpace(nested))
                        return nested;
                }

                return string.Empty;
            }
            case JsonValueKind.Array:
            {
                foreach (var item in element.EnumerateArray())
                {
                    var nested = TryExtractDeviceAuthTokenFromElement(item);
                    if (!string.IsNullOrWhiteSpace(nested))
                        return nested;
                }

                return string.Empty;
            }
            default:
                return string.Empty;
        }
    }

    private async Task<DeviceAuthStartAttemptResult> TryStartDeviceAuthAsync(string url, Dictionary<string, string> payload, CancellationToken ct)
    {
        using var msg = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, _jsonOptions), Encoding.UTF8, "application/json")
        };

        using var res = await _http.SendAsync(msg, ct);
        var body = await res.Content.ReadAsStringAsync(ct);
        return new DeviceAuthStartAttemptResult
        {
            IsSuccessStatusCode = res.IsSuccessStatusCode,
            StatusCode = res.StatusCode,
            Body = body
        };
    }

    private static string BuildDeviceAuthStartErrorMessage(DeviceAuthStartAttemptResult attempt)
    {
        var baseMessage = $"Player2 device auth start failed: {(int)attempt.StatusCode} ({attempt.StatusCode}).";
        var detail = TryExtractApiErrorMessage(attempt.Body);
        if (string.IsNullOrWhiteSpace(detail))
            return baseMessage;

        return $"{baseMessage} {detail}";
    }

    private static string TryExtractApiErrorMessage(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return string.Empty;

        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return TrimForLog(body);

            if (TryGetPropertyCaseInsensitive(root, "message", out var messageProp) && messageProp.ValueKind == JsonValueKind.String)
                return TrimForLog(messageProp.GetString());

            if (TryGetPropertyCaseInsensitive(root, "error_description", out var descriptionProp) && descriptionProp.ValueKind == JsonValueKind.String)
                return TrimForLog(descriptionProp.GetString());

            if (TryGetPropertyCaseInsensitive(root, "error", out var errorProp) && errorProp.ValueKind == JsonValueKind.String)
                return TrimForLog(errorProp.GetString());

            if (TryGetPropertyCaseInsensitive(root, "detail", out var detailProp) && detailProp.ValueKind == JsonValueKind.String)
                return TrimForLog(detailProp.GetString());
        }
        catch
        {
            // Fall through to plain-text logging.
        }

        return TrimForLog(body);
    }

    private static DeviceAuthStartResponse ParseDeviceAuthStartResponse(string body)
    {
        var data = JsonSerializer.Deserialize<DeviceAuthStartResponse>(body) ?? new DeviceAuthStartResponse();
        if (string.IsNullOrWhiteSpace(body))
            return data;

        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return data;

            data.DeviceCode = FirstNonEmpty(data.DeviceCode, TryGetStringByAliases(root, "device_code", "deviceCode"));
            data.UserCode = FirstNonEmpty(data.UserCode, TryGetStringByAliases(root, "user_code", "userCode"));
            data.VerificationUri = FirstNonEmpty(data.VerificationUri, TryGetStringByAliases(root, "verification_uri", "verificationUri"));
            data.VerificationUrl = FirstNonEmpty(
                data.VerificationUrl,
                TryGetStringByAliases(root, "verification_url", "verificationUrl", "verification_uri_complete", "verificationUriComplete"));
            data.ExpiresIn = FirstNonDefault(data.ExpiresIn, TryGetIntByAliases(root, "expires_in", "expiresIn"));
            data.IntervalSeconds = FirstNonDefault(data.IntervalSeconds, TryGetIntByAliases(root, "interval", "interval_seconds", "intervalSeconds"));
        }
        catch
        {
            // Keep already-deserialized values.
        }

        return data;
    }

    private static string FirstNonEmpty(string current, string candidate)
    {
        return string.IsNullOrWhiteSpace(current) && !string.IsNullOrWhiteSpace(candidate)
            ? candidate
            : current;
    }

    private static int FirstNonDefault(int current, int candidate)
    {
        return current > 0 ? current : candidate;
    }

    private static string TryGetStringByAliases(JsonElement root, params string[] aliases)
    {
        for (var i = 0; i < aliases.Length; i++)
        {
            if (TryGetPropertyCaseInsensitive(root, aliases[i], out var prop) && prop.ValueKind == JsonValueKind.String)
            {
                var value = prop.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                    return value.Trim();
            }
        }

        return string.Empty;
    }

    private static int TryGetIntByAliases(JsonElement root, params string[] aliases)
    {
        for (var i = 0; i < aliases.Length; i++)
        {
            if (!TryGetPropertyCaseInsensitive(root, aliases[i], out var prop))
                continue;

            if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var intVal))
                return intVal;

            if (prop.ValueKind == JsonValueKind.String && int.TryParse(prop.GetString(), out var parsed))
                return parsed;
        }

        return 0;
    }

    private static string TrimForLog(string? text)
    {
        var value = (text ?? string.Empty).Trim();
        if (value.Length <= 180)
            return value;

        return value[..177] + "...";
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

    public async Task<string?> SendNpcChatAsync(string apiBaseUrl, string p2Key, string npcId, NpcChatRequest req, CancellationToken ct)
    {
        var url = $"{apiBaseUrl.TrimEnd('/')}/npcs/{Uri.EscapeDataString(npcId)}/chat";
        using var msg = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(req, _jsonOptions), Encoding.UTF8, "application/json")
        };
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", p2Key);

        using var res = await _http.SendAsync(msg, ct);
        var body = await res.Content.ReadAsStringAsync(ct);
        res.EnsureSuccessStatusCode();
        return string.IsNullOrWhiteSpace(body) ? null : body;
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
                var townProfile = ResolveActiveTownProfile();
                var url = $"{apiBaseUrl.TrimEnd('/')}/npcs/{Uri.EscapeDataString(npcId)}/chat";
                using var msg = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(JsonSerializer.Serialize(new NpcChatRequest
                    {
                        SenderName = townProfile.NewspaperDeskName,
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

            var languageRule = I18n.BuildPromptLanguageInstruction();
            var townProfile = ResolveActiveTownProfile();
            var req = new SpawnNpcRequest
            {
                ShortName = "Editor",
                Name = townProfile.NewspaperEditorName,
                CharacterDescription = "Town newspaper editor writing short, dramatic headlines.",
                SystemPrompt = $"Write one sensational newspaper headline per request. {languageRule} Reply with headline text only. Max 30 characters.",
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

    public async Task<string?> TryGetLatestNpcHistoryMessageAsync(string apiBaseUrl, string p2Key, string npcId, CancellationToken ct)
    {
        var snapshot = await TryGetLatestNpcHistorySnapshotAsync(apiBaseUrl, p2Key, npcId, ct);
        return snapshot?.LatestMessage;
    }

    public async Task<NpcHistorySnapshot?> TryGetLatestNpcHistorySnapshotAsync(string apiBaseUrl, string p2Key, string npcId, CancellationToken ct)
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
            return new NpcHistorySnapshot
            {
                LatestMessage = TryExtractLatestMessage(body),
                SnapshotHash = ComputeSnapshotHash(body)
            };
        }
        catch
        {
            return null;
        }
    }

    private async Task<string?> TryGetLatestNpcMessageFromHistoryAsync(string apiBaseUrl, string p2Key, string npcId, CancellationToken ct)
    {
        var snapshot = await TryGetLatestNpcHistorySnapshotAsync(apiBaseUrl, p2Key, npcId, ct);
        return snapshot?.LatestMessage;
    }

    private static string ComputeSnapshotHash(string payload)
    {
        var text = payload ?? string.Empty;
        var bytes = Encoding.UTF8.GetBytes(text);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    private static string BuildHeadlinePrompt(string articleTitle, string articleCategory, string articleContent)
    {
        var languageRule = I18n.BuildPromptLanguageInstruction();
        return $"You are a tabloid newspaper editor. Convert this article into a sensational 30-char headline. {languageRule}\n\nTitle: {articleTitle}\nCategory: {articleCategory}\nContent: {articleContent}\n\nRespond with ONLY the headline, max 30 characters. Make it exciting and exaggerated like a small-town tabloid.";
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

        if (TryExtractStructuredHeadline(headline, out var structuredHeadline))
            headline = structuredHeadline;
        else if (LooksLikeJsonPayload(headline))
            return null;

        if (LooksLikePromptEcho(headline, articleTitle))
            return null;

        // Guard against metadata tokens being mistaken as headlines.
        if (!headline.Any(char.IsLetter))
            return null;

        if (headline.Length > 30)
            headline = headline.Substring(0, 27) + "...";

        return string.IsNullOrWhiteSpace(headline) ? null : headline;
    }

    private static bool TryExtractStructuredHeadline(string raw, out string headline)
    {
        headline = string.Empty;
        if (!LooksLikeJsonPayload(raw))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return false;

            if (TryGetPropertyCaseInsensitive(root, "headline", out var headlineProp)
                && headlineProp.ValueKind == JsonValueKind.String)
            {
                headline = (headlineProp.GetString() ?? string.Empty).Trim();
                return !string.IsNullOrWhiteSpace(headline);
            }

            if (!TryGetPropertyCaseInsensitive(root, "articles", out _)
                && TryGetPropertyCaseInsensitive(root, "title", out var titleProp)
                && titleProp.ValueKind == JsonValueKind.String)
            {
                headline = (titleProp.GetString() ?? string.Empty).Trim();
                return !string.IsNullOrWhiteSpace(headline);
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static bool LooksLikeJsonPayload(string text)
    {
        var trimmed = (text ?? string.Empty).Trim();
        return trimmed.StartsWith("{", StringComparison.Ordinal)
            || trimmed.StartsWith("[", StringComparison.Ordinal);
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
                var townProfile = ResolveActiveTownProfile();
                var url = $"{apiBaseUrl.TrimEnd('/')}/npcs/{Uri.EscapeDataString(npcId)}/chat";
                using var msg = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(JsonSerializer.Serialize(new NpcChatRequest
                    {
                        SenderName = townProfile.NewspaperDeskName,
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

    public async Task<List<string>> GenerateMarketOutlookHintsAsync(GenerateMarketOutlookRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_apiBaseUrl) || string.IsNullOrWhiteSpace(_p2Key))
            return new List<string>();

        return await GenerateMarketOutlookHintsAsync(_apiBaseUrl, _p2Key, request, ct);
    }

    public async Task<List<string>> GenerateMarketOutlookHintsAsync(string apiBaseUrl, string p2Key, GenerateMarketOutlookRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(apiBaseUrl) || string.IsNullOrWhiteSpace(p2Key))
            return new List<string>();

        var requestedCount = Math.Clamp(request.Context?.Count ?? 2, 1, 3);
        for (var attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                var npcId = await EnsureHeadlineEditorNpcIdAsync(apiBaseUrl, p2Key, ct);
                var previousHistoryMessage = await TryGetLatestNpcMessageFromHistoryAsync(apiBaseUrl, p2Key, npcId, ct);

                var prompt = BuildMarketOutlookPrompt(request, requestedCount);
                var townProfile = ResolveActiveTownProfile();
                var url = $"{apiBaseUrl.TrimEnd('/')}/npcs/{Uri.EscapeDataString(npcId)}/chat";
                using var msg = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(JsonSerializer.Serialize(new NpcChatRequest
                    {
                        SenderName = townProfile.NewspaperDeskName,
                        SenderMessage = prompt,
                        GameStateInfo = BuildMarketOutlookStateInfo(request.Context)
                    }, _jsonOptions), Encoding.UTF8, "application/json")
                };
                msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", p2Key);

                using var res = await _http.SendAsync(msg, ct);
                var body = await res.Content.ReadAsStringAsync(ct);
                res.EnsureSuccessStatusCode();

                var immediate = ParseMarketOutlookPayload(body, requestedCount);
                if (immediate.Count > 0)
                    return immediate;

                var immediateMessage = TryExtractLatestMessage(body);
                var immediateFromMessage = ParseMarketOutlookPayload(immediateMessage, requestedCount);
                if (immediateFromMessage.Count > 0)
                    return immediateFromMessage;

                var fromHistory = await WaitForFreshHeadlineFromHistoryAsync(apiBaseUrl, p2Key, npcId, previousHistoryMessage, ct);
                var historyOutlook = ParseMarketOutlookPayload(fromHistory, requestedCount);
                if (historyOutlook.Count > 0)
                    return historyOutlook;
            }
            catch (HttpRequestException ex) when (attempt == 0 && IsNpcMissing(ex))
            {
                _headlineEditorNpcId = null;
                continue;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Player2Client] GenerateMarketOutlookHintsAsync EXCEPTION: {ex.GetType().Name}: {ex.Message}");
                break;
            }
        }

        return new List<string>();
    }

    public async Task<RewrittenNewspaperEventArticle?> TryRewriteNewspaperEventArticleAsync(RewriteNewspaperEventArticleRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_apiBaseUrl) || string.IsNullOrWhiteSpace(_p2Key))
            return null;

        return await TryRewriteNewspaperEventArticleAsync(_apiBaseUrl, _p2Key, request, ct);
    }

    public async Task<RewrittenNewspaperEventArticle?> TryRewriteNewspaperEventArticleAsync(
        string apiBaseUrl,
        string p2Key,
        RewriteNewspaperEventArticleRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(apiBaseUrl) || string.IsNullOrWhiteSpace(p2Key))
            return null;

        for (var attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                var npcId = await EnsureHeadlineEditorNpcIdAsync(apiBaseUrl, p2Key, ct);
                var previousHistoryMessage = await TryGetLatestNpcMessageFromHistoryAsync(apiBaseUrl, p2Key, npcId, ct);

                var prompt = BuildEventArticleRewritePrompt(request);
                var townProfile = ResolveActiveTownProfile();
                var url = $"{apiBaseUrl.TrimEnd('/')}/npcs/{Uri.EscapeDataString(npcId)}/chat";
                using var msg = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(JsonSerializer.Serialize(new NpcChatRequest
                    {
                        SenderName = townProfile.NewspaperDeskName,
                        SenderMessage = prompt,
                        GameStateInfo = BuildEventArticleRewriteStateInfo(request.Context)
                    }, _jsonOptions), Encoding.UTF8, "application/json")
                };
                msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", p2Key);

                using var res = await _http.SendAsync(msg, ct);
                var body = await res.Content.ReadAsStringAsync(ct);
                res.EnsureSuccessStatusCode();

                var immediateMessage = TryExtractLatestMessage(body);
                var immediateFromMessage = ParseRewrittenEventArticlePayload(immediateMessage);
                if (immediateFromMessage is not null)
                    return immediateFromMessage;

                var immediate = ParseRewrittenEventArticlePayload(body);
                if (immediate is not null)
                    return immediate;

                var fromHistory = await WaitForFreshHeadlineFromHistoryAsync(apiBaseUrl, p2Key, npcId, previousHistoryMessage, ct);
                var historyRewrite = ParseRewrittenEventArticlePayload(fromHistory);
                if (historyRewrite is not null)
                    return historyRewrite;
            }
            catch (HttpRequestException ex) when (attempt == 0 && IsNpcMissing(ex))
            {
                _headlineEditorNpcId = null;
                continue;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Player2Client] TryRewriteNewspaperEventArticleAsync EXCEPTION: {ex.GetType().Name}: {ex.Message}");
                break;
            }
        }

        return null;
    }

    private static string BuildArticleGenerationPrompt(GenerateArticlesRequest request, int count)
    {
        var season = request.Context?.Season ?? "spring";
        var day = request.Context?.Day ?? 1;
        var year = request.Context?.Year ?? 1;
        var existing = request.Context?.ExistingArticles ?? new List<string>();
        var existingList = existing.Count == 0 ? "(none)" : string.Join(", ", existing.Take(20));
        var languageRule = I18n.BuildPromptLanguageInstruction();
        var townProfile = ResolveActiveTownProfile();

        return
            $"You are the editor of {townProfile.NewspaperTitle} in Stardew Valley. " +
            $"Generate {count} fresh short newspaper stories for season {season}, day {day}, year {year}. " +
            $"Avoid repeating these recent titles: {existingList}. " +
            $"{languageRule} " +
            "Stories should feel like town progression for this point in the year. " +
            "Each story must fit within 80 characters total (title + content). " +
            "For JSON responses, translate only string values. Never translate JSON keys. " +
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

    private static string BuildMarketOutlookPrompt(GenerateMarketOutlookRequest request, int count)
    {
        var context = request.Context ?? new MarketOutlookContext();
        var movers = context.MarketMovers.Count == 0 ? "(none)" : string.Join("; ", context.MarketMovers.Take(8));
        var eventsList = context.ActiveEvents.Count == 0 ? "(none)" : string.Join("; ", context.ActiveEvents.Take(6));
        var scarcity = string.IsNullOrWhiteSpace(context.ScarcityLead) ? "(none)" : context.ScarcityLead;
        var season = string.IsNullOrWhiteSpace(context.Season) ? "spring" : context.Season;
        var mode = string.IsNullOrWhiteSpace(context.Mode) ? "cozy_canon" : context.Mode;
        var languageRule = I18n.BuildPromptLanguageInstruction();
        var townProfile = ResolveActiveTownProfile();

        return
            $"You are the editor of {townProfile.NewspaperTitle} writing the Market Outlook section for Stardew Valley. " +
            $"Generate {count} concise outlook lines grounded in current game signals. " +
            $"Season={season}, Day={context.Day}, Year={context.Year}, Mode={mode}. " +
            $"Market movers: {movers}. Scarcity signal: {scarcity}. Active events: {eventsList}. " +
            $"{languageRule} " +
            "Focus on near-term guidance for tomorrow's market. Keep each line under 95 characters. " +
            "For JSON responses, translate only string values. Never translate JSON keys. " +
            "Return STRICT JSON only with schema: {\"outlook\":[\"line 1\",\"line 2\"]}. " +
            "No markdown, no prose outside JSON.";
    }

    private static string BuildMarketOutlookStateInfo(MarketOutlookContext? context)
    {
        if (context is null)
            return "market_outlook_generation";

        var mode = string.IsNullOrWhiteSpace(context.Mode) ? "cozy_canon" : context.Mode;
        return $"market_outlook_generation: season={context.Season}, day={context.Day}, year={context.Year}, mode={mode}";
    }

    private static string BuildEventArticleRewritePrompt(RewriteNewspaperEventArticleRequest request)
    {
        var context = request.Context ?? new NewspaperEventRewriteContext();
        var season = string.IsNullOrWhiteSpace(context.Season) ? "spring" : context.Season;
        var kind = string.IsNullOrWhiteSpace(context.EventKind) ? "incident" : context.EventKind.Trim();
        var visibility = string.IsNullOrWhiteSpace(context.EventVisibility) ? "local" : context.EventVisibility.Trim();
        var location = string.IsNullOrWhiteSpace(context.EventLocation) ? "(none)" : context.EventLocation.Trim();
        var summary = string.IsNullOrWhiteSpace(context.SourceSummary) ? "(none)" : context.SourceSummary.Trim();
        var requiredDayLabel = string.IsNullOrWhiteSpace(context.RequiredDayLabel) ? "(none)" : context.RequiredDayLabel.Trim();
        var originalTitle = string.IsNullOrWhiteSpace(context.OriginalTitle) ? "(none)" : context.OriginalTitle.Trim();
        var originalContent = string.IsNullOrWhiteSpace(context.OriginalContent) ? "(none)" : context.OriginalContent.Trim();
        var languageRule = I18n.BuildPromptLanguageInstruction();
        var townProfile = ResolveActiveTownProfile();
        var locationRule = string.Equals(location, "(none)", StringComparison.Ordinal)
            ? "Location is unspecified; do not invent one."
            : $"Location to preserve verbatim: {location}.";

        return
            $"You are the editor of {townProfile.NewspaperTitle} in Stardew Valley. " +
            $"Rewrite one event article for season {season}, day {context.Day}, year {context.Year}. " +
            $"{languageRule} " +
            "Style goal: immersive local-newspaper tone, concise and grounded. " +
            "Fact guardrail: DO NOT alter facts, timeline, participants, outcomes, or severity. " +
            "Rewrite requirement: rewrite both title and body, and do not copy original draft wording except required fact strings. " +
            "Do not invent names, causes, injuries, or consequences. " +
            $"Event metadata: kind={kind}, event_day={context.EventDay}, visibility={visibility}, severity={context.EventSeverity}. " +
            $"Summary to preserve verbatim: {summary}. " +
            $"Day label to preserve verbatim: {requiredDayLabel}. " +
            $"{locationRule} " +
            $"Original draft title: {originalTitle}. Original draft content: {originalContent}. " +
            "Output must include the exact summary text and exact day label text in content. " +
            "For JSON responses, translate only string values. Never translate JSON keys. " +
            "Return STRICT JSON only with schema: {\"title\":\"...\",\"content\":\"...\"}. " +
            "No markdown, no prose outside JSON.";
    }

    private static string BuildEventArticleRewriteStateInfo(NewspaperEventRewriteContext? context)
    {
        if (context is null)
            return "event_article_rewrite";

        return
            $"event_article_rewrite: season={context.Season}, day={context.Day}, year={context.Year}, " +
            $"event_day={context.EventDay}, kind={context.EventKind}, visibility={context.EventVisibility}, severity={context.EventSeverity}";
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

    private List<string> ParseMarketOutlookPayload(string? payload, int maxCount)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return new List<string>();

        var candidates = BuildJsonCandidates(payload);
        foreach (var candidate in candidates)
        {
            if (!TryParseMarketOutlookJson(candidate, maxCount, out var hints))
                continue;

            if (hints.Count > 0)
                return hints;
        }

        return ParseMarketOutlookPlainText(payload, maxCount);
    }

    private RewrittenNewspaperEventArticle? ParseRewrittenEventArticlePayload(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return null;

        var candidates = BuildJsonCandidates(payload);
        foreach (var candidate in candidates)
        {
            if (TryParseRewrittenEventArticleJson(candidate, out var rewritten))
                return rewritten;
        }

        return ParseRewrittenEventArticlePlainText(payload);
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

    private bool TryParseMarketOutlookJson(string json, int maxCount, out List<string> hints)
    {
        hints = new List<string>();
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Object)
            {
                if (TryGetPropertyCaseInsensitive(root, "outlook", out var outlookProp))
                    AppendMarketOutlookItems(outlookProp, hints, maxCount);
                else if (TryGetPropertyCaseInsensitive(root, "hints", out var hintsProp))
                    AppendMarketOutlookItems(hintsProp, hints, maxCount);
                else if (TryGetPropertyCaseInsensitive(root, "lines", out var linesProp))
                    AppendMarketOutlookItems(linesProp, hints, maxCount);
                else if (TryGetPropertyCaseInsensitive(root, "message", out var messageProp) && messageProp.ValueKind == JsonValueKind.String)
                    hints.AddRange(ParseMarketOutlookPlainText(messageProp.GetString(), maxCount));
            }
            else if (root.ValueKind == JsonValueKind.Array)
            {
                AppendMarketOutlookItems(root, hints, maxCount);
            }

            if (hints.Count > maxCount)
                hints = hints.Take(maxCount).ToList();

            return true;
        }
        catch
        {
            return false;
        }
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

                var cleanTitle = (title ?? string.Empty).Trim();
                var cleanContent = (content ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(cleanTitle) || string.IsNullOrWhiteSpace(cleanContent))
                    continue;

                if (!seenTitles.Add(cleanTitle))
                    continue;

                var category = TryGetStringProperty(item, "category", out var categoryValue)
                    ? NormalizeArticleCategory(categoryValue)
                    : "community";

                articles.Add(new NewspaperArticle
                {
                    Title = cleanTitle,
                    Content = cleanContent,
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

    private bool TryParseRewrittenEventArticleJson(string json, out RewrittenNewspaperEventArticle rewritten)
    {
        rewritten = new RewrittenNewspaperEventArticle();
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            JsonElement sourceObject;
            if (root.ValueKind == JsonValueKind.Object
                && TryGetPropertyCaseInsensitive(root, "article", out var articleObject)
                && articleObject.ValueKind == JsonValueKind.Object)
            {
                sourceObject = articleObject;
            }
            else if (root.ValueKind == JsonValueKind.Object
                && TryGetPropertyCaseInsensitive(root, "rewrite", out var rewriteObject)
                && rewriteObject.ValueKind == JsonValueKind.Object)
            {
                sourceObject = rewriteObject;
            }
            else if (root.ValueKind == JsonValueKind.Object
                && HasAnyStringProperty(root, "title", "headline", "subject")
                && HasAnyStringProperty(root, "content", "body"))
            {
                sourceObject = root;
            }
            else
            {
                return false;
            }

            var hasTitle = TryGetFirstStringProperty(sourceObject, out var title, "title", "headline", "subject");
            var hasContent = TryGetFirstStringProperty(sourceObject, out var content, "content", "body");
            if (!hasContent
                && TryGetFirstStringProperty(sourceObject, out var messageLikeContent, "message", "text")
                && IsLikelyBodyContent(messageLikeContent, hasTitle ? title : null))
            {
                hasContent = true;
                content = messageLikeContent;
            }

            if (!hasContent || string.IsNullOrWhiteSpace(content))
                return false;

            var cleanTitle = hasTitle ? SanitizeRewrittenEventText(title, MaxRewrittenEventTitleCharacters) : string.Empty;
            var cleanContent = SanitizeRewrittenEventText(content, MaxRewrittenEventContentCharacters);
            if (string.IsNullOrWhiteSpace(cleanContent))
                return false;

            rewritten = new RewrittenNewspaperEventArticle
            {
                Title = cleanTitle,
                Content = cleanContent
            };

            return true;
        }
        catch
        {
            return false;
        }
    }

    private RewrittenNewspaperEventArticle? ParseRewrittenEventArticlePlainText(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return null;

        var normalized = payload.Replace("\r", "\n", StringComparison.Ordinal);
        var lines = normalized
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();
        if (lines.Count == 0)
            return null;

        if (lines.Count == 1 && LooksLikeJsonPayload(lines[0]))
            return null;

        string? title = null;
        string? content = null;
        foreach (var line in lines)
        {
            if (title is null && TryExtractLabeledValue(line, out var labeledTitle, "title", "headline", "subject"))
            {
                title = labeledTitle;
                continue;
            }

            if (content is null && TryExtractLabeledValue(line, out var labeledContent, "content", "body", "message", "text"))
                content = labeledContent;
        }

        if (string.IsNullOrWhiteSpace(title) && lines.Count >= 2)
            title = lines[0];

        if (string.IsNullOrWhiteSpace(content) && lines.Count >= 2)
            content = string.Join(" ", lines.Skip(1));

        if (string.IsNullOrWhiteSpace(content) && lines.Count == 1)
            content = lines[0];

        if (string.IsNullOrWhiteSpace(content) || !IsLikelyBodyContent(content, title))
            return null;

        var cleanTitle = string.IsNullOrWhiteSpace(title)
            ? string.Empty
            : SanitizeRewrittenEventText(title, MaxRewrittenEventTitleCharacters);
        var cleanContent = SanitizeRewrittenEventText(content, MaxRewrittenEventContentCharacters);
        if (string.IsNullOrWhiteSpace(cleanContent))
            return null;

        return new RewrittenNewspaperEventArticle
        {
            Title = cleanTitle,
            Content = cleanContent
        };
    }

    private static void AppendMarketOutlookItems(JsonElement source, List<string> output, int maxCount)
    {
        if (output.Count >= maxCount)
            return;

        if (source.ValueKind == JsonValueKind.String)
        {
            var line = SanitizeMarketOutlookLine(source.GetString());
            if (!string.IsNullOrWhiteSpace(line) && !output.Contains(line, StringComparer.OrdinalIgnoreCase))
                output.Add(line);
            return;
        }

        if (source.ValueKind != JsonValueKind.Array)
            return;

        foreach (var item in source.EnumerateArray())
        {
            if (output.Count >= maxCount)
                break;

            var line = item.ValueKind == JsonValueKind.String
                ? SanitizeMarketOutlookLine(item.GetString())
                : null;

            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (output.Contains(line, StringComparer.OrdinalIgnoreCase))
                continue;

            output.Add(line);
        }
    }

    private static List<string> ParseMarketOutlookPlainText(string? payload, int maxCount)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return new List<string>();

        var normalized = payload.Replace("\r", "\n", StringComparison.Ordinal);
        var parts = normalized.Split(new[] { '\n', ';', '|' }, StringSplitOptions.RemoveEmptyEntries);
        var hints = new List<string>();

        foreach (var part in parts)
        {
            if (hints.Count >= maxCount)
                break;

            var line = SanitizeMarketOutlookLine(part);
            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (hints.Contains(line, StringComparer.OrdinalIgnoreCase))
                continue;

            hints.Add(line);
        }

        if (hints.Count == 0)
        {
            var single = SanitizeMarketOutlookLine(payload);
            if (!string.IsNullOrWhiteSpace(single))
                hints.Add(single);
        }

        return hints.Take(maxCount).ToList();
    }

    private static string? SanitizeMarketOutlookLine(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var line = raw
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Trim()
            .Trim('"')
            .Trim('\'')
            .Trim();

        if (line.StartsWith("- ", StringComparison.Ordinal))
            line = line[2..].Trim();

        if (line.Length > MaxMarketOutlookLineCharacters)
            line = line[..(MaxMarketOutlookLineCharacters - 3)].TrimEnd() + "...";

        if (!line.Any(char.IsLetter))
            return null;

        return string.IsNullOrWhiteSpace(line) ? null : line;
    }

    private static string SanitizeRewrittenEventText(string? raw, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        var line = raw
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Trim()
            .Trim('"')
            .Trim('\'')
            .Trim();

        while (line.Contains("  ", StringComparison.Ordinal))
            line = line.Replace("  ", " ", StringComparison.Ordinal);

        if (line.Length > maxLength)
            line = line[..(maxLength - 3)].TrimEnd() + "...";

        return line;
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

    private static bool HasAnyStringProperty(JsonElement obj, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (TryGetStringProperty(obj, propertyName, out var value) && !string.IsNullOrWhiteSpace(value))
                return true;
        }

        return false;
    }

    private static bool TryGetFirstStringProperty(JsonElement obj, out string value, params string[] propertyNames)
    {
        value = string.Empty;
        foreach (var propertyName in propertyNames)
        {
            if (!TryGetStringProperty(obj, propertyName, out var candidate) || string.IsNullOrWhiteSpace(candidate))
                continue;

            value = candidate;
            return true;
        }

        return false;
    }

    private static bool TryExtractLabeledValue(string line, out string value, params string[] labels)
    {
        value = string.Empty;
        if (string.IsNullOrWhiteSpace(line))
            return false;

        foreach (var label in labels)
        {
            var prefix = $"{label}:";
            if (!line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            value = line[prefix.Length..].Trim();
            return !string.IsNullOrWhiteSpace(value);
        }

        return false;
    }

    private static bool IsLikelyBodyContent(string? candidate, string? title)
    {
        if (string.IsNullOrWhiteSpace(candidate))
            return false;

        var cleanCandidate = candidate.Trim();
        if (cleanCandidate.Length < 36)
            return false;

        if (!string.IsNullOrWhiteSpace(title)
            && cleanCandidate.Equals(title.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var wordCount = cleanCandidate.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        if (wordCount < 7)
            return false;

        return cleanCandidate.Any(ch => ch is '.' or '!' or '?') || wordCount >= 12;
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
    /// Calls onConnected once headers are received, then onLine for each non-empty line until cancelled or stream closes.
    /// </summary>
    public async Task StreamNpcResponsesAsync(string apiBaseUrl, string p2Key, Func<string, Task> onLine, CancellationToken ct, Func<Task>? onConnected = null)
    {
        var url = $"{apiBaseUrl.TrimEnd('/')}/npcs/responses";
        using var msg = new HttpRequestMessage(HttpMethod.Get, url);
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", p2Key);
        msg.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var res = await _http.SendAsync(msg, HttpCompletionOption.ResponseHeadersRead, ct);
        res.EnsureSuccessStatusCode();
        if (onConnected is not null)
            await onConnected();

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

    private sealed class DeviceAuthStartAttemptResult
    {
        public bool IsSuccessStatusCode { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public string Body { get; set; } = string.Empty;
    }

    private sealed class DeviceAuthTokenPollAttemptResult
    {
        public bool IsSuccessStatusCode { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public string Body { get; set; } = string.Empty;
        public IReadOnlyDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
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
                                  || string.Equals(Status, "error", StringComparison.OrdinalIgnoreCase)
                                  || string.Equals(Status, "unauthorized_client", StringComparison.OrdinalIgnoreCase)
                                  || string.Equals(Status, "access_denied", StringComparison.OrdinalIgnoreCase)
                                  || string.Equals(Status, "expired_token", StringComparison.OrdinalIgnoreCase);
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

public sealed class NpcHistorySnapshot
{
    public string? LatestMessage { get; set; }
    public string SnapshotHash { get; set; } = string.Empty;
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
    public string CharacterDescription { get; set; } = "You are the newspaper editor for the town paper. Generate seasonal filler articles based on game progress.";

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

public sealed class RewriteNewspaperEventArticleRequest
{
    [JsonPropertyName("short_name")]
    public string ShortName { get; set; } = "Editor";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "Town Editor";

    [JsonPropertyName("character_description")]
    public string CharacterDescription { get; set; } = "You rewrite town event briefs into immersive but factual newspaper prose.";

    [JsonPropertyName("system_prompt")]
    public string SystemPrompt { get; set; } = "Rewrite the provided event article for readability while preserving facts exactly.";

    [JsonPropertyName("keep_game_state")]
    public bool KeepGameState { get; set; } = false;

    [JsonPropertyName("context")]
    public NewspaperEventRewriteContext? Context { get; set; }
}

public sealed class NewspaperEventRewriteContext
{
    [JsonPropertyName("season")]
    public string Season { get; set; } = "spring";

    [JsonPropertyName("day")]
    public int Day { get; set; }

    [JsonPropertyName("year")]
    public int Year { get; set; } = 1;

    [JsonPropertyName("event_day")]
    public int EventDay { get; set; }

    [JsonPropertyName("event_kind")]
    public string EventKind { get; set; } = "incident";

    [JsonPropertyName("event_location")]
    public string EventLocation { get; set; } = string.Empty;

    [JsonPropertyName("event_visibility")]
    public string EventVisibility { get; set; } = "local";

    [JsonPropertyName("event_severity")]
    public int EventSeverity { get; set; } = 1;

    [JsonPropertyName("source_summary")]
    public string SourceSummary { get; set; } = string.Empty;

    [JsonPropertyName("required_day_label")]
    public string RequiredDayLabel { get; set; } = string.Empty;

    [JsonPropertyName("original_title")]
    public string OriginalTitle { get; set; } = string.Empty;

    [JsonPropertyName("original_content")]
    public string OriginalContent { get; set; } = string.Empty;
}

public sealed class RewrittenNewspaperEventArticle
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

public sealed class GenerateMarketOutlookRequest
{
    [JsonPropertyName("short_name")]
    public string ShortName { get; set; } = "Editor";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "Town Editor";

    [JsonPropertyName("character_description")]
    public string CharacterDescription { get; set; } = "You write concise market outlook notes grounded in live town economy data.";

    [JsonPropertyName("system_prompt")]
    public string SystemPrompt { get; set; } = "Generate short market outlook lines for the town newspaper based only on the provided state.";

    [JsonPropertyName("keep_game_state")]
    public bool KeepGameState { get; set; } = false;

    [JsonPropertyName("context")]
    public MarketOutlookContext? Context { get; set; }
}

public sealed class MarketOutlookContext
{
    [JsonPropertyName("season")]
    public string Season { get; set; } = "spring";

    [JsonPropertyName("day")]
    public int Day { get; set; }

    [JsonPropertyName("year")]
    public int Year { get; set; } = 1;

    [JsonPropertyName("mode")]
    public string Mode { get; set; } = "cozy_canon";

    [JsonPropertyName("market_movers")]
    public List<string> MarketMovers { get; set; } = new();

    [JsonPropertyName("scarcity_lead")]
    public string ScarcityLead { get; set; } = string.Empty;

    [JsonPropertyName("active_events")]
    public List<string> ActiveEvents { get; set; } = new();

    [JsonPropertyName("count")]
    public int Count { get; set; } = 2;
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

