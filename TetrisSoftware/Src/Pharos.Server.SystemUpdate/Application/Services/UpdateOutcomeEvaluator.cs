using Microsoft.Extensions.Logging;
using Pharos.Server.Application;
using Pharos.Server.SystemUpdate.Application.Interfaces;
using Pharos.Server.SystemUpdate.Domain;

namespace Pharos.Server.SystemUpdate.Application.Services;

internal sealed class UpdateOutcomeEvaluator : IUpdateOutcomeEvaluator
{
    private readonly ISystemUpdateDbContext _dbContext;
    private readonly IPharosVersionService _versionProvider;
    private readonly ILogger<UpdateOutcomeEvaluator> _logger;

    public UpdateOutcomeEvaluator(
        ISystemUpdateDbContext dbContext,
        IPharosVersionService versionProvider,
        ILogger<UpdateOutcomeEvaluator> logger)
    {
        _dbContext = dbContext;
        _versionProvider = versionProvider;
        _logger = logger;
    }

    public async Task EvaluateUpdateOutcome(CancellationToken cancellationToken = default)
    {
        var pending = await _dbContext.GetLatestPendingAsync(cancellationToken);
        if (pending is null)
        {
            _logger.LogInformation("No pending system updates found.");
            return;
        }

        _logger.LogInformation("Found pending system update with previous version {PreviousVersion}. Evaluating outcome...", pending.PreviousVersion);

        // Currently system version equals Pharos application version. This may change when we have more complex update scenarios, and we may need to track system version separately.
        var currentVersion = _versionProvider.Version;

        var success = VersionGreaterThan(currentVersion, pending.PreviousVersion);
        if (success)
        {
            pending.Status = UpdateStatus.Successful;

            _logger.LogInformation("System update to version {Version} succeeded.", currentVersion);
        }
        else
        {
            pending.Status = UpdateStatus.Failed;

            _logger.LogWarning("System update to version {Version} failed.", currentVersion);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static bool VersionGreaterThan(string a, string b)
    {
        if (Version.TryParse(a, out var va) && Version.TryParse(b, out var vb))
        {
            return va > vb;
        }

        return !string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    }
}