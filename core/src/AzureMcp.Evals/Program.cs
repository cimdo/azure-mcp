using System.CommandLine;
using System.Text.Json;
using AzureMcp.Evals.Models;
using AzureMcp.Evals.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzureMcp.Evals;

/// <summary>
/// Main program for running MCP evaluations
/// </summary>
public static class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Azure MCP Evaluation Tool - Automated testing and scoring of MCP server implementations");

        var configOption = new Option<FileInfo?>(
            "--config",
            "Path to configuration file (JSON)")
        {
            IsRequired = false
        };

        var evaluationsOption = new Option<FileInfo>(
            "--evaluations",
            "Path to evaluations file (JSON or e2eTestPrompts.md)")
        {
            IsRequired = true
        };

        var serverOption = new Option<string>(
            "--server",
            "Path to MCP server executable")
        {
            IsRequired = false
        };

        var modelOption = new Option<string>(
            "--model",
            () => "gpt-4",
            "LLM model to use for scoring");

        var outputOption = new Option<DirectoryInfo>(
            "--output",
            () => new DirectoryInfo("./results"),
            "Output directory for results");

        var parallelOption = new Option<bool>(
            "--parallel",
            () => true,
            "Run evaluations in parallel");

        var categoryOption = new Option<string?>(
            "--category",
            "Filter evaluations by category");

        var verboseOption = new Option<bool>(
            "--verbose",
            () => false,
            "Enable verbose logging");

        rootCommand.AddOption(configOption);
        rootCommand.AddOption(evaluationsOption);
        rootCommand.AddOption(serverOption);
        rootCommand.AddOption(modelOption);
        rootCommand.AddOption(outputOption);
        rootCommand.AddOption(parallelOption);
        rootCommand.AddOption(categoryOption);
        rootCommand.AddOption(verboseOption);

        rootCommand.SetHandler(async (
            FileInfo? configFile,
            FileInfo evaluationsFile,
            string? serverPath,
            string model,
            DirectoryInfo output,
            bool parallel,
            string? category,
            bool verbose) =>
        {
            var config = await LoadConfigurationAsync(configFile, evaluationsFile, serverPath, model, output, parallel, category);
            var exitCode = await RunEvaluationsAsync(config, verbose);
            Environment.Exit(exitCode);
        }, configOption, evaluationsOption, serverOption, modelOption, outputOption, parallelOption, categoryOption, verboseOption);

        return await rootCommand.InvokeAsync(args);
    }

    private static async Task<EvaluationConfig> LoadConfigurationAsync(
        FileInfo? configFile,
        FileInfo evaluationsFile,
        string? serverPath,
        string model,
        DirectoryInfo output,
        bool parallel,
        string? category)
    {
        EvaluationConfig? config = null;

        // Load from config file if provided
        if (configFile?.Exists == true)
        {
            var configJson = await File.ReadAllTextAsync(configFile.FullName);
            config = JsonSerializer.Deserialize<EvaluationConfig>(configJson, JsonOptions);
        }

        // Override with command line options
        return new EvaluationConfig
        {
            McpServerPath = serverPath ?? config?.McpServerPath ?? "dotnet run",
            McpServerArgs = config?.McpServerArgs ?? [],
            EvaluationCasesPath = evaluationsFile.FullName,
            ScoringModel = model,
            OpenAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? config?.OpenAiApiKey,
            AzureOpenAiEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? config?.AzureOpenAiEndpoint,
            AzureOpenAiApiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY") ?? config?.AzureOpenAiApiKey,
            OutputDirectory = output.FullName,
            Parallel = parallel,
            MaxParallelism = config?.MaxParallelism ?? Environment.ProcessorCount,
            TimeoutSeconds = config?.TimeoutSeconds ?? 30,
            CategoryFilter = category ?? config?.CategoryFilter
        };
    }

    private static async Task<int> RunEvaluationsAsync(EvaluationConfig config, bool verbose)
    {
        try
        {
            var host = CreateHost(config, verbose);
            await using var scope = host.Services.CreateAsyncScope();

            var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("AzureMcp.Evals");
            var evaluationService = scope.ServiceProvider.GetRequiredService<IMcpEvaluationService>();

            // Load evaluation cases
            var evaluationCases = await LoadEvaluationCasesAsync(config.EvaluationCasesPath, config.CategoryFilter);
            logger.LogInformation("Loaded {Count} evaluation cases", evaluationCases.Count);

            if (evaluationCases.Count == 0)
            {
                logger.LogWarning("No evaluation cases found");
                return 1;
            }

            // Run evaluations
            var results = await evaluationService.RunEvaluationsAsync(evaluationCases);

            // Save results
            await SaveResultsAsync(results, config.OutputDirectory, logger);

            // Print summary
            PrintSummary(results, logger);

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            if (verbose)
            {
                Console.Error.WriteLine(ex.StackTrace);
            }
            return 1;
        }
    }

    private static IHost CreateHost(EvaluationConfig config, bool verbose)
    {
        return Host.CreateDefaultBuilder()
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(verbose ? LogLevel.Debug : LogLevel.Information);
            })
            .ConfigureServices(services =>
            {
                services.AddSingleton(config);
                services.AddHttpClient<ILlmScoringService, OpenAiScoringService>();
                services.AddTransient<IMcpEvaluationService, McpEvaluationService>();
            })
            .Build();
    }

    private static async Task<IReadOnlyList<EvaluationCase>> LoadEvaluationCasesAsync(string filePath, string? categoryFilter)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Evaluation cases file not found: {filePath}");
        }

        IReadOnlyList<EvaluationCase> cases;

        // Check if the file is the e2eTestPrompts.md file
        if (filePath.EndsWith("e2eTestPrompts.md", StringComparison.OrdinalIgnoreCase))
        {
            // Parse from markdown
            cases = await E2eTestPromptsParser.ParseFromFileAsync(filePath);
        }
        else
        {
            // Parse as JSON
            var json = await File.ReadAllTextAsync(filePath);
            cases = JsonSerializer.Deserialize<EvaluationCase[]>(json, JsonOptions) ?? [];
        }

        // Apply category filter if specified
        if (!string.IsNullOrEmpty(categoryFilter))
        {
            cases = cases.Where(c => string.Equals(c.Category, categoryFilter, StringComparison.OrdinalIgnoreCase)).ToArray();
        }

        return cases;
    }

    private static async Task SaveResultsAsync(IReadOnlyList<EvaluationResult> results, string outputDirectory, ILogger logger)
    {
        Directory.CreateDirectory(outputDirectory);

        var timestamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH-mm-ss");
        var resultFile = Path.Combine(outputDirectory, $"evaluation-results-{timestamp}.json");

        var json = JsonSerializer.Serialize(results, JsonOptions);
        await File.WriteAllTextAsync(resultFile, json);

        logger.LogInformation("Results saved to: {ResultFile}", resultFile);

        // Also save a summary
        var summary = new
        {
            Timestamp = DateTimeOffset.UtcNow,
            TotalEvaluations = results.Count,
            SuccessfulEvaluations = results.Count(r => r.Success),
            FailedEvaluations = results.Count(r => !r.Success),
            AverageScores = new
            {
                Accuracy = results.Where(r => r.Success).Average(r => r.Score.Accuracy),
                Completeness = results.Where(r => r.Success).Average(r => r.Score.Completeness),
                Relevance = results.Where(r => r.Success).Average(r => r.Score.Relevance),
                Clarity = results.Where(r => r.Success).Average(r => r.Score.Clarity),
                Reasoning = results.Where(r => r.Success).Average(r => r.Score.Reasoning),
                Overall = results.Where(r => r.Success).Average(r => r.Score.Overall)
            },
            TotalDurationMs = results.Sum(r => r.DurationMs),
            Results = results
        };

        var summaryFile = Path.Combine(outputDirectory, $"evaluation-summary-{timestamp}.json");
        var summaryJson = JsonSerializer.Serialize(summary, JsonOptions);
        await File.WriteAllTextAsync(summaryFile, summaryJson);

        logger.LogInformation("Summary saved to: {SummaryFile}", summaryFile);
    }

    private static void PrintSummary(IReadOnlyList<EvaluationResult> results, ILogger logger)
    {
        var successful = results.Where(r => r.Success).ToList();
        var failed = results.Where(r => !r.Success).ToList();

        logger.LogInformation("=== EVALUATION SUMMARY ===");
        logger.LogInformation("Total Evaluations: {Total}", results.Count);
        logger.LogInformation("Successful: {Successful}", successful.Count);
        logger.LogInformation("Failed: {Failed}", failed.Count);

        if (successful.Any())
        {
            logger.LogInformation("Average Scores:");
            logger.LogInformation("  Accuracy: {Accuracy:F2}", successful.Average(r => r.Score.Accuracy));
            logger.LogInformation("  Completeness: {Completeness:F2}", successful.Average(r => r.Score.Completeness));
            logger.LogInformation("  Relevance: {Relevance:F2}", successful.Average(r => r.Score.Relevance));
            logger.LogInformation("  Clarity: {Clarity:F2}", successful.Average(r => r.Score.Clarity));
            logger.LogInformation("  Reasoning: {Reasoning:F2}", successful.Average(r => r.Score.Reasoning));
            logger.LogInformation("  Overall: {Overall:F2}", successful.Average(r => r.Score.Overall));
        }

        logger.LogInformation("Total Duration: {Duration}ms", results.Sum(r => r.DurationMs));
    }
}
