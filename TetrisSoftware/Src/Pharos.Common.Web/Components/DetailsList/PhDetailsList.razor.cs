using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.DetailsList;

/// <summary>
/// A list of labels with colons and values where the labels have a dimmer color than the values.
/// The main usage is to display a list of details with a label and value on each row with the values horizontally aligned with one another.
/// It's possible to divide the details into up to 3 columns.
/// It's also possible to disable the horizontal alignment of the values.
/// </summary>
public partial class PhDetailsList
{
    /// <summary>
    /// <see cref="PhDetailsListItem"/> elements to be displayed in the list.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Show the values in the list with a highlighted color.
    /// </summary>
    [Parameter] public bool HighlightedValues { get; set; }

    /// <summary>
    /// Set to false to disable alignment of the values in the list. Default is true.
    /// </summary>
    [Parameter] public bool AlignedValues { get; set; } = true;

    /// <summary>
    /// The number of columns to display. Can be set to 1, 2 or 3. Default is 1.
    /// </summary>
    [Parameter] public int Columns { get; set; } = 1;

    /// <summary>
    /// Set to true to space the columns evenly. Only applies if <see cref="Columns"/> is set to 2 or 3.
    /// To get equal column width, also set <see cref="AlignedValues"/> to false.
    /// </summary>
    [Parameter] public bool SpaceColumnsEvenly { get; set; }

    /// <summary>
    /// Can be used to adjust the spacing between the elements placed inside this component.
    /// </summary>
    [Parameter] public DetailsListGap RowGap { get; set; } = DetailsListGap.Sm;

    /// <summary>
    /// Sets the text size used for rendering the details list.
    /// </summary>
    [Parameter] public DetailsListTextSize TextSize { get; set; } = DetailsListTextSize.Default;

    private string? CssClasses { get; set; }

    protected override void OnParametersSet()
    {
        Columns = Math.Min(3, Math.Max(1, Columns));

        CssClasses = new CssBuilder("ph-details-list")
                    .AddClass("ph-details-list--highlighted", HighlightedValues)
                    .AddClass("ph-details-list--aligned-values", AlignedValues)
                    .AddClass("ph-details-list--2-column", Columns == 2)
                    .AddClass("ph-details-list--3-column", Columns == 3)
                    .AddClass("ph-details-list--space-evenly", SpaceColumnsEvenly)
                    .AddClass("ph-details-list--gap-md", RowGap == DetailsListGap.Md)
                    .AddClass("ph-details-list--gap-lg", RowGap == DetailsListGap.Lg)
                    .AddClass("ph-details-list--text-size-body2", TextSize == DetailsListTextSize.Body2)
                    .AddClass("ph-details-list--text-size-helper", TextSize == DetailsListTextSize.HelperText)
                    .Build();
    }
}