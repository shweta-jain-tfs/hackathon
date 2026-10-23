using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Pharos.Server.Grpc;
using Pharos.Server.Infrastructure;
using Pharos.Server.Infrastructure.Settings;
using Pharos.Server.Instrument.Application.Interfaces;
using Pharos.Server.Instrument.Infrastructure.Grpc.Interceptors;
using Tetris.Protocol.Common;
using Tetris.Protocol.Instrument;
using Tetris.Protocol.Instrument.Articles;
using Tetris.Protocol.Instrument.Control;
using Tetris.Protocol.Instrument.Limits;
using Tetris.Protocol.Instrument.Methods;
using Tetris.Protocol.Instrument.Move;
using Tetris.Protocol.Instrument.SetTrayNotUsable;
using Tetris.Protocol.Instrument.TestOrder;
using Tetris.Protocol.Instrument.UpdateBottledReagent;

namespace Pharos.Server.Instrument.Infrastructure.Grpc;

internal class InstrumentConnection : IInstrumentConnection
{
    private readonly string _instrumentNumber;
    private readonly IServiceProvider _serviceProvider;

    private GrpcChannel? _channel;
    private InstrumentService.InstrumentServiceClient? _grpcClient;
    private readonly Metadata _metaData;

    public InstrumentConnection(
        IPAddress ipAddress,
        int port,
        Guid sessionId,
        string instrumentNumber,
        IServiceProvider serviceProvider)
    {
        _instrumentNumber = instrumentNumber;
        _serviceProvider = serviceProvider;
        IpAddress = ipAddress;
        Port = port;
        SessionId = sessionId;
        _metaData = new Metadata
        {
            { CommunicationSession.SessionIdHeader, SessionId.ToString() }
        };
    }

    public Guid SessionId { get; }

    public IPAddress IpAddress { get; }

    public int Port { get; }

    public async Task ConnectAsync()
    {
        var mainService = _serviceProvider.GetRequiredService<IOptions<GrpcSettings>>().Value.MainService;
        var serverName = IpAddress.Equals(IPAddress.Loopback) || IpAddress.Equals(IPAddress.IPv6Loopback)
            ? "localhost"
            : $"{_instrumentNumber}.local";
        var handler = new SocketsHttpHandler
        {
            PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
            KeepAlivePingDelay = TimeSpan.FromSeconds(60),
            KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
            EnableMultipleHttp2Connections = true
        };

        if (mainService.UseHttps)
        {
            // The channel connects by IP, so the default validator compares the certificate to that IP.
            // Retain normal chain validation and replace only that IP-name comparison with the instrument DNS name.
            handler.SslOptions.RemoteCertificateValidationCallback = (_, certificate, _, policyErrors) =>
            {
                if (policyErrors == SslPolicyErrors.None)
                {
                    return true;
                }

                if (policyErrors != SslPolicyErrors.RemoteCertificateNameMismatch || certificate is null)
                {
                    return false;
                }

                using var remoteCertificate = new X509Certificate2(certificate);
                return remoteCertificate.MatchesHostname(serverName, allowWildcards: false, allowCommonName: false);
            };

            if (mainService.RequireClientCertificate)
            {
                var certificateSettings = _serviceProvider.GetRequiredService<IOptions<CertificateSettings>>().Value;
                var certificateStore = _serviceProvider.GetRequiredService<ICertificateStore>();
                handler.SslOptions.ClientCertificates = new X509CertificateCollection
                {
                    certificateStore.GetTlsCertificate(certificateSettings.DefaultServerName)
                };
            }
        }

        var ip = IpAddress.ToString();
        if (ip.Contains(':'))
        {
            ip = '[' + ip + ']';
        }

        var scheme = mainService.UseHttps ? Uri.UriSchemeHttps : Uri.UriSchemeHttp;
        var address = $"{scheme}://{ip}:{Port}";
        _channel = GrpcChannel.ForAddress(address, new GrpcChannelOptions
        {
            HttpHandler = handler
        });

        var invoker = _channel.Intercept(
            ActivatorUtilities.CreateInstance<ClientLoggingInterceptor>(_serviceProvider, _instrumentNumber),
            ActivatorUtilities.CreateInstance<CallSynchronizationInterceptor>(_serviceProvider),
            ActivatorUtilities.CreateInstance<ExceptionInterceptor>(_serviceProvider));

        _grpcClient = new InstrumentService.InstrumentServiceClient(invoker);
        await _channel.ConnectAsync().ConfigureAwait(false);
        StartWaitingForStateChange(_channel.State);
    }

