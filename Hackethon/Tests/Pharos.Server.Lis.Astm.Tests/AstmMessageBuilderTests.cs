using System.ComponentModel.DataAnnotations;
using Pharos.Server.Application;
using Pharos.Server.Application.Lis;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Infrastructure;
using Pharos.Server.Lis.Astm.Application.Services;


namespace Pharos.Server.Lis.Astm.Tests;

public class AstmMessageBuilderTests
{
    private readonly AstmMessageBuilder _messageBuilder;

    public AstmMessageBuilderTests()
    {
        var mockVersionService = new Mock<IPharosVersionService>();
        var mockDateTime = new Mock<IDateTime>();
        _messageBuilder = new AstmMessageBuilder(mockVersionService.Object, mockDateTime.Object);

        mockVersionService.Setup(x => x.Version).Returns("1.0.0");
        mockDateTime.Setup(x => x.Now).Returns(DateTimeOffset.Now);

    }

    [Fact]
    public void BuildSampleResultMessage_WhenSkipExportOfPatientInformationIsTrue_ShouldExcludePatientInformation()
    {
        // Arrange
        var lisChannel = CreateLisChannelWithSettings(skipPatientInfo: true);
        var patients = CreateTestPatients();

        // Act
        var message = _messageBuilder.BuildSampleTestResultMessage(lisChannel, patients).GetMessageString();

        // Assert
        message.Should().NotBeNull();
        message.Should().Contain("H|"); // Header record
        message.Should().Contain("P|"); // Patient record
        message.Should().Contain("O|"); // Order record
        message.Should().Contain("R|"); // Result record
        message.Should().Contain("L|1|N"); // Terminator

        // Should not contain patient name, diagnosis, or other patient info
        message.Should().NotContain("John Doe");
        message.Should().NotContain("Test Diagnosis");
        message.Should().NotContain("123 Main St");
    }

    [Fact]
    public void BuildSampleResultMessage_WhenSkipExportOfPatientInformationIsFalse_ShouldIncludePatientInformation()
    {
        // Arrange
        var lisChannel = CreateLisChannelWithSettings(skipPatientInfo: false);
        var patients = CreateTestPatients();

        // Act
        var message = _messageBuilder.BuildSampleTestResultMessage(lisChannel, patients).GetMessageString();

        // Assert
        message.Should().NotBeNull();
        message.Should().Contain("H|"); // Header record
        message.Should().Contain("P|"); // Patient record
        message.Should().Contain("O|"); // Order record
        message.Should().Contain("R|"); // Result record
        message.Should().Contain("L|1|N"); // Terminator

        // Should contain patient information
        message.Should().Contain("John Doe");
        message.Should().Contain("Test Diagnosis");
        message.Should().Contain("123 Main St");
    }

    [Trait("ProductRequirement", "PR_1306")]
    [Trait("ModuleRequirement", "MR_5856")]
    [Fact]
    public void BuildSampleResultMessage_WhenExportSettingsIsNull_ShouldIncludePatientInformation()
    {
        // Arrange
        var lisChannel = CreateLisChannelWithSettings(skipPatientInfo: null);
        var patients = CreateTestPatients();

        // Act
        var message = _messageBuilder.BuildSampleTestResultMessage(lisChannel, patients).GetMessageString();

        // Assert
        message.Should().NotBeNull();
        // Should include patient information when setting is null (default behavior)
        message.Should().Contain("John Doe");
        message.Should().Contain("Test Diagnosis");
    }

    #region Override Instrument Name Tests

    [Trait("ProductRequirement", "PR_1306")]
    [Trait("ModuleRequirement", "MR_5626")]
    [Fact]
    public void BuildSampleResultMessage_WhenOverrideInstrumentNameIsSet_ShouldUseOverrideInstrumentName()
    {
        // Arrange
        var lisChannel = CreateLisChannelWithOverrideInstrumentName("OverrideInstrument");
        var patients = CreateTestPatientsWithInstrumentName("OriginalInstrument");

        // Act
        var message = _messageBuilder.BuildSampleTestResultMessage(lisChannel, patients).GetMessageString();

        // Assert
        message.Should().NotBeNull();
        message.Should().Contain("OverrideInstrument");
        message.Should().NotContain("OriginalInstrument");
    }

    [Trait("ProductRequirement", "PR_1306")]
    [Trait("ModuleRequirement", "MR_5626")]
    [Fact]
    public void BuildSampleResultMessage_WhenOverrideInstrumentNameIsNotSet_ShouldUseOriginalInstrumentName()
    {
        // Arrange
        var lisChannel = CreateLisChannelWithOverrideInstrumentName("");
        var patients = CreateTestPatientsWithInstrumentName("OriginalInstrument");

        // Act
        var message = _messageBuilder.BuildSampleTestResultMessage(lisChannel, patients).GetMessageString();

        // Assert
        message.Should().NotBeNull();
        message.Should().Contain("OriginalInstrument");
    }

    #endregion

    #region AssignDataOrMeasurementValues Tests

