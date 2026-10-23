using FluentAssertions;
using Pharos.Server.Application.Lis;
using Pharos.Server.Lis.HL7.Application.Mappings;
using Pharos.Server.Lis.HL7.Application.Messages;
using Xunit;

namespace Pharos.Server.Lis.HL7.Tests.Application.Mappings;

public class HL7CommonOrderSegmentExtensionsTests
{
    [Trait("ProductRequirement", "PR_570")]
    [Fact]
    public void HL7ImportSetting_SkipImportOFRequestorInformation_OFF_ImportRequestorInfoIfPresentInIncomingHL7Data()
    {
        var order = ParseCommonOrderSegment();
        var sample = new LisSample();

        order.MapToSample(sample, skipRequestorInformation:false);

        sample.Requestor.Should().NotBeNull();
        sample.Requestor!.RequestorId.Should().Be("REQ1001");
        sample.Requestor.Name.Should().Be("Doe, A, John");
        sample.Requestor.PhoneNumber.Should().Be("+441234567890");
        sample.Requestor.Address.Should().Be($"123 Baker Street{Environment.NewLine}London{Environment.NewLine}Greater London{Environment.NewLine}NW16XE{Environment.NewLine}UK");
        sample.Requestor.ZipCode.Should().Be("NW16XE");
    }

    [Trait("ProductRequirement", "PR_570")]
    [Fact]
    public void HL7ImportSetting_SkipImportOFRequestorInformation_ON_SkipImportingRequestorInfoIfPresentInIncomingHL7Data()
    {
        var order = ParseCommonOrderSegment();
        var sample = new LisSample();

        order.MapToSample(sample, skipRequestorInformation:true);

        sample.Requestor.Should().BeNull();
    }


    #region Test Helpers

    private static HL7CommonOrderSegment ParseCommonOrderSegment()
    {
        var hl7LisMessageWithRequestorInformation = """
            MSH|^&~\|LIS||||20110116124500||ORM^O01|20110412165020|P|2.5|||AL
            PID|1|PID1||12345|Patient Name||19610207|M|||Street^^City^State^ZIP^Country
            NTE|1|O|Comment|O
            ORC|NW|RID1|||||A||20110116161600|||REQ1001^Doe^John^A|||||||||||+441234567890|123 Baker Street^^London^Greater London^NW16XE^UK
            SPM|1|||SER
            SAC|||SACSID1||||||||||RACKID|2
            OBR|1|RID1||sIgE-f1|||20110116154433||||N||||P|
            OBX|1|CE|OBXSID1
            OBX|2|CE|SY^Symptom||1
            OBX|3|CE|SY^Symptom||5
            """.ReplaceLineEndings("\r");

        var parser = new HL7MessageParser();

        var message = (HL7Message)parser.ParseMessage(hl7LisMessageWithRequestorInformation);
        var orderMessage = HL7OrderMessage.Parse(message);

        var order = orderMessage.Patients.Single().Orders.Single();

        return order.CommonOrder;
    }

    #endregion
}
