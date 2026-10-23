using FluentAssertions;
using Pharos.Server.Application.Lis;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Lis.HL7.Application.Mappings;
using Pharos.Server.Lis.HL7.Application.Messages;
using Xunit;

namespace Pharos.Server.Lis.HL7.Tests.Application.Mappings;

public class HL7CommentSegmentFactoryTests
{
    [Fact]
    public void CreateFromSampleTestResult_WhenCommentSettingsAreEnabled_ShouldCreateConfiguredComments()
    {
        var testResult = new LisSampleTestResult
        {
            ApprovedByUser = "operator1",
            Ru = "45,6",
            Reagents =
            [
                new LisReagent { ReagentType = ArticleType.ImmunoCapCarrier, LotNumber = "LOT-1" }
            ]
        };
        var exportSettings = new HL7ExportSettings
        {
            IncludeUserIdInCommentRecord = true,
            IncludeLotNumbersInCommentRecord = true,
            IncludeRawDataInCommentRecord = true
        };

        var comments = HL7CommentSegmentFactory.CreateFromSampleTestResult(testResult, exportSettings);

        comments.Should().HaveCount(3);
        comments[0].SequenceNumber.Should().Be(1);
        comments[0].CommentSource.Should().Be(HL7CommentSource.Prime);
        comments[0].CommentType.Should().Be(HL7CommentType.Automatic);
        comments[0].Comment.Should().Be("Operator id operator1");
        comments[1].Comment.Should().Be("ImmunoCAP LOT-1");
        comments[2].Comment.Should().Be("Response value in RU 45.6");
    }

    [Fact]
    public void CreateFromSampleTestResult_WhenLotNumberSettingIsEnabled_ShouldCreateLotCommentsForApplicableReagentsIncludingEmptyLots()
    {
        var testResult = new LisSampleTestResult
        {
            Reagents =
            [
                new LisReagent { ReagentType = ArticleType.ImmunoCapCarrier, LotNumber = "LOT-1" },
                new LisReagent { ReagentType = ArticleType.ConjugateCartridge, LotNumber = "" },
                new LisReagent { ReagentType = ArticleType.DevelopmentSolutionCartridge, LotNumber = null },
                new LisReagent { ReagentType = ArticleType.None, LotNumber = "IGNORED" }
            ]
        };
        var exportSettings = new HL7ExportSettings
        {
            IncludeLotNumbersInCommentRecord = true
        };

        var comments = HL7CommentSegmentFactory.CreateFromSampleTestResult(testResult, exportSettings);

        comments.Select(x => (x.SequenceNumber, x.CommentSource, x.CommentType, x.Comment)).Should().Equal(
            (1, HL7CommentSource.Prime, HL7CommentType.Automatic, "ImmunoCAP LOT-1"),
            (2, HL7CommentSource.Prime, HL7CommentType.Automatic, "Conjugate "),
            (3, HL7CommentSource.Prime, HL7CommentType.Automatic, "Development solution "));
    }

    [Fact]
    public void CreateFromQualityControlTestResult_WhenLotNumberSettingIsEnabled_ShouldCreateMappedLotComments()
    {
        var testResult = new LisQualityControlTestResult
        {
            Reagents =
            [
                new LisReagent { ReagentType = ArticleType.QualityControlCartridge, LotNumber = "QC-LOT-1" }
            ]
        };
        var exportSettings = new HL7ExportSettings
        {
            IncludeLotNumbersInCommentRecord = true
        };

        var comments = HL7CommentSegmentFactory.CreateFromQualityControlTestResult(testResult, exportSettings);

        comments.Should().ContainSingle();
        comments[0].SequenceNumber.Should().Be(1);
        comments[0].CommentSource.Should().Be(HL7CommentSource.Prime);
        comments[0].CommentType.Should().Be(HL7CommentType.Automatic);
        comments[0].Comment.Should().Be("Quality control QC-LOT-1");
    }

