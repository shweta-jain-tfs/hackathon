using System.Text;
using Microsoft.EntityFrameworkCore;
using Pharos.Server.Settings.Application.Commands;
using Pharos.Server.Settings.Application.Interfaces;
using Pharos.Server.Settings.Application.Metadata;
using Pharos.Server.Settings.Application.Services;
using Pharos.Server.Settings.Infrastructure.ConfigurationProvider;
using Pharos.Server.Localization;

namespace Pharos.Server.Settings.Application.Validation;

/// <summary>
/// Validates PageList max items constraints across add, copy, and import operations.
/// </summary>
internal sealed class PageListMaxItemsValidator(
    ISettingsDbContext dbContext,
    ISettingsPagesLoader loader,
    IPageTreeNavigator pageTreeNavigator) : IPageListMaxItemsValidator
{
    private static readonly CompositeFormat _maxItemsReachedFormat = CompositeFormat.Parse(Translations.Validation_PageListMaxItemsReached);

    public async Task<PageListMaxItemsValidationResult> ValidateCanAddAsync(
        string pageListKey,
        string rootPageLabel,
        CancellationToken cancellationToken)
    {
        // Load the page structure to find the PageList
        var populatedPages = await loader.LoadAsync(rootPageLabel, cancellationToken)
            .ConfigureAwait(false);

        var pageList = pageTreeNavigator.FindPageListByKey(populatedPages, pageListKey);

        if (pageList is null)
        {
            return PageListMaxItemsValidationResult.Success(); // If PageList not found, allow addition
        }

        // Count existing items from database
        var keys = await dbContext.Settings
            .Where(s => s.Key.StartsWith(pageListKey + PageHelper.SettingsKeyDeLimiter))
            .Select(s => s.Key)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var currentCount = PageHelper.CountDistinctPageListItemIndices(keys, pageListKey);

        if (!pageList.MaxItems.HasValue)
        {
            return PageListMaxItemsValidationResult.Success(currentCount); // No limit set
        }

        var canAdd = currentCount < pageList.MaxItems.Value;

        if (!canAdd)
        {
            var errorMessage = string.Format(null, _maxItemsReachedFormat, pageList.MaxItems.Value);
            return PageListMaxItemsValidationResult.Failure(errorMessage, currentCount, pageList.MaxItems.Value);
        }

        return PageListMaxItemsValidationResult.Success(currentCount, pageList.MaxItems.Value);
    }

    public PageListMaxItemsValidationResult ValidateExistingPageLists(
        Page page,
        IEnumerable<string> existingSettingKeys)
    {
        var errors = new List<FailedSettingImportError>();
        var pageLists = pageTreeNavigator.FindAllPageListsInPage(page);

        foreach (var pageList in pageLists)
        {
            if (!pageList.MaxItems.HasValue)
            {
                continue;
            }

            var existingCount = PageHelper.CountDistinctPageListItemIndices(existingSettingKeys, pageList.Key);
            if (existingCount > pageList.MaxItems.Value)
            {
                errors.Add(new FailedSettingImportError(
                    pageList.Key,
                    existingCount.ToString(),
                    SettingImportError.MaxItemsExceeded));
            }
        }

        return errors.Count > 0
            ? PageListMaxItemsValidationResult.Failure(errors)
            : PageListMaxItemsValidationResult.Success();
    }

    public PageListMaxItemsValidationResult ValidateImportedPageLists(Page page)
    {
        var errors = new List<FailedSettingImportError>();

        // For CSV imports, page.Children contains flat Option list, not structured PageLists
        // We need to count imported settings by analyzing the Option keys
        var importedSettingKeys = page.Children
            .OfType<Option>()
            .Select(o => o.Key)
            .ToList();

        if (importedSettingKeys.Count == 0)
        {
            return PageListMaxItemsValidationResult.Success();
        }

        // Load default page structures to get PageList definitions with MaxItems
        var rootPageLabels = importedSettingKeys
            .Select(k => k.Split(':').FirstOrDefault())
            .Where(label => !string.IsNullOrEmpty(label))
            .Distinct()
            .ToList();

        foreach (var rootLabel in rootPageLabels)
        {
            var populatedPages = loader.LoadAsync(rootLabel!, CancellationToken.None).GetAwaiter().GetResult();
            var pageLists = pageTreeNavigator.FindAllPageLists(populatedPages);

            foreach (var pageList in pageLists)
            {
                if (!pageList.MaxItems.HasValue)
                {
                    continue;
                }

                // Count how many distinct items are being imported for this PageList
                var itemCount = PageHelper.CountDistinctPageListItemIndices(importedSettingKeys, pageList.Key);

                if (itemCount > pageList.MaxItems.Value)
                {
                    errors.Add(new FailedSettingImportError(
                        pageList.Key,
                        itemCount.ToString(),
                        SettingImportError.MaxItemsExceeded));
                }
            }
        }

        return errors.Count > 0
            ? PageListMaxItemsValidationResult.Failure(errors)
            : PageListMaxItemsValidationResult.Success();
    }
}

