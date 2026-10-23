using System.Linq.Expressions;
using Pharos.Client.App.Quality.Models;
using Pharos.Client.Common.ViewModels;
using Pharos.Common.Web.Models.DataGrid;

namespace Pharos.Client.App.Quality.ViewModels;

public sealed class MethodQualityControlDetailsViewModel : ViewModelItem<QualityControl>
{
    private readonly IMethodQualityControlDetailsDataSource _dataSource;
    private readonly List<QualityControlTubeViewModel> _tubes;

    public MethodQualityControlDetailsViewModel(QualityControl model, IMethodQualityControlDetailsDataSource dataSource) : base(model)
    {
        _dataSource = dataSource;

        ConstructLotsFilter(model.Cartridges);

        _tubes = [];

        foreach (var qualityControlTube in model.Tubes)
        {
            _tubes.Add(new QualityControlTubeViewModel(qualityControlTube));
        }
    }

    public Dictionary<string, string> CartridgeLotDictionary { get; } = new();

    public bool IsQcInTube => Model.IsQcInTube;

    private void ConstructLotsFilter(ICollection<UnloadableQualityControlCartridge> cartridges)
    {
        CartridgeLotDictionary.Clear();

        var uniqueLots = cartridges.Select(x => x.Lot).OrderBy(x => x).Distinct();

        foreach (var lot in uniqueLots)
        {
            CartridgeLotDictionary.Add(lot, lot);
        }
    }

    private async Task ReloadLots()
    {
        var qualityControl = await _dataSource.GetByIdAsync(Model.Id);
        Model.Lots = qualityControl.Lots;
    }

    public async Task<int> CreateNewLot(QualityControlLot lot)
    {
        if (Model.Id == 0)
        {
            throw new InvalidOperationException("Cannot add a lot to a QualityControl that has not been saved yet.");
        }

        var createdLotId = await _dataSource.CreateLot(Model.Id, lot);
        await ReloadLots();
        return createdLotId;
    }

    public async Task<bool> DeleteLots(List<int> lotIds)
    {
        if (Model.Id == 0)
        {
            throw new InvalidOperationException("Cannot delete lots from a QualityControl that has not been saved yet.");
        }

        var isDeleted = await _dataSource.DeleteLots(lotIds);
        await ReloadLots();
        return isDeleted;
    }

    public async Task<int> UpdateLot(QualityControlLot lot)
    {
        if (lot.Id == 0)
        {
            throw new InvalidOperationException("Lot is missing id.");
        }

        var createdLotId = await _dataSource.UpdateLot(lot);
        await ReloadLots();
        return createdLotId;
    }

    public ValueTask<GridItemsProviderResult<UnloadableQualityControlCartridge>> CartridgesProvider(GridItemsProviderRequest<UnloadableQualityControlCartridge> request)
    {
        var ds = request.ToDataSourceRequest(true);

        var lot = FilterBuilder.GetList(ds.Filters, nameof(UnloadableQualityControlCartridge.Lot));
        var instrument = FilterBuilder.GetList(ds.Filters, nameof(UnloadableQualityControlCartridge.InstrumentName));

        var selector = FilterBuilder.True<UnloadableQualityControlCartridge>();

        if (lot.Count > 0)
        {
            selector = selector.And(x => lot.Contains(x.Lot));
        }

        if (instrument.Count > 0)
        {
            selector = selector.And(x => instrument.Contains(x.InstrumentName));
        }

        return GetItemsToDisplay(Model.Cartridges, selector, request);
    }

    public ValueTask<GridItemsProviderResult<QualityControlTubeViewModel>> TubesProvider(GridItemsProviderRequest<QualityControlTubeViewModel> request)
    {
        var ds = request.ToDataSourceRequest(true);

        var detectedDate = FilterBuilder.GetToAndFromDate(ds.Filters, nameof(UnloadableQualityControlTube.DetectedDateTime));

        var selector = FilterBuilder.True<QualityControlTubeViewModel>();

        if (detectedDate.FromDate.HasValue)
        {
            selector = selector.And(x => x.DetectedDateTime.Date >= detectedDate.FromDate.Value.Date);
        }

        if (detectedDate.ToDate.HasValue)
        {
            selector = selector.And(x => x.DetectedDateTime.Date <= detectedDate.ToDate.Value.Date);
        }

        return GetItemsToDisplay(_tubes, selector, request);
    }

    public ValueTask<GridItemsProviderResult<QualityControlLot>> LotsProvider(GridItemsProviderRequest<QualityControlLot> request)
    {
        var ds = request.ToDataSourceRequest(true);

        var registryDate = FilterBuilder.GetToAndFromDate(ds.Filters, nameof(QualityControlLot.RegistrationDate));
        var hideExpired = FilterBuilder.GetBoolean(ds.Filters, nameof(QualityControlLot.HideExpired));

        var selector = FilterBuilder.True<QualityControlLot>();

        if (registryDate.FromDate.HasValue)
        {
            selector = selector.And(x => x.RegistrationDate.HasValue && x.RegistrationDate.Value.Date >= registryDate.FromDate.Value.Date);
        }

        if (registryDate.ToDate.HasValue)
        {
            selector = selector.And(x => x.RegistrationDate.HasValue && x.RegistrationDate.Value.Date <= registryDate.ToDate.Value.Date);
        }

        if (hideExpired.HasValue && hideExpired.Value)
        {
            selector = selector.And(x => !x.ExpirationDate.HasValue || x.ExpirationDate.Value.Date >= DateTimeOffset.Now.Date);
        }

        return GetItemsToDisplay(Model.Lots, selector, request);
    }

    private static ValueTask<GridItemsProviderResult<T>> GetItemsToDisplay<T>(ICollection<T> allItems, Expression<Func<T, bool>> selector, GridItemsProviderRequest<T> request)
    {
        return new ValueTask<GridItemsProviderResult<T>>(GridItemsProviderResult.From(allItems, selector, request));
    }

    public async Task UnloadReagents(HashSet<UnloadableQualityControlCartridge> selectedCartridges)
    {
        if (selectedCartridges.Count == 0)
        {
            return;
        }

        var unloadCartridges = selectedCartridges.Select(cartridge => new UnloadReagentRequestItem
        {
            Barcode = cartridge.Barcode,
            InstrumentNumber = cartridge.InstrumentNumber,
            Position = cartridge.Position,
            AbortUnload = false,
            StorageArea = GetReagentStorageArea(cartridge.Area)
        }).ToList();

        await _dataSource.UnloadReagents(unloadCartridges);
    }

    private static ReagentStorageArea GetReagentStorageArea(CartridgeStorageArea cartridgeArea)
    {
        return cartridgeArea switch
        {
            CartridgeStorageArea.BackStorage => ReagentStorageArea.RearCartridgeStorageArea,
            CartridgeStorageArea.FrontStorage => ReagentStorageArea.FrontCartridgeStorageArea,
            _ => throw new ArgumentOutOfRangeException(nameof(cartridgeArea), cartridgeArea, null)
        };
    }

    public async Task UnloadTubes(HashSet<QualityControlTubeViewModel> selectedTubes)
    {
        if (selectedTubes.Count == 0)
        {
            return;
        }

        var tubeIds = selectedTubes.Select(t => t.Model.Id).ToList();
        await _dataSource.UnloadTubes(tubeIds);
    }
}