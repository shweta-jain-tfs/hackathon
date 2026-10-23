using Pharos.Server.Application.Lis;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Lis.Astm.Application.Mappings;
using Pharos.Server.Lis.Astm.Application.Messages;

namespace Pharos.Server.Lis.Astm.Tests.Application.Mappings;

public class AstmCommentRecordFactoryTests
{
    [Trait("ProductRequirement", "PR_1305")]
    [Trait("ModuleRequirement", "MR_5623")]
    [Fact]
    public void CreateFromSampleResult_ShouldIncludeAllImmunoCapReagents_WhenIncludeLotNumbersIsUsedIncludingEmpty()
    {
        // Arrange
        var testResult = new LisSampleTestResult
        {
            Reagents = new List<LisReagent>
            {
                new() { ReagentType = ArticleType.ImmunoCapCarrier, LotNumber = "LOT001" },
                new() { ReagentType = ArticleType.WashConcentrateBottle, LotNumber = "LOT002" },
                new() { ReagentType = ArticleType.WashAdditiveBottle, LotNumber = "" },
                new() { ReagentType = ArticleType.ConjugateCartridge, LotNumber = "LOT004" },
                new() { ReagentType = ArticleType.DevelopmentSolutionCartridge, LotNumber = "LOT005" },
                new() { ReagentType = ArticleType.StopSolutionBottle, LotNumber = "LOT006" },
                new() { ReagentType = ArticleType.FluoroCVial, LotNumber = "" },
                new() { ReagentType = ArticleType.CalibratorCartridge, LotNumber = "LOT008" }
            }
        };
        var exportSettings = new AstmExportSettings
        {
            IncludeLotNumbersInCommentRecord = IncludeLotNumbersInCommentRecord.UsedIncludingEmptyLotNumbers
        };

        // Act
        var commentRecords = AstmCommentRecordFactory.CreateFromSampleTestResult(testResult, exportSettings);

        // Assert
        commentRecords.Should().HaveCount(8);
        commentRecords[0].Text.Should().Be("ImmunoCAP LOT001");
        commentRecords[1].Text.Should().Be("Wash concentrate LOT002");
        commentRecords[2].Text.Should().Be("Wash additive ");
        commentRecords[3].Text.Should().Be("Conjugate LOT004");
        commentRecords[4].Text.Should().Be("Development solution LOT005");
        commentRecords[5].Text.Should().Be("Stop solution LOT006");
        commentRecords[6].Text.Should().Be("FluoroC ");
        commentRecords[7].Text.Should().Be("Calibrator LOT008");
    }

    [Trait("ProductRequirement", "PR_1310")]
    [Trait("ModuleRequirement", "MR_5694")]
    [Fact]
    public void CreateFromSampleResult_ShouldIncludeAllEliaReagents_WhenIncludeLotNumbersIsUsedIncludingEmpty()
    {
        // Arrange
        var testResult = new LisSampleTestResult
        {
            Reagents = new List<LisReagent>
            {
                new() { ReagentType = ArticleType.EliaWellCarrier, LotNumber = "LOT101" },
                new() { ReagentType = ArticleType.WashConcentrateBottle, LotNumber = "LOT102" },
                new() { ReagentType = ArticleType.WashAdditiveBottle, LotNumber = "LOT103" },
                new() { ReagentType = ArticleType.ConjugateCartridge, LotNumber = "" },
                new() { ReagentType = ArticleType.DiluentCartridge, LotNumber = "LOT105" },
                new() { ReagentType = ArticleType.DilutionWellCarrier, LotNumber = "LOT106" },
                new() { ReagentType = ArticleType.DevelopmentSolutionCartridge, LotNumber = "LOT107" },
                new() { ReagentType = ArticleType.StopSolutionBottle, LotNumber = "" },
                new() { ReagentType = ArticleType.FluoroCVial, LotNumber = "LOT109" },
                new() { ReagentType = ArticleType.EliaCalibratorWellCarrier, LotNumber = "LOT110" }
            }
        };
        var exportSettings = new AstmExportSettings
        {
            IncludeLotNumbersInCommentRecord = IncludeLotNumbersInCommentRecord.UsedIncludingEmptyLotNumbers
        };

        // Act
        var commentRecords = AstmCommentRecordFactory.CreateFromSampleTestResult(testResult, exportSettings);

        // Assert
        commentRecords.Should().HaveCount(10);
        commentRecords[0].Text.Should().Be("EliA well LOT101");
        commentRecords[1].Text.Should().Be("Wash concentrate LOT102");
        commentRecords[2].Text.Should().Be("Wash additive LOT103");
        commentRecords[3].Text.Should().Be("Conjugate ");
        commentRecords[4].Text.Should().Be("Diluent LOT105");
        commentRecords[5].Text.Should().Be("Dilution well LOT106");
        commentRecords[6].Text.Should().Be("Development solution LOT107");
        commentRecords[7].Text.Should().Be("Stop solution ");
        commentRecords[8].Text.Should().Be("FluoroC LOT109");
        commentRecords[9].Text.Should().Be("EliA calibrator well LOT110");
    }

