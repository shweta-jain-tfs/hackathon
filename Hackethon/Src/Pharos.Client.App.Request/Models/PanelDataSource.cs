namespace Pharos.Client.App.Request.Models;

internal sealed class PanelDataSource : IEntityDataSource<Panel>
{
    private readonly HttpClient _httpClient;

    public PanelDataSource(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<DataSourceResult<Panel>> GetAsync(DataSourceRequest request)
    {
        string? panelName = null;
        string? longPanelName = null;
        
        SetFilter(request, ref panelName, ref longPanelName);

        var client = new PanelsClient(_httpClient);
        var result = await client.GetPanelsAsync(panelName, longPanelName, request.StartIndex, request.Count ?? 0, request.SortByProperty, request.SortByAscending);

        return new DataSourceResult<Panel>(result.Items.ToList(), result.TotalCount);
    }

    public Task<Panel> GetByIdAsync(int entityId)
    {
        throw new NotSupportedException();
    }

    private static void SetFilter(DataSourceRequest request, ref string? panelName, ref string? longPanelName)
    {
        foreach (var f in request.Filters)
        {
            if (f.PropertyName == nameof(Panel.PanelName))
            {
                switch (f.FilterOperator)
                {
                    case FilterOperator.Contains:
                        panelName = f.FilterValue as string;
                        continue;
                }
            }

            if (f.PropertyName == nameof(Panel.PanelLongName))
            {
                switch (f.FilterOperator)
                {
                    case FilterOperator.Contains:
                        longPanelName = f.FilterValue as string;
                        continue;
                }
            }

            throw new NotSupportedException(
                $"The specified filter is not supported by:  {nameof(DataSourceFilter.PropertyName)}={f.PropertyName}, {nameof(DataSourceFilter.FilterOperator)} = {f.FilterOperator}");
        }
    }
}