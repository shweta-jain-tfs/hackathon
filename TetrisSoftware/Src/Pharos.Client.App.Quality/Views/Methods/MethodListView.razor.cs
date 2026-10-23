using Microsoft.Extensions.Localization;
using Pharos.Client.App.Quality.ViewModels;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.ViewModels;
using Pharos.Common.Web.Components.Status;

namespace Pharos.Client.App.Quality.Views.Methods;

public partial class MethodListView : IDisposable
{
    [Inject] private MethodListViewModel ViewModel { get; set; } = default!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] private InstrumentFilterViewModel InstrumentFilterModel { get; set; } = default!;

    private Dictionary<string, string> _methodNameDict = [];

    protected override async Task OnInitializedAsync()
    {
        await InstrumentFilterModel.LoadAsync();
        _methodNameDict = await ViewModel.GetMethodNamesAsync();
        ViewModel.ViewRefreshNeeded += OnViewRefreshNeeded;
    }

    private void OnViewRefreshNeeded(object? sender, EventArgs e)
    {
        InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        ViewModel.ViewRefreshNeeded -= OnViewRefreshNeeded;
        ViewModel.Dispose();
        GC.SuppressFinalize(this);
    }

    private static StatusType GetCalibratorsExpiresSymbol(MethodQuality methodQuality)
    {
        // No symbol used at the moment...
        _ = methodQuality;

        return StatusType.None;
    }

    private string GetCalibratorsExpiresText(string conjugateLotNumber, bool curveControlsFailed, int maxNumberOfDays, DateTimeOffset measurementDateTime)
    {
        if (string.IsNullOrEmpty(conjugateLotNumber) || curveControlsFailed)
        {
            return Loc["MethodQualityList_NoValidCurve"];
        }

        var validDate = measurementDateTime.AddDays(maxNumberOfDays);
        var daysLeft = validDate - DateTimeOffset.UtcNow;
        var expiryDate = validDate.LocalDateTime.ToShortDateString();

        var validDateText = daysLeft.TotalDays switch
        {
            < 1 => string.Format(Loc["MethodQualityList_ValidTextLessThanOneDay"], expiryDate),
            < 2 => string.Format(Loc["MethodQualityList_ValidTextOneDay"], expiryDate),
            _ => string.Format(Loc["MethodQualityList_ValidText"], expiryDate, daysLeft.Days)
        };

        return validDateText;
    }

    private string GetQualityIssueText(MethodQualityIssue methodQualityIssue)
    {
        var text = methodQualityIssue switch
        {
            MethodQualityIssue.NoIssues => string.Empty,
            MethodQualityIssue.LastCcFailed => Loc["MethodQualityList_LastCcFailed"],
            MethodQualityIssue.LastCcFlagged => Loc["MethodQualityList_LastCcFlagged"],
            MethodQualityIssue.LastQcFailed => Loc["MethodQualityList_LastQcFailed"],
            MethodQualityIssue.CurveExpired => Loc["MethodQualityList_CurveExpired"],
            _ => throw new ArgumentOutOfRangeException(nameof(methodQualityIssue))
        };

        return text;
    }
}