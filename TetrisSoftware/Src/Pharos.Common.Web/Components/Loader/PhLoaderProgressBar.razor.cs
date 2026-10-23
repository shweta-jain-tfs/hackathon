namespace Pharos.Common.Web.Components.Loader;

public partial class PhLoaderProgressBar
{
    [Parameter] public double Percentage { get; set; } = 0;
    [Parameter] public string? Text { get; set; }
    [Parameter] public EventCallback Cancel { get; set; }
}