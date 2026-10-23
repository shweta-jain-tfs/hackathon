using Pharos.Server.Application.Lis;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Lis.Astm.Application.Mappings;
using Pharos.Server.Lis.Astm.Application.Messages;

namespace Pharos.Server.Lis.Astm.Tests;

public class AstmMessageParserTests
{
    [Fact]
    public void ParsingMessageHeaderRecordFromOldSimulator()
    {
        var str = "H|\\^&|||UniCAP Data Manager (UDM)^5.00^4.0|||||LIS^127.0.0.1||P|1|20210120150028";
        var header = AstmHeaderRecord.Parse(str);

        header.SenderName.Should().Be("UniCAP Data Manager (UDM)");
        header.SenderVersion.Should().Be("5.00");
        header.UnifaceVersion.Should().Be("4.0");
        header.ReceiverHostName.Should().Be("LIS");
        header.ReceiverIpAddress.Should().Be("127.0.0.1");
        header.ProcessingId.Should().Be(AstmProcessingId.Production);
        header.DateAndTimeOfMessage.Should().Be(DateTime.Parse("2021-01-20 15:00:28"));
    }

    [Fact]
    public void ParsingMessageTerminatorRecord()
    {
        var str = "L|1|N";
        var terminator = AstmTerminatorRecord.Parse(str, AstmFieldDelimiters.Default);

        terminator.TerminationCode.Should().Be("N");
    }

    [Fact]
    public void ParsingPatientInformationRecord()
    {
        var str = "P|1|PracticePID|LabPID|PatientNumber|PatientName||20210120|G||PatientAddress1^PatientAddress2^PatientAddress3^PatientAddress4^PatientAddress5||||Comment1^Comment2^Comment3^Comment4^Comment5|5|||Diagnos^1\\2\\3\\9|||MEDREC";

        var patient = AstmPatientRecord.Parse(str, AstmFieldDelimiters.Default);

        patient.PracticePatientId.Should().Be("PracticePID");
        patient.LaboratoryPatientId.Should().Be("LabPID");
        patient.PatientNumber.Should().Be("PatientNumber");
        patient.PatientName1.Should().Be("PatientName");
        patient.BirthDate.Should().Be(DateOnly.Parse("2021-01-20"));
        patient.Gender.Should().Be(Gender.Undefined);
        patient.AddressLine1.Should().Be("PatientAddress1");
        patient.AddressLine2.Should().Be("PatientAddress2");
        patient.AddressLine3.Should().Be("PatientAddress3");
        patient.AddressLine4.Should().Be("PatientAddress4");
        patient.AddressLine5.Should().Be("PatientAddress5");
        patient.CommentLine1.Should().Be("Comment1");
        patient.CommentLine2.Should().Be("Comment2");
        patient.CommentLine3.Should().Be("Comment3");
        patient.CommentLine4.Should().Be("Comment4");
        patient.CommentLine5.Should().Be("Comment5");
        patient.Age.Should().Be(5);
        patient.MedicalRecordNumber.Should().Be("MEDREC");
        patient.Diagnosis.Should().Be("Diagnos");
        patient.Symptoms!.Select(s => (short)s).ToArray().Should().Equal(1, 2, 3, 9);
    }

    [Fact]
    public void ParseTestOrderRecord()
    {
        var str = "O|1|^^^0||^^^d^e^2^4^p^6\\a^b^c^d^e^0||20210120150028|20210120150028||||A||0|||RequestorId^RequestorName^RequestorShortcutCode||RequestorAddress^^^^^RequestorComment^0||RequestOrigin||20210120150028|||||01^HOSP^01^SECT^01^WARD";

        var order = AstmTestOrderRecord.Parse(str, AstmFieldDelimiters.Default);

        order.UniversalTestIds[0].TestName.Should().Be("d");
        order.UniversalTestIds[0].LisMethodId.Should().Be("e");
        order.UniversalTestIds[0].InstrumentDilution.Should().Be(2);
        order.UniversalTestIds[0].SuppressReflex.Should().Be(4);
        order.UniversalTestIds[0].ReflexName.Should().Be("p");
        order.UniversalTestIds[0].Replicates.Should().Be(6);
        order.SpecimenId.Should().Be("");
        order.TubeType.Should().Be("N");
        order.RackId.Should().Be("");
        order.RackPosition.Should().Be(0);
        order.RequestedDateAndTime.Should().Be(DateTime.Parse("2021-01-20 15:00:28"));
        order.SpecimenCollectionDateAndTime.Should().Be(DateTime.Parse("2021-01-20 15:00:28"));
        order.CollectionVolume.Should().BeNull();
        order.TestAction.Should().Be(AstmTestAction.AddTest);
        order.Dilution.Should().Be(0);
        order.RequestorId.Should().Be("RequestorId");
        order.RequestorName.Should().Be("RequestorName");
        order.RequestorShortcutCode.Should().Be("RequestorShortcutCode");
        order.RequestorPhone.Should().Be("");
        order.RequestorAddressLine1.Should().Be("RequestorAddress");
        order.RequestorAddressLine2.Should().Be("");
        order.RequestorAddressLine3.Should().Be("");
        order.RequestorAddressLine4.Should().Be("");
        order.RequestorAddressLine5.Should().Be("");
        order.RequestorComment.Should().Be("RequestorComment");
        order.RequestorReflexTesting.Should().Be(0);
        order.LaboratoryFieldNo1.Should().Be("RequestOrigin");
        order.ResultsReportedOrLastModifiedDateAndTime.Should().Be(DateTime.Parse("2021-01-20 15:00:28"));
        order.ReportType.Should().Be(AstmReportType.Undefined);
        order.HospitalCode.Should().Be("01");
        order.HospitalName.Should().Be("HOSP");
        order.SectionCode.Should().Be("01");
        order.SectionName.Should().Be("SECT");
        order.WardCode.Should().Be("01");
        order.WardName.Should().Be("WARD");
    }

