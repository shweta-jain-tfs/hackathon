using Pharos.Server.Domain.Enums;

namespace Pharos.Server.Sample.Domain;

internal class SampleDrawer
{
    public DrawerType DrawerType { get; set; }

    public bool ScanningOngoing { get; set; }

    public List<SampleTray> Trays { get; set; } = [];
}
