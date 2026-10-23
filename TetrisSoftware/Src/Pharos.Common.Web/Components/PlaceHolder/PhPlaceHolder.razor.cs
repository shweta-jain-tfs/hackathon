namespace Pharos.Common.Web.Components.PlaceHolder;

public partial class PhPlaceHolder
{
    [Parameter] public string Title { get; set; } = "Placeholder";
    [Parameter] public string Text { get; set; } = "To be replaced with content later";
}