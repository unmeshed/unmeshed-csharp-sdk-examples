using System.Text.Json;
using Unmeshed.Sdk;
using Unmeshed.Sdk.Workers;

namespace Unmeshed.Sdk.Workers.Examples;

/// <summary>
/// Worker that makes HTTP requests — demonstrates I/O-bound async work.
/// Useful for API integrations within workflow pipelines.
/// </summary>
public class HttpWorker
{
    private static readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    /// <summary>
    /// Makes a GET request to a given URL and returns the response body and metadata.
    /// Input: { "url": "https://httpbin.org/get", "headers": { "X-Custom": "value" } }
    /// </summary>
    [WorkerFunction(Name = "http_get", Namespace = "default", MaxInProgress = 20, IoThread = true)]
    public async Task<object> HttpGetAsync(Dictionary<string, object> input)
    {
        var url = GetString(input, "url")
            ?? throw new ArgumentException("'url' is required");

        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Add optional custom headers
        if (input.TryGetValue("headers", out var headersObj) &&
            headersObj is JsonElement headersEl &&
            headersEl.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in headersEl.EnumerateObject())
            {
                request.Headers.TryAddWithoutValidation(prop.Name, prop.Value.GetString());
            }
        }

        Console.WriteLine($"[HttpWorker] GET {url}");
        var response = await _httpClient.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        return new
        {
            url,
            statusCode = (int)response.StatusCode,
            contentLength = body.Length,
            body,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
    }

    /// <summary>
    /// Makes a POST request with a JSON body.
    /// Input: { "url": "https://httpbin.org/post", "body": { ... }, "headers": { ... } }
    /// </summary>
    [WorkerFunction(Name = "http_post", Namespace = "default", MaxInProgress = 20, IoThread = true)]
    public async Task<object> HttpPostAsync(Dictionary<string, object> input)
    {
        var url = GetString(input, "url")
            ?? throw new ArgumentException("'url' is required");

        var request = new HttpRequestMessage(HttpMethod.Post, url);

        // Set JSON body
        if (input.TryGetValue("body", out var bodyObj))
        {
            var jsonBody = bodyObj is JsonElement el ? el.GetRawText() : JsonSerializer.Serialize(bodyObj);
            request.Content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json");
        }

        // Add optional custom headers
        if (input.TryGetValue("headers", out var headersObj) &&
            headersObj is JsonElement headersEl &&
            headersEl.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in headersEl.EnumerateObject())
            {
                request.Headers.TryAddWithoutValidation(prop.Name, prop.Value.GetString());
            }
        }

        Console.WriteLine($"[HttpWorker] POST {url}");
        var response = await _httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        return new
        {
            url,
            statusCode = (int)response.StatusCode,
            contentLength = responseBody.Length,
            body = responseBody,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
    }

    // ─── Helpers ─────────────────────────────────────────────────────

    private static string? GetString(Dictionary<string, object> input, string key)
    {
        if (!input.TryGetValue(key, out var value)) return null;
        if (value is string s) return s;
        if (value is JsonElement je && je.ValueKind == JsonValueKind.String)
            return je.GetString();
        return value?.ToString();
    }
}
