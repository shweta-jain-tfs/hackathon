namespace Pharos.Client.App.Parameters.QualityControl.Models;

public interface IPhadiaQualityControlItemSource : IPagedDataSource<PhadiaQualityControl>, IDetailsPageDataService<PhadiaQualityControlDetail>
{
    Task UpdateEnabled(int id, bool enabled);
}

public class PhadiaQualityControlItemSource : IPhadiaQualityControlItemSource
{
    private readonly HttpClient _httpClient;

    public PhadiaQualityControlItemSource(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<DataSourceResult<PhadiaQualityControl>> GetAsync(DataSourceRequest request)
    {
        var client = new QualityControlsClient(_httpClient);
        var enabledOnly = true;
        List<string> methods = [];
        SetFilter(ref enabledOnly, ref methods, request.Filters);
        var result = await client.GetPhadiaQualityControlsAsync(enabledOnly, methods, request.StartIndex, request.Count, request.SortByProperty, request.SortByAscending);
        return new DataSourceResult<PhadiaQualityControl>(result.Items.ToList(), result.TotalCount, result.GroupedCount);
    }

    private static void SetFilter(ref bool enabledOnly, ref List<string> methods, ICollection<DataSourceFilter> requestFilters)
    {
        foreach (var f in requestFilters)
        {
            switch (f.PropertyName)
            {
                case nameof(PhadiaQualityControl.MethodName):
                    switch (f.FilterOperator)
                    {
                        case FilterOperator.EqualsTo:
                            methods?.Add(f.FilterValue as string ?? string.Empty);
                            continue;
                    }
                    break;
                case nameof(PhadiaQualityControl.Enabled):
                    enabledOnly = f.FilterValue as bool? ?? true;
                    break;
            }
        }
    }

    public async Task UpdateEnabled(int id, bool enabled)
    {
        var client = new QualityControlsClient(_httpClient);
        await client.UpdatePhadiaQualityControlEnabledAsync(id, enabled);
    }

    public async Task<PhadiaQualityControlDetail> GetByIdAsync(int id)
    {
        var client = new QualityControlsClient(_httpClient);
        return await client.GetPhadiaQualityControlDetailsAsync(id);
    }

    public Task<int> CreateAsync(PhadiaQualityControlDetail model)
    {
        throw new NotImplementedException();
    }

    public async Task UpdateAsync(PhadiaQualityControlDetail model)
    {
        var client = new QualityControlsClient(_httpClient);
        var command = new UpdatePhadiaQualityControlCommand
        {
            Id = model.Id,
            TubeTypeId = model.TubeTypeId,
            Tests = new List<PhadiaQualityControlUpdateTest>()
        };
        foreach (var test in model.PhadiaQualityControlTests)
        {
            command.Tests.Add(new PhadiaQualityControlUpdateTest
            {
                Id = test.Id,
                Replicates = test.Replicates,
                ReplicateCvLimit = test.ReplicateCvLimit,
                ApplyAcceptanceRange = test.ApplyAcceptanceRange,
                UseAcceptanceRange = test.UseAcceptanceRange,
                MaxCv = test.MaxCv,
                NumberOfSd = test.NumberOfSd,
                UseLotQualification = test.UseLotQualification,
                NumberOfTests = test.NumberOfTests,
                QualityControlType = test.QualityControlType
            });
        }

        await client.UpdatePhadiaQualityControlDetailsAsync(command.Id, command);
    }
}