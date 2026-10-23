using Pharos.Client.App.Logs.Models.Errors;
using Pharos.Client.App.Logs.ViewModels.Errors;
using Pharos.Common.Web.Components.DataGrid;

namespace Pharos.Client.App.Logs.Views.Errors;

public partial class SystemErrorsView : IDisposable
{
    [Inject] private SystemErrorsListViewModel ViewModel { get; set; } = default!;
    [Inject] private ISystemErrorDetailsDataService SystemErrorDetailsDataService { get; set; } = default!;
    [Inject] private IErrorTranslator Translator { get; set; } = default!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] private SystemErrorsInstrumentFilterViewModel InstrumentFilterModel { get; set; } = default!;

    [Parameter] public EventCallback<int> OnErrorSelected { get; set; }
    [Parameter] public bool IsPanelOpen { get; set; }
    [Parameter] public int? HighlightedErrorId { get; set; }
    [Parameter] public int? RespondedErrorId { get; set; }

    private readonly HashSet<int> _selectedErrorIds = new();
    private readonly Dictionary<string, string> _classificationFilterDataSource = Enum
        .GetValues<ErrorClassification>()
        .ToDictionary(value => value.ToString(), value => string.Empty);

    private readonly ItemStyleOptions<SystemErrorSummary> _nonConfirmableRowSelectionStyle = CreateNonConfirmableRowSelectionStyle();

    private PhDataGrid<SystemErrorSummary>? _gridRef;
    private bool _isUpdatingSelectionProgrammatically;
    private bool _isConfirming;
    private bool _refreshPendingWhilePanelOpen;
    private readonly HashSet<int> _locallyRespondedIds = [];
    private int? _previousRespondedErrorId;

    private static ItemStyleOptions<SystemErrorSummary> CreateNonConfirmableRowSelectionStyle()
    {
        return new ItemStyleOptions<SystemErrorSummary>
        {
            Items = [true],
            Match = static (item, _) => !item.IsAwaitingAcknowledge,
            Style = ItemStyleFlags.Disabled
        };
    }

    protected override async Task OnInitializedAsync()
    {
        // Initialize translated labels for the classification filter.
        foreach (var classification in _classificationFilterDataSource.Keys.ToList())
        {
            if (Enum.TryParse<ErrorClassification>(classification, out var parsed))
            {
                _classificationFilterDataSource[classification] = Translator.GetErrorLevel(parsed);
            }
        }

        await InstrumentFilterModel.LoadAsync();
        ViewModel.ViewRefreshNeeded += OnViewRefreshNeeded;
    }

    protected override void OnParametersSet()
    {
        // Track each newly responded ID so the icon stays hidden
        // even after RespondedErrorId is reset when the user navigates to the next item.
        if (RespondedErrorId.HasValue && RespondedErrorId != _previousRespondedErrorId)
        {
            _locallyRespondedIds.Add(RespondedErrorId.Value);
            _previousRespondedErrorId = RespondedErrorId;
        }

        // Keep the list stable while the detail panel is open.
        // Apply any queued refresh once the panel is closed.
        if (!IsPanelOpen && _refreshPendingWhilePanelOpen)
        {
            _locallyRespondedIds.Clear();
            _previousRespondedErrorId = null;
            _refreshPendingWhilePanelOpen = false;
            _ = InvokeAsync(StateHasChanged);
        }
    }

    private void OnViewRefreshNeeded(object? sender, EventArgs e)
    {
        // Do not rewrite the list while the panel is open.
        // This keeps the highlighted row and list state unchanged until close.
        if (IsPanelOpen)
        {
            _refreshPendingWhilePanelOpen = true;
            return;
        }

        _ = InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        ViewModel.ViewRefreshNeeded -= OnViewRefreshNeeded;
        ViewModel.Dispose();
        GC.SuppressFinalize(this);
    }

    private static bool IsWaitingForResponse(SystemErrorSummary item)
    {
        return !item.Response.HasValue;
    }

    private async Task HandleSelectedItemsChangedAsync(HashSet<SystemErrorSummary> selectedItems)
    {
        if (_isUpdatingSelectionProgrammatically)
        {
            return;
        }

        var confirmableItems = selectedItems
            .Where(x => x.IsAwaitingAcknowledge)
            .ToList();

        _selectedErrorIds.Clear();
        foreach (var item in confirmableItems)
        {
            _selectedErrorIds.Add(item.Id);
        }

        if (confirmableItems.Count != selectedItems.Count && _gridRef is not null)
        {
            _isUpdatingSelectionProgrammatically = true;
            try
            {
                await _gridRef.SetSelectItems(confirmableItems);
            }
            finally
            {
                _isUpdatingSelectionProgrammatically = false;
            }
        }
    }

    private async Task ShowErrorDetails(int id)
    {
        await OnErrorSelected.InvokeAsync(id);
    }

    private async Task ConfirmSelectedErrors()
    {
        if (_isConfirming || _selectedErrorIds.Count == 0)
        {
            return;
        }

        _isConfirming = true;
        var selectedErrorIds = _selectedErrorIds.ToHashSet();

        try
        {
            await SystemErrorDetailsDataService.AcknowledgeSystemErrors(selectedErrorIds);
            _selectedErrorIds.Clear();
        }
        finally
        {
            _isConfirming = false;
            await InvokeAsync(StateHasChanged);
        }
    }
}
