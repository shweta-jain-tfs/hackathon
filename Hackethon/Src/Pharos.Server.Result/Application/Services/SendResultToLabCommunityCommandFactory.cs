using Pharos.Server.Application.LabCommunity;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Result.Application.Mapping;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Services;

internal static class SendResultToLabCommunityCommandFactory
{
    internal static SendBlankResultToLabCommunityCommand Create(Blank source)
    {
        var replicates = source.BlankReplicates?
            .Select((replicate, index) => new BlankResultReplicate(
                index + 1, replicate.MeasurementTime, replicate.FluorometerValue))
            .ToList() ?? [];

        return new SendBlankResultToLabCommunityCommand(
            source.Id, source.InstrumentNumber, source.MeasurementTime, source.BlankType.ToBlankResultType(), source.ResultOk,
            source.DevSolutionBarcode, source.StopSolutionBarcode, replicates);
    }

    internal static SendQualityControlResultToLabCommunityCommand Create(
        QualityControlTestResult source,
        double rangeMin,
        double rangeMax,
        double percentCvLimit)
    {
        var judgment = source.Judgment.ToQualityControlResultJudgment();
        var replicates = source.Replicates?
            .OfType<QualityControlTestReplicate>()
            .Select(replicate => new QualityControlResultReplicate(
                replicate.ReplicateNumber, replicate.MeasurementDateTime, replicate.Concentration, replicate.Response, judgment,
                GetLotNumber(replicate, ArticleType.ConjugateCartridge),
                GetSolidPhaseLotNumber(replicate),
                GetLotNumber(replicate, ArticleType.QualityControlCartridge)))
            .ToList() ?? [];

        var firstReplicate = replicates.FirstOrDefault(replicate => replicate.ReplicateNumber == 1)
                             ?? replicates.FirstOrDefault();

        return new SendQualityControlResultToLabCommunityCommand(
            source.GroupId, source.InstrumentNumber, source.MeasurementDateTime, source.QualityControlName,
            source.MethodName, source.TestName, judgment,
            firstReplicate?.ConjugateLotNumber, firstReplicate?.SolidPhaseLotNumber, firstReplicate?.QualityControlLotNumber,
            rangeMin, rangeMax, percentCvLimit, replicates);
    }

    internal static SendCurveControlResultToLabCommunityCommand Create(CurveControlTestResult source)
    {
        var replicates = source.Replicates?
            .OfType<CurveControlTestReplicate>()
            .Select(replicate => new CurveControlResultReplicate(
                replicate.ReplicateNumber, replicate.MeasurementDateTime, replicate.Concentration, replicate.Response,
                replicate.Judgment.ToCurveControlResultJudgment(),
                GetLotNumber(replicate, ArticleType.ConjugateCartridge),
                GetSolidPhaseLotNumber(replicate),
                GetLotNumber(replicate, ArticleType.CurveControlCartridge)))
            .ToList() ?? [];

        var firstReplicate = replicates.FirstOrDefault(replicate => replicate.ReplicateNumber == 1)
                             ?? replicates.FirstOrDefault();

        return new SendCurveControlResultToLabCommunityCommand(
            source.GroupId, source.InstrumentNumber, source.MeasurementDateTime, source.CurveControlPointName,
            source.MethodName, source.TestName, source.JudgmentSummary.ToCurveControlResultJudgment(), source.ExpectedConcentration,
            firstReplicate?.ConjugateLotNumber, firstReplicate?.SolidPhaseLotNumber, firstReplicate?.CurveControlLotNumber, replicates);
    }

    private static string? GetSolidPhaseLotNumber(TestReplicate replicate)
    {
        return GetLotNumber(replicate, ArticleType.ImmunoCapCarrier, ArticleType.EliaCalibratorWellCarrier, ArticleType.EliaWellCarrier);
    }

    private static string? GetLotNumber(TestReplicate replicate, params ArticleType[] articleTypes)
    {
        return replicate.TestReplicateReagents?
            .FirstOrDefault(reagent => articleTypes.Contains(reagent.ReagentType))?
            .LotNumber;
    }
}
