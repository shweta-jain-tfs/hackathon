using Pharos.Server.Application.Lis;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Infrastructure;
using Pharos.Server.Lis.Astm.Application.Mappings;
using Pharos.Server.Lis.Astm.Application.Messages;

namespace Pharos.Server.Lis.Astm.Tests;

public class AstmPatientRecordExtensionsTests
{
    private readonly Mock<IDateTime> _mockDateTime;

    public AstmPatientRecordExtensionsTests()
    {
        _mockDateTime = new Mock<IDateTime>();
        _mockDateTime.Setup(x => x.Now).Returns(new DateTimeOffset(2024, 6, 15, 10, 0, 0, TimeSpan.Zero));
    }


    [Fact]
    public void CombineCommentFields_WithShortComments_ReturnsCorrectText()
    {
        // Arrange
        var patientRecord = new AstmPatientRecord
        {
            CommentLine1 = "Comment1",
            CommentLine2 = "Comment2",
            CommentLine3 = "Comment3",
            CommentLine4 = "Comment4",
            CommentLine5 = "Comment5"
        };
        var settings = new AstmSettings { Other = new AstmOtherSettings { AllowExtraLongPatientComments = false } };
        var lisImportSettings = new LisImportSettings { CalculatePatientAge = false };
        // Act
        var result = patientRecord.MapToPatient(settings, lisImportSettings);

        // Assert
        result.Comment.Should().Be("Comment1\nComment2\nComment3\nComment4\nComment5");
    }

    [Fact]
    public void AssignCommentFields_WithShortComment_SetsAllLines()
    {
        // Arrange
        var patientRecord = new AstmPatientRecord();
        var patient = new LisPatient { Comment = "Line1\nLine2\nLine3" };
        var settings = new AstmSettings { Other = new AstmOtherSettings { AllowExtraLongPatientComments = false } };

        // Act
        patientRecord.MapFromPatient(patient, "TestRequestId", settings);

        // Assert
        patientRecord.CommentLine1.Should().Be("Line1");
        patientRecord.CommentLine2.Should().Be("Line2");
        patientRecord.CommentLine3.Should().Be("Line3");
        patientRecord.CommentLine4.Should().BeEmpty();
        patientRecord.CommentLine5.Should().BeEmpty();
    }

    [Fact]
    [Trait("ProductRequirement", "PR_570")]
    [Trait("ModuleRequirement", "MR_5388")]
    public void AssignCommentFields_WithLongCommentAndSettingDisabled_TruncatesTo80Characters()
    {
        // Arrange
        var patientRecord = new AstmPatientRecord();
        var longComment = new string('A', 100); // 100 characters
        var patient = new LisPatient { Comment = longComment };
        var settings = new AstmSettings { Other = new AstmOtherSettings { AllowExtraLongPatientComments = false } };

        // Act
        patientRecord.MapFromPatient(patient, "TestRequestId", settings);

        // Assert
        patientRecord.CommentLine1.Length.Should().Be(80);
        patientRecord.CommentLine1.Should().Be(new string('A', 80));
    }

    [Fact]
    [Trait("ProductRequirement", "PR_570")]
    [Trait("ModuleRequirement", "MR_5388")]
    public void AssignCommentFields_WithLongCommentAndSettingEnabled_AllowsUpTo1000Characters()
    {
        // Arrange
        var patientRecord = new AstmPatientRecord();
        var longComment = new string('B', 500); // 500 characters
        var patient = new LisPatient { Comment = longComment };
        var settings = new AstmSettings { Other = new AstmOtherSettings { AllowExtraLongPatientComments = true } };

        // Act
        patientRecord.MapFromPatient(patient, "TestRequestId", settings);

        // Assert
        patientRecord.CommentLine1.Length.Should().Be(500);
        patientRecord.CommentLine1.Should().Be(new string('B', 500));
    }

    [Fact]
    [Trait("ProductRequirement", "PR_570")]
    [Trait("ModuleRequirement", "MR_5388")]
    public void AssignCommentFields_WithVeryLongCommentAndSettingEnabled_TruncatesTo1000Characters()
    {
        // Arrange
        var patientRecord = new AstmPatientRecord();
        var veryLongComment = new string('C', 1200); // 1200 characters
        var patient = new LisPatient { Comment = veryLongComment };
        var settings = new AstmSettings { Other = new AstmOtherSettings { AllowExtraLongPatientComments = true } };

        // Act
        patientRecord.MapFromPatient(patient, "TestRequestId", settings);

        // Assert
        patientRecord.CommentLine1.Length.Should().Be(1000);
        patientRecord.CommentLine1.Should().Be(new string('C', 1000));
    }

