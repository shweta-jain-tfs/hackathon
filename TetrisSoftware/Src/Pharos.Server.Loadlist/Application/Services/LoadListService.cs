using System.Collections.Concurrent;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.LoadList.Application.Commands.HandleProcessListCommand;
using Pharos.Server.LoadList.Domain;
using Tetris.Protocol.Pharos.Consumables;

namespace Pharos.Server.LoadList.Application.Services;

internal class LoadListService : ILoadListService
{
    private readonly ConcurrentDictionary<string, Dictionary<string, LoadListBottleDto>> _bottles = new();
    private readonly ConcurrentDictionary<string, Dictionary<string, LoadListCarrierDto>> _carriers = new();
    private readonly ConcurrentDictionary<string, Dictionary<string, LoadListCartridgeDto>> _cartridges = new();
    private readonly ConcurrentDictionary<string, Dictionary<string, LoadListDilutionPlateDto>> _dilutionPlates = new();
    private readonly ConcurrentDictionary<string, Dictionary<string, LoadListSampleTubeDto>> _sampleTubes = new();
    
    public int GetReagentLoadersCount(IEnumerable<string>? instrumentNumbers = null, IEnumerable<LoadListStatus>? statuses = null)
    {
        var totalLoaders = 0;

        var statusList = statuses?.ToList();

        var allInstrumentNumbers = _carriers.Keys.Union(_cartridges.Keys).Distinct().ToList();

        var instrumentNumbersList = instrumentNumbers?.ToList();

        var instrumentsToProcess = instrumentNumbersList == null || instrumentNumbersList.Count == 0
            ? allInstrumentNumbers
            : allInstrumentNumbers.Where(instrumentNumbersList.Contains);

        foreach (var instrumentNumber in instrumentsToProcess)
        {
            var totalSpaces = 0;

            if (_carriers.TryGetValue(instrumentNumber, out var carriers))
            {
                var filteredCarriers = statusList == null || statusList.Count == 0
                    ? carriers
                    : carriers.Where(c => statusList.Contains(c.Value.Status));

                //Each carrier takes 1 space
                totalSpaces += filteredCarriers.Sum(c => c.Value.QuantityToLoad);
            }

            if (_cartridges.TryGetValue(instrumentNumber, out var cartridges))
            {
                var filteredCartridges = statusList == null || statusList.Count == 0
                    ? cartridges
                    : cartridges.Where(c => statusList.Contains(c.Value.Status));

                //Each cartridge takes 6 spaces
                totalSpaces += filteredCartridges.Sum(c => c.Value.QuantityToLoad * 6);
            }

            //Each loader can hold 72 spaces
            var loadersForInstrument = (int)Math.Ceiling(totalSpaces / 72.0);
            totalLoaders += loadersForInstrument;
        }

        return totalLoaders;
    }

    public void ResetPlannedQuantity(string instrumentNumber)
    {
        ResetPlannedQuantity(_bottles, instrumentNumber);
        ResetPlannedQuantity(_carriers, instrumentNumber);
        ResetPlannedQuantity(_cartridges, instrumentNumber);
        ResetPlannedQuantity(_dilutionPlates, instrumentNumber);
        ResetPlannedQuantity(_sampleTubes, instrumentNumber);
    }

    private static void ResetPlannedQuantity<TItem>(ConcurrentDictionary<string, Dictionary<string, TItem>> storage, string instrumentNumber) where TItem : LoadListItemBase
    {
        if (!storage.TryGetValue(instrumentNumber, out var items))
        {
            return;
        }

        foreach (var item in items.Values)
        {
            item.ResetPlannedQuantity();
        }
    }

