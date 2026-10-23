using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.InstrumentManagement.ViewModels.Dashboard;
using Pharos.Client.Common.Localization;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.InstrumentManagement.Views.Dashboard;

public partial class AssaySelectRunModalView
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;

    [CascadingParameter] public ModalInstance Modal { get; set; } = default!;

    [Parameter] public required AssaySelectRunViewModel ViewModel { get; set; }

    private enum WhatToRunOption
    {
        CurveControls,
        NewCalibrationCurve
    }

    private WhatToRunOption _selectedWhatToRun;

    protected override void OnInitialized()
    {
        // If Calibrator is true: "New calibration curve" is forced
        if (ViewModel.Calibrator)
        {
            _selectedWhatToRun = WhatToRunOption.NewCalibrationCurve;
        }
        // If Calibrator is false: selection is based on ForceCalibrator
        else
        {
            _selectedWhatToRun = ViewModel.ForceCalibrator
                ? WhatToRunOption.NewCalibrationCurve
                : WhatToRunOption.CurveControls;
        }
    }

    private string GetCalibrationValidity()
    {
        if (ViewModel.CalibrationValidForDays == 0)
        {
            return Loc["DashboardPage_AssaySelectRunModal_Calibration_Validity_Not_Valid"];
        }

        return string.Format(Loc["DashboardPage_AssaySelectRunModal_Calibration_Validity_Days"], ViewModel.CalibrationValidForDays);
    }

    private async Task CloseModal(ModalResult result)
    {
        if (!result.Canceled)
        {
            // Only update ForceCalibrator when Calibrator is false
            if (!ViewModel.Calibrator)
            {
                // Set ForceCalibrator based on what the user selected
                ViewModel.ForceCalibrator = _selectedWhatToRun == WhatToRunOption.NewCalibrationCurve;
            }
        }

        await Modal.CloseAsync(result);
    }
}
