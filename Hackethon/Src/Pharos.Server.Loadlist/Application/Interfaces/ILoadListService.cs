using System.Collections.Concurrent;
using Pharos.Server.LoadList.Application.Commands.HandleProcessListCommand;
using Pharos.Server.LoadList.Domain;
using Tetris.Protocol.Pharos.Consumables;

namespace Pharos.Server.LoadList.Application.Interfaces;

public interface ILoadListService
{
    ConcurrentDictionary<string, Dictionary<string, LoadListBottleDto>> GetLoadListBottles();
    ConcurrentDictionary<string, Dictionary<string, LoadListCarrierDto>> GetLoadListCarriers();
    ConcurrentDictionary<string, Dictionary<string, LoadListCartridgeDto>> GetLoadListCartridges();
    ConcurrentDictionary<string, Dictionary<string, LoadListDilutionPlateDto>> GetLoadListDilutionPlates();
    ConcurrentDictionary<string, Dictionary<string, LoadListSampleTubeDto>> GetLoadListSampleTubes();
    
    UrgencyLevel GetUrgencyLevel(string instrumentNumber);
    int GetReagentLoadersCount(IEnumerable<string>? instrumentNumbers = null, IEnumerable<LoadListStatus>? statuses = null);
    void ResetPlannedQuantity(string instrumentNumber);
    
    Task UpdateMissingBottleReagents(string key, MissingConsumable missingConsumable, DateTimeOffset? startTime, ArticleType articleType, string instrumentNumber, IArticleUsageService articleUsageService, CancellationToken cancellationToken);
    Task UpdateMissingCarrier(string key, MissingConsumable missingConsumable, DateTimeOffset? startTime, ArticleType articleType, string instrumentNumber, IArticleUsageService articleUsageService, CancellationToken cancellationToken);
    Task UpdateMissingCartridges(string key, MissingConsumable missingConsumable, DateTimeOffset? startTime, ArticleType articleType, string instrumentNumber, IArticleUsageService articleUsageService, CancellationToken cancellationToken);
    Task UpdateMissingQualityControl(string key, MissingConsumable missingConsumable, DateTimeOffset? startTime, string instrumentNumber, IArticleUsageService articleUsageService, CancellationToken cancellationToken);
    Task UpdateMissingDilutionCells(string key, MissingConsumable missingConsumable, DateTimeOffset? startTime, string instrumentNumber, IArticleUsageService articleUsageService, CancellationToken cancellationToken);
    void UpdateMissingSampleTubes(string key, MissingConsumable missingConsumable, DateTimeOffset? startTime, string instrumentNumber);
    Task UpdateMissingCalibrators(string key, MissingConsumable missingConsumable, DateTimeOffset? startTime, ArticleType calibratorCartridge, string instrumentNumber, IArticleUsageService articleUsageService, CancellationToken cancellationToken);
    Task UpdateMissingCurveControls(string key, MissingConsumable missingConsumable, DateTimeOffset? startTime, ArticleType curveControlCartridge, string instrumentNumber, IArticleUsageService articleUsageService, CancellationToken cancellationToken);
    Task UpdateMissingStop(string key, MissingConsumable missingConsumable, DateTimeOffset? startTime, ArticleType stopSolutionBottle, string instrumentNumber, IArticleUsageService articleUsageService, CancellationToken cancellationToken);
    void UpdateTriggerCarrier(string key, LoadListConfiguration item);
    void UpdateTriggerStop(string key, LoadListConfiguration item);
    void UpdateTriggerBottle(string key, LoadListConfiguration item);
    void UpdateTriggerCalibrator(string key, LoadListConfiguration item);
    void UpdateTriggerCartridge(string key, LoadListConfiguration item);
    void UpdateTriggerCurveControl(string key, LoadListConfiguration item);
    void UpdateTriggerDilutionPlate(string key, LoadListConfiguration item);
    void UpdateTriggerQualityControl(string key, LoadListConfiguration item);
    void UpdateLoadedCarriers(string instrumentNumber, bool isFullUpdate, Dictionary<string, int> carriers);
    void UpdateLoadedCartridges(string instrumentNumber, bool isFullUpdate, Dictionary<string, int> cartridges);
    void UpdateLoadedBottles(string instrumentNumber, bool isFullUpdate, Dictionary<string, int> bottles);
    void DeleteInstrument(string instrumentNumber);
}