    private List<LoadListItemBase> AllMissingItems(string instrumentNumber)
    {
        var items = new List<LoadListItemBase>();
        if (_bottles.TryGetValue(instrumentNumber, out var bottles))
        {
            items.AddRange(bottles.Values);
        }
        if (_carriers.TryGetValue(instrumentNumber, out var carriers))
        {
            items.AddRange(carriers.Values);
        }
        if (_cartridges.TryGetValue(instrumentNumber, out var cartridges))
        {
            items.AddRange(cartridges.Values);
        }
        if (_dilutionPlates.TryGetValue(instrumentNumber, out var dilutionPlates))
        {
            items.AddRange(dilutionPlates.Values);
        }
        if (_sampleTubes.TryGetValue(instrumentNumber, out var sampleTubes))
        {
            items.AddRange(sampleTubes.Values);
        }

        return items;
    }

    public UrgencyLevel GetUrgencyLevel(string instrumentNumber)
    {
        var highestSeverity = AllMissingItems(instrumentNumber)
            .Where(x => x.Planned.MissingQuantity > 0)
            .OrderBy(x => x.Planned.StartTime == null)
            .ThenBy(x => x.Planned.StartTime)
            .FirstOrDefault();

        if (highestSeverity == null)
        {
            return new UrgencyLevel
            {
                AnythingMissing = false,
                TaskCardState = null,
                ExpiresAt = null
            };
        }

        if(!highestSeverity.Planned.StartTime.HasValue)
        {
            return new UrgencyLevel
            {
                AnythingMissing = true,
                TaskCardState = TaskCardState.Information,
                ExpiresAt = null
            };
        }

        var time = TimeSpan.FromMinutes(highestSeverity.MinutesLeft ?? 0);
        return new UrgencyLevel
        {
            AnythingMissing = true,
            TaskCardState = highestSeverity.Status switch
            {
                LoadListStatus.Alarm => TaskCardState.Error,
                LoadListStatus.Warning => TaskCardState.Warning,
                _ => TaskCardState.Information
            },
            ExpiresAt = time
        };
    }

    public ConcurrentDictionary<string, Dictionary<string, LoadListBottleDto>> GetLoadListBottles()
    {
        return _bottles;
    }

    public ConcurrentDictionary<string, Dictionary<string, LoadListCarrierDto>> GetLoadListCarriers()
    {
        return _carriers;
    }

    public ConcurrentDictionary<string, Dictionary<string, LoadListCartridgeDto>> GetLoadListCartridges()
    {
        return _cartridges;
    }

    public ConcurrentDictionary<string, Dictionary<string, LoadListDilutionPlateDto>> GetLoadListDilutionPlates()
    {
        return _dilutionPlates;
    }

    public ConcurrentDictionary<string, Dictionary<string, LoadListSampleTubeDto>> GetLoadListSampleTubes()
    {
        return _sampleTubes;
    }
    
    private static TDto? GetItem<TDto>(string instrumentNumber, string key, ConcurrentDictionary<string, Dictionary<string, TDto>> collection) where TDto : LoadListItemBase
    {
        if (collection.TryGetValue(instrumentNumber, out var itemsDict))
        {
            if (itemsDict.TryGetValue(key, out var item))
            {
                return item;
            }
        }

        return null;
    }

    internal void AddItemToBottleDict(string instrumentNumber, string key, LoadListBottleDto bottle)
    {
        _bottles.AddOrUpdate(
            instrumentNumber,
            // If instrumentNumber doesn't exist, create new dictionary with the item
            _ => new Dictionary<string, LoadListBottleDto> { { key, bottle } },
            // If instrumentNumber exists, add/update the item in existing dictionary
            (_, existingDict) =>
            {
                existingDict[key] = bottle;
                return existingDict;
            });
    }

    internal void AddItemToCarrierDict(string instrumentNumber, string key, LoadListCarrierDto carrier)
    {
        _carriers.AddOrUpdate(
            instrumentNumber,
            _ => new Dictionary<string, LoadListCarrierDto> { { key, carrier } },
            (_, existingDict) =>
            {
                existingDict[key] = carrier;
                return existingDict;
            });
    }