    [Theory]
    [InlineData(false, "45,6", 0, null)]
    [InlineData(true, null, 0, null)]
    [InlineData(true, "", 0, null)]
    [InlineData(true, " ", 0, null)]
    [InlineData(true, "45,6", 1, "Response value in RU 45.6")]
    public void CreateFromSampleTestResult_ShouldIncludeRawDataComment_WhenSettingIsEnabledAndRuIsAvailable(
        bool includeRawDataInCommentRecord,
        string? ru,
        int expectedCount,
        string? expectedComment)
    {
        var testResult = new LisSampleTestResult
        {
            Ru = ru
        };
        var exportSettings = new HL7ExportSettings
        {
            IncludeRawDataInCommentRecord = includeRawDataInCommentRecord
        };

        var comments = HL7CommentSegmentFactory.CreateFromSampleTestResult(testResult, exportSettings);

        comments.Should().HaveCount(expectedCount);

        if (expectedCount > 0)
        {
            comments[0].SequenceNumber.Should().Be(1);
            comments[0].CommentSource.Should().Be(HL7CommentSource.Prime);
            comments[0].CommentType.Should().Be(HL7CommentType.Automatic);
            comments[0].Comment.Should().Be(expectedComment);
        }
    }

    [Trait("ProductRequirement", "PR_1310")]
    [Theory]
    [InlineData(true, "operator1", 1, "Operator id operator1")]
    [InlineData(true, null, 0, null)]
    [InlineData(true, "", 0, null)]
    [InlineData(true, " ", 0, null)]
    [InlineData(false, "operator1", 0, null)]
    public void CreateFromSampleTestResult_ShouldIncludeUserIdComment_WhenSettingIsEnabledAndUserIsAvailable(
        bool includeUserIdInCommentRecord,
        string? approvedByUser,
        int expectedCount,
        string? expectedComment)
    {
        var testResult = new LisSampleTestResult
        {
            ApprovedByUser = approvedByUser
        };
        var exportSettings = new HL7ExportSettings
        {
            IncludeUserIdInCommentRecord = includeUserIdInCommentRecord
        };

        var comments = HL7CommentSegmentFactory.CreateFromSampleTestResult(testResult, exportSettings);

        comments.Should().HaveCount(expectedCount);

        if (expectedCount > 0)
        {
            comments[0].SequenceNumber.Should().Be(1);
            comments[0].CommentSource.Should().Be(HL7CommentSource.Prime);
            comments[0].CommentType.Should().Be(HL7CommentType.Automatic);
            comments[0].Comment.Should().Be(expectedComment);
        }
    }

    [Trait("ProductRequirement", "PR_1310")]
    [Fact]
    public void CreateFromSampleTestResult_WithRejectedResult_ShouldIncludeRejectingUserIdComment_WhenSettingIsEnabled()
    {
        var testResult = new LisSampleTestResult
        {
            ApprovedByUser = "rejector1",
            HasError = true
        };
        var exportSettings = new HL7ExportSettings
        {
            IncludeUserIdInCommentRecord = true
        };

        var comments = HL7CommentSegmentFactory.CreateFromSampleTestResult(testResult, exportSettings);

        comments.Should().ContainSingle();
        comments[0].SequenceNumber.Should().Be(1);
        comments[0].CommentSource.Should().Be(HL7CommentSource.Prime);
        comments[0].CommentType.Should().Be(HL7CommentType.Automatic);
        comments[0].Comment.Should().Be("Operator id rejector1");
    }

    [Fact]
    public void CreateFromSampleTestResult_WhenCommentSettingsAreDisabled_ShouldNotCreateComments()
    {
        var testResult = new LisSampleTestResult
        {
            ApprovedByUser = "operator1",
            Ru = "45.6",
            Reagents =
            [
                new LisReagent { ReagentType = ArticleType.ImmunoCapCarrier, LotNumber = "LOT-1" }
            ]
        };

        var comments = HL7CommentSegmentFactory.CreateFromSampleTestResult(testResult, new HL7ExportSettings());

        comments.Should().BeEmpty();
    }

    [Trait("ProductRequirement", "PR_1785")]
    [Fact]
    public void CreateFromSampleTestResult_WhenCommentSettingsAreEnabled_ShouldCreateConfiguredCommentsInSequence()
    {
        var testResult = new LisSampleTestResult
        {
            ApprovedByUser = "operator1",
            Ru = "45.6",
            Reagents =
            [
                new LisReagent { ReagentType = ArticleType.ImmunoCapCarrier, LotNumber = "" },
                new LisReagent { ReagentType = ArticleType.ConjugateCartridge, LotNumber = "LOT-2" }
            ]
        };
        var exportSettings = new HL7ExportSettings
        {
            IncludeUserIdInCommentRecord = true,
            IncludeLotNumbersInCommentRecord = true,
            IncludeRawDataInCommentRecord = true
        };

        var comments = HL7CommentSegmentFactory.CreateFromSampleTestResult(testResult, exportSettings);

        comments.Should().HaveCount(4);
        comments[0].SequenceNumber.Should().Be(1);
        comments[0].Comment.Should().Be("Operator id operator1");
        comments[1].SequenceNumber.Should().Be(2);
        comments[1].Comment.Should().Be("ImmunoCAP ");
        comments[2].SequenceNumber.Should().Be(3);
        comments[2].Comment.Should().Be("Conjugate LOT-2");
        comments[3].SequenceNumber.Should().Be(4);
        comments[3].Comment.Should().Be("Response value in RU 45.6");
    }

