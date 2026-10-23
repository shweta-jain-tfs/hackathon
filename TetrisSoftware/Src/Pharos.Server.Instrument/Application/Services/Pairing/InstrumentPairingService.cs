using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pharos.Contracts.Notifications;
using Pharos.Server.Application.Exceptions;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Infrastructure;
using Pharos.Server.Instrument.Application.Commands.CreateInstrument;
using Pharos.Server.Instrument.Application.Interfaces;
using Pharos.Server.Instrument.Application.Jobs;
using Pharos.Server.Instrument.Infrastructure.Grpc;
using Quartz;
using Tetris.Protocol.Pharos.Pairing;

namespace Pharos.Server.Instrument.Application.Services.Pairing;

internal sealed class InstrumentPairingService : IInstrumentPairingService
{
    private const int _instrumentNonceLength = 32;
    private readonly GrpcSettings _options;
    private readonly Lock _operationsLock = new();
    private readonly List<PairingOperation> _operations = new();
    private readonly TimeSpan _tokenLifetime;
    private readonly IInstrumentManager _instrumentManager;
    private readonly ICertificateStore _certificateStore;
    private readonly IDateTime _dateTime;
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<InstrumentPairingService> _logger;

    public InstrumentPairingService(IOptions<GrpcSettings> options,
        IInstrumentManager instrumentManager,
        ICertificateStore certificateStore,
        IDateTime dateTime,
        ISchedulerFactory schedulerFactory,
        IServiceScopeFactory scopeFactory,
        ILogger<InstrumentPairingService> logger)
    {
        _instrumentManager = instrumentManager;
        _certificateStore = certificateStore;
        _dateTime = dateTime;
        _schedulerFactory = schedulerFactory;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
        _tokenLifetime = TimeSpan.FromSeconds(_options.PairingService.PairingModeTimeoutSeconds);
    }

    public bool PairingEnabled { get; private set; }

    public async Task EnablePairing()
    {
        if (!_options.MainService.RequireClientCertificate)
        {
            throw new NotAllowedException("The instrument paring feature has been disabled in the configuration.");
        }

        var job = JobBuilder.Create<StopPairingJob>()
            .WithIdentity(StopPairingJob.Key)
            .Build();
        var trigger = TriggerBuilder.Create()
            .StartAt(_dateTime.UtcNow.Add(_tokenLifetime))
            .ForJob(job)
            .Build();

        var scheduler = await _schedulerFactory.GetScheduler();
        await scheduler.DeleteJob(StopPairingJob.Key);
        await scheduler.ScheduleJob(job, trigger);
        PairingEnabled = true;
        _logger.LogInformation("Pairing mode enabled for {Timeout} seconds.", _options.PairingService.PairingModeTimeoutSeconds);

        using var scope = _scopeFactory.CreateScope();
        var instrumentPairingHub = scope.ServiceProvider.GetRequiredService<IInstrumentPairingHub>();
        await instrumentPairingHub.PairingModeStateChanged(true);
    }

    public async Task DisablePairing()
    {
        if (!_options.MainService.RequireClientCertificate)
        {
            throw new NotAllowedException("The instrument paring feature has been disabled in the configuration.");
        }

        PairingEnabled = false;
        lock (_operationsLock)
        {
            _operations.Clear();
        }

        var scheduler = await _schedulerFactory.GetScheduler();
        await scheduler.DeleteJob(StopPairingJob.Key);
        _logger.LogInformation("Pairing mode disabled and pending pairing operations cleared.");

        using var scope = _scopeFactory.CreateScope();
        var instrumentPairingHub = scope.ServiceProvider.GetRequiredService<IInstrumentPairingHub>();
        await instrumentPairingHub.PairingModeStateChanged(false);
    }

