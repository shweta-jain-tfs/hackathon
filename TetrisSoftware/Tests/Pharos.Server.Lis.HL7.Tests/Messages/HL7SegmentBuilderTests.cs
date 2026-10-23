using FluentAssertions;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Lis.HL7.Application.Messages;
using Xunit;

namespace Pharos.Server.Lis.HL7.Tests.Messages;

public class HL7SegmentBuilderTests
{
    [Fact]
    public void BuildHeaderSegment()
    {
        var segment = new HL7HeaderSegment(HL7FieldDelimiters.Default, "Pharos", new DateTime(2026, 1, 20, 15, 0, 28),
            HL7MessageType.Qbp, HL7MessageType.Zos, "b1b4b3c50d70c8e9d4", HL7ProcessingId.Production);

        var result = segment.ToString();

        result.Should().Be("MSH|^~\\&|Pharos||||20260120150028||QBP^ZOS|b1b4b3c50d70c8e9d4|P|2.5|||AL");
    }

    [Theory]
    [InlineData("SAMPLE001")]
    [InlineData("")]
    [InlineData(null)]
    public void BuildQueryParameterDefinitionSegment(string? sampleId)
    {
        var segment = new HL7QueryParameterDefinitionSegment("b1b4b3c50d70c8e9d4", sampleId);

        var result = segment.ToSegmentString(HL7FieldDelimiters.Default);

        if (string.IsNullOrEmpty(sampleId))
        {
            result.Should().Be("QPD|ZOS^Lab Order Specimen Query|b1b4b3c50d70c8e9d4|ALL");
        }
        else
        {
            result.Should().Be($"QPD|ZOS^Lab Order Specimen Query|b1b4b3c50d70c8e9d4|{sampleId}");
        }
    }

    [Fact]
    public void BuildResponseControlParameterSegment()
    {
        var segment = new HL7ResponseControlParameterSegment();

        var result = segment.ToSegmentString(HL7FieldDelimiters.Default);

        result.Should().Be("RCP|I||R");
    }

    [Fact]
    public void BuildQueryAcknowledgementSegment()
    {
        var segment = new HL7QueryAcknowledgementSegment
        {
            QueryTag = "b1b4b3c50d70c8e9d4",
            QueryResponseStatus = HL7QueryResponseStatus.DataFound,
            HitCount = 1
        };

        var result = segment.ToSegmentString(HL7FieldDelimiters.Default);

        result.Should().Be("QAK|b1b4b3c50d70c8e9d4|OK|ZOS^Lab Order Specimen Query|1");
    }

    [Fact]
    public void BuildAcknowledgementSegment()
    {
        var segment = new HL7AcknowledgementSegment(HL7AcknowledgementCode.MessageAccept, "20110412165020");

        var result = segment.ToSegmentString(HL7FieldDelimiters.Default);

        result.Should().Be("MSA|AA|20110412165020");
    }

    [Fact]
    public void BuildPatientIdentificationSegment()
    {
        var segment = new HL7PatientIdentificationSegment
        {
            SequenceNumber = 1,
            PatientId = "PatientId1",
            PatientListId = "PatientId1",
            AlternatePatientId = "PNR12345",
            FirstName = "FirstName",
            LastName = "LastName",
            BirthDate = new DateOnly(1980, 1, 1),
            Gender = Gender.Undefined,
            StreetAddress = "Street",
            City = "City",
            State = "State",
            ZipCode = "12345",
            Country = "Country"
        };

        var result = segment.ToSegmentString(HL7FieldDelimiters.Default);

        result.Should().Be("PID|1|PatientId1|PatientId1|PNR12345|LastName^FirstName||19800101|U|||Street^^City^State^12345^Country");
    }

    [Fact]
    public void BuildSpecimenSegment()
    {
        var segment = new HL7SpecimenSegment
        {
            SequenceNumber = 1,
            SpecimenType = HL7SpecimenType.Serum,
            ContainerType = "N"
        };

        var result = segment.ToSegmentString(HL7FieldDelimiters.Default);

        result.Should().Be("SPM|1|||SER|||||||||||||||||||||||N");
    }

    [Trait("ProductRequirement", "PR_1785")]
    [Fact]
    public void BuildSpecimenSegment_ShouldUseSequenceNumberAsSetId()
    {
        var segment = new HL7SpecimenSegment
        {
            SequenceNumber = 2,
            SpecimenType = HL7SpecimenType.Serum,
            ContainerType = "N"
        };

        var fields = segment.ToSegmentString(HL7FieldDelimiters.Default).Split('|');

        fields[1].Should().Be("2");
    }

