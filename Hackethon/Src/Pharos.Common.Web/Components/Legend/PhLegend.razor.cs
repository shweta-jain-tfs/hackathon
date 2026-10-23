using Pharos.Common.Web.Components.Reagent;
using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.Legend;
public partial class PhLegend<TStatus> : ComponentBase where TStatus : struct, Enum
{
    [Parameter]
    public string StatusHeaderText { get; init; } = string.Empty;

    [Parameter]
    public IEnumerable<TStatus> LegendStatuses { get; set; } = Enumerable.Empty<TStatus>();

    [Parameter]
    public string ErrorHeaderText { get; init; } = string.Empty;

    [Parameter]
    public IEnumerable<LegendError> LegendErrors { get; set; } = Enumerable.Empty<LegendError>();

    [Parameter]
    public bool IsAnyHighPriority { get; set; }

    [Parameter]
    public bool IsAnyManualEntry { get; set; }

    [Parameter]
    public string HighPriorityText { get; set; } = default!;

    [Parameter]
    public string ManualEntryText { get; set; } = default!;

    [Parameter]
    public int UnloadRequestedCount { get; set; }

    [Parameter]
    public string UnloadRequestedText { get; set; } = default!;

    [Parameter]
    [EditorRequired]
    public Func<TStatus, string> GetStatusText { get; set; } = default!;

    [Parameter]
    public EventCallback<LegendSelectionResult> SelectionChanged { get; set; }

    private readonly LegendSelectionResult _selectionResult = new();

    public IEnumerable<LegendError> OrderedLegendErrors => LegendErrors
        .OrderBy(i => i.GroupOrder)
        .ThenBy(i => i.Order);

    private bool HasStatusItems => LegendStatuses.Any() || IsAnyHighPriority || IsAnyManualEntry;

    private bool HasErrorItems => UnloadRequestedCount > 0 || LegendErrors.Any();

    protected override async Task OnParametersSetAsync()
    {
        await ResetCurrentSelectionIfNotAvailable();
        await base.OnParametersSetAsync();
    }

    public async Task ResetSelection()
    {
        _selectionResult.IsHighPrioritySelected = false;
        _selectionResult.IsManualEntrySelected = false;
        _selectionResult.IsUnloadRequestedSelected = false;
        _selectionResult.SelectedStatus = null;
        _selectionResult.SelectedError = null;
        await UpdateSelectionResult(false, false, false, null, null);
    }

    private async Task OnHighPriorityClick()
    {
        await UpdateSelectionResult(isHighPrioritySelected: !_selectionResult.IsHighPrioritySelected);
    }

    private async Task OnManualEntryClick()
    {
        await UpdateSelectionResult(isManualEntrySelected: !_selectionResult.IsManualEntrySelected);
    }

    private async Task OnUnloadRequestedClick()
    {
        await UpdateSelectionResult(isUnloadRequestedSelected: !_selectionResult.IsUnloadRequestedSelected);
    }

    private async Task OnStatusClick(TStatus? status)
    {
        status = _selectionResult.SelectedStatus?.Equals(status) == true ? null : status;
        await UpdateSelectionResult(selectedStatus: status);
    }

    private async Task OnErrorClick(LegendError? error)
    {
        error = _selectionResult.SelectedError?.Equals(error) == true ? null : error;
        await UpdateSelectionResult(selectedError: error);
    }

    private async Task UpdateSelectionResult(
        bool isHighPrioritySelected = false,
        bool isManualEntrySelected = false,
        bool isUnloadRequestedSelected = false,
        TStatus? selectedStatus = null,
        LegendError? selectedError = null)
    {
        _selectionResult.IsHighPrioritySelected = isHighPrioritySelected;
        _selectionResult.IsManualEntrySelected = isManualEntrySelected;
        _selectionResult.IsUnloadRequestedSelected = isUnloadRequestedSelected;
        _selectionResult.SelectedStatus = selectedStatus;
        _selectionResult.SelectedError = selectedError;

        await SelectionChanged.InvokeAsync(_selectionResult);
        StateHasChanged();
    }


    private string GetItemCssClass(object? selectedItem)
    {
        return new CssBuilder("ph-legend__item")
         .AddClass("ph-legend__item--selected", IsItemSelected(selectedItem))
         .AddClass("ph-legend__item--clikable", IsItemClickable(selectedItem))
         .Build();
    }

