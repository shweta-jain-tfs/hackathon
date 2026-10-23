using System.Globalization;
using Pharos.Client.App.Parameters.Panels.ViewModels;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.App.Validation;
using Pharos.Common.Web.Components.FormElements;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Parameters.Panels.Views;

public partial class PanelTestEditView : ComponentBase
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] private IInstrumentDilutionService InstrumentDilutionService { get; set; } = default!;
    [CascadingParameter] public ModalInstance Modal { get; set; } = default!;

    [Parameter, EditorRequired] public PanelTestViewModel ViewModel { get; set; } = default!;
    [Parameter] public bool IsEditingDisabled { get; set; }

    private readonly List<DropDownOption<int>> _instrumentDilutionValues = [];
    private PhValidationContext? _validation;

    protected override async Task OnInitializedAsync()
    {
        var instrumentDilutions = await InstrumentDilutionService.GetDilutionFactors(ViewModel.MethodName ?? string.Empty, ViewModel.TestName ?? string.Empty, 1);

        _instrumentDilutionValues.Clear();
        _instrumentDilutionValues.AddRange(instrumentDilutions.Where(x => x.IsValid).Select(x => new DropDownOption<int>
        {
            Text = x.DilutionFactor.ToString(CultureInfo.InvariantCulture),
            Value = x.DilutionFactor
        }).ToList());
    }

    private async Task OnClose(ModalResult result)
    {
        await Modal.CloseAsync(result);
    }

    private async Task ValidateAndSave()
    {
        if (IsEditingDisabled)
        {
            return;
        }

        if (!_validation?.Validate() ?? false)
        {
            return;
        }

        await OnClose(ModalResult.Ok(ViewModel));
    }

    private bool IsValid()
    {
        return _validation?.IsValid() ?? true;
    }
}
