using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Instrument.Infrastructure.Kestrel;

namespace Pharos.Server.Instrument.Infrastructure.Grpc.Interceptors;

public sealed class AuthInterceptorTests
{
    private readonly Mock<IInstrumentManager> _instrumentManager = new();

    [Fact]
    public async Task UnaryServerHandler_TrustedCertificateForRequestedInstrument_CallsContinuation()
    {
        const string instrumentNumber = "INSTR-001";
        using var certificate = CreateCertificate();
        var cache = new ClientCertificateCache();
        cache.Add(certificate.Thumbprint, instrumentNumber);
        var interceptor = CreateInterceptor(cache);
        var context = CreateContext(instrumentNumber, certificate);

        var response = await interceptor.UnaryServerHandler(
            "request",
            context,
            (_, _) => Task.FromResult("response"));

        response.Should().Be("response");
        _instrumentManager.Verify(manager => manager.FindInstrument(instrumentNumber), Times.Once);
    }

    [Fact]
    public async Task UnaryServerHandler_TrustedCertificateForAnotherInstrument_RejectsRequest()
    {
        using var certificate = CreateCertificate();
        var cache = new ClientCertificateCache();
        cache.Add(certificate.Thumbprint, "INSTR-OTHER");
        var interceptor = CreateInterceptor(cache);
        var context = CreateContext("INSTR-001", certificate);
        var continuationCalled = false;

        var exception = await Assert.ThrowsAsync<RpcException>(() => interceptor.UnaryServerHandler(
            "request",
            context,
            (_, _) =>
            {
                continuationCalled = true;
                return Task.FromResult("response");
            }));

        exception.StatusCode.Should().Be(StatusCode.Unauthenticated);
        continuationCalled.Should().BeFalse();
        _instrumentManager.Verify(manager => manager.FindInstrument(It.IsAny<string>()), Times.Never);
    }

    private AuthInterceptor CreateInterceptor(ClientCertificateCache cache)
    {
        return new AuthInterceptor(
            _instrumentManager.Object,
            cache,
            Options.Create(new GrpcSettings
            {
                MainService = new GrpcSettings.MainServiceSettings { RequireClientCertificate = true }
            }));
    }

    private static TestServerCallContext CreateContext(string instrumentNumber, X509Certificate2 certificate)
    {
        var httpContext = new DefaultHttpContext
        {
            Connection =
            {
                ClientCertificate = certificate
            }
        };
        return new TestServerCallContext(
            new Metadata { { "instrumentSerial", instrumentNumber } },
            new Dictionary<object, object> { ["__HttpContext"] = httpContext });
    }

    private static X509Certificate2 CreateCertificate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=Instrument CA", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
    }

    private sealed class TestServerCallContext(Metadata requestHeaders, IDictionary<object, object> userState) : ServerCallContext
    {
        protected override string MethodCore => "TestMethod";
        protected override string HostCore => "localhost";
        protected override string PeerCore => "ipv4:127.0.0.1:0";
        protected override DateTime DeadlineCore => DateTime.MaxValue;
        protected override Metadata RequestHeadersCore => requestHeaders;
        protected override CancellationToken CancellationTokenCore => CancellationToken.None;
        protected override Metadata ResponseTrailersCore { get; } = [];
        protected override Status StatusCore { get; set; }
        protected override WriteOptions? WriteOptionsCore { get; set; }
        protected override AuthContext AuthContextCore { get; } = new("test", []);
        protected override IDictionary<object, object> UserStateCore => userState;

        protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders)
        {
            return Task.CompletedTask;
        }

        protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options)
        {
            return null!;
        }
    }
}
