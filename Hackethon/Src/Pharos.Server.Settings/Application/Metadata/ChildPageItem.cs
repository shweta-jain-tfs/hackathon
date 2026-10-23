using System.Text.Json.Serialization;

namespace Pharos.Server.Settings.Application.Metadata;

[JsonDerivedType(typeof(PagePlaceholder), nameof(PagePlaceholder))]
[JsonDerivedType(typeof(Page), nameof(Page))]
public class ChildPageItem : SettingsChildItem
{
}
