using MediatR;
using Microsoft.Extensions.Logging;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Parameters.Application.Commands.ImportArticles;
using Pharos.Server.Parameters.Application.Commands.ImportMethodsAndTests;
using Pharos.Server.Parameters.Application.Commands.ImportPanels;
using Pharos.Server.Parameters.Application.Commands.ImportVersion;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Commands;

internal record ImportMethodUpdatePackageCommand(string MethodUpdatePackageFilePath, string? OriginalFileName) : IRequest<bool>;

internal class ImportMethodUpdatePackageCommandHandler(
    IMethodUpdatePackageFactory packageFactory,
    IParametersDbContext dbContext,
    ISender sender,
    IPublisher publisher,
    ILogger<ImportMethodUpdatePackageCommand> logger) : IRequestHandler<ImportMethodUpdatePackageCommand, bool>
{
    public async Task<bool> Handle(ImportMethodUpdatePackageCommand request, CancellationToken cancellationToken)
    {
        using var package = packageFactory.Create(request.MethodUpdatePackageFilePath, request.OriginalFileName);

        logger.LogInformation("Importing method update package {PackageName}", package.Name);

        if (string.IsNullOrEmpty(await package.GetDescription()))
        {
            logger.LogWarning("The method update package {PackageName} contains no description", package.Name);
        }

        await dbContext.BeginTransactionAsync(cancellationToken);

        try
        {
            await sender.Send(new ImportVersionCommand(package), cancellationToken);
            var articlesUpdated = await sender.Send(new ImportArticlesCommand(package), cancellationToken);
            var methodsUpdated = await sender.Send(new ImportMethodsAndTestsCommand(package), cancellationToken);
            await sender.Send(new ImportPanelsCommand(package), cancellationToken);
            await dbContext.CommitTransactionAsync(cancellationToken);

            logger.LogInformation("Successfully imported method update package {PackageName}", package.Name);

            // updating method and article information on all instruments takes a long time, so we do it in the background
            if (methodsUpdated)
            {
                await publisher.Publish(new MethodsUpdatedEvent(), cancellationToken);
            }

            if (articlesUpdated)
            {
                await publisher.Publish(new ArticlesUpdatedEvent(), cancellationToken);
            }

            return true;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error occured while importing method update package {PackageName}", package.Name);
            await dbContext.RollbackTransactionAsync(cancellationToken);
            return false;
        }
    }
}