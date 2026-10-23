using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.Models;
using Pharos.Client.Common.Models.DataServices;
using Pharos.Client.Common.ViewModels;
using Pharos.Common.Web.Components.DataGrid.Filters.Models;
using Pharos.Common.Web.Components.Status;
using Pharos.Common.Web.Models.DataGrid;

namespace Pharos.Client.App.InstrumentManagement.ViewModels.TaskCards;

public class TaskCardsListViewModel : LoadingViewModel, IDisposable
{
    private readonly IPagedDataSource<TaskCardListItem, int> _taskCardsDataSource;
    private readonly ITaskCardRefreshNotificationProvider _refreshNotificationProvider;
    private readonly ITaskCardTranslator _translator;

    private IColumnFilter? _instrumentColumnFilter;

    public TaskCardsListViewModel(
        IPagedDataSource<TaskCardListItem, int> taskCardsDataSource,
        ITaskCardRefreshNotificationProvider refreshNotificationProvider,
        ITaskCardTranslator translator)
    {
        _taskCardsDataSource = taskCardsDataSource;
        _refreshNotificationProvider = refreshNotificationProvider;
        _translator = translator;
    }

    public List<TaskCardListItem>? Tasks { get; set; }

    public int TaskCardsCount
    {
        get;
        private set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged(nameof(TaskCardsCount));
            }
        }
    }

    public string? SelectedInstrumentCard
    {
        get;
        private set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged(nameof(SelectedInstrumentCard));
            }
        }
    }

    public void SelectInstrument(string? serialNumber)
    {
        SelectedInstrumentCard = serialNumber;
        SyncColumnFilterWithCardSelection(serialNumber);
    }

    private void SyncColumnFilterWithCardSelection(string? serialNumber)
    {
        if (_instrumentColumnFilter is null)
        {
            return;
        }

        _instrumentColumnFilter.ApplyState(new KeyValueListFilterState
        {
            PropertyName = nameof(TaskCardListItem.InstrumentDisplayName),
            SelectedKeys = serialNumber is null ? [] : [serialNumber]
        });
    }

    protected override Task DoLoadAsync()
    {
        _refreshNotificationProvider.RefreshNeeded += OnRefreshNeeded;
        return Task.CompletedTask;
    }

    private async void OnRefreshNeeded(object? sender, EventArgs e) => OnTaskCardsUpdated();

    public async ValueTask<GridItemsProviderResult<TaskCardListItem>> ProvideItems(GridItemsProviderRequest<TaskCardListItem> request)
    {
        CaptureColumnFilterRef(request);

        var filters = BuildDataSourceFilters(request);

        if (!string.IsNullOrEmpty(SelectedInstrumentCard))
        {
            if (CheckIfColumnFiltersUpdated())
            {
                DeselectInstrumentCard();
            }
            else
            {
                ApplyInstrumentCardFilter(filters);
            }
        }

        var dataSourceRequest = new DataSourceRequest
        {
            StartIndex = request.StartIndex,
            Count = request.Count,
            SortByProperty = request.SortByColumn == nameof(TaskCardListItem.Id) ? null : request.SortByColumn,
            SortByAscending = request.SortByAscending,
            Filters = filters,
            // Use CancellationToken.None to prevent WASM scheduling issues with grid's cancellation token
            // The grid's cancellation token can fire at unpredictable times causing BodyStreamBuffer abort errors
            CancellationToken = CancellationToken.None
        };

        var result = await _taskCardsDataSource.GetAsync(dataSourceRequest);
        Tasks = result.Items;
        TaskCardsCount = result.TotalItemCount;

        return GridItemsProviderResult.From(Tasks, TaskCardsCount);
    }

    private void CaptureColumnFilterRef(GridItemsProviderRequest<TaskCardListItem> request)
    {
        _instrumentColumnFilter ??= request.ColumnFilters?
            .FirstOrDefault(cf => cf.PropertyName == nameof(TaskCardListItem.InstrumentDisplayName));
    }

    private static List<DataSourceFilter> BuildDataSourceFilters(GridItemsProviderRequest<TaskCardListItem> request)
    {
        var filters = request.ColumnFilters?
            .SelectMany(cf => cf.FilterExpressions.Select(expr =>
                new DataSourceFilter(
                    cf.PropertyName,
                    PropertyToDataSourceFilterOperator(expr.Operator),
                    expr.UntypedValue)))
            .ToList() ?? [];

        return filters;
    }

    private void ApplyInstrumentCardFilter(List<DataSourceFilter> filters)
    {
        filters.RemoveAll(f => f.PropertyName == nameof(TaskCardListItem.InstrumentDisplayName));
        filters.Add(new DataSourceFilter(
            nameof(TaskCardListItem.InstrumentDisplayName),
            FilterOperator.EqualsTo,
            SelectedInstrumentCard));
    }

    private bool CheckIfColumnFiltersUpdated()
    {
        var columnInstrumentKeys = _instrumentColumnFilter?
            .FilterExpressions
            .Select(e => e.UntypedValue?.ToString())
            .Where(v => v is not null)
            .ToHashSet() ?? [];

        // User manually changed the column filter UI — clear the programmatic selection.
        return !columnInstrumentKeys.SetEquals([SelectedInstrumentCard]);
    }

    private void DeselectInstrumentCard()
    {
        SelectedInstrumentCard = null;
    }

    private void OnTaskCardsUpdated()
    {
        OnPropertyChanged(nameof(Tasks));
    }

    private static FilterOperator PropertyToDataSourceFilterOperator(PropertyFilterOperator propertyFilterOperator)
    {
        return propertyFilterOperator switch
        {
            PropertyFilterOperator.EqualsTo => FilterOperator.EqualsTo,
            PropertyFilterOperator.GreaterThan => FilterOperator.GreaterThan,
            PropertyFilterOperator.GreaterThanOrEqualTo => FilterOperator.GreaterThanOrEqualTo,
            PropertyFilterOperator.LessThan => FilterOperator.LessThan,
            PropertyFilterOperator.LessThanOrEqualTo => FilterOperator.LessThanOrEqualTo,
            PropertyFilterOperator.Contains => FilterOperator.Contains,
            _ => throw new ArgumentOutOfRangeException(nameof(propertyFilterOperator), propertyFilterOperator, null)
        };
    }

    public string GetTitle(TaskCardListItem taskCard)
    {
        return _translator.GetTitle(taskCard.CardType);
    }

    public static StatusType GetStatus(TaskCardState cardState)
    {
        return cardState switch
        {
            TaskCardState.FlashingRed or TaskCardState.Error => StatusType.Alarm,
            TaskCardState.Warning => StatusType.Warning,
            TaskCardState.Information => StatusType.Info,
            _ => StatusType.None
        };
    }

    public void Dispose()
    {
        _refreshNotificationProvider.RefreshNeeded -= OnRefreshNeeded;
        GC.SuppressFinalize(this);
    }
}
