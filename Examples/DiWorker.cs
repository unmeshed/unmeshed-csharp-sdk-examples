using Unmeshed.Sdk;
using Unmeshed.Sdk.Workers;

namespace Unmeshed.Sdk.Workers.Examples;

// ─── Contracts & Implementations ─────────────────────────────────────

/// <summary>
/// Simple service interface to demonstrate DI in workers.
/// </summary>
public interface IGreetingProvider
{
    string GetGreeting(string name);
}

/// <summary>
/// Default implementation of <see cref="IGreetingProvider"/>.
/// </summary>
public class GreetingProvider : IGreetingProvider
{
    public string GetGreeting(string name) => $"Hello, {name}! Welcome to Unmeshed 🚀";
}

// ─── Worker ──────────────────────────────────────────────────────────

/// <summary>
/// Demonstrates constructor-based dependency injection in workers.
/// Register this worker's dependencies in <c>Program.cs</c> via
/// <see cref="Microsoft.Extensions.DependencyInjection.ServiceCollection"/>
/// and call <c>WorkerScanner.ConfigureServiceProvider(serviceProvider)</c>.
/// </summary>
public class DiWorker
{
    private readonly IGreetingProvider _greetingProvider;

    public DiWorker(IGreetingProvider greetingProvider)
    {
        _greetingProvider = greetingProvider;
    }

    [WorkerFunction(Name = "greet", Namespace = "default", MaxInProgress = 50)]
    public Task<object> GreetAsync(Dictionary<string, object> input)
    {
        var name = "World";
        if (input.TryGetValue("name", out var nameObj) && nameObj != null)
        {
            name = nameObj.ToString() ?? name;
        }

        var greeting = _greetingProvider.GetGreeting(name);

        return Task.FromResult<object>(new
        {
            greeting,
            injectedService = _greetingProvider.GetType().Name,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        });
    }
}
