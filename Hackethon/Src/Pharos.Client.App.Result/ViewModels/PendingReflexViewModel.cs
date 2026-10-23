using Pharos.Client.App.Result.Models;

namespace Pharos.Client.App.Result.ViewModels;

public class PendingReflexViewModel
{
    private readonly ISampleTestResultDataService _dataService;
    
    public PendingReflexViewModel(ISampleTestResultDataService dataService)
    {
        _dataService = dataService;
    }

    public async Task AddPendingReflexTests(ICollection<PendingReflex> selectedTests)
    {
        if (selectedTests.Count == 0)
        {
            return;
        }

        var reflexIds = selectedTests.Select(x => x.Id).ToArray();

        await _dataService.HandlePendingReflexes(reflexIds);
    }

    public async Task DiscardPendingReflexTests(ICollection<PendingReflex> selectedTests)
    {
        if (selectedTests.Count == 0)
        {
            return;
        }

        var reflexIds = selectedTests.Select(x => x.Id).ToArray();

        await _dataService.HandlePendingReflexes(reflexIds, true);
    }

    public static string FormatResultDisplay(PendingReflex pendingReflex)
    {
        if (pendingReflex.ResultType == null)
        {
            return string.Empty;
        }

        var resultTypeText = pendingReflex.ResultType.ToString() ?? string.Empty;
        
        // Use the ResultValue and ResultUnit that were populated from TestResultWithReflex
        if (!string.IsNullOrEmpty(pendingReflex.ResultValue))
        {
            if (!string.IsNullOrEmpty(pendingReflex.ResultUnit))
            {
                return $"{resultTypeText} = {pendingReflex.ResultValue} {pendingReflex.ResultUnit}";
            }

            return $"{resultTypeText} = {pendingReflex.ResultValue}";
            
        }
        
        return resultTypeText;
    } 
}