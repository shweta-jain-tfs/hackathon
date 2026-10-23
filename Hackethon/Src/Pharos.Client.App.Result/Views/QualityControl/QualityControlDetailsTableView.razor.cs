using Microsoft.Extensions.Localization;
using Pharos.Client.App.Result.Models;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.ViewModels.Interfaces;
using Pharos.Common.Web.Components.Modal;
using Pharos.Contracts.Security;

namespace Pharos.Client.App.Result.Views.QualityControl;

public partial class QualityControlDetailsTableView
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] private IModalService Modal { get; set; } = default!;
    [Inject] private IExceptionHandlerService ExceptionHandlerService { get; set; } = default!;
    [Inject] private IQualityControlDataSource DataService { get; set; } = default!;
    [Inject] private IAuthorizationService AuthorizationService { get; set; } = default!;

    [Parameter]
    public List<QualityControlTestResult>? QcTestResults { get; set; }

    private static StatusType GetStatusSymbol(QualityControlJudgement? judgment)
    {
        return judgment switch
        {
            QualityControlJudgement.Ok => StatusType.None,
            QualityControlJudgement.FlaggedHigh or QualityControlJudgement.FlaggedLow => StatusType.Warning,
            _ => StatusType.Alarm
        };
    }

    private async Task OpenDetailsView(QualityControlTestResult testResult)
    {
        var hasPermission = await AuthorizationService.IsAuthorized(Permissions.ApproveTestResults);

        var parameters = new ModalParameters
        {
            { nameof(QualityControlTestResultDetailsModalView.CanEdit), hasPermission},
            { nameof(QualityControlTestResultDetailsModalView.TestResult), testResult}
        };

        var modalOptions = new ModalOptions
        {
            ModalHeight = ModalSize.Large,
            ModalWidth = ModalSize.Large,
            Title = Loc["QualityControlTestResultDetailsModalView_Title"]
        };

        var modal = Modal.Show<QualityControlTestResultDetailsModalView>(modalOptions, parameters);
        var result = await modal.Result;

        if (result.Canceled || result.Data == null)
        {
            return;
        }

        try
        {
            await DataService.UpdateQualityControlAsync((result.Data as QualityControlTestResult)!);

            StateHasChanged();
        }
        catch (Exception ex)
        {
            // Keep page interactive on unexpected error in this action.
            ExceptionHandlerService.ShowExceptionMessage(ex);
        }
    }

    private static string GetConcentrationText(QualityControlTestResult context, string concentration)
    {
        var conc = concentration.FormatDecimalForDisplay();

        if (string.IsNullOrWhiteSpace(conc))
        {
            return string.Empty;
        }

        return $"{conc} {context.Unit}";
    }

    private static string GetConcentrationText(QualityControlTestResult context, double? expectedConcentration)
    {
        if (expectedConcentration.HasValue)
        {
            return GetConcentrationText(context, expectedConcentration.Value.ToString());
        }

        return string.Empty;
    }
}