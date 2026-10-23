using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.Drawer;

public partial class PhEditableTray : ComponentBase
{
    [Parameter]
    public List<SampleTube> SampleTubes { get; set; } = default!;

    [Parameter]
    public Func<SampleTube, Task<bool>>? TubeSelected { get; set; }

    [Parameter]
    public bool IsEditMode { get; set; }

    [Parameter]
    public bool IsTubeSelected { get; set; }

    [Parameter]
    public int? InitialSelectedPosition { get; set; }

    [CascadingParameter(Name = "SampleDrawerOnResetLegendSelectionRequested")]
    public Action? SampleDrawerOnResetLegendSelectionRequested { get; set; }

    [CascadingParameter(Name = "TrayPanelOnResetLegendSelectionRequested")]
    public Action? TrayPanelOnResetLegendSelectionRequested { get; set; }


    private int _selectedRow = -1;
    private int _selectedColumn = -1;
    private int _selectedTubePosition = -1;
    
    protected override void OnParametersSet()
    {
        if (!InitialSelectedPosition.HasValue)
        {
            ResetSelectedTubePosition();
        }

        if (InitialSelectedPosition.HasValue)
        {
            SetSelectedPosition(InitialSelectedPosition.Value);
        }
    }

    private void SetSelectedPosition(int selectedPosition)
    {
        _selectedTubePosition = selectedPosition;

        if (_selectedTubePosition != -1)
        {
            const int columns = 10;

            _selectedRow = ((_selectedTubePosition - 1) / columns) + 1;
            _selectedColumn = ((_selectedTubePosition - 1) % columns) + 1;
        }
    }

    private async Task OnSampleTubeClick(SampleTube sampleTube)
    {
        var isTubeSelectionChanged = await TubeSelected!.Invoke(sampleTube);
        if (isTubeSelectionChanged)
        {
            SetSelectedPosition(sampleTube.Position ?? -1);

            SampleDrawerOnResetLegendSelectionRequested?.Invoke();

            TrayPanelOnResetLegendSelectionRequested?.Invoke();
        }
    }

    private string SampleTubeCssClass(int? position)
    {
        return new CssBuilder("ph-tray-detail__tray-content-sample-tube")
            .AddClass("tube-selected", position == _selectedTubePosition)
            .Build();
    }

    private string RowLabelCssClass(int row)
    {
        return new CssBuilder("ph-tray-detail__tray-content-coordinate")
             .AddClass("ph-tray-detail--highlight-label", row == _selectedRow)
             .Build();
    }

    private string ColumnLabelClass(int column)
    {
        return new CssBuilder("ph-tray-detail__tray-content-col-coordinate")
            .AddClass("ph-tray-detail--highlight-label", column == _selectedColumn)
            .Build();
    }

    private void ResetSelectedTubePosition()
    {
        if (!IsTubeSelected)
        {
            _selectedTubePosition = _selectedRow = _selectedColumn = -1;
        }
    }
}