    [Fact]
    public void BuildSampleResultMessage_WhenSomeMeasurementValuesAreProvided_ShouldIncludeOnlyProvidedValues()
    {
        // Arrange
        var lisChannel = CreateLisChannelWithSettings(skipPatientInfo: false);
        var patients = CreateTestPatientsWithMeasurementValues(
            concentration: "15.3",
            classValue: null,
            cutoff: "0.7",
            quotient: null);

        // Act
        var message = _messageBuilder.BuildSampleTestResultMessage(lisChannel, patients).GetMessageString();

        // Assert
        message.Should().NotBeNull();
        message.Should().Contain("R|");
        message.Should().Contain("15.3");
        message.Should().Contain("0.7");
    }

    [Trait("ProductRequirement", "PR_1306")]
    [Trait("ModuleRequirement", "MR_5626")]
    [Fact]
    public void BuildSampleResultMessage_WhenReportFlagsAreDisabled_ShouldSuppressMeasurementValues()
    {
        // Arrange
        var lisChannel = CreateLisChannelWithSettings(skipPatientInfo: false);
        var patients = CreateTestPatientsWithMeasurementValues(
            concentration: "15.3",
            classValue: "2",
            cutoff: "0.7",
            quotient: "1.5",
            enableReportFlags: false);

        // Act
        var message = _messageBuilder.BuildSampleTestResultMessage(lisChannel, patients).GetMessageString();

        // Assert
        message.Should().NotBeNull();
        message.Should().Contain("R|");
        message.Should().NotContain("15.3");
        message.Should().NotContain("0.7");
        message.Should().NotContain("1.5");
    }

    [Trait("ProductRequirement", "PR_1306")]
    [Trait("ModuleRequirement", "MR_5626")]
    [Fact]
    public void BuildSampleResultMessage_WhenOnlySomeReportFlagsAreEnabled_ShouldIncludeOnlyEnabledMeasurementValues()
    {
        // Arrange
        var lisChannel = CreateLisChannelWithSettings(skipPatientInfo: false);
        var patients = CreateTestPatientsWithMeasurementValues(
            concentration: "15.3",
            classValue: "2",
            cutoff: "0.7",
            quotient: "1.5",
            reportConcentration: true,
            reportClass: false,
            reportCutOff: false,
            reportQuotient: true);

        // Act
        var message = _messageBuilder.BuildSampleTestResultMessage(lisChannel, patients).GetMessageString();

        // Assert
        message.Should().NotBeNull();
        message.Should().Contain("R|");
        message.Should().Contain("15.3");
        message.Should().Contain("1.5");
        message.Should().NotContain("0.7");
    }

    #endregion

    #region UseFourFieldsInResultReporting Tests

    [Trait("ProductRequirement", "PR_1305")]
    [Trait("ModuleRequirement", "MR_5620")]
    [Fact]
    public void BuildSampleResultMessage_WhenUseFourFieldsIsTrue_ShouldHaveOneSeparatorBetweenCutoffAndQuotient()
    {
        // Arrange
        var lisChannel = CreateLisChannelWithFourFieldsReporting(useFourFields: true);
        var patients = CreateTestPatientsWithMeasurementValues(
            concentration: "10.5",
            classValue: "2",
            cutoff: "0.35",
            quotient: "1.5");

        // Act
        var message = _messageBuilder.BuildSampleTestResultMessage(lisChannel, patients).GetMessageString();

        // Assert
        message.Should().NotBeNull();
        message.Should().Contain("R|");
        message.Should().Contain("0.35^1.5");
        message.Should().NotContain("0.35^^1.5");
    }

    [Trait("ProductRequirement", "PR_1305")]
    [Trait("ModuleRequirement", "MR_5620")]
    [Fact]
    public void BuildSampleResultMessage_WhenUseFourFieldsIsFalse_ShouldHaveTwoSeparatorsBetweenCutoffAndQuotient()
    {
        // Arrange
        var lisChannel = CreateLisChannelWithFourFieldsReporting(useFourFields: false);
        var patients = CreateTestPatientsWithMeasurementValues(
            concentration: "10.5",
            classValue: "2",
            cutoff: "0.35",
            quotient: "1.5");

        // Act
        var message = _messageBuilder.BuildSampleTestResultMessage(lisChannel, patients).GetMessageString();

        // Assert
        message.Should().NotBeNull();
        message.Should().Contain("R|");
        message.Should().Contain("0.35^^1.5");
        message.Should().NotContain("0.35^1.5");
    }

    #endregion

    #region User ID in Result Record Tests

    [Trait("ProductRequirement", "PR_1305")]
    [Trait("ModuleRequirement", "MR_5622")]
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BuildSampleResultMessage_IncludeUserIdInResultsRecord_ShouldIncludeOperatorIdBasedOnSetting(bool includeUserIdInResultsRecord)
    {
        // Arrange
        var lisChannel = new LisChannel
        {
            AstmSettings = new AstmSettings
            {
                ExportSettings = new AstmExportSettings
                {
                    IncludeUserIdInResultsRecord = includeUserIdInResultsRecord,
                }
            }
        };
        var patients = new List<LisPatient>
        {
            new() {
                Samples = new List<LisSample>
                {
                    new() {
                        TestOrders = new List<LisTestOrder>
                        {
                            new() {
                                TestResult = new LisSampleTestResult
                                {
                                    Concentration = "10.5",
                                    ApprovedByUser = "JohnDoe"
                                }
                            }
                        }
                    }
                }
            }
        };

        // Act
        var message = _messageBuilder.BuildSampleTestResultMessage(lisChannel, patients).GetMessageString();

        // Assert
        message.Should().NotBeNullOrEmpty();
        message.Should().Contain("R|");

        _ = includeUserIdInResultsRecord
        ? message.Should().Contain("JohnDoe")
        : message.Should().NotContain("JohnDoe");
    }

