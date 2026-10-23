using Microsoft.AspNetCore.Components.Sections;

namespace Pharos.Client.App.Settings.Views;

/// <summary>
/// This layout is used as a wrapper around the page content instead of a nested layout.
/// Uses a <see cref="SectionOutlet"/> to place content in the page header the same way a nested layout would.
/// </summary>
public partial class SettingsLayout
{
    [Parameter] public RenderFragment? ChildContent { get; set; }
}