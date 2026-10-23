using System.Net;

namespace Pharos.Server.Application.Instrument;

public interface IInstrumentManager
{
    ICollection<IInstrument> GetInstruments();
    IInstrument GetInstrument(int instrumentId);
    IInstrument GetInstrument(string instrumentNumber);
    IInstrument? FindInstrument(string? instrumentNumber);
    IInstrument? FindInstrument(IPAddress ipAddress);
}