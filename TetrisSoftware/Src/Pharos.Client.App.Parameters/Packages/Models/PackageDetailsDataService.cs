namespace Pharos.Client.App.Parameters.Packages.Models;

internal class PackageDetailsDataService(HttpClient httpClient) : IEntityDataService<PackageDetails>
{
    public async Task<PackageDetails> GetByIdAsync(int id)
    {
        var client = new PackagesClient(httpClient);
        var dto = await client.GetPackageDetailsAsync(id);

        return dto;
    }
    
    public Task<DataSourceResult<PackageDetails>> GetAsync(DataSourceRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<int> CreateAsync(PackageDetails value)
    {
        throw new NotImplementedException();
    }

    public Task UpdateAsync(PackageDetails value)
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
}
