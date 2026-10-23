using System.Reflection;
using System.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Pharos.Contracts.Notifications;
using Pharos.Contracts.Security;
using Pharos.Server.Application;
using Pharos.Server.Application.Settings;
using Pharos.Server.Application.Settings.RnD;
using Pharos.Server.Application.Settings.Security;
using Pharos.Server.Identity.Application.Helpers;
using Pharos.Server.Identity.Application.Interfaces;
using Pharos.Server.Identity.Application.Services;
using Pharos.Server.Identity.Domain;
using Pharos.Server.Identity.Infrastructure.Authentication;
using Pharos.Server.Identity.Infrastructure.Authorization;
using Pharos.Server.Identity.Infrastructure.Database;
using Pharos.Server.Identity.Infrastructure.Hubs;
using Pharos.Server.Identity.Web.Components.Account;
using Pharos.Server.Infrastructure;
using Pharos.Server.Infrastructure.Database;


[assembly: NeutralResourcesLanguage("en-US")]

namespace Pharos.Server.Identity;

public class IdentityModule : IServerAppModule
{
    public const string ApplicationCookieName = ".AspNetCore.Identity.Application";
    public static readonly TimeSpan ApplicationCookieExpireTimeSpan = TimeSpan.FromMinutes(12 * 60);

    private IdentityModule()
    {
    }

    public static IServerAppModule Instance { get; } = new IdentityModule();

    public Assembly Assembly => Assembly.GetExecutingAssembly();

    public void AddServices(IServiceCollection services, IConfiguration configuration, object? mvcBuilder)
    {
        services.ConfigureSettings<RndSettings>(configuration);
        services.ConfigureSettings<SecuritySettings>(configuration);

        services.AddDbContext<IdentityDbContext>();
        services.AddScoped<IIdentityDbContext>(provider => provider.GetRequiredService<IdentityDbContext>());

        services.AddScoped<IdentityDbContextInitializer>();

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredUniqueChars = 0;

                options.SignIn.RequireConfirmedAccount = false;
                options.SignIn.RequireConfirmedEmail = false;
                options.SignIn.RequireConfirmedPhoneNumber = false;

                options.User.AllowedUserNameCharacters = UserContract.AllowedNameCharacters;
                options.User.RequireUniqueEmail = false;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<IdentityDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.AddOptions<IdentityOptions>()
            .Configure<IOptionsMonitor<SecuritySettings>>((options, securitySettingsMonitor) =>
            {
                var securitySettings = securitySettingsMonitor.CurrentValue;
                options.Lockout.AllowedForNewUsers = securitySettings.EnablePasswordLoginLockout;
                options.Lockout.MaxFailedAccessAttempts = securitySettings.PasswordLoginLockoutMaxFailedAttempts;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(securitySettings.PasswordLoginLockoutDurationMinutes);
            });

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = ApplicationCookieName;
            options.ExpireTimeSpan = ApplicationCookieExpireTimeSpan; // set timeout
            options.SlidingExpiration = true;
            options.Cookie.HttpOnly = true; // security best practice
            options.Cookie.IsEssential = true; // ensure it's always stored
        });

        services.AddSingleton<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IAuthorizationPolicyProvider, PharosPolicyProvider>();
        services.AddScoped<IApplicationRoleManager, ApplicationRoleManager>();
        services.AddScoped<IApplicationUserManager, ApplicationUserManager>();
        services.AddScoped<Server.Application.Security.IAuthorizationService, AuthorizationService>();
        services.AddScoped<IAuthorizationHandler, PharosPolicyAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, ActiveUserAuthorizationHandler>();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, AuthorizationMiddlewareResultHandler>();

        services.AddCascadingAuthenticationState();
        services.AddScoped<IIdentityRedirectManager, IdentityRedirectManager>();
        services.AddSingleton<IChangePasswordTicketService, ChangePasswordTicketService>();
        services.AddScoped<AuthenticationStateProvider, PersistingServerAuthenticationStateProvider>();
        services.AddScoped<IPermissionProvider, PermissionProvider>();


        services.AddIdentityApiEndpoints<ApplicationUser>();
        services.AddAuthorization();

        services.AddScoped<SignInManager<ApplicationUser>, CustomSignInManager>();
        services.AddScoped<IPinLockoutService, PinLockoutService>();
        services.AddScoped<ICurrentUserServerHub, CurrentUserHub>();


        // register web api controllers from this assembly
        //(mvcBuilder as IMvcBuilder)?.AddApplicationPart(Assembly);
    }

    public void UseModule(IHost webApplication, object endpointConventionBuilder)
    {
        var app = (WebApplication)webApplication;
        app.MapAdditionalIdentityEndpoints();
        app.MapHub<CurrentUserHub>(HubRoutes.CurrentUser);

        ((RazorComponentsEndpointConventionBuilder)endpointConventionBuilder).AddAdditionalAssemblies(Assembly);
    }

    public IDbContextInitializer GetDbContextInitializer(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<IdentityDbContextInitializer>();
    }
}
