using MediatR;
using Tetris.Protocol.Pharos.TestResult;

namespace Pharos.Server.Application.Result;

/// <summary>
/// This command processes a reagent blank result from an instrument.
/// </summary>
/// <param name="InstrumentNumber">The instrument serial number (used as the instrument's globally unique ID).</param>
/// <param name="Message">The blank result data.</param>
/// <param name="AllowRepeatedMessage">Indicates a special mode when repeated blank results are possible. Only used for FLR-import.</param>
public record ProcessBlankResultCommand(string InstrumentNumber, BlankResult Message, bool AllowRepeatedMessage = false) : IRequest;