    public async Task<StartPairingResponse> StartPairing(StartPairingRequest request)
    {
        StartPairingResponse? result = null;
        string? instrumentSerial = null;
        try
        {
            lock (_operationsLock)
            {
                if (!TryReadInstrumentNumber(request.InstrumentRootCA, out instrumentSerial))
                {
                    // the instrument root CA is invalid
                    _logger.LogWarning("Rejected pairing request with an invalid instrument certificate.");
                    result = new StartPairingResponse { State = EnumPairingState.Rejected };
                    return result;
                }

                if (request.InstrumentNonce.Length != _instrumentNonceLength)
                {
                    _logger.LogWarning("Rejected pairing request from instrument {Instrument} with an invalid nonce length.", instrumentSerial);
                    result = new StartPairingResponse { State = EnumPairingState.Rejected };
                    return result;
                }

                if (_operations.Any(operation => operation.InstrumentSerial == instrumentSerial && operation.State == EnumPairingState.Rejected))
                {
                    // A rejected instrument remains blocked until pairing mode is disabled.
                    _logger.LogInformation("Rejected repeated pairing request from previously rejected instrument {Instrument}.", instrumentSerial);
                    result = new StartPairingResponse { State = EnumPairingState.Rejected };
                    return result;
                }

                var previousOperation = _operations.FirstOrDefault(operation => operation.InstrumentSerial == instrumentSerial);
                if (previousOperation is not null)
                {
                    _operations.Remove(previousOperation);
                    _logger.LogInformation("Removed the previous pairing operation for instrument {Instrument}.", instrumentSerial);
                }

                using var pharosCa = _certificateStore.GetLocalTrustRoot();
                try
                {
                    var instrumentNonce = request.InstrumentNonce.ToByteArray();
                    var pharosProof = SignNonce(pharosCa, instrumentNonce);
                    var pharosCaDer = pharosCa.Export(X509ContentType.Cert);

                    if (_instrumentManager.FindInstrument(instrumentSerial) != null &&
                        IsTrustedRoot(request.InstrumentRootCA.DerBytes.ToByteArray()) && !_options.PairingService.AllowRepeatedPairing)
                    {
                        // this instrument has already been paired
                        _logger.LogInformation("Returning recovery pairing response for a trusted instrument {Instrument}.", instrumentSerial);
                        result = new StartPairingResponse
                        {
                            State = EnumPairingState.Trusted,
                            PharosRootCA = new Certificate
                            {
                                DerBytes = ByteString.CopyFrom(pharosCaDer),
                                Sha256Fingerprint = Convert.ToHexString(SHA256.HashData(pharosCaDer))
                            },
                            InstrumentNonceSignature = ByteString.CopyFrom(pharosProof.Signature),
                            SignatureAlgorithm = pharosProof.Algorithm
                        };
                        return result;
                    }

                    // this is a new instrument
                    var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
                    var expiresAt = _dateTime.Now.Add(_tokenLifetime);
                    var pharosNonce = RandomNumberGenerator.GetBytes(32);

                    _operations.Add(new PairingOperation(token, request.InstrumentRootCA.DerBytes.ToByteArray(), instrumentSerial, pharosNonce, expiresAt));
                    var pendingInstrument = GetPendingApproval();
                    _logger.LogInformation("Created pairing operation for the instrument {Instrument}.", instrumentSerial);
                    if (pendingInstrument != instrumentSerial)
                    {
                        instrumentSerial = null;
                    }


                    result = new StartPairingResponse
                    {
                        State = EnumPairingState.WaitingForApproval,
                        PairingToken = token,
                        TokenExpiresAt = expiresAt.ToUniversalTime().ToTimestamp(),
                        PharosRootCA = new Certificate
                        {
                            DerBytes = ByteString.CopyFrom(pharosCaDer),
                            Sha256Fingerprint = Convert.ToHexString(SHA256.HashData(pharosCaDer))
                        },
                        PharosNonce = ByteString.CopyFrom(pharosNonce),
                        InstrumentNonceSignature = ByteString.CopyFrom(pharosProof.Signature),
                        SignatureAlgorithm = pharosProof.Algorithm
                    };
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(exception, "Error starting a pairing request from the instrument {Instrument}.", instrumentSerial);
                    result = new StartPairingResponse { State = EnumPairingState.Rejected };
                }
                return result;
            }
        }
        finally
        {
            if ((result?.State is EnumPairingState.WaitingForApproval or EnumPairingState.Trusted) && instrumentSerial != null)
            {
                using var scope = _scopeFactory.CreateScope();
                var instrumentPairingHub = scope.ServiceProvider.GetRequiredService<IInstrumentPairingHub>();
                if (result.State == EnumPairingState.WaitingForApproval)
                {
                    await instrumentPairingHub.ApprovalRequested(instrumentSerial);
                }
                else
                {
                    await instrumentPairingHub.InstrumentPaired(instrumentSerial);
                }
            }
        }
    }

    public string? GetPendingApproval()
    {
        lock (_operationsLock)
        {
            return _operations.FirstOrDefault(x => x.State == EnumPairingState.WaitingForApproval && x.ExpiresAt > _dateTime.Now)?.InstrumentSerial;
        }
    }