    [Trait("ProductRequirement", "PR_1785")]
    [Fact]
    public void CreateFromSampleTestResult_WithMultipleLotNumbers_ShouldCreateOneAutomaticCommentPerLotNumber()
    {
        var testResult = new LisSampleTestResult
        {
            Reagents =
            [
                new LisReagent { ReagentType = ArticleType.ImmunoCapCarrier, LotNumber = "LOT-1" },
                new LisReagent { ReagentType = ArticleType.ConjugateCartridge, LotNumber = "LOT-2" }
            ]
        };
        var exportSettings = new HL7ExportSettings
        {
            IncludeLotNumbersInCommentRecord = true
        };

        var comments = HL7CommentSegmentFactory.CreateFromSampleTestResult(testResult, exportSettings);

        comments.Select(comment => comment.SequenceNumber).Should().Equal(1, 2);
        comments.Select(comment => comment.CommentType).Should().OnlyContain(commentType => commentType == HL7CommentType.Automatic);
        comments.Select(comment => comment.Comment).Should().Equal("ImmunoCAP LOT-1", "Conjugate LOT-2");
    }

    [Trait("ProductRequirement", "PR_1785")]
    [Fact]
    public void CreateFromQualityControlTestResult_WhenCommentSettingsAreEnabled_ShouldCreateLotAndRawDataComments()
    {
        var testResult = new LisQualityControlTestResult
        {
            Ru = "45,6",
            Reagents =
            [
                new LisReagent { ReagentType = ArticleType.QualityControlCartridge, LotNumber = "QC-LOT-1" }
            ]
        };
        var exportSettings = new HL7ExportSettings
        {
            IncludeUserIdInCommentRecord = true,
            IncludeLotNumbersInCommentRecord = true,
            IncludeRawDataInCommentRecord = true
        };

        var comments = HL7CommentSegmentFactory.CreateFromQualityControlTestResult(testResult, exportSettings);

        comments.Should().HaveCount(2);
        comments[0].SequenceNumber.Should().Be(1);
        comments[0].Comment.Should().Be("Quality control QC-LOT-1");
        comments[0].CommentType.Should().Be(HL7CommentType.Automatic);
        comments[1].SequenceNumber.Should().Be(2);
        comments[1].Comment.Should().Be("Response value in RU 45.6");
        comments[1].CommentType.Should().Be(HL7CommentType.Automatic);
    }

    [Fact]
    public void CreateFromPatient_WithMultilineComment_ShouldCreatePatientComments()
    {
        var patient = new LisPatient
        {
            Comment = "first\r\nsecond"
        };

        var comments = HL7CommentSegmentFactory.CreateFromPatient(patient, allowExtraLongPatientComments: false);

        comments.Should().HaveCount(2);
        comments[0].SequenceNumber.Should().Be(1);
        comments[0].CommentType.Should().Be(HL7CommentType.Generic);
        comments[0].Comment.Should().Be("first");
        comments[1].SequenceNumber.Should().Be(2);
        comments[1].Comment.Should().Be("second");
    }

    [Trait("ProductRequirement", "PR_576")]
    [Fact]
    public void CreateFromPatient_WhenExtraLongPatientCommentsAreDisabled_ShouldTruncateToDefaultCommentLength()
    {
        var patient = new LisPatient
        {
            Comment = new string('A', 300)
        };

        var comments = HL7CommentSegmentFactory.CreateFromPatient(patient, allowExtraLongPatientComments: false);

        comments.Should().ContainSingle();
        comments[0].Comment.Should().Be(new string('A', 255));
    }

    [Trait("ProductRequirement", "PR_576")]
    [Fact]
    public void CreateFromPatient_WhenExtraLongPatientCommentsAreEnabled_ShouldAllowUpToOneThousandCharacters()
    {
        var patient = new LisPatient
        {
            Comment = new string('B', 1000)
        };

        var comments = HL7CommentSegmentFactory.CreateFromPatient(patient, allowExtraLongPatientComments: true);

        comments.Should().ContainSingle();
        comments[0].Comment.Should().Be(new string('B', 1000));
    }
}
