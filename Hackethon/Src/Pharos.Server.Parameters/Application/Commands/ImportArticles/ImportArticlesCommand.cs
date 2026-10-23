using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Commands.ImportArticles;

internal record ImportArticlesCommand(IMethodUpdatePackage Package) : IRequest<bool>;

internal class ImportArticlesCommandHandler(
    ArticleParametersHandler articleHandler,
    UpdateArticleParameterHandler updateArticleParameterHandler,
    ILogger<ImportArticlesCommandHandler> logger) : IRequestHandler<ImportArticlesCommand, bool>
{

    public async Task<bool> Handle(ImportArticlesCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Importing articles from the package. Package: {PackageName}", request.Package.Name);

        var articleParameters = new ArticleParameters();
        var fileNames = request.Package.GetFileNames();

        foreach (var fileName in fileNames)
        {
            var extension = Path.GetExtension(fileName);
            if (extension.Equals(".arf", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogInformation("Parsing articles file: {ParameterFile}", fileName);
                var fileContent = await request.Package.GetFileContent(fileName);
                var parseResult = await articleHandler.Handle(fileContent, fileName);
                if (articleParameters.Version != 0 && articleParameters.Version != parseResult.Version)
                {
                    throw new ValidationException("The package contains multiple article files with different versions.");
                }
                articleParameters.Version = parseResult.Version;
                articleParameters.Articles.AddRange(parseResult.Articles);
                articleParameters.Markets.AddRange(parseResult.Markets);
                articleParameters.Packages.AddRange(parseResult.Packages);
            }
        }

        if (articleParameters.Articles.Count == 0)
        {
            logger.LogWarning("The package {PackageName} does not contain article definitions", request.Package.Name);
            return false;
        }

        logger.LogInformation("Updating articles and packages in the database");
        await updateArticleParameterHandler.Handle(articleParameters);
        logger.LogInformation("Successfully imported articles from the package. Package: {PackageName}", request.Package.Name);

        return true;
    }
}
