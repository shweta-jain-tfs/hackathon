using Microsoft.Extensions.Localization;
using Pharos.Common.Web.Components.FormElements;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Result.Views.Result;
public partial class RejectSingleResultModalView
{
    [CascadingParameter] public ModalInstance Modal { get; set; } = default!;

    [Parameter] public AnalyticalRunTestResult? TestToReject { get; set; }
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Parameter] public List<DropDownOption<int>>? ValidDilutions { get; set; }
    [Parameter] public int OriginalInstrumentDilution { get; set; }
    [Parameter] public int OriginalPreDilution { get; set; }

    public int NewInstrumentDilution { get; set; }
    public bool CanReject => TestToReject != null;
    private bool AddNewTest { get; set; }
    private DilutionOption SelectedDilutionOption { get; set; }

    protected override void OnParametersSet()
    {
        NewInstrumentDilution = OriginalInstrumentDilution;
    }

    private async Task CloseModal(ModalResult result)
    {
        await Modal.CloseAsync(result);
    }
    private DilutionOption? GetReturnDilutionOption()
    {
        return AddNewTest ? SelectedDilutionOption : null;
    }
}
public enum DilutionOption
{
    SameDilutions,
    ChangeInstrumentDilution,
}