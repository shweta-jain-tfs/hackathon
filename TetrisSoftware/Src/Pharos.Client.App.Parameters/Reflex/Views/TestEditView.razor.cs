using System.Globalization;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.App.Validation;
using Pharos.Common.Web.Components.FormElements;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Parameters.Reflex.Views;

public partial class TestEditView : ComponentBase
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = null!;
    [Inject] private IInstrumentDilutionService InstrumentDilutionService { get; set; } = null!;
    [CascadingParameter] public ModalInstance Modal { get; set; } = null!;

    [Parameter] public ReflexTest ReflexTest { get; set; } = null!;
    [Parameter] public bool CanEdit { get; set; }

    private readonly List<DropDownOption<int>> _instrumentDilutionValues = [];
    private PhValidationContext? _validation;

    protected override async Task OnInitializedAsync()
    {
        var instrumentDilutions = await InstrumentDilutionService.GetDilutionFactors(ReflexTest.MethodName, ReflexTest.TestName, 1);

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
        if (!_validation?.Validate() ?? false)
        {
            return;
        }

        await OnClose(ModalResult.Ok());
    }

    private bool IsValid()
    {
        return _validation?.IsValid() ?? true;
    }
}