    #endregion

    #region Units Tests

    [Fact]
    public void BuildSampleResultMessage_WhenUnitsAreNull_ShouldNotIncludeUnitsInMessage()
    {
        // Arrange
        var lisChannel = CreateLisChannelWithSettings(skipPatientInfo: false);
        var patients = CreateTestPatientsWithUnits(null);

        // Act
        var message = _messageBuilder.BuildSampleTestResultMessage(lisChannel, patients).GetMessageString();

        // Assert
        message.Should().NotBeNull();
        message.Should().Contain("R|");
    }

    [Theory]
    [InlineData("kU/L")]
    [InlineData("mg/dL")]
    [InlineData("IU/mL")]
    [InlineData("µg/L")]
    public void BuildSampleResultMessage_WithDifferentUnitTypes_ShouldIncludeCorrectUnits(string units)
    {
        // Arrange
        var lisChannel = CreateLisChannelWithSettings(skipPatientInfo: false);
        var patients = CreateTestPatientsWithUnits(units);

        // Act
        var message = _messageBuilder.BuildSampleTestResultMessage(lisChannel, patients).GetMessageString();

        // Assert
        message.Should().NotBeNull();
        message.Should().Contain("R|");
        message.Should().Contain(units);
    }

    [Fact]
    public void BuildSampleResultMessage_WithMultipleTestsAndDifferentUnits_ShouldIncludeAllUnits()
    {
        // Arrange
        var lisChannel = CreateLisChannelWithSettings(skipPatientInfo: false);
        var patients = CreateTestPatientsWithMultipleTestsAndDifferentUnits();

        // Act
        var message = _messageBuilder.BuildSampleTestResultMessage(lisChannel, patients).GetMessageString();

        // Assert
        message.Should().NotBeNull();
        message.Should().Contain("R|");
        message.Should().Contain("kU/L");
        message.Should().Contain("mg/dL");
        message.Should().Contain("IU/mL");
    }

    private static List<LisPatient> CreateTestPatientsWithUnits(string? units)
    {
        var testResult = new LisSampleTestResult
        {
            TestOrderId = Guid.NewGuid(),
            Concentration = "10.5",
            Units = units,
            Pending = false,
            HasError = false,
            MeasurementDateTime = DateTimeOffset.Now
        };

        var testOrder = new LisTestOrder
        {
            TestName = "Glucose",
            MethodName = "GOD-PAP",
            ReportConcentration = true,
            TestResult = testResult
        };

        var sample = new LisSample
        {
            SampleId = "SAMPLE001",
            TestOrders = new List<LisTestOrder> { testOrder }
        };

        var patient = new LisPatient
        {
            Name = "John Doe",
            Gender = Gender.Male,
            BirthDate = new DateOnly(1980, 1, 1),
            PatientNumber = "P12345",
            Samples = new List<LisSample> { sample }
        };

        return new List<LisPatient> { patient };
    }

    private static List<LisPatient> CreateTestPatientsWithMultipleTestsAndDifferentUnits()
    {
        var testResult1 = new LisSampleTestResult
        {
            TestOrderId = Guid.NewGuid(),
            Concentration = "10.5",
            Units = "kU/L",
            Pending = false,
            HasError = false,
            MeasurementDateTime = DateTimeOffset.Now
        };

        var testResult2 = new LisSampleTestResult
        {
            TestOrderId = Guid.NewGuid(),
            Concentration = "15.3",
            Units = "mg/dL",
            Pending = false,
            HasError = false,
            MeasurementDateTime = DateTimeOffset.Now
        };

        var testResult3 = new LisSampleTestResult
        {
            TestOrderId = Guid.NewGuid(),
            Concentration = "8.7",
            Units = "IU/mL",
            Pending = false,
            HasError = false,
            MeasurementDateTime = DateTimeOffset.Now
        };

        var testOrder1 = new LisTestOrder
        {
            TestName = "TestA",
            MethodName = "MethodA",
            ReportConcentration = true,
            TestResult = testResult1
        };

        var testOrder2 = new LisTestOrder
        {
            TestName = "TestB",
            MethodName = "MethodB",
            ReportConcentration = true,
            TestResult = testResult2
        };

        var testOrder3 = new LisTestOrder
        {
            TestName = "TestC",
            MethodName = "MethodC",
            ReportConcentration = true,
            TestResult = testResult3
        };

        var sample = new LisSample
        {
            SampleId = "SAMPLE001",
            TestOrders = new List<LisTestOrder> { testOrder1, testOrder2, testOrder3 }
        };

        var patient = new LisPatient
        {
            Name = "John Doe",
            Gender = Gender.Male,
            BirthDate = new DateOnly(1980, 1, 1),
            PatientNumber = "P12345",
            Samples = new List<LisSample> { sample }
        };

        return new List<LisPatient> { patient };
    }

