namespace Pharos.Contracts.Notifications;
public interface ILoadListHub : IHub
{
    Task BottleListChanged();
    Task CarrierListChanged();
    Task CartridgeListChanged();
    Task DilutionPlateListChanged();
    Task SampleTubeListChanged();
}