    [Trait("ProductRequirement", "PR_1310")]
    [Trait("ModuleRequirement", "MR_5694")]
    [Fact]
    public void CreateFromSampleResult_ShouldExcludeEmptyLotNumbers_WhenIncludeLotNumbersIsUsedExcludingEmpty()
    {
        // Arrange
        var testResult = new LisSampleTestResult
        {
            Reagents = new List<LisReagent>
            {
                new() { ReagentType = ArticleType.ImmunoCapCarrier, LotNumber = "LOT123" },
                new() { ReagentType = ArticleType.ConjugateCartridge, LotNumber = "" },
                new() { ReagentType = ArticleType.CalibratorCartridge, LotNumber = null },
                new() { ReagentType = ArticleType.CurveControlCartridge, LotNumber = "LOT456" }
            }
        };
        var exportSettings = new AstmExportSettings
        {
            IncludeLotNumbersInCommentRecord = IncludeLotNumbersInCommentRecord.UsedExcludingEmptyLotNumbers
        };

        // Act
        var commentRecords = AstmCommentRecordFactory.CreateFromSampleTestResult(testResult, exportSettings);

        // Assert
        commentRecords.Should().HaveCount(2);
        commentRecords[0].Text.Should().Be("ImmunoCAP LOT123");
        commentRecords[1].Text.Should().Be("Curve control LOT456");
    }

    [Trait("ProductRequirement", "PR_1310")]
    [Trait("ModuleRequirement", "MR_5694")]
    [Fact]
    public void CreateFromSampleResult_ShouldNotIncludeReagents_WhenIncludeLotNumbersIsNotUsed()
    {
        // Arrange
        var testResult = new LisSampleTestResult
        {
            Reagents = new List<LisReagent>
            {
                new() { ReagentType = ArticleType.ImmunoCapCarrier, LotNumber = "LOT123" }
            }
        };
        var exportSettings = new AstmExportSettings
        {
            IncludeLotNumbersInCommentRecord = IncludeLotNumbersInCommentRecord.NotUsed
        };

        // Act
        var commentRecords = AstmCommentRecordFactory.CreateFromSampleTestResult(testResult, exportSettings);

        // Assert
        commentRecords.Should().BeEmpty();
    }

    [Trait("ProductRequirement", "PR_1305")]
    [Trait("ModuleRequirement", "MR_5624")]
    [Theory]
    [InlineData(true, "12.5", 1, "Response value in RU 12.5")]
    [InlineData(true, null, 1, "Response value in RU ")]
    [InlineData(false, "12.5", 0, null)]
    public void CreateFromSampleResult_ShouldIncludeRUComment_WhenIncludeRawDataIsTrue(
         bool includeRawData,
        string? ruValue,
        int expectedCount,
        string? expectedText)
    {
        // Arrange
        var testResult = new LisSampleTestResult
        {
            Ru = ruValue
        };
        var exportSettings = new AstmExportSettings
        {
            IncludeRawDataInCommentRecord = includeRawData,
            IncludeLotNumbersInCommentRecord = IncludeLotNumbersInCommentRecord.NotUsed
        };

        // Act
        var commentRecords = AstmCommentRecordFactory.CreateFromSampleTestResult(testResult, exportSettings);

        // Assert
        commentRecords.Should().HaveCount(expectedCount);

        if (includeRawData)
        {
            commentRecords[0].SequenceNumber.Should().Be(1);
            commentRecords[0].Source.Should().Be(AstmCommentSource.Prime);
            commentRecords[0].Type.Should().Be(AstmCommentType.Automatic);
            commentRecords[0].Text.Should().Be(expectedText);
        }
    }

