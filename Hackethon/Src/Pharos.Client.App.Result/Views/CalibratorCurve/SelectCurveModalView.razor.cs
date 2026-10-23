using Microsoft.Extensions.Localization;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Result.Views.CalibratorCurve;

public partial class SelectCurveModalView
{
    [CascadingParameter] public ModalInstance Modal { get; set; } = default!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Parameter] public List<AvailableCurve> AvailableCurves { get; set; } = default!;

    private int? _selectedId;
    private async Task CloseModal(ModalResult result)
    {
        await Modal.CloseAsync(result);
    }
    private void SelectedCurveChanged(AvailableCurve selectedCurve)
    {
        _selectedId = selectedCurve.Id;
    }
}
