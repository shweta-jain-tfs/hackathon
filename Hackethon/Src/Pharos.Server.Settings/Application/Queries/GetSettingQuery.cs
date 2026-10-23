using MediatR;
using Microsoft.Extensions.Logging;
using Pharos.Server.Application.Settings;
using Pharos.Server.Settings.Application.Interfaces;
using Pharos.Server.Settings.Application.Metadata;

namespace Pharos.Server.Settings.Application.Queries;

internal record GetSettingQuery(string Key, string RootPageLabel) : IRequest<Page>;

internal class GetSettingQueryHandler : IRequestHandler<GetSettingQuery, Page>
{
    private readonly ILogger<GetSettingQueryHandler> _logger;
    private readonly ISettingsPermissionHelper _permissionHelper;
    private readonly ISettingsPagesLoader _loader;
    private readonly IEnumerable<IDropdownValueProvider> _dropdownProviders;

    public GetSettingQueryHandler(
        ILogger<GetSettingQueryHandler> logger, 
        ISettingsPermissionHelper permissionHelper, 
        ISettingsPagesLoader loader,
        IEnumerable<IDropdownValueProvider> dropdownProviders)
    {
        _logger = logger;
        _permissionHelper = permissionHelper;
        _loader = loader;
        _dropdownProviders = dropdownProviders;
    }

    public async Task<Page> Handle(GetSettingQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Settings page {Key} requested", request.Key);

        var populatedPages = await _loader.LoadAsync(request.RootPageLabel, cancellationToken);

        if (!populatedPages.TryGetValue(request.Key, out var selectedPage))
        {
            throw new ArgumentException($"Page with key [{request.Key}] not found");
        }

        await _permissionHelper.RemoveInaccessibleChildrenAsync(selectedPage);

        await PopulateDynamicDropdownsAsync(selectedPage, cancellationToken);

        SettingsQueryHelper.ReplaceSubPages(ref selectedPage);

        return selectedPage;
    }

    private async Task PopulateDynamicDropdownsAsync(Page page, CancellationToken cancellationToken)
    {
        var optionsWithProviders = CollectOptionsWithProviders(page);

        foreach (var option in optionsWithProviders)
        {
            try
            {
                if (option.DynamicDropdownProviderType == null)
                {
                    continue;
                }

                var provider = _dropdownProviders.FirstOrDefault(p => p.GetType() == option.DynamicDropdownProviderType);

                if (provider == null)
                {
                    throw new InvalidOperationException($"Provider {option.DynamicDropdownProviderType.Name} is not registered in DI");
                }

                var dropdownOptions = await provider.GetOptionsAsync(cancellationToken);

                option.Choices = dropdownOptions.Select(dto => new Choice
                {
                    Value = dto.Value,
                    Label = dto.DisplayName
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to populate dropdown for {Key}", option.Key);
            }
        }
    }

    private static List<Option> CollectOptionsWithProviders(SettingsChildItem item)
    {
        var options = new List<Option>();

        if (item is Option option && option.DynamicDropdownProviderType != null)
        {
            options.Add(option);
        }

        if (item is ISettingsParentItem parent && parent.Children != null)
        {
            foreach (var child in parent.Children)
            {
                options.AddRange(CollectOptionsWithProviders(child));
            }
        }

        return options;
    }
}