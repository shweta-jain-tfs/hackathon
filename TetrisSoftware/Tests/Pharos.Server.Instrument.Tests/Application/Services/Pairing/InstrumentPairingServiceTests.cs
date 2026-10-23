using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Google.Protobuf;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Pharos.Contracts.Notifications;
using Pharos.Server.Application.Exceptions;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Infrastructure;
using Pharos.Server.Instrument.Application.Commands.CreateInstrument;
using Pharos.Server.Instrument.Application.Jobs;
using Pharos.Server.Instrument.Infrastructure.Grpc;
using Quartz;
using Tetris.Protocol.Pharos.Pairing;

namespace Pharos.Server.Instrument.Application.Services.Pairing;

public sealed class InstrumentPairingServiceTests
{
    private readonly Mock<IInstrumentManager> _instrumentManager = new();
    private readonly Mock<ICertificateStore> _certificateStore = new();
    private readonly Mock<IDateTime> _dateTime = new();
    private readonly Mock<ISchedulerFactory> _schedulerFactory = new();
    private readonly Mock<IScheduler> _scheduler = new();
    private readonly Mock<IInstrumentPairingHub> _hub = new();
    private readonly Mock<ILogger<InstrumentPairingService>> _logger = new();
    private readonly Mock<ISender> _sender = new();
    private DateTimeOffset _now = new(2026, 9, 17, 10, 0, 0, TimeSpan.Zero);

