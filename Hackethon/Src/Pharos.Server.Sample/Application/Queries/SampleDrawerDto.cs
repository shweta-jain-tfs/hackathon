using Pharos.Server.Domain.Enums;

namespace Pharos.Server.Sample.Application.Queries;

public class SampleDrawerDto
{
    public DrawerType Drawer { get; set; }
    public LockState LockState { get; set; }
    public bool IsUnlockInProgress { get; set; }
    public bool IsScanOngoing { get; set; }
    public List<SampleTrayDetailsDto> SampleTrays { get; set; } = default!;
}
