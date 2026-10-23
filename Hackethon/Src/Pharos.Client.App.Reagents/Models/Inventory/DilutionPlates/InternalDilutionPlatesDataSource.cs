using Pharos.Client.Common.Models;
using Pharos.Client.Common.Models.DataServices;

namespace Pharos.Client.App.Reagents.Models.Inventory.DilutionPlates;

internal class InternalDilutionPlatesDataSource : IEntityDataSource<InternalDilutionPlatesResult>, ISupportsRowLimitBypass
{
    private readonly HttpClient _httpClient;

    public InternalDilutionPlatesDataSource(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<DataSourceResult<InternalDilutionPlatesResult>> GetAsync(DataSourceRequest request)
    {
        var client = new InventoryClient(_httpClient);
        List<string>? selectedInstrument = [];
        Filter(request, selectedInstrument);
        var result = await client.GetInternalDilutionPlatesAsync(
            selectedInstrument,
            request.RequiresRowLimitBypass,
            request.StartIndex,
            request.Count,
            request.SortByProperty,
            request.SortByAscending,
            request.CancellationToken
        );
        return new DataSourceResult<InternalDilutionPlatesResult>(result.Items.ToList(), result.TotalCount);
    }

    private static void Filter(DataSourceFilterRequest request, List<string> instrumentNameList)
    {
        foreach (var f in request.Filters)
        {
            if (f.PropertyName == nameof(InternalDilutionPlate.Instrument))
            {
                switch (f.FilterOperator)
                {
                    case FilterOperator.EqualsTo:
                        instrumentNameList?.Add(f.FilterValue as string ?? string.Empty);
                        continue;
                }
            }

            throw new NotSupportedException(
                $"The specified filter is not supported by:  {nameof(DataSourceFilter.PropertyName)}={f.PropertyName}, {nameof(DataSourceFilter.FilterOperator)} = {f.FilterOperator}");
        }
    }

    Task<InternalDilutionPlatesResult> IGetByIdDataSource<InternalDilutionPlatesResult, int>.GetByIdAsync(int entityId)
    {
        throw new NotImplementedException();
    }
}