    public async Task DisconnectAsync()
    {
        try
        {
            if (_channel != null)// && _channel.State != ConnectivityState.Shutdown)
            {
                await _channel.ShutdownAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            _channel?.Dispose();
            _channel = null;
            _grpcClient = null;
        }
    }

    public event EventHandler<Guid>? RemoteDisconnect;

    public async Task DeleteTestOrderAsync(DeleteTestOrderRequest request, CancellationToken cancellationToken)
    {
        var client = GetGrpcClient();
        await client.DeleteTestOrderAsync(request, _metaData, null, cancellationToken);
    }

    public async Task SendActiveMethodsAsync(ActiveMethodsRequest request, CancellationToken cancellationToken)
    {
        var client = GetGrpcClient();
        await client.SendActiveMethodsAsync(request, _metaData, null, cancellationToken);
    }

    public async Task UpdateTestOrderAsync(TestOrderRequest request, CancellationToken cancellationToken)
    {
        var client = GetGrpcClient();
        await client.UpdateTestOrderAsync(request, _metaData, null, cancellationToken);
    }

    public async Task UpdateSettingsAsync(Settings request, CancellationToken cancellationToken)
    {
        var client = GetGrpcClient();
        await client.UpdateSettingsAsync(request, _metaData, null, cancellationToken);
    }

    public async Task UpdateLimitsAsync(Limits request, CancellationToken cancellationToken)
    {
        var client = GetGrpcClient();
        await client.UpdateLimitsAsync(request, _metaData, null, cancellationToken);
    }

    public async Task UpdateMethodsAsync(Methods request, CancellationToken cancellationToken)
    {
        var client = GetGrpcClient();
        await client.UpdateMethodsAsync(request, _metaData, null, cancellationToken);
    }

    public async Task UpdateMethodStatusAsync(UpdateMethodStatusRequest request, CancellationToken cancellationToken)
    {
        var client = GetGrpcClient();
        await client.UpdateMethodStatusAsync(request, _metaData, null, cancellationToken);
    }

    public async Task UpdateArticlesAsync(Articles request, CancellationToken cancellationToken)
    {
        var client = GetGrpcClient();
        await client.UpdateArticlesAsync(request, _metaData, null, cancellationToken);
    }

    public async Task SendQcSchedulingAsync(QcSchedulingRequest request, CancellationToken cancellationToken)
    {
        var client = GetGrpcClient();
        await client.SendQcSchedulingAsync(request, _metaData, null, cancellationToken);
    }

    public async Task<CmrTestOrderResponse> SendCmrTestOrderAsync(CmrTestOrderRequest request, CancellationToken cancellationToken)
    {
        var client = GetGrpcClient();
        var response = await client.SendCmrTestOrderAsync(request, _metaData, null, cancellationToken);
        return response;
    }

    public async Task<NewCalCcResponse> OrderNewCalCcAsync(NewCalCcRequest request, CancellationToken cancellationToken)
    {
        var client = GetGrpcClient();
        var response = await client.OrderNewCalCcAsync(request, _metaData, null, cancellationToken);
        return response;
    }

    public async Task UnlockCompartmentAsync(UnlockCompartmentRequest request, CancellationToken cancellationToken)
    {
        var client = GetGrpcClient();
        await client.UnlockCompartmentAsync(request, _metaData, null, cancellationToken);
    }

    public async Task SendErrorResponseAsync(ErrorResponse request, CancellationToken cancellationToken)
    {
        var client = GetGrpcClient();
        await client.SendErrorResponseAsync(request, _metaData, null, cancellationToken);
    }

    public async Task<CurrentCalibrationInformation> SendCalibrationInformationAsync(AvailableCalibrationInformation request, CancellationToken cancellationToken)
    {
        var client = GetGrpcClient();
        var result = await client.SendCalibrationInformationAsync(request, _metaData, null, cancellationToken);
        return result;
    }

    private InstrumentService.InstrumentServiceClient GetGrpcClient()
    {
        if (_channel == null || _channel.State != ConnectivityState.Ready)
        {
            throw new InvalidOperationException("The gRPC channel is not is not ready.");
        }
        return _grpcClient!;
    }

    public async Task<ControlInstrumentStateResponse> ControlInstrumentStateAsync(ControlInstrumentStateRequest request, CancellationToken cancellationToken)
    {
        var client = GetGrpcClient();
        return await client.ControlInstrumentStateAsync(request, _metaData, null, cancellationToken);
    }

    public async Task ControlSignalTowerAsync(ControlSignalTowerRequest request, CancellationToken cancellationToken)
    {
        var client = GetGrpcClient();
        await client.ControlSignalTowerAsync(request, _metaData, null, cancellationToken);
    }

    public async Task ControlLightAsync(CancellationToken cancellationToken)
    {
        var client = GetGrpcClient();
        await client.ControlLightAsync(new Empty(), _metaData, null, cancellationToken);
    }

    public async Task<ImageResponse> GetImageAsync(ImageRequest request, CancellationToken cancellationToken)
    {
        var client = GetGrpcClient();
        return await client.GetImageAsync(request, _metaData, null, cancellationToken);
    }

    public async Task SendMoveSampleQcRequestAsync(MoveSampleQcRequest request, CancellationToken cancellationToken)
    {
        var client = GetGrpcClient();
        await client.SendMoveSampleQcRequestAsync(request, _metaData, null, cancellationToken);
    }

    public async Task SendUnloadReagentRequestAsync(UnloadReagentRequest request, CancellationToken cancellationToken)
    {
        var client = GetGrpcClient();
        await client.SendUnloadReagentRequestAsync(request, _metaData, null, cancellationToken);
    }

    public async Task<ControlMaintenanceResponse> ControlMaintenanceAsync(ControlMaintenanceRequest request, CancellationToken cancellationToken)
    {
        var client = GetGrpcClient();
        return await client.ControlMaintenanceAsync(request, _metaData, null, cancellationToken);
    }

    //private GrpcChannelOptions CreateChannelOptions()
    //{
    //    var options = new GrpcChannelOptions();

    //    if (_clientCertificate != null)
    //    {
    //        var handler = new HttpClientHandler();
    //        handler.ClientCertificates.Add(_clientCertificate);

    //        options.HttpClient = new HttpClient(handler);
    //    }

    //    return options;
    //}

    //private Channel CreateChannel()
    //{
    //    ChannelCredentials credentials;
    //    if (_clientCertificate != null)
    //    {
    //        // todo: construct custom ChannelCredentials using the client certificate
    //        // (see KeyCertificatePair).
    //        credentials = ChannelCredentials.SecureSsl;
    //    }
    //    else
    //    {
    //        credentials = ChannelCredentials.Insecure;
    //    }

    //    return new Channel(IpAddress.ToString(), Port, credentials);
    //}

    private void StartWaitingForStateChange(ConnectivityState currentState)
    {
        Task.Run(async () =>
        {
            if (_channel == null)
            {
                return;
            }

            await _channel.WaitForStateChangedAsync(currentState);

            var newState = _channel?.State;

            if (newState is null or ConnectivityState.Shutdown or ConnectivityState.Idle)
            {
                RemoteDisconnect?.Invoke(this, SessionId);
            }
            else
            {
                StartWaitingForStateChange(newState.Value);
            }
        });
    }

    public async Task UpdateBottledReagentAsync(UpdateBottledReagentInformation request, CancellationToken cancellationToken)
    {
        var client = GetGrpcClient();
        await client.UpdateBottledReagentAsync(request, _metaData, null, cancellationToken);
    }

    public async Task SetTrayNotUsableAsync(TrayNotUsableRequest request, CancellationToken cancellationToken)
    {
        var client = GetGrpcClient();
        await client.SetTrayNotUsableAsync(request, _metaData, null, cancellationToken);
    }

    public async Task SendPreMeasureSampleTubeVolumeRequest(PreMeasureSampleTubeVolumeRequest request, CancellationToken cancellationToken)
    {
        var client = GetGrpcClient();
        await client.SendPreMeasureSampleTubeVolumeRequestAsync(request, _metaData, null, cancellationToken);
    }

    public async Task<CalculateNeededSampleVolumeResponse> CalculateNeededSampleVolumeAsync(CalculateNeededSampleVolumeRequest request, CancellationToken cancellationToken)
    {
        var client = GetGrpcClient();
        var response = await client.CalculateNeededSampleVolumeAsync(request, _metaData, null, cancellationToken);
        return response;
    }
}
