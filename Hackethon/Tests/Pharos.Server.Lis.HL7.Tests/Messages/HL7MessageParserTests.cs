using FluentAssertions;
using System.Linq.Expressions;
using Moq;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Lis.HL7.Application.Mappings;
using Pharos.Server.Lis.HL7.Application.Messages;
using Xunit;

namespace Pharos.Server.Lis.HL7.Tests.Messages;

public class HL7MessageParserTests
{
    [Fact]
    public void ParseHeaderSegment()
    {
        var str = "MSH|^&~\\|LIS||||20110116124500||ORM^O01|20110412165020|P|2.5|||AL";

        var header = HL7HeaderSegment.Parse(str);

        header.SendingApplication.Should().Be("LIS");
        header.DateAndTimeOfMessage.Should().Be(DateTime.Parse("2011-01-16 12:45:00"));
        header.MessageType1.Should().Be("ORM");
        header.MessageType2.Should().Be("O01");
        header.MessageControlId.Should().Be("20110412165020");
        header.ProcessingId.Should().Be(HL7ProcessingId.Production);
    }

    [Theory]
    [InlineData("AA", HL7AcknowledgementCode.MessageAccept)]
    [InlineData("AE", HL7AcknowledgementCode.MessageError)]
    [InlineData("AR", HL7AcknowledgementCode.MessageReject)]
    public void ParseAcknowledgement_WithSupportedCode_ShouldPreserveCodeAndMessageControlId(
        string acknowledgementCode,
        HL7AcknowledgementCode expectedAcknowledgementCode)
    {
        // Use a 20-character Message Control ID to verify the maximum supported field length.
        var expectedMessageControlId = new string('M', 20);
        var messageString =
            "MSH|^~\\&|LIS||||20110116124500||ACK^R23|ACK1|P|2.5|||AL\r" +
            $"MSA|{acknowledgementCode}|{expectedMessageControlId}";

        var message = (HL7Message)new HL7MessageParser().ParseMessage(messageString);
        var acknowledgement = message.Segments.OfType<HL7AcknowledgementSegment>().Single();

        // Verify that Pharos preserves both supported MSA fields received from the LIS host.
        acknowledgement.AcknowledgementCode.Should().Be(expectedAcknowledgementCode);
        acknowledgement.MessageControlId.Should().Be(expectedMessageControlId);
        acknowledgement.MessageControlId.Should().HaveLength(20);
    }

    [Fact]
    public void ParseAcknowledgement_With80CharacterErrorMessage_ShouldPreserveErrorMessage()
    {
        // Create an ERR-1 value at the maximum length defined by the LIS-HL7 requirement.
        var expectedErrorMessage = new string('E', 80);
        var messageString =
            "MSH|^~\\&|LIS||||20110116124500||ACK^R23|ACK1|P|2.5|||AL\r" +
            "MSA|AE|MESSAGE1\r" +
            $"ERR|{expectedErrorMessage}";

        var message = (HL7Message)new HL7MessageParser().ParseMessage(messageString);
        var errorSegment = message.Segments.OfType<HL7ErrorSegment>().Single();

        // Verify that Pharos recognizes ERR-1 and preserves all 80 received characters.
        errorSegment.ErrorMessage.Should().Be(expectedErrorMessage);
        errorSegment.ErrorMessage.Should().HaveLength(80);
    }

    [Fact]
    public void ParseQueryAcknowledgementSegment()
    {
        var str = "QAK|QUERY123|OK|ZOS^Lab Order Specimen Query|1";

        var queryAcknowledgement = HL7QueryAcknowledgementSegment.Parse(str, HL7FieldDelimiters.Default);

        queryAcknowledgement.QueryTag.Should().Be("QUERY123");
        queryAcknowledgement.QueryResponseStatus.ToString().Should().Be("DataFound");
        queryAcknowledgement.MessageQueryNameIdentifier.Should().Be("ZOS");
        queryAcknowledgement.MessageQueryNameText.Should().Be("Lab Order Specimen Query");
        queryAcknowledgement.HitCount.Should().Be(1);
    }

    [Fact]
    public void ParsePatientIdentificationSegment()
    {
        var str = "PID|1|PID1||12345|Doe^John||19610207|M|||Street^^City^State^ZIP^Country";

        var patient = HL7PatientIdentificationSegment.Parse(str, HL7FieldDelimiters.Default);

        patient.SequenceNumber.Should().Be(1);
        patient.PatientId.Should().Be("PID1");
        patient.PatientListId.Should().Be("");
        patient.AlternatePatientId.Should().Be("12345");
        patient.LastName.Should().Be("Doe");
        patient.FirstName.Should().Be("John");
        patient.MiddleName.Should().Be("");
        patient.BirthDate.Should().Be(new DateOnly(1961, 2, 7));
        patient.Gender.Should().Be(Gender.Male);
        patient.StreetAddress.Should().Be("Street");
        patient.OtherAddress.Should().Be("");
        patient.City.Should().Be("City");
        patient.State.Should().Be("State");
        patient.ZipCode.Should().Be("ZIP");
        patient.Country.Should().Be("Country");
    }

