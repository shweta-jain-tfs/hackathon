using Pharos.Server.Lis.Application.Interfaces;
using Pharos.Server.Lis.HL7.Application.Common;
using Pharos.Server.Lis.HL7.Application.Interfaces;

namespace Pharos.Server.Lis.HL7.Application.Messages;

internal sealed class HL7MessageParser : ILisMessageParser<IHL7Message>
{
    public IHL7Message ParseMessage(string messageString)
    {
        if (string.IsNullOrWhiteSpace(messageString))
        {
            throw new HL7Exception("HL7 message is empty.");
        }

        var segments = messageString.Split(HL7ControlSymbols.SegmentDelimiter)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        if (segments.Count == 0)
        {
            throw new HL7Exception("HL7 message contains no segments.");
        }

        var headerSegment = segments.First();
        var header = HL7HeaderSegment.Parse(headerSegment);

        var message = new HL7Message { Header = header };

        for (var i = 1; i < segments.Count; i++)
        {
            var segment = ParseSegment(segments[i], header.Delimiters);
            if (segment != null)
            {
                message.Segments.Add(segment);
            }
        }

        return message;
    }

    private static IHL7Segment? ParseSegment(string segmentString, HL7FieldDelimiters delimiters)
    {
        var segmentType = HL7SegmentParser.GetRecordType(segmentString);
        return segmentType switch
        {
            HL7SegmentType.MessageHeader => throw new HL7Exception("Duplicate header segments found in the HL7 message."),
            HL7SegmentType.MessageAcknowledgement => HL7AcknowledgementSegment.Parse(segmentString, delimiters),
            HL7SegmentType.PatientIdentification => HL7PatientIdentificationSegment.Parse(segmentString, delimiters),
            HL7SegmentType.ObservationRequest => HL7ObservationRequestSegment.Parse(segmentString, delimiters),
            HL7SegmentType.AdditionalTestInformation => HL7AdditionalTestInformationSegment.Parse(segmentString, delimiters),
            HL7SegmentType.CommonOrder => HL7CommonOrderSegment.Parse(segmentString, delimiters),
            HL7SegmentType.Specimen => HL7SpecimenSegment.Parse(segmentString, delimiters),
            HL7SegmentType.SpecimenContainer => HL7SpecimenContainerSegment.Parse(segmentString, delimiters),
            HL7SegmentType.Error => HL7ErrorSegment.Parse(segmentString, delimiters),
            HL7SegmentType.Comment => null,
            HL7SegmentType.QueryAcknowledgement => HL7QueryAcknowledgementSegment.Parse(segmentString, delimiters),
            HL7SegmentType.QueryParameterDefinition => HL7QueryParameterDefinitionSegment.Parse(segmentString, delimiters),
            _ => throw new HL7Exception($"Unknown HL7 segment type '{segmentType}'.")
        };
    }
}
