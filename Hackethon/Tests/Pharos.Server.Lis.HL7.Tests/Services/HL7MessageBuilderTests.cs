using FluentAssertions;
using Moq;
using Pharos.Server.Application.Lis;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Infrastructure;
using Pharos.Server.Lis.Application.Interfaces;
using Pharos.Server.Lis.HL7.Application.Services;
using Xunit;

namespace Pharos.Server.Lis.HL7.Tests.Services;

public class HL7MessageBuilderTests
{
    private readonly Mock<IDateTime> _dateTimeMock = new();
    private readonly HL7MessageBuilder _messageBuilder;

    public HL7MessageBuilderTests()
    {
        _dateTimeMock.Setup(x => x.Now).Returns(DateTimeOffset.Now);
        _messageBuilder = new HL7MessageBuilder(_dateTimeMock.Object);
    }

    [Fact]
    public void BuildRequestMessage_WithSingleSampleId_ShouldBuildQbpZosMessage()
    {
        // Arrange
        var lisChannel = new LisChannel
        {
            HL7Settings = new HL7Settings()
        };

        var sampleIds = new[] { "SAMPLE-001" };

        // Act
        var message = _messageBuilder.BuildRequestMessage(lisChannel, sampleIds);

        // Assert
        var segments = GetSegments(message);

        segments.Should().HaveCount(3);
        segments[0].Should().StartWith("MSH|^~\\&|Pharos|");
        segments[0].Should().Contain("|QBP^ZOS|");
        segments[1].Should().StartWith("QPD|ZOS^Lab Order Specimen Query|");
        segments[1].Should().EndWith("|SAMPLE-001");
        segments[2].Should().Be("RCP|I||R");
    }

    [Fact]
    public void BuildRequestMessage_WithNoSampleIds_ShouldBuildQueryForAllSamples()
    {
        // Arrange
        var lisChannel = new LisChannel
        {
            HL7Settings = new HL7Settings()
        };
        var sampleIds = Array.Empty<string>();

        // Act
        var message = _messageBuilder.BuildRequestMessage(lisChannel, sampleIds);

        // Assert
        var segments = GetSegments(message);

        segments.Should().HaveCount(3);
        segments[0].Should().StartWith("MSH|^~\\&|Pharos|");
        segments[0].Should().Contain("|QBP^ZOS|");
        segments[1].Should().StartWith("QPD|ZOS^Lab Order Specimen Query|");
        segments[1].Should().EndWith("|ALL");
        segments[2].Should().Be("RCP|I||R");
    }

    [Theory]
    [InlineData(false, "m1-f1")]
    [InlineData(true, "f1")]
    public void BuildSampleTestResultMessage_WithDefaultResultFormat_ShouldBuildOulR23Message(
        bool forceUniqueLaboratoryTestNames,
        string expectedUniversalServiceIdentifier)
    {
        // Arrange
        var lisChannel = CreateHL7Channel();
        lisChannel.ImportSettings.ForceUniqueLaboratoryTestNames = forceUniqueLaboratoryTestNames;
        var patients = CreatePatients();

        // Act
        var message = _messageBuilder.BuildSampleTestResultMessage(lisChannel, patients);

        // Assert
        var segments = GetSegments(message);
        segments[0].Should().Contain("|OUL^R23|");
        segments.Should().Contain(s => s.StartsWith("PID|"));
        segments.Should().Contain(s => s.StartsWith("ORC|"));
        segments.Should().Contain(s => s.StartsWith("SPM|"));
        segments.Should().Contain(s => s.StartsWith("SAC|"));
        segments.Should().Contain(s => s.StartsWith("OBR|"));
        segments.Should().Contain(s => s.StartsWith("OBX|"));
        segments.Single(s => s.StartsWith("OBR|")).Split('|')[4].Should().Be(expectedUniversalServiceIdentifier);
    }

