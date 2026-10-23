using System.Globalization;
using Pharos.Client.App.Request.ViewModels;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.App.Validation;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Request.Views.RequestDetail;

public partial class TestEditView : ComponentBase
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;

    [Inject] private IInstrumentDilutionService InstrumentDilutionService { get; set; } = null!;

    [CascadingParameter] public ModalInstance Modal { get; set; } = default!;

    [Parameter] public TestOrderViewModel RequestTest { get; set; } = default!;

    [Parameter] public bool IsEditMode { get; set; } = true;

    [Parameter] public int PreDilutionFactor { get; set; } = 1;

    private int TotalDilution => PreDilutionFactor * RequestTest.InstrumentDilutionFactor;

    private readonly List<DropDownOption<int>> _instrumentDilutionValues = [];
    private PhValidationContext? _validation;

    protected override async Task OnInitializedAsync()
    {
        if (IsEditMode)
        {
            var instrumentDilutions = await InstrumentDilutionService.GetDilutionFactors(RequestTest.MethodName, RequestTest.TestName, PreDilutionFactor);

            _instrumentDilutionValues.Clear();
            _instrumentDilutionValues.AddRange(instrumentDilutions.Where(x => x.IsValid).Select(x => new DropDownOption<int>
            {
                Text = x.DilutionFactor.ToString(CultureInfo.InvariantCulture),
                Value = x.DilutionFactor
            }).ToList());
        }
    }

    private async Task OnClose(ModalResult result)
    {
        await Modal.CloseAsync(result);
    }

    private async Task ValidateAndSave(Microsoft.AspNetCore.Components.Web.MouseEventArgs args)
    {
        if (!_validation?.Validate() ?? false)
        {
            return;
        }

        await OnClose(ModalResult.Ok());
    }

    private bool IsValid()
    {
        return IsEditMode && (_validation?.IsValid() ?? true) && RequestTest.IsModified;
    }
}