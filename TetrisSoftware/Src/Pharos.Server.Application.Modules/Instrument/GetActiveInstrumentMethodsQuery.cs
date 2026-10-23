using MediatR;

namespace Pharos.Server.Application.Instrument;

/// <summary>
/// Use this query to get the names of the active assay methods for an instrument.
/// </summary>
public record GetActiveInstrumentMethodsQuery(string InstrumentNumber) : IRequest<HashSet<string>>;
