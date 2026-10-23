namespace Pharos.Client.App.Sample.Models;

public interface IAllSampleDrawerDataSource
{
    Task<IReadOnlyList<SampleDrawer>> GetDrawersDetailsAsync();
}