    internal void AddItemToCartridgeDict(string instrumentNumber, string key, LoadListCartridgeDto cartridge)
    {
        _cartridges.AddOrUpdate(
            instrumentNumber,
            _ => new Dictionary<string, LoadListCartridgeDto> { { key, cartridge } },
            (_, existingDict) =>
            {
                existingDict[key] = cartridge;
                return existingDict;
            });
    }

    internal void AddItemToDilutionPlateDict(string instrumentNumber, string key, LoadListDilutionPlateDto dilutionPlate)
    {
        _dilutionPlates.AddOrUpdate(
            instrumentNumber,
            _ => new Dictionary<string, LoadListDilutionPlateDto> { { key, dilutionPlate } },
            (_, existingDict) =>
            {
                existingDict[key] = dilutionPlate;
                return existingDict;
            });
    }

    internal void AddItemToSampleTubeDict(string instrumentNumber, string key, LoadListSampleTubeDto sampleTube)
    {
        _sampleTubes.AddOrUpdate(
            instrumentNumber,
            _ => new Dictionary<string, LoadListSampleTubeDto> { { key, sampleTube } },
            (_, existingDict) =>
            {
                existingDict[key] = sampleTube;
                return existingDict;
            });
    }

    public async Task UpdateMissingBottleReagents(string key, MissingConsumable missingConsumable, DateTimeOffset? startTime, ArticleType articleType, string instrumentNumber, IArticleUsageService articleUsageService, CancellationToken cancellationToken)
    {
        var existingBottle = GetItem(instrumentNumber, key, _bottles);

        if (existingBottle != null)
        {
            UpdateCommonProperties(existingBottle, startTime, missingConsumable);
        }
        else
        {
            var articleUsage = await articleUsageService.GetInformation(instrumentNumber, missingConsumable.ReagentName, articleType, null, cancellationToken);
            var newBottle = CreateLoadListItem<LoadListBottleDto>(missingConsumable, startTime, articleUsage, instrumentNumber, articleType);
            AddItemToBottleDict(instrumentNumber, key, newBottle);
        }
    }

    public void UpdateTriggerBottle(string key, LoadListConfiguration item)
    {
        var loadListItem = GetItem(item.InstrumentSerial, key, _bottles);
        if (loadListItem == null)
        {
            loadListItem = new LoadListBottleDto
            {
                ArticleIdentity = key,
                ArticleType = item.ArticleType,
                InstrumentName = item.InstrumentSerial,
                MethodName = null
            };

            AddItemToBottleDict(item.InstrumentSerial, key, loadListItem);
        }

        loadListItem.FriendlyName = item.FriendlyName;
        loadListItem.Shape = item.Shape;
        loadListItem.QuantityInArticle = item.Quantity;
        loadListItem.Colors = item.Colors ?? [];

        SetTrigger(item, loadListItem);
    }

    public void UpdateLoadedBottles(string instrumentNumber, bool isFullUpdate, Dictionary<string, int> bottles)
    {
        if (isFullUpdate)
        {
            ResetLoadedAmount(instrumentNumber, _bottles);
        }

        foreach (var bottle in bottles)
        {
            var loadListItem = GetItem(instrumentNumber, bottle.Key, _bottles);
            if (loadListItem != null)
            {
                SetLoadedAmount(bottle.Value, loadListItem);
            }
        }
    }

    public void DeleteInstrument(string instrumentNumber)
    {
        _bottles.Remove(instrumentNumber, out _);
        _carriers.Remove(instrumentNumber, out _);
        _cartridges.Remove(instrumentNumber, out _);
        _dilutionPlates.Remove(instrumentNumber, out _);
        _sampleTubes.Remove(instrumentNumber, out _);
    }

    public async Task UpdateMissingStop(string key, MissingConsumable missingConsumable, DateTimeOffset? startTime, ArticleType articleType, string instrumentNumber, IArticleUsageService articleUsageService, CancellationToken cancellationToken)
    {
        var existingBottle = GetItem(instrumentNumber, key, _bottles);

        if (existingBottle != null)
        {
            UpdateCommonProperties(existingBottle, startTime, missingConsumable);
        }
        else
        {
            var articleUsage = await articleUsageService.GetInformation(instrumentNumber, missingConsumable.ReagentName, articleType, null, cancellationToken);
            var newBottle = CreateLoadListItem<LoadListBottleDto>(missingConsumable, startTime, articleUsage, instrumentNumber, articleType);
            AddItemToBottleDict(instrumentNumber, key, newBottle);
        }
    }

