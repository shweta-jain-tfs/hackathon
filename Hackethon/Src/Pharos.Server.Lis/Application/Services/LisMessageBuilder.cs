using Microsoft.Extensions.DependencyInjection;
using Pharos.Server.Application.Lis;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Lis.Application.Interfaces;

namespace Pharos.Server.Lis.Application.Services;

internal class LisMessageBuilder(
    [FromKeyedServices(LisServiceKeys.AstmMessageBuilder)] ILisMessageBuilder astmMessageBuilder,
    [FromKeyedServices(LisServiceKeys.HL7MessageBuilder)] ILisMessageBuilder hl7MessageBuilder)
    : ILisMessageBuilder
{

    /// <summary>
    /// Builds protocol-specific message for requesting test orders from LIS for a specified list of samples.
    /// </summary>
    public ILisMessage BuildRequestMessage(LisChannel lisChannel, IReadOnlyCollection<string> sampleIds)
    {
        return lisChannel.ProtocolType switch
        {
            LisProtocolType.Astm => astmMessageBuilder.BuildRequestMessage(lisChannel, sampleIds),
            LisProtocolType.HL7 => hl7MessageBuilder.BuildRequestMessage(lisChannel, sampleIds),
            LisProtocolType.DataCap => throw new NotImplementedException(),
            _ => throw new NotImplementedException(),
        };
    }

    public ILisMessage BuildSampleTestResultMessage(LisChannel lisChannel, IReadOnlyList<LisPatient> patients)
    {
        return lisChannel.ProtocolType switch
        {
            LisProtocolType.Astm => astmMessageBuilder.BuildSampleTestResultMessage(lisChannel, patients),
            LisProtocolType.HL7 => hl7MessageBuilder.BuildSampleTestResultMessage(lisChannel, patients),
            LisProtocolType.DataCap => throw new NotImplementedException(),
            _ => throw new NotImplementedException(),
        };
    }

    public ILisMessage BuildQualityControlTestResultMessage(LisChannel lisChannel, LisQualityControlTestResult qcTestResult)
    {
        return lisChannel.ProtocolType switch
        {
            LisProtocolType.Astm => astmMessageBuilder.BuildQualityControlTestResultMessage(lisChannel, qcTestResult),
            LisProtocolType.HL7 => hl7MessageBuilder.BuildQualityControlTestResultMessage(lisChannel, qcTestResult),
            LisProtocolType.DataCap => throw new NotImplementedException(),
            _ => throw new NotImplementedException(),
        };
    }

    public ILisMessage? BuildAcknowledgementMessage(LisChannel lisChannel, Dictionary<string, string> messageMetadata, bool success)
    {
        return lisChannel.ProtocolType switch
        {
            LisProtocolType.Astm => astmMessageBuilder.BuildAcknowledgementMessage(lisChannel, messageMetadata, success),
            LisProtocolType.HL7 => hl7MessageBuilder.BuildAcknowledgementMessage(lisChannel, messageMetadata, success),
            LisProtocolType.DataCap => throw new NotImplementedException(),
            _ => throw new NotImplementedException(),
        };
    }
}