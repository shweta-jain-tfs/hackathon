using Microsoft.AspNetCore.Http;

namespace Pharos.Server.Infrastructure;

public interface IIdentityRedirectManager
{
    bool IsPathIdentityPage(PathString path);

    void RedirectTo(string? uri);

    Task RedirectToRegisterIfNotInitialized(string? returnUrl);
}