using MediatR;

namespace Pharos.Server.Application.Instrument;

public record OrderNewCalCcCommand(string InstrumentNumber, bool IsCalibrator, string MethodName, string ConjugateLot, string CarrierLot, string? CalibratorCurveControlLot = null) : IInstrumentCommand, IRequest;
