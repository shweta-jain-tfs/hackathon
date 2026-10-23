using System.Security.Claims;
using Pharos.Contracts.Security;
using Pharos.Server.Identity.Domain;
using Pharos.Server.Identity.Infrastructure.Authorization;

namespace Pharos.Server.Identity.Application.Helpers
{
    internal class PermissionProvider : IPermissionProvider
    {
        private readonly IApplicationRoleManager _manager;

        public PermissionProvider(IApplicationRoleManager manager)
        {
            _manager = manager;
        }

        public async Task<List<Claim>?> GetPermissionClaims(ApplicationRole role)
        {
            if (string.IsNullOrWhiteSpace(role.Name))
            {
                return [];
            }

            var applicationRole = await _manager.FindByNameAsync(role.Name).ConfigureAwait(false);
            if (applicationRole == null)
            {
                return [];
            }

            var permissions = (await _manager.GetClaimsAsync(applicationRole).ConfigureAwait(false))
                .Where(x => x.Type == Permissions.ClaimType)
                .ToList();

            return permissions.Select(p => p).ToList();
        }
        public List<Claim> GetAllPermissionClaims()
        {
            return Permissions.All
            .Select(permission => new Claim(Permissions.ClaimType, permission)).ToList();

        }
    }
}