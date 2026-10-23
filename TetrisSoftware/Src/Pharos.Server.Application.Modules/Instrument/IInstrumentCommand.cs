namespace Pharos.Server.Application.Instrument;

/// <summary>
/// A marker interface to represent a request sent to the instrument.
/// </summary>
public interface IInstrumentCommand
{
    /// <summary>
    /// The instrument number of the instrument that the command is for.
    /// </summary>
    string InstrumentNumber { get; }
}