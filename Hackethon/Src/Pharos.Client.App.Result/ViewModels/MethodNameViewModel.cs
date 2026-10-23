using Pharos.Client.App.Result.Models;

namespace Pharos.Client.App.Result.ViewModels;
internal sealed class MethodNameViewModel(ISampleTestResultDataService resultDataService)
{
    public async Task<MethodAndInstrumentName>GetMethodNameForRun(int analyticalRunId)
    {
       return await resultDataService.GetMethodNameForAnalyticalRun(analyticalRunId);
    }
}