    public InstrumentPairingServiceTests()
    {
        _dateTime.SetupGet(value => value.Now).Returns(() => _now);
        _dateTime.SetupGet(value => value.UtcNow).Returns(() => _now);
        _certificateStore.Setup(value => value.GetLocalTrustRoot()).Returns(() => CreateCertificate("Pharos CA"));
        _schedulerFactory.Setup(value => value.GetScheduler(It.IsAny<CancellationToken>())).ReturnsAsync(_scheduler.Object);
        _scheduler.Setup(value => value.ScheduleJob(It.IsAny<IJobDetail>(), It.IsAny<ITrigger>(), It.IsAny<CancellationToken>())).ReturnsAsync(_now);
        _scheduler.Setup(value => value.DeleteJob(StopPairingJob.Key, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _sender.Setup(value => value.Send(It.IsAny<CreateInstrumentCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(new Domain.Instrument());
    }

    [Fact]
    public async Task StartPairing_InvalidCertificate_RejectsRequest()
    {
        var service = CreateService();

        var response = await service.StartPairing(new StartPairingRequest
        {
            InstrumentRootCA = new Certificate { DerBytes = ByteString.CopyFromUtf8("not a certificate"), Sha256Fingerprint = new string('0', 64) }
        });

        Assert.Equal(EnumPairingState.Rejected, response.State);
        Assert.Null(service.GetPendingApproval());
        _certificateStore.Verify(value => value.GetLocalTrustRoot(), Times.Never);
        _hub.Verify(value => value.ApprovalRequested(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task StartPairing_NewValidInstrument_CreatesPendingApprovalAndNotifiesHub()
    {
        using var instrumentCa = CreateInstrumentCertificate("INSTR-001");
        var service = CreateService();

        var response = await service.StartPairing(CreateStartRequest(instrumentCa, RandomNumberGenerator.GetBytes(32)));

        Assert.Equal(EnumPairingState.WaitingForApproval, response.State);
        Assert.Equal("INSTR-001", service.GetPendingApproval());
        Assert.Equal(EnumPairingState.WaitingForApproval, service.GetPairingState(response.PairingToken));
        Assert.Equal(64, response.PairingToken.Length);
        Assert.Equal(32, response.PharosNonce.Length);
        Assert.Equal(_now.AddSeconds(2), response.TokenExpiresAt.ToDateTimeOffset());
        _hub.Verify(value => value.ApprovalRequested("INSTR-001"), Times.Once);
    }

    [Fact]
    public async Task StartPairing_RepeatedRequest_ReplacesPreviousOperationAndCreatesInstrumentOnlyOnce()
    {
        using var instrumentCa = CreateInstrumentCertificate("INSTR-REPEATED");
        var service = CreateService();
        var first = await service.StartPairing(CreateStartRequest(instrumentCa, RandomNumberGenerator.GetBytes(32)));
        var repeated = await service.StartPairing(CreateStartRequest(instrumentCa, RandomNumberGenerator.GetBytes(32)));

        Assert.NotEqual(first.PairingToken, repeated.PairingToken);
        Assert.Equal(EnumPairingState.InvalidToken, service.GetPairingState(first.PairingToken));
        Assert.Equal(EnumPairingState.WaitingForApproval, service.GetPairingState(repeated.PairingToken));
        _hub.Verify(value => value.ApprovalRequested("INSTR-REPEATED"), Times.Exactly(2));

        await service.HandleApproval("INSTR-REPEATED", true);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ConfirmPairing(new ConfirmPairingRequest
        {
            PairingToken = first.PairingToken,
            PharosNonceSignature = ByteString.Empty,
            SignatureAlgorithm = "RS256"
        }, IPAddress.Loopback));

        using var rsa = instrumentCa.GetRSAPrivateKey();
        var signature = rsa!.SignData(repeated.PharosNonce.ToByteArray(), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var state = await service.ConfirmPairing(new ConfirmPairingRequest
        {
            PairingToken = repeated.PairingToken,
            PharosNonceSignature = ByteString.CopyFrom(signature),
            SignatureAlgorithm = "RS256"
        }, IPAddress.Loopback);

        Assert.Equal(EnumPairingState.Trusted, state);
        _sender.Verify(value => value.Send(
            It.Is<CreateInstrumentCommand>(command => command.InstrumentNumber == "INSTR-REPEATED"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartPairing_TrustedInstrument_ReturnsVerifiableRecoveryResponseWithoutCreatingOperation()
    {
        using var instrumentCa = CreateInstrumentCertificate("INSTR-TRUSTED");
        using var existingTrustedRoot = X509CertificateLoader.LoadCertificate(instrumentCa.Export(X509ContentType.Cert));
        var nonce = RandomNumberGenerator.GetBytes(32);
        _instrumentManager.Setup(value => value.FindInstrument("INSTR-TRUSTED")).Returns(Mock.Of<IInstrument>());
        _certificateStore.SetupGet(value => value.StoreLocation).Returns(StoreLocation.CurrentUser);
        _certificateStore
            .Setup(value => value.GetCertificate(StoreLocation.CurrentUser, StoreName.Root, instrumentCa.Thumbprint))
            .Returns(existingTrustedRoot);
        var service = CreateService();

        var response = await service.StartPairing(CreateStartRequest(instrumentCa, nonce));

        Assert.Equal(EnumPairingState.Trusted, response.State);
        Assert.Empty(response.PairingToken);
        Assert.Empty(response.PharosNonce);
        Assert.Equal("RS256", response.SignatureAlgorithm);
        Assert.Equal(Convert.ToHexString(SHA256.HashData(response.PharosRootCA.DerBytes.ToByteArray())), response.PharosRootCA.Sha256Fingerprint);
        using var pharosCa = X509CertificateLoader.LoadCertificate(response.PharosRootCA.DerBytes.ToByteArray());
        using var rsa = pharosCa.GetRSAPublicKey();
        Assert.True(rsa!.VerifyData(nonce, response.InstrumentNonceSignature.ToByteArray(), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
        Assert.Null(service.GetPendingApproval());
        _hub.Verify(value => value.ApprovalRequested(It.IsAny<string>()), Times.Never);
        _hub.Verify(value => value.InstrumentPaired("INSTR-TRUSTED"), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(31)]
    [InlineData(33)]
    public async Task StartPairing_InvalidInstrumentNonceLength_IsRejected(int nonceLength)
    {
        using var instrumentCa = CreateInstrumentCertificate("INSTR-INVALID-NONCE");
        var service = CreateService();

        var response = await service.StartPairing(CreateStartRequest(instrumentCa, RandomNumberGenerator.GetBytes(nonceLength)));

        Assert.Equal(EnumPairingState.Rejected, response.State);
        Assert.Null(service.GetPendingApproval());
        _hub.Verify(value => value.ApprovalRequested(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task StartPairing_ExistingInstrumentWithoutTrustedRoot_StartsPairing()
    {
        using var instrumentCa = CreateInstrumentCertificate("INSTR-NO-TRUST");
        _instrumentManager.Setup(value => value.FindInstrument("INSTR-NO-TRUST")).Returns(Mock.Of<IInstrument>());
        _certificateStore.SetupGet(value => value.StoreLocation).Returns(StoreLocation.CurrentUser);
        _certificateStore
            .Setup(value => value.GetCertificate(StoreLocation.CurrentUser, StoreName.Root, instrumentCa.Thumbprint))
            .Returns((X509Certificate2?)null);
        var service = CreateService();

        var response = await service.StartPairing(CreateStartRequest(instrumentCa, RandomNumberGenerator.GetBytes(32)));

        Assert.Equal(EnumPairingState.WaitingForApproval, response.State);
        Assert.Equal("INSTR-NO-TRUST", service.GetPendingApproval());
    }

    [Fact]
    public async Task ConfirmPairing_AfterApprovalAndValidSignature_TrustsInstrumentAndNotifiesHub()
    {
        using var instrumentCa = CreateInstrumentCertificate("INSTR-002");
        var service = CreateService();
        var start = await service.StartPairing(CreateStartRequest(instrumentCa, RandomNumberGenerator.GetBytes(32)));
        await service.HandleApproval("INSTR-002", true);

        using var rsa = instrumentCa.GetRSAPrivateKey();
        var signature = rsa!.SignData(start.PharosNonce.ToByteArray(), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var state = await service.ConfirmPairing(new ConfirmPairingRequest
        {
            PairingToken = start.PairingToken,
            PharosNonceSignature = ByteString.CopyFrom(signature),
            SignatureAlgorithm = "RS256"
        }, IPAddress.Loopback);

        Assert.Equal(EnumPairingState.Trusted, state);
        Assert.Equal(EnumPairingState.InvalidToken, service.GetPairingState(start.PairingToken));
        _certificateStore.Verify(value => value.AddTrustedRoot(It.IsAny<X509Certificate2>()), Times.Once);
        _sender.Verify(value => value.Send(
            It.Is<CreateInstrumentCommand>(command =>
                command.IpAddress.Equals(IPAddress.Loopback) &&
                command.InstrumentNumber == "INSTR-002"),
            It.IsAny<CancellationToken>()), Times.Once);
        _hub.Verify(value => value.InstrumentPaired("INSTR-002"), Times.Once);
    }

    [Fact]
    public async Task ConfirmPairing_WhenInstrumentCertificateIsAlreadyTrusted_CreatesInstrumentWithoutAddingRootAgain()
    {
        using var instrumentCa = CreateInstrumentCertificate("INSTR-TRUSTED-ROOT");
        using var existingTrustedRoot = X509CertificateLoader.LoadCertificate(instrumentCa.Export(X509ContentType.Cert));
        _certificateStore.SetupGet(value => value.StoreLocation).Returns(StoreLocation.CurrentUser);
        _certificateStore
            .Setup(value => value.GetCertificate(StoreLocation.CurrentUser, StoreName.Root, instrumentCa.Thumbprint))
            .Returns(existingTrustedRoot);
        var service = CreateService();
        var start = await service.StartPairing(CreateStartRequest(instrumentCa, RandomNumberGenerator.GetBytes(32)));
        await service.HandleApproval("INSTR-TRUSTED-ROOT", true);

        using var rsa = instrumentCa.GetRSAPrivateKey();
        var signature = rsa!.SignData(start.PharosNonce.ToByteArray(), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var state = await service.ConfirmPairing(new ConfirmPairingRequest
        {
            PairingToken = start.PairingToken,
            PharosNonceSignature = ByteString.CopyFrom(signature),
            SignatureAlgorithm = "RS256"
        }, IPAddress.Loopback);

        Assert.Equal(EnumPairingState.Trusted, state);
        _certificateStore.Verify(value => value.AddTrustedRoot(It.IsAny<X509Certificate2>()), Times.Never);
        _sender.Verify(value => value.Send(
            It.Is<CreateInstrumentCommand>(command => command.InstrumentNumber == "INSTR-TRUSTED-ROOT"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmPairing_ExistingInstrument_DoesNotCreateAnotherInstrument()
    {
        using var instrumentCa = CreateInstrumentCertificate("INSTR-EXISTING");
        _instrumentManager.Setup(value => value.FindInstrument("INSTR-EXISTING")).Returns(Mock.Of<IInstrument>());
        var service = CreateService();
        var start = await service.StartPairing(CreateStartRequest(instrumentCa, RandomNumberGenerator.GetBytes(32)));
        await service.HandleApproval("INSTR-EXISTING", true);

        using var rsa = instrumentCa.GetRSAPrivateKey();
        var signature = rsa!.SignData(start.PharosNonce.ToByteArray(), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var state = await service.ConfirmPairing(new ConfirmPairingRequest
        {
            PairingToken = start.PairingToken,
            PharosNonceSignature = ByteString.CopyFrom(signature),
            SignatureAlgorithm = "RS256"
        }, IPAddress.Loopback);

        Assert.Equal(EnumPairingState.Trusted, state);
        _sender.Verify(value => value.Send(It.IsAny<CreateInstrumentCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        _hub.Verify(value => value.InstrumentPaired("INSTR-EXISTING"), Times.Once);
    }

    [Fact]
    public async Task ConfirmPairing_AfterApprovalWithInvalidSignature_RejectsInstrumentWithoutTrustingIt()
    {
        using var instrumentCa = CreateInstrumentCertificate("INSTR-BAD-SIGNATURE");
        var service = CreateService();
        var start = await service.StartPairing(CreateStartRequest(instrumentCa, RandomNumberGenerator.GetBytes(32)));
        await service.HandleApproval("INSTR-BAD-SIGNATURE", true);

        var state = await service.ConfirmPairing(new ConfirmPairingRequest
        {
            PairingToken = start.PairingToken,
            PharosNonceSignature = ByteString.CopyFrom(RandomNumberGenerator.GetBytes(256)),
            SignatureAlgorithm = "RS256"
        }, IPAddress.Loopback);

        Assert.Equal(EnumPairingState.Rejected, state);
        Assert.Equal(EnumPairingState.Rejected, service.GetPairingState(start.PairingToken));
        _certificateStore.Verify(value => value.AddTrustedRoot(It.IsAny<X509Certificate2>()), Times.Never);
        _sender.Verify(value => value.Send(It.IsAny<CreateInstrumentCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        _hub.Verify(value => value.InstrumentPaired(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmPairing_WhenTrustFinalizationThrows_DoesNotCreateInstrument()
    {
        using var instrumentCa = CreateInstrumentCertificate("INSTR-CRYPTO-ERROR");
        using var nextInstrumentCa = CreateInstrumentCertificate("INSTR-NEXT");
        var service = CreateService();
        var start = await service.StartPairing(CreateStartRequest(instrumentCa, RandomNumberGenerator.GetBytes(32)));
        var next = await service.StartPairing(CreateStartRequest(nextInstrumentCa, RandomNumberGenerator.GetBytes(32)));
        await service.HandleApproval("INSTR-CRYPTO-ERROR", true);
        _certificateStore
            .Setup(value => value.AddTrustedRoot(It.IsAny<X509Certificate2>()))
            .Throws(new CryptographicException("Trust store failed."));

        using var rsa = instrumentCa.GetRSAPrivateKey();
        var signature = rsa!.SignData(start.PharosNonce.ToByteArray(), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var state = await service.ConfirmPairing(new ConfirmPairingRequest
        {
            PairingToken = start.PairingToken,
            PharosNonceSignature = ByteString.CopyFrom(signature),
            SignatureAlgorithm = "RS256"
        }, IPAddress.Loopback);

        Assert.Equal(EnumPairingState.Rejected, state);
        Assert.Equal(EnumPairingState.Rejected, service.GetPairingState(start.PairingToken));
        Assert.Equal(EnumPairingState.WaitingForApproval, service.GetPairingState(next.PairingToken));
        Assert.Equal("INSTR-NEXT", service.GetPendingApproval());
        _sender.Verify(value => value.Send(It.IsAny<CreateInstrumentCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        _hub.Verify(value => value.InstrumentPaired(It.IsAny<string>()), Times.Never);
        _hub.Verify(value => value.ApprovalRequested("INSTR-NEXT"), Times.Once);
    }

    [Fact]
    public async Task StartPairing_WhenInstrumentWasPreviouslyRejected_RejectsSubsequentAttempt()
    {
        using var instrumentCa = CreateInstrumentCertificate("INSTR-REJECTED");
        var service = CreateService();
        var first = await service.StartPairing(CreateStartRequest(instrumentCa, RandomNumberGenerator.GetBytes(32)));
        await service.HandleApproval("INSTR-REJECTED", false);

        var retry = await service.StartPairing(CreateStartRequest(instrumentCa, RandomNumberGenerator.GetBytes(32)));

        Assert.Equal(EnumPairingState.Rejected, retry.State);
        Assert.Equal(EnumPairingState.Rejected, service.GetPairingState(first.PairingToken));
        _certificateStore.Verify(value => value.GetLocalTrustRoot(), Times.Once);
        _hub.Verify(value => value.ApprovalRequested("INSTR-REJECTED"), Times.Once);
    }

    [Fact]
    public async Task GetPairingState_ExpiredOperation_ReturnsInvalidToken()
    {
        using var instrumentCa = CreateInstrumentCertificate("INSTR-003");
        var service = CreateService();
        var start = await service.StartPairing(CreateStartRequest(instrumentCa, RandomNumberGenerator.GetBytes(32)));
        _now = _now.AddSeconds(3);

        var state = service.GetPairingState(start.PairingToken);

        Assert.Equal(EnumPairingState.InvalidToken, state);
        Assert.Null(service.GetPendingApproval());
    }

    [Fact]
    public async Task GetPendingApproval_WhenFirstOperationHasExpired_ReturnsNextPendingInstrument()
    {
        using var firstInstrumentCa = CreateInstrumentCertificate("INSTR-EXPIRED");
        using var secondInstrumentCa = CreateInstrumentCertificate("INSTR-PENDING");
        var service = CreateService();
        await service.StartPairing(CreateStartRequest(firstInstrumentCa, RandomNumberGenerator.GetBytes(32)));
        _now = _now.AddSeconds(1);
        await service.StartPairing(CreateStartRequest(secondInstrumentCa, RandomNumberGenerator.GetBytes(32)));
        _now = _now.AddSeconds(1.5);

        var pendingApproval = service.GetPendingApproval();

        Assert.Equal("INSTR-PENDING", pendingApproval);
    }

    [Fact]
    public async Task HandleApproval_WhenFirstOfMultipleOperationsIsRejected_NotifiesNextPendingInstrument()
    {
        using var firstInstrumentCa = CreateInstrumentCertificate("INSTR-FIRST");
        using var secondInstrumentCa = CreateInstrumentCertificate("INSTR-SECOND");
        var service = CreateService();
        var first = await service.StartPairing(CreateStartRequest(firstInstrumentCa, RandomNumberGenerator.GetBytes(32)));
        var second = await service.StartPairing(CreateStartRequest(secondInstrumentCa, RandomNumberGenerator.GetBytes(32)));

        await service.HandleApproval("INSTR-FIRST", false);

        Assert.Equal(EnumPairingState.Rejected, service.GetPairingState(first.PairingToken));
        Assert.Equal(EnumPairingState.WaitingForApproval, service.GetPairingState(second.PairingToken));
        Assert.Equal("INSTR-SECOND", service.GetPendingApproval());
        _hub.Verify(value => value.ApprovalRequested("INSTR-SECOND"), Times.Once);
    }

    [Fact]
    public async Task ConfirmPairing_WhenFirstOfMultipleOperationsIsTrusted_NotifiesNextPendingInstrument()
    {
        using var firstInstrumentCa = CreateInstrumentCertificate("INSTR-FIRST-TRUSTED");
        using var secondInstrumentCa = CreateInstrumentCertificate("INSTR-SECOND-TRUSTED");
        var service = CreateService();
        var first = await service.StartPairing(CreateStartRequest(firstInstrumentCa, RandomNumberGenerator.GetBytes(32)));
        var second = await service.StartPairing(CreateStartRequest(secondInstrumentCa, RandomNumberGenerator.GetBytes(32)));
        await service.HandleApproval("INSTR-FIRST-TRUSTED", true);
        _hub.Verify(value => value.ApprovalRequested("INSTR-SECOND-TRUSTED"), Times.Never);

        using var rsa = firstInstrumentCa.GetRSAPrivateKey();
        var signature = rsa!.SignData(first.PharosNonce.ToByteArray(), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var state = await service.ConfirmPairing(new ConfirmPairingRequest
        {
            PairingToken = first.PairingToken,
            PharosNonceSignature = ByteString.CopyFrom(signature),
            SignatureAlgorithm = "RS256"
        }, IPAddress.Loopback);

        Assert.Equal(EnumPairingState.Trusted, state);
        Assert.Equal(EnumPairingState.InvalidToken, service.GetPairingState(first.PairingToken));
        Assert.Equal(EnumPairingState.WaitingForApproval, service.GetPairingState(second.PairingToken));
        Assert.Equal("INSTR-SECOND-TRUSTED", service.GetPendingApproval());
        _hub.Verify(value => value.ApprovalRequested("INSTR-SECOND-TRUSTED"), Times.Once);
    }

    [Fact]
    public async Task EnableAndDisablePairing_WhenConfigured_UpdatesStateSchedulesJobAndNotifiesHub()
    {
        var service = CreateService();

        await service.EnablePairing();
        await service.DisablePairing();

        Assert.False(service.PairingEnabled);
        _scheduler.Verify(value => value.ScheduleJob(It.Is<IJobDetail>(job => job.Key == StopPairingJob.Key), It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()), Times.Once);
        _scheduler.Verify(value => value.DeleteJob(StopPairingJob.Key, It.IsAny<CancellationToken>()), Times.Exactly(2));
        _hub.Verify(value => value.PairingModeStateChanged(true), Times.Once);
        _hub.Verify(value => value.PairingModeStateChanged(false), Times.Once);
    }

    [Fact]
    public async Task EnablePairing_SchedulesOneShotStopJobAfterConfiguredTimeout()
    {
        ITrigger? scheduledTrigger = null;
        _scheduler
            .Setup(value => value.ScheduleJob(It.IsAny<IJobDetail>(), It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()))
            .Callback<IJobDetail, ITrigger, CancellationToken>((_, trigger, _) => scheduledTrigger = trigger)
            .ReturnsAsync(_now);
        var service = CreateService();

        await service.EnablePairing();

        Assert.True(service.PairingEnabled);
        Assert.NotNull(scheduledTrigger);
        Assert.Equal(_now.AddSeconds(2), scheduledTrigger.StartTimeUtc);
        Assert.Equal(0, Assert.IsAssignableFrom<ISimpleTrigger>(scheduledTrigger).RepeatCount);
        _scheduler.Verify(value => value.DeleteJob(StopPairingJob.Key, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EnablePairing_WhenDisabledInConfiguration_ThrowsWithoutScheduling()
    {
        var service = CreateService(requirePairing: false);

        await Assert.ThrowsAsync<NotAllowedException>(service.EnablePairing);

        Assert.False(service.PairingEnabled);
        _schedulerFactory.Verify(value => value.GetScheduler(It.IsAny<CancellationToken>()), Times.Never);
        _hub.Verify(value => value.PairingModeStateChanged(It.IsAny<bool>()), Times.Never);
    }

    private InstrumentPairingService CreateService(bool requirePairing = true)
    {
        var services = new ServiceCollection();
        services.AddSingleton(_hub.Object);
        services.AddScoped(_ => _sender.Object);
        var provider = services.BuildServiceProvider();

        return new InstrumentPairingService(
            Options.Create(new GrpcSettings
            {
                MainService = new GrpcSettings.MainServiceSettings { RequireClientCertificate = requirePairing },
                PairingService = new GrpcSettings.PairingServiceSettings { PairingModeTimeoutSeconds = 2 }
            }),
            _instrumentManager.Object,
            _certificateStore.Object,
            _dateTime.Object,
            _schedulerFactory.Object,
            provider.GetRequiredService<IServiceScopeFactory>(),
            _logger.Object);
    }

    private static StartPairingRequest CreateStartRequest(X509Certificate2 certificate, byte[] nonce)
    {
        var derBytes = certificate.Export(X509ContentType.Cert);
        return new StartPairingRequest
        {
            InstrumentRootCA = new Certificate
            {
                DerBytes = ByteString.CopyFrom(derBytes),
                Sha256Fingerprint = Convert.ToHexString(SHA256.HashData(derBytes))
            },
            InstrumentNonce = ByteString.CopyFrom(nonce)
        };
    }

    private X509Certificate2 CreateInstrumentCertificate(string serial)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=Phadia E Instrument CA, O=Thermo Fisher Scientific", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, true, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, true));
        var subjectAlternativeName = new SubjectAlternativeNameBuilder();
        subjectAlternativeName.AddUri(new Uri($"urn:thermofisher:phadia-e:instrument:{serial}"));
        request.CertificateExtensions.Add(subjectAlternativeName.Build());
        return request.CreateSelfSigned(_now.AddDays(-1), _now.AddYears(31));
    }

    private static X509Certificate2 CreateCertificate(string commonName)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest($"CN={commonName}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
    }
}
