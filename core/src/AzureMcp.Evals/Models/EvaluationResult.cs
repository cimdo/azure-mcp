using System.Text.Json.Serialization;

namespace AzureMcp.Evals.Models;

/// <summary>
/// Represents the result of running an evaluation case
/// </summary>
public record EvaluationResult
{
    /// <summary>
    /// The evaluation case that was run
    /// </summary>
    [JsonPropertyName("evaluationCase")]
    public required EvaluationCase EvaluationCase { get; init; }

    /// <summary>
    /// The actual response received from the MCP server
    /// </summary>
    [JsonPropertyName("actualResponse")]
    public required string ActualResponse { get; init; }

    /// <summary>
    /// The tool that was actually invoked (if any)
    /// </summary>
    [JsonPropertyName("actualTool")]
    public string? ActualTool { get; init; }

    /// <summary>
    /// The scores assigned by the LLM evaluator
    /// </summary>
    [JsonPropertyName("score")]
    public required EvaluationScore Score { get; init; }

    /// <summary>
    /// Duration of the evaluation in milliseconds
    /// </summary>
    [JsonPropertyName("durationMs")]
    public long DurationMs { get; init; }

    /// <summary>
    /// Timestamp when the evaluation was run
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Whether the evaluation was successful (no errors)
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    /// <summary>
    /// Error message if the evaluation failed
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; init; }
}