    public void UpdateTriggerStop(string key, LoadListConfiguration item)
    {
        var loadListItem = GetItem(item.InstrumentSerial, key, _bottles);
        if (loadListItem == null)
        {
            loadListItem = new LoadListBottleDto
            {
                ArticleIdentity = key,
                ArticleType = item.ArticleType,
                InstrumentName = item.InstrumentSerial,
                MethodName = null
            };

            AddItemToBottleDict(item.InstrumentSerial, key, loadListItem);
        }

        loadListItem.FriendlyName = item.FriendlyName;
        loadListItem.Shape = item.Shape;
        loadListItem.QuantityInArticle = item.Quantity;
        loadListItem.Colors = item.Colors ?? [];
        
        SetTrigger(item, loadListItem);
    }

    public async Task UpdateMissingCarrier(string key, MissingConsumable missingConsumable, DateTimeOffset? startTime, ArticleType articleType, string instrumentNumber, IArticleUsageService articleUsageService, CancellationToken cancellationToken)
    {
        var existingCarrier = GetItem(instrumentNumber, key, _carriers);

        if (existingCarrier != null)
        {
            UpdateCommonProperties(existingCarrier, startTime, missingConsumable);
        }
        else
        {
            var articleUsage = await articleUsageService.GetInformation(instrumentNumber, missingConsumable.ReagentName, articleType, null, cancellationToken);
            var newCarrier = CreateLoadListItem<LoadListCarrierDto>(missingConsumable, startTime, articleUsage, instrumentNumber, articleType);
            AddItemToCarrierDict(instrumentNumber, key, newCarrier);
        }
    }

    public void UpdateTriggerCarrier(string key, LoadListConfiguration item)
    {
        var loadListItem = GetItem(item.InstrumentSerial, key, _carriers);
        if (loadListItem == null)
        {
            loadListItem = new LoadListCarrierDto
            {
                ArticleIdentity = key,
                ArticleType = item.ArticleType,
                InstrumentName = item.InstrumentSerial,
                MethodName = null
            };
            
            AddItemToCarrierDict(item.InstrumentSerial, key, loadListItem);
        }

        loadListItem.FriendlyName = item.FriendlyName;
        loadListItem.Shape = item.Shape;
        loadListItem.QuantityInArticle = item.Quantity;
        loadListItem.Colors = item.Colors ?? [];

        SetTrigger(item, loadListItem);
    }

    public void UpdateLoadedCarriers(string instrumentNumber, bool isFullUpdate, Dictionary<string, int> carriers)
    {
        if (isFullUpdate)
        {
            ResetLoadedAmount(instrumentNumber, _carriers);
        }

        foreach (var carrier in carriers)
        {
            var loadListItem = GetItem(instrumentNumber, carrier.Key, _carriers);
            if (loadListItem != null)
            {
                SetLoadedAmount(carrier.Value, loadListItem);
            }
        }
    }
    
    public async Task UpdateMissingCartridges(string key, MissingConsumable missingConsumable, DateTimeOffset? startTime, ArticleType articleType, string instrumentNumber, IArticleUsageService articleUsageService, CancellationToken cancellationToken)
    {
        var existingCartridge = GetItem(instrumentNumber, key, _cartridges);

        if (existingCartridge != null)
        {
            UpdateCommonProperties(existingCartridge, startTime, missingConsumable);
        }
        else
        {
            var articleUsage = await articleUsageService.GetInformation(instrumentNumber, missingConsumable.ReagentName, articleType, null, cancellationToken);
            var newCartridge = CreateLoadListItem<LoadListCartridgeDto>(missingConsumable, startTime, articleUsage, instrumentNumber, articleType);
            newCartridge.Texts = articleUsage.Texts;
            newCartridge.MethodName = null;
            AddItemToCartridgeDict(instrumentNumber, key, newCartridge);
        }
    }

