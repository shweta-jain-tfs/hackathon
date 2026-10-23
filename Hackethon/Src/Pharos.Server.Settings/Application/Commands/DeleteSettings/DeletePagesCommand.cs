using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Pharos.Server.Application.AuditTrail.Events;
using Pharos.Server.Application.Command;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Request;
using Pharos.Server.Domain.Exceptions;
using Pharos.Contracts.Settings;
using Pharos.Server.Settings.Application.Commands.NotifySettingsChanged;
using Pharos.Server.Settings.Application.Interfaces;
using Pharos.Server.Settings.Application.Metadata;
using Pharos.Server.Settings.Application.Services;

namespace Pharos.Server.Settings.Application.Commands.DeleteSettings;

internal record DeletePagesCommand(string[] PageListEntryKeys) : IRequest<DeleteResponse>;

internal sealed class DeletePagesCommandHandler(
    ISettingsDbContext dbContext,
    ILogger<DeletePagesCommandHandler> logger,
    ISender sender,
    IConfiguration configuration,
    ISettingsPagesLoader settingsPagesLoader,
    IPageTreeNavigator pageTreeNavigator) : IRequestHandler<DeletePagesCommand, DeleteResponse>
{
    private const string _tubeTypesPrefix = SettingsKeys.Samples.TubeTypes.TubeTypeList;

    // Tube type defaults are stored as settings keys (values are TubeDefinitionId strings).
    // When deleting TubeTypes page list entries, we must NOT delete the entry whose TubeDefinitionId
    // equals either default; otherwise the configured defaults would point to a missing tube type.
    private const string _normalDefaultKey = SettingsKeys.Samples.TubeTypes.NormalTubeDefinitionId;
    private const string _pediatricDefaultKey = SettingsKeys.Samples.TubeTypes.PediatricTubeDefinitionId;

    public async Task<DeleteResponse> Handle(DeletePagesCommand request, CancellationToken cancellationToken)
    {
        var keysToDelete = new List<string>();
        var skippedKeys = new List<string>();
        var skippedDefaultTubeDefinitionIds = new List<int>();
        var skippedTubeTypeReasons = new List<DeleteSkippedTubeTypeReason>();

        var normalDefaultId = await GetDefaultTubeDefinitionIdAsync(_normalDefaultKey, cancellationToken)
            .ConfigureAwait(false);
        var pediatricDefaultId = await GetDefaultTubeDefinitionIdAsync(_pediatricDefaultKey, cancellationToken)
            .ConfigureAwait(false);

        foreach (var key in request.PageListEntryKeys)
        {
            if (IsTubeTypeEntryKey(key))
            {
                var tubeDefinitionId = await GetTubeDefinitionIdForEntryAsync(key, cancellationToken).ConfigureAwait(false);

                // If we can't resolve the selected tube type id, do not delete.
                if (!tubeDefinitionId.HasValue)
                {
                    logger.LogWarning(
                        "Skipping deletion for tube type key {Key} because TubeDefinitionId could not be resolved.",
                        key);

                    skippedKeys.Add(key);
                    continue;
                }

                var isUsedByAnyRequest = await sender.Send(new IsTubeTypeUsedByAnyRequestQuery(tubeDefinitionId.Value), cancellationToken)
                    .ConfigureAwait(false);
                var isUsedByAnyQualityControl = await sender.Send(new IsTubeTypeUsedByAnyQualityControlQuery(tubeDefinitionId.Value), cancellationToken)
                    .ConfigureAwait(false);

                if (isUsedByAnyRequest || isUsedByAnyQualityControl)
                {
                    skippedKeys.Add(key);
                    skippedTubeTypeReasons.Add(new DeleteSkippedTubeTypeReason
                    {
                        PageListEntryKey = key,
                        UsedByRequests = isUsedByAnyRequest,
                        UsedByQualityControls = isUsedByAnyQualityControl
                    });
                    continue;
                }

                if (IsDefaultTubeType(tubeDefinitionId, normalDefaultId, pediatricDefaultId))
                {
                    skippedKeys.Add(key);
                    skippedDefaultTubeDefinitionIds.Add(tubeDefinitionId.Value);
                    skippedTubeTypeReasons.Add(new DeleteSkippedTubeTypeReason
                    {
                        PageListEntryKey = key,
                        IsDefaultTubeType = true
                    });
                    continue;
                }

                if (!normalDefaultId.HasValue || !pediatricDefaultId.HasValue)
                {
                    logger.LogWarning(
                        "Skipping deletion for tube type key {Key} because default tube type IDs could not be resolved. " +
                        "normalDefaultId={NormalDefaultId}, pediatricDefaultId={PediatricDefaultId}",
                        key, normalDefaultId, pediatricDefaultId);

                    skippedKeys.Add(key);
                    continue;
                }
            }

            keysToDelete.Add(key);
        }

        keysToDelete = await ApplyPageListMinItemLimitsAsync(
                keysToDelete,
                skippedKeys,
                cancellationToken)
            .ConfigureAwait(false);

        // Nothing deletable
        if (keysToDelete.Count == 0)
        {
            return new DeleteResponse
            {
                Deleted = 0,
                Skipped = skippedKeys.Count,
                SkippedIds = skippedDefaultTubeDefinitionIds,
                SkippedPageListEntryKeys = skippedKeys,
                SkippedTubeTypeReasons = skippedTubeTypeReasons
            };
        }

        var pagesToDeleteEvent = new Dictionary<string, string>();
        foreach (var key in keysToDelete)
        {
            logger.LogInformation("Request to delete page: {Key}", key);

            var settings = await dbContext.Settings
                .Where(s => s.Key.StartsWith(key))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            if (settings.Count == 0)
            {
                throw new NotFoundException($"No settings found! Id: {string.Join(",", key)}");
            }

            foreach (var setting in settings)
            {
                dbContext.Settings.Remove(setting);
            }

            pagesToDeleteEvent.Add(key, "Page Deleted");
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Trigger audit trail event
        await sender.Send(SettingsEvents.SettingsModifiedEvent.Create(pagesToDeleteEvent).ToCommand(), cancellationToken);
        await sender.Send(new NotifySettingsChangedCommand(DeletedKeys: pagesToDeleteEvent.Keys.ToList()), cancellationToken);

        return new DeleteResponse
        {
            Deleted = keysToDelete.Count,
            Skipped = skippedKeys.Count,
            SkippedIds = skippedDefaultTubeDefinitionIds,
            SkippedPageListEntryKeys = skippedKeys,
            SkippedTubeTypeReasons = skippedTubeTypeReasons
        };
    }

    private async Task<List<string>> ApplyPageListMinItemLimitsAsync(
        List<string> candidateKeys,
        List<string> skippedKeys,
        CancellationToken cancellationToken)
    {
        var minItemsByPageListKey = await GetMinItemsByPageListKeyAsync(candidateKeys, cancellationToken)
            .ConfigureAwait(false);

        if (minItemsByPageListKey.Count == 0)
        {
            return candidateKeys;
        }

        var keysToDelete = new List<string>();

        // The minimum is configured per page list, so check each selected item against its own list.
        foreach (var pageListGroup in candidateKeys.GroupBy(GetPageListKey))
        {
            // If the key cannot be matched to a page list with a configured minimum, min-item validation does not apply.
            if (pageListGroup.Key is null ||
                !minItemsByPageListKey.TryGetValue(pageListGroup.Key, out var minItems))
            {
                keysToDelete.AddRange(pageListGroup);
                continue;
            }

            var currentItemCount = await CountCurrentItemsAsync(pageListGroup.Key, cancellationToken)
                .ConfigureAwait(false);
            var deleteCapacity = Math.Max(0, currentItemCount - minItems);
            var groupKeys = pageListGroup.ToList();

            // If this selection would go below the minimum, skip the whole group.
            if (groupKeys.Count > deleteCapacity)
            {
                skippedKeys.AddRange(groupKeys);

                logger.LogInformation(
                    "Skipping deletion for {SkippedCount} page list items in {PageListKey} because deleting them would go below the configured minimum {MinItems}. Current count: {CurrentCount}",
                    groupKeys.Count,
                    pageListGroup.Key,
                    minItems,
                    currentItemCount);

                continue;
            }

            keysToDelete.AddRange(groupKeys);
        }

        return keysToDelete;
    }

    private async Task<Dictionary<string, int>> GetMinItemsByPageListKeyAsync(
        IEnumerable<string> pageListItemKeys,
        CancellationToken cancellationToken)
    {
        var pageListKeys = pageListItemKeys
            .Select(GetPageListKey)
            .Where(key => key is not null)
            .Distinct(StringComparer.Ordinal)
            .Cast<string>()
            .ToList();

        var result = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var pageListKeysByRoot in pageListKeys.GroupBy(GetRootPageLabel))
        {
            Dictionary<string, Page> pages;
            try
            {
                pages = await settingsPagesLoader.LoadAsync(pageListKeysByRoot.Key, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Could not load settings metadata for root page {RootPageLabel} while checking PageList minimum item limits.",
                    pageListKeysByRoot.Key);
                continue;
            }

            foreach (var pageListKey in pageListKeysByRoot)
            {
                var pageList = pageTreeNavigator.FindPageListByKey(pages, pageListKey);
                if (pageList?.MinItems is > 0)
                {
                    result[pageListKey] = pageList.MinItems.Value;
                }
            }
        }

        return result;
    }

    private async Task<int> CountCurrentItemsAsync(string pageListKey, CancellationToken cancellationToken)
    {
        var keys = await dbContext.Settings
            .Where(s => s.Key.StartsWith(pageListKey + PageHelper.SettingsKeyDeLimiter))
            .Select(s => s.Key)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return PageHelper.CountDistinctPageListItemIndices(keys, pageListKey);
    }

    private static bool IsTubeTypeEntryKey(string pageKey)
        => pageKey.StartsWith(_tubeTypesPrefix, StringComparison.Ordinal);

    private static bool IsDefaultTubeType(int? tubeDefinitionId, int? normalDefaultId, int? pediatricDefaultId)
        => tubeDefinitionId.HasValue
           && ((normalDefaultId.HasValue && tubeDefinitionId.Value == normalDefaultId.Value)
               || (pediatricDefaultId.HasValue && tubeDefinitionId.Value == pediatricDefaultId.Value));

    private async Task<int?> GetTubeDefinitionIdForEntryAsync(string pageKey, CancellationToken cancellationToken)
    {
        var tubeDefinitionKey = $"{pageKey}:TubeDefinitionId";
        var value = await dbContext.Settings
            .Where(s => s.Key == tubeDefinitionKey)
            .Select(s => s.Value)
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return TryParseInt(value);
    }

    private async Task<int?> GetDefaultTubeDefinitionIdAsync(string key, CancellationToken cancellationToken)
    {
        var configurationValue = TryParseInt(configuration[key]);
        if (configurationValue.HasValue)
        {
            return configurationValue;
        }

        var settingsValue = await dbContext.Settings
            .Where(setting => setting.Key == key)
            .Select(setting => setting.Value)
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return TryParseInt(settingsValue);
    }

    private static string GetRootPageLabel(string pageListKey)
        => pageListKey.Split(PageHelper.SettingsKeyDeLimiter).First();

    private static string? GetPageListKey(string pageListEntryKey)
    {
        var segments = pageListEntryKey.Split(PageHelper.SettingsKeyDeLimiter);
        if (segments.Length < 2 ||
            !int.TryParse(segments[^1], out _))
        {
            return null;
        }

        return string.Join(PageHelper.SettingsKeyDeLimiter, segments.Take(segments.Length - 1));
    }

    private static int? TryParseInt(string? s) => int.TryParse(s, out var v) ? v : null;
}
