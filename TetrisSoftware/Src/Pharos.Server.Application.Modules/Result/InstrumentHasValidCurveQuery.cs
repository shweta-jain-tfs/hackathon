using MediatR;

namespace Pharos.Server.Application.Result;

public record InstrumentHasValidCurveQuery(string InstrumentNumber, string MethodName, string ConjugateLot, string? CarrierLot, string? CalibratorLot) : IRequest<bool>;