    [Trait("ProductRequirement", "PR_576")]
    [Theory]
    [InlineData(false, "F", "C")]
    [InlineData(true, "W", "N")]
    public void BuildSampleTestResultMessage_WithLegacyGenderAndTubeTypeSetting_ShouldUseExpectedExportValues(
        bool useLegacyValues,
        string expectedGender,
        string expectedTubeType)
    {
        // Arrange
        var lisChannel = CreateHL7Channel();
        lisChannel.HL7Settings!.ExportSettings.UseLegacyValueForGenderAndTubeType = useLegacyValues;
        var patients = CreatePatients();
        patients[0].Samples[0].TubeType = "C";

        // Act
        var message = _messageBuilder.BuildSampleTestResultMessage(lisChannel, patients);

        // Assert
        var segments = GetSegments(message);
        segments.Single(s => s.StartsWith("PID|")).Split('|')[8].Should().Be(expectedGender);
        segments.Single(s => s.StartsWith("SPM|")).Should().EndWith($"|{expectedTubeType}");
    }

    [Fact]
    public void BuildSampleTestResultMessage_WithOrmResultFormat_ShouldBuildOrmO01MessageWithoutSpecimenSegments()
    {
        var lisChannel = CreateHL7Channel(HL7ExportResultFormat.ORM);
        var patients = CreatePatients();

        var message = _messageBuilder.BuildSampleTestResultMessage(lisChannel, patients);

        var segments = GetSegments(message);
        segments[0].Should().Contain("|ORM^O01|");
        segments.Should().NotContain(s => s.StartsWith("SPM|"));
        segments.Should().NotContain(s => s.StartsWith("SAC|"));
    }

    [Fact]
    public void BuildSampleTestResultMessage_WithOruResultFormat_ShouldBuildOruR01MessageWithoutSpecimenSegments()
    {
        var lisChannel = CreateHL7Channel(HL7ExportResultFormat.ORU);
        var patients = CreatePatients();

        var message = _messageBuilder.BuildSampleTestResultMessage(lisChannel, patients);

        var segments = GetSegments(message);
        segments[0].Should().Contain("|ORU^R01|");
        segments.Should().NotContain(s => s.StartsWith("SPM|"));
        segments.Should().NotContain(s => s.StartsWith("SAC|"));
    }

    [Fact]
    public void BuildSampleTestResultMessage_WithMultipleTestsForSameSample_ShouldBuildOneSampleWithMultipleObservations()
    {
        var lisChannel = CreateHL7Channel();
        var patients = CreatePatients(testOrders: [CreateTestOrder("m1", "f1"), CreateTestOrder("m1", "f2")]);

        var message = _messageBuilder.BuildSampleTestResultMessage(lisChannel, patients);

        var segments = GetSegments(message);
        segments.Count(s => s.StartsWith("PID|")).Should().Be(1);
        segments.Count(s => s.StartsWith("ORC|")).Should().Be(1);
        segments.Count(s => s.StartsWith("SPM|")).Should().Be(1);
        segments.Count(s => s.StartsWith("SAC|")).Should().Be(1);
        segments.Count(s => s.StartsWith("OBR|")).Should().Be(2);
        segments.Count(s => s.StartsWith("OBX|")).Should().Be(2);
    }

    [Fact]
    public void BuildSampleTestResultMessage_WithPendingResult_ShouldUsePendingStatus()
    {
        var lisChannel = CreateHL7Channel();
        var patients = CreatePatients(testOrders: [CreateTestOrder("m1", "f1", new LisSampleTestResult { Concentration = "1.2", Pending = true })]);

        var message = _messageBuilder.BuildSampleTestResultMessage(lisChannel, patients);

        GetSegments(message).Single(s => s.StartsWith("OBX|")).Should().EndWith("|I");
    }

    [Fact]
    public void BuildSampleTestResultMessage_WithErrorResult_ShouldUseCanceledStatus()
    {
        var lisChannel = CreateHL7Channel();
        var patients = CreatePatients(testOrders: [CreateTestOrder("m1", "f1", new LisSampleTestResult { Concentration = "1.2", HasError = true })]);

        var message = _messageBuilder.BuildSampleTestResultMessage(lisChannel, patients);

        GetSegments(message).Single(s => s.StartsWith("OBX|")).Should().EndWith("|X");
    }