    [Fact]
    public void ParseTestOrderRecord_WhenFieldParsingFails_ShouldIgnoreExceptionAndStoreValidationError()
    {
        var str = "O|ABC|^^^X||^^^d^e^2^4^p^6\\a^b^c^d^e^0||INVALID_DATE|20210120150028||||A||INVALID|||RequestorId^RequestorName^RequestorShortcutCode||RequestorAddress^^^^^RequestorComment^0||RequestOrigin||20210120150028|||||01^HOSP^01^SECT^01^WARD";

        var act = () => AstmTestOrderRecord.Parse(str, AstmFieldDelimiters.Default);

        act.Should().NotThrow();

        var order = AstmTestOrderRecord.Parse(str, AstmFieldDelimiters.Default);
        order.SequenceNumber.Should().Be(1);
        order.RackPosition.Should().Be(0);
        order.Dilution.Should().Be(0);
        order.RequestedDateAndTime.Should().BeNull();
        order.ParsingErrors.Should().NotBeEmpty();
    }

    [Fact]
    public void ParsePatientRecord_WhenFieldParsingFails_ShouldIgnoreExceptionAndStoreValidationError()
    {
        var str = "P|1|PracticePID|LabPID|PatientNumber|PatientName||INVALID_DATE|G||PatientAddress1^PatientAddress2^PatientAddress3^PatientAddress4^PatientAddress5||||Comment1^Comment2^Comment3^Comment4^Comment5|ABC|||Diagnos^1\\X\\3\\9|||MEDREC";

        var act = () => AstmPatientRecord.Parse(str, AstmFieldDelimiters.Default);

        act.Should().NotThrow();

        var patient = AstmPatientRecord.Parse(str, AstmFieldDelimiters.Default);
        patient.BirthDate.Should().BeNull();
        patient.Age.Should().BeNull();
        patient.Symptoms.Should().BeNull();
        patient.ParsingErrors.Should().NotBeEmpty();
    }

    [Fact]
    public void ParseHeaderRecord_WhenFieldParsingFails_ShouldIgnoreExceptionAndStoreValidationError()
    {
        var str = "H|\\^&|||UniCAP Data Manager (UDM)^5.00^4.0|||||LIS^127.0.0.1||P|1|INVALID_DATE";

        var act = () => AstmHeaderRecord.Parse(str);

        act.Should().NotThrow();

        var header = AstmHeaderRecord.Parse(str);
        header.DateAndTimeOfMessage.Should().BeNull();
        header.ParsingErrors.Should().NotBeEmpty();
    }

    [Fact]
    public void ParseRequestInformationRecord_WhenFieldParsingFails_ShouldIgnoreExceptionAndStoreValidationError()
    {
        var str = "Q|ABC|INV||||||||||D";

        var act = () => AstmRequestRecord.Parse(str, AstmFieldDelimiters.Default);

        act.Should().NotThrow();

        var requestInfo = AstmRequestRecord.Parse(str, AstmFieldDelimiters.Default);
        requestInfo.SequenceNumber.Should().Be(0);
        requestInfo.ParsingErrors.Should().NotBeEmpty();
    }

    [Fact]
    [Trait("ProductRequirement", "PR_570")]
    [Trait("ModuleRequirement", "MR_5387")]
    public void MapToRequestor_WhenNonUniqueOrderingPhysicianCodeDisabled_UsesRequestorIdAsIs()
    {
        // Arrange
        var record = new AstmTestOrderRecord
        {
            RequestorId = "DOC123",
            RequestorName = "Dr. Smith",
            RequestorPhone = "123-456-7890",
            RequestorComment = "Test comment",
            RequestorShortcutCode = "DS",
            SectionCode = "LAB",
            HospitalCode = "H001"
        };

        var sample = new LisSample();
        var settings = new AstmSettings
        {
            Other = new AstmOtherSettings
            {
                CombineRequestorAddressLinesOneToThree = false,
                NonUniqueOrderingPhysicianCode = false
            },
        };

        // Act
        record.MapToRequestor(sample, settings);

        // Assert
        sample.Requestor.Should().NotBeNull();
        sample.Requestor!.RequestorId.Should().Be("DOC123");
        sample.Requestor.Name.Should().Be("Dr. Smith");
        sample.Requestor.PhoneNumber.Should().Be("123-456-7890");
        sample.Requestor.Comment.Should().Be("Test comment");
        sample.Requestor.ShortcutCode.Should().Be("DS");
        sample.Requestor.ZipCode.Should().Be("LAB");
        sample.Requestor.OrderingPhysicianCode.Should().Be("H001");
    }

