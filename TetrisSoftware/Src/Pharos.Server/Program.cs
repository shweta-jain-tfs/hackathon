using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting.WindowsServices;
using Pharos.Contracts;
using Pharos.Contracts.Notifications;
using Pharos.Server.Application;
using Pharos.Server.Application.Settings;
using Pharos.Server.Application.Settings.Database;
using Pharos.Server.Application.Settings.General;
using Pharos.Server.Components;
using Pharos.Server.Helpers;
using Pharos.Server.Infrastructure;
using Pharos.Server.Infrastructure.Hubs;
using Pharos.Server.Infrastructure.Logging;
using Pharos.Server.Localization;
using Pharos.Server.Middleware;
using Pharos.Server.Services;
using Pharos.Server.Settings;
using Serilog;

namespace Pharos.Server;

public sealed class Program
{
    private static ConfigurationManager _configuration = null!;
    public static Activity? StartupActivity { get; private set; }
    private static WebApplication? _app;

    public static async Task<int> Main(params string[] args)
    {
        // Bootstrap Serilog early so startup errors are captured before the full configuration is available.
        // Once builder.Build() completes, UseSerilog replaces this with the full host-aware configuration.
        var bootstrapConfig = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(bootstrapConfig)
            .CreateBootstrapLogger();

        try
        {
            var appOptions = new WebApplicationOptions
            {
                Args = args,
                ContentRootPath = WindowsServiceHelpers.IsWindowsService() ? AppContext.BaseDirectory : default,
            };

            var builder = WebApplication.CreateBuilder(appOptions);
            var services = builder.Services;
            _configuration = builder.Configuration;

            builder.Host.UseWindowsService();

            services.AddLocalization();

            // Use Serilog as logger
            builder.Host.ConfigureSerilog();

            List<Assembly> configModules = [Assembly.GetExecutingAssembly(), typeof(GeneralSettings).Assembly, .. Modules.ModuleList.Select(m => m.Assembly)];
            _configuration.AddEntityConfiguration(configModules, typeof(Translations));

            services.Configure<ClientSettings>(options => _configuration.GetSection(ClientSettings.ClientSection).Bind(options));
            services.ConfigureSettings<GeneralSettings>(_configuration);
            services.ConfigureSettings<AutomaticDeleteSettings>(_configuration);

            // Configure cookie policy to enhance security (used only in Production environment)
            services.AddCookiePolicy(options =>
            {
                options.MinimumSameSitePolicy = SameSiteMode.Strict;
                options.HttpOnly = HttpOnlyPolicy.Always;
                options.Secure = CookieSecurePolicy.Always;
                options.OnAppendCookie = ctx =>
                {
                    ctx.CookieOptions.HttpOnly = true;
                    ctx.CookieOptions.Secure = true;
                    ctx.CookieOptions.SameSite = SameSiteMode.Strict;
                };
                options.OnDeleteCookie = ctx =>
                {
                    ctx.CookieOptions.HttpOnly = true;
                    ctx.CookieOptions.Secure = true;
                    ctx.CookieOptions.SameSite = SameSiteMode.Strict;
                };
            });

            services.AddProblemDetails();

            var mvcBuilder = services
                .AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
                    options.JsonSerializerOptions.Converters.Add(new EncodingJsonConverter());
                });

            services.AddRazorComponents()
                .AddInteractiveWebAssemblyComponents();

            // We want to replace the original error messages because they contain the raw input values.
            // This leads to security issues in the Qualys report (although they are false positives).
            builder.Services.Configure<ApiBehaviorOptions>(o =>
            {
                o.InvalidModelStateResponseFactory = ctx =>
                {
                    var errors = ctx.ModelState
                        .Where(e => e.Value!.Errors.Count > 0)
                        .ToDictionary(
                            e => e.Key,
                            e => new[] { $"Invalid value for {e.Key}." });

                    var pd = new ValidationProblemDetails(errors)
                    {
                        Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                        Status = StatusCodes.Status400BadRequest
                    };
                    return new ObjectResult(pd) { StatusCode = pd.Status };
                };
            });

            services.AddSingleton<IPharosVersionService, PharosVersionService>();
            services.AddSingleton<IOperatingSystemInformation, OperatingSystemInformation>();
            services.AddScoped<IClientDeviceService, ClientDeviceService>();

            services.AddModuleServices(_configuration, mvcBuilder);
            services.AddInfrastructureServices(_configuration);
            ConfigurationManagerExtensions.InitializeSettingsPages(configModules);

            services.AddHostedService<StartupTracker>();

#if NSWAG || DEBUG
            services.AddOpenApiDocument();
#endif

            _app = builder.Build();