    [Fact]
    public void BuildSampleTestResultMessage_WithApprovedResult_ShouldUseFinalStatus()
    {
        var lisChannel = CreateHL7Channel();
        var patients = CreatePatients(testOrders: [CreateTestOrder("m1", "f1", new LisSampleTestResult { Concentration = "1.2" })]);

        var message = _messageBuilder.BuildSampleTestResultMessage(lisChannel, patients);

        GetSegments(message).Single(s => s.StartsWith("OBX|")).Should().EndWith("|F");
    }

    [Fact]
    public void BuildSampleTestResultMessage_WithFinalLocalSample_ShouldUseFinalWithManualTestsStatus()
    {
        var lisChannel = CreateHL7Channel();
        var testOrder = CreateTestOrder("m1", "f1", new LisSampleTestResult { Concentration = "1.2" });
        var patients = CreatePatients(testOrders: [testOrder]);
        patients[0].Samples[0].IsSampleLocallyCreated = true;

        var message = _messageBuilder.BuildSampleTestResultMessage(lisChannel, patients);

        GetSegments(message).Single(s => s.StartsWith("OBX|")).Should().EndWith("|M");
    }

    [Fact]
    public void BuildSampleTestResultMessage_WithConcentrationClassCutOffAndQuotient_ShouldBuildSeparateObxSegments()
    {
        // Arrange
        var lisChannel = CreateHL7Channel();
        var testResult = new LisSampleTestResult
        {
            Concentration = "1.2",
            Class = "2",
            CutOff = "0.35",
            Quotient = "3.4"
        };
        var patients = CreatePatients(testOrders: [CreateTestOrder("m1", "f1", testResult)]);

        // Act
        var message = _messageBuilder.BuildSampleTestResultMessage(lisChannel, patients);

        // Assert
        var obxSegments = GetSegments(message).Where(s => s.StartsWith("OBX|")).ToArray();
        obxSegments.Should().HaveCount(4);
        obxSegments.Should().Contain(s => s.Contains("CONC-m1-f1"));
        obxSegments.Should().Contain(s => s.Contains("CLASS-m1-f1"));
        obxSegments.Should().Contain(s => s.Contains("CUTOFF-m1-f1"));
        obxSegments.Should().Contain(s => s.Contains("QUOT-m1-f1"));
    }

    [Fact]
    public void BuildSampleTestResultMessage_WhenForceUniqueLaboratoryTestNamesIsEnabled_ShouldUseLaboratoryTestNameOnly()
    {
        // Arrange
        var lisChannel = CreateHL7Channel();
        lisChannel.ImportSettings.ForceUniqueLaboratoryTestNames = true;
        var testOrder = CreateTestOrder("m1", "f1") with { FullTestName = "Full test 1" };
        var patients = CreatePatients([testOrder]);

        // Act
        var message = _messageBuilder.BuildSampleTestResultMessage(lisChannel, patients);

        // Assert
        var obxSegment = GetSegments(message).Single(s => s.StartsWith("OBX|"));
        obxSegment.Split('|')[3].Should().Be("CONC-f1^Full test 1 (f1)");
    }

    [Fact]
    public void BuildSampleTestResultMessage_WithTrayAndTubePosition_ShouldBuildSingleSacSegment()
    {
        var lisChannel = CreateHL7Channel();
        var patients = CreatePatients(testOrders:
        [
            CreateTestOrder("m1", "f1") with { TrayId = "TRAY01", TubePosition = 3 },
            CreateTestOrder("m1", "f2") with { TrayId = "TRAY02", TubePosition = 4 }
        ]);

        var message = _messageBuilder.BuildSampleTestResultMessage(lisChannel, patients);

        var sacSegments = GetSegments(message).Where(s => s.StartsWith("SAC|")).ToArray();
        sacSegments.Should().ContainSingle();
        sacSegments[0].Should().Contain("SAMPLE-001");
        sacSegments[0].Should().Contain("TRAY01|3");
    }

