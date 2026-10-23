using Pharos.Server.Application;
using Pharos.Server.Application.Lis;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Infrastructure;
using Pharos.Server.Lis.Astm.Application.Mappings;
using Pharos.Server.Lis.Astm.Application.Messages;
using Pharos.Server.Lis.Domain;

namespace Pharos.Server.Lis.Astm.Application.Services;

public sealed class AstmMessageBuilder(
    IPharosVersionService pharosVersionService,
    IDateTime dateTime) : ILisMessageBuilder
{
    public ILisMessage BuildRequestMessage(LisChannel lisChannel, IReadOnlyCollection<string> sampleIds)
    {
        var astmMessage = CreateAstmMessage(lisChannel, LisMessageType.TestOrderQuery);

        var sequenceNumber = 1;
        if (sampleIds.Count == 0)
        {
            astmMessage.Records.Add(new AstmRequestRecord
            {
                SequenceNumber = sequenceNumber
            }); // get all test orders
        }
        else
        {
            foreach (var sampleId in sampleIds)
            {
                astmMessage.Records.Add(new AstmRequestRecord
                {
                    SequenceNumber = sequenceNumber,
                    SpecimenId = sampleId
                });
                sequenceNumber++;
            }
        }
        return astmMessage;
    }

    public ILisMessage BuildSampleTestResultMessage(LisChannel lisChannel, IReadOnlyList<LisPatient> patients)
    {
        var astmMessage = CreateAstmMessage(lisChannel, LisMessageType.SampleTestResult);

        foreach (var patient in patients)
        {
            if (patient.Samples == null || patient.Samples.Count == 0)
            {
                throw new InvalidOperationException($"Every patient in the test result message must contain at least one sample in the ${nameof(LisPatient)}.{nameof(LisPatient.Samples)} collection.");
            }

            foreach (var sample in patient.Samples)
            {
                var patientRecord = new AstmPatientRecord();
                patientRecord.MapFromPatient(patient, sample.SampleId, lisChannel.AstmSettings!);
                astmMessage.Records.Add(patientRecord);

                AddTestResultRecords(astmMessage, sample, lisChannel.AstmSettings!);
            }
        }
        return astmMessage;
    }

    public ILisMessage BuildQualityControlTestResultMessage(LisChannel lisChannel, LisQualityControlTestResult qcTestResult)
    {
        var astmMessage = CreateAstmMessage(lisChannel, LisMessageType.QualityControlTestResult);

        astmMessage.Records.Add(new AstmPatientRecord());

        var testOrderRecord = new AstmTestOrderRecord();
        testOrderRecord.MapFromQualityControlResult(qcTestResult, lisChannel.AstmSettings!.ExportSettings, dateTime);
        astmMessage.Records.Add(testOrderRecord);

        var testResultRecord = new AstmTestResultRecord();
        testResultRecord.MapFromQualityControlResult(qcTestResult, lisChannel.AstmSettings!.ExportSettings);
        astmMessage.Records.Add(testResultRecord);

        var commentRecords = AstmCommentRecordFactory.CreateFromQualityControlResult(qcTestResult, lisChannel.AstmSettings!.ExportSettings);
        foreach (var commentRecord in commentRecords)
        {
            astmMessage.Records.Add(commentRecord);
        }

        return astmMessage;
    }

    private AstmMessage CreateAstmMessage(LisChannel lisChannel, LisMessageType messageType)
    {
        var settings = lisChannel.AstmSettings!;
        var hostName = "";
        var lisAddress = "";
        if (settings.CommunicationSettings.CommunicationMode == AstmCommunicationMode.Tcp)
        {
            var tcpSettings = settings.CommunicationSettings.TcpSettings;
            lisAddress = tcpSettings.UseSeparateExportPort
                           ? tcpSettings.IpAddressForSeparateExportPort ?? ""
                           : tcpSettings.IpAddress ?? "";
            hostName = tcpSettings.UseSeparateExportPort
                        ? tcpSettings.HostNameForSeparateExportPort ?? ""
                        : tcpSettings.HostName ?? "";
        }

        var processingId = messageType == LisMessageType.QualityControlTestResult
            ? AstmProcessingId.QualityControl
            : AstmProcessingId.Production;

        var astmMessage = new AstmMessage
        {
            Header = new AstmHeaderRecord(AstmFieldDelimiters.Default, "Pharos", pharosVersionService.Version, "4.0", hostName, lisAddress, processingId, dateTime.Now),
            Terminator = AstmTerminatorRecord.Default
        };

        return astmMessage;
    }

    private void AddTestResultRecords(AstmMessage astmMessage, LisSample sample, AstmSettings settings)
    {
        var orderSequenceNumber = 1;
        var resultSequenceNumber = 1;

        if (sample.TestOrders == null || sample.TestOrders.Count == 0)
        {
            throw new InvalidOperationException($"Every sample in the test result message must contain at least one test order in the {nameof(LisSample)}.{nameof(LisSample.TestOrders)} collection.");
        }

        if (sample.TestOrders.Any(x => x.TestResult == null))
        {
            throw new InvalidOperationException("Every test order must include the test result object.");
        }

        if (settings.ExportSettings.ExportResultInOneOrderRecord)
        {
            AddTestOrderRecord(sample.TestOrders.Last(), astmMessage, sample, settings, sequenceNumber: 1);
            foreach (var testOrder in sample.TestOrders)
            {
                AddResultAndCommentRecords(astmMessage, testOrder, sample.IsSampleLocallyCreated, settings.ExportSettings, resultSequenceNumber++);
            }

            return;
        }

        foreach (var testOrder in sample.TestOrders)
        {
            AddTestOrderRecord(testOrder, astmMessage, sample, settings, orderSequenceNumber++);
            AddResultAndCommentRecords(astmMessage, testOrder, sample.IsSampleLocallyCreated, settings.ExportSettings, sequenceNumber: 1);
        }
    }

    private void AddTestOrderRecord(LisTestOrder order, AstmMessage astmMessage, LisSample sample, AstmSettings settings, int sequenceNumber)
    {
        var record = new AstmTestOrderRecord { SequenceNumber = sequenceNumber };
        record.MapFromSampleAndTestOrder(sample, order, settings, dateTime);
        astmMessage.Records.Add(record);
    }

    private static void AddResultAndCommentRecords(AstmMessage astmMessage, LisTestOrder testOrder, bool isLocalSample, AstmExportSettings exportSettings, int sequenceNumber)
    {
        var testResultRecord = new AstmTestResultRecord { SequenceNumber = sequenceNumber };
        testResultRecord.MapFromTestOrderAndResult(testOrder, testOrder.TestResult!, isLocalSample, exportSettings);
        astmMessage.Records.Add(testResultRecord);

        var commentRecords = AstmCommentRecordFactory.CreateFromSampleTestResult(testOrder.TestResult!, exportSettings);
        foreach (var commentRecord in commentRecords)
        {
            astmMessage.Records.Add(commentRecord);
        }
    }

    public ILisMessage? BuildAcknowledgementMessage(LisChannel lisChannel, Dictionary<string, string> messageMetadata, bool success)
    {
        return null;
    }
}
