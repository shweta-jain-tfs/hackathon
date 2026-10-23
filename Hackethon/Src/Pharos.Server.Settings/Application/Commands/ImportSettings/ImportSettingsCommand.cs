using System.Globalization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pharos.Server.Application.AuditTrail.Events;
using Pharos.Server.Settings.Application.Commands.NotifySettingsChanged;
using Pharos.Server.Settings.Application.Interfaces;
using Pharos.Server.Settings.Application.Metadata;
using Pharos.Server.Settings.Application.Services;
using Pharos.Server.Settings.Application.Validation;
using Pharos.Server.Settings.Domain;
using Pharos.Server.Settings.Infrastructure.ConfigurationProvider;

namespace Pharos.Server.Settings.Application.Commands.ImportSettings;

public record ImportSettingsCommand(string? CsvContent) : IRequest<SettingsImportResult>;

internal sealed class ImportSettingsCommandHandler(
    ISettingsDbContext dbContext,
    ILogger<ImportSettingsCommandHandler> logger,
    ISender sender,
    IPageListMaxItemsValidator validator,
    IEnumerable<ISettingsCrossValidator>? crossValidators = null) : IRequestHandler<ImportSettingsCommand, SettingsImportResult>
{
    public async Task<SettingsImportResult> Handle(ImportSettingsCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Request to import settings");

        if (request.CsvContent == null)
        {
            throw new InvalidOperationException("CsvContent cannot be null");
        }

        var defaultOptions = BaseDefaultSettingsProvider.GetDefaultOptions();
        var (importStatus, settingsToImport, importErrors, unrecognizedSettings) =
            SettingsImporter.ParseCsvToSettings(request.CsvContent, defaultOptions);

        var providerManagedReadOnlyDisplayNamePageListKeys = SettingsCommandHelper.GetProviderManagedReadOnlyDisplayNamePageListKeys();
        var options = PageHelper.FindOptions(settingsToImport);
        SettingsCommandHelper.RemoveProviderManagedReadOnlyDisplayNameOptions(options, providerManagedReadOnlyDisplayNamePageListKeys);

        var allSettings = await dbContext.Settings
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var importedSettings = options
            .Select(kv => new Setting { Key = kv.Key, Value = kv.Value.Value })
            .ToList();
        var validationErrors = SettingsCommandHelper.ValidateOptions(options, importedSettings, crossValidators);
        importErrors.AddRange(validationErrors);

        var maxItemsErrors = ValidatePageListMaxItemsForImport(settingsToImport);
        importErrors.AddRange(maxItemsErrors);

        if (!importStatus || validationErrors is { Count: > 0 } || maxItemsErrors is { Count: > 0 })
        {
            return new SettingsImportResult
            {
                Success = false,
                Errors = importErrors,
                UnrecognizedSettings = unrecognizedSettings
            };
        }

        var updatedKeys = GetUpdatedKeys(options, allSettings);
        var deletedKeys = GetDeletedKeys(options, allSettings);
        AddPageListEntryNotificationKeys(
            updatedKeys,
            deletedKeys,
            options.Keys,
            allSettings.Select(setting => setting.Key));
        RemoveAllSettingsFromDb(allSettings);
        var modifiedSettings = ImportSettingsToDb(options, allSettings);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await sender.Send(SettingsEvents.SettingsImportedEvent.Create(modifiedSettings).ToCommand(), cancellationToken).ConfigureAwait(false);
        if (updatedKeys.Count > 0 || deletedKeys.Count > 0)
        {
            await sender.Send(
                    new NotifySettingsChangedCommand(
                        UpdatedKeys: updatedKeys,
                        DeletedKeys: deletedKeys),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        return new SettingsImportResult
        {
            Success = true,
            Errors = importErrors,
            UnrecognizedSettings = unrecognizedSettings
        };
    }

    private static List<string> GetUpdatedKeys(Dictionary<string, Option> options, List<Setting> allSettings)
    {
        var settingsByKey = allSettings.ToDictionary(setting => setting.Key, StringComparer.Ordinal);

        return options
            .Where(option =>
            {
                if (settingsByKey.TryGetValue(option.Key, out var setting))
                {
                    var oldValue = SettingValueStorage.GetEffectiveValue(setting, option.Value);
                    return !string.Equals(oldValue, option.Value.Value, StringComparison.Ordinal);
                }

                return !string.Equals(option.Value.Value, option.Value.DefaultValue, StringComparison.Ordinal);
            })
            .Select(option => option.Key)
            .ToList();
    }

    private static List<string> GetDeletedKeys(Dictionary<string, Option> options, List<Setting> allSettings)
    {
        return allSettings
            .Select(setting => setting.Key)
            .Except(options.Keys, StringComparer.Ordinal)
            .ToList();
    }

    private static void AddPageListEntryNotificationKeys(
        List<string> updatedKeys,
        List<string> deletedKeys,
        IEnumerable<string> importedKeys,
        IEnumerable<string> storedKeys)
    {
        var pageListKeys = GetPageListKeys();
        if (pageListKeys.Count == 0)
        {
            return;
        }

        var importedEntryKeys = GetPageListEntryKeys(importedKeys, pageListKeys);
        var storedEntryKeys = GetPageListEntryKeys(storedKeys, pageListKeys);
        var deletedEntryKeys = storedEntryKeys
            .Except(importedEntryKeys, StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);

        var updatedEntryKeys = updatedKeys
            .Concat(deletedKeys)
            .Select(key => TryGetPageListEntryKey(key, pageListKeys))
            .Where(key => key is not null && !deletedEntryKeys.Contains(key))
            .Select(key => key!)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var updatedPageListKeys = updatedEntryKeys
            .Select(key => TryGetPageListKeyFromEntryKey(key, pageListKeys))
            .Where(key => key is not null)
            .Select(key => key!)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        AddMissingKeys(updatedKeys, updatedEntryKeys);
        AddMissingKeys(updatedKeys, updatedPageListKeys);
        AddMissingKeys(deletedKeys, deletedEntryKeys);
    }

    private static HashSet<string> GetPageListEntryKeys(
        IEnumerable<string> keys,
        IReadOnlyCollection<string> pageListKeys)
    {
        return keys
            .Select(key => TryGetPageListEntryKey(key, pageListKeys))
            .Where(key => key is not null)
            .Select(key => key!)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static string? TryGetPageListEntryKey(string key, IReadOnlyCollection<string> pageListKeys)
    {
        foreach (var pageListKey in pageListKeys)
        {
            var prefix = pageListKey + PageHelper.SettingsKeyDeLimiter;
            if (!key.StartsWith(prefix, StringComparison.Ordinal))
            {
                continue;
            }

            var suffix = key[prefix.Length..];
            var delimiterIndex = suffix.IndexOf(PageHelper.SettingsKeyDeLimiter, StringComparison.Ordinal);
            if (delimiterIndex <= 0)
            {
                continue;
            }

            var entryIndex = suffix[..delimiterIndex];
            if (!int.TryParse(entryIndex, NumberStyles.None, CultureInfo.InvariantCulture, out _))
            {
                continue;
            }

            return pageListKey + PageHelper.SettingsKeyDeLimiter + entryIndex;
        }

        return null;
    }

    private static string? TryGetPageListKeyFromEntryKey(
        string pageListEntryKey,
        IReadOnlyCollection<string> pageListKeys)
    {
        return pageListKeys.FirstOrDefault(pageListKey =>
            pageListEntryKey.StartsWith(pageListKey + PageHelper.SettingsKeyDeLimiter, StringComparison.Ordinal));
    }

    private static List<string> GetPageListKeys()
    {
        var pages = BaseDefaultSettingsProvider.GetTopLevelPages();
        var result = new List<string>();

        CollectPageListKeys(pages.Values, result);

        return result
            .Distinct(StringComparer.Ordinal)
            .OrderByDescending(key => key.Length)
            .ToList();
    }

    private static void CollectPageListKeys(IEnumerable<SettingsChildItem> children, List<string> result)
    {
        foreach (var child in children)
        {
            switch (child)
            {
                case PageList pageList:
                    result.Add(pageList.Key);

                    if (pageList.PageStructure is ISettingsParentItem pageStructure)
                    {
                        CollectPageListKeys(pageStructure.Children, result);
                    }

                    CollectPageListKeys(pageList.PageEntries, result);
                    break;
                case ISettingsParentItem parent:
                    CollectPageListKeys(parent.Children, result);
                    break;
            }
        }
    }

    private static void AddMissingKeys(List<string> target, IEnumerable<string> keys)
    {
        var existingKeys = target.ToHashSet(StringComparer.Ordinal);
        foreach (var key in keys)
        {
            if (existingKeys.Add(key))
            {
                target.Add(key);
            }
        }
    }

    private void RemoveAllSettingsFromDb(List<Setting> settings)
    {
        foreach (var setting in settings)
        {
            dbContext.Settings.Remove(setting);
        }
    }

    private Dictionary<string, string> ImportSettingsToDb(Dictionary<string, Option> options, List<Setting> allSettings)
    {
        Dictionary<string, string> modifiedSettings = new();
        foreach (var option in options.Where(option => option.Value.Value != option.Value.DefaultValue))
        {
            var existingSetting = allSettings.FirstOrDefault(s => s.Key == option.Key);
            var oldValue = existingSetting is null
                ? option.Value.DefaultValue
                : SettingValueStorage.GetEffectiveValue(existingSetting, option.Value);

            dbContext.Settings.Add(SettingValueStorage.CreateSetting(option.Value));


            if (oldValue != option.Value.Value)
            {
                modifiedSettings.Add(option.Key, $"Old: {oldValue}, New: {option.Value.Value}");
            }
        }

        return modifiedSettings;
    }

    private List<FailedSettingImportError> ValidatePageListMaxItemsForImport(Page page)
    {
        var result = validator.ValidateImportedPageLists(page);
        return result.Errors;
    }
}
