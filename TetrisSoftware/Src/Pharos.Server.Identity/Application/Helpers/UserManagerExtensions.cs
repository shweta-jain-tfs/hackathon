using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Pharos.Server.Identity.Domain;

namespace Pharos.Server.Identity.Application.Helpers;

internal static class UserManagerExtensions
{
    public static Task<bool> AreUsersInitialized(this UserManager<ApplicationUser> userManager)
    {
        return userManager.Users.AnyAsync();
    }
}
