using System.Security.Claims;
using Pharos.Server.Identity.Domain;

namespace Pharos.Server.Identity.Application.Helpers;
public interface IPermissionProvider
{
    Task<List<Claim>?> GetPermissionClaims(ApplicationRole role);
    List<Claim> GetAllPermissionClaims();
}