    public EnumPairingState GetPairingState(string pairingToken)
    {
        lock (_operationsLock)
        {
            return TryGetOperation(pairingToken, out var operation)
                ? operation.State
                : EnumPairingState.InvalidToken;
        }
    }

    public async Task HandleApproval(string instrumentNumber, bool approveParing)
    {
        string? nextInstrumentToApprove = null;
        lock (_operationsLock)
        {
            var operation = _operations.FirstOrDefault(x => x.State == EnumPairingState.WaitingForApproval && x.InstrumentSerial == instrumentNumber);
            if (operation == null)
            {
                throw new NotFoundException("The specified instrument is not waiting for the approval.");
            }

            operation.State = approveParing ? EnumPairingState.WaitingForSignature : EnumPairingState.Rejected;
            if (approveParing)
            {
                _logger.LogInformation("Approved pairing for the instrument {Instrument}.", instrumentNumber);
            }
            else
            {
                _logger.LogInformation("Rejected pairing for the instrument {Instrument}.", instrumentNumber);
                nextInstrumentToApprove = GetPendingApproval();
            }
        }

        if (nextInstrumentToApprove != null)
        {
            using var scope = _scopeFactory.CreateScope();
            var instrumentPairingHub = scope.ServiceProvider.GetRequiredService<IInstrumentPairingHub>();
            await instrumentPairingHub.ApprovalRequested(nextInstrumentToApprove);
        }
    }

    public async Task<EnumPairingState> ConfirmPairing(ConfirmPairingRequest request, IPAddress instrumentIpAddress)
    {
        PairingOperation? operation;
        string? nextInstrumentToApprove;
        var pairingSucceeded = false;
        lock (_operationsLock)
        {
            if (!TryGetOperation(request.PairingToken, out operation))
            {
                throw new InvalidOperationException($"Invalid pairing token: {request.PairingToken}");
            }

            if (operation.State != EnumPairingState.WaitingForSignature)
            {
                throw new InvalidOperationException($"Pairing operation is not in a valid state: {operation.State}");
            }

            try
            {
                _logger.LogDebug("Verifying signature for instrument {Instrument}.", operation.InstrumentSerial);
                using var instrumentCa = X509CertificateLoader.LoadCertificate(operation.InstrumentCa);

                if (VerifySignature(instrumentCa, operation.PharosNonce, request.PharosNonceSignature.ToByteArray(), request.SignatureAlgorithm))
                {
                    // In case the instrument simulator is running on the same machine as Pharos,
                    // we check if the certificate is already trusted before adding it to the store.
                    // This prevents a duplicate certificate from being added to the store.
                    _logger.LogDebug("Adding instrument CA to the trusted root store.");
                    using var existingTrustedRoot = _certificateStore.GetCertificate(
                        _certificateStore.StoreLocation,
                        StoreName.Root,
                        instrumentCa.Thumbprint);
                    if (existingTrustedRoot is null)
                    {
                        _certificateStore.AddTrustedRoot(instrumentCa);
                    }
                    operation.State = EnumPairingState.Trusted;

                    // We only remove trusted instruments from the operations list.
                    // Until we turn of the paring mode, we want to reject subsequent pairing attempts
                    // from an already rejected instrument.
                    _operations.Remove(operation);
                    pairingSucceeded = true;
                    _logger.LogInformation("Successfully paired instrument {Instrument}.", operation.InstrumentSerial);
                }
                else
                {
                    operation.State = EnumPairingState.Rejected;
                    _logger.LogWarning("Rejected pairing for the instrument {Instrument} because the Pharos nonce signature was invalid.", operation.InstrumentSerial);
                }
            }
            catch (CryptographicException exception)
            {
                operation.State = EnumPairingState.Rejected;
                _logger.LogWarning(exception, "Error completing pairing of the instrument {Instrument}.", operation.InstrumentSerial);
            }

            nextInstrumentToApprove = GetPendingApproval();
        }

        using var scope = _scopeFactory.CreateScope();
        var instrumentPairingHub = scope.ServiceProvider.GetRequiredService<IInstrumentPairingHub>();
        if (pairingSucceeded)
        {
            if (_instrumentManager.FindInstrument(operation.InstrumentSerial) is null)
            {
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                await sender.Send(new CreateInstrumentCommand(instrumentIpAddress, operation.InstrumentSerial));
            }
            await instrumentPairingHub.InstrumentPaired(operation.InstrumentSerial);
        }
        if (nextInstrumentToApprove != null)
        {
            await instrumentPairingHub.ApprovalRequested(nextInstrumentToApprove);
        }

        return operation.State;
    }

