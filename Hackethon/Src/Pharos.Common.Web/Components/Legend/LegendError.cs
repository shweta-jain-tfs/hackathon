namespace Pharos.Common.Web.Components.Legend;
public class LegendError
{
    public required object Error { get; init; }
    public required string Text { get; init; } = string.Empty;
    public bool IsClickable { get; init; }
    public required int Count { get; init; } = 1;
    public required int GroupOrder { get; set; }
    public required int Order { get; set; }
}