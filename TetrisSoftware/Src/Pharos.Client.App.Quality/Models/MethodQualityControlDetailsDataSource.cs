namespace Pharos.Client.App.Quality.Models;

public interface IMethodQualityControlDetailsDataSource : IDetailsPageDataService<QualityControl>
{
    public Task<int> CreateLot(int qualityControlId, QualityControlLot value);
    public Task<bool> DeleteLots(List<int> lotIds);
    public Task<int> UpdateLot(QualityControlLot lot);
    public Task UnloadReagents(List<UnloadReagentRequestItem> unloadReagents);
    public Task UnloadTubes(List<string> tubeIds);
}

internal sealed class MethodQualityControlDetailsDataSource(HttpClient httpClient) : IMethodQualityControlDetailsDataSource
{
    public Task<int> CreateAsync(QualityControl value)
    {
        throw new NotImplementedException();
    }

    public async Task<int> CreateLot(int qualityControlId, QualityControlLot value)
    {
        var client = new MethodsClient(httpClient);
        var command = new CreateLabQualityControlLotCommand
        {
            QualityControlId = qualityControlId,
            LotType = value.LotType,
            LotNumber = value.LotNumber,
            RangeMin = value.RangeMin,
            RangeMax = value.RangeMax,
            ExpectedConcentration = value.ExpectedConcentration,
            ExpectedStdDev = value.ExpectedStdDev,
            StdDevLimit = value.StdDevLimit,
            ExpirationDate = value.ExpirationDate
        };

        return await client.CreateLabQualityControlLotAsync(command);
    }

    public async Task<bool> DeleteLots(List<int> lotIds)
    {
        var client = new MethodsClient(httpClient);
        var command = new DeleteMethodQualityControlLotsCommand
        {
            LotIds = lotIds
        };

        return await client.DeleteQualityControlLotAsync(command);
    }
    
    public async Task<QualityControl> GetByIdAsync(int entityId)
    {
        var client = new MethodsClient(httpClient);
        return await client.GetMethodQualityControlsByIdAsync(entityId);
    }

    public Task UpdateAsync(QualityControl value)
    {
        throw new NotImplementedException();
    }

    public async Task<int> UpdateLot(QualityControlLot lot)
    {
        var client = new MethodsClient(httpClient);
        var command = new UpdateQualityControlLotCommand
        {
            Id = lot.Id,
            LotType = lot.LotType,
            RangeMin = lot.RangeMin,
            RangeMax = lot.RangeMax,
            ExpectedConcentration = lot.ExpectedConcentration,
            ExpectedStdDev = lot.ExpectedStdDev,
            StdDevLimit = lot.StdDevLimit
        };

        return await client.UpdateQualityControlLotAsync(command);
    }

    public async Task UnloadReagents(List<UnloadReagentRequestItem> unloadReagents)
    {
        var client = new InventoryClient(httpClient);
        await client.UnloadReagentAsync(unloadReagents);
    }

    public Task UnloadTubes(List<string> tubeIds)
    {
        var client = new TubesClient(httpClient);
        return client.UnloadQualityControlTubesAsync(new UnloadQualityControlTubesCommand
        {
            TubeIds = tubeIds
        });
    }
}
