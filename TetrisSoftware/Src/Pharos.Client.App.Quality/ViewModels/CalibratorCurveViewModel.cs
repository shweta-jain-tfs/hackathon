using Pharos.Client.App.Quality.Models;
using Pharos.Client.Common.ViewModels;
using Pharos.Common.Web.Components.Status;

namespace Pharos.Client.App.Quality.ViewModels;

public class CalibratorCurveViewModel(ICurvesDataSource dataSource) : LoadingViewModel
{
    public ICollection<CurvePresentation>? Data { get; set; }
    public bool IsLoaded { get; set; }

    public string InstrumentNumber { get; set; } = string.Empty;
    public string MethodName { get; set; } = string.Empty;
    public StatusType Status { get; set; } = StatusType.None;

    protected override async Task DoLoadAsync()
    {
        Data = await dataSource.GetCurrentCurves(InstrumentNumber, MethodName);
        
        if (Data is { Count: > 0 })
        {
            if (Data.Any(x => x.Judgment == CurveJudgment.NotOK))
            {
                Status = StatusType.Alarm;
            }
            else if (Data.Any(x => x.Judgment == CurveJudgment.Flagged))
            {
                Status = StatusType.Warning;
            }
            else
            {
                Status = StatusType.Success;
            }
        }

        IsLoaded = true;
    }
}