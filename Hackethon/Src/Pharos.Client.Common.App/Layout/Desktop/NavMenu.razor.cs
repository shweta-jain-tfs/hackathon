using Microsoft.Extensions.DependencyInjection;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.App.Services;

namespace Pharos.Client.Common.App.Layout.Desktop;

public partial class NavMenu : IDisposable
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] private UiSettingsService UiSettingsService { get; set; } = default!;
    [Inject] private IServiceProvider ServiceProvider { get; set; } = default!;

    private UiSettings? _uiSettings;
    private IReadOnlyList<RouteSubMenuItem> _settingsMenuItems = [];

    protected override async Task OnInitializedAsync()
    {
        UiSettingsService.SettingsChanged += HandleSettingsChangedAsync;
        _uiSettings = await UiSettingsService.GetSettingsAsync();
        await LoadSettingsMenuItemsAsync();
    }

    private async Task LoadSettingsMenuItemsAsync(CancellationToken cancellationToken = default)
    {
        var settingsMenuProvider = ServiceProvider.GetService<ISettingsMenuProvider>();

        if (settingsMenuProvider is null)
        {
            _settingsMenuItems = [];
            return;
        }

        _settingsMenuItems = await settingsMenuProvider.GetSettingsMenuItemsAsync(cancellationToken);
    }

    private async Task HandleSettingsChangedAsync()
    {
        _uiSettings = await UiSettingsService.GetSettingsAsync();
        await InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        UiSettingsService.SettingsChanged -= HandleSettingsChangedAsync;
        GC.SuppressFinalize(this);
    }
}