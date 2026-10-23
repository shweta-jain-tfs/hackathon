using System.Net;
using Moq;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Domain.Enums;
using Tetris.Protocol.Pharos;
using InstrumentState = Pharos.Server.Domain.Enums.InstrumentState;

namespace Pharos.Server.Integration.Tests.Common;

public sealed class TestInstrument(string serialNumber, InstrumentType instrumentType, string? displayName, string? instrumentSoftwareVersion = null, InstrumentState? state = null, DateTimeOffset? installationDate = null) : IInstrument
{
    public int Id { get; init; }
    public string SerialNumber { get; } = serialNumber;
    public InstrumentType InstrumentType { get; } = instrumentType;
    public string? DisplayName { get; } = displayName;
    public bool Enabled { get; init; }
    public IPAddress IpAddress { get; init; } = IPAddress.None;
    public string? InstrumentSoftwareVersion { get; } = instrumentSoftwareVersion;
    public DateTimeOffset? InstallationDate { get; } = installationDate;
    public InstrumentState State { get; init; } = state ?? default;
    public bool IsInAssayMode { get; init; }
    public bool IsInMaintenanceMode => false;
    public bool CanStartMaintenance => false;
    public IReadOnlyList<HardwareModule>? Modules { get; set; }
    public IDrawerLockStateList DrawersLockState { get; init; } = Mock.Of<IDrawerLockStateList>();
    public IReadOnlyList<ContinuousSensor> EnvironmentSensors { get; init; } = [];
}
