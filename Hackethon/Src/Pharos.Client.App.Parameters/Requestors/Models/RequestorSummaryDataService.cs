namespace Pharos.Client.App.Parameters.Requestors.Models;

internal sealed class RequestorSummaryDataService : IEntityDataService<RequestorSummary>, ISupportsRowLimitBypass
{
    private readonly HttpClient _httpClient;

    public RequestorSummaryDataService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<DataSourceResult<RequestorSummary>> GetAsync(DataSourceRequest request)
    {
        string? requestorId = null;
        string? name = null;

        var client = new RequestorsClient(_httpClient);
        SetFilter(request, ref requestorId, ref name);

        var result = await client.GetRequestorsAsync(
            requestorId,
            name,
            request.RequiresRowLimitBypass,
            request.StartIndex,
            request.Count ?? 0,
            request.SortByProperty,
            request.SortByAscending);

        return new DataSourceResult<RequestorSummary>(result.Items.ToList(), result.TotalCount);
    }

    public Task<RequestorSummary> GetByIdAsync(int entityId)
    {
        throw new NotImplementedException();
    }

    private static void SetFilter(DataSourceFilterRequest request, ref string? requestorId, ref string? name)
    {
        foreach (var f in request.Filters)
        {
            if (f.PropertyName == nameof(RequestorSummary.RequestorId))
            {
                switch (f.FilterOperator)
                {
                    case FilterOperator.Contains:
                        requestorId = f.FilterValue as string;
                        break;
                }
            }
            if (f.PropertyName == nameof(RequestorSummary.Name))
            {
                switch (f.FilterOperator)
                {
                    case FilterOperator.Contains:
                        name = f.FilterValue as string;
                        break;
                }
            }
        }
    }

    public Task<int> CreateAsync(RequestorSummary value)
    {
        throw new NotImplementedException();
    }

    public Task UpdateAsync(RequestorSummary value)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(int entityId)
    {
        throw new NotImplementedException("");
    }

    public async Task<DeleteResponse> DeleteAsync(int[] entityIds)
    {
        var client = new RequestorsClient(_httpClient);
        return await client.DeleteRequestorsAsync(entityIds);
    }
}