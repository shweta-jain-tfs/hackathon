using MediatR;
using Tetris.Protocol.Pharos.TestResult;

namespace Pharos.Server.Application.Result;

/// <summary>
/// This command processes an assay result received from an instrument.
/// </summary>
/// <param name="InstrumentNumber">The instrument serial number (used as the instrument's globally unique ID).</param>
/// <param name="Message">The assay result data.</param>
/// <param name="AllowRepeatedMessage">Indicates a special mode when repeated assay results are possible. Only used for FLR-import.</param>
public record ProcessAssayResultCommand(string InstrumentNumber, AssayResult Message, bool AllowRepeatedMessage = false) : IRequest;