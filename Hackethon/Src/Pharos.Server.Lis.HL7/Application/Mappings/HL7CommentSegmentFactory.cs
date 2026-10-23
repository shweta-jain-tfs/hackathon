using Pharos.Server.Application.Lis;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Lis.HL7.Application.Messages;

namespace Pharos.Server.Lis.HL7.Application.Mappings;

internal static class HL7CommentSegmentFactory
{
    public static IReadOnlyList<HL7CommentSegment> CreateFromPatient(LisPatient patient, bool allowExtraLongPatientComments)
    {
        if (string.IsNullOrWhiteSpace(patient.Comment))
        {
            return [];
        }

        var sequenceNumber = 1;
        return SplitComment(patient.Comment, allowExtraLongPatientComments)
            .Select(comment => CreateCommentSegment(sequenceNumber++, comment, HL7CommentType.Generic))
            .ToList();
    }

    public static IReadOnlyList<HL7CommentSegment> CreateFromSampleTestResult(LisSampleTestResult result, HL7ExportSettings exportSettings)
    {
        return CreateFromTestResult(result.ApprovedByUser, result.Reagents, result.Ru, exportSettings);
    }

    public static IReadOnlyList<HL7CommentSegment> CreateFromQualityControlTestResult(LisQualityControlTestResult result, HL7ExportSettings exportSettings)
    {
        return CreateFromTestResult(null, result.Reagents, result.Ru, exportSettings);
    }

    private static List<HL7CommentSegment> CreateFromTestResult(
        string? approvedByUser,
        IReadOnlyCollection<LisReagent> reagents,
        string? responseValue,
        HL7ExportSettings exportSettings)
    {
        var comments = new List<HL7CommentSegment>();
        var sequenceNumber = 1;

        if (exportSettings.IncludeUserIdInCommentRecord && !string.IsNullOrWhiteSpace(approvedByUser))
        {
            comments.Add(CreateCommentSegment(sequenceNumber++, $"Operator id {approvedByUser}", HL7CommentType.Automatic));
        }

        if (exportSettings.IncludeLotNumbersInCommentRecord)
        {
            foreach (var reagent in reagents)
            {
                var reagentTypeName = MapReagentType(reagent.ReagentType);

                if (string.IsNullOrEmpty(reagentTypeName))
                {
                    continue;
                }

                comments.Add(CreateCommentSegment(sequenceNumber++, $"{reagentTypeName} {reagent.LotNumber ?? ""}", HL7CommentType.Automatic));
            }
        }

        if (exportSettings.IncludeRawDataInCommentRecord && !string.IsNullOrWhiteSpace(responseValue))
        {
            comments.Add(CreateCommentSegment(sequenceNumber, $"Response value in RU {NormalizeDecimalSeparator(responseValue)}", HL7CommentType.Automatic));
        }

        return comments;
    }

    private static HL7CommentSegment CreateCommentSegment(int sequenceNumber, string comment, HL7CommentType commentType)
    {
        return new HL7CommentSegment
        {
            SequenceNumber = sequenceNumber,
            CommentSource = HL7CommentSource.Prime,
            Comment = comment,
            CommentType = commentType
        };
    }

    private static string[] SplitComment(string comment, bool allowExtraLongPatientComments)
    {
        var maxLength = allowExtraLongPatientComments ? 1000 : 255;
        if (comment.Length > maxLength)
        {
            comment = comment[..maxLength];
        }

        return comment
            .Split(["\r\n", "\n"], StringSplitOptions.None);
    }

    private static string NormalizeDecimalSeparator(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "" : value.Replace(",", ".");
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
