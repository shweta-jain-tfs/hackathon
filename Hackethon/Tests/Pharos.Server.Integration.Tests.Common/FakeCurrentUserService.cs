using Pharos.Server.Application;

namespace Pharos.Server.Integration.Tests.Common;

public class FakeCurrentUserService : ICurrentUserService
{
    public static Guid? CurrentUserId { get; set; } = Guid.NewGuid();

    public Guid? UserId => CurrentUserId;
}