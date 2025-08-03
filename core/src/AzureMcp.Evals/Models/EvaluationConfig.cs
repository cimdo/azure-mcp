using System.Text.Json.Serialization;

namespace AzureMcp.Evals.Models;

/// <summary>
/// Configuration for the MCP evaluation runner
/// </summary>
public record EvaluationConfig
{
    /// <summary>
    /// Path to the MCP server executable or configuration
    /// </summary>
    [JsonPropertyName("mcpServerPath")]
    public required string McpServerPath { get; init; }

    /// <summary>
    /// Arguments to pass to the MCP server
    /// </summary>
    [JsonPropertyName("mcpServerArgs")]
    public IReadOnlyList<string> McpServerArgs { get; init; } = [];

    /// <summary>
    /// Path to the evaluation cases file
    /// </summary>
    [JsonPropertyName("evaluationCasesPath")]
    public required string EvaluationCasesPath { get; init; }

    /// <summary>
    /// LLM model to use for scoring (e.g., "gpt-4", "gpt-3.5-turbo")
    /// </summary>
    [JsonPropertyName("scoringModel")]
    public string ScoringModel { get; init; } = "gpt-4";

    /// <summary>
    /// OpenAI API key for LLM scoring
    /// </summary>
    [JsonPropertyName("openAiApiKey")]
    public string? OpenAiApiKey { get; init; }

    /// <summary>
    /// Azure OpenAI endpoint (alternative to OpenAI)
    /// </summary>
    [JsonPropertyName("azureOpenAiEndpoint")]
    public string? AzureOpenAiEndpoint { get; init; }

    /// <summary>
    /// Azure OpenAI API key
    /// </summary>
    [JsonPropertyName("azureOpenAiApiKey")]
    public string? AzureOpenAiApiKey { get; init; }

    /// <summary>
    /// Output directory for results
    /// </summary>
    [JsonPropertyName("outputDirectory")]
    public string OutputDirectory { get; init; } = "./results";

    /// <summary>
    /// Whether to run evaluations in parallel
    /// </summary>
    [JsonPropertyName("parallel")]
    public bool Parallel { get; init; } = true;

    /// <summary>
    /// Maximum number of parallel evaluations
    /// </summary>
    [JsonPropertyName("maxParallelism")]
    public int MaxParallelism { get; init; } = Environment.ProcessorCount;

    /// <summary>
    /// Timeout for each evaluation in seconds
    /// </summary>
    [JsonPropertyName("timeoutSeconds")]
    public int TimeoutSeconds { get; init; } = 30;

    /// <summary>
    /// Filter evaluations by category
    /// </summary>
    [JsonPropertyName("categoryFilter")]
    public string? CategoryFilter { get; init; }

    /// <summary>
    /// Filter evaluations by tags
    /// </summary>
    [JsonPropertyName("tagFilter")]
    public IReadOnlyList<string> TagFilter { get; init; } = [];
}
