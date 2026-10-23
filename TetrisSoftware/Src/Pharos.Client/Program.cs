using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Pharos.Client;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.App.Services;
using Pharos.Client.Common.App.ViewModels;
using Pharos.Client.Common.Models.DataServices;
using Pharos.Client.Common.Models.Services;
using Pharos.Client.Models;
using Pharos.Client.Services;
using Pharos.Client.ViewModels;
using Pharos.Common.Web;
using Pharos.Common.Web.Components.Loader;
using Pharos.Contracts.Security;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

//From Blazor Template
builder.Services.AddAuthorizationCore(options =>
{
    //AUTH: create a policy for each permission
    foreach (var permission in Permissions.All)
    {
        options.AddPolicy(permission, policy => policy.RequireClaim(Permissions.ClaimType, permission));
    }

    options.AddPolicy(
        Policies.ServiceEngineerOrRndMode,
        policy => policy.RequireClaim(Permissions.ClaimType, Policies.ServiceEngineerOrRndMode));
});
builder.Services.AddCascadingAuthenticationState();
//End from Blazor Template

//// **** Pharos stuff below ****
builder.Services.AddSingleton<ClientDeviceService, ClientDeviceService>();
builder.Services.AddSingleton<IClientDeviceService>(f => f.GetRequiredService<ClientDeviceService>());
builder.Services.AddSingleton<IDateTime, DateTimeService>();
builder.Services.AddSingleton<TimerService>();

builder.Services.AddScoped<AuthorizationHttpMessageHandler>();
builder.Services.AddScoped<FormatService>();
builder.Services.AddScoped<ISingletonDataSource<UiSettings>, UiSettingsDataSource>();

builder.Services.AddScoped<UiSettingsService>();
builder.Services.AddScoped<CultureService>();

builder.Services.AddCommonViewModels();
builder.Services.AddCommonWebProviders();
builder.Services.AddCommonAppServices();
builder.Services.AddModuleServices();

builder.Services.AddTransient<AboutViewModel>();
builder.Services.AddTransient<LockScreenViewModel>();

// SignalR connections
builder.Services.AddNotificationProviders();

// Register Http Client (logged in)
builder.Services.AddHttpClient("Pharos.WebApp")
    .ConfigureHttpClient(client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
    .AddHttpMessageHandler<AuthorizationHttpMessageHandler>()
    .AddHttpMessageHandler<ConnectivityHttpMessageHandler>()
    .AddHttpMessageHandler<DisplayLoaderAutomaticallyHttpMessageHandler>();

builder.Services.AddScoped(provider => provider.GetRequiredService<IHttpClientFactory>().CreateClient("Pharos.WebApp"));


builder.Services.AddLocalization();
builder.Services.AddBlazoredLocalStorage();

var host = builder.Build();

var cultureService = host.Services.GetRequiredService<CultureService>();
await cultureService.SetCultureAsync();

await host.RunAsync();