    public void UpdateTriggerCartridge(string key, LoadListConfiguration item)
    {
        var loadListItem = GetItem(item.InstrumentSerial, key, _cartridges);
        if (loadListItem == null)
        {
            loadListItem = new LoadListCartridgeDto
            {
                ArticleIdentity = key,
                InstrumentName = item.InstrumentSerial,
                ArticleType = item.ArticleType,
                MethodName = null
            };

            AddItemToCartridgeDict(item.InstrumentSerial, key, loadListItem);
        }
        
        loadListItem.FriendlyName = item.FriendlyName;
        loadListItem.Shape = item.Shape;
        loadListItem.QuantityInArticle = item.Quantity;
        loadListItem.Colors = item.Colors ?? [];
        loadListItem.Texts = item.Texts;

        SetTrigger(item, loadListItem);
    }

    public void UpdateLoadedCartridges(string instrumentNumber, bool isFullUpdate, Dictionary<string, int> cartridges)
    {
        if (isFullUpdate)
        {
            ResetLoadedAmount(instrumentNumber, _cartridges);
        }

        foreach (var cartridge in cartridges)
        {
            var loadListItem = GetItem(instrumentNumber, cartridge.Key, _cartridges);
            if (loadListItem != null)
            {
                SetLoadedAmount(cartridge.Value, loadListItem);
            }
        }
    }

    public async Task UpdateMissingCalibrators(string key, MissingConsumable missingConsumable, DateTimeOffset? startTime, ArticleType articleType, string instrumentNumber, IArticleUsageService articleUsageService, CancellationToken cancellationToken)
    {
        var method = missingConsumable.ReagentName;
        
        var existingCartridge = GetItem(instrumentNumber, key, _cartridges);

        if (existingCartridge != null)
        {
            UpdateCommonProperties(existingCartridge, startTime, missingConsumable);
        }
        else
        {
            var articleUsage = await articleUsageService.GetInformation(instrumentNumber, string.Empty, articleType, method, cancellationToken);
            var newCartridge = CreateLoadListItem<LoadListCartridgeDto>(missingConsumable, startTime, articleUsage, instrumentNumber, articleType);
            newCartridge.MethodName = method;
            newCartridge.Texts = articleUsage.Texts;
            AddItemToCartridgeDict(instrumentNumber, key, newCartridge);
        }
    }

    public void UpdateTriggerCalibrator(string key, LoadListConfiguration item)
    {
        var loadListItem = GetItem(item.InstrumentSerial, key, _cartridges);
        if (loadListItem == null)
        {
            loadListItem = new LoadListCartridgeDto
            {
                ArticleIdentity = key,
                ArticleType = item.ArticleType,
                InstrumentName = item.InstrumentSerial,
                MethodName = item.MethodName
            };

            AddItemToCartridgeDict(item.InstrumentSerial, key, loadListItem);
        }

        loadListItem.FriendlyName = item.FriendlyName;
        loadListItem.Shape = item.Shape;
        loadListItem.QuantityInArticle = item.Quantity;
        loadListItem.Colors = item.Colors ?? [];
        loadListItem.Texts = item.Texts;

        SetTrigger(item, loadListItem);
    }

    public async Task UpdateMissingCurveControls(string key, MissingConsumable missingConsumable, DateTimeOffset? startTime, ArticleType articleType, string instrumentNumber, IArticleUsageService articleUsageService, CancellationToken cancellationToken)
    {
        var method = missingConsumable.ReagentName;
        
        var existingCartridge = GetItem(instrumentNumber, key, _cartridges);

        if (existingCartridge != null)
        {
            UpdateCommonProperties(existingCartridge, startTime, missingConsumable);
        }
        else
        {
            var articleUsage = await articleUsageService.GetInformation(instrumentNumber, string.Empty, articleType, method, cancellationToken);
            var newCartridge = CreateLoadListItem<LoadListCartridgeDto>(missingConsumable, startTime, articleUsage, instrumentNumber, articleType);
            newCartridge.MethodName = method;
            newCartridge.Texts = articleUsage.Texts;
            AddItemToCartridgeDict(instrumentNumber, key, newCartridge);
        }
    }