    [Trait("ProductRequirement", "PR_576")]
    [Fact]
    public void BuildSampleTestResultMessage_WhenLocalSampleAndUseLocalSampleIdAsLisSampleIdIsFalse_ShouldExportEmptySacContainerIdentifier()
    {
        var lisChannel = CreateHL7Channel();
        lisChannel.HL7Settings!.ExportSettings.UseLocalSampleIdAsLisSampleId = false;
        var patients = CreatePatients();
        patients[0].Samples[0].IsSampleLocallyCreated = true;

        var message = _messageBuilder.BuildSampleTestResultMessage(lisChannel, patients);

        var specimenContainerFields = GetSegments(message).Single(s => s.StartsWith("SAC|")).Split('|');
        specimenContainerFields[3].Should().BeEmpty();
        specimenContainerFields[13].Should().Be("TRAY01");
        specimenContainerFields[14].Should().Be("3");
    }

    [Trait("ProductRequirement", "PR_576")]
    [Fact]
    public void BuildSampleTestResultMessage_WhenLocalSampleAndUseLocalSampleIdAsLisSampleIdIsTrue_ShouldExportSampleIdAsSacContainerIdentifier()
    {
        var lisChannel = CreateHL7Channel();
        lisChannel.HL7Settings!.ExportSettings.UseLocalSampleIdAsLisSampleId = true;
        var patients = CreatePatients();
        patients[0].Samples[0].IsSampleLocallyCreated = true;

        var message = _messageBuilder.BuildSampleTestResultMessage(lisChannel, patients);

        GetSegments(message).Single(s => s.StartsWith("SAC|")).Split('|')[3].Should().Be("SAMPLE-001");
    }

