using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pharos.Server.Application;
using Pharos.Server.Infrastructure.Testing;
using Pharos.Server.Parameters.Application.Commands;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Events;

internal sealed class ApplicationStartedEventHandler(
    IParametersDbContext dbContext,
    ISender sender,
    IHostEnvironment hostEnvironment,
    ILogger<ApplicationStartedEventHandler> logger) : INotificationHandler<ApplicationStartedEvent>
{
    private const string _packageUpdateFolderPath = @"Thermo Fisher Scientific\Pharos\PUP";
    private const string _packageUpdateFileMask = "ParameterUpdatePackage*.zip";

    public async Task Handle(ApplicationStartedEvent notification, CancellationToken cancellationToken)
    {
        if (TestEnvironment.IsXUnitDetected())
        {
            return;
        }

        try
        {
            await ImportParameterUpdatePackage(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Parameter update package import failed during application startup.");
        }
    }

    private async Task ImportParameterUpdatePackage(CancellationToken cancellationToken)
    {
        var hasMethods = await dbContext.Methods.AnyAsync(cancellationToken).ConfigureAwait(false);
        var hasPanels = await dbContext.Panels.AnyAsync(cancellationToken).ConfigureAwait(false);
        var hasArticles = await dbContext.Articles.AnyAsync(cancellationToken).ConfigureAwait(false);

        if (hasMethods && hasPanels && hasArticles)
        {
            return;
        }

        var directoryInfo = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            _packageUpdateFolderPath));
        if (!directoryInfo.Exists)
        {
            LogMissingPackage(directoryInfo);
            return;
        }

        var packageUpdateFiles = directoryInfo.GetFiles(_packageUpdateFileMask);
        if (packageUpdateFiles.Length == 0)
        {
            LogMissingPackage(directoryInfo);
            return;
        }

        if (packageUpdateFiles.Length > 1)
        {
            logger.LogWarning("Parameter update package import failed: multiple files matching {PackageUpdateFileMask} were found in {Directory}.",
                _packageUpdateFileMask, directoryInfo.FullName);
            return;
        }

        var packageUpdateFile = packageUpdateFiles[0].FullName;

        logger.LogInformation("Importing parameter update package file {PackageFile}.", packageUpdateFile);

        var success = await sender.Send(new ImportMethodUpdatePackageCommand(packageUpdateFile, null), cancellationToken);
        if (!success)
        {
            logger.LogWarning("Parameter update package import failed: {PackageFile}.", packageUpdateFile);
        }
    }

    private void LogMissingPackage(DirectoryInfo directoryInfo)
    {
        if (hostEnvironment.IsDevelopment())
        {
            var devSharedFilesFolder = "https://thermofisher.sharepoint.com/:f:/r/sites/TetrisUppsalaSoftware/Shared%20Documents/General/10.%20Shared%20files?csf=1&web=1&e=ifIJR5";

            logger.LogError(@"Parameter update package import failed: package file not found.
To resolve the problem:
1. Open the shared folder {PackageDownloadFolder}
2. Download a development parameter update package file: {PackageUpdateFileMask}
3. Put the file to {Directory}
4. Restart the application", devSharedFilesFolder, _packageUpdateFileMask, directoryInfo.FullName);
            return;
        }

        logger.LogError("Parameter update package import failed: no file matching {PackageUpdateFileMask} was found in {Directory}.",
            _packageUpdateFileMask, directoryInfo.FullName);
    }
}
