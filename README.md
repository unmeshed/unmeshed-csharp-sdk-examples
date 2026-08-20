# Unmeshed C# SDK — Worker Examples

Example workers built with the [unmeshed-csharp-sdk](https://www.nuget.org/packages/unmeshed-csharp-sdk).

## About Unmeshed

[Unmeshed](https://unmeshed.io) is a workflow orchestration platform for building resilient, durable business processes — connecting APIs, AI agents, and human-in-the-loop tasks into observable, end-to-end flows.

- 🔄 **Processes & Steps** — Model workflows as processes composed of individual steps (API calls, scripts, approvals, AI invocations).
- 🤖 **AI-native** — First-class support for governing AI model calls, optimising token usage, and tying AI operations to business outcomes.
- 👤 **Human-in-the-Loop** — Pause workflows for approvals, structured input, or manual decisions before continuing.
- 🔁 **Resilient by default** — Automatic retries, state persistence, and long-running execution support out of the box.
- 🛠️ **Multi-language SDKs** — Official SDKs for Java, Go, TypeScript, Python, and C#.

👉 [Documentation](https://unmeshed.io/docs/start-here/getting-started)) · [C# SDK](https://github.com/unmeshed/unmeshed-csharp-sdk) · [GitHub](https://github.com/unmeshed)

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- An Unmeshed Engine instance ([get started](https://unmeshed.io/docs/start-here/getting-started))

## Setup

1. **Clone the repo**

   ```bash
   git clone https://github.com/unmeshed/unmeshed-csharp-sdk-examples.git
   cd unmeshed-csharp-sdk-examples
   ```

2. **Set your credentials**

   You'll need your **Client ID** and **Auth Token** from your [Unmeshed Instance](https://your-instance.unmeshed.com).

   ```bash
   export UNMESHED_AUTH_ID="your-client-id"
   export UNMESHED_AUTH_TOKEN="your-auth-token"
   export UNMESHED_ENGINE_URL="http://localhost"
   export UNMESHED_ENGINE_PORT="8080"
   ```

   See [`.env.example`](.env.example) for the full list of optional configuration variables.

3. **Run**

   ```bash
   dotnet restore
   dotnet run
   ```

   The app will connect to the engine and start polling for work. Press `Ctrl+C` to stop.

## Example Workers

### Echo Worker

A simple worker that echoes the input back. Demonstrates the basic `[WorkerFunction]` attribute pattern and `WorkContext` access.

```csharp
[WorkerFunction(Name = "echo", Namespace = "default", MaxInProgress = 100, IoThread = true)]
public async Task<EchoResponse> EchoMessageAsync(Dictionary<string, object> input)
{
    var request = JsonSerializer.Deserialize<EchoRequest>(JsonSerializer.Serialize(input));
    if (request.DelayMs > 0) await Task.Delay(request.DelayMs);

    return new EchoResponse
    {
        Echo = request.Message,
        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        ProcessedBy = Environment.MachineName
    };
}
```

### Calculator Worker

Shows multiple worker functions on a single class, different return types (primitives, maps, lists), deliberate failures for error-handling tests, and step rescheduling via `StepResult`.

Browse all examples in the [`Examples/`](Examples/) directory.

## License

MIT
