using System.Diagnostics;
using System.Text.Json;
using AzureMcp.Evals.Models;
using Microsoft.Extensions.Logging;

namespace AzureMcp.Evals.Services;

/// <summary>
/// Service for running MCP evaluations against an MCP server
/// </summary>
public class McpEvaluationService(
    ILlmScoringService scoringService,
    EvaluationConfig config,
    ILogger<McpEvaluationService> logger) : IMcpEvaluationService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public async Task<EvaluationResult> RunEvaluationAsync(
        EvaluationCase evaluationCase,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            logger.LogInformation("Running evaluation: {EvaluationName}", evaluationCase.Name);

            // For now, we'll simulate the MCP interaction
            // In a full implementation, this would connect to an actual MCP server
            var (actualResponse, actualTool) = await SimulateMcpInteractionAsync(evaluationCase, cancellationToken);

            // Score the response using LLM
            var score = await scoringService.ScoreAsync(evaluationCase, actualResponse, actualTool, cancellationToken);

            stopwatch.Stop();

            return new EvaluationResult
            {
                EvaluationCase = evaluationCase,
                ActualResponse = actualResponse,
                ActualTool = actualTool,
                Score = score,
                DurationMs = stopwatch.ElapsedMilliseconds,
                Success = true
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "Failed to run evaluation: {EvaluationName}", evaluationCase.Name);

            return new EvaluationResult
            {
                EvaluationCase = evaluationCase,
                ActualResponse = string.Empty,
                Score = new EvaluationScore
                {
                    Accuracy = 1,
                    Completeness = 1,
                    Relevance = 1,
                    Clarity = 1,
                    Reasoning = 1,
                    Explanation = $"Evaluation failed: {ex.Message}"
                },
                DurationMs = stopwatch.ElapsedMilliseconds,
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async Task<IReadOnlyList<EvaluationResult>> RunEvaluationsAsync(
        IEnumerable<EvaluationCase> evaluationCases,
        CancellationToken cancellationToken = default)
    {
        var cases = evaluationCases.ToList();
        logger.LogInformation("Running {Count} evaluations", cases.Count);

        if (config.Parallel)
        {
            var semaphore = new SemaphoreSlim(config.MaxParallelism);
            var tasks = cases.Select(async evalCase =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    timeoutCts.CancelAfter(TimeSpan.FromSeconds(config.TimeoutSeconds));
                    return await RunEvaluationAsync(evalCase, timeoutCts.Token);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            return await Task.WhenAll(tasks);
        }
        else
        {
            var results = new List<EvaluationResult>();
            foreach (var evalCase in cases)
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(config.TimeoutSeconds));
                var result = await RunEvaluationAsync(evalCase, timeoutCts.Token);
                results.Add(result);
            }
            return results;
        }
    }

    private async Task<(string response, string? tool)> SimulateMcpInteractionAsync(
        EvaluationCase evaluationCase,
        CancellationToken cancellationToken)
    {
        // TODO: Implement actual MCP server interaction
        // For now, we'll simulate based on the prompt patterns
        
        await Task.Delay(100, cancellationToken); // Simulate processing time

        var prompt = evaluationCase.Prompt.ToLowerInvariant();
        
        // Simple pattern matching to simulate tool selection and responses
        if (prompt.Contains("list") && prompt.Contains("storage"))
        {
            return ("Found 3 storage accounts: storage1, storage2, storage3", "azmcp-storage-list");
        }
        
        if (prompt.Contains("create") && prompt.Contains("storage"))
        {
            return ("Successfully created storage account with name storageaccount123", "azmcp-storage-create");
        }
        
        if (prompt.Contains("list") && prompt.Contains("virtual machine"))
        {
            return ("Found 2 virtual machines: vm1 (Running), vm2 (Stopped)", "azmcp-extension-az");
        }
        
        if (prompt.Contains("cognitive search") || prompt.Contains("search service"))
        {
            return ("Found 1 Cognitive Search service: my-search-service (Standard tier)", "azmcp-search-list");
        }
        
        if (prompt.Contains("app configuration"))
        {
            return ("Found 2 App Configuration stores: config1, config2", "azmcp-appconfig-account-list");
        }
        
        // Default response for unrecognized patterns
        return ($"I understood your request: '{evaluationCase.Prompt}' but I don't have the specific tools to handle this yet.", null);
    }
}