    private bool TryReadInstrumentNumber(Certificate? certificate, out string serial)
    {
        serial = string.Empty;
        if (certificate is null || certificate.DerBytes.IsEmpty || certificate.Sha256Fingerprint.Length != 64)
        {
            return false;
        }

        var derBytes = certificate.DerBytes.ToByteArray();
        if (!string.Equals(Convert.ToHexString(SHA256.HashData(derBytes)), certificate.Sha256Fingerprint, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            using var instrumentCa = X509CertificateLoader.LoadCertificate(derBytes);
            if (!IsInstrumentCa(instrumentCa, out serial))
            {
                return false;
            }

            return true;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    private bool IsTrustedRoot(byte[] certificateDer)
    {
        using var certificate = X509CertificateLoader.LoadCertificate(certificateDer);
        using var trustedRoot = _certificateStore.GetCertificate(
            _certificateStore.StoreLocation,
            StoreName.Root,
            certificate.Thumbprint);
        return trustedRoot is not null;
    }

    private bool IsInstrumentCa(X509Certificate2 certificate, out string serial)
    {
        serial = string.Empty;
        var basicConstraints = certificate.Extensions.OfType<X509BasicConstraintsExtension>().FirstOrDefault();
        var keyUsage = certificate.Extensions.OfType<X509KeyUsageExtension>().FirstOrDefault();
        var now = _dateTime.UtcNow;

        if (certificate.NotBefore > now || certificate.NotAfter <= now ||
            certificate.NotAfter - certificate.NotBefore < TimeSpan.FromDays(365 * 30) ||
            !string.Equals(certificate.GetNameInfo(X509NameType.SimpleName, false), "Phadia E Instrument CA", StringComparison.OrdinalIgnoreCase) ||
            !certificate.Subject.Contains("O=Thermo Fisher Scientific", StringComparison.OrdinalIgnoreCase) ||
            basicConstraints is not { Critical: true, CertificateAuthority: true, HasPathLengthConstraint: true, PathLengthConstraint: 0 } ||
            keyUsage is not { Critical: true, KeyUsages: X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign })
        {
            return false;
        }

        serial = certificate.GetInstrumentNumber();
        var isValidLength = serial.Length <= Constants.InstrumentSerialNumberLength;
        if (!isValidLength)
        {
            _logger.LogWarning("Instrument serial number '{Instrument}' exceeds the maximum length of {MaxLength}.", serial, Constants.InstrumentSerialNumberLength);
        }
        return !string.IsNullOrWhiteSpace(serial) && isValidLength;
    }

    private static (string Algorithm, byte[] Signature) SignNonce(X509Certificate2 certificate, byte[] nonce)
    {
        using var rsa = certificate.GetRSAPrivateKey();
        if (rsa is not null)
        {
            return ("RS256", rsa.SignData(nonce, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
        }
        throw new CryptographicException("The Pharos CA must have an RSA private key.");
    }

    private static bool VerifySignature(X509Certificate2 certificate, byte[] nonce, byte[] signature, string algorithm)
    {
        using var publicKey = certificate.GetRSAPublicKey();
        if (publicKey is null)
        {
            return false;
        }

        return algorithm switch
        {
            "RS256" => publicKey.VerifyData(nonce, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1),
            "RS384" => publicKey.VerifyData(nonce, signature, HashAlgorithmName.SHA384, RSASignaturePadding.Pkcs1),
            "RS512" => publicKey.VerifyData(nonce, signature, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1),
            "PS256" => publicKey.VerifyData(nonce, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pss),
            "PS384" => publicKey.VerifyData(nonce, signature, HashAlgorithmName.SHA384, RSASignaturePadding.Pss),
            "PS512" => publicKey.VerifyData(nonce, signature, HashAlgorithmName.SHA512, RSASignaturePadding.Pss),
            _ => false
        };
    }

    private bool TryGetOperation(string token, out PairingOperation operation)
    {
        if (!string.IsNullOrWhiteSpace(token))
        {
            var op = _operations.FirstOrDefault(x => x.Token == token);
            if (op != null)
            {
                if (op.ExpiresAt <= _dateTime.Now)
                {
                    _operations.Remove(op);
                }
                else
                {
                    operation = op;
                    return true;
                }
            }
        }
        operation = null!;
        return false;
    }

}
