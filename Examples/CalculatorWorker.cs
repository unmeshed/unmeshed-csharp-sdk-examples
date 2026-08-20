using System.Text.Json;
using Unmeshed.Sdk;
using Unmeshed.Sdk.Workers;
using Unmeshed.Sdk.Models;

namespace Unmeshed.Sdk.Workers.Examples;

/// <summary>
/// Worker that performs mathematical calculations.
/// Demonstrates multiple <see cref="WorkerFunction"/> methods on a single class,
/// different return types, error handling, and rescheduling.
/// </summary>
public class CalculatorWorker
{
    private static int _attempt = 0;

    // ─── Arithmetic ──────────────────────────────────────────────────

    [WorkerFunction(Name = "calculate", Namespace = "default", MaxInProgress = 50)]
    public async Task<object> CalculateAsync(Dictionary<string, object> input)
    {
        var operation = GetStringValue(input, "operation")
            ?? throw new ArgumentException("Operation is required");

        double a = GetDoubleValue(input, "a")
            ?? throw new ArgumentException("Parameter 'a' must be a number");

        double b = GetDoubleValue(input, "b")
            ?? throw new ArgumentException("Parameter 'b' must be a number");

        double result = operation.ToLower() switch
        {
            "add"      => a + b,
            "subtract" => a - b,
            "multiply" => a * b,
            "divide"   => b != 0
                            ? a / b
                            : throw new DivideByZeroException("Cannot divide by zero"),
            _ => throw new ArgumentException($"Unknown operation: {operation}")
        };

        await Task.CompletedTask;

        return new
        {
            operation,
            a,
            b,
            result,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
    }

    // ─── Deliberate Failure ──────────────────────────────────────────

    /// <summary>
    /// A worker that intentionally fails — useful for testing error handling.
    /// </summary>
    [WorkerFunction(Name = "fail", Namespace = "default", WorkStepNames = new[] { "step1", "step2" }, MaxInProgress = 100)]
    public Task FailAsync(Dictionary<string, object> input)
    {
        string message = "This is a deliberate failure.";

        if (input.TryGetValue("message", out var msgObj) && msgObj != null)
        {
            message = msgObj.ToString() ?? message;
        }

        throw new InvalidOperationException(message);
    }

    // ─── Return-type Variations ──────────────────────────────────────

    /// <summary>Returns a primitive string value.</summary>
    [WorkerFunction(Name = "return_primitive", Namespace = "default")]
    public string ReturnPrimitive(Dictionary<string, object> input)
    {
        return "Hello from primitive worker!";
    }

    /// <summary>Returns a dictionary / map.</summary>
    [WorkerFunction(Name = "return_map", Namespace = "default")]
    public Dictionary<string, object> ReturnMap(Dictionary<string, object> input)
    {
        return new Dictionary<string, object>
        {
            { "key1", "value1" },
            { "key2", 123 },
            { "nested", new { foo = "bar" } }
        };
    }

    /// <summary>Returns a list.</summary>
    [WorkerFunction(Name = "return_list", Namespace = "default")]
    public List<string> ReturnList(Dictionary<string, object> input)
    {
        return new List<string> { "item1", "item2", "item3" };
    }

    // ─── Rescheduling ────────────────────────────────────────────────

    /// <summary>
    /// Demonstrates rescheduling: the worker keeps returning a
    /// <see cref="StepResult"/> with <c>Running</c> status until the global
    /// attempt counter exceeds 3, then completes.
    /// </summary>
    [WorkerFunction(Name = "reschedule", Namespace = "default")]
    public StepResult RescheduleAsync(Dictionary<string, object> input)
    {
        _attempt++;
        Console.WriteLine($"[RescheduleWorker] Global Attempt = {_attempt}");

        if (_attempt <= 3)
        {
            return new StepResult
            {
                Output = new Dictionary<string, object>
                {
                    { "attempt", _attempt },
                    { "message", $"Rescheduling attempt {_attempt}" }
                },
                Status = UnmeshedConstants.StepStatus.Running,
                RescheduleAfterSeconds = 5
            };
        }

        return new StepResult
        {
            Output = new Dictionary<string, object>
            {
                { "attempt", _attempt },
                { "message", "Completed after global increments" }
            },
            Status = UnmeshedConstants.StepStatus.Completed
        };
    }

    // ─── Helpers ─────────────────────────────────────────────────────

    private static string? GetStringValue(Dictionary<string, object> input, string key)
    {
        if (!input.TryGetValue(key, out var value))
            return null;

        if (value is string str)
            return str;

        if (value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.String)
            return jsonElement.GetString();

        return value?.ToString();
    }

    private static double? GetDoubleValue(Dictionary<string, object> input, string key)
    {
        if (!input.TryGetValue(key, out var value))
            return null;

        if (value is double d) return d;
        if (value is int i) return i;

        if (value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Number)
            return jsonElement.GetDouble();

        if (double.TryParse(value?.ToString(), out var parsed))
            return parsed;

        return null;
    }
}
