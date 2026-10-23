using Microsoft.Extensions.DependencyInjection;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.App.Services;
using Pharos.Client.Common.App.State.CurrentInstrument;

namespace Pharos.Client.Common.App.Layout.Instrument;

public partial class NavMenu : IDisposable
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = null!;
    [Inject] private ICurrentInstrumentViewModel InstrumentModel { get; set; } = null!;
    [Inject] private UiSettingsService UiSettingsService { get; set; } = default!;
    [Inject] private IServiceProvider ServiceProvider { get; set; } = default!;

    private UiSettings? _uiSettings;
    private IReadOnlyList<RouteSubMenuItem> _settingsMenuItems = [];
    private bool IsDebugMode { get; set; }
    private NotificationLevel TaskCardNotificationLevel => GetNotificationLevel(InstrumentModel.HighestUrgencyLevel);

    protected override async Task OnInitializedAsync()
    {
#if DEBUG
        IsDebugMode = true;
#else
        IsDebugMode = false;
#endif

        InstrumentModel.OnTaskCardChange += InstrumentStateOnTaskCardOnChange;
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

    private void InstrumentStateOnTaskCardOnChange()
    {
        StateHasChanged();
    }

    private static NotificationLevel GetNotificationLevel(TaskCardState taskCardState)
    {
        return taskCardState switch
        {
            TaskCardState.Warning => NotificationLevel.Warning,
            TaskCardState.Error or TaskCardState.FlashingRed => NotificationLevel.Error,
            _ => NotificationLevel.Info
        };
    }

    public void Dispose()
    {
        InstrumentModel.OnTaskCardChange -= InstrumentStateOnTaskCardOnChange;
        UiSettingsService.SettingsChanged -= HandleSettingsChangedAsync;
        GC.SuppressFinalize(this);
    }
}