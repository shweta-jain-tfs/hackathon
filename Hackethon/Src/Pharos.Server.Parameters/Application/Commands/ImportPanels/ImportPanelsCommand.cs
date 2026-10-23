using MediatR;
using Microsoft.Extensions.Logging;
using Pharos.Server.Domain.Parameters;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Commands.ImportPanels;

internal record ImportPanelsCommand(IMethodUpdatePackage Package) : IRequest<(bool Success, Exception? Error)>;

internal class ImportPanelsCommandHandler(
    PanelParametersHandler testPanelHandler,
    UpdatePanelParametersHandler updatePanelParameterHandler,
    ILogger<ImportPanelsCommandHandler> logger) : IRequestHandler<ImportPanelsCommand, (bool Success, Exception? Error)>
{

    public async Task<(bool Success, Exception? Error)> Handle(ImportPanelsCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Importing test panels from the package. Package: {PackageName}", request.Package.Name);

        var importedPanels = new List<Panel>();
        var fileNames = request.Package.GetFileNames();

        foreach (var fileName in fileNames)
        {
            var extension = Path.GetExtension(fileName);
            if (extension.Equals(".tpf", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    logger.LogInformation("Parsing panels file: {ParameterFile}", fileName);
                    var fileContent = await request.Package.GetFileContent(fileName);
                    var parseResult = await testPanelHandler.Handle(fileContent, fileName);
                    importedPanels.AddRange(parseResult);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Error processing parameter file");
                    return (false, e);
                }
            }
        }

        if (importedPanels.Count == 0)
        {
            logger.LogWarning("The package {PackageName} does not contain test panels", request.Package.Name);
        }

        try
        {
            logger.LogInformation("Updating test panels in the database");
            await updatePanelParameterHandler.Handle(importedPanels);
            logger.LogInformation("Successfully imported test panels from the package. Package: {PackageName}", request.Package.Name);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error updating articles and packages in the database");
            return (false, e);
        }

        return (true, null);
    }
}
