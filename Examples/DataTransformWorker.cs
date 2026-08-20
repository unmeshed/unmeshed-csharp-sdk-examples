using System.Text.Json;
using System.Text.Json.Serialization;
using Unmeshed.Sdk;
using Unmeshed.Sdk.Workers;

namespace Unmeshed.Sdk.Workers.Examples;

/// <summary>
/// Workers that demonstrate data transformation patterns useful in workflow pipelines.
/// </summary>
public class DataTransformWorker
{
    // ─── String Transformation ───────────────────────────────────────

    /// <summary>
    /// Transforms input text: uppercase, lowercase, reverse, or title-case.
    /// </summary>
    [WorkerFunction(Name = "transform_text", Namespace = "default", MaxInProgress = 50, IoThread = true)]
    public Task<object> TransformTextAsync(Dictionary<string, object> input)
    {
        var text = GetString(input, "text")
            ?? throw new ArgumentException("'text' is required");

        var operation = GetString(input, "operation")?.ToLower() ?? "uppercase";

        string result = operation switch
        {
            "uppercase"  => text.ToUpperInvariant(),
            "lowercase"  => text.ToLowerInvariant(),
            "reverse"    => new string(text.Reverse().ToArray()),
            "titlecase"  => System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(text.ToLower()),
            "wordcount"  => text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length.ToString(),
            _ => throw new ArgumentException($"Unknown text operation: {operation}")
        };

        return Task.FromResult<object>(new
        {
            original = text,
            operation,
            result,
            length = result.Length,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        });
    }

    // ─── JSON Merge ──────────────────────────────────────────────────

    /// <summary>
    /// Merges two JSON objects into one. Useful for combining outputs from parallel steps.
    /// </summary>
    [WorkerFunction(Name = "merge_json", Namespace = "default", MaxInProgress = 50)]
    public Task<Dictionary<string, object>> MergeJsonAsync(Dictionary<string, object> input)
    {
        var merged = new Dictionary<string, object>();

        if (input.TryGetValue("left", out var leftObj) && leftObj is JsonElement leftEl && leftEl.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in leftEl.EnumerateObject())
                merged[prop.Name] = prop.Value;
        }

        if (input.TryGetValue("right", out var rightObj) && rightObj is JsonElement rightEl && rightEl.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in rightEl.EnumerateObject())
                merged[prop.Name] = prop.Value; // right wins on conflict
        }

        merged["_mergedAt"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        return Task.FromResult(merged);
    }

    // ─── Filter Array ────────────────────────────────────────────────

    /// <summary>
    /// Filters a JSON array of objects, keeping only items where a given field matches a value.
    /// Input: { "items": [...], "field": "status", "value": "active" }
    /// </summary>
    [WorkerFunction(Name = "filter_items", Namespace = "default", MaxInProgress = 50)]
    public Task<object> FilterItemsAsync(Dictionary<string, object> input)
    {
        var field = GetString(input, "field")
            ?? throw new ArgumentException("'field' is required");

        var value = GetString(input, "value")
            ?? throw new ArgumentException("'value' is required");

        if (!input.TryGetValue("items", out var itemsObj))
            throw new ArgumentException("'items' array is required");

        var items = itemsObj is JsonElement el && el.ValueKind == JsonValueKind.Array
            ? el.EnumerateArray().ToList()
            : throw new ArgumentException("'items' must be a JSON array");

        var filtered = items
            .Where(item =>
                item.ValueKind == JsonValueKind.Object &&
                item.TryGetProperty(field, out var prop) &&
                prop.ToString() == value)
            .ToList();

        return Task.FromResult<object>(new
        {
            total = items.Count,
            matched = filtered.Count,
            items = filtered,
            filter = new { field, value }
        });
    }

    // ─── Aggregate Numbers ───────────────────────────────────────────

    /// <summary>
    /// Aggregates a list of numbers: sum, average, min, max.
    /// Input: { "numbers": [1, 2, 3, 4, 5] }
    /// </summary>
    [WorkerFunction(Name = "aggregate_numbers", Namespace = "default", MaxInProgress = 50)]
    public Task<object> AggregateNumbersAsync(Dictionary<string, object> input)
    {
        if (!input.TryGetValue("numbers", out var numbersObj))
            throw new ArgumentException("'numbers' array is required");

        List<double> numbers;
        if (numbersObj is JsonElement el && el.ValueKind == JsonValueKind.Array)
        {
            numbers = el.EnumerateArray()
                .Where(n => n.ValueKind == JsonValueKind.Number)
                .Select(n => n.GetDouble())
                .ToList();
        }
        else
        {
            throw new ArgumentException("'numbers' must be a JSON array of numbers");
        }

        if (numbers.Count == 0)
            throw new ArgumentException("'numbers' array must not be empty");

        return Task.FromResult<object>(new
        {
            count = numbers.Count,
            sum = numbers.Sum(),
            average = numbers.Average(),
            min = numbers.Min(),
            max = numbers.Max(),
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        });
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
