using Pharos.Server.Request.Application.Commands.RequestGenerator;

namespace Pharos.Server.Request.Application.Interfaces;

/// <summary>
/// Shared service that validates a list of items against the parameters
/// that the same DB-backed rules are enforced at preview time and at generation time.
/// </summary>
public interface IRequestGeneratorValidationService
{
    /// <summary>
    /// Validates full request generator input, including test definitions and generated ranges.
    /// </summary>
    /// <param name="request">The generator request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ValidateAsync(
        GenerateRequestsDto request,
        CancellationToken cancellationToken);
}
