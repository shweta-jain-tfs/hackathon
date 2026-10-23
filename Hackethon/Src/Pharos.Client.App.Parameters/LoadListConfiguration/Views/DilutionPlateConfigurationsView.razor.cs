namespace Pharos.Client.App.Parameters.LoadListConfiguration.Views;

public partial class DilutionPlateConfigurationsView : LoadListConfigurationsView
{
    protected override LoadListItemType? GetLoadListItemType()
    {
        return LoadListItemType.DilutionPlate;
    }
}