using Pharos.Common.Web.Contexts;

namespace Pharos.Client.Common.App;

public interface IUiComponentStringsLocalizer
{
    UiComponentStrings Build();
}

public class UiComponentStringsLocalizer : IUiComponentStringsLocalizer
{
    private readonly IStringLocalizer<Translations> _loc;

    public UiComponentStringsLocalizer(IStringLocalizer<Translations> loc)
    {
        _loc = loc;
    }

    public UiComponentStrings Build()
    {
        return new UiComponentStrings
        {
            // Base on Default to avoid empty strings if a localization lookup is missing
            DataGrid = DataGridStrings.Default with
            {
                ClearAllFilters = _loc["UiComponent_DataGrid_ClearAllFilters"],
                ClearFilter = _loc["UiComponent_DataGrid_ClearFilter"],
                CustomRange = _loc["UiComponent_DataGrid_CustomRange"],
                From = _loc["UiComponent_DataGrid_From"],
                LastDaysPrefix = _loc["UiComponent_DataGrid_LastDaysPrefix"],
                LastDaysSuffix = _loc["UiComponent_DataGrid_LastDaysSuffix"],
                Month = _loc["UiComponent_DataGrid_Month"],
                ProgressBarText = _loc["UiComponent_DataGrid_ProgressBarText"],
                ResetToDefault = _loc["UiComponent_DataGrid_ResetToDefault"],
                SelectedText = _loc["UiComponent_DataGrid_SelectedText"],
                Show = _loc["UiComponent_DataGrid_Show"],
                To = _loc["UiComponent_DataGrid_To"],
                Today = _loc["UiComponent_DataGrid_Today"],
            },
            NavigationLock = NavigationLockStrings.Default with
            {
                UnsavedChangesTitle = _loc["UiComponent_NavigationLock_UnsavedChangesTitle"],
                UnsavedChangesMessage = _loc["UiComponent_NavigationLock_UnsavedChangesMessage"],
                Save = _loc["UiComponent_NavigationLock_Save"],
                DiscardChanges = _loc["UiComponent_NavigationLock_DiscardChanges"]
            }
        };
    }

}
