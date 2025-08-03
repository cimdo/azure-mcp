using System.Text.Json.Serialization;

namespace AzureMcp.Evals.Models;

/// <summary>
/// Represents an evaluation test case for MCP tools
/// </summary>
public record EvaluationCase
{
    /// <summary>
    /// Unique identifier for the evaluation case
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    /// Name of the evaluation case
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Description of what this evaluation tests
    /// </summary>
    [JsonPropertyName("description")]
    public required string Description { get; init; }

    /// <summary>
    /// The prompt/query to send to the MCP server
    /// </summary>
    [JsonPropertyName("prompt")]
    public required string Prompt { get; init; }

    /// <summary>
    /// Expected tool name that should be invoked (optional)
    /// </summary>
    [JsonPropertyName("expectedTool")]
    public string? ExpectedTool { get; init; }

    /// <summary>
    /// Expected behavior or outcome (for scoring reference)
    /// </summary>
    [JsonPropertyName("expectedBehavior")]
    public string? ExpectedBehavior { get; init; }

    /// <summary>
    /// Category/area this evaluation belongs to (e.g., "storage", "compute")
    /// </summary>
    [JsonPropertyName("category")]
    public string? Category { get; init; }

    /// <summary>
    /// Tags for organizing evaluations
    /// </summary>
    [JsonPropertyName("tags")]
    public IReadOnlyList<string> Tags { get; init; } = [];
}
