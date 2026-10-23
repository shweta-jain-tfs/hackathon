namespace Pharos.Client.App.Parameters.QualityControl.Models;

public interface ILabQualityControlItemSource : IPagedDataSource<LabQualityControl>, IDetailsPageDataService<LabQualityControlDetail>
{
    Task UpdateEnabled(int id, bool enabled);
    Task Delete(HashSet<int> ids);
}

public class LabQualityControlItemSource : ILabQualityControlItemSource
{
    private readonly HttpClient _httpClient;

    public LabQualityControlItemSource(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<DataSourceResult<LabQualityControl>> GetAsync(DataSourceRequest request)
    {
        var client = new QualityControlsClient(_httpClient);
        var enabledOnly = true;
        List<string> methods = [];
        SetFilter(ref enabledOnly, ref methods, request.Filters);
        var result = await client.GetLabQualityControlsAsync(enabledOnly, methods, request.StartIndex, request.Count, request.SortByProperty, request.SortByAscending);
        return new DataSourceResult<LabQualityControl>(result.Items.ToList(), result.TotalCount, result.GroupedCount);
    }

    private static void SetFilter(ref bool enabledOnly, ref List<string> methods, ICollection<DataSourceFilter> requestFilters)
    {
        foreach (var f in requestFilters)
        {
            switch (f.PropertyName)
            {
                case nameof(LabQualityControl.MethodName):
                    switch (f.FilterOperator)
                    {
                        case FilterOperator.EqualsTo:
                            methods?.Add(f.FilterValue as string ?? string.Empty);
                            continue;
                    }
                    break;
                case nameof(LabQualityControl.Enabled):
                    enabledOnly = f.FilterValue as bool? ?? true;
                    break;
            }
        }
    }

    public async Task UpdateEnabled(int id, bool enabled)
    {
        var client = new QualityControlsClient(_httpClient);
        await client.UpdateLabQualityControlEnabledAsync(id, enabled);
    }

    public async Task Delete(HashSet<int> ids)
    {
        var client = new QualityControlsClient(_httpClient);
        await client.DeleteLabQualityControlsAsync(ids);
    }

    public async Task<LabQualityControlDetail> GetByIdAsync(int id)
    {
        var client = new QualityControlsClient(_httpClient);
        return await client.GetLabQualityControlDetailsAsync(id);
    }

    public async Task<int> CreateAsync(LabQualityControlDetail model)
    {
        var client = new QualityControlsClient(_httpClient);
        var command = new CreateLabQualityControlCommand
        {
            Name = model.Name,
            TubeTypeId = model.TubeTypeId,
            IdOnBarcode = model.IdOnBarcode,
            BarcodeFormatId = model.BarcodeFormatId ?? 0,
            Tests = new List<LabQualityControlCreateTest>()
        };
        foreach (var test in model.LabQualityControlTests)
        {
            command.Tests.Add(new LabQualityControlCreateTest
            {
                ApplyAcceptanceRange = test.ApplyAcceptanceRange,
                LowerLimit = test.LowerLimit,
                UpperLimit = test.UpperLimit,
                InstrumentDilutionFactor = test.InstrumentDilutionFactor,
                MaxCv = test.MaxCv,
                NumberOfSd = test.NumberOfSd,
                ReplicateCvLimit = test.ReplicateCvLimit,
                PreDilutionFactor = test.PreDilutionFactor,
                Replicates = test.Replicates,
                TestId = test.TestId,
                UseAcceptanceRange = test.UseAcceptanceRange,
                UseLotQualification = test.UseLotQualification,
                NumberOfTests = test.NumberOfTests,
                QualityControlType = test.QualityControlType
            });
        }

        return await client.CreateLabQualityControlAsync(command);
    }

    public async Task UpdateAsync(LabQualityControlDetail model)
    {
        var client = new QualityControlsClient(_httpClient);
        var command = new UpdateLabQualityControlCommand
        {
            Id = model.Id,
            BarcodeFormatId = model.BarcodeFormatId ?? 0,
            IdOnBarcode = model.IdOnBarcode,
            TubeTypeId = model.TubeTypeId,
            QcDetailsQueriedAt = model.QueriedAt,
            Tests = new List<LabQualityControlUpdateTest>()
        };
        foreach (var test in model.LabQualityControlTests)
        {
            command.Tests.Add(new LabQualityControlUpdateTest
            {
                ApplyAcceptanceRange = test.ApplyAcceptanceRange,
                LowerLimit = test.LowerLimit,
                UpperLimit = test.UpperLimit,
                InstrumentDilutionFactor = test.InstrumentDilutionFactor,
                MaxCv = test.MaxCv,
                NumberOfSd = test.NumberOfSd,
                ReplicateCvLimit = test.ReplicateCvLimit,
                PreDilutionFactor = test.PreDilutionFactor,
                Replicates = test.Replicates,
                TestId = test.TestId,
                UseAcceptanceRange = test.UseAcceptanceRange,
                UseLotQualification = test.UseLotQualification,
                NumberOfTests = test.NumberOfTests,
                Id = test.Id,
                QualityControlType = test.QualityControlType
            });
        }

        await client.UpdateLabQualityControlAsync(model.Id, command);
    }
}