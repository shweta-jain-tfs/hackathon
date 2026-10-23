using Pharos.Client.App.LoadList.ViewModels;
using Pharos.Client.App.LoadList.Views.LoadListGrid;
using Pharos.Client.App.LoadList.Views.LoadListHeaderView;
using Pharos.Client.Common.Models;
using Pharos.Common.Web.Components.Status;

namespace Pharos.Client.App.LoadList.Views;

public partial class LoadListPage : IDisposable
{
    [Inject] private LoadListPageViewModel PageViewModel { get; set; } = default!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;

    private List<IGridColumn<LoadListBottle>> BottleColumns { get; set; } = default!;
    private List<IGridColumn<LoadListCartridge>> CartridgeColumns { get; set; } = default!;
    private List<IGridColumn<LoadListCarrier>> CarriersColumns { get; set; } = default!;
    private List<IGridColumn<LoadListDilutionPlate>> DilutionPlateColumns { get; set; } = default!;
    private List<IGridColumn<LoadListSampleTube>> SampleTubeColumns { get; set; } = default!;

    private ICollection<LoadListBottle> BottleRows => GetBottleRows();
    private ICollection<LoadListCartridge> CartridgeRows => GetCartridgesRows();
    private ICollection<LoadListCarrier> CarrierRows => GetCarrierRows();
    private ICollection<LoadListDilutionPlate> DilutionPlateRows => GetDilutionPlateRows();
    private ICollection<LoadListSampleTube> SampleTubeRows => GetSampleTubeRows();

    private Dictionary<string, string>? _selectedInstruments;
    private List<LoadListStatus>? _selectedStatuses;
    private int _reagentLoaders;

    protected override async Task OnInitializedAsync()
    {
        SetColumns();

        await PageViewModel.LoadAsync();
        _reagentLoaders = await PageViewModel.GetReagentLoadersCount(_selectedInstruments?.Keys, _selectedStatuses);
        PageViewModel.PropertyChanged += PageViewModelOnPropertyChanged;
    }

    private async Task ApplyFilters(LoadListFilters filters)
    {
        _selectedInstruments = filters.Instruments;
        _selectedStatuses = filters.Statuses;
        _reagentLoaders = await PageViewModel.GetReagentLoadersCount(_selectedInstruments?.Keys, _selectedStatuses);
        StateHasChanged();
    }

    private ICollection<T> GetFilteredRows<T>(ICollection<T>? items, Func<T, string> instrumentSelector, Func<T, LoadListStatus> statusSelector)
    {
        if (items == null)
        {
            return Array.Empty<T>();
        }

        IEnumerable<T> query = items;

        // Filter by instruments if any are selected
        if (_selectedInstruments is { Count: > 0 })
        {
            query = query.Where(x => _selectedInstruments != null && _selectedInstruments.ContainsValue(instrumentSelector(x)));
        }

        // Filter by statuses if any are selected
        if (_selectedStatuses is { Count: > 0 })
        {
            query = query.Where(x => _selectedStatuses.Contains(statusSelector(x)));
        }

        return query.ToList();
    }

    private ICollection<LoadListBottle> GetBottleRows()
    {
        return GetFilteredRows(PageViewModel.BottleLoadList, x => x.InstrumentName, x => x.Status);
    }

    private ICollection<LoadListCartridge> GetCartridgesRows()
    {
        return GetFilteredRows(PageViewModel.CartridgeLoadList, x => x.InstrumentName, x => x.Status);
    }

    private ICollection<LoadListCarrier> GetCarrierRows()
    {
        return GetFilteredRows(PageViewModel.CarrierLoadList, x => x.InstrumentName, x => x.Status);
    }

    private ICollection<LoadListDilutionPlate> GetDilutionPlateRows()
    {
        return GetFilteredRows(PageViewModel.DilutionPlateLoadList, x => x.InstrumentName, x => x.Status);
    }

    private ICollection<LoadListSampleTube> GetSampleTubeRows()
    {
        return GetFilteredRows(PageViewModel.SampleTubeLoadList, x => x.InstrumentName, x => x.Status);
    }

    private void SetColumns()
    {
        BottleColumns = BuildBottleColumns();
        CartridgeColumns = BuildCartridgeColumns();
        CarriersColumns = BuildCarrierColumns();
        DilutionPlateColumns = BuildDilutionPlateColumns();
        SampleTubeColumns = BuildSampleTubeColumns();
    }

    private static StatusType GetStatus(LoadListStatus loadListStatus)
    {
        return loadListStatus switch
        {
            LoadListStatus.Alarm => StatusType.Alarm,
            LoadListStatus.Warning => StatusType.Warning,
            LoadListStatus.Pending => StatusType.Info,
            LoadListStatus.RoutineLoading => StatusType.None,
            _ => throw new ArgumentOutOfRangeException(nameof(loadListStatus))
        };
    }

    private List<IGridColumn<LoadListBottle>> BuildBottleColumns()
    {
        return
        [
            new StatusColumn<LoadListBottle>(Loc["LoadListPage_Status"], c => c.MinutesLeft,
                st => GetStatus(st.Status),
                text => GetTimeToDisplay(text.MinutesLeft)),
            new ColorColumn<LoadListBottle>(
                Loc["LoadListPage_BottleName"],
                c => c.FriendlyName,
                c => c.Shape,
                c => c.Colors)
            {
                ColumnSpan = 2,
                WrapText = true
            },
            new GridColumn<LoadListBottle, int>(Loc["LoadListPage_Quantity_ML"], c => c.QuantityToLoad)
            {
                AlignRight = true
            },
            new GridColumn<LoadListBottle, string?>(Loc["LoadListPage_Instrument"], c => c.InstrumentName),
            new GridColumn<LoadListBottle, string?>(Loc["LoadListPage_Lot"], c => c.Lot)
        ];
    }

