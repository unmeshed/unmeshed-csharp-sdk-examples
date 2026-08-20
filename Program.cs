using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Unmeshed.Sdk;
using Unmeshed.Sdk.Configuration;
using Unmeshed.Sdk.Workers.Examples;

namespace Unmeshed.Sdk.Workers;

/// <summary>
/// Main entry point for the Unmeshed SDK Workers example application.
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Starting Unmeshed SDK Workers (C#)");
        Console.WriteLine("===================================");

        // Configure logging
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddConsole()
                .SetMinimumLevel(GetLogLevel());
        });

        var logger = loggerFactory.CreateLogger<Program>();

        try
        {
            // Load configuration from environment variables
            var config = LoadConfiguration();

            logger.LogInformation("Connecting to Unmeshed engine at {Url}", config.ServerUrl);
            logger.LogInformation("Client ID: {ClientId}", config.ClientId);

            // Create DI container for worker dependencies
            var services = new ServiceCollection();
            services.AddSingleton<IGreetingProvider, GreetingProvider>();
            services.AddTransient<DiWorker>();
            var serviceProvider = services.BuildServiceProvider();
            WorkerScanner.ConfigureServiceProvider(serviceProvider);

            // Create the Unmeshed client
            using var client = new UnmeshedClient(config, loggerFactory);

            // Register workers based on command-line args or defaults
            await RegisterWorkersAsync(client, logger);

            // Start the client (begins polling)
            logger.LogInformation("Starting client...");
            await client.StartAsync();

            logger.LogInformation("SDK workers started successfully. Press Ctrl+C to stop.");

            // Wait for cancellation
            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
                logger.LogInformation("Shutdown requested...");
            };

            await Task.Delay(Timeout.Infinite, cts.Token);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Application stopped");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fatal error: {Message}", ex.Message);
            Environment.Exit(1);
        }
    }

    /// <summary>
    /// Loads configuration from environment variables.
    /// </summary>
    private static ClientConfig LoadConfiguration()
    {
        var clientId = GetEnv("UNMESHED_AUTH_ID", "");
        var authToken = GetEnv("UNMESHED_AUTH_TOKEN", "");
        var baseUrl = GetEnv("UNMESHED_ENGINE_URL", "http://localhost");
        var port = int.Parse(GetEnv("UNMESHED_ENGINE_PORT", "8080"));
        var batchSize = int.Parse(GetEnv("UNMESHED_WORK_BATCH_SIZE", "200"));
        var responseBatchSize = int.Parse(GetEnv("UNMESHED_WORK_RESPONSE_BATCH_SIZE", "50"));
        var maxSubmitAttempts = int.Parse(GetEnv("UNMESHED_MAX_SUBMIT_ATTEMPTS", "100"));
        var connectionTimeoutSeconds = int.Parse(GetEnv("UNMESHED_CONNECTION_TIMEOUT_SECONDS", "60"));
        var fixedThreadPoolSize = int.Parse(GetEnv("UNMESHED_FIXED_THREAD_POOL_SIZE", "2"));
        var enableResultsSubmission = bool.Parse(GetEnv("UNMESHED_ENABLE_RESULTS_SUBMISSION", "true"));

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(authToken))
        {
            throw new InvalidOperationException(
                "Required environment variables not set:\n" +
                "  * UNMESHED_AUTH_ID\n" +
                "  * UNMESHED_AUTH_TOKEN\n" +
                "  * UNMESHED_ENGINE_URL");
        }

        return new ClientConfig
        {
            ClientId = clientId,
            AuthToken = authToken,
            BaseUrl = baseUrl,
            Port = port,
            WorkRequestBatchSize = batchSize,
            ResponseSubmitBatchSize = responseBatchSize,
            MaxSubmitAttempts = maxSubmitAttempts,
            ConnectionTimeoutSeconds = connectionTimeoutSeconds,
            FixedThreadPoolSize = fixedThreadPoolSize,
            EnableResultsSubmission = enableResultsSubmission,
            InitialDelayMillis = 20,
            StepTimeoutMillis = 1000L * 60 * 60 * 24 * 365 // 1 year (effectively no timeout)
        };
    }

    /// <summary>
    /// Registers workers via attribute scanning and optionally programmatically.
    /// </summary>
    private static async Task RegisterWorkersAsync(UnmeshedClient client, ILogger logger)
    {
        // Register workers using attribute scanning
        logger.LogInformation("Scanning for workers in namespace: Unmeshed.Sdk.Workers.Examples");
        await client.RegisterWorkersAsync("Unmeshed.Sdk.Workers.Examples");

        // Optionally register additional workers from a custom namespace
        var customWorkerNamespace = GetEnv("UNMESHED_CUSTOM_WORKERS", "");
        if (!string.IsNullOrWhiteSpace(customWorkerNamespace))
        {
            logger.LogInformation("Registering custom workers from: {Namespace}", customWorkerNamespace);
            await client.RegisterWorkersAsync(customWorkerNamespace);
        }

        // Register a simple programmatic (lambda) worker as an example
        await client.RegisterWorkerFunctionAsync(
            workerFunction: async (input) =>
            {
                await Task.Delay(100); // Simulate work
                return new
                {
                    message = "Hello from programmatic worker!",
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    input
                };
            },
            @namespace: "default",
            name: "hello_world",
            maxInProgress: 10,
            ioThread: true
        );

        logger.LogInformation("All workers registered successfully");
    }

    /// <summary>
    /// Gets an environment variable with a default value.
    /// </summary>
    private static string GetEnv(string name, string defaultValue)
    {
        return Environment.GetEnvironmentVariable(name) ?? defaultValue;
    }

    /// <summary>
    /// Gets the log level from environment variable.
    /// </summary>
    private static LogLevel GetLogLevel()
    {
        var logLevel = GetEnv("LOG_LEVEL", "Information");
        return Enum.TryParse<LogLevel>(logLevel, true, out var level)
            ? level
            : LogLevel.Information;
    }
}
