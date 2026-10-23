namespace Pharos.Client.App.InstrumentManagement.Models.Instruments;

public interface ITestResultDataService
{
    Task<int> GetTestResultsCount(string serialNumber);
}