    public void UpdateTriggerCurveControl(string key, LoadListConfiguration item)
    {
        var loadListItem = GetItem(item.InstrumentSerial, key, _cartridges);
        if (loadListItem == null)
        {
            loadListItem = new LoadListCartridgeDto
            {
                ArticleIdentity = key,
                ArticleType = item.ArticleType,
                InstrumentName = item.InstrumentSerial,
                MethodName = item.MethodName
            };

            AddItemToCartridgeDict(item.InstrumentSerial, key, loadListItem);
        }

        loadListItem.FriendlyName = item.FriendlyName;
        loadListItem.Shape = item.Shape;
        loadListItem.QuantityInArticle = item.Quantity;
        loadListItem.Colors = item.Colors ?? [];
        loadListItem.Texts = item.Texts;

        SetTrigger(item, loadListItem);
    }

    public async Task UpdateMissingQualityControl(string key, MissingConsumable missingConsumable, DateTimeOffset? startTime, string instrumentNumber, IArticleUsageService articleUsageService, CancellationToken cancellationToken)
    {
        if (TryUpdateExistingQualityControl(key, instrumentNumber, missingConsumable, startTime))
        {
            return;
        }

        await CreateNewQualityControlItem(instrumentNumber, missingConsumable, startTime, articleUsageService, cancellationToken);
    }

    public void UpdateTriggerQualityControl(string key, LoadListConfiguration item)
    {
        if (item.ArticleType == ArticleType.QualityControlCartridge)
        {
            var loadListItem = GetItem(item.InstrumentSerial, key, _cartridges);
            if (loadListItem == null)
            {
                loadListItem = new LoadListCartridgeDto
                {
                    ArticleIdentity = key,
                    ArticleType = item.ArticleType,
                    InstrumentName = item.InstrumentSerial,
                    MethodName = null
                };

                AddItemToCartridgeDict(item.InstrumentSerial, key, loadListItem);
            }

            loadListItem.FriendlyName = item.FriendlyName;
            loadListItem.Shape = item.Shape;
            loadListItem.QuantityInArticle = item.Quantity;
            loadListItem.Colors = item.Colors ?? [];
            loadListItem.Texts = item.Texts;

            SetTrigger(item, loadListItem);
        }
        else if(item.ArticleType == ArticleType.QualityControlTube)
        {
            var loadListItem = GetItem(item.InstrumentSerial, key, _sampleTubes);
            if (loadListItem == null)
            {
                loadListItem = new LoadListSampleTubeDto
                {
                    ArticleIdentity = key,
                    ArticleType = item.ArticleType,
                    InstrumentName = item.InstrumentSerial,
                    MethodName = null,
                    SampleTubeType = SampleTubeType.PhadiaQualityControl
                };

                AddItemToSampleTubeDict(item.InstrumentSerial, key, loadListItem);
            }

            loadListItem.FriendlyName = item.FriendlyName;
            loadListItem.Shape = item.Shape;
            loadListItem.QuantityInArticle = item.Quantity;
            loadListItem.Colors = item.Colors ?? [];

            SetTrigger(item, loadListItem);
        }
    }
    
    private bool TryUpdateExistingQualityControl(string key, string instrumentNumber, MissingConsumable reagent, DateTimeOffset? startTime)
    {
        var existingSampleTube = GetItem(instrumentNumber, key, _sampleTubes);
        if (existingSampleTube != null)
        {
            UpdateCommonProperties(existingSampleTube, startTime, reagent);
            return true;
        }

        var existingCartridge = GetItem(instrumentNumber, key, _cartridges);
        if (existingCartridge != null)
        {
            UpdateCommonProperties(existingCartridge, startTime, reagent);
            return true;
        }

        return false;
    }

