using Pharos.Server.Application.Lis;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Lis.Astm.Application.Messages;

namespace Pharos.Server.Lis.Astm.Application.Mappings;

internal static class AstmCommentRecordFactory
{
    public static List<AstmCommentRecord> CreateFromSampleTestResult(LisSampleTestResult sampleTestResult, AstmExportSettings exportSettings)
    {
        return CreateCommentRecords(sampleTestResult.ApprovedByUser, sampleTestResult.Reagents, sampleTestResult.Ru, exportSettings);
    }

    public static List<AstmCommentRecord> CreateFromQualityControlResult(LisQualityControlTestResult qcTestResult, AstmExportSettings exportSettings)
    {
        return CreateCommentRecords(null, qcTestResult.Reagents, qcTestResult.Ru, exportSettings);
    }

    private static List<AstmCommentRecord> CreateCommentRecords(string? approvedByUser, List<LisReagent> reagents, string? responseValue, AstmExportSettings exportSettings)
    {
        var commentRecords = new List<AstmCommentRecord>();
        var sequenceNumber = 1;

        AddOperatorIdCommentRecord(commentRecords, ref sequenceNumber, approvedByUser, exportSettings);
        AddLotNumberCommentRecords(commentRecords, ref sequenceNumber, reagents, exportSettings);
        AddRawDataCommentRecord(commentRecords, ref sequenceNumber, responseValue, exportSettings);

        return commentRecords;
    }

    private static void AddOperatorIdCommentRecord(List<AstmCommentRecord> records, ref int sequenceNumber, string? approvedByUser, AstmExportSettings exportSettings)
    {
        if (!exportSettings.IncludeUserIdInCommentRecord || approvedByUser == null)
        {
            return;
        }

        records.Add(new AstmCommentRecord
        {
            SequenceNumber = sequenceNumber++,
            Source = AstmCommentSource.Prime,
            Type = AstmCommentType.Automatic,
            Text = $"Operator id {approvedByUser ?? ""}"
        });
    }

    private static void AddLotNumberCommentRecords(List<AstmCommentRecord> records, ref int sequenceNumber, List<LisReagent> reagents, AstmExportSettings exportSettings)
    {
        if (exportSettings.IncludeLotNumbersInCommentRecord == IncludeLotNumbersInCommentRecord.NotUsed)
        {
            return;
        }

        foreach (var reagent in reagents)
        {
            if (reagent.ReagentType == ArticleType.None)
            {
                continue;
            }

            if (exportSettings.IncludeLotNumbersInCommentRecord == IncludeLotNumbersInCommentRecord.UsedExcludingEmptyLotNumbers
                && string.IsNullOrEmpty(reagent.LotNumber))
            {
                continue;
            }

            var reagentTypeName = MapReagentType(reagent.ReagentType);

            records.Add(new AstmCommentRecord
            {
                SequenceNumber = sequenceNumber++,
                Source = AstmCommentSource.Prime,
                Type = AstmCommentType.Automatic,
                Text = $"{reagentTypeName} {reagent.LotNumber ?? ""}"
            });
        }
    }

    private static void AddRawDataCommentRecord(List<AstmCommentRecord> records, ref int sequenceNumber, string? responseValue, AstmExportSettings exportSettings)
    {
        if (!exportSettings.IncludeRawDataInCommentRecord)
        {
            return;
        }

        records.Add(new AstmCommentRecord
        {
            SequenceNumber = sequenceNumber++,
            Source = AstmCommentSource.Prime,
            Type = AstmCommentType.Automatic,
            Text = $"Response value in RU {responseValue ?? ""}"
        });
    }

    private static string MapReagentType(ArticleType articleType)
    {
        return articleType switch
        {
            ArticleType.ImmunoCapCarrier => "ImmunoCAP",
            ArticleType.ConjugateCartridge => "Conjugate",
            ArticleType.CalibratorCartridge => "Calibrator",
            ArticleType.CurveControlCartridge => "Curve control",
            ArticleType.QualityControlCartridge => "Quality control",
            ArticleType.DevelopmentSolutionCartridge => "Development solution",
            ArticleType.StopSolutionBottle => "Stop solution",
            ArticleType.FluoroCVial => "FluoroC",
            ArticleType.DiluentCartridge => "Diluent",
            ArticleType.EliaWellCarrier => "EliA well",
            ArticleType.EliaCalibratorWellCarrier => "EliA calibrator well",
            ArticleType.DilutionWellCarrier => "Dilution well",
            ArticleType.WashConcentrateBottle => "Wash concentrate",
            ArticleType.WashAdditiveBottle => "Wash additive",
            _ => string.Empty
        };
    }
}
