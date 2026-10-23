namespace Pharos.Client.App.Parameters.LoadListConfiguration.Views;

public partial class CartridgeConfigurationsView : LoadListTotalRefillLevelConfigurationView
{
    protected override LoadListItemType? GetLoadListItemType()
    {
        return LoadListItemType.Cartridge;
    }
}