    #endregion

    #region Test Helper Methods
    private static LisChannel CreateLisChannelWithSettings(bool? skipPatientInfo)
    {
        var astmSettings = new AstmSettings
        {
            Other = new AstmOtherSettings
            {
                PatientNameInTwoFields = false,
                CombineRequestorAddressLinesOneToThree = false
            }
        };

        if (skipPatientInfo.HasValue)
        {
            astmSettings.ExportSettings = new AstmExportSettings
            {
                SkipExportOfPatientInformation = skipPatientInfo.Value,
            };
        }

        return new LisChannel
        {
            AstmSettings = astmSettings
        };
    }

    private static List<LisPatient> CreateTestPatients()
    {
        var testResult = new LisSampleTestResult
        {
            TestOrderId = Guid.NewGuid(),
            Concentration = "10.5",
            Pending = false,
            HasError = false,
            MeasurementDateTime = DateTimeOffset.Now
        };

        var testOrder = new LisTestOrder
        {
            TestName = "Glucose",
            MethodName = "GOD-PAP",
            TestResult = testResult
        };

        var sample = new LisSample
        {
            SampleId = "SAMPLE001",
            TestOrders = new List<LisTestOrder> { testOrder }
        };

        var patient = new LisPatient
        {
            Name = "John Doe",
            Gender = Gender.Male,
            BirthDate = new DateOnly(1980, 1, 1),
            PatientNumber = "P12345",
            MedicalRecordNumber = "MR67890",
            PracticePatientId = "PP11111",
            Address = "123 Main St\nAnytown\nAnystate\n12345",
            ZipCode = "12345",
            Diagnosis = "Test Diagnosis",
            Comment = "Test Comment",
            PatientAge = 44,
            Samples = new List<LisSample> { sample }
        };

        return new List<LisPatient> { patient };
    }

    private static List<LisPatient> CreateTestPatientsWithInstrumentName(string? instrumentName)
    {
        var testResult = new LisSampleTestResult
        {
            TestOrderId = Guid.NewGuid(),
            Concentration = "10.5",
            InstrumentName = instrumentName,
            Pending = false,
            HasError = false,
            MeasurementDateTime = DateTimeOffset.Now
        };

        var testOrder = new LisTestOrder
        {
            TestName = "Glucose",
            MethodName = "GOD-PAP",
            TestResult = testResult
        };

        var sample = new LisSample
        {
            SampleId = "SAMPLE001",
            TestOrders = new List<LisTestOrder> { testOrder }
        };

        var patient = new LisPatient
        {
            Name = "John Doe",
            Gender = Gender.Male,
            BirthDate = new DateOnly(1980, 1, 1),
            PatientNumber = "P12345",
            Samples = new List<LisSample> { sample }
        };

        return new List<LisPatient> { patient };
    }

    private static LisChannel CreateLisChannelWithOverrideInstrumentName(string overrideInstrumentName)
    {
        var astmSettings = new AstmSettings
        {
            Other = new AstmOtherSettings
            {
                PatientNameInTwoFields = false,
                CombineRequestorAddressLinesOneToThree = false
            },
            ExportSettings = new AstmExportSettings
            {
                OverrideInstrumentName = overrideInstrumentName
            }
        };

        return new LisChannel
        {
            AstmSettings = astmSettings
        };
    }

    private static List<LisPatient> CreateTestPatientsWithMeasurementValues(
        string? concentration = null,
        string? classValue = null,
        string? cutoff = null,
        string? quotient = null,
        bool enableReportFlags = true,
        bool? reportConcentration = null,
        bool? reportClass = null,
        bool? reportCutOff = null,
        bool? reportQuotient = null)
    {
        var testResult = new LisSampleTestResult
        {
            TestOrderId = Guid.NewGuid(),
            Concentration = concentration,
            Class = classValue,
            CutOff = cutoff,
            Quotient = quotient,
            Pending = false,
            HasError = false,
            MeasurementDateTime = DateTimeOffset.Now
        };

        var testOrder = new LisTestOrder
        {
            TestName = "Glucose",
            MethodName = "GOD-PAP",
            ReportConcentration = reportConcentration ?? (enableReportFlags && !string.IsNullOrWhiteSpace(concentration)),
            ReportClass = reportClass ?? (enableReportFlags && !string.IsNullOrWhiteSpace(classValue)),
            ReportCutOff = reportCutOff ?? (enableReportFlags && !string.IsNullOrWhiteSpace(cutoff)),
            ReportQuotient = reportQuotient ?? (enableReportFlags && !string.IsNullOrWhiteSpace(quotient)),
            TestResult = testResult
        };

        var sample = new LisSample
        {
            SampleId = "SAMPLE001",
            TestOrders = new List<LisTestOrder> { testOrder }
        };

        var patient = new LisPatient
        {
            Name = "John Doe",
            Gender = Gender.Male,
            BirthDate = new DateOnly(1980, 1, 1),
            PatientNumber = "P12345",
            Samples = new List<LisSample> { sample }
        };

        return new List<LisPatient> { patient };
    }