    [Fact]
    public void ParseCommonOrderSegment()
    {
        var str = "ORC|NW|HL7RID1|||||A||20110116161600|||REQID1^Last Name^First Name^Middle|||||||||||12345|Street^^City^State^ZIP^Country";

        var commonOrder = HL7CommonOrderSegment.Parse(str, HL7FieldDelimiters.Default);

        commonOrder.OrderControl.Should().Be(HL7OrderControl.New);
        commonOrder.PlacerOrderNumber.Should().Be("HL7RID1");
        commonOrder.TransactionDateTime.Should().Be(DateTimeOffset.Parse("2011-01-16 16:16:00"));
        commonOrder.RequestorId.Should().Be("REQID1");
        commonOrder.RequestorLastName.Should().Be("Last Name");
        commonOrder.RequestorFirstName.Should().Be("First Name");
        commonOrder.RequestorMiddleName.Should().Be("Middle");
        commonOrder.RequestorPhone.Should().Be("12345");
        commonOrder.RequestorStreetAddress.Should().Be("Street");
        commonOrder.RequestorOtherAddress.Should().Be("");
        commonOrder.RequestorCity.Should().Be("City");
        commonOrder.RequestorState.Should().Be("State");
        commonOrder.RequestorZipCode.Should().Be("ZIP");
        commonOrder.RequestorCountry.Should().Be("Country");
    }

    [Fact]
    public void ParseSpecimenSegment()
    {
        var str = "SPM|1|||SER";

        var specimen = HL7SpecimenSegment.Parse(str, HL7FieldDelimiters.Default);

        specimen.SequenceNumber.Should().Be(1);
        specimen.SpecimenType.Should().Be(HL7SpecimenType.Serum);
        specimen.ContainerType.Should().Be("");
    }

    [Fact]
    public void ParseSpecimenContainerSegment()
    {
        var str = "SAC|||SP_HL7_7036||||||||||TRAYID|2";

        var specimen = HL7SpecimenContainerSegment.Parse(str, HL7FieldDelimiters.Default);

        specimen.SampleId.Should().Be("SP_HL7_7036");
        specimen.TrayId.Should().Be("TRAYID");
        specimen.TrayPosition.Should().Be(2);
    }

    [Theory]
    [InlineData("N", "SER", "New", "Serum")]
    [InlineData("A", "QC3", "Add", "QualityControl")]
    public void ParseObservationRequestSegment(
        string specimenActionCode,
        string specimenSourceCode,
        string expectedSpecimenActionCode,
        string expectedSpecimenSourceCode)
    {
        var str = $"OBR|1|HL7RID1||sIgE-f1|||20110116154433||||{specimenActionCode}||||{specimenSourceCode}|";

        var observation = HL7ObservationRequestSegment.Parse(str, HL7FieldDelimiters.Default);

        observation.SequenceNumber.Should().Be(1);
        observation.PlacerOrderNumber.Should().Be("HL7RID1");
        observation.UniversalServiceIdentifier.Should().Be("sIgE-f1");
        observation.ObservationDateTime.Should().Be(DateTimeOffset.Parse("2011-01-16 15:44:33"));
        observation.SpecimenActionCode.ToString().Should().Be(expectedSpecimenActionCode);
        observation.SpecimenSourceCode.ToString().Should().Be(expectedSpecimenSourceCode);
    }

    [Theory]
    [InlineData(true, "f1", "", "f1")]
    [InlineData(true, "a-ECP", "", "a-ECP")]
    [InlineData(true, "ECP-a-ECP", "ECP", "a-ECP")]
    [InlineData(false, "ECP-a-ECP", "ECP", "a-ECP")]
    [InlineData(false, "EI-M-Mt-w-1R", "EI-M", "Mt-w-1R")]
    public void ParseObservationRequestSegment_ShouldKeepIdentifierAndResolveMethodAndTestNames(
        bool forceUniqueLaboratoryTestNames,
        string identifier,
        string expectedMethodName,
        string expectedTestName)
    {
        // Arrange
        var methodsService = new Mock<IMethodsService>();
        if (!string.IsNullOrEmpty(expectedMethodName))
        {
            methodsService
                .Setup(x => x.GetMethod(expectedMethodName, It.IsAny<Expression<Func<Method, string>>>()))
                .ReturnsAsync(expectedMethodName);
        }
        var str = $"OBR|1|HL7RID1||{identifier}|||20110116154433||||N||||SER|";

        // Act
        var observation = HL7ObservationRequestSegment.Parse(str, HL7FieldDelimiters.Default);
        var testOrder = observation.MapToTestOrder(
            new LisImportSettings { ForceUniqueLaboratoryTestNames = forceUniqueLaboratoryTestNames },
            methodsService.Object);

        // Assert
        observation.UniversalServiceIdentifier.Should().Be(identifier);
        testOrder.MethodName.Should().Be(expectedMethodName);
        testOrder.TestName.Should().Be(expectedTestName);
        testOrder.ForceUniqueLaboratoryTestNames.Should().Be(forceUniqueLaboratoryTestNames);
    }

    [Fact]
    public void ParseAdditionalTestInformationSegment()
    {
        var str = "OBX|1|CE|SY^Symptom||1";

        var observation = HL7AdditionalTestInformationSegment.Parse(str, HL7FieldDelimiters.Default);

        observation.SequenceNumber.Should().Be(1);
        observation.ObservationId.Should().Be("SY");
        observation.ObservationText.Should().Be("Symptom");
        observation.ObservationCodingSystem.Should().Be("");
        observation.ObservationValue.Should().Be("1");
        observation.SampleId.Should().Be("");
        observation.Symptoms.Should().NotBeNullOrEmpty();
        observation.Symptoms.Select(s => (short)s).ToArray().Should().Equal(1);
    }
}
