using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.Loader;

public partial class PhLoaderContent
{
    [Parameter] public bool IsLoading { get; set; }
    [Parameter] public bool ShowText { get; set; }
    [Parameter] public string? Text { get; set; }
    [Parameter] public string? ProgressText { get; set; }
    [Parameter] public bool ShowProgressBar { get; set; }
    [Parameter] public double ProgressPercentage { get; set; } = 0;
    [Parameter] public EventCallback Cancel { get; set; }

    private string CssClass => new CssBuilder("ph-loader-overlay")
        .AddClass("ph-loader-overlay--disable", !IsLoading)
        .Build();

    private string InnerCssClass => new CssBuilder("ph-loader-overlay-content_text")
        .AddClass("ph-loader-overlay_content_text--disabled", !ShowText)
        .Build();

    private void OnCancel()
    {
        Cancel.InvokeAsync();
    }
}