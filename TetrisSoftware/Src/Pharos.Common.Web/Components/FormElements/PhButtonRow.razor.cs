namespace Pharos.Common.Web.Components.FormElements;

/// <summary>
/// Places buttons to the right with standard spacing with optional other content to the left.
/// </summary>
public partial class PhButtonRow
{
    [Parameter] public RenderFragment? LeftContent { get; set; }
    [Parameter, EditorRequired] public RenderFragment? Buttons { get; set; }

    /// <summary>
    /// Equal width buttons
    /// </summary>
    [Parameter] public bool Justified { get; set; } = true;
}