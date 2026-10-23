namespace Pharos.Client.App.Parameters.Reflex.Models;

public interface IReflexDataService : IEntityDataService<ReflexDefinitionSummary>
{
    Task SetActiveReflexDefinitionAsync(int entityId, bool active);
}

internal sealed class ReflexDefinitionSummaryDataService : IReflexDataService
{
    private readonly HttpClient _httpClient;

    public ReflexDefinitionSummaryDataService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<int> CreateAsync(ReflexDefinitionSummary value)
    {
        throw new NotImplementedException("");
    }

    public Task DeleteAsync(int entityId)
    {
        throw new NotImplementedException("");
    }

    public async Task<DeleteResponse> DeleteAsync(int[] entityIds)
    {
        var client = new ReflexClient(_httpClient);
        await client.DeleteReflexDefinitionAsync(entityIds);

        return new DeleteResponse();
    }

    public async Task<DataSourceResult<ReflexDefinitionSummary>> GetAsync(DataSourceRequest request)
    {
        string? definitionName = null;
        bool? automatic = null;
        bool? active = null;
        string? requestorId = null;

        SetFilter(request, ref definitionName, ref automatic, ref active, ref requestorId);

        var client = new ReflexClient(_httpClient);

        var result = await client.GetReflexDefinitionsAsync(definitionName, automatic, active, requestorId, request.StartIndex, request.Count ?? 0, request.SortByProperty, request.SortByAscending);

        return new DataSourceResult<ReflexDefinitionSummary>(result.Items.ToList(), result.TotalCount);
    }

    private static void SetFilter(DataSourceRequest request, ref string? definitionName, ref bool? automatic, ref bool? active, ref string? requestorId)
    {
        foreach (var f in request.Filters)
        {
            if (f.PropertyName == nameof(ReflexDefinitionSummary.Name))
            {
                switch (f.FilterOperator)
                {
                    case FilterOperator.Contains:
                        definitionName = f.FilterValue as string;
                        continue;
                }
            }

            if (f.PropertyName == nameof(ReflexDefinitionSummary.Automatic))
            {
                switch (f.FilterOperator)
                {
                    case FilterOperator.EqualsTo:
                        automatic = f.FilterValue as bool?;
                        continue;
                }
            }

            if (f.PropertyName == nameof(ReflexDefinitionSummary.Active))
            {
                switch (f.FilterOperator)
                {
                    case FilterOperator.EqualsTo:
                        active = f.FilterValue as bool?;
                        continue;
                }
            }

            if (f.PropertyName == nameof(ReflexDefinitionSummary.RequestorId))
            {
                switch (f.FilterOperator)
                {
                    case FilterOperator.Contains:
                        requestorId = f.FilterValue as string;
                        continue;
                }
            }

            throw new NotSupportedException(
                $"The specified filter is not supported by:  {nameof(DataSourceFilter.PropertyName)}={f.PropertyName}, {nameof(DataSourceFilter.FilterOperator)} = {f.FilterOperator}");
        }
    }

    public Task<ReflexDefinitionSummary> GetByIdAsync(int entityId)
    {
        throw new NotImplementedException("");
    }

    public Task UpdateAsync(ReflexDefinitionSummary value)
    {
        throw new NotImplementedException("");
    }

    public async Task SetActiveReflexDefinitionAsync(int entityId, bool active)
    {
        var client = new ReflexClient(_httpClient);
        await client.SetActiveReflexDefinitionAsync(entityId, active);
    }
}