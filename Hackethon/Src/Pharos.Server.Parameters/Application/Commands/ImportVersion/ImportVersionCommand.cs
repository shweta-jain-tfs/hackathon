using MediatR;
using Microsoft.Extensions.Logging;
using Pharos.Server.Parameters.Application.Interfaces;
using Pharos.Server.Parameters.Domain;

namespace Pharos.Server.Parameters.Application.Commands.ImportVersion;

internal record ImportVersionCommand(IMethodUpdatePackage Package) : IRequest;

internal class ImportVersionCommandHandler(
    VersionParametersHandler versionHandler,
    UpdateVersionCommandHandler updateVersionCommand,
    ILogger<ImportVersionCommand> logger) : IRequestHandler<ImportVersionCommand>
{
    private const string _fileName = "version.txt";

    public async Task Handle(ImportVersionCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Importing version from the package. Package: {PackageName}", request.Package.Name);
        var versionParameters = new ParameterUpdatePackageVersion();

        logger.LogInformation("Parsing version file: {ParameterFile}", _fileName);
        var versionFileContent = await request.Package.GetFileContent(_fileName);
        var parseResult = await versionHandler.Handle(versionFileContent, _fileName);
        versionParameters.VersionNumber = parseResult.VersionNumber;
        versionParameters.ImportDate = parseResult.ImportDate;

        logger.LogInformation("Updating version in the database");
        await updateVersionCommand.Handle(versionParameters);
        logger.LogInformation("Successfully imported version from the package. Package: {PackageName}", request.Package.Name);
    }
}