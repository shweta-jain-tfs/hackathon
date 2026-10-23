namespace Pharos.Client.App.Parameters.QualityControl.Models;

public interface IQualityControlBarcodeFormatItemSource
{
    Task<List<QualityControlBarcodeFormatDetail>> GetAll();
    Task<QualityControlBarcodeFormatDetail> GetDetails(int entityId);
    Task CreateAsync(QualityControlBarcodeFormatDetail model);
    Task UpdateAsync(QualityControlBarcodeFormatDetail model);
    Task DeleteAsync(HashSet<int> ids);
}

public class QualityControlBarcodeFormatItemSource : IQualityControlBarcodeFormatItemSource
{
    private readonly HttpClient _httpClient;

    public QualityControlBarcodeFormatItemSource(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<QualityControlBarcodeFormatDetail>> GetAll()
    {
        var client = new QualityControlsClient(_httpClient);
        var result = await client.GetQualityControlBarcodeFormatsAsync();
        return result.ToList();
    }

    public async Task<QualityControlBarcodeFormatDetail> GetDetails(int entityId)
    {
        var client = new QualityControlsClient(_httpClient);
        return await client.GetQualityControlBarcodeFormatDetailsAsync(entityId);
    }

    public async Task CreateAsync(QualityControlBarcodeFormatDetail model)
    {
        var client = new QualityControlsClient(_httpClient);
        var command = new CreateQualityControlBarcodeFormatCommand
        {
            Name = model.Name,
            TotalLength = model.TotalLength,
            QcStartPosition = model.QcStartPosition,
            QcLength = model.QcLength,
            ContainsLotInformation = model.ContainsLotInformation,
            LotStartPosition = model.LotStartPosition,
            LotLength = model.LotLength
        };
        await client.CreateQualityControlBarcodeFormatAsync(command);
    }

    public async Task UpdateAsync(QualityControlBarcodeFormatDetail model)
    {
        var client = new QualityControlsClient(_httpClient);
        var command = new UpdateQualityControlBarcodeFormatCommand
        {
            Id = model.Id,
            Name = model.Name,
            TotalLength = model.TotalLength,
            QcStartPosition = model.QcStartPosition,
            QcLength = model.QcLength,
            ContainsLotInformation = model.ContainsLotInformation,
            LotStartPosition = model.LotStartPosition,
            LotLength = model.LotLength
        };
        await client.UpdateQualityControlBarcodeFormatAsync(model.Id, command);
    }

    public async Task DeleteAsync(HashSet<int> ids)
    {
        var client = new QualityControlsClient(_httpClient);
        foreach (var id in ids)
        {
            await client.DeleteQualityControlBarcodeFormatAsync(id);
        }
    }
}
