using System.ComponentModel;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.Quality.ViewModels;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.App.PageRouting;
using Pharos.Client.Common.Localization;
using Pharos.Common.Web.Components.Modal;
using Pharos.Common.Web.Components.Toaster;

namespace Pharos.Client.App.Quality.Views;

public partial class QcCartridgeDetailsView : IDisposable
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] public QcCartridgeDetailsViewModel ViewModel { get; set; } = default!;
    [Inject] private IToasterService ToasterService { get; set; } = default!;

    [CascadingParameter]
    public ModalInstance Modal { get; set; } = default!;

    [Parameter]
    public string? Barcode { get; set; }

    private IQueryable<QcCartridgeTestInfo> QcTests => ViewModel.Tests.AsQueryable();

    protected override async Task OnInitializedAsync()
    {
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        ViewModel.OnShowToast += ShowToast;

        if (!string.IsNullOrEmpty(Barcode))
        {
            await ViewModel.LoadQcDataAsync(Barcode);

            if (ViewModel.QcCartridgeInfo == null)
            {
                // Close modal if parsing completely failed
                await Modal.CloseAsync(ModalResult.Cancel());
            }
        }
    }

    private static bool CanOpenTest(QcCartridgeTestInfo test)
    {
        return test is { QcTestId: not null, QcTestEnabled: true };
    }

    private static string GetTestLink(int? qcTestId)
    {
        return qcTestId.HasValue
        ? $"{PageRoute.Quality.MethodQualityControl.Default}/{qcTestId}"
        : PageRoute.Quality.MethodQualityControl.Default;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        StateHasChanged();
    }

    private void ShowToast()
    {
        ToasterService.ShowToast(ToastType.Information, Loc["QC_Ranges_Registered"]);
    }

    public void Dispose()
    {
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        GC.SuppressFinalize(this);
    }

    public string GetTargetText(ICollection<QcCartridgeTestLimit> limits)
    {
        if (limits == null)
        {
            return Loc["QcCartridgeInfo_NoTarget_Text"];
        }

        var equalLimit = limits.FirstOrDefault(l => l.LimitType == QcCartridgeLimitType.Equal);
        return equalLimit != null ? $"{equalLimit.LimitValue}" : Loc["QcCartridgeInfo_NoTarget_Text"];
    }
}
