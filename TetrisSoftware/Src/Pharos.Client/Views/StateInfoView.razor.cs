using System.Globalization;
using Pharos.Client.Common.App.ViewModels;
using Pharos.Common.Web.Utilities;

namespace Pharos.Client.Views;

public partial class StateInfoView : IDisposable
{
    [Inject] private LockScreenViewModel LockScreenViewModel { get; set; } = null!;

    [Parameter] public bool HasTaskCards { get; set; } = true;

    protected override async Task OnInitializedAsync()
    {
        LockScreenViewModel.OnLockScreenTestInfoChanged += OnStateRowsChanged;
        await LockScreenViewModel.LoadAsync();
    }

    private string GetStatusPanelsClass()
    {
        return new CssBuilder("ph-state-info-view")
        .AddClass("ph-state-info-view--no-tasks", !HasTaskCards)
        .AddClass("ph-state-info-view--single", LockScreenViewModel.StateInfoItems.Count == 1)
        .Build();
    }

    private string GetStateInfoCssClass()
    {
        return new CssBuilder("ph-state-info-view__state-info")
        .AddClass("ph-state-info-view__state-info--single", LockScreenViewModel.StateInfoItems.Count == 1)
        .Build();
    }

    private static string FormatMinutes(LockScreenStateInfoItem item)
    {
        return item.Hours.HasValue && item.Minutes.HasValue
            ? item.Minutes.Value.ToString("D2", CultureInfo.InvariantCulture)
            : item.Minutes?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private void OnStateRowsChanged()
    {
        InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        LockScreenViewModel.OnLockScreenTestInfoChanged -= OnStateRowsChanged;
        GC.SuppressFinalize(this);
    }
}
