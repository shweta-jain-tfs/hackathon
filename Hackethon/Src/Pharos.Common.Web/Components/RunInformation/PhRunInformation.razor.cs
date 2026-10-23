using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.RunInformation;

/// <summary>
/// Displays instrument status and progress information in the status bar.
/// </summary>
public partial class PhRunInformation
{
    [Parameter] public string? Row1 { get; set; }
    [Parameter] public string? Row2 { get; set; }

    /// <summary>
    /// Optional content rendered inline after Row2, e.g. an action link.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Use the same text color for both rows of text
    /// </summary>
    [Parameter] public bool UniformTextColor { get; set; }

    private string CssClasses => new CssBuilder("ph-run-information")
        .AddClass("ph-run-information--uniform-text-color", UniformTextColor)
        .Build();

    protected override Task OnParametersSetAsync()
    {
        Row1 ??= "-";
        return base.OnParametersSetAsync();
    }
}