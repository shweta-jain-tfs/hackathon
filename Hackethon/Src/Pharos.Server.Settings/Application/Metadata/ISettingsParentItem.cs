namespace Pharos.Server.Settings.Application.Metadata;
public interface ISettingsParentItem
{
    public List<SettingsChildItem> Children { get; set; }

    /// <summary>
    /// The url associated with the item
    /// </summary>
    string Url { get; set; }
}
