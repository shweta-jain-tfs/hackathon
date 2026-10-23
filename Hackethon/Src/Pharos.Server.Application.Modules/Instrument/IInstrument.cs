using System.Net;
using Pharos.Server.Domain.Enums;
using Tetris.Protocol.Pharos;
using InstrumentState = Pharos.Server.Domain.Enums.InstrumentState;

namespace Pharos.Server.Application.Instrument;

public interface IInstrument
{
    int Id { get; }
    string SerialNumber { get; }
    InstrumentType InstrumentType { get; }
    string? DisplayName { get; }
    bool Enabled { get; }
    IPAddress IpAddress { get; }
    string? InstrumentSoftwareVersion { get; }
    DateTimeOffset? InstallationDate { get; }
    InstrumentState State { get; }
    bool IsInAssayMode { get; }
    bool IsInMaintenanceMode { get; }
    bool CanStartMaintenance { get; }
    IReadOnlyList<HardwareModule>? Modules { get; }
    IDrawerLockStateList DrawersLockState { get; }
    IReadOnlyList<ContinuousSensor> EnvironmentSensors { get; }
}

public interface IDrawerLockStateList : IReadOnlyList<DrawerLockState>
{
    DrawerLockState this[DrawerType drawerType] { get; }
}