using BlazingStory.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pharos.Common.Web;
using Pharos.Stories.Components.Pages;
using Pharos.Stories.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var serverUrl = builder.Configuration[WebHostDefaults.ServerUrlsKey]?.Split(';').FirstOrDefault() ?? "http://localhost:5211";
builder.Services.TryAddScoped(sp => new HttpClient { BaseAddress = new Uri(serverUrl) });
builder.Services.AddSingleton<ThemeService>();
builder.Services.AddSingleton<AuthenticationStateProvider, MockAuthStateProvider>();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddCommonWebProviders();

builder.Services.AddSingleton<ThemeService>();
builder.Host.UseWindowsService();

var persistKeys = builder.Configuration.GetValue<bool>("DataProtection:PersistKeys");
var keyFolder = builder.Configuration.GetValue<string>("DataProtection:KeyFolder");
var appName = builder.Configuration.GetValue<string>("DataProtection:ApplicationName");

if (persistKeys && !string.IsNullOrEmpty(keyFolder) && !string.IsNullOrEmpty(appName))
{
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(keyFolder))
        .SetApplicationName(appName);
}
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

// app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAntiforgery();

app.MapRazorComponents<BlazingStoryServerComponent<IndexPage, IFramePage>>()
    .AddInteractiveServerRenderMode();

app.Run();