    private List<IGridColumn<LoadListCartridge>> BuildCartridgeColumns()
    {
        return
        [
            new StatusColumn<LoadListCartridge>(Loc["LoadListPage_Status"], c => c.MinutesLeft, st => GetStatus(st.Status),
                text => GetTimeToDisplay(text.MinutesLeft)),
            new MultiColorColumn<LoadListCartridge>(
                Loc["LoadListPage_CartridgeMethod"],
                c => c.Texts,
                c => c.Shape,
                c => c.Colors)
            {
                SortFunc = x => x.Texts == null ? string.Empty : string.Join("/", x.Texts)
            },
            new GridColumn<LoadListCartridge, string?>(Loc["LoadListPage_CartridgeName"], c => c.FriendlyName)
            {
                WrapText = true
            },
            new GridColumn<LoadListCartridge, int>(Loc["LoadListPage_Quantity"], c => c.QuantityToLoad)
            {
                AlignRight = true
            },
            new GridColumn<LoadListCartridge, string?>(Loc["LoadListPage_Instrument"], c => c.InstrumentName),
            new GridColumn<LoadListCartridge, string?>(Loc["LoadListPage_Lot"], c => c.Lot)
        ];
    }

    private List<IGridColumn<LoadListCarrier>> BuildCarrierColumns()
    {
        return
        [
            new StatusColumn<LoadListCarrier>(Loc["LoadListPage_Status"], c => c.MinutesLeft, st => GetStatus(st.Status),
                text => GetTimeToDisplay(text.MinutesLeft)),
            new ColorColumn<LoadListCarrier>(
                Loc["LoadListPage_CarrierTestName"],
                c => c.FriendlyName,
                c => c.Shape,
                c => c.Colors),
            new GridColumn<LoadListCarrier, string>(Loc["LoadListPage_CarrierType"], c => c.ArticleType.ToLocalizedString()),
            new GridColumn<LoadListCarrier, int>(Loc["LoadListPage_Quantity"], c => c.QuantityToLoad)
            {
                AlignRight = true
            },
            new GridColumn<LoadListCarrier, string?>(Loc["LoadListPage_Instrument"], c => c.InstrumentName),
            new GridColumn<LoadListCarrier, string?>(Loc["LoadListPage_Lot"], c => c.Lot)
        ];
    }
    
    private List<IGridColumn<LoadListDilutionPlate>> BuildDilutionPlateColumns()
    {
        return
        [
            new StatusColumn<LoadListDilutionPlate>(Loc["LoadListPage_Status"], c => c.MinutesLeft, st => GetStatus(st.Status),
                text => GetTimeToDisplay(text.MinutesLeft)),
            new ColorColumn<LoadListDilutionPlate>(
                Loc["LoadListPage_DilutionPlateName"],
                c => c.FriendlyName,
                c => c.Shape,
                c => c.Colors)
            {
                ColumnSpan = 2,
                WrapText = true
            },
            new GridColumn<LoadListDilutionPlate, int>(Loc["LoadListPage_Quantity"], c => c.QuantityToLoad)
            {
                AlignRight = true
            },
            new GridColumn<LoadListDilutionPlate, string?>(Loc["LoadListPage_Instrument"], c => c.InstrumentName){
                ColumnSpan = 2
            }
        ];
    }

    private List<IGridColumn<LoadListSampleTube>> BuildSampleTubeColumns()
    {
        return
        [
            new StatusColumn<LoadListSampleTube>(Loc["LoadListPage_Status"], c => c.MinutesLeft, st => GetStatus(st.Status),
                text => GetTimeToDisplay(text.MinutesLeft)),
            new GridColumn<LoadListSampleTube, string?>(Loc["LoadListPage_SampleTubeName"], c => c.FriendlyName)
            {
                WrapText = true
            },
            new GridColumn<LoadListSampleTube, string>(Loc["LoadListPage_SampleTubeType"], c => c.SampleTubeType.ToLocalizedString()),
            new GridColumn<LoadListSampleTube, int>(Loc["LoadListPage_Quantity_UL"], c => c.QuantityToLoad)
            {
                AlignRight = true
            },
            new GridColumn<LoadListSampleTube, string?>(Loc["LoadListPage_Instrument"], c => c.InstrumentName),
            new GridColumn<LoadListSampleTube, string?>(Loc["LoadListPage_Lot"], c => c.Lot)
        ];
    }

    private string GetTimeToDisplay(int? minutesLeft)
    {
        minutesLeft ??= 0;
        if (minutesLeft < 0)
        {
            minutesLeft = 0;
        }

        var timeSpan = TimeSpan.FromMinutes(minutesLeft.Value);

        if (timeSpan.Days > 0)
        {
            return Loc["LoadListPage_Status_Days", timeSpan.Days];
        }

        if (timeSpan.Hours > 0)
        {
            return Loc["LoadListPage_Status_Hours", timeSpan.Hours];
        }

        return Loc["LoadListPage_Status_Minutes", timeSpan.Minutes];
    }

    private async void PageViewModelOnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "ReagentLoaders")
        {
            _reagentLoaders = await PageViewModel.GetReagentLoadersCount(_selectedInstruments?.Keys, _selectedStatuses);
        }

        StateHasChanged();
    }

    public void Dispose()
    {
        PageViewModel.PropertyChanged -= PageViewModelOnPropertyChanged;
        PageViewModel.Dispose();
        GC.SuppressFinalize(this);
    }
}