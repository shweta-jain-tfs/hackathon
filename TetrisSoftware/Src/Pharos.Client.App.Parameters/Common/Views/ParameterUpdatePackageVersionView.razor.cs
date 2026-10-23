using Pharos.Client.App.Parameters.Common.ViewModels;
using Pharos.Client.Common.Models.Notifications;

namespace Pharos.Client.App.Parameters.Common.Views;

public partial class ParameterUpdatePackageVersionView : IDisposable
{
    public string? Version { get; set; }

    [Inject] private ParameterUpdatePackageVersionViewModel ViewModel { get; set; } = default!;
    [Inject] private IParametersNotificationProvider NotificationProvider { get; set; } = default!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        Version = await ViewModel.GetVersion();
        NotificationProvider.ParameterUpdateFinished += OnParameterUpdateFinished;
    }

    private async void OnParameterUpdateFinished(object? sender, bool e)
    {
        Version = await ViewModel.GetVersion();
        StateHasChanged();
    }

    public void Dispose()
    {
        NotificationProvider.ParameterUpdateFinished -= OnParameterUpdateFinished;
        GC.SuppressFinalize(this);
    }
}