    [Trait("ProductRequirement", "PR_576")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BuildSampleTestResultMessage_WhenSampleIsFromLis_ShouldExportSampleIdAsSacContainerIdentifier(bool useLocalSampleIdAsLisSampleId)
    {
        var lisChannel = CreateHL7Channel();
        lisChannel.HL7Settings!.ExportSettings.UseLocalSampleIdAsLisSampleId = useLocalSampleIdAsLisSampleId;
        var patients = CreatePatients();
        patients[0].Samples[0].IsSampleLocallyCreated = false;

        var message = _messageBuilder.BuildSampleTestResultMessage(lisChannel, patients);

        GetSegments(message).Single(s => s.StartsWith("SAC|")).Split('|')[3].Should().Be("SAMPLE-001");
    }

    [Fact]
    public void BuildSampleTestResultMessage_WithDecimalComma_ShouldNormalizeDecimalSeparator()
    {
        var lisChannel = CreateHL7Channel();
        var patients = CreatePatients(testOrders: [CreateTestOrder("m1", "f1", new LisSampleTestResult { Concentration = "1,23", Quotient = "3,45" })]);

        var message = _messageBuilder.BuildSampleTestResultMessage(lisChannel, patients);

        var obxSegments = GetSegments(message).Where(s => s.StartsWith("OBX|")).ToArray();
        obxSegments.Should().Contain(s => s.Contains("|1.23|"));
        obxSegments.Should().Contain(s => s.Contains("|3.45|"));
    }

    [Fact]
    public void BuildSampleTestResultMessage_WithEnabledCommentSettings_ShouldBuildApprovedUserLotAndRawDataNteSegments()
    {
        var lisChannel = CreateHL7Channel();
        lisChannel.HL7Settings!.ExportSettings.IncludeUserIdInCommentRecord = true;
        lisChannel.HL7Settings.ExportSettings.IncludeLotNumbersInCommentRecord = true;
        lisChannel.HL7Settings.ExportSettings.IncludeRawDataInCommentRecord = true;

        var result = new LisSampleTestResult
        {
            Concentration = "1.2",
            Ru = "45,6",
            ApprovedByUser = "operator1",
            Reagents = [new LisReagent { ReagentType = ArticleType.ImmunoCapCarrier, LotNumber = "LOT-1" }]
        };
        var patients = CreatePatients(testOrders: [CreateTestOrder("m1", "f1", result)]);

        var message = _messageBuilder.BuildSampleTestResultMessage(lisChannel, patients);

        var nteSegments = GetSegments(message).Where(s => s.StartsWith("NTE|")).ToArray();
        nteSegments.Should().Contain(s => s.Contains("Operator id operator1"));
        nteSegments.Should().Contain(s => s.Contains("ImmunoCAP LOT-1"));
        nteSegments.Should().Contain(s => s.Contains("Response value in RU 45.6"));
    }

    [Fact]
    public void BuildSampleTestResultMessage_WithLotNumberComments_ShouldBuildMappedNteSegmentsIncludingEmptyLots()
    {
        var lisChannel = CreateHL7Channel();
        lisChannel.HL7Settings!.ExportSettings.IncludeLotNumbersInCommentRecord = true;

        var result = new LisSampleTestResult
        {
            Concentration = "1.2",
            Reagents =
            [
                new LisReagent { ReagentType = ArticleType.ImmunoCapCarrier, LotNumber = "LOT-1" },
                new LisReagent { ReagentType = ArticleType.ConjugateCartridge, LotNumber = "" },
                new LisReagent { ReagentType = ArticleType.None, LotNumber = "IGNORED" }
            ]
        };
        var patients = CreatePatients(testOrders: [CreateTestOrder("m1", "f1", result)]);

        var message = _messageBuilder.BuildSampleTestResultMessage(lisChannel, patients);

        var commentSegments = GetSegments(message).Where(s => s.StartsWith("NTE|")).ToArray();
        commentSegments.Should().Equal(
            "NTE|1|O|ImmunoCAP LOT-1|I",
            "NTE|2|O|Conjugate |I");
    }

    [Trait("ProductRequirement", "PR_1310")]
    [Theory]
    [InlineData(false, "sIgE-d1")]
    [InlineData(true, "d1")]
    public void BuildQualityControlTestResultMessage_WithDefaultResultFormat_ShouldBuildOulR23Message(
        bool forceUniqueLaboratoryTestNames,
        string expectedUniversalServiceIdentifier)
    {
        // Arrange
        var lisChannel = CreateHL7Channel();
        lisChannel.ImportSettings.ForceUniqueLaboratoryTestNames = forceUniqueLaboratoryTestNames;
        var qcTestResult = CreateQualityControlTestResult();

        // Act
        var message = _messageBuilder.BuildQualityControlTestResultMessage(lisChannel, qcTestResult);

        // Assert
        var segments = GetSegments(message);
        segments.Should().HaveCount(7);
        segments[0].Should().Contain("|OUL^R23|");
        segments[1].Should().Be("PID|1|||||||U");
        segments[2].Should().Be("ORC|RE");
        segments[3].Should().Be("SPM|1|||QC3");
        segments[4].Should().Be("SAC|||sIgE Pos");
        segments[5].Should().Be($"OBR|1|||{expectedUniversalServiceIdentifier}|||20260120160000||||||||QC3");
        segments[6].Should().Be("OBX|1|ST|CONC-sIgE-d1^Concentration||2.32|kU/L|||||F|||20260120160000");
    }

    [Trait("ProductRequirement", "PR_1310")]
    [Theory]
    [InlineData(HL7ExportResultFormat.ORM, "ORM^O01")]
    [InlineData(HL7ExportResultFormat.ORU, "ORU^R01")]
    public void BuildQualityControlTestResultMessage_WithoutOulResultFormat_ShouldOmitSpecimenSegments(
        HL7ExportResultFormat resultFormat,
        string expectedMessageType)
    {
        // Arrange
        var lisChannel = CreateHL7Channel(resultFormat);
        var qcTestResult = CreateQualityControlTestResult();

        // Act
        var message = _messageBuilder.BuildQualityControlTestResultMessage(lisChannel, qcTestResult);

        // Assert
        var segments = GetSegments(message);
        segments[0].Should().Contain($"|{expectedMessageType}|");
        segments.Should().NotContain(s => s.StartsWith("SPM|"));
        segments.Should().NotContain(s => s.StartsWith("SAC|"));
        segments.Should().Contain(s => s.StartsWith("OBR|")).Which.Should().EndWith("|QC3");
    }

    [Trait("ProductRequirement", "PR_1310")]
    [Fact]
    public void BuildQualityControlTestResultMessage_WithoutConcentration_ShouldUseCanceledStatus()
    {
        // Arrange
        var lisChannel = CreateHL7Channel();
        var qcTestResult = CreateQualityControlTestResult() with { Concentration = null };

        // Act
        var message = _messageBuilder.BuildQualityControlTestResultMessage(lisChannel, qcTestResult);

        // Assert
        var resultSegment = GetSegments(message).Single(s => s.StartsWith("OBX|"));
        resultSegment.Split('|')[11].Should().Be("X");
    }

    [Trait("ProductRequirement", "PR_1310")]
    [Fact]
    public void BuildQualityControlTestResultMessage_WithEnabledComments_ShouldBuildLotAndRawDataComments()
    {
        // Arrange
        var lisChannel = CreateHL7Channel();
        lisChannel.HL7Settings!.ExportSettings.IncludeUserIdInCommentRecord = true;
        lisChannel.HL7Settings.ExportSettings.IncludeLotNumbersInCommentRecord = true;
        lisChannel.HL7Settings.ExportSettings.IncludeRawDataInCommentRecord = true;
        var qcTestResult = CreateQualityControlTestResult() with
        {
            Ru = "45,6",
            Reagents = [new LisReagent { ReagentType = ArticleType.QualityControlCartridge, LotNumber = "QC-LOT-1" }]
        };

        // Act
        var message = _messageBuilder.BuildQualityControlTestResultMessage(lisChannel, qcTestResult);

        // Assert
        var commentSegments = GetSegments(message).Where(s => s.StartsWith("NTE|")).ToArray();
        commentSegments.Should().Equal(
            "NTE|1|O|Quality control QC-LOT-1|I",
            "NTE|2|O|Response value in RU 45.6|I");
    }

    private static string[] GetSegments(ILisMessage message)
    {
        return message.GetMessageString().Split('\r', StringSplitOptions.RemoveEmptyEntries);
    }

    private static LisChannel CreateHL7Channel(HL7ExportResultFormat resultFormat = HL7ExportResultFormat.OUL)
    {
        var channel = new LisChannel
        {
            ProtocolType = LisProtocolType.HL7
        };

        channel.HL7Settings!.ExportSettings.ResultFormat = resultFormat;
        return channel;
    }

    private static List<LisPatient> CreatePatients(IReadOnlyList<LisTestOrder>? testOrders = null)
    {
        return
        [
            new LisPatient
            {
                PracticePatientId = "PID1",
                MedicalRecordNumber = "MRN1",
                PatientNumber = "PNR1",
                Name = "Doe Jane",
                BirthDate = new DateOnly(1980, 1, 2),
                Gender = Gender.Female,
                Address = "Street",
                ZipCode = "12345",
                Samples =
                [
                    new LisSample
                    {
                        SampleId = "SAMPLE-001",
                        RequestId = "REQ-001",
                        RequestDate = new DateTimeOffset(2026, 1, 20, 15, 0, 0, TimeSpan.Zero),
                        SampleCollectionDate = new DateTimeOffset(2026, 1, 20, 14, 0, 0, TimeSpan.Zero),
                        TubeType = "N",
                        TestOrders = (testOrders ?? [CreateTestOrder("m1", "f1")]).ToList()
                    }
                ]
            }
        ];
    }

    private static LisTestOrder CreateTestOrder(string methodName, string testName, LisSampleTestResult? testResult = null)
    {
        testResult ??= new LisSampleTestResult
        {
            Concentration = "1.2",
            Units = "kU/L",
            MeasurementDateTime = new DateTimeOffset(2026, 1, 20, 16, 0, 0, TimeSpan.Zero)
        };

        return new LisTestOrder
        {
            MethodName = methodName,
            TestName = testName,
            TrayId = "TRAY01",
            TubePosition = 3,
            ReportConcentration = !string.IsNullOrWhiteSpace(testResult.Concentration),
            ReportClass = !string.IsNullOrWhiteSpace(testResult.Class),
            ReportCutOff = !string.IsNullOrWhiteSpace(testResult.CutOff),
            ReportQuotient = !string.IsNullOrWhiteSpace(testResult.Quotient),
            TestResult = testResult
        };
    }

    private static LisQualityControlTestResult CreateQualityControlTestResult()
    {
        return new LisQualityControlTestResult
        {
            QualityControlName = "sIgE Pos",
            MethodName = "sIgE",
            TestName = "d1",
            Concentration = "2,32",
            Units = "kU/L",
            MeasurementDateTime = new DateTimeOffset(2026, 1, 20, 16, 0, 0, TimeSpan.Zero)
        };
    }

    #region Panel Name Export Tests

    [Fact]
    public void BuildSampleTestResultMessage_WhenExportPanelNameIsOff_ShouldUseTestName()
    {
        // Arrange
        var lisChannel = CreateHL7Channel();
        lisChannel.HL7Settings!.ExportSettings.ExportPanelNameInOrderRecord = false;

        var testOrder = CreateTestOrder("MethodA", "TestB");
        testOrder.PanelNames = ["Panel1", "Panel2"];

        var patients = CreatePatients([testOrder]);

        // Act
        var message = _messageBuilder.BuildSampleTestResultMessage(lisChannel, patients);

        // Assert
        var segments = GetSegments(message);
        var obrSegment = segments.FirstOrDefault(s => s.StartsWith("OBR|"));
        obrSegment.Should().NotBeNull();
        obrSegment.Should().Contain("MethodA"); // Should contain method name
        obrSegment.Should().Contain("TestB"); // Should contain test name
        obrSegment.Should().NotContain("Panel1"); // Should NOT contain panel name
    }

    [Fact]
    public void BuildSampleTestResultMessage_WhenExportPanelNameIsOnWithPanels_ShouldUseFirstPanelName()
    {
        // Arrange
        var lisChannel = CreateHL7Channel();
        lisChannel.HL7Settings!.ExportSettings.ExportPanelNameInOrderRecord = true;

        var testOrder = CreateTestOrder("MethodA", "TestB");
        testOrder.PanelNames = ["PanelX", "PanelY"];

        var patients = CreatePatients([testOrder]);

        // Act
        var message = _messageBuilder.BuildSampleTestResultMessage(lisChannel, patients);

        // Assert
        var segments = GetSegments(message);
        var obrSegment = segments.FirstOrDefault(s => s.StartsWith("OBR|"));
        obrSegment.Should().NotBeNull();
        obrSegment.Should().Contain("PanelX"); // Should contain first panel name
        obrSegment.Should().NotContain("TestB"); // Should NOT contain test name
        obrSegment.Should().NotContain("MethodA"); // Should NOT contain method name
        obrSegment.Should().NotContain("PanelY"); // Should NOT contain second panel name
    }

    [Fact]
    public void BuildSampleTestResultMessage_WhenExportPanelNameIsOnButNoPanels_ShouldFallbackToTestName()
    {
        // Arrange
        var lisChannel = CreateHL7Channel();
        lisChannel.HL7Settings!.ExportSettings.ExportPanelNameInOrderRecord = true;

        var testOrder = CreateTestOrder("MethodA", "TestB");
        testOrder.PanelNames = [];

        var patients = CreatePatients([testOrder]);

        // Act
        var message = _messageBuilder.BuildSampleTestResultMessage(lisChannel, patients);

        // Assert
        var segments = GetSegments(message);
        var obrSegment = segments.FirstOrDefault(s => s.StartsWith("OBR|"));
        obrSegment.Should().NotBeNull();
        obrSegment.Should().Contain("MethodA"); // Should fallback to method name
        obrSegment.Should().Contain("TestB"); // Should fallback to test name
    }

    [Fact]
    public void BuildSampleTestResultMessage_WhenExportPanelNameDefaultValue_ShouldBeOff()
    {
        // Arrange
        var lisChannel = CreateHL7Channel();
        // Don't explicitly set ExportPanelNameInOrderRecord - verify default is false

        var testOrder = CreateTestOrder("MethodA", "TestB");
        testOrder.PanelNames = ["Panel1"];

        var patients = CreatePatients([testOrder]);

        // Act
        var message = _messageBuilder.BuildSampleTestResultMessage(lisChannel, patients);

        // Assert
        lisChannel.HL7Settings!.ExportSettings.ExportPanelNameInOrderRecord.Should().BeFalse("default value should be false");
        var segments = GetSegments(message);
        var obrSegment = segments.FirstOrDefault(s => s.StartsWith("OBR|"));
        obrSegment.Should().NotBeNull();
        obrSegment.Should().Contain("TestB"); // Should use test name (default behavior)
    }

    #endregion
}
