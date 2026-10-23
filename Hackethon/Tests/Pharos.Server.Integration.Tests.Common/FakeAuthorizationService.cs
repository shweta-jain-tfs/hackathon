using Pharos.Server.Application;
using Pharos.Server.Application.Security;

namespace Pharos.Server.Integration.Tests.Common;

internal class FakeAuthorizationService : IAuthorizationService
{
    private static readonly Dictionary<Guid, HashSet<string>> _userPermissions = [];
    public static bool DefaultAuthorizationResult { get; set; } = true;
    public static bool RndModeEnabled { get; set; }

    private readonly ICurrentUserService _currentUserService;

    public FakeAuthorizationService(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public static void Reset()
    {
        _userPermissions.Clear();
        DefaultAuthorizationResult = true;
        RndModeEnabled = false;
    }

    public static void SetPermissions(Guid userId, params string[] permissions)
    {
        _userPermissions[userId] = permissions.ToHashSet(StringComparer.Ordinal);
    }

    public Task<bool> AuthorizeCurrentUserAsync(params string[] permissions)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return Task.FromResult(DefaultAuthorizationResult);
        }

        if (!_userPermissions.TryGetValue(userId.Value, out var grantedPermissions))
        {
            return Task.FromResult(DefaultAuthorizationResult);
        }

        if (permissions.Length == 0)
        {
            return Task.FromResult(true);
        }

        return Task.FromResult(permissions.All(grantedPermissions.Contains));
    }

    public Task<bool> AuthorizeUserAsync(Guid userId, params string[] permissions)
    {
        if (!_userPermissions.TryGetValue(userId, out var grantedPermissions))
        {
            return Task.FromResult(DefaultAuthorizationResult);
        }

        if (permissions.Length == 0)
        {
            return Task.FromResult(true);
        }

        return Task.FromResult(permissions.All(grantedPermissions.Contains));
    }

    public Task<bool> IsCurrentUserInRoleAsync(string role)
    {
        return Task.FromResult(true);
    }

    public bool RndMode => RndModeEnabled;

    public bool RuoMode => false;
}