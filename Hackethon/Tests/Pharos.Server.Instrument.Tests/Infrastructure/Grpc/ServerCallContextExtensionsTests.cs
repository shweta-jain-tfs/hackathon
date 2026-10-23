using System.Net;

namespace Pharos.Server.Instrument.Infrastructure.Grpc;

public class ServerCallContextExtensionsTests
{
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void GetInstrumentIp_Should_ReturnNone_When_UriIsEmptyStringOrNull(string? uri)
    {
        var ipAddress = ServerCallContextExtensions.GetInstrumentIp(uri);
        ipAddress.Should().Be(IPAddress.None);
    }

    [Theory]
    [InlineData("ipv4:127.0.0.1:4328", "127.0.0.1")]
    [InlineData("ipv6:[::ffff:127.0.0.1]:4328", "127.0.0.1")]
    [InlineData("ipv6:[::1]:63280", "::1")]
    public void GetInstrumentIp_Should_ReturnAnIPAddress_When_UriIsAValidIpv4OrIpv6Address(string? uri, string expectedIp)
    {
        var ipAddress = ServerCallContextExtensions.GetInstrumentIp(uri);
        ipAddress.Should().NotBe(IPAddress.None);

        ipAddress.ToString().Should().Be(expectedIp);
    }
}