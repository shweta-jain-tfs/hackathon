using Pharos.Server.Application.Lis;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Infrastructure;
using Pharos.Server.Lis.Application.Interfaces;
using Pharos.Server.Lis.Domain;
using Pharos.Server.Lis.HL7.Application.Mappings;
using Pharos.Server.Lis.HL7.Application.Messages;

namespace Pharos.Server.Lis.HL7.Application.Services;

internal sealed class HL7MessageBuilder(IDateTime dateTime) : ILisMessageBuilder
{
    private const int _messageControlIdLength = 18;
    private const string _applicationName = "Pharos";

    public ILisMessage BuildRequestMessage(LisChannel lisChannel, IReadOnlyCollection<string> sampleIds)
    {
        if (sampleIds.Count > 1)
        {
            throw new NotSupportedException("HL7 does not support generating request messages for multiple sample IDs in a single message.");
        }

        var sampleId = sampleIds.FirstOrDefault();
        var messageControlId = CreateMessageControlId();

        var hl7Message = new HL7Message
        {
            Header = CreateHeader(HL7MessageType.Qbp, HL7MessageType.Zos, messageControlId)
        };

        hl7Message.Segments.Add(new HL7QueryParameterDefinitionSegment(messageControlId, sampleId));
        hl7Message.Segments.Add(new HL7ResponseControlParameterSegment());

        return hl7Message;
    }

    public ILisMessage BuildSampleTestResultMessage(LisChannel lisChannel, IReadOnlyList<LisPatient> patients)
    {
        var messageControlId = CreateMessageControlId();
        var settings = lisChannel.HL7Settings!;
        var exportSettings = settings.ExportSettings;
        var forceUniqueLaboratoryTestNames = lisChannel.ImportSettings.ForceUniqueLaboratoryTestNames;
        var (messageCode, triggerEvent) = ResolveResultMessageType(exportSettings.ResultFormat);
        var includeSpecimen = exportSettings.ResultFormat == HL7ExportResultFormat.OUL;

        var hl7Message = new HL7Message
        {
            Header = CreateHeader(messageCode, triggerEvent, messageControlId)
        };

        foreach (var patient in patients)
        {
            if (patient.Samples == null || patient.Samples.Count == 0)
            {
                throw new InvalidOperationException($"Every patient in the test result message must contain at least one sample in the ${nameof(LisPatient)}.{nameof(LisPatient.Samples)} collection.");
            }

            var patientSegment = new HL7PatientIdentificationSegment();
            patientSegment.MapFromPatient(patient, exportSettings);
            hl7Message.Segments.Add(patientSegment);

            foreach (var commentSegment in HL7CommentSegmentFactory.CreateFromPatient(patient, settings.Other.AllowExtraLongPatientComments))
            {
                hl7Message.Segments.Add(commentSegment);
            }

            foreach (var sample in patient.Samples)
            {
                AddSampleSegments(hl7Message, sample, exportSettings, includeSpecimen, forceUniqueLaboratoryTestNames);
            }
        }

        return hl7Message;
    }

    public ILisMessage BuildQualityControlTestResultMessage(LisChannel lisChannel, LisQualityControlTestResult qcTestResult)
    {
        var messageControlId = CreateMessageControlId();
        var exportSettings = lisChannel.HL7Settings!.ExportSettings;
        var forceUniqueLaboratoryTestNames = lisChannel.ImportSettings.ForceUniqueLaboratoryTestNames;
        var (messageCode, triggerEvent) = ResolveResultMessageType(exportSettings.ResultFormat);
        var includeSpecimen = exportSettings.ResultFormat == HL7ExportResultFormat.OUL;

        var hl7Message = new HL7Message
        {
            Header = CreateHeader(messageCode, triggerEvent, messageControlId)
        };

        hl7Message.Segments.Add(new HL7PatientIdentificationSegment());
        hl7Message.Segments.Add(new HL7CommonOrderSegment());

        if (includeSpecimen)
        {
            hl7Message.Segments.Add(new HL7SpecimenSegment
            {
                SequenceNumber = 1,
                SpecimenType = HL7SpecimenType.QualityControl
            });
            hl7Message.Segments.Add(new HL7SpecimenContainerSegment(qcTestResult.QualityControlName, "", 0));
        }

        var observationRequestSegment = new HL7ObservationRequestSegment { SequenceNumber = 1 };
        observationRequestSegment.MapFromQualityControlResult(qcTestResult, forceUniqueLaboratoryTestNames);
        hl7Message.Segments.Add(observationRequestSegment);

        hl7Message.Segments.Add(HL7AdditionalTestInformationSegmentFactory.CreateFromQualityControlResult(qcTestResult));

        var commentSegments = HL7CommentSegmentFactory.CreateFromQualityControlTestResult(qcTestResult, exportSettings);
        foreach (var commentSegment in commentSegments)
        {
            hl7Message.Segments.Add(commentSegment);
        }

        return hl7Message;
    }

