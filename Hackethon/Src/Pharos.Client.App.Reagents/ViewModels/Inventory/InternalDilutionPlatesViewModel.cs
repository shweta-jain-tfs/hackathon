using Pharos.Client.App.Reagents.Models.Inventory;
using Pharos.Client.App.Reagents.Models.Inventory.DilutionPlates;
using Pharos.Client.Common.Models;
using Pharos.Client.Common.Models.DataServices;
using Pharos.Client.Common.ViewModels;
using Pharos.Common.Web.Models.DataGrid;

namespace Pharos.Client.App.Reagents.ViewModels.Inventory
{
    public class InternalDilutionPlatesViewModel : LoadingViewModel, IDisposable
    {
        private readonly IEntityDataSource<InternalDilutionPlatesResult> _internalDilutionPlatesDataSource;
        private readonly IDilutionPlatesNotificationProvider _dilutionPlatesNotificationProvider;
        private readonly IReagentDataSource _reagentDataSource;
        private IEnumerable<InternalDilutionPlate> _internalDilutionPlates = [];
        private bool _isNotificationSubscribed;

        public InternalDilutionPlatesViewModel(
            IEntityDataSource<InternalDilutionPlatesResult> internalDilutionPlatesDataSource,
            IDilutionPlatesNotificationProvider dilutionPlatesNotificationProvider,
            IReagentDataSource reagentDataSource)
        {
            _internalDilutionPlatesDataSource = internalDilutionPlatesDataSource;
            _dilutionPlatesNotificationProvider = dilutionPlatesNotificationProvider;
            _reagentDataSource = reagentDataSource;
        }

        #region Public/Protected Methods Members
        public HashSet<InternalDilutionPlate>? SelectedInternalDilutionPlates { get; private set; } = new();

        public IEnumerable<InternalDilutionPlate> InternalDilutionPlates
        {
            get => _internalDilutionPlates;
            private set
            {
                if (_internalDilutionPlates != value)
                {
                    _internalDilutionPlates = value;
                    OnPropertyChanged(nameof(InternalDilutionPlates));
                }
            }
        }

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

        public int FreeWellsCount
        {
            get;
            private set
            {
                if (field != value)
                {
                    field = value;
                    OnPropertyChanged(nameof(FreeWellsCount));
                }
            }
        }

        public List<DataSourceFilter> DrawerDilutionPlatesFilters
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    ClearAllSelections();
                    OnPropertyChanged(nameof(DrawerDilutionPlatesFilters));
                }
            }
        } = new();

        protected override Task DoLoadAsync()
        {
            if (!_isNotificationSubscribed)
            {
                _dilutionPlatesNotificationProvider.InternalDilutionPlatesChanged += OnInventoryInternalDilutionPlatesUpdated;
                _isNotificationSubscribed = true;
            }
            return Task.CompletedTask;
        }

        public async ValueTask<GridItemsProviderResult<InternalDilutionPlate>> ProvideItems(GridItemsProviderRequest<InternalDilutionPlate> request)
        {
            var filters = request.ColumnFilters?
                .SelectMany(cf => cf.FilterExpressions.Select(expr =>
                    new DataSourceFilter(
                        cf.PropertyName,
                        MapPropertyFilterOperatorToFilterOperator(expr.Operator),
                        expr.UntypedValue
                    )))
                .ToList() ?? new List<DataSourceFilter>();

            // Add instrument filters from parent
            if (DrawerDilutionPlatesFilters != null && DrawerDilutionPlatesFilters.Count > 0)
            {
                filters.RemoveAll(f => f.PropertyName == nameof(DrawerDilutionPlate.Instrument));
                filters.AddRange(DrawerDilutionPlatesFilters);
            }

            var dataSourceRequest = new DataSourceRequest
            {
                StartIndex = request.StartIndex,
                Count = request.Count,
                SortByProperty = request.SortByColumn == nameof(CarrierListItem.Id) ? null : request.SortByColumn,
                SortByAscending = request.SortByAscending,
                Filters = filters,
                RequiresRowLimitBypass = request.RequiresRowLimitBypass,
                CancellationToken = request.CancellationToken
            };

            var result = await _internalDilutionPlatesDataSource.GetAsync(dataSourceRequest);
            var internalDilutionPlatesListResult = result.Items.FirstOrDefault();
            var items = internalDilutionPlatesListResult?.Items ?? Array.Empty<InternalDilutionPlate>();
            _internalDilutionPlates = items;
            UpdateSelection();
            DilutionPlatesCount = internalDilutionPlatesListResult?.PlatesCount ?? 0;
            FreeWellsCount = internalDilutionPlatesListResult?.FreeWellsCount ?? 0;
            var totalCount = result.TotalItemCount;
            return GridItemsProviderResult.From(_internalDilutionPlates.ToList(), totalCount);
        }

        public void Dispose()
        {
            if (_isNotificationSubscribed)
            {
                _dilutionPlatesNotificationProvider.InternalDilutionPlatesChanged -= OnInventoryInternalDilutionPlatesUpdated;
                _isNotificationSubscribed = false;
            }
            GC.SuppressFinalize(this);
        }

        public Task OnInventoryInternalDilutionPlatesUpdated()
        {
            OnPropertyChanged(nameof(InternalDilutionPlates));
            return Task.CompletedTask;
        }

        public void OnSelectedItemsChanged(HashSet<InternalDilutionPlate> selected)
        {
            SelectedInternalDilutionPlates = selected;
        }

        public async Task UnloadSelectedDilutionPlatesAsync(HashSet<int> unloadDilutionPlateIds)
        {
            var unloadDilutionPlates = _internalDilutionPlates.Where(x => unloadDilutionPlateIds.Contains(x.Id)).ToList();
            if (unloadDilutionPlates.Count == 0)
            {
                return;
            }
            var unloadReagentRequestItems = MapUnloadDilutionPlateToReagentRequest(unloadDilutionPlates, false);
            await _reagentDataSource.UnloadReagentAsync(unloadReagentRequestItems);
        }

        public HashSet<InternalDilutionPlate> GetCurrentItemSelection()
        {
            if (SelectedInternalDilutionPlates?.Count > 0)
            {
                return _internalDilutionPlates.Where(x => SelectedInternalDilutionPlates.Any(s => s.Id == x.Id)).ToHashSet();
            }

            return new();
        }
        #endregion

        #region Private Methods Members
        private void ClearAllSelections()
        {
            if (SelectedInternalDilutionPlates != null && SelectedInternalDilutionPlates.Count > 0)
            {
                SelectedInternalDilutionPlates.Clear();
                OnPropertyChanged(nameof(SelectedInternalDilutionPlates));
            }
        }

        private void UpdateSelection()
        {
            var currentPlateIds = _internalDilutionPlates.Select(p => p.Id).ToHashSet();
            SelectedInternalDilutionPlates?.RemoveWhere(plate => !currentPlateIds.Contains(plate.Id));
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

        private static List<UnloadReagentRequestItem> MapUnloadDilutionPlateToReagentRequest(List<InternalDilutionPlate> unloadDilutionPlates, bool abortUnload = true)
        {
            return unloadDilutionPlates.Select(dilutionPlate => new UnloadReagentRequestItem
            {
                Barcode = string.Empty,
                InstrumentNumber = dilutionPlate.InstrumentNumber,
                Position = (int)dilutionPlate.Position,
                AbortUnload = abortUnload,
                StorageArea = ReagentStorageArea.DilutionStationArea,
            }).ToList();
        }
        #endregion
    }
}