    private static LisChannel CreateLisChannelWithFourFieldsReporting(bool useFourFields)
    {
        var astmSettings = new AstmSettings
        {
            Other = new AstmOtherSettings
            {
                PatientNameInTwoFields = false,
                CombineRequestorAddressLinesOneToThree = false
            },
            ExportSettings = new AstmExportSettings
            {
                UseFourFieldsInResultReporting = useFourFields
            }
        };

        return new LisChannel
        {
            AstmSettings = astmSettings
        };
    }

    #endregion

    #region HostName Validation Tests

    [Fact]
    public void HostName_WithExactly64Characters_ShouldPassValidation()
    {
        // Arrange - Create a hostname with exactly 64 characters
        var hostname64 = new string('H', 64);
        var tcpSettings = new AstmCommunicationTcpSettings
        {
            HostName = hostname64
        };

        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(tcpSettings);

        // Act
        var isValid = Validator.TryValidateObject(tcpSettings, validationContext, validationResults, validateAllProperties: true);

        // Assert
        isValid.Should().BeTrue();
        validationResults.Should().BeEmpty("because no validation errors should occur for 64 characters");
    }

    [Fact]
    public void HostName_With65Characters_ShouldBeBlocked()
    {
        // Arrange - Create a hostname with 65 characters (1 over limit)
        var hostname65 = new string('H', 65);
        var tcpSettings = new AstmCommunicationTcpSettings
        {
            HostName = hostname65
        };

        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(tcpSettings);

        // Act
        var isValid = Validator.TryValidateObject(tcpSettings, validationContext, validationResults, validateAllProperties: true);

        // Assert
        isValid.Should().BeFalse("because the 65th character should be blocked");
        validationResults.Should().HaveCount(1, "because exactly one validation error should be generated");
        validationResults[0].MemberNames.Should().Contain(nameof(AstmCommunicationTcpSettings.HostName));
        validationResults[0].ErrorMessage.Should().Contain("64", "because error message should mention the 64 character limit");
    }

