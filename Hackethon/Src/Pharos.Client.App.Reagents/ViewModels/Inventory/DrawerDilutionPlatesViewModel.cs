using Pharos.Client.App.Reagents.Models.Inventory.DilutionPlates;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models;
using Pharos.Client.Common.Models.DataServices;
using Pharos.Client.Common.ViewModels;
using Pharos.Common.Web.Models.DataGrid;

namespace Pharos.Client.App.Reagents.ViewModels.Inventory
{
    public class DrawerDilutionPlatesViewModel : LoadingViewModel, IDisposable
    {
        private readonly IDilutionPlatesNotificationProvider _dilutionPlatesNotificationProvider;
        private readonly IEntityDataSource<DrawerDilutionPlatesResult> _drawerDilutionPlatesDataSource;
        private IEnumerable<DrawerDilutionPlate> _drawerDilutionPlates = [];
        private bool _isNotificationSubscribed;
        private List<string> _previousInstrumentFilters = new();

        public event Action<List<DataSourceFilter>>? InstrumentFilterChanged;
        public IEnumerable<DrawerDilutionPlate> DrawerDilutionPlates { get; set; } = [];

        public DrawerDilutionPlatesViewModel(IEntityDataSource<DrawerDilutionPlatesResult> drawerDilutionPlatesDataSource,
            IDilutionPlatesNotificationProvider dilutionPlatesNotificationProvider)
        {
            _drawerDilutionPlatesDataSource = drawerDilutionPlatesDataSource;
            _dilutionPlatesNotificationProvider = dilutionPlatesNotificationProvider;
        }

        #region Public/Protected Methods Members

        public int DilutionPlatesCount
        {
            get;
            private set
            {
                if (field != value)
                {
                    field = value;
                    OnPropertyChanged(nameof(DilutionPlatesCount));
                }
            }
        }

        public int DilutionPlatesWellsCount
        {
            get;
            private set
            {
                if (field != value)
                {
                    field = value;
                    OnPropertyChanged(nameof(DilutionPlatesWellsCount));
                }
            }
        }

        protected override Task DoLoadAsync()
        {
            if (!_isNotificationSubscribed)
            {
                _dilutionPlatesNotificationProvider.DrawerDilutionPlatesChanged += OnInventoryDrawerDilutionPlatesUpdated;
                _isNotificationSubscribed = true;
            }
            return Task.CompletedTask;
        }

        public async ValueTask<GridItemsProviderResult<DrawerDilutionPlate>> ProvideItems(GridItemsProviderRequest<DrawerDilutionPlate> request)
        {
            var filters = request.ColumnFilters?
                .SelectMany(cf => cf.FilterExpressions.Select(expr =>
                    new DataSourceFilter(
                        cf.PropertyName,
                        MapPropertyFilterOperatorToFilterOperator(expr.Operator),
                        expr.UntypedValue
                    )))
                .ToList() ?? new List<DataSourceFilter>();

            var dataSourceRequest = new DataSourceRequest
            {
                StartIndex = request.StartIndex,
                Count = request.Count,
                SortByProperty = request.SortByColumn == nameof(DrawerDilutionPlate.Id) ? null : request.SortByColumn,
                SortByAscending = request.SortByAscending,
                Filters = filters,
                CancellationToken = request.CancellationToken
            };

            var result = await _drawerDilutionPlatesDataSource.GetAsync(dataSourceRequest);
            var drawerDilutionPlateListResult = result.Items.FirstOrDefault();
            var items = drawerDilutionPlateListResult?.Items ?? Array.Empty<DrawerDilutionPlate>();
            _drawerDilutionPlates = items;
            DilutionPlatesCount = drawerDilutionPlateListResult?.PlatesCount ?? 0;
            DilutionPlatesWellsCount = drawerDilutionPlateListResult?.WellsCount ?? 0;
            var totalCount = result.TotalItemCount;

            CheckFiltersChanged(filters);

            return GridItemsProviderResult.From(_drawerDilutionPlates.ToList(), totalCount);
        }

        public void Dispose()
        {
            if (_isNotificationSubscribed)
            {
                _dilutionPlatesNotificationProvider.DrawerDilutionPlatesChanged -= OnInventoryDrawerDilutionPlatesUpdated;
                _isNotificationSubscribed = false;
            }
            GC.SuppressFinalize(this);
        }

        public Task OnInventoryDrawerDilutionPlatesUpdated()
        {
            OnPropertyChanged(nameof(DrawerDilutionPlates));
            return Task.CompletedTask;
        }

        public static bool HasStatusError(ReagentPositionStatus status)
        {
            return status == ReagentPositionStatus.DilutionPlateNotDetected ||
                   status == ReagentPositionStatus.DilutionPlateNotPickedUp;
        }

        public static string GetStatusText(ReagentPositionStatus status)
        {
            var state = status.ToLocalizedString("Reagent");
            return state;
        }
        #endregion

        #region Private Methods Members
        private void CheckFiltersChanged(List<DataSourceFilter> filters)
        {
            // Extract selected instruments from filters
            var currentInstrumentFilters = filters
                .Where(f => f.PropertyName == nameof(DrawerDilutionPlate.Instrument))
                .Select(f => f.FilterValue?.ToString())
                .OfType<string>()
                .ToList();

            // Compare with previous filters and invoke event only if filters changed
            var filtersChanged = !currentInstrumentFilters.SequenceEqual(_previousInstrumentFilters);
            if (filtersChanged)
            {
                InstrumentFilterChanged?.Invoke(filters);
                _previousInstrumentFilters = [.. currentInstrumentFilters];
            }
        }

        private static FilterOperator MapPropertyFilterOperatorToFilterOperator(PropertyFilterOperator propertyFilterOperator)
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
        #endregion
    }
}