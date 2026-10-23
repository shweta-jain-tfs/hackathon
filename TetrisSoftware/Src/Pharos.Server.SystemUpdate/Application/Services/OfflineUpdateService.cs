using System.Globalization;
using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using Pharos.Server.Application;
using Pharos.Server.Infrastructure;
using Pharos.Server.Infrastructure.Process;
using Pharos.Server.SystemUpdate.Application.Interfaces;
using Pharos.Server.SystemUpdate.Domain;

namespace Pharos.Server.SystemUpdate.Application.Services;

internal class OfflineUpdateService : IUpdateService
{
    private readonly IFileSystem _fs;
    private readonly ILogger<OfflineUpdateService> _logger;
    private readonly IDateTime _dateTime;
    private readonly IProcessRunner _processRunner;
    private readonly IUpdatePackageAccessor _packageAccessor;
    private readonly ISystemUpdateDbContext _dbContext;
    private readonly IPharosVersionService _versionService;
    private readonly IUpdateMediumStateStore _updateMedium;

    private const string _logFolder = @"C:\ProgramData\Thermo Fisher Scientific\Pharos\InstallationLogs";

    public OfflineUpdateService(
        IFileSystem fs,
        ILogger<OfflineUpdateService> logger,
        IDateTime dateTime,
        IProcessRunner processRunner,
        IUpdatePackageAccessor packageAccessor,
        ISystemUpdateDbContext dbContext,
        IPharosVersionService versionService,
        IUpdateMediumStateStore updateMedium)
    {
        _fs = fs;
        _logger = logger;
        _dateTime = dateTime;
        _processRunner = processRunner;
        _packageAccessor = packageAccessor;
        _dbContext = dbContext;
        _versionService = versionService;
        _updateMedium = updateMedium;
    }

    public List<string> GetAvailableUpdates(CancellationToken cancellationToken = default)
    {
        if (!IsMediumRegistered())
        {
            throw new InvalidOperationException("No update medium is currently registered.");
        }

        List<string> updates;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            updates = _packageAccessor.EnumerateContents(_updateMedium.Get()?.DriveName!, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while enumerating updates (drive {DriveName}).", _updateMedium.Get()!.DriveName);
            throw;
        }

        return updates;
    }

    public async Task InitiateUpdate(CancellationToken cancellationToken = default)
    {
        if (!IsMediumRegistered())
        {
            throw new InvalidOperationException("No update medium is currently registered.");
        }

        if (GetAvailableUpdates(cancellationToken).Count <= 0)
        {
            _logger.LogInformation("No updates available on drive {DriveName}.", _updateMedium.Get()!.DriveName);
            return;
        }

        var previousVersion = _versionService.Version;

        var pending = new SystemUpdateSession
        {
            Source = UpdateSource.Usb,
            UsbDrive = _updateMedium.Get()!.DriveName,
            PreviousVersion = previousVersion,
            StartedAtUtc = _dateTime.UtcNow,
            Status = UpdateStatus.Pending
        };

        await _dbContext.SystemUpdateSessions.AddAsync(pending, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Recorded pending update with previous version {Prev}", previousVersion);

        cancellationToken.ThrowIfCancellationRequested();

        var tempPath = _packageAccessor.StageToDirectory(_updateMedium.Get()?.DriveName!, cancellationToken);

        var candidates = _packageAccessor.FindUpdateCandidates(tempPath, cancellationToken);

        var installerPath = candidates.FirstOrDefault(f => Path.GetExtension(f).Equals(".msi", StringComparison.OrdinalIgnoreCase));
        if (installerPath == null)
        {
            throw new UpdateFailedException($"No MSI installer found in staged update at '{tempPath}'.");
        }

        var installerExt = Path.GetExtension(installerPath).ToUpperInvariant();
        _logger.LogInformation("Starting installer {InstallerPath} (type {Ext}).", installerPath, installerExt);

        cancellationToken.ThrowIfCancellationRequested();

        // Ensure log folder exists
        if (!_fs.Directory.Exists(_logFolder))
        {
            _fs.Directory.CreateDirectory(_logFolder);
        }

        var timestamp = _dateTime.Now.ToLocalTime().ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        var logPath = Path.Combine(_logFolder, $"Pharos-install-{timestamp}.log");

        using var started = StartWindowsInstaller(installerPath, logPath);

        if (started is null)
        {
            throw new UpdateFailedException($"Failed to start installer process for '{installerPath}'.");
        }

        _logger.LogInformation("Installer process started (PID {Pid}) for {InstallerPath}. Log: {LogPath}", started.Id, installerPath, logPath);

        await started.WaitForExitAsync(cancellationToken);

        if (started.ExitCode != 0)
        {
            throw new UpdateFailedException($"Installer process (PID {started.Id}) for {installerPath} exited with code {started.ExitCode}. See log: {logPath}");
        }
    }

    private IStartedProcess? StartWindowsInstaller(string installerPath, string logPath)
    {
        var args = $"/i \"{installerPath}\" /quiet /log \"{logPath}\"";

        return _processRunner.Start("msiexec.exe", args);
    }

    private bool IsMediumRegistered()
    {
        return !string.IsNullOrWhiteSpace(_updateMedium.Get()?.DriveName);
    }
}
