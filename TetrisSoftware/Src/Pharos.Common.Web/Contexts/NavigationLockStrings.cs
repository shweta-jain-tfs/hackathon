namespace Pharos.Common.Web.Contexts;

public record NavigationLockStrings
{
    public string UnsavedChangesTitle { get; set; } = string.Empty;
    public string UnsavedChangesMessage { get; set; } = string.Empty;
    public string Save { get; set; } = string.Empty;
    public string DiscardChanges { get; set; } = string.Empty;

    public static NavigationLockStrings Default => new()
    {
        UnsavedChangesTitle = Missing(nameof(UnsavedChangesTitle)),
        UnsavedChangesMessage = Missing(nameof(UnsavedChangesMessage)),
        Save = Missing(nameof(Save)),
        DiscardChanges = Missing(nameof(DiscardChanges))
    };

    private static string Missing(string name)
    {
        return $"[{nameof(NavigationLockStrings)}.{name}]";
    }
}
