using Pharos.Client.Common.Models;
using Pharos.Client.Common.Models.DataServices;

namespace Pharos.Client.App.Maintenance.Models;

public interface IMaintenanceItemsSource : IItemsSource<MaintenanceActivity>;

public interface IMaintenanceService
{
    //Checks if the instrument is in a state where maintenance can be started. Returns true if it can be started, false otherwise.
    Task<MaintenanceInfo> CanStartMaintenance();

    //Sends maintenance start to the instrument. The instrument will then change state to maintenance.
    Task<StartMaintenanceActivityResponse> StartMaintenanceActivity(MaintenanceActivity maintenanceActivity);

    //Sends maintenance done to the instrument. The instrument will then change state to standby.
    Task MaintenanceActivityDone(MaintenanceActivity maintenanceActivity);

    //Sets the last performed date for the given activity type. Should only be used for on demand activities
    Task MaintenanceActivityPerformed(MaintenanceActivityType activityType);

    Task AbortMaintenanceActivity(MaintenanceActivity maintenanceActivity);

    Task<BottleState> GetMaintenanceBottleState();
}

public class MaintenanceItemsSource : IMaintenanceItemsSource, IMaintenanceService
{
    private readonly HttpClient _httpClient;

    public MaintenanceItemsSource(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<MaintenanceActivity>> GetAsync()
    {
        var client = new MaintenanceClient(_httpClient);
        var items = await client.GetMaintenanceActivitiesAsync();
        return items.ToList();
    }

    public async Task<StartMaintenanceActivityResponse> StartMaintenanceActivity(MaintenanceActivity maintenanceActivity)
    {
        var client = new MaintenanceClient(_httpClient);
        return await client.StartMaintenanceActivityAsync(maintenanceActivity.Id);
    }

    public async Task MaintenanceActivityDone(MaintenanceActivity maintenanceActivity)
    {
        var client = new MaintenanceClient(_httpClient);
        await client.MaintenanceActivityDoneAsync(maintenanceActivity.Id);
    }

    public async Task MaintenanceActivityPerformed(MaintenanceActivityType activityType)
    {
        var client = new MaintenanceClient(_httpClient);
        await client.MaintenanceActivityPerformedAsync(activityType);
    }

    public async Task AbortMaintenanceActivity(MaintenanceActivity maintenanceActivity)
    {
        var client = new MaintenanceClient(_httpClient);
        await client.AbortMaintenanceActivityAsync(maintenanceActivity.Id);
    }

    public async Task<BottleState> GetMaintenanceBottleState()
    {
        var client = new MaintenanceClient(_httpClient);
        return await client.GetMaintenanceBottleStateAsync();
    }

    public async Task<MaintenanceInfo> CanStartMaintenance()
    {
        var client = new MaintenanceClient(_httpClient);
        return await client.CanStartMaintenanceAsync();
    }
}