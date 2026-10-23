namespace Pharos.Client.Common.App.Interfaces;

public interface IInstrumentDilutionService
{
    Task<List<InstrumentDilution>> GetDilutionFactors(string methodName, string testName, int predilutionFactor);
}