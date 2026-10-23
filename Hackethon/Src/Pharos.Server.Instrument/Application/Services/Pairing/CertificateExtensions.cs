using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Pharos.Server.Instrument.Application.Services.Pairing;

internal static class CertificateExtensions
{
    private const string _instrumentSanPrefix = "urn:thermofisher:phadia-e:instrument:";

    internal static string GetInstrumentNumber(this X509Certificate2 certificate)
    {
        var san = certificate.Extensions["2.5.29.17"];
        if (san is null)
        {
            return "";
        }

        var sanText = Encoding.ASCII.GetString(san.RawData);
        var serialStart = sanText.IndexOf(_instrumentSanPrefix, StringComparison.OrdinalIgnoreCase);
        if (serialStart < 0)
        {
            return "";
        }

        var instrumentNumber = new string(sanText[(serialStart + _instrumentSanPrefix.Length)..]
            .TakeWhile(character => character is >= ' ' and <= '~')
            .ToArray());
        return instrumentNumber;
    }
}
