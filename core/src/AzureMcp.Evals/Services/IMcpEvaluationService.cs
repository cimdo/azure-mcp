using AzureMcp.Evals.Models;

namespace AzureMcp.Evals.Services;

/// <summary>
/// Service for running MCP evaluations
/// </summary>
public interface IMcpEvaluationService
{
    /// <summary>
    /// Run a single evaluation case
    /// </summary>
    /// <param name="evaluationCase">The evaluation case to run</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The evaluation result</returns>
    Task<EvaluationResult> RunEvaluationAsync(
        EvaluationCase evaluationCase, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Run multiple evaluation cases
    /// </summary>
    /// <param name="evaluationCases">The evaluation cases to run</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The evaluation results</returns>
    Task<IReadOnlyList<EvaluationResult>> RunEvaluationsAsync(
        IEnumerable<EvaluationCase> evaluationCases,
        CancellationToken cancellationToken = default);
}
