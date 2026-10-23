namespace Pharos.Client.App.Sample.Models;

public interface ISampleDrawerDataSource
{
    Task UnlockDrawerAsync();
    Task<SampleDrawer> GetDrawerDetailsAsync();
}