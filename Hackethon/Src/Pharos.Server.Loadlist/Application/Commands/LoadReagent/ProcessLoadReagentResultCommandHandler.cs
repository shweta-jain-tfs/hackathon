using Microsoft.EntityFrameworkCore;
using Pharos.Server.Application.LoadList;
using Pharos.Server.Application.Reagents;
using Pharos.Server.LoadList.Application.Events;
using Pharos.Server.LoadList.Domain;
using Tetris.Protocol.Common;

namespace Pharos.Server.LoadList.Application.Commands.LoadReagent;

internal class ProcessLoadReagentResultCommandHandler : IRequestHandler<ProcessLoadReagentResultCommand>
{
    private readonly ILoadListDbContext _dbContext;
    private readonly ILoadListKeyService _keyService;
    private readonly IReagentBarcodeService _barcodeService;

    public ProcessLoadReagentResultCommandHandler(ILoadListDbContext dbContext, ILoadListKeyService keyService, IReagentBarcodeService barcodeService)
    {
        _dbContext = dbContext;
        _keyService = keyService;
        _barcodeService = barcodeService;
    }

    public async Task Handle(ProcessLoadReagentResultCommand request, CancellationToken cancellationToken)
    {
        var articleType = GetArticleType(request.Request.ReagentType);
        var reagent = await _barcodeService.GetReagentInfo(articleType, request.Request.ReagentBarcode, request.Request.ReagentUdiBarcode);

        if (reagent == null)
        {
            return;
        }

        var key = await _keyService.GetKey(request.Request.ReagentType, request.Request.ReagentBarcode, request.Request.ReagentUdiBarcode);

        var changedOrUpdatedItems = new List<LastLoadedReagent>();

        var existingItem = await _dbContext.LastLoadedReagents.FirstOrDefaultAsync(x => x.InstrumentNumber == request.InstrumentSerial && x.ReagentKey == key, cancellationToken);

        if(existingItem != null)
        {
            if (existingItem.ArticleNumber != reagent.Article.ArticleNumber || existingItem.LotNumber != reagent.Lot.LotNumber)
            {
                existingItem.ArticleNumber = reagent.Article.ArticleNumber;
                existingItem.LotNumber = reagent.Lot.LotNumber;

                changedOrUpdatedItems.Add(existingItem);
            }
        }
        else
        {
            existingItem = new LastLoadedReagent
            {
                InstrumentNumber = request.InstrumentSerial,
                ReagentKey = key,
                ArticleNumber = reagent.Article.ArticleNumber,
                LotNumber = reagent.Lot.LotNumber
            };

            changedOrUpdatedItems.Add(existingItem);

            _dbContext.LastLoadedReagents.Add(existingItem);
        }

        if(_dbContext.HasChanges())
        {
            existingItem.AddDomainEvent(new LoadedReagentUpdatedEvent(request.InstrumentSerial, changedOrUpdatedItems));
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static ArticleType GetArticleType(EnumReagentType reagentType)
    {
        return reagentType switch
        {
            EnumReagentType.QualityControlCartridge => ArticleType.QualityControlCartridge,
            EnumReagentType.CalibratorCartridge => ArticleType.CalibratorCartridge,
            EnumReagentType.CurveControlCartridge => ArticleType.CurveControlCartridge,
            EnumReagentType.ConjugateCartridge => ArticleType.ConjugateCartridge,
            EnumReagentType.DevelopmentSolutionCartridge => ArticleType.DevelopmentSolutionCartridge,
            EnumReagentType.DiluentCartridge => ArticleType.DiluentCartridge,
            EnumReagentType.ImmunoCapCarrier => ArticleType.ImmunoCapCarrier,
            EnumReagentType.EliaWellCarrier => ArticleType.EliaWellCarrier,
            EnumReagentType.EliaCalibrationWellCarrier => ArticleType.EliaCalibratorWellCarrier,
            EnumReagentType.DilutionWellCarrier => ArticleType.DilutionWellCarrier,
            EnumReagentType.StopSolution => ArticleType.StopSolutionBottle,
            EnumReagentType.WashAdditive => ArticleType.WashAdditiveBottle,
            EnumReagentType.WashConcentrate => ArticleType.WashConcentrateBottle,
            EnumReagentType.MaintenanceConcentrate => ArticleType.MaintenanceConcentrateBottle,
            EnumReagentType.FluoroC => ArticleType.FluoroCVial,
            EnumReagentType.DilutionPlate => ArticleType.DilutionPlate,
            EnumReagentType.QualityControlTube => ArticleType.QualityControlTube,
            _ => throw new ArgumentOutOfRangeException(nameof(reagentType), reagentType, null)
        };
    }
}