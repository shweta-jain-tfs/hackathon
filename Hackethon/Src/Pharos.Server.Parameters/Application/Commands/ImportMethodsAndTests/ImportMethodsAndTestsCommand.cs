using MediatR;
using Microsoft.Extensions.Logging;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Commands.ImportMethodsAndTests;

internal record ImportMethodsAndTestsCommand(IMethodUpdatePackage Package) : IRequest<bool>;

internal class ImportMethodsAndTestsCommandHandler(
    MethodParametersHandler methodHandler,
    MethodTestParametersHandler methodTestHandler,
    UpdateMethodParameterHandler updateMethodParameterHandler,
    ILogger<ImportMethodsAndTestsCommandHandler> logger) : IRequestHandler<ImportMethodsAndTestsCommand, bool>
{

    public async Task<bool> Handle(ImportMethodsAndTestsCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Importing methods and tests from the package. Package: {PackageName}", request.Package.Name);

        var importedMethods = new List<Method>();
        var importedMethodTests = new List<MethodTest>();
        var fileNames = request.Package.GetFileNames();

        foreach (var fileName in fileNames)
        {
            var extension = Path.GetExtension(fileName);
            if (extension.Equals(".mth", StringComparison.OrdinalIgnoreCase) || extension.Equals(".tst", StringComparison.OrdinalIgnoreCase))
            {
                var methodName = Path.GetFileNameWithoutExtension(fileName);
                var fileContent = await request.Package.GetFileContent(fileName);

                if (extension.Equals(".mth", StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogInformation("Parsing method file: {ParameterFile}", fileName);
                    var importedMethod = await methodHandler.Handle(fileContent, fileName);
                    importedMethods.Add(importedMethod);

                    if (fileNames.All(x => !x.Equals($"{methodName}.tst", StringComparison.OrdinalIgnoreCase)))
                    {
                        logger.LogWarning("Tests for the method {MethodName} are missing from the package.", methodName);
                    }
                }

                if (extension.Equals(".tst", StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogInformation("Parsing method test file: {ParameterFile}", fileName);
                    var importedTests = await methodTestHandler.Handle(fileContent, fileName);

                    importedMethodTests.AddRange(importedTests);

                    if (fileNames.All(x => !x.Equals($"{methodName}.mth", StringComparison.OrdinalIgnoreCase)))
                    {
                        logger.LogWarning("Tests for the method {MethodName} are present in the package but the method file is missing.", methodName);
                    }
                }
            }
        }

        if (importedMethods.Count > 0 || importedMethodTests.Count > 0)
        {
            await updateMethodParameterHandler.Handle(importedMethods, importedMethodTests);
            return true;
        }

        return false;
    }
}