    private async Task CreateNewQualityControlItem(string instrumentNumber, MissingConsumable reagent, DateTimeOffset? startTime, IArticleUsageService articleUsageService, CancellationToken cancellationToken)
    {
        var articleUsageCartridge = await articleUsageService.GetInformation(instrumentNumber, reagent.ReagentName, ArticleType.QualityControlCartridge, null, cancellationToken);

        if (articleUsageCartridge.FullName == "Unknown article")
        {
            await CreateQualityControlTube(instrumentNumber, reagent, startTime, articleUsageService, cancellationToken);
        }
        else
        {
            CreateQualityControlCartridge(instrumentNumber, reagent, startTime, articleUsageCartridge);
        }
    }

    private async Task CreateQualityControlTube(string instrumentNumber, MissingConsumable reagent, DateTimeOffset? startTime, IArticleUsageService articleUsageService, CancellationToken cancellationToken)
    {
        var articleUsageTube = await articleUsageService.GetInformation(instrumentNumber, reagent.ReagentName, ArticleType.QualityControlTube, null, cancellationToken);

        var qcType = await articleUsageService.GetQualityControlType(reagent.ReagentName, cancellationToken);

        var item = new LoadListSampleTubeDto
        {
            QuantityInArticle = articleUsageTube.Quantity,
            InstrumentName = instrumentNumber,
            ArticleType = ArticleType.QualityControlTube,
            SampleTubeType = qcType.IsPhadiaQualityControl ? SampleTubeType.PhadiaQualityControl : qcType.IsLabQualityControl ? SampleTubeType.LabQualityControl : SampleTubeType.Unknown,
            ArticleIdentity = reagent.ReagentName,
            FriendlyName = articleUsageTube.FriendlyName ?? articleUsageTube.ArticleIdentity
        };
        item.SetPlanned(startTime, reagent.MissingQuantity, reagent.LotNumber);

        SetLotNumber(articleUsageTube, item);
        AddItemToSampleTubeDict(instrumentNumber, reagent.ReagentName, item);
    }

    private void CreateQualityControlCartridge(string instrumentNumber, MissingConsumable reagent, DateTimeOffset? startTime, ArticleUsage articleUsageCartridge)
    {
        var newCartridge = CreateLoadListItem<LoadListCartridgeDto>(reagent, startTime, articleUsageCartridge, instrumentNumber, ArticleType.QualityControlCartridge);
        newCartridge.Texts = articleUsageCartridge.Texts;
        AddItemToCartridgeDict(instrumentNumber, reagent.ReagentName, newCartridge);
    }

    public async Task UpdateMissingDilutionCells(string key, MissingConsumable missingConsumable, DateTimeOffset? startTime, string instrumentNumber, IArticleUsageService articleUsageService, CancellationToken cancellationToken)
    {
        var existingDilutionPlate = GetItem(instrumentNumber, key, _dilutionPlates);

        if (existingDilutionPlate != null)
        {
            UpdateCommonProperties(existingDilutionPlate, startTime, missingConsumable);
        }
        else
        {
            var articleUsage = await articleUsageService.GetInformation(instrumentNumber, string.Empty, ArticleType.DilutionPlate, null, cancellationToken);
            var newDilutionPlate = CreateLoadListItem<LoadListDilutionPlateDto>(missingConsumable, startTime, articleUsage, instrumentNumber, ArticleType.DilutionPlate);

            newDilutionPlate.SetPlanned(startTime, missingConsumable.MissingQuantity, missingConsumable.LotNumber);
            
            AddItemToDilutionPlateDict(instrumentNumber, key, newDilutionPlate);
        }
    }

