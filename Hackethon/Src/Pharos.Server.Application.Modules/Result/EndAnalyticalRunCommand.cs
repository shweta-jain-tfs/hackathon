using MediatR;

namespace Pharos.Server.Application.Result;

/// <summary>
/// This command ends the specified analytical run.
/// </summary>
public record EndAnalyticalRunCommand(string InstrumentNumber, string MethodName, string AnalyticalRunId, string RunId) : IRequest;