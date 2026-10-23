using Pharos.Server.Infrastructure.Logging;
using Pharos.Server.Lis.Astm.Application.Messages;

namespace Pharos.Server.Lis.Astm.Tests;

public class AstmUnsupportedRecordTests
{
    [Theory]
    [InlineData("C")]
    [InlineData("M")]
    public void GetMessageString_WithRedactionEnabled_DoesNotExposeUnsupportedPayload(string recordType)
    {
        const string sensitiveData = "Patient name and diagnosis";
        var recordString = $"{recordType}|1|{sensitiveData}";
        var message = new AstmMessageParser().ParseMessage($"H|\\^&\r{recordString}\rL|1|F");

        var redactedMessage = message.GetMessageString(redactSensitiveData: true);

        redactedMessage.Should().NotContain(sensitiveData);
        redactedMessage.Should().Contain(PropertyRedactionExtensions.RedactedValue);
        message.GetMessageString(redactSensitiveData: false).Should().Contain(recordString);
    }
}
