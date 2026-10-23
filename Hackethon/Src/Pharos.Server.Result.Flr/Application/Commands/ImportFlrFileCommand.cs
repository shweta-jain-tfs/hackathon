using Google.Protobuf.WellKnownTypes;
using MediatR;
using Microsoft.Extensions.Logging;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Result;
using Pharos.Server.Result.Flr.Application.Mapping;
using Pharos.Server.Result.Flr.Application.Parsers;
using Tetris.Protocol.Common;
using Tetris.Protocol.Pharos.TestResult;
using BlankReplicate = Tetris.Protocol.Pharos.TestResult.BlankReplicate;

namespace Pharos.Server.Result.Flr.Application.Commands;

internal record ImportFlrFileCommand(string FileName, StreamReader FileReader) : IRequest;

internal class ImportFlrFileCommandHandler(
    IMediator mediator,
    IArticlesService articlesService,
    ILogger<ImportFlrFileCommandHandler> logger) : IRequestHandler<ImportFlrFileCommand>
{
    public async Task Handle(ImportFlrFileCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Reading FLR file {FileName}", request.FileName);

        var parser = new FlrFileImportFileContentParser();
        var parseResult = parser.Parse(request.FileReader);

        logger.LogInformation("FLR file header: {Header}", parseResult.HeaderInformation);
        logger.LogInformation("FLR file contains {NoOfResults} results", parseResult.EntityList.Count);

        AdjustParsedContents(parseResult.EntityList);
        await SendMessages(parseResult);
    }

    private async Task SendMessages(FlrFileImportFileContentParserResult p)
    {
        await SendBlankMessages(p);
        await SendAssayResultMessages(p);
    }

    private async Task SendBlankMessages(FlrFileImportFileContentParserResult p)
    {
        var blankGroups = p.EntityList
            .OfType<BlankResultFlrEntry>()
            .Select((entry, index) => new { Entry = entry, Index = index })
            .GroupBy(x => string.IsNullOrWhiteSpace(x.Entry.GroupId) ? $"__blank_{x.Index}" : x.Entry.GroupId)
            .Select(g => g.Select(x => x.Entry).ToList());

        foreach (var blankGroup in blankGroups)
        {
            await SendBlankResultMessage(p, blankGroup);
        }
    }

    private async Task SendAssayResultMessages(FlrFileImportFileContentParserResult p)
    {
        foreach (var assay in p.EntityList.OfType<AssayResultFlrEntry>())
        {
            await SendAssayResultMessage(p, assay);
        }
    }

    private async Task SendBlankResultMessage(FlrFileImportFileContentParserResult p, IReadOnlyCollection<BlankResultFlrEntry> entries)
    {
        var firstEntry = entries.First();

        var blankResult = new BlankResult
        {
            BlankType = MeasurementTypeToBlankType(firstEntry.MeasurementType, firstEntry.Technology),
            DevelopmentSolutionIdentity = new DevelopmentSolutionIdentity
            {
                ReagentBarcode = firstEntry.DevelopmentBarcode,
                CellPosition = firstEntry.DevelopmentWell,
            },
            LowerLimit = 0.0,
            UpperLimit = 0.0,
            ResultOk = true, // TODO: this field is needed, by the information is missing from the FLR-spec as of edit 
            StopSolutionBarcode = firstEntry.StopBarcode,
            MeanValue = entries.Average(x => x.MeasurementValue)
        };

        foreach (var entry in entries)
        {
            var replicate = new BlankReplicate
            {
                FluorometerResult = new FluorometerValue(),
                TimeOfMeasurement = entry.MeasurementDateAndTime,
            };
            blankResult.Replicates.Add(replicate);

            replicate.FluorometerResult.MeasurementValue = entry.MeasurementValue; // Milli-volt
        }

        var serialNumber = p.HeaderInformation.InstrumentSerial;

        await mediator.Send(new ProcessBlankResultCommand(serialNumber, blankResult, AllowRepeatedMessage: true));
    }


    private static EnumBlankType MeasurementTypeToBlankType(FlrMeasurementType entityMeasurementType, FlrTechnology? technology)
    {
        if (entityMeasurementType == FlrMeasurementType.RinseBlank)
        {
            return EnumBlankType.Rinse;
        }

        if (entityMeasurementType == FlrMeasurementType.ReagentBlank)
        {
            return technology == FlrTechnology.EliA ? EnumBlankType.ReagentElia : EnumBlankType.ReagentImmunoCap;
        }

        throw new InvalidDataException("FLR entity measurement type is not a handled blank type");
    }

    private async Task SendAssayResultMessage(FlrFileImportFileContentParserResult p, AssayResultFlrEntry entry)
    {
        // SampleDate is not included in the FLR file as of time of edit, so we set
        // it to the result measurement date (to avoid ).
        var sampleDate = entry.MeasurementDateAndTime.ToDateTimeOffset();
        sampleDate = new DateTimeOffset(sampleDate.Year, sampleDate.Month, sampleDate.Day, 0, 0, 0, TimeSpan.Zero);
        var sampleTimestamp = sampleDate.ToTimestamp();

        var assayResult = new AssayResult
        {
            TestType = entry.SampleType.ToEnumTestType(),
            Replicate = new ReplicateInfo
            {
                ReplicateNumber = entry.ReplicateNumber,
                ReplicatesInTestOrder = entry.ReplicatesInTestOrder,
                TestOrderNumber = entry.TestOrderNumber,
                TestOrdersInGroup = entry.TestOrdersInGroup,
                CmrTestId = entry.CmrTestId,
                GroupId = entry.GroupId,
                ReplicateId = entry.ReplicateId,
            },
            Sample = new SampleInformation
            {
                SampleId = entry.SampleId,
                SampleDate = sampleTimestamp,
                TubeId = entry.TubeId,
                TubeTypeIndex = entry.SampleTubeType,
                HighPriority = entry.SampleHighPriority,
                PreDilutionFactor = entry.PreDilution,
                IdentitySource = EnumIdentitySource.UnspecifiedIdentitySource // IdentitySource is not included in the FLR file as of time of edit
            },
            Run = new RunInfo
            {
                AnalyticalRunId = entry.AnalyticalRunId,
                AssayRunId = p.HeaderInformation.AssayRunId,
                CustomRunId = entry.CustomRunId,
            },
            MethodName = entry.MethodName,
            TestName = entry.TestName,
            InstrumentDilutionFactor = entry.InstrumentDilution,
            IrwRow = entry.IrwRow,
            IrwSpoke = entry.IrwSpoke,
            ErwRow = entry.ErwRow,
            ErwSpoke = entry.ErwSpoke,
            TimeOfMeasurement = entry.MeasurementDateAndTime,
            FluorometerResult = new FluorometerValue
            {
                MeasurementValue = entry.MeasurementValue
            },
            IsAborted = entry.IsAborted,
        };

        if (entry.SampleType != FlrSampleType.Sample && !string.IsNullOrWhiteSpace(entry.SampleBarcode))
        {
            var reagentType = entry.SampleType switch
            {
                FlrSampleType.QualityControl => EnumReagentType.QualityControlCartridge,
                FlrSampleType.CurveControl => EnumReagentType.CurveControlCartridge,
                FlrSampleType.Calibrator => EnumReagentType.CalibratorCartridge,
                _ => throw new InvalidDataException("Unknown sample type in the FLR file record.")
            };

            AddReagent(assayResult, reagentType, 0, false, entry.SampleBarcode);
        }

        AddReagent(assayResult, EnumReagentType.DevelopmentSolutionCartridge, entry.DevelopmentWell, entry.DevelopmentExpired, entry.DevelopmentBarcode);
        AddReagent(assayResult, EnumReagentType.ConjugateCartridge, entry.ConjugateWell, entry.ConjugateExpired, entry.ConjugateBarcode);
        AddReagent(assayResult, EnumReagentType.DiluentCartridge, entry.DiluentWell1, entry.DiluentExpired1, entry.DiluentBarcode1);
        AddReagent(assayResult, EnumReagentType.DiluentCartridge, entry.DiluentWell2, entry.DiluentExpired2, entry.DiluentBarcode2);
        AddReagent(assayResult, EnumReagentType.DiluentCartridge, entry.DiluentWell3, entry.DiluentExpired3, entry.DiluentBarcode3);
        AddReagent(assayResult, EnumReagentType.StopSolution, 0, entry.StopExpired, string.Empty, entry.StopBarcode);
        AddReagent(assayResult, EnumReagentType.WashConcentrate, 0, false, string.Empty, entry.WashConcentrationBarcode);
        AddReagent(assayResult, EnumReagentType.WashAdditive, 0, false, string.Empty, entry.WashAddBarcode);
        AddReagent(assayResult, EnumReagentType.DilutionPlate, 0, false, entry.DilutionPlateBarcode);

        if (!string.IsNullOrEmpty(entry.TestBarcode))
        {
            var article = await articlesService.GetArticleFromBarcode(entry.TestBarcode);
            if (article != null)
            {
                var reagentType = article.ArticleType.ToEnumReagentType();
                AddReagent(assayResult, reagentType, 0, entry.TestExpired, entry.TestBarcode, entry.TestUdi);
            }
        }

        var serialNumber = p.HeaderInformation.InstrumentSerial;
        await mediator.Send(new ProcessAssayResultCommand(serialNumber, assayResult, AllowRepeatedMessage: true));
    }

    private static void AddReagent(AssayResult assayResult, EnumReagentType reagentType, int? cellPosition, bool expired, string reagentBarcode, string reagentUdi = "")
    {
        if (!string.IsNullOrEmpty(reagentBarcode))
        {
            assayResult.Reagents.Add(new ReagentInformation
            {
                ReagentType = reagentType,
                CellPosition = cellPosition ?? 1,
                ExpiredReagent = expired,
                ReagentBarcode = reagentBarcode,
                ReagentUdiBarcode = reagentUdi
            });
        }
    }

    private static void AdjustParsedContents(List<FlrEntry> entityList)
    {
        AdjustAnalyticalRunId(entityList);
        AdjustReplicateInfo(entityList);
    }

    private static void AdjustAnalyticalRunId(List<FlrEntry> entityList)
    {
        var arDict = new Dictionary<string, string>();

        foreach (var entry in entityList)
        {
            if (entry is AssayResultFlrEntry assayEntry && string.IsNullOrEmpty(assayEntry.AnalyticalRunId))
            {
                if (!arDict.TryGetValue(assayEntry.MethodName, out var arId))
                {
                    arId = Guid.NewGuid().ToString();
                    arDict.Add(assayEntry.MethodName, arId);
                }
                assayEntry.AnalyticalRunId = arId;

            }
        }
    }

    private static void AdjustReplicateInfo(List<FlrEntry> entryList)
    {
        var assayEntries = entryList.Where(r => r is AssayResultFlrEntry).Cast<AssayResultFlrEntry>().ToList();

        // Count replicates for every sample in every group, and set ReplicateNumber and ReplicatesInTestOrder to each entry.
        var groupedEntries = assayEntries
            .GroupBy(x => new { x.GroupId, x.SampleId })
            .Select(g => new
            {
                Count = g.Count(),
                Entries = g.ToList()
            });

        foreach (var item in groupedEntries)
        {
            if (item.Count == 1)
            {
                continue;
            }

            for (var i = 0; i < item.Entries.Count; i++)
            {
                item.Entries[i].ReplicateNumber = i + 1;
                item.Entries[i].ReplicatesInTestOrder = item.Count;
            }
        }

        // For calibrators and curve controls, we need to set TestOrderNumber and TestOrdersInGroup
        var groupedCalibratorAndCurveControlEntries = assayEntries
            .Where(x => x.SampleType is FlrSampleType.Calibrator or FlrSampleType.CurveControl)
            .GroupBy(x => x.GroupId);

        foreach (var group in groupedCalibratorAndCurveControlEntries)
        {
            var testOrderBySampleId = group
                .GroupBy(x => x.SampleId)
                .Select((g, index) => new
                {
                    SampleId = g.Key,
                    TestOrderNumber = index + 1
                })
                .ToDictionary(x => x.SampleId, x => x.TestOrderNumber);

            var testOrdersInGroup = testOrderBySampleId.Count;

            foreach (var entry in group)
            {
                entry.TestOrderNumber = testOrderBySampleId[entry.SampleId];
                entry.TestOrdersInGroup = testOrdersInGroup;
            }
        }
    }
}
