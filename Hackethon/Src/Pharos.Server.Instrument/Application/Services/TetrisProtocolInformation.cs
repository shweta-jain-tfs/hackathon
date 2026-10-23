using Pharos.Server.Application;
using Tetris.Protocol.Pharos;

namespace Pharos.Server.Instrument.Application.Services;

internal class TetrisProtocolInformation : ITetrisProtocolInformation
{
    public Version Version
    {
        get
        {
            if (field == null)
            {
                var protocolAssembly = typeof(PharosService).Assembly;
                field = protocolAssembly.GetName().Version ?? throw new InvalidOperationException($"{protocolAssembly.FullName} does not contain the version number.");
            }
            return field;
        }
    }

    public string VersionString => $"{Version.Major}.{Version.Minor}.{Version.Build}";
}