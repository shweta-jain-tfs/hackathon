namespace Pharos.Common.Web.Components.DetailsList;

/// <summary>
/// A single item in a <see cref="PhDetailsList"/>. Can only be used inside <see cref="PhDetailsList"/>.
/// </summary>
public partial class PhDetailsListItem
{
    /// <summary>
    /// The value to show next to the label. Typically a string.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Label to show next to the value. A colon will be added automatically.
    /// </summary>
    [Parameter] public string? Label { get; set; }

    private string? LabelText
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Label))
            {
                return null;
            }

            return $"{Label.Trim().TrimEnd(':')}:";
        }
    }
}