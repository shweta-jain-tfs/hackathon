namespace Pharos.Client.App.Sample.Models;

public interface ISampleTrayDetailsDataService : IEntityDataService<SampleTrayDetails, string>
{
    Task UpdateSamplePositionAsync(string trayId, int tubePosition, string tubeBarcode, bool isHighPriority);
    Task<string> GetSampleTrayLocationAsync(string trayId);
}