    [Fact]
    public void AssignCommentFields_WithNullSettings_DefaultsTo80Characters()
    {
        // Arrange
        var patientRecord = new AstmPatientRecord();
        var longComment = new string('D', 100); // 100 characters
        var patient = new LisPatient { Comment = longComment };
        var settings = new AstmSettings();

        // Act
        patientRecord.MapFromPatient(patient, "TestRequestId", settings);

        // Assert
        patientRecord.CommentLine1.Length.Should().Be(80);
        patientRecord.CommentLine1.Should().Be(new string('D', 80));
    }

    [Fact]
    public void AssignCommentFields_WithMultiLineComment_PreservesLineOrder()
    {
        // Arrange
        var patientRecord = new AstmPatientRecord();
        var multiLineComment = "First line\nSecond line with more text\nThird line";
        var patient = new LisPatient { Comment = multiLineComment };
        var settings = new AstmSettings { Other = new AstmOtherSettings { AllowExtraLongPatientComments = true } };

        // Act
        patientRecord.MapFromPatient(patient, "TestRequestId", settings);

        // Assert
        patientRecord.CommentLine1.Should().Be("First line");
        patientRecord.CommentLine2.Should().Be("Second line with more text");
        patientRecord.CommentLine3.Should().Be("Third line");
        patientRecord.CommentLine4.Should().BeEmpty();
        patientRecord.CommentLine5.Should().BeEmpty();
    }

    [Fact]
    public void CombineCommentFields_BackwardCompatibility_WorksWithExistingData()
    {
        // Arrange
        var patientRecord = new AstmPatientRecord
        {
            CommentLine1 = "Old",
            CommentLine2 = "Format",
            CommentLine3 = "Data",
            CommentLine4 = "",
            CommentLine5 = ""
        };
        var settings = new AstmSettings { Other = new AstmOtherSettings { AllowExtraLongPatientComments = false } };
        var lisImportSettings = new LisImportSettings { CalculatePatientAge = false };
        // Act
        var result = patientRecord.MapToPatient(settings, lisImportSettings);

        // Assert
        result.Comment.Should().Be("Old\nFormat\nData\n\n");
    }



    #region PatientInfo export Include/Skip Tests

    [Trait("ProductRequirement", "PR_1305")]
    [Trait("ModuleRequirement", "MR_5391")]
    [Fact]
    public void MapFromPatient_WhenSkipExportOfPatientInformationIsTrue_ShouldSkipPatientInformation()
    {
        // Arrange
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
            PatientAge = 44
        };

        var messageSettings = new AstmSettings
        {
            Other = new AstmOtherSettings
            {
                PatientNameInTwoFields = false
            },
            ExportSettings = new AstmExportSettings { SkipExportOfPatientInformation = true }
        };

        var patientRecord = new AstmPatientRecord();
        var requestId = "REQ001";

        // Act
        patientRecord.MapFromPatient(patient, requestId, messageSettings);

        // Assert
        patientRecord.LaboratoryPatientId.Should().Be(requestId, "LaboratoryPatientId should always be set for message structure");
        patientRecord.PracticePatientId.Should().Be("PP11111");

