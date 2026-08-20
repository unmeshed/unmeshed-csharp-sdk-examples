# Unmeshed C# SDK — Worker Examples

Example workers built with the [unmeshed-csharp-sdk](https://www.nuget.org/packages/unmeshed-csharp-sdk).

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- An Unmeshed Engine instance ([get started](https://docs.unmeshed.io))

## Setup

1. **Clone the repo**

   ```bash
   git clone https://github.com/unmeshed/unmeshed-csharp-sdk-examples.git
   cd unmeshed-csharp-sdk-examples
   ```

2. **Set your credentials**

   You'll need your **Client ID** and **Auth Token** from the [Unmeshed Console](https://console.unmeshed.io).

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