            var isDevelopment = _app.Environment.IsDevelopment();

#if !NSWAG
            // Production/Installer: Support database initialization command to be run separately from application startup
            if (!isDevelopment && args.Any(a => string.Equals(a, "--init-db", StringComparison.OrdinalIgnoreCase)))
            {
                await _app.Services.InitializeDatabase();
                return 0;
            }
#endif
            LogStartupInfo();

#if !NSWAG
            // Development: For developer convenience, initialize database on startup
            if (isDevelopment)
            {
                await _app.Services.InitializeDatabase();
            }
#endif
            if (!isDevelopment)
            {
                _app.UseCookiePolicy();

                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                _app.UseHsts();

                // Add security headers middleware for production only
                AddSecurityHeaders(_app);
            }

            _app.UseSerilogRequestLogging();
            _app.UseExceptionHandler(errorApp => errorApp.Run(ProblemDetailsMiddleware.HandleHttpException));

            _app.UseStaticFiles();
#if !NSWAG
            _app.MapStaticAssets(); // Breaks default antiforgery token handling in webassembly client, but needed for cache busting
#endif

            _app.UseRouting();

#if !NSWAG
            var supportedCultures = UICultureHelper.GetSupportedCultures();
            var localizationOptions = new RequestLocalizationOptions()
                .SetDefaultCulture(UICultureHelper.GetDefaultCulture())
                .AddSupportedCultures(supportedCultures)
                .AddSupportedUICultures(supportedCultures);

            _app.UseRequestLocalization(localizationOptions);
#endif

            // Configure the HTTP request pipeline.
            if (isDevelopment)
            {
                _app.UseWebAssemblyDebugging();
                _app.UseMigrationsEndPoint();

#if DEBUG
                // Only enable Swagger UI in DEBUG builds
                _app.UseOpenApi();
                _app.UseSwaggerUi(settings =>
                {
                    // Sort tags alphabetically in the Swagger UI navigation
                    settings.TagsSorter = "alpha";
                });
#endif
            }

            _app.UseAuthentication();
            _app.UseAuthorization();

            _app.UseAntiforgery();

            _app.MapControllers();
            _app.MapHub<HealthHub>(HubRoutes.Health);
            _app.MapHub<JobHub>(HubRoutes.Job);

#if !NSWAG
            var endpointConventionBuilder = _app.MapRazorComponents<App>()
                .AllowAnonymous()
                .AddInteractiveWebAssemblyRenderMode()
                // We cannot get assemblies based on the client device type here,
                // because this code only runs once on startup, not per request.
                .AddAdditionalAssemblies(Client.Modules.GetAllAssemblies());

            _app.UseModules(endpointConventionBuilder);

            _app.MapFallback(async context =>
            {
                var problemDetails = ProblemDetailsMiddleware.CreateNotFound();
                await context.WriteProblemDetailsAsync(problemDetails);
            });
#endif

            await _app.RunAsync();

            return 0;
        }
        catch (Exception e)
        {
            Log.ForContext<Program>().Fatal(e, "Application terminated unexpectedly during startup.");

            return 1;
        }
        finally
        {
            StartupActivity?.Dispose();
            await Log.CloseAndFlushAsync();
        }
    }

    /// <summary>
    /// Adds security headers middleware to protect against common web vulnerabilities.
    /// Only called in production environment.
    /// </summary>
    /// <param name="app">The web application instance</param>
    private static void AddSecurityHeaders(WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            // Use OnStarting callback to ensure headers are set at the correct time
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;

                // Prevent clickjacking attacks
                headers.Append("X-Frame-Options", "DENY");

                // Prevent MIME type sniffing
                headers.Append("X-Content-Type-Options", "nosniff");

                // Enable XSS protection (legacy browsers)
                headers.Append("X-XSS-Protection", "1; mode=block");

                // Control referrer information
                headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

                // Content Security Policy - production configuration
                headers.Append("Content-Security-Policy",
                    "default-src 'self'; " +
                    "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
                    "style-src 'self' 'unsafe-inline'; " +
                    "img-src 'self' data:; " +
                    "connect-src 'self'; " +  // Strict for production (blocks Browser Link used in development)
                    "font-src 'self'; " +
                    "frame-ancestors 'none'");

                return Task.CompletedTask;
            });

            await next();
        });
    }

    private static void LogStartupInfo()
    {
        StartupActivity = new Activity("Application startup");
        StartupActivity.Start();

        var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        if (string.IsNullOrWhiteSpace(environmentName))
        {
            environmentName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        }

        if (string.IsNullOrWhiteSpace(environmentName))
        {
            environmentName = "Production";
        }

        Log.ForContext<Program>()
            .ForContext("EnvironmentName", environmentName)
            .ForContext("StartupInfo",
                new
                {
                    Application = typeof(Program).Assembly.GetName().Name,
                    Version = typeof(Program).Assembly.GetName().Version?.ToString(),
                    AssemblyPath = Assembly.GetExecutingAssembly().Location,
                    LastModified = File.GetLastWriteTime(typeof(Program).Assembly.Location),
                    BuildConfig = WindowsServiceHelpers.IsWindowsService() ? "Windows Service" : "Console App",
                    Hostname = Environment.MachineName
                },
                destructureObjects: true)
            .Information("Starting application.");

    }
}
