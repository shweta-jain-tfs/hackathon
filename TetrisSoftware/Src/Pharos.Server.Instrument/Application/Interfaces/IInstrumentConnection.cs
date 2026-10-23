using System.Net;
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

namespace Pharos.Server.Instrument.Application.Interfaces;

/// <summary>
/// Represents an abstraction of a connection from Pharos to an instrument.
/// </summary>
public interface IInstrumentConnection
{

    /// <summary>
    /// Gets the current communication session ID. This ID is sent by the instrument
    /// with every request to Pharos gRPC service.
    /// </summary>
    Guid SessionId { get; }

    /// <summary>
    /// Gets the instrument host IP address
    /// </summary>
    IPAddress IpAddress { get; }

    /// <summary>
    /// Gets the instrument connection port number.
    /// </summary>
    int Port { get; }

    Task ConnectAsync();

    Task DisconnectAsync();

    event EventHandler<Guid> RemoteDisconnect;

    Task SendActiveMethodsAsync(ActiveMethodsRequest request, CancellationToken cancellationToken);
    Task UpdateTestOrderAsync(TestOrderRequest request, CancellationToken cancellationToken);
    Task UpdateSettingsAsync(Settings request, CancellationToken cancellationToken);
    Task UpdateLimitsAsync(Limits request, CancellationToken cancellationToken);
    Task UpdateMethodsAsync(Methods request, CancellationToken cancellationToken);
    Task UpdateMethodStatusAsync(UpdateMethodStatusRequest request, CancellationToken cancellationToken);
    Task UpdateArticlesAsync(Articles request, CancellationToken cancellationToken);
    Task SendQcSchedulingAsync(QcSchedulingRequest request, CancellationToken cancellationToken);
    Task<CmrTestOrderResponse> SendCmrTestOrderAsync(CmrTestOrderRequest request, CancellationToken cancellationToken);
    Task<NewCalCcResponse> OrderNewCalCcAsync(NewCalCcRequest request, CancellationToken cancellationToken);
    Task SendErrorResponseAsync(ErrorResponse request, CancellationToken cancellationToken);
    Task UnlockCompartmentAsync(UnlockCompartmentRequest request, CancellationToken cancellationToken);
    Task<ControlInstrumentStateResponse> ControlInstrumentStateAsync(ControlInstrumentStateRequest request, CancellationToken cancellationToken);
    Task ControlLightAsync(CancellationToken cancellationToken);
    Task ControlSignalTowerAsync(ControlSignalTowerRequest request, CancellationToken cancellationToken);
    Task<ImageResponse> GetImageAsync(ImageRequest request, CancellationToken cancellationToken);
    Task SendMoveSampleQcRequestAsync(MoveSampleQcRequest request, CancellationToken cancellationToken);
    Task SendUnloadReagentRequestAsync(UnloadReagentRequest value, CancellationToken cancellationToken);
    Task<ControlMaintenanceResponse> ControlMaintenanceAsync(ControlMaintenanceRequest request, CancellationToken cancellationToken);
    Task<CurrentCalibrationInformation> SendCalibrationInformationAsync(AvailableCalibrationInformation request, CancellationToken cancellationToken);
    Task UpdateBottledReagentAsync(UpdateBottledReagentInformation request, CancellationToken cancellationToken);
    Task SetTrayNotUsableAsync(TrayNotUsableRequest request, CancellationToken cancellationToken);
    Task SendPreMeasureSampleTubeVolumeRequest(PreMeasureSampleTubeVolumeRequest request, CancellationToken cancellationToken);
    Task<CalculateNeededSampleVolumeResponse> CalculateNeededSampleVolumeAsync(CalculateNeededSampleVolumeRequest request, CancellationToken cancellationToken);
    Task DeleteTestOrderAsync(DeleteTestOrderRequest request, CancellationToken cancellationToken);
}