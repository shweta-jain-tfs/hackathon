namespace Pharos.Client.App.Request.Models;

public interface ISearchPatientDataSource
{
    Task<DataSourceResult<Patient>> GetAsync(DataSourceRequest request);
}

public class PatientDataSource : ISearchPatientDataSource
{
    private readonly HttpClient _httpClient;

    public PatientDataSource(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<Patient> GetByIdAsync(int entityId)
    {
        throw new NotImplementedException();
    }

    public async Task<DataSourceResult<Patient>> GetAsync(DataSourceRequest request)
    {
        var client = new PatientsClient(_httpClient);
        var result = await client.GetPatientAsync(request.Filters.First().FilterValue!.ToString(), request.StartIndex, request.Count, request.SortByProperty, request.SortByAscending);

        return new DataSourceResult<Patient>(result.Items.ToList(), result.TotalCount);
    }
}