    [Fact]
    public void HostName_WithSpecialCharacter_ShouldPassValidation()
    {
        // Arrange
        var hostname = "my-server_123.domain.com";  // Hyphen, underscore, and dots
        var tcpSettings = new AstmCommunicationTcpSettings { HostName = hostname };
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(tcpSettings);

        // Act
        var isValid = Validator.TryValidateObject(tcpSettings, validationContext, validationResults, validateAllProperties: true);

        // Assert
        isValid.Should().BeTrue();
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void HostNameForSeparateExportPort_WithSpecialCharacter_ShouldPassValidation()
    {
        // Arrange
        var hostname = "my-server_123.domain.com";  // Hyphen, underscore, and dots
        var tcpSettings = new AstmCommunicationTcpSettings
        {
            UseSeparateExportPort = true,
            HostNameForSeparateExportPort = hostname
        };
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(tcpSettings);

        // Act
        var isValid = Validator.TryValidateObject(tcpSettings, validationContext, validationResults, validateAllProperties: true);

        // Assert
        isValid.Should().BeTrue();
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void HostNameForSeparateExportPort_WithExactly64Characters_ShouldPassValidation()
    {
        // Arrange
        var hostname64 = new string('E', 64);
        var tcpSettings = new AstmCommunicationTcpSettings
        {
            UseSeparateExportPort = true,
            HostNameForSeparateExportPort = hostname64
        };

        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(tcpSettings);

        // Act
        var isValid = Validator.TryValidateObject(tcpSettings, validationContext, validationResults, validateAllProperties: true);

        // Assert
        isValid.Should().BeTrue("because export hostname with exactly 64 characters should be accepted");
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void HostNameForSeparateExportPort_With65Characters_ShouldBeBlocked()
    {
        // Arrange
        var hostname65 = new string('E', 65);
        var tcpSettings = new AstmCommunicationTcpSettings
        {
            UseSeparateExportPort = true,
            HostNameForSeparateExportPort = hostname65
        };

        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(tcpSettings);

        // Act
        var isValid = Validator.TryValidateObject(tcpSettings, validationContext, validationResults, validateAllProperties: true);

        // Assert
        isValid.Should().BeFalse("because export hostname with 65 characters should be blocked");
        validationResults.Should().HaveCount(1);
        validationResults[0].MemberNames.Should().Contain(nameof(AstmCommunicationTcpSettings.HostNameForSeparateExportPort));
    }

    #endregion

    #region ExportResultInOneOrderRecord Tests

    [Fact]
    public void BuildSampleResultMessage_WhenExportResultInOneOrderRecordIsFalse_ShouldCreateOrderResultPairForEachTest()
    {
        // Arrange
        var lisChannel = CreateLisChannelWithExportResultInOneOrderRecord(exportInOneOrder: false);
        var patients = CreateTestPatientsWithMultipleTests();

        // Act
        var message = _messageBuilder.BuildSampleTestResultMessage(lisChannel, patients).GetMessageString();

        // Assert
        message.Should().NotBeNull();

        // Pattern should be: P-O-R-O-R-O-R (Patient, then Order-Result pairs)
        // Count Order records by looking for record type 'O'
        var lines = message.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        var orderRecords = lines.Count(line => line.StartsWith("O|"));
        var resultRecords = lines.Count(line => line.StartsWith("R|"));

        orderRecords.Should().Be(3, "because there should be one order record for each test");
        resultRecords.Should().Be(3, "because there should be one result record for each test");
    }

    [Trait("ProductRequirement", "PR_1305")]
    [Trait("ModuleRequirement", "MR_5629")]
    [Fact]
    public void BuildSampleResultMessage_WhenExportResultInOneOrderRecordIsTrue_ShouldCreateOneOrderRecordFollowedByAllResults()
    {
        // Arrange
        var lisChannel = CreateLisChannelWithExportResultInOneOrderRecord(exportInOneOrder: true);
        var patients = CreateTestPatientsWithMultipleTests();

        // Act
        var message = _messageBuilder.BuildSampleTestResultMessage(lisChannel, patients).GetMessageString();

        // Assert
        message.Should().NotBeNull();

        // Pattern should be: P-O-R-R-R (Patient, one Order, then all Results)
        var lines = message.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        var orderRecords = lines.Count(line => line.StartsWith("O|"));
        var resultRecords = lines.Count(line => line.StartsWith("R|"));

        orderRecords.Should().Be(1, "because there should be only one order record for all tests");
        resultRecords.Should().Be(3, "because there should be one result record for each test");

        // Verify the order record contains the last test's information
        message.Should().Contain("TestC", "the order record should be for the last test");
    }

    private static LisChannel CreateLisChannelWithExportResultInOneOrderRecord(bool exportInOneOrder)
    {
        var astmSettings = new AstmSettings
        {
            Other = new AstmOtherSettings
            {
                PatientNameInTwoFields = false,
                CombineRequestorAddressLinesOneToThree = false
            },
            ExportSettings = new AstmExportSettings
            {
                ExportResultInOneOrderRecord = exportInOneOrder
            }
        };

        return new LisChannel
        {
            AstmSettings = astmSettings
        };
    }

    private static List<LisPatient> CreateTestPatientsWithMultipleTests()
    {
        var testResult1 = new LisSampleTestResult
        {
            TestOrderId = Guid.NewGuid(),
            Concentration = "10.5",
            Pending = false,
            HasError = false,
            MeasurementDateTime = DateTimeOffset.Now
        };

        var testResult2 = new LisSampleTestResult
        {
            TestOrderId = Guid.NewGuid(),
            Concentration = "15.3",
            Pending = false,
            HasError = false,
            MeasurementDateTime = DateTimeOffset.Now
        };

        var testResult3 = new LisSampleTestResult
        {
            TestOrderId = Guid.NewGuid(),
            Concentration = "8.7",
            Pending = false,
            HasError = false,
            MeasurementDateTime = DateTimeOffset.Now
        };

        var testOrder1 = new LisTestOrder
        {
            TestName = "TestA",
            MethodName = "MethodA",
            TestResult = testResult1
        };

        var testOrder2 = new LisTestOrder
        {
            TestName = "TestB",
            MethodName = "MethodB",
            TestResult = testResult2
        };

        var testOrder3 = new LisTestOrder
        {
            TestName = "TestC",
            MethodName = "MethodC",
            TestResult = testResult3
        };

        var sample = new LisSample
        {
            SampleId = "SAMPLE001",
            TestOrders = new List<LisTestOrder> { testOrder1, testOrder2, testOrder3 }
        };

        var patient = new LisPatient
        {
            Name = "John Doe",
            Gender = Gender.Male,
            BirthDate = new DateOnly(1980, 1, 1),
            PatientNumber = "P12345",
            Samples = new List<LisSample> { sample }
        };

        return new List<LisPatient> { patient };
    }

    #endregion

    #region Report Type Tests

    [Trait("ProductRequirement", "PR_1813")]
    [Trait("ModuleRequirement", "MR_5870")]
    [Fact]
    public void BuildSampleResultMessage_WhenTestResultIsFinalReportType()
    {
        // Arrange
        var lisChannel = CreateLisChannelWithSettings(skipPatientInfo: false);
        var patients = CreateTestPatientsWithReportTypeFlags(pending: false, hasError: false);

        // Act
        var message = _messageBuilder.BuildSampleTestResultMessage(lisChannel, patients).GetMessageString();

        // Assert
        message.Should().NotBeNull();
        message.Should().Contain("O|"); // Order record
        message.Should().Contain("|F|", "because Pending=false and HasError=false should map to FinalResult (F)");
    }

    [Trait("ProductRequirement", "PR_1813")]
    [Trait("ModuleRequirement", "MR_5870")]
    [Fact]
    public void BuildSampleResultMessage_WhenTestResultHasError_ShouldOutputErrorReportType()
    {
        // Arrange
        var lisChannel = CreateLisChannelWithSettings(skipPatientInfo: false);
        var patients = CreateTestPatientsWithReportTypeFlags(pending: false, hasError: true);

        // Act
        var message = _messageBuilder.BuildSampleTestResultMessage(lisChannel, patients).GetMessageString();

        // Assert
        message.Should().NotBeNull();
        message.Should().Contain("O|"); // Order record
        message.Should().Contain("|X|", "because HasError=true should map to Error (X)");
    }

    [Trait("ProductRequirement", "PR_1813")]
    [Trait("ModuleRequirement", "MR_5870")]
    [Fact]
    public void BuildSampleResultMessage_WhenTestResultIsPending_ShouldOutputPendingReportType()
    {
        // Arrange
        var lisChannel = CreateLisChannelWithSettings(skipPatientInfo: false);
        var patients = CreateTestPatientsWithReportTypeFlags(pending: true, hasError: false);

        // Act
        var message = _messageBuilder.BuildSampleTestResultMessage(lisChannel, patients).GetMessageString();

        // Assert
        message.Should().NotBeNull();
        message.Should().Contain("O|"); // Order record
        message.Should().Contain("|I", "because Pending=true should map to Pending (I)");
    }

    private static List<LisPatient> CreateTestPatientsWithReportTypeFlags(bool pending, bool hasError)
    {
        var testResult = new LisSampleTestResult
        {
            TestOrderId = Guid.NewGuid(),
            Concentration = hasError ? null : "10.5", // Error results should not have concentration
            Pending = pending,
            HasError = hasError,
            MeasurementDateTime = DateTimeOffset.Now
        };

        var testOrder = new LisTestOrder
        {
            TestName = "Glucose",
            MethodName = "GOD-PAP",
            TestResult = testResult
        };

        var sample = new LisSample
        {
            SampleId = "SAMPLE001",
            TestOrders = new List<LisTestOrder> { testOrder }
        };

        var patient = new LisPatient
        {
            Name = "John Doe",
            Gender = Gender.Male,
            BirthDate = new DateOnly(1980, 1, 1),
            PatientNumber = "P12345",
            Samples = new List<LisSample> { sample }
        };

        return new List<LisPatient> { patient };
    }

    #endregion

    #region Manual Test Status Tests

    [Trait("ProductRequirement", "PR_1813")]
    [Trait("ModuleRequirement", "MR_5870")]
    [Fact]
    public void BuildSampleResultMessage_WhenSampleIsLocallyCreated_ShouldOutputManualTestStatus()
    {
        // Arrange
        var lisChannel = CreateLisChannelWithSettings(skipPatientInfo: false);
        var patients = CreateTestPatientsWithLocallyCreatedSample();

        // Act
        var message = _messageBuilder.BuildSampleTestResultMessage(lisChannel, patients).GetMessageString();

        // Assert
        message.Should().NotBeNull();
        message.Should().Contain("R|"); // Result record
        message.Should().Contain("|M|", "because locally created samples should map to FinalWithManualTests (M)");
    }

    [Trait("ProductRequirement", "PR_1813")]
    [Trait("ModuleRequirement", "MR_5870")]
    [Fact]
    public void BuildSampleResultMessage_WhenSampleIsFromLisAndNotReflex_ShouldOutputFinalStatus()
    {
        // Arrange
        var lisChannel = CreateLisChannelWithSettings(skipPatientInfo: false);
        var patients = CreateTestPatientsFromLis();

        // Act
        var message = _messageBuilder.BuildSampleTestResultMessage(lisChannel, patients).GetMessageString();

        // Assert
        message.Should().NotBeNull();
        message.Should().Contain("R|"); // Result record
        message.Should().Contain("|F|", "because LIS samples with non-reflex tests should map to Final (F)");
        message.Should().NotContain("|M|", "because manual test status should not be used");
    }

    private static List<LisPatient> CreateTestPatientsWithLocallyCreatedSample()
    {
        var testResult = new LisSampleTestResult
        {
            TestOrderId = Guid.NewGuid(),
            Concentration = "10.5",
            Pending = false,
            HasError = false,
            MeasurementDateTime = DateTimeOffset.Now
        };

        var testOrder = new LisTestOrder
        {
            TestName = "Glucose",
            MethodName = "GOD-PAP",
            TestResult = testResult,
        };

        var sample = new LisSample
        {
            SampleId = "SAMPLE001",
            IsSampleLocallyCreated = true, // Sample created locally in Pharos
            TestOrders = new List<LisTestOrder> { testOrder }
        };

        var patient = new LisPatient
        {
            Name = "John Doe",
            Gender = Gender.Male,
            BirthDate = new DateOnly(1980, 1, 1),
            PatientNumber = "P12345",
            Samples = new List<LisSample> { sample }
        };

        return new List<LisPatient> { patient };
    }

    private static List<LisPatient> CreateTestPatientsFromLis()
    {
        var testResult = new LisSampleTestResult
        {
            TestOrderId = Guid.NewGuid(),
            Concentration = "10.5",
            Pending = false,
            HasError = false,
            MeasurementDateTime = DateTimeOffset.Now
        };

        var testOrder = new LisTestOrder
        {
            TestName = "Glucose",
            MethodName = "GOD-PAP",
            TestResult = testResult,
        };

        var sample = new LisSample
        {
            SampleId = "SAMPLE001",
            IsSampleLocallyCreated = false, // Sample from LIS
            TestOrders = new List<LisTestOrder> { testOrder }
        };

        var patient = new LisPatient
        {
            Name = "John Doe",
            Gender = Gender.Male,
            BirthDate = new DateOnly(1980, 1, 1),
            PatientNumber = "P12345",
            Samples = new List<LisSample> { sample }
        };

        return new List<LisPatient> { patient };
    }

    #endregion

    #region Quality Control Result Tests

    [Fact]
    public void BuildQualityControlResultMessage_ShouldIncludeHeaderWithQualityControlProcessingId()
    {
        // Arrange
        var lisChannel = new LisChannel { AstmSettings = new AstmSettings() };
        var qcResult = CreateQualityControlResult();

        // Act
        var message = _messageBuilder.BuildQualityControlTestResultMessage(lisChannel, qcResult).GetMessageString();

        // Assert
        var headerRecord = GetRecord(message, "H|");

        headerRecord.Should().Contain("|Q|", "because QC result messages should use processing id Q");
        message.Should().Contain("P|1", "because QC result message includes one patient record");
        message.Should().Contain("O|1", "because QC result message includes one order record");
        message.Should().Contain("R|1", "because QC result message includes one result record");
        message.Should().Contain("L|1|N");
    }

    [Trait("ProductRequirement", "PR_1813")]
    [Trait("ModuleRequirement", "MR_5870")]
    [Fact]
    public void BuildQualityControlResultMessage_ShouldIncludeQualityControlOrderRecord()
    {
        // Arrange
        var lisChannel = new LisChannel
        {
            AstmSettings = new AstmSettings
            {
                ExportSettings = new AstmExportSettings
                {
                    UseLocalSampleIdAsLisSampleId = true
                }
            }
        };
        var qcResult = CreateQualityControlResult();

        // Act
        var message = _messageBuilder.BuildQualityControlTestResultMessage(lisChannel, qcResult).GetMessageString();

        // Assert
        var orderRecord = GetRecord(message, "O|");

        orderRecord.Should().StartWith("O|1|sIgE Pos^N", "because QC name should be included as specimen id with tube type N");
        orderRecord.Should().Contain("^^^d1^sIgE^1");
        orderRecord.Should().Contain("|Q|", "because order record should use action code Q");
        orderRecord.Should().Contain("|Instrument1|F", "because instrument name and final report type should be included");
    }

    [Trait("ProductRequirement", "PR_1813")]
    [Trait("ModuleRequirement", "MR_5870")]
    [Fact]
    public void BuildQualityControlResultMessage_ShouldIncludeQualityControlResultRecord()
    {
        // Arrange
        var lisChannel = new LisChannel
        {
            AstmSettings = new AstmSettings
            {
                ExportSettings = new AstmExportSettings
                {
                    IncludeUserIdInResultsRecord = true
                }
            }
        };
        var qcResult = CreateQualityControlResult();

        // Act
        var message = _messageBuilder.BuildQualityControlTestResultMessage(lisChannel, qcResult).GetMessageString();

        // Assert
        var resultRecord = GetRecord(message, "R|");

        resultRecord.Should().StartWith("R|1|^^^d1^sIgE^1");
        resultRecord.Should().Contain("|2.32");
        resultRecord.Should().Contain("|kU/L|");
        resultRecord.Should().Contain("|M|", "because result should include final manual result status");
        resultRecord.Should().EndWith("|Instrument1");
    }

    [Trait("ProductRequirement", "PR_1813")]
    [Trait("ModuleRequirement", "MR_5870")]
    [Fact]
    public void BuildQualityControlResultMessage_ShouldIncludetErrorStatus_WhenConcentrationIsMissing()
    {
        // Arrange
        var lisChannel = new LisChannel { AstmSettings = new AstmSettings() };
        var qcResult = CreateQualityControlResult() with { Concentration = null };

        // Act
        var message = _messageBuilder.BuildQualityControlTestResultMessage(lisChannel, qcResult).GetMessageString();

        // Assert
        var orderRecord = GetRecord(message, "O|");
        var resultRecord = GetRecord(message, "R|");

        orderRecord.Should().Contain("|Instrument1|X", "because missing concentration maps order report type to error");
        resultRecord.Should().Contain("|X|", "because missing concentration maps result status to error");
    }

    private static LisQualityControlTestResult CreateQualityControlResult()
    {
        return new LisQualityControlTestResult
        {
            QualityControlName = "sIgE Pos",
            TestName = "d1",
            MethodName = "sIgE",
            InstrumentDilutionFactor = 1,
            PreDilutionFactor = 1,
            Concentration = "2,32",
            Units = "kU/L",
            MeasurementDateTime = DateTimeOffset.Now,
            InstrumentName = "Instrument1",
            Ru = "1000",
            Reagents = new List<LisReagent>
            {
                new() { ReagentType = ArticleType.QualityControlCartridge, LotNumber = "QC-LOT-1" }
            }
        };
    }

    private static string GetRecord(string message, string prefix)
    {
        return message
           .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
           .Single(line => line.StartsWith(prefix, StringComparison.Ordinal));
    }

    #endregion
}