    [Trait("ProductRequirement", "PR_1785")]
    [Theory]
    [InlineData(false, "SER")]
    [InlineData(true, "QC3")]
    public void BuildSpecimenSegment_ShouldUseSupportedSpecimenType(bool qualityControl, string expectedSpecimenType)
    {
        var segment = new HL7SpecimenSegment
        {
            SequenceNumber = 1,
            SpecimenType = qualityControl ? HL7SpecimenType.QualityControl : HL7SpecimenType.Serum,
            ContainerType = "N"
        };

        var fields = segment.ToSegmentString(HL7FieldDelimiters.Default).Split('|');

        fields[4].Should().Be(expectedSpecimenType);
    }

    [Trait("ProductRequirement", "PR_1785")]
    [Theory]
    [InlineData("N")]
    [InlineData("C")]
    [InlineData("3")]
    [InlineData("4")]
    [InlineData("5")]
    public void BuildSpecimenSegment_ShouldUseSupportedContainerType(string containerType)
    {
        var segment = new HL7SpecimenSegment
        {
            SequenceNumber = 1,
            SpecimenType = HL7SpecimenType.Serum,
            ContainerType = containerType
        };

        var fields = segment.ToSegmentString(HL7FieldDelimiters.Default).Split('|');

        fields[27].Should().Be(containerType);
    }

    [Trait("ProductRequirement", "PR_1785")]
    [Fact]
    public void BuildSpecimenSegment_TrayPositionIsEmptyWhenZero()
    {
        var segment = new HL7SpecimenContainerSegment("SAMPLE001", "", 0);

        var result = segment.ToSegmentString(HL7FieldDelimiters.Default);

        result.Should().Be("SAC|||SAMPLE001");
    }

    [Fact]
    public void BuildSpecimenContainerSegment()
    {
        var segment = new HL7SpecimenContainerSegment("SAMPLE001", "TRAY01", 5);

        var result = segment.ToSegmentString(HL7FieldDelimiters.Default);

        result.Should().Be("SAC|||SAMPLE001||||||||||TRAY01|5");
    }

    [Trait("ProductRequirement", "PR_1785")]
    [Fact]
    public void BuildSpecimenContainerSegment_ShouldUseSampleIdAsContainerIdentifier()
    {
        var segment = new HL7SpecimenContainerSegment("MAINFRAME-SAMPLE-001", "TRAY01", 5);

        var fields = segment.ToSegmentString(HL7FieldDelimiters.Default).Split('|');

        fields[3].Should().Be("MAINFRAME-SAMPLE-001");
    }

    [Trait("ProductRequirement", "PR_1785")]
    [Fact]
    public void BuildSpecimenContainerSegment_ShouldUseTrayIdAsTrayIdentifier()
    {
        var segment = new HL7SpecimenContainerSegment("SAMPLE001", "TRAY-42", 5);

        var fields = segment.ToSegmentString(HL7FieldDelimiters.Default).Split('|');

        fields[13].Should().Be("TRAY-42");
    }

    [Trait("ProductRequirement", "PR_1785")]
    [Fact]
    public void BuildSpecimenContainerSegment_ShouldUseTubePositionAsPositionInTray()
    {
        var segment = new HL7SpecimenContainerSegment("SAMPLE001", "TRAY01", 42);

        var fields = segment.ToSegmentString(HL7FieldDelimiters.Default).Split('|');

        fields[14].Should().Be("42");
    }

    [Trait("ProductRequirement", "PR_1785")]
    [Fact]
    public void BuildCommentSegment_WithGenericCommentType_ShouldUseGenericNteCommentType()
    {
        var segment = new HL7CommentSegment
        {
            SequenceNumber = 2,
            CommentSource = HL7CommentSource.Prime,
            Comment = "Comment text",
            CommentType = HL7CommentType.Generic
        };

        var result = segment.ToSegmentString(HL7FieldDelimiters.Default);

        result.Should().Be("NTE|2|O|Comment text|G");
    }

    [Trait("ProductRequirement", "PR_1785")]
    [Fact]
    public void BuildCommentSegment_WithAutomaticCommentType_ShouldUseAutomaticNteCommentType()
    {
        var segment = new HL7CommentSegment
        {
            SequenceNumber = 2,
            CommentSource = HL7CommentSource.Prime,
            Comment = "Comment text",
            CommentType = HL7CommentType.Automatic
        };

        var result = segment.ToSegmentString(HL7FieldDelimiters.Default);

        result.Should().Be("NTE|2|O|Comment text|I");
    }
}
