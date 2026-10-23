using Microsoft.Extensions.Localization;
using Pharos.Client.App.Quality.ViewModels;
using Pharos.Client.App.Quality.Views.QualityControls.Helpers;
using Pharos.Client.Common.Localization;

namespace Pharos.Client.App.Quality.Views.QualityControls;

public partial class MethodQualityControlOverviewView : IDisposable
{
    [Inject] private MethodQualityControlOverviewViewModel ViewModel { get; set; } = default!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    private Dictionary<string, string> _qualityControlMethodNameDict = [];

    protected override async Task OnInitializedAsync()
    {
        ViewModel.ViewRefreshNeeded += OnViewRefreshNeeded;
        _qualityControlMethodNameDict = await ViewModel.GetQualityControlsMethodNames();
    }

    private string QualityControlTypeString(QualityControlSystemType systemType)
    {
        return MethodQualityControlHelper.QualityControlTypeToString(systemType == QualityControlSystemType.Lab, Loc);
    }

    private void OnViewRefreshNeeded(object? sender, EventArgs e)
    {
        InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        ViewModel.ViewRefreshNeeded -= OnViewRefreshNeeded;
        ViewModel.Dispose();
        GC.SuppressFinalize(this);
    }
}