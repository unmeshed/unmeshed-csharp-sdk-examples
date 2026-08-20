using System.Text.Json;
using Unmeshed.Sdk;
using Unmeshed.Sdk.Workers;
using Unmeshed.Sdk.Models;

namespace Unmeshed.Sdk.Workers.Examples;

/// <summary>
/// Simulates a long-running batch processing job that reports progress
/// and reschedules itself until all items are processed.
/// </summary>
public class LongRunningWorker
{
    /// <summary>
    /// Processes items in batches. Each invocation processes a batch and reschedules
    /// itself until all items are done.
    /// Input: { "totalItems": 50, "batchSize": 10, "processedSoFar": 0 }
    /// </summary>
    [WorkerFunction(Name = "batch_process", Namespace = "default", MaxInProgress = 5)]
    public async Task<StepResult> BatchProcessAsync(Dictionary<string, object> input)
    {
        var totalItems = GetInt(input, "totalItems") ?? 100;
        var batchSize = GetInt(input, "batchSize") ?? 10;
        var processedSoFar = GetInt(input, "processedSoFar") ?? 0;

        // Simulate batch processing time
        var itemsToProcess = Math.Min(batchSize, totalItems - processedSoFar);
        Console.WriteLine($"[BatchProcess] Processing items {processedSoFar + 1} to {processedSoFar + itemsToProcess} of {totalItems}");
        await Task.Delay(itemsToProcess * 50); // 50ms per item

        var newProcessed = processedSoFar + itemsToProcess;
        var progress = (double)newProcessed / totalItems * 100;

        if (newProcessed < totalItems)
        {
            // More work to do — reschedule
            return new StepResult
            {
                Output = new Dictionary<string, object>
                {
                    { "totalItems", totalItems },
                    { "batchSize", batchSize },
                    { "processedSoFar", newProcessed },
                    { "progress", $"{progress:F1}%" },
                    { "status", "in_progress" }
                },
                Status = UnmeshedConstants.StepStatus.Running,
                RescheduleAfterSeconds = 2
            };
        }

        // All done
        return new StepResult
        {
            Output = new Dictionary<string, object>
            {
                { "totalItems", totalItems },
                { "processedSoFar", newProcessed },
                { "progress", "100%" },
                { "status", "completed" },
                { "completedAt", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }
            },
            Status = UnmeshedConstants.StepStatus.Completed
        };
    }

    /// <summary>
    /// A slow worker that simply sleeps for a configurable duration.
    /// Useful for testing timeouts and concurrency.
    /// Input: { "sleepMs": 5000 }
    /// </summary>
    [WorkerFunction(Name = "slow_worker", Namespace = "default", MaxInProgress = 10, IoThread = true)]
    public async Task<object> SlowWorkerAsync(Dictionary<string, object> input)
    {
        var sleepMs = GetInt(input, "sleepMs") ?? 3000;

        Console.WriteLine($"[SlowWorker] Sleeping for {sleepMs}ms");
        await Task.Delay(sleepMs);

        return new
        {
            message = $"Woke up after {sleepMs}ms",
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
    }

    // ─── Helpers ─────────────────────────────────────────────────────

    private static int? GetInt(Dictionary<string, object> input, string key)
    {
        if (!input.TryGetValue(key, out var value)) return null;
        if (value is int i) return i;
        if (value is JsonElement je && je.ValueKind == JsonValueKind.Number)
            return je.GetInt32();
        if (int.TryParse(value?.ToString(), out var parsed))
            return parsed;
        return null;
    }
}