    [Trait("ProductRequirement", "PR_1305")]
    [Trait("ModuleRequirement", "MR_5621")]
    [Theory]
    [InlineData(true, "JohnDoe", 1, "Operator id JohnDoe")]
    [InlineData(true, null, 0, null)]
    [InlineData(false, "JohnDoe", 0, null)]
    public void CreateFromSampleResult_ShouldIncludeUserIdComment_WhenSettingIsEnabled(
       bool includeUserIdInCommentRecord,
       string? userId,
       int expectedCount,
       string? expectedText)
    {
        // Arrange
        var testResult = new LisSampleTestResult
        {
            ApprovedByUser = userId
        };
        var exportSettings = new AstmExportSettings
        {
            IncludeUserIdInCommentRecord = includeUserIdInCommentRecord,
        };

        // Act
        var commentRecords = AstmCommentRecordFactory.CreateFromSampleTestResult(testResult, exportSettings);

        // Assert
        commentRecords.Should().HaveCount(expectedCount);

        if (includeUserIdInCommentRecord && expectedCount > 0)
        {
            commentRecords[0].SequenceNumber.Should().Be(1);
            commentRecords[0].Source.Should().Be(AstmCommentSource.Prime);
            commentRecords[0].Type.Should().Be(AstmCommentType.Automatic);
            commentRecords[0].Text.Should().Be(expectedText);
        }
    }

    [Trait("ProductRequirement", "PR_1310")]
    [Trait("ModuleRequirement", "MR_5694")]
    [Fact]
    public void CreateFromSampleResult_ShouldIncludeAllCommentsWithCorrectSequence_WhenAllSettingsEnabled()
    {
        // Arrange
        var testResult = new LisSampleTestResult
        {
            ApprovedByUser = "JohnDoe",
            Reagents = new List<LisReagent>
            {
                new() { ReagentType = ArticleType.ImmunoCapCarrier, LotNumber = "LOT123" },
                new() { ReagentType = ArticleType.ConjugateCartridge, LotNumber = "LOT456" }
            },
            Ru = "15.7"
        };
        var exportSettings = new AstmExportSettings
        {
            IncludeUserIdInCommentRecord = true,
            IncludeLotNumbersInCommentRecord = IncludeLotNumbersInCommentRecord.UsedIncludingEmptyLotNumbers,
            IncludeRawDataInCommentRecord = true
        };

        // Act
        var commentRecords = AstmCommentRecordFactory.CreateFromSampleTestResult(testResult, exportSettings);

        // Assert
        commentRecords.Should().HaveCount(4);
        commentRecords.Select(x => (x.SequenceNumber, x.Text)).Should().Equal(
            (1, "Operator id JohnDoe"),
            (2, "ImmunoCAP LOT123"),
            (3, "Conjugate LOT456"),
            (4, "Response value in RU 15.7"));
    }

    [Trait("ProductRequirement", "PR_1310")]
    [Trait("ModuleRequirement", "MR_5694")]
    [Fact]
    public void CreateFromQualityControlResult_ShouldIncludeAllCommentsWithCorrectSequence_WhenAllSettingsEnabled()
    {
        var qcResult = new LisQualityControlTestResult
        {
            Reagents = new List<LisReagent>
            {
                new() { ReagentType = ArticleType.QualityControlCartridge, LotNumber = "QC001" }
            },
            Ru = "22.4"
        };

        var exportSettings = new AstmExportSettings
        {
            IncludeUserIdInCommentRecord = true,
            IncludeLotNumbersInCommentRecord = IncludeLotNumbersInCommentRecord.UsedIncludingEmptyLotNumbers,
            IncludeRawDataInCommentRecord = true
        };

        var commentRecords = AstmCommentRecordFactory.CreateFromQualityControlResult(qcResult, exportSettings);

        commentRecords.Should().HaveCount(2);
        commentRecords.Select(x => (x.SequenceNumber, x.Text)).Should().Equal(
            (1, "Quality control QC001"),
            (2, "Response value in RU 22.4"));
    }

    [Trait("ProductRequirement", "PR_1310")]
    [Trait("ModuleRequirement", "MR_5694")]
    [Fact]
    public void CreateFromQualityControlResult_ShouldIncludeAllCommentsWithCorrectSequence_WhenUserIdCommentIsDisabled()
    {
        var qcResult = new LisQualityControlTestResult
        {
            Reagents = new List<LisReagent>
            {
                new() { ReagentType = ArticleType.QualityControlCartridge, LotNumber = "QC001" }
            },
            Ru = "22.4"
        };

        var exportSettings = new AstmExportSettings
        {
            IncludeUserIdInCommentRecord = false,
            IncludeLotNumbersInCommentRecord = IncludeLotNumbersInCommentRecord.UsedIncludingEmptyLotNumbers,
            IncludeRawDataInCommentRecord = true
        };

        var commentRecords = AstmCommentRecordFactory.CreateFromQualityControlResult(qcResult, exportSettings);

        commentRecords.Should().HaveCount(2);
        commentRecords.Select(x => (x.SequenceNumber, x.Text)).Should().Equal(
            (1, "Quality control QC001"),
            (2, "Response value in RU 22.4"));
    }
}
