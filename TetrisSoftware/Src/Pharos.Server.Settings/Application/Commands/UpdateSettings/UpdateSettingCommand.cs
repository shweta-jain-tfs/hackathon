using System.Globalization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pharos.Server.Application.AuditTrail.Events;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Localization;
using Pharos.Server.Settings.Application.Commands.NotifySettingsChanged;
using Pharos.Server.Settings.Application.Interfaces;
using Pharos.Server.Settings.Application.Metadata;
using Pharos.Server.Settings.Application.Services;
using Pharos.Server.Settings.Application.Validation;
using Pharos.Server.Settings.Domain;
using Pharos.Server.Settings.Infrastructure.ConfigurationProvider;

namespace Pharos.Server.Settings.Application.Commands.UpdateSettings;

public record UpdateSettingCommand(
    string? Value,
    Page? SettingToSave,
    bool? IsPageListUpdate = null) : IRequest<SettingsUpdateResult>;

internal sealed class UpdateSettingCommandHandler(
    ISettingsDbContext dbContext,
    ILogger<UpdateSettingCommandHandler> logger,
    ISender sender,
    IPageListMaxItemsValidator validator,
    IAbsoluteDirectoryWriteAccessValidator writeAccessValidator,
    IEnumerable<ISettingsCrossValidator>? crossValidators = null) : IRequestHandler<UpdateSettingCommand, SettingsUpdateResult>
{
    public async Task<SettingsUpdateResult> Handle(UpdateSettingCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Request to update settings for page: {Key}", request.SettingToSave?.Key);

        if (request.SettingToSave == null)
        {
            throw new InvalidOperationException("SettingToSave cannot be null");
        }

        var providerManagedReadOnlyDisplayNamePageListKeys = SettingsCommandHelper.GetProviderManagedReadOnlyDisplayNamePageListKeys();
        var options = PageHelper.FindOptions(request.SettingToSave);
        SettingsCommandHelper.RemoveProviderManagedReadOnlyDisplayNameOptions(options, providerManagedReadOnlyDisplayNamePageListKeys);

        var isPageListUpdate = request.IsPageListUpdate == true;

        var allSettings = await dbContext.Settings
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (isPageListUpdate && IsDeletedPageListEntryUpdate(request.SettingToSave, allSettings))
        {
            logger.LogWarning(
                "Concurrent modification detected for {PageKey}. The PageList entry was deleted before the current edit was saved.",
                request.SettingToSave.Key);

            throw new ObjectGoneException(GetDeletedSettingMessage());
        }

        var validationErrors = SettingsCommandHelper.ValidateOptions(options, allSettings, crossValidators);
        if (validationErrors is { Count: > 0 })
        {
            return new SettingsUpdateResult
            {
                Success = false,
                Errors = validationErrors.Select(ToUpdateError).ToList()
            };
        }

        if (!isPageListUpdate)
        {
            var addMaxItemsErrors = await ValidatePageListMaxItemsForNewEntryAsync(request.SettingToSave, cancellationToken)
                .ConfigureAwait(false);

            if (addMaxItemsErrors is { Count: > 0 })
            {
                return new SettingsUpdateResult
                {
                    Success = false,
                    Errors = addMaxItemsErrors
                };
            }
        }

        var maxItemsErrors = ValidatePageListMaxItems(request.SettingToSave, allSettings);
        if (maxItemsErrors is { Count: > 0 })
        {
            return new SettingsUpdateResult
            {
                Success = false,
                Errors = maxItemsErrors
            };
        }

        var writeAccessErrors = ValidateAbsoluteDirectoryPathWriteAccess(options, allSettings);
        if (writeAccessErrors is { Count: > 0 })
        {
            return new SettingsUpdateResult
            {
                Success = false,
                Errors = writeAccessErrors
            };
        }

        var modifiedSettings = SaveSettingsToDb(options, allSettings, providerManagedReadOnlyDisplayNamePageListKeys);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await sender.Send(SettingsEvents.SettingsModifiedEvent.Create(modifiedSettings).ToCommand(), cancellationToken).ConfigureAwait(false);
        if (modifiedSettings.Count > 0)
        {
            await sender.Send(new NotifySettingsChangedCommand(UpdatedKeys: modifiedSettings.Keys.ToList()), cancellationToken).ConfigureAwait(false);
        }

        return new SettingsUpdateResult
        {
            Success = true
        };
    }

    private Dictionary<string, string> SaveSettingsToDb(
        Dictionary<string, Option> options,
        List<Setting> allSettings,
        List<string> providerManagedReadOnlyDisplayNamePageListKeys)
    {
        Dictionary<string, string> modifiedSettings = new();
        RemoveProviderManagedReadOnlyDisplayNameSettings(
            allSettings,
            providerManagedReadOnlyDisplayNamePageListKeys,
            modifiedSettings);

        var sameKeySettings = allSettings.Where(s => options.ContainsKey(s.Key)).ToList();

        //Update settings if needed
        foreach (var setting in sameKeySettings)
        {
            var matchingSetting = options[setting.Key];
            var oldValue = SettingValueStorage.GetEffectiveValue(setting, matchingSetting);
            var valueChanged = oldValue != matchingSetting.Value;
            var storageMigrationNeeded = SettingValueStorage.NeedsStorageMigration(setting, matchingSetting);
            if (matchingSetting.Value == matchingSetting.DefaultValue)
            {
                modifiedSettings.Add(setting.Key, $"Old: {oldValue}, New: {matchingSetting.Value}");
                //Remove setting from DB because value is default
                logger.LogInformation("Remove setting because value is default: {Key}", setting.Key);
                dbContext.Settings.Remove(setting);
            }
            else if (valueChanged || storageMigrationNeeded)
            {
                if (valueChanged)
                {
                    modifiedSettings.Add(setting.Key, $"Old: {oldValue}, New: {matchingSetting.Value}");
                }
                //Update value
                logger.LogInformation(
                    "Update setting: {Key};{Value}",
                    setting.Key,
                    matchingSetting.Value);
                SettingValueStorage.SetStoredValue(setting, matchingSetting, matchingSetting.Value);
                dbContext.Settings.Update(setting);
            }
        }

        //Add settings if needed
        foreach (var option in options)
        {
            if (sameKeySettings.All(s => s.Key != option.Key)
                && option.Value.Value != option.Value.DefaultValue)
            {
                logger.LogInformation(
                    "Add setting: {Key};{Value}",
                    option.Key,
                    option.Value.Value);
                dbContext.Settings.Add(SettingValueStorage.CreateSetting(option.Value));
                var oldValue = string.IsNullOrEmpty(option.Value.DefaultValue) ? "(New Addition)" : option.Value.DefaultValue;
                modifiedSettings.Add(option.Key, $"Old: {oldValue}, New: {option.Value.Value}");
            }
        }

        return modifiedSettings;
    }

    private void RemoveProviderManagedReadOnlyDisplayNameSettings(
        List<Setting> allSettings,
        List<string> providerManagedReadOnlyDisplayNamePageListKeys,
        Dictionary<string, string> modifiedSettings)
    {
        if (providerManagedReadOnlyDisplayNamePageListKeys.Count == 0)
        {
            return;
        }

        var settingsToRemove = allSettings
            .Where(s => SettingsCommandHelper.IsProviderManagedReadOnlyDisplayNameKey(s.Key, providerManagedReadOnlyDisplayNamePageListKeys))
            .ToList();

        foreach (var setting in settingsToRemove)
        {
            logger.LogInformation("Remove provider-managed display name setting: {Key}", setting.Key);
            dbContext.Settings.Remove(setting);
            modifiedSettings.TryAdd(setting.Key, $"Old: {setting.Value}, New: (provider display name)");
        }
    }

    private List<FailedSettingUpdateError> ValidatePageListMaxItems(Page page, List<Setting> allSettings)
    {
        var keys = allSettings.Select(s => s.Key);
        var result = validator.ValidateExistingPageLists(page, keys);
        return result.Errors.Select(ToUpdateError).ToList();
    }

    private List<FailedSettingUpdateError> ValidateAbsoluteDirectoryPathWriteAccess(
        Dictionary<string, Option> options,
        List<Setting> allSettings)
    {
        return options.Values
            .Where(option => option.IsAbsoluteDirectoryPath)
            .Where(option => HasValueChanged(option, allSettings))
            .Select(option => new
            {
                Option = option,
                Result = writeAccessValidator.Validate(option.Value)
            })
            .Where(x => !x.Result.IsValid)
            .Select(x => new FailedSettingUpdateError(
                x.Option.Key,
                x.Option.Value ?? string.Empty,
                SettingUpdateError.AbsoluteDirectoryWriteAccessDenied))
            .ToList();
    }

    private static bool HasValueChanged(Option option, List<Setting> allSettings)
    {
        var existingSetting = allSettings.FirstOrDefault(setting => setting.Key == option.Key);

        var currentValue = existingSetting is null
            ? option.DefaultValue
            : SettingValueStorage.GetEffectiveValue(existingSetting, option);

        return !string.Equals(currentValue, option.Value, StringComparison.Ordinal);
    }

    private static bool IsDeletedPageListEntryUpdate(Page page, List<Setting> allSettings)
    {
        if (!TryGetPageListEntryKey(page.Key, out var pageListEntryKey))
        {
            return false;
        }

        if (IsProviderManagedPageListEntry(pageListEntryKey))
        {
            return false;
        }

        var entryPrefix = pageListEntryKey + PageHelper.SettingsKeyDeLimiter;
        return !allSettings.Any(
            s => s.Key == pageListEntryKey ||
                s.Key.StartsWith(entryPrefix, StringComparison.Ordinal));
    }

    private async Task<List<FailedSettingUpdateError>> ValidatePageListMaxItemsForNewEntryAsync(
        Page page,
        CancellationToken cancellationToken)
    {
        // PageList entry keys end with the numeric index, e.g. "Lis:LisChannels:3".
        if (!IsPageListEntryKey(page.Key))
        {
            return [];
        }

        var segments = page.Key!.Split(PageHelper.SettingsKeyDeLimiter);
        var entryPrefix = page.Key + PageHelper.SettingsKeyDeLimiter;

        // Validate max items and check for conflicts
        var pageListKey = string.Join(PageHelper.SettingsKeyDeLimiter, segments.Take(segments.Length - 1));
        var rootPageLabel = segments[0];

        // Re-fetch the current count from database to catch concurrent creations
        var currentKeysInDb = await dbContext.Settings
            .Where(s => s.Key.StartsWith(pageListKey + PageHelper.SettingsKeyDeLimiter))
            .Select(s => s.Key)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    
        var actualCurrentCount = PageHelper.CountDistinctPageListItemIndices(currentKeysInDb, pageListKey);

        var validationResult = await validator.ValidateCanAddAsync(pageListKey, rootPageLabel, cancellationToken)
            .ConfigureAwait(false);

        // Max items limit (higher priority)
        if (validationResult.MaxItems.HasValue && actualCurrentCount >= validationResult.MaxItems.Value)
        {
            logger.LogWarning(
                "PageList max items exceeded for {PageListKey}. Current count in DB: {Current}, Max allowed: {Max}",
                pageListKey, actualCurrentCount, validationResult.MaxItems.Value);
            
            return [new FailedSettingUpdateError(
                pageListKey,
                actualCurrentCount.ToString(CultureInfo.InvariantCulture),
                SettingUpdateError.MaxItemsExceeded)];
        }

        // Concurrent creation conflict (same specific key)
        var conflictingEntry = currentKeysInDb.Any(k => k.StartsWith(entryPrefix, StringComparison.Ordinal));
        if (conflictingEntry)
        {
            logger.LogWarning(
                "Concurrent creation conflict detected for {PageKey}. Another user created this entry while the current user was editing.",
                page.Key);
            
            return [new FailedSettingUpdateError(
                page.Key,
                page.Key,
                SettingUpdateError.ConcurrentModification)];
        }

        return [];
    }

    private static bool IsPageListEntryKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        var segments = key.Split(PageHelper.SettingsKeyDeLimiter);
        return segments.Length >= 2 &&
            int.TryParse(segments[^1], NumberStyles.None, CultureInfo.InvariantCulture, out _);
    }

    private static bool TryGetPageListEntryKey(
        string? key,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? pageListEntryKey)
    {
        pageListEntryKey = null;

        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        var segments = key.Split(PageHelper.SettingsKeyDeLimiter);
        for (var i = segments.Length - 1; i >= 1; i--)
        {
            if (!int.TryParse(segments[i], NumberStyles.None, CultureInfo.InvariantCulture, out _))
            {
                continue;
            }

            pageListEntryKey = string.Join(PageHelper.SettingsKeyDeLimiter, segments.Take(i + 1));
            return true;
        }

        return false;
    }

    private static bool IsProviderManagedPageListEntry(string pageListEntryKey)
    {
        if (!TryGetPageListKey(pageListEntryKey, out var pageListKey))
        {
            return false;
        }

        var pages = BaseDefaultSettingsProvider.GetTopLevelPages();
        return ContainsProviderManagedPageList(pages.Values, pageListKey);
    }

    private static bool TryGetPageListKey(
        string pageListEntryKey,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? pageListKey)
    {
        pageListKey = null;

        var segments = pageListEntryKey.Split(PageHelper.SettingsKeyDeLimiter);
        if (segments.Length < 2)
        {
            return false;
        }

        pageListKey = string.Join(PageHelper.SettingsKeyDeLimiter, segments.Take(segments.Length - 1));
        return true;
    }

    private static bool ContainsProviderManagedPageList(IEnumerable<Page> pages, string pageListKey)
    {
        foreach (var page in pages)
        {
            if (ContainsProviderManagedPageList(page.Children, pageListKey))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsProviderManagedPageList(IEnumerable<SettingsChildItem> children, string pageListKey)
    {
        foreach (var child in children)
        {
            switch (child)
            {
                case PageList pageList:
                    if (string.Equals(pageList.Key, pageListKey, StringComparison.Ordinal))
                    {
                        return pageList.IsProviderManaged;
                    }

                    if (pageList.PageStructure is Page pageStructure &&
                        ContainsProviderManagedPageList(pageStructure.Children, pageListKey))
                    {
                        return true;
                    }

                    foreach (var pageEntry in pageList.PageEntries.OfType<Page>())
                    {
                        if (ContainsProviderManagedPageList(pageEntry.Children, pageListKey))
                        {
                            return true;
                        }
                    }

                    break;
                case Page page:
                    if (ContainsProviderManagedPageList(page.Children, pageListKey))
                    {
                        return true;
                    }

                    break;
                case Group group:
                    if (ContainsProviderManagedPageList(group.Children, pageListKey))
                    {
                        return true;
                    }

                    break;
            }
        }

        return false;
    }

    private static string GetDeletedSettingMessage() =>
        Translations.ResourceManager.GetString("DeletedSetting", Translations.Culture) ??
        "The setting no longer exists.";

    private static FailedSettingUpdateError ToUpdateError(FailedSettingImportError error)
    {
        return new FailedSettingUpdateError(error.Key, error.Value, ToUpdateError(error.Error));
    }

    private static SettingUpdateError ToUpdateError(SettingImportError error)
    {
        return error switch
        {
            SettingImportError.MaxItemsExceeded => SettingUpdateError.MaxItemsExceeded,
            SettingImportError.ConcurrentModification => SettingUpdateError.ConcurrentModification,
            SettingImportError.SectionsOverlap => SettingUpdateError.SectionsOverlap,
            _ => SettingUpdateError.ValidationError
        };
    }
}