    private bool IsItemSelected(object? item)
    {
        return item switch
        {
            TStatus status when _selectionResult.SelectedStatus is not null
                => EqualityComparer<TStatus>.Default.Equals(status, (TStatus)_selectionResult.SelectedStatus),

            LegendError error when _selectionResult.SelectedError is not null
                => error.Error.Equals(_selectionResult.SelectedError.Error),

            bool isHighPriority when isHighPriority && _selectionResult.IsHighPrioritySelected
            => _selectionResult.IsHighPrioritySelected,

            bool isManualEntry when isManualEntry && _selectionResult.IsManualEntrySelected
                => _selectionResult.IsManualEntrySelected,

            bool isUnloadRequested when isUnloadRequested && _selectionResult.IsUnloadRequestedSelected
                => _selectionResult.IsUnloadRequestedSelected,

            _ => false
        };
    }

    private static bool IsItemClickable(object? item)
    {
        return item switch
        {
            LegendError error when !error.IsClickable
                => false,

            _ => true
        };
    }

    private static bool IsUnloadRequestedLegendError(LegendError legendError)
    {
        return legendError.Error is ReagentError.UnloadRequested;
    }

    private static string GetErrorIconCss(LegendError legendError)
    {
        return new CssBuilder("ph-legend__item-icon")
            .AddClass("ph-legend__item-icon--unload-requested", IsUnloadRequestedLegendError(legendError))
            .Build();
    }

    private Dictionary<string, object> GetErrorAttributes(LegendError error)
    {
        var attributes = new Dictionary<string, object>();

        if (error.IsClickable)
        {
            attributes["onclick"] = EventCallback.Factory.Create(this, () => OnErrorClick(error));
        }

        return attributes;
    }

    private async Task ResetCurrentSelectionIfNotAvailable()
    {
        var isSelectionChanged = false;

        if (_selectionResult.SelectedStatus is not null)
        {
            var isCurrentSelectedStatusAvailable = _selectionResult.SelectedStatus is TStatus selectedStatus &&
                                                    LegendStatuses.Contains(selectedStatus);
            if (!isCurrentSelectedStatusAvailable)
            {
                _selectionResult.SelectedStatus = null;
                isSelectionChanged = true;
            }
        }

        if (_selectionResult.SelectedError is not null)
        {
            var isCurrentSelectedErrorAvailable = _selectionResult.SelectedError is LegendError selectedError &&
                                                  LegendErrors.Select(le => le.Error).Contains(selectedError.Error);
            if (!isCurrentSelectedErrorAvailable)
            {
                _selectionResult.SelectedError = null;
                isSelectionChanged = true;
            }
        }

        if (_selectionResult.IsHighPrioritySelected && !IsAnyHighPriority)
        {
            _selectionResult.IsHighPrioritySelected = false;
            isSelectionChanged = true;
        }

        if (_selectionResult.IsManualEntrySelected && !IsAnyManualEntry)
        {
            _selectionResult.IsManualEntrySelected = false;
            isSelectionChanged = true;
        }

        if (_selectionResult.IsUnloadRequestedSelected && UnloadRequestedCount == 0)
        {
            _selectionResult.IsUnloadRequestedSelected = false;
            isSelectionChanged = true;
        }

        if (isSelectionChanged)
        {
            await SelectionChanged.InvokeAsync(_selectionResult);
        }
    }

    private static string GetReagentItemIconCss(ReagentStatus status)
    {
        return new CssBuilder("ph-legend__reagent-item-icon")
            .AddClass("ph-legend__reagent-item-icon--identified", status == ReagentStatus.Identified)
            .AddClass("ph-legend__reagent-item-icon--not-identified", status == ReagentStatus.NotIdentified)
            .Build();
    }

    private static string GetBottleItemIconCss(BottleStatus status)
    {
        return new CssBuilder("ph-legend__bottle-item-icon")
            .AddClass("ph-legend__bottle-item-icon--in-use", status == BottleStatus.InUse)
            .AddClass("ph-legend__bottle-item-icon--empty", status == BottleStatus.Empty)
            .Build();
    }

    private static string GetLiquidItemIconCss(LiquidBottleStatus status)
    {
        return new CssBuilder("ph-legend__liquid-item-icon")
            .AddClass("ph-legend__liquid-item-icon--disabled", status == LiquidBottleStatus.Disabled)
            .AddClass("ph-legend__liquid-item-icon--in-use", status == LiquidBottleStatus.InUse)
            .Build();
    }

    private static string GetItemTextCss(bool isClickable = true)
    {
        return new CssBuilder("ph-legend__item-text")
            .AddClass("ph-legend__item-text--clickable", isClickable)
            .Build();
    }
}
