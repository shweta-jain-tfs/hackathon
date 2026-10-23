using MediatR;

namespace Pharos.Server.Application.Lis;

/// <summary>
/// Command to remove the scheduled missing test orders job.
/// </summary>
/// <param name="SampleIds">The collection of sample IDs for which the missing test orders job should be removed.</param>
public sealed record RemoveMissingTestOrdersJobCommand(IReadOnlyCollection<string> SampleIds) : IRequest;