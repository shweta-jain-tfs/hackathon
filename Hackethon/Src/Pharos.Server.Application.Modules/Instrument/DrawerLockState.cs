using Pharos.Server.Domain.Enums;

namespace Pharos.Server.Application.Instrument;

public record DrawerLockState(DrawerType Drawer, LockState LockState, bool IsUnlockInProgress, DateTimeOffset? OpenedAt = null);