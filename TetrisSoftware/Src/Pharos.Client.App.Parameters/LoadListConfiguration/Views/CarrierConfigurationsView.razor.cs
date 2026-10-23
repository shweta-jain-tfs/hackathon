namespace Pharos.Client.App.Parameters.LoadListConfiguration.Views;
public partial class CarrierConfigurationsView : LoadListTotalRefillLevelConfigurationView
{
    protected override LoadListItemType? GetLoadListItemType()
    {
        return LoadListItemType.Carrier;
    }
}