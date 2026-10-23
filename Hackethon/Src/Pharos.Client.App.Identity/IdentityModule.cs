using System.Reflection;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Client.App.Identity.Models;
using Pharos.Client.App.Identity.Services;
using Pharos.Client.App.Identity.ViewModels;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.Models.Notifications;

namespace Pharos.Client.App.Identity;

public class IdentityModule : IClientAppModule
{
    private IdentityModule()
    {
    }

    public static IClientAppModule Instance { get; } = new IdentityModule();

    public Assembly Assembly => Assembly.GetExecutingAssembly();

    void IClientAppModule.AddServices(IServiceCollection services)
    {
        services.AddSingleton<ICurrentUserNotificationProvider, CurrentUserNotificationProvider>();

        // Must be Scoped: depends on scoped services (e.g., HttpClient) and holds per-user auth state.
        services.AddScoped<AuthenticationStateProvider, RefreshingAuthenticationStateProvider>();
        services.AddScoped<IAuthorizationService, AuthorizationService>();
        services.AddScoped<IAutoLogoutService, AutoLogoutService>();

        services.AddScoped<IApplicationUserDataService, ApplicationUserDataService>();
        services.AddScoped<IRoleDataService, RoleDataService>();

        services.AddScoped<ISingletonDataSource<PharosInformation>, PharosInformationDataSource>();
        services.AddScoped<ISingletonDataSource<ModuleInformation>, PharosModuleInformationDataSource>();

        services.AddTransient<ApplicationUsersListViewModel>();
        services.AddTransient<ApplicationUserDetailsPageViewModel>();
        services.AddTransient<UserAccountPageViewModel>();
        services.AddTransient<RoleListViewModel>();
        services.AddTransient<RoleDetailsPageViewModel>();
    }
}
