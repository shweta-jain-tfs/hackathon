using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pharos.Server.Settings.Application.Commands.NotifySettingsChanged;
using Pharos.Server.Settings.Application.Interfaces;
using Pharos.Server.Settings.Application.Validation;

namespace Pharos.Server.Settings.Application.Commands.CopySettings;

public record CopySettingsCommand(string Key, string RootPageLabel) : IRequest<CopySettingsResult>;

internal sealed class CopySettingsCommandHandler(ISettingsDbContext dbContext,
    ILogger<CopySettingsCommandHandler> logger, ISettingsPagesLoader loader,
    ISettingsCopy copyFunction, IPageListMaxItemsValidator validator, ISender sender) : IRequestHandler<CopySettingsCommand, CopySettingsResult>
{
    public async Task<CopySettingsResult> Handle(CopySettingsCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Request to copy settings for page: {Entry}", request.Key);

        var populatedPages = await loader.LoadAsync(request.RootPageLabel, cancellationToken)
            .ConfigureAwait(false);

        // Find the PageList key that contains this source page
        var pageListKey = copyFunction.FindPageListKeyForSourcePage(populatedPages, request.Key);
        if (pageListKey == null)
        {
            logger.LogWarning("Could not find parent PageList for source page: {SourceKey}", request.Key);
            return new CopySettingsResult(false, CopySettingsErrorCode.ParentPageListNotFound);
        }

        // Validate if we can add more items using the shared validator
        var validationResult = await validator.ValidateCanAddAsync(pageListKey, request.RootPageLabel, cancellationToken)
            .ConfigureAwait(false);

        if (!validationResult.IsValid)
        {
            logger.LogWarning("Cannot copy page '{Key}': {ErrorMessage}", request.Key, validationResult.ErrorMessage);
            return new CopySettingsResult(false, CopySettingsErrorCode.MaxItemsReached, IsWarning: true, MaxItemsLimit: validationResult.MaxItems);
        }

        var existingKeys = await dbContext.Settings
            .Select(s => s.Key)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var existingSet = existingKeys.ToHashSet(StringComparer.Ordinal);
        bool KeyExists(string key) => existingSet.Contains(key);

        // Use the count from the validation result
        logger.LogInformation("Counted {Count} existing items for copy operation of page '{Key}'", validationResult.CurrentCount, request.Key);

        var copyResult = copyFunction.CreateCopy(populatedPages, request.Key, KeyExists, validationResult.CurrentCount);

        if (!copyResult.Success)
        {
            logger.LogWarning("Copy operation failed for page '{Key}'. Reason: {Reason}",
                request.Key, copyResult.ErrorCode?.ToString() ?? "Unknown");
            return new CopySettingsResult(false, copyResult.ErrorCode, copyResult.IsWarning);
        }

        logger.LogInformation("Copy operation succeeded for page '{Key}'. Created new page: {NewPageKey}",
            request.Key, copyResult.NewPage?.Key ?? "N/A");

        foreach (var setting in copyResult.SettingsToPersist)
        {
            dbContext.Settings.Add(setting);
        }

        if (copyResult.SettingsToPersist.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await sender.Send(
                    new NotifySettingsChangedCommand(
                        UpdatedKeys: copyResult.SettingsToPersist.Select(setting => setting.Key).ToList()),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        return new CopySettingsResult(true, null);
    }
}

public record CopySettingsResult(bool Success, CopySettingsErrorCode? ErrorCode = null, bool IsWarning = false, int? MaxItemsLimit = null);

public enum CopySettingsErrorCode
{
    None = 0,
    ParentPageListNotFound = 1,
    MaxItemsReached = 2,
    CopyDisabled = 3,
    SourcePageNotFound = 4,
    NameTooLong = 5,
    UnknownError = 6
}
