using Microsoft.Extensions.Localization;
using Pharos.Client.App.Quality.ViewModels;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.Localization;
using Pharos.Common.Web.Components.Modal;
using Pharos.Common.Web.Components.Toaster;

namespace Pharos.Client.App.Quality.Views.QualityControls;

public partial class MethodQualityControlLotsTabContent
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] private IModalService Modal { get; set; } = default!;
    [Inject] private IToasterService Toaster { get; set; } = default!;

    [Parameter] public required MethodQualityControlDetailsViewModel ViewModel { get; set; }

    private HashSet<QualityControlLot> _selectedItems = new();

    private bool IsDeleteAvailable => _selectedItems.Count > 0;

    private void OnSelectedItemsChanged(HashSet<QualityControlLot> selectedItems)
    {
        _selectedItems = selectedItems;
    }

    private string QcDefaultString(QualityControlLot lot)
    {
        if (lot.DefaultUsesRange)
        {
            return string.Format(Loc["MethodQualityControlDetails_AcceptanceRangeDescription"], lot.ManufacturingRangeMin, lot.ManufacturingRangeMax);
        }

        if (lot.DefaultUsesTarget)
        {
            return string.Format(@Loc["MethodQualityControlDetails_TargetValueDescription"], lot.ExpectedConcentration, lot.ExpectedStdDev, lot.StdDevLimit);
        }

        return string.Empty;
    }

    private string CurrentLotValue(QualityControlLot lot)
    {
        return lot.LotType switch
        {
            LotType.TargetValue => string.Format(@Loc["MethodQualityControlDetails_TargetValueDescription"], lot.ExpectedConcentration, lot.ExpectedStdDev, lot.StdDevLimit),
            LotType.AcceptanceRange => string.Format(@Loc["MethodQualityControlDetails_AcceptanceRangeDescription"], lot.RangeMin, lot.RangeMax),
            LotType.UseDefaultQcValues => QcDefaultString(lot),
            _ => string.Empty
        };
    }

    private string AcceptanceCriteriaLot(QualityControlLot lot)
    {
        return lot.LotType switch
        {
            LotType.TargetValue => Loc["MethodQualityControlDetails_AcceptanceCriteriaTargetValue"],
            LotType.AcceptanceRange => Loc["MethodQualityControlDetails_AcceptanceCriteriaRange"],
            LotType.UseDefaultQcValues => GetDefaultAcceptanceCriteria(lot),
            _ => string.Empty
        };
    }

    private string GetDefaultAcceptanceCriteria(QualityControlLot lot)
    {
        if (lot.DefaultUsesRange)
        {
            return Loc["MethodQualityControlDetails_DefaultRange"];
        }

        if (lot.DefaultUsesTarget)
        {
            return Loc["MethodQualityControlDetails_DefaultTarget"];
        }

        return string.Empty;
    }

    private async Task AddNewLot()
    {
        var options = new ModalOptions
        {
            Title = Loc["MethodQualityControlDetails_AddModalTitle"],
            ModalHeight = ModalSize.Large,
            ModalWidth = ModalSize.Small
        };
        var parameters = new ModalParameters
        {
            { nameof(AddLotModalView.IsUseDefined), ViewModel.Model.IsUserDefined }
        };
        var modalRef = Modal.Show<AddLotModalView>(options, parameters);
        var result = await modalRef.Result;
        if (!result.Canceled && result.Data is QualityControlLot lot && !string.IsNullOrEmpty(lot.LotNumber))
        {
            var toasterText = string.Format(@Loc["MethodQualityControlDetails_CreateToster"], lot.LotNumber);
            await ViewModel.CreateNewLot(lot);
            Toaster.ShowToast(ToastType.Confirmation, toasterText);
        }
    }

    private async Task EditLot(int lotId)
    {
        var lot = ViewModel.Model.Lots.FirstOrDefault(l => l.Id == lotId);
        if (lot == null)
        {
            return;
        }

        var options = new ModalOptions
        {
            Title = Loc["MethodQualityControlDetails_AddModalTitle"],
            ModalHeight = ModalSize.Large,
            ModalWidth = ModalSize.Small
        };

        var parameters = new ModalParameters
        {
            { nameof(EditLotModalView.LotToEdit), lot },
            { nameof(EditLotModalView.IsUseDefined), ViewModel.Model.IsUserDefined }
        };

        var modalRef = Modal.Show<EditLotModalView>(options, parameters);
        var result = await modalRef.Result;
        if (!result.Canceled && result.Data is QualityControlLot updatedLot)
        {
            var toasterText = string.Format(Loc["MethodQualityControlDetails_EditToster"], updatedLot.LotNumber, AcceptanceCriteriaLot(updatedLot));
            await ViewModel.UpdateLot(updatedLot);
            Toaster.ShowToast(ToastType.Confirmation, toasterText);
        }
    }

    private async Task DeleteSelectedLots()
    {
        var options = new ModalOptions
        {
            Title = Loc["MethodQualityControlDetails_DeleteModalTitle"],
            ModalHeight = ModalSize.Small,
            ModalWidth = ModalSize.Small
        };

        var lotNumbers = _selectedItems.Select(i => i.LotNumber).ToList();
        var parameters = new ModalParameters
        {
            { nameof(DeleteLotModalView.LotNumbers),  string.Join(", ", lotNumbers) },
        };

        var modalRef = Modal.Show<DeleteLotModalView>(options, parameters);
        var result = await modalRef.Result;

        if (!result.Canceled && result.Data is bool deleteConfirmed && deleteConfirmed)
        {
            var lotIds = _selectedItems.Select(lot => lot.Id).ToList();
            await ViewModel.DeleteLots(lotIds);
            var toasterText = string.Format(Loc["MethodQualityControlDetails_DeleteToster"], lotIds.Count, string.Join(", ", lotNumbers));
            Toaster.ShowToast(ToastType.Information, toasterText);
            _selectedItems.Clear();
        }
    }
}
