namespace Pharos.Client.App.Parameters.Panels.Models;

public interface IMethodPanelsDataSource : IPagedDataSource<MethodPanel>, ISupportsRowLimitBypass
{
    Task<bool> CheckPanelsReferenced(int[] ids);

    Task<DeleteResponse> DeleteAsync(int[] entityIds, bool removeTestPanelReferences);
}

internal class MethodPanelsDataSource(HttpClient httpClient) : IMethodPanelsDataSource
{
    public async Task<DataSourceResult<MethodPanel>> GetAsync(DataSourceRequest request)
    {
        var client = new PanelsClient(httpClient);
        var result = await client.GetMethodPanelsAsync();

        return new DataSourceResult<MethodPanel>(result.ToList(), result.Count);
    }

    public async Task<DeleteResponse> DeleteAsync(int[] entityIds, bool removeTestPanelReferences)
    {
        var client = new PanelsClient(httpClient);
        return await client.DeleteMethodPanelsAsync(new DeletePanelsCommand
        {
            Ids = entityIds,
            RemoveTestPanelReferences = removeTestPanelReferences
        });
    }

    public async Task<bool> CheckPanelsReferenced(int[] ids)
    {
        var client = new PanelsClient(httpClient);
        return await client.CheckMethodPanelsReferencedAsync(ids);
    }
}