    public ILisMessage BuildAcknowledgementMessage(LisChannel lisChannel, Dictionary<string, string> messageMetadata, bool success)
    {
        messageMetadata.TryGetValue(LisMessageMetadataKeys.TriggerEvent, out var triggerEvent);
        messageMetadata.TryGetValue(LisMessageMetadataKeys.MessageControlId, out var messageControlId);
        messageMetadata.TryGetValue(LisMessageMetadataKeys.ProcessingId, out var processingId);
        if (!Enum.TryParse(processingId, out HL7ProcessingId hL7ProcessingId))
        {
            hL7ProcessingId = HL7ProcessingId.Production;
        }

        var messageAcknowledgementCode = success ? HL7AcknowledgementCode.MessageAccept : HL7AcknowledgementCode.MessageError;

        var acknowledgement = new HL7Message
        {
            Header = CreateHeader(HL7MessageType.Ack, triggerEvent ?? string.Empty, messageControlId ?? string.Empty, hL7ProcessingId)
        };

        acknowledgement.Segments.Add(new HL7AcknowledgementSegment(messageAcknowledgementCode, messageControlId ?? string.Empty));

        if (!success)
        {
            acknowledgement.Segments.Add(new HL7ErrorSegment("ERROR"));
        }
        return acknowledgement;
    }

    private static (string MessageCode, string TriggerEvent) ResolveResultMessageType(HL7ExportResultFormat resultFormat)
    {
        return resultFormat switch
        {
            HL7ExportResultFormat.ORM => (HL7MessageType.Orm, HL7MessageType.O01),
            HL7ExportResultFormat.ORU => (HL7MessageType.Oru, HL7MessageType.R01),
            _ => (HL7MessageType.Oul, HL7MessageType.R23)
        };
    }

    private static void AddSampleSegments(HL7Message message, LisSample sample, HL7ExportSettings exportSettings, bool includeSpecimen, bool forceUniqueLaboratoryTestNames)
    {
        if (sample.TestOrders == null || sample.TestOrders.Count == 0)
        {
            throw new InvalidOperationException($"Every sample in the test result message must contain at least one test order in the {nameof(LisSample)}.{nameof(LisSample.TestOrders)} collection.");
        }

        if (sample.TestOrders.Any(x => x.TestResult == null))
        {
            throw new InvalidOperationException("Every test order must include the test result object.");
        }

        var commonOrderSegment = new HL7CommonOrderSegment();
        commonOrderSegment.MapFromSample(sample, exportSettings);
        message.Segments.Add(commonOrderSegment);

        if (includeSpecimen)
        {
            var specimenSegment = new HL7SpecimenSegment();
            specimenSegment.MapFromSample(sample, exportSettings);
            message.Segments.Add(specimenSegment);
            message.Segments.Add(HL7SpecimenContainerSegmentFactory.CreateFromSample(sample, exportSettings));
        }

        if (exportSettings.ExportResultInOneOBR)
        {
            message.Segments.Add(CreateObservationRequestSegment(sample, sample.TestOrders.Last(), exportSettings, sequenceNumber: 1, forceUniqueLaboratoryTestNames));

            var obxSequenceNumber = 1;
            foreach (var testOrder in sample.TestOrders)
            {
                AddResultAndCommentSegments(message, testOrder, exportSettings, forceUniqueLaboratoryTestNames, sample.IsSampleLocallyCreated, ref obxSequenceNumber);
            }

            return;
        }

        var obrSequenceNumber = 1;
        foreach (var testOrder in sample.TestOrders)
        {
            message.Segments.Add(CreateObservationRequestSegment(sample, testOrder, exportSettings, obrSequenceNumber++, forceUniqueLaboratoryTestNames));
            var obxSequenceNumber = 1;
            AddResultAndCommentSegments(message, testOrder, exportSettings, forceUniqueLaboratoryTestNames, sample.IsSampleLocallyCreated, ref obxSequenceNumber);
        }
    }

    private static HL7ObservationRequestSegment CreateObservationRequestSegment(
        LisSample sample,
        LisTestOrder testOrder,
        HL7ExportSettings exportSettings,
        int sequenceNumber,
        bool forceUniqueLaboratoryTestNames)
    {
        var segment = new HL7ObservationRequestSegment { SequenceNumber = sequenceNumber };
        segment.MapFromSampleAndTestOrder(sample, testOrder, exportSettings, forceUniqueLaboratoryTestNames);
        return segment;
    }

    private static void AddResultAndCommentSegments(
        HL7Message message,
        LisTestOrder testOrder,
        HL7ExportSettings exportSettings,
        bool forceUniqueLaboratoryTestNames,
        bool isSampleLocallyCreated,
        ref int obxSequenceNumber)
    {
        foreach (var obxSegment in HL7AdditionalTestInformationSegmentFactory.CreateFromTestOrderAndResult(testOrder, obxSequenceNumber, forceUniqueLaboratoryTestNames, isSampleLocallyCreated))
        {
            message.Segments.Add(obxSegment);
            obxSequenceNumber++;
        }

        foreach (var commentSegment in HL7CommentSegmentFactory.CreateFromSampleTestResult(testOrder.TestResult!, exportSettings))
        {
            message.Segments.Add(commentSegment);
        }
    }

    private HL7HeaderSegment CreateHeader(
        string messageCode,
        string triggerEvent,
        string messageControlId,
        HL7ProcessingId processingId = HL7ProcessingId.Production)
    {
        return new HL7HeaderSegment(
            HL7FieldDelimiters.Default,
            _applicationName,
            dateTime.Now,
            messageCode,
            triggerEvent,
            messageControlId,
            processingId);
    }

    private static string CreateMessageControlId()
    {
        return Guid.NewGuid().ToString("N")[.._messageControlIdLength];
    }
}