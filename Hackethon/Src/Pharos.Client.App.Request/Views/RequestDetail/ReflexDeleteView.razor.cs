using Pharos.Client.Common.App.Helpers;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Request.Views.RequestDetail;

public partial class ReflexDeleteView : ComponentBase
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;

    [CascadingParameter] public ModalInstance Modal { get; set; } = default!;

    [Parameter] public RequestReflexDefinition ReflexCopyDetails { get; set; } = default!;

    [Parameter] public TestOrderState TestOrderState { get; set; }
    [Parameter] public string DataGridTitleText { get; set; } = default!;

    private string FormattedCondition => ReflexCopyDetails.ReflexCondition != null
        ? ReflexConditionFormatter.Format(
            ReflexCopyDetails.ReflexCondition.TestName,
            ReflexCopyDetails.ReflexCondition.ResultType,
            ReflexCopyDetails.ReflexCondition.CompareType,
            ReflexCopyDetails.ReflexCondition.ConcentrationCompareValue,
            ReflexCopyDetails.ReflexCondition.ConcentrationCompareValueUpperLimit,
            ReflexCopyDetails.ReflexCondition.ConcentrationRawCompareValue,
            ReflexCopyDetails.ReflexCondition.ConcentrationRawCompareValueUpperLimit,
            ReflexCopyDetails.ReflexCondition.QuotientCompareValue,
            ReflexCopyDetails.ReflexCondition.QuotientCompareValueUpperLimit,
            ReflexCopyDetails.ReflexCondition.ClassCompareValue,
            ReflexCopyDetails.ReflexCondition.CutOffCompareValue,
            ReflexCopyDetails.ReflexCondition.RuCompareValue)
        : string.Empty;

    protected override void OnInitialized()
    {
    }

    private async Task OnClose(ModalResult result)
    {
        await Modal.CloseAsync(result);
    }
}