    [Fact]
    [Trait("ProductRequirement", "PR_570")]
    [Trait("ModuleRequirement", "MR_5387")]
    public void MapToRequestor_WhenNonUniqueOrderingPhysicianCodeEnabled_AndNameEmpty_UsesRequestorIdOnly()
    {
        // Arrange
        var record = new AstmTestOrderRecord
        {
            RequestorId = "DOC123",
            RequestorName = ""
        };

        var sample = new LisSample();
        var settings = new AstmSettings
        {
            Other = new AstmOtherSettings
            {
                CombineRequestorAddressLinesOneToThree = false,
                NonUniqueOrderingPhysicianCode = true
            }
        };

        // Act
        record.MapToRequestor(sample, settings);

        // Assert
        sample.Requestor.Should().NotBeNull();
        sample.Requestor!.RequestorId.Should().Be("DOC123");
        sample.Requestor.Name.Should().Be("");
    }

    [Fact]
    [Trait("ProductRequirement", "PR_570")]
    [Trait("ModuleRequirement", "MR_5387")]
    public void MapToRequestor_WhenNonUniqueOrderingPhysicianCodeEnabled_AndBothEmpty_ReturnsEmpty()
    {
        // Arrange
        var record = new AstmTestOrderRecord
        {
            RequestorId = "",
            RequestorName = ""
        };

        var sample = new LisSample();
        var settings = new AstmSettings
        {
            Other = new AstmOtherSettings
            {
                CombineRequestorAddressLinesOneToThree = false,
                NonUniqueOrderingPhysicianCode = true
            }
        };

        // Act
        record.MapToRequestor(sample, settings);

        // Assert
        sample.Requestor.Should().NotBeNull();
        sample.Requestor!.RequestorId.Should().Be("");
        sample.Requestor.Name.Should().Be("");
    }


    [Fact]
    public void ParseRequestInformationRecordForInventory()
    {
        var str = "Q|2|INV||||||||||D";

        var requestInfo = AstmRequestRecord.Parse(str, AstmFieldDelimiters.Default);
        requestInfo.SpecimenId.Should().BeNull();
    }

    [Fact]
    public void ParseMessageWithSingleTestOrder()
    {
        var messageStr =
            "H|\\^&|||Host|||||||P|1|20010226150028\r" +
            "P|1|PID001|RID001\r" +
            "O|1|SID001^N^01^5||^^^f1^sIgE^1|||20010226090000||||N||1||||||||||||O\r" +
            "L|1|F";

        var message = (AstmMessage)new AstmMessageParser().ParseMessage(messageStr);

        message.Records.Should().HaveCount(2);

        var header = message.Header;
        message.Header.Should().NotBeNull();
        header!.SenderName.Should().Be("Host");
        header.ProcessingId.Should().Be(AstmProcessingId.Production);
        header.DateAndTimeOfMessage.Should().Be(DateTime.Parse("2001-02-26 15:00:28"));

        message.Records[0].Should().BeOfType<AstmPatientRecord>();
        var patient = (AstmPatientRecord)message.Records[0];
        patient.PracticePatientId.Should().Be("PID001");
        patient.LaboratoryPatientId.Should().Be("RID001");

        message.Records[1].Should().BeOfType<AstmTestOrderRecord>();
        var order = (AstmTestOrderRecord)message.Records[1];

        order.SpecimenId.Should().Be("SID001");
        order.TubeType.Should().Be("N");
        order.RackPosition.Should().Be(5);
        order.RackId.Should().Be("01");

        message.Terminator.Should().NotBeNull();
    }

    [Fact]
    public void ParseDelimiters()
    {
        var str = "H|\\^&";

        var delimiters = AstmFieldDelimiters.Parse(str);

        delimiters.Field.Should().Be('|');
        delimiters.Repeat.Should().Be('\\');
        delimiters.Component.Should().Be('^');
        delimiters.Escape.Should().Be('&');
    }

    [Fact]
    public void DeescapingOfFieldData()
    {
        var message = "H|\\^&|||data &F&&R&&S&&E& escape|";

        var header = AstmHeaderRecord.Parse(message);
        header.SenderName.Should().Be("data |\\^& escape");
    }
}
