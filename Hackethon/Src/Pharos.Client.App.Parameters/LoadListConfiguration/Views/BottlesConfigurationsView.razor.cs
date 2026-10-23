namespace Pharos.Client.App.Parameters.LoadListConfiguration.Views;

public partial class BottlesConfigurationsView : LoadListConfigurationsView
{
    protected override LoadListItemType? GetLoadListItemType()
    {
        return LoadListItemType.Bottle;
    }
}