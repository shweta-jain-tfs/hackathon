namespace Pharos.Client.App.Sample.Models;

public interface ISampleTrayDataService : IEntityDataService<SampleTray, string>
{
    Task<List<string>> ClearTraysAsync(string[] entityIds);
    Task<List<string>> DeleteTraysAsync(string[] entityIds);
    Task SetTrayNotInInstrumentAsync(string entityId);
}