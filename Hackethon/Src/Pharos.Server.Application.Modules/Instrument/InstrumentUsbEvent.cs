using MediatR;
using Tetris.Protocol.Pharos.Files;

namespace Pharos.Server.Application.Instrument;

/// <summary>
/// This message is published when an instrument detects that the user has inserted or removed
/// a USB memory drive or hard drive into the instrument's USB port.
/// </summary>
public record InstrumentUsbEvent(string InstrumentNumber, UsbEvent UsbEvent) : INotification;
