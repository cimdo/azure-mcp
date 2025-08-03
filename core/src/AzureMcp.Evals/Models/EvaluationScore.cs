using System.Text.Json;
using System.Text.Json.Serialization;

namespace AzureMcp.Evals.Models;

/// <summary>
/// Represents the scoring criteria for evaluating MCP tool responses
/// </summary>
public record EvaluationScore
{
    /// <summary>
    /// How correct is the information provided? (1-5)
    /// </summary>
    [JsonPropertyName("accuracy")]
    public int Accuracy { get; init; }

    /// <summary>
    /// Does it provide all necessary information? (1-5)
    /// </summary>
    [JsonPropertyName("completeness")]
    public int Completeness { get; init; }

    /// <summary>
    /// Is the response appropriate for the query? (1-5)
    /// </summary>
    [JsonPropertyName("relevance")]
    public int Relevance { get; init; }

    /// <summary>
    /// Is the information presented clearly? (1-5)
    /// </summary>
    [JsonPropertyName("clarity")]
    public int Clarity { get; init; }

    /// <summary>
    /// Does the model show sound reasoning in its use of tools? (1-5)
    /// </summary>
    [JsonPropertyName("reasoning")]
    public int Reasoning { get; init; }

    /// <summary>
    /// Overall score calculated as average of all metrics
    /// </summary>
    [JsonPropertyName("overall")]
    public double Overall => (Accuracy + Completeness + Relevance + Clarity + Reasoning) / 5.0;

    /// <summary>
    /// Detailed explanation of the scores
    /// </summary>
    [JsonPropertyName("explanation")]
    public string? Explanation { get; init; }
}
