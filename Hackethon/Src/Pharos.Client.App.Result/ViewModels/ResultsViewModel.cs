using Pharos.Client.Common.ViewModels;

namespace Pharos.Client.App.Result.ViewModels;
public class ResultsViewModel : LoadingViewModel
{
    public int AnalyticalRunId { get; set; }
    public string InstrumentSerial { get; set; } = string.Empty;
    public string MethodName { get; set; } = string.Empty;
    public bool Miniature { get; set; }

    protected override Task DoLoadAsync()
    {
        return Task.CompletedTask;
    }
}