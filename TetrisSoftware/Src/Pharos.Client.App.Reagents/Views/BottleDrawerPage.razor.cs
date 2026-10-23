using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.Reagents.ViewModels.BottleDrawer;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.Localization;
using Pharos.Common.Web.Components.Toaster;

namespace Pharos.Client.App.Reagents.Views;
public partial class BottleDrawerPage : IDisposable
{
    [Inject] private BottlePageViewModel ViewModel { get; set; } = default!;
    [Inject] private IToasterService ToasterService { get; set; } = default!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        ViewModel.OnShowToast += ShowToast;
        await ViewModel.LoadAsync();
    }
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        StateHasChanged();
    }
    public void Dispose()
    {
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        ViewModel.OnShowToast -= ShowToast;
        ViewModel.Dispose();
        GC.SuppressFinalize(this);
    }

    private string GetToastMessage(BottlePageViewModel.ToastType type)
    {
        return type switch
        {
            BottlePageViewModel.ToastType.Unauthorized => Loc["BottlesPage_LoadReagents_Toaster"],
            BottlePageViewModel.ToastType.NoPositionAvailable => Loc["BottlesPage_NoPositionAvailable_Toaster"],
            BottlePageViewModel.ToastType.MismatchedBottleType => Loc["BottlesPage_MismatchedBottle_Toaster"],
            BottlePageViewModel.ToastType.MultipleScanDetected => Loc["BottlesPage_MultipleScan_Toaster"],
            BottlePageViewModel.ToastType.IncorrectBottlePlacement => Loc["BottlesPage_IncorrectPlacement_Toaster"],
            BottlePageViewModel.ToastType.ScanBottleBeforeLoading => Loc["BottlesPage_ScanBottleBeforeLoading_Toaster"],
            BottlePageViewModel.ToastType.IncorrectBarcodeFormat => Loc["BottlesPage_IncorrectBarcodeFormat_Toaster"],
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, Loc["BottlePage_UnknownToaster"]),
        };
    }

    private void ShowToast(BottlePageViewModel.ToastType type)
    {
        ToasterService.ShowToast(ToastType.Information, GetToastMessage(type));
    }
}