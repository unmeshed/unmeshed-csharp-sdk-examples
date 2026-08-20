using System.Text.Json;
using System.Text.Json.Serialization;
using Unmeshed.Sdk;
using Unmeshed.Sdk.Workers;
using Unmeshed.Sdk.Models;

namespace Unmeshed.Sdk.Workers.Examples;

// ─── Request / Response Models ───────────────────────────────────────

/// <summary>
/// Request model for the echo worker.
/// </summary>
public class EchoRequest
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("delayMs")]
    public int DelayMs { get; set; } = 0;
}

/// <summary>
/// Response model for the echo worker.
/// </summary>
public class EchoResponse
{
    [JsonPropertyName("echo")]
    public string Echo { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    [JsonPropertyName("processedBy")]
    public string ProcessedBy { get; set; } = string.Empty;
}

// ─── Worker ──────────────────────────────────────────────────────────

/// <summary>
/// Simple echo worker for testing purposes.
/// Receives a message, optionally delays, then echoes the message back.
/// </summary>
public class EchoWorker
{
    [WorkerFunction(Name = "echo", Namespace = "default", MaxInProgress = 100, IoThread = true)]
    public async Task<EchoResponse> EchoMessageAsync(Dictionary<string, object> input)
    {
        var request = JsonSerializer.Deserialize<EchoRequest>(
            JsonSerializer.Serialize(input));

        if (request == null)
        {
            throw new ArgumentException("Invalid request: could not deserialize input");
        }

        // Access current work request context
        var currentWorkRequest = WorkContext.CurrentWorkRequest();
        if (currentWorkRequest != null)
        {
            Console.WriteLine($"[EchoWorker] Executing step {currentWorkRequest.StepName} (ID: {currentWorkRequest.StepId})");
            if (currentWorkRequest.ShardInstanceId != null)
            {
                Console.WriteLine($"[EchoWorker] Shard instance ID: {currentWorkRequest.ShardInstanceId}");
            }
        }

        // Simulate processing delay if specified
        if (request.DelayMs > 0)
        {
            await Task.Delay(request.DelayMs);
        }

        return new EchoResponse
        {
            Echo = request.Message,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ProcessedBy = Environment.MachineName
        };
    }
}
