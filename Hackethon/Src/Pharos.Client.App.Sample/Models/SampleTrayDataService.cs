namespace Pharos.Client.App.Sample.Models;

internal class SampleTrayDataService : ISampleTrayDataService, ISupportsRowLimitBypass
{
    private readonly SampleTrayClient _client;

    public SampleTrayDataService(HttpClient httpClient)
    {
        _client = new SampleTrayClient(httpClient);
    }

    public async Task<DataSourceResult<SampleTray>> GetAsync(DataSourceRequest request)
    {
        string? trayId = null;
        List<string>? trayLocation = null;
        var hasTubeIssues = false;

        SetFilter(request, ref trayId, ref trayLocation, ref hasTubeIssues);

        var result = await _client.GetAllAsync(trayId, trayLocation, hasTubeIssues, request.RequiresRowLimitBypass, request.StartIndex, request.Count ?? 0, request.SortByProperty, request.SortByAscending);

        return new DataSourceResult<SampleTray>(result.Items.ToList(), result.TotalCount);
    }

    private static void SetFilter(DataSourceFilterRequest request, ref string? trayId, ref List<string>? trayLocation, ref bool hasTubeIssues)
    {
        trayLocation ??= new List<string>();

        foreach (var filter in request.Filters)
        {
            switch (filter.PropertyName)
            {
                case nameof(SampleTray.Id):
                    if (filter.FilterOperator == FilterOperator.Contains)
                    {
                        trayId = filter.FilterValue as string;
                    }
                    break;

                case nameof(SampleTray.InstrumentName):
                    if (filter.FilterOperator == FilterOperator.EqualsTo && filter.FilterValue is string name)
                    {
                        trayLocation.Add(name == SampleConstants.NotLoaded ? string.Empty : name);
                    }
                    break;

                case nameof(SampleTray.HasTubeIssues):
                    if (filter.FilterOperator == FilterOperator.Contains && filter.FilterValue is bool hasIssues)
                    {
                        hasTubeIssues = hasIssues;
                    }
                    break;

                default:
                    throw new NotSupportedException($"Unsupported filter: {filter.PropertyName} with operator {filter.FilterOperator}");
            }
        }
    }

    public async Task<SampleTray> GetByIdAsync(string entityId)
    {
        return await _client.GetSampleTrayAsync(entityId);
    }

    public async Task<string> CreateAsync(SampleTray value)
    {
        var command = new CreateSampleTrayCommand() { TrayId = value.Id };
        return await _client.CreateSampleTrayAsync(command);
    }

    public Task UpdateAsync(SampleTray value)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(string entityId)
    {
        throw new NotImplementedException();
    }

    public Task<DeleteResponse> DeleteAsync(string[] entityIds)
    {
        throw new NotImplementedException();
    }

    public async Task<List<string>> ClearTraysAsync(string[] entityIds)
    {
        var result = await _client.ClearSampleTraysAsync(entityIds);
        return result.ToList();
    }

    public async Task<List<string>> DeleteTraysAsync(string[] entityIds)
    {
        var result = await _client.DeleteSampleTrayAsync(entityIds);
        return result.ToList();
    }

    public Task SetTrayNotInInstrumentAsync(string entityId)
    {
        return _client.SetTrayNotInInstrumentAsync(entityId);
    }
}