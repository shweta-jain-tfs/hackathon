using MediatR;

namespace Pharos.Server.Application.Result;

/// <summary>
/// This command ends the specified assay run.
/// </summary>
public record EndAssayRunCommand(string InstrumentNumber, string AssayRunId, string CustomRunId) : IRequest;