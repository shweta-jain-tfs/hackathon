namespace Pharos.Client.App.Parameters.LoadListConfiguration.Models;

public class LoadListConfigurationsDataService : IEntityDataService<Client.Common.Models.LoadListConfiguration>, ISupportsRowLimitBypass
{
    private readonly HttpClient _httpClient;

    public LoadListConfigurationsDataService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<Client.Common.Models.LoadListConfiguration> GetByIdAsync(int entityId)
    {
        throw new NotImplementedException();
    }

    public async Task<DataSourceResult<Client.Common.Models.LoadListConfiguration>> GetAsync(DataSourceRequest request)
    {
        var instrument = "";
        LoadListItemType? loadListItemType = null;
        var friendlyName = "";

        var client = new LoadListClient(_httpClient);

        SetFilter(request, ref instrument, ref loadListItemType, ref friendlyName);
        var items =
            await client.GetLoadListConfigurationsAsync(instrument, loadListItemType, friendlyName, request.RequiresRowLimitBypass, request.StartIndex, request.Count, request.SortByProperty, request.SortByAscending);

        return new DataSourceResult<Client.Common.Models.LoadListConfiguration>(items.Items.ToList(), items.TotalCount);
    }

    public Task<int> CreateAsync(Client.Common.Models.LoadListConfiguration value)
    {
        throw new NotImplementedException();
    }

    public Task UpdateAsync(Client.Common.Models.LoadListConfiguration value)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(int entityId)
    {
        throw new NotImplementedException();
    }

    public Task<DeleteResponse> DeleteAsync(int[] entityIds)
    {
        throw new NotImplementedException();
    }

    private static void SetFilter(DataSourceFilterRequest request, ref string? instrument, ref LoadListItemType? loadListItemType, ref string? friendlyName)
    {
        foreach (var f in request.Filters)
        {
            if (f.PropertyName == nameof(Client.Common.Models.LoadListConfiguration.InstrumentSerial))
            {
                instrument = f.FilterValue as string;
                continue;
            }

            if (f.PropertyName == nameof(Client.Common.Models.LoadListConfiguration.LoadListItemType))
            {
                switch (f.FilterOperator)
                {
                    case FilterOperator.EqualsTo:

                        if (Enum.TryParse(f.FilterValue?.ToString()?.Trim(), ignoreCase: true, out LoadListItemType result))
                        {
                            loadListItemType = result;
                        }

                        continue;
                }
            }

            if (f.PropertyName == nameof(Client.Common.Models.LoadListConfiguration.FriendlyName))
            {
                friendlyName = f.FilterValue as string;
                continue;
            }

            throw new NotSupportedException(
                $"The specified filter is not supported by:  {nameof(DataSourceFilter.PropertyName)}={f.PropertyName}, {nameof(DataSourceFilter.FilterOperator)} = {f.FilterOperator}");
        }
    }

    public async Task<RefillLevelInfo> GetTotalRefillLevel(LoadListItemType loadListItemType, string selectedInstrument)
    {
        var client = new LoadListClient(_httpClient);
        return await client.GetTotalRefillLevelAsync(loadListItemType, selectedInstrument);
    }

    public async Task UpdateLevelsAsync(List<int> updatedConfigurations, int? refillLevel, int? triggerLevel)
    {
        var client = new LoadListClient(_httpClient);

        var command = new UpdateLoadListConfigurationLevelsCommand
        {
            UpdatedConfigurationsIds = updatedConfigurations,
            RefillLevel = refillLevel,
            TriggerLevel = triggerLevel,
        };

        await client.UpdateLoadListLevelConfigurationsAsync(command);
    }

    public async Task CopyLevelsAsync(string sourceInstrument, HashSet<string> selectedInstruments)
    {
        var client = new LoadListClient(_httpClient);

        var command = new CopyLoadListLevelsCommand
        {
            SourceInstrument = sourceInstrument,
            TargetInstruments = selectedInstruments
        };

        await client.CopyLoadListLevelsAsync(command);
    }
}