namespace Pharos.Server.SystemUpdate.Application.Interfaces;

internal interface IUpdateOutcomeEvaluator
{
    /// <summary>
    /// Evaluates the outcomes of pending system updates and marks them as successful or failed based on their execution results.
    /// </summary>
    Task EvaluateUpdateOutcome(CancellationToken cancellationToken = default);
}