    public void UpdateTriggerDilutionPlate(string key, LoadListConfiguration item)
    {
        var loadListItem = GetItem(item.InstrumentSerial, key, _dilutionPlates);
        if (loadListItem == null)
        {
            loadListItem = new LoadListDilutionPlateDto
            {
                ArticleIdentity = key,
                ArticleType = item.ArticleType,
                InstrumentName = item.InstrumentSerial,
                MethodName = null
            };

            AddItemToDilutionPlateDict(item.InstrumentSerial, key, loadListItem);
        }
        
        loadListItem.FriendlyName = item.FriendlyName;
        loadListItem.Shape = item.Shape;
        loadListItem.QuantityInArticle = item.Quantity;
        loadListItem.Colors = item.Colors ?? [];

        SetTrigger(item, loadListItem);
    }

    public void UpdateMissingSampleTubes(string key, MissingConsumable missingConsumable, DateTimeOffset? startTime, string instrumentNumber)
    {
        var existingSampleTube = GetItem(instrumentNumber, key, _sampleTubes);

        if (existingSampleTube != null)
        {
            UpdateCommonProperties(existingSampleTube, startTime, missingConsumable);
        }
        else
        {
            var newSampleTube = new LoadListSampleTubeDto
            {
                ArticleIdentity = missingConsumable.ReagentName,
                ArticleType = ArticleType.None,
                SampleTubeType = SampleTubeType.Sample,
                InstrumentName = instrumentNumber,
                QuantityInArticle = 1,
                FriendlyName = missingConsumable.ReagentName
            };
            newSampleTube.SetPlanned(startTime, missingConsumable.MissingQuantity, missingConsumable.LotNumber);

            AddItemToSampleTubeDict(instrumentNumber, key, newSampleTube);
        }
    }
    
    private static void UpdateCommonProperties(LoadListItemBase item, DateTimeOffset? startTime, MissingConsumable reagent)
    {
        var missing = item.Planned.MissingQuantity += reagent.MissingQuantity;
        
        item.SetPlanned(startTime, missing, reagent.LotNumber);
    }

    private static TItem CreateLoadListItem<TItem>(MissingConsumable reagent, DateTimeOffset? startTime, ArticleUsage articleUsage, string instrumentNumber, ArticleType articleType) where TItem : LoadListItemBase, new()
    {
        var item = new TItem
        {
            QuantityInArticle = articleUsage.Quantity,
            InstrumentName = instrumentNumber,
            Shape = articleUsage.Shape,
            ArticleType = articleType,
            ArticleIdentity = reagent.ReagentName,
            Colors = articleUsage.Colors,
            FriendlyName = articleUsage.FriendlyName ?? articleUsage.ArticleIdentity
        };
        item.SetPlanned(startTime, reagent.MissingQuantity, reagent.LotNumber);

        SetLotNumber(articleUsage, item);
        return item;
    }

    private static void SetLotNumber(ArticleUsage articleUsage, LoadListItemBase newItem)
    {
        if (string.IsNullOrWhiteSpace(newItem.Planned.Lot))
        {
            newItem.Planned.Lot = articleUsage.LotNumber;
        }
    }

    private static void ResetLoadedAmount<TDto>(string instrumentNumber, ConcurrentDictionary<string, Dictionary<string, TDto>> collection) where TDto : LoadListItemBase
    {
        if (collection.TryGetValue(instrumentNumber, out var itemsDict))
        {
            foreach (var item in itemsDict.Values)
            {
                item.SetTrigger(item.Trigger.RefillLevel, item.Trigger.TriggerLevel, 0, item.Trigger.Lot);
            }
        }
    }

    private static void SetLoadedAmount(int carrierValue, LoadListItemBase loadListItem)
    {
        loadListItem.SetTrigger(loadListItem.Trigger.RefillLevel, loadListItem.Trigger.TriggerLevel, carrierValue, loadListItem.Trigger.Lot);
    }

    private static void SetTrigger(LoadListConfiguration item, LoadListItemBase loadListItem)
    {
        loadListItem.SetTrigger(item.RefillLevel, item.TriggerLevel, loadListItem.Trigger.Loaded, loadListItem.Trigger.Lot);
    }
}