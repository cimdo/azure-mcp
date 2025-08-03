using AzureMcp.Evals.Models;

namespace AzureMcp.Evals.Services;

/// <summary>
/// Service for scoring evaluation results using LLMs
/// </summary>
public interface ILlmScoringService
{
    /// <summary>
    /// Score an evaluation result using the configured LLM
    /// </summary>
    /// <param name="evaluationCase">The evaluation case</param>
    /// <param name="actualResponse">The actual response from the MCP server</param>
    /// <param name="actualTool">The tool that was actually invoked</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The evaluation score</returns>
    Task<EvaluationScore> ScoreAsync(
        EvaluationCase evaluationCase,
        string actualResponse,
        string? actualTool,
        CancellationToken cancellationToken = default);
}