        // All patient information fields should be empty/default when skip is enabled
        patientRecord.PatientName1.Should().BeEmpty();
        patientRecord.PatientName2.Should().BeEmpty();
        patientRecord.Gender.Should().Be(Gender.Undefined);
        patientRecord.BirthDate.Should().BeNull();
        patientRecord.PatientNumber.Should().BeEmpty();
        patientRecord.MedicalRecordNumber.Should().BeEmpty();
        patientRecord.AddressLine1.Should().BeEmpty();
        patientRecord.AddressLine2.Should().BeEmpty();
        patientRecord.AddressLine3.Should().BeEmpty();
        patientRecord.AddressLine4.Should().BeEmpty();
        patientRecord.AddressLine5.Should().BeEmpty();
        patientRecord.Diagnosis.Should().BeEmpty();
        patientRecord.CommentLine1.Should().BeEmpty();
        patientRecord.CommentLine2.Should().BeEmpty();
        patientRecord.CommentLine3.Should().BeEmpty();
        patientRecord.CommentLine4.Should().BeEmpty();
        patientRecord.CommentLine5.Should().BeEmpty();
        patientRecord.Age.Should().BeNull();
        patientRecord.Symptoms.Should().BeNull();
    }

    [Fact]
    public void MapFromPatient_WhenSkipExportOfPatientInformationIsFalse_ShouldIncludePatientInformation()
    {
        // Arrange
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
            PatientAge = 44
        };
        patient.Symptoms.Add(new LisSymptom { SymptomType = SymptomType.Asthma });

        var messageSettings = new AstmSettings
        {
            Other = new AstmOtherSettings
            {
                PatientNameInTwoFields = false
            },
            ExportSettings = new AstmExportSettings { SkipExportOfPatientInformation = false }
        };

        var patientRecord = new AstmPatientRecord();
        var requestId = "REQ001";

        // Act
        patientRecord.MapFromPatient(patient, requestId, messageSettings);

        // Assert
        patientRecord.LaboratoryPatientId.Should().Be(requestId);

        // All patient information fields should be populated when skip is disabled
        patientRecord.PatientName1.Should().Be("John Doe");
        patientRecord.Gender.Should().Be(Gender.Male);
        patientRecord.BirthDate.Should().Be(new DateOnly(1980, 1, 1));
        patientRecord.PatientNumber.Should().Be("P12345");
        patientRecord.MedicalRecordNumber.Should().Be("MR67890");
        patientRecord.PracticePatientId.Should().Be("PP11111");
        patientRecord.AddressLine1.Should().Be("123 Main St");
        patientRecord.AddressLine2.Should().Be("Anytown");
        patientRecord.AddressLine3.Should().Be("Anystate");
        patientRecord.AddressLine4.Should().Be("12345");
        patientRecord.AddressLine5.Should().Be("12345");
        patientRecord.Diagnosis.Should().Be("Test Diagnosis");
        patientRecord.CommentLine1.Should().Be("Test Comment");
        patientRecord.Age.Should().Be(44);
        patientRecord.Symptoms.Should().HaveCount(1);
        patientRecord.Symptoms![0].Should().Be(SymptomType.Asthma);
    }

    #endregion

    [Fact]
    [Trait("ProductRequirement", "PR_570")]
    [Trait("ModuleRequirement", "MR_5385")]
    public void MapToPatient_WhenPatientNameInTwoFieldsIsFalseAndPatientName2IsNotEmpty_ReturnsOnlyPatientName1()
    {
        // Arrange
        var patientRecord = new AstmPatientRecord
        {
            PatientName1 = "Mayur",
            PatientName2 = "Mangale"
        };
        var settings = new AstmSettings { Other = new AstmOtherSettings { PatientNameInTwoFields = false } };
        var lisImportSettings = new LisImportSettings { CalculatePatientAge = false };

        // Act
        var result = patientRecord.MapToPatient(settings, lisImportSettings);

        // Assert
        result.Name.Should().Be("Mayur");
    }

    [Fact]
    [Trait("ProductRequirement", "PR_570")]
    [Trait("ModuleRequirement", "MR_5385")]
    public void MapToPatient_WhenPatientNameInTwoFieldsIsFalseAndPatientName2IsNull_ReturnsPatientName1()
    {
        // Arrange
        var patientRecord = new AstmPatientRecord
        {
            PatientName1 = "Mayur",
            PatientName2 = string.Empty
        };
        var settings = new AstmSettings { Other = new AstmOtherSettings { PatientNameInTwoFields = false } };
        var lisImportSettings = new LisImportSettings { CalculatePatientAge = false };

        // Act
        var result = patientRecord.MapToPatient(settings, lisImportSettings);

        // Assert
        result.Name.Should().Be("Mayur");
    }

    [Fact]
    [Trait("ProductRequirement", "PR_570")]
    [Trait("ModuleRequirement", "MR_5385")]
    public void MapToPatient_WhenPatientNameInTwoFieldsIsTrueAndPatientName2IsNotEmpty_CombinesBothNames()
    {
        // Arrange
        var patientRecord = new AstmPatientRecord
        {
            PatientName1 = "Mayur",
            PatientName2 = "Mangale"
        };
        var settings = new AstmSettings { Other = new AstmOtherSettings { PatientNameInTwoFields = true } };
        var lisImportSettings = new LisImportSettings { CalculatePatientAge = false };

        // Act
        var result = patientRecord.MapToPatient(settings, lisImportSettings);

        // Assert
        result.Name.Should().Be("Mayur, Mangale");
    }

    [Fact]
    [Trait("ProductRequirement", "PR_570")]
    [Trait("ModuleRequirement", "MR_5385")]
    public void MapToPatient_WhenPatientNameInTwoFieldsIsTrueAndPatientName2IsNull_ReturnsPatientName1()
    {
        // Arrange
        var patientRecord = new AstmPatientRecord
        {
            PatientName1 = "Mayur",
            PatientName2 = string.Empty
        };
        var settings = new AstmSettings { Other = new AstmOtherSettings { PatientNameInTwoFields = true } };
        var lisImportSettings = new LisImportSettings { CalculatePatientAge = false };

        // Act
        var result = patientRecord.MapToPatient(settings, lisImportSettings);

        // Assert
        result.Name.Should().Be("Mayur");
    }

    [Fact]
    [Trait("ProductRequirement", "PR_570")]
    [Trait("ModuleRequirement", "MR_5385")]
    public void MapFromPatient_WhenPatientNameInTwoFieldsIsFalseAndNameIsSimple_SetsPatientName1Only()
    {
        // Arrange
        var patientRecord = new AstmPatientRecord();
        var patient = new LisPatient { Name = "Mayur Mangale" };
        var settings = new AstmSettings { Other = new AstmOtherSettings { PatientNameInTwoFields = false } };

        // Act
        patientRecord.MapFromPatient(patient, "TestRequestId", settings);

        // Assert
        patientRecord.PatientName1.Should().Be("Mayur Mangale");
        patientRecord.PatientName2.Should().BeEmpty();
    }

    [Fact]
    [Trait("ProductRequirement", "PR_570")]
    [Trait("ModuleRequirement", "MR_5385")]
    public void MapFromPatient_WhenPatientNameInTwoFieldsIsFalseAndNameHasComma_SetsPatientName1WithComma()
    {
        // Arrange
        var patientRecord = new AstmPatientRecord();
        var patient = new LisPatient { Name = "Mayur, Mangale" };
        var settings = new AstmSettings { Other = new AstmOtherSettings { PatientNameInTwoFields = false } };

        // Act
        patientRecord.MapFromPatient(patient, "TestRequestId", settings);

        // Assert
        patientRecord.PatientName1.Should().Be("Mayur, Mangale");
        patientRecord.PatientName2.Should().BeEmpty();
    }

    [Fact]
    [Trait("ProductRequirement", "PR_570")]
    [Trait("ModuleRequirement", "MR_5385")]
    public void MapFromPatient_WhenPatientNameInTwoFieldsIsTrueAndNameHasComma_SplitsNameIntoTwoFields()
    {
        // Arrange
        var patientRecord = new AstmPatientRecord();
        var patient = new LisPatient { Name = "Mayur, Mangale" };
        var settings = new AstmSettings { Other = new AstmOtherSettings { PatientNameInTwoFields = true } };

        // Act
        patientRecord.MapFromPatient(patient, "TestRequestId", settings);

        // Assert
        patientRecord.PatientName1.Should().Be("Mayur");
        patientRecord.PatientName2.Should().Be("Mangale");
    }

    [Fact]
    [Trait("ProductRequirement", "PR_570")]
    [Trait("ModuleRequirement", "MR_5385")]
    public void MapFromPatient_WhenPatientNameInTwoFieldsIsTrueAndNameHasNoComma_SetsPatientName1Only()
    {
        // Arrange
        var patientRecord = new AstmPatientRecord();
        var patient = new LisPatient { Name = "Mayur Mangale" };
        var settings = new AstmSettings { Other = new AstmOtherSettings { PatientNameInTwoFields = true } };

        // Act
        patientRecord.MapFromPatient(patient, "TestRequestId", settings);

        // Assert
        patientRecord.PatientName1.Should().Be("Mayur Mangale");
        patientRecord.PatientName2.Should().BeEmpty();
    }
}
