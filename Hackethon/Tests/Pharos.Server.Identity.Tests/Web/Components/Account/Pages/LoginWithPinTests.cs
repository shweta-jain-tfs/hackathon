using System.Security.Claims;
using Bunit;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pharos.Contracts.Security;
using Pharos.Server.Application;
using Pharos.Server.Application.Settings.Security;
using Pharos.Server.Identity.Domain;
using Pharos.Server.Identity.Infrastructure.Authentication;
using Pharos.Server.Identity.Infrastructure.Authorization;
using Pharos.Server.Infrastructure;
using Pharos.Server.Localization;

namespace Pharos.Server.Identity.Web.Components.Account.Pages;

public sealed class LoginWithPinTests : IDisposable
{
    private readonly IdentityDbContext<ApplicationUser, ApplicationRole, Guid> _dbContext;
    private readonly UserManager<ApplicationUser> _identityUserManager;
    private readonly ApplicationUserManager _applicationUserManager;
    private readonly CustomSignInManager _signInManager;
    private readonly Mock<IAuthenticationService> _mockAuthenticationService;
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<IDateTime> _mockDateTime;
    private readonly Mock<ICurrentInstrumentService> _mockCurrentInstrumentService;
    private readonly Mock<IOptionsSnapshot<SecuritySettings>> _mockSecuritySettings;
    private readonly Mock<IPinLockoutService> _mockPinLockoutService;
    private readonly Mock<ILogger<LoginWithPin>> _mockLogger;
    private readonly TestIdentityRedirectManager _redirectManager;
    private readonly MemoryCache _memoryCache;

    public LoginWithPinTests()
    {
        var dbContextOptions = new DbContextOptionsBuilder<IdentityDbContext<ApplicationUser, ApplicationRole, Guid>>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbContext = new IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(dbContextOptions);

#pragma warning disable CA2000
        var userStore = new UserStore<ApplicationUser, ApplicationRole, IdentityDbContext<ApplicationUser, ApplicationRole, Guid>, Guid>(_dbContext);
#pragma warning restore CA2000

        _identityUserManager = new UserManager<ApplicationUser>(
            userStore,
            Options.Create(new IdentityOptions()),
            new PasswordHasher<ApplicationUser>(),
            null!,
            null!,
            null!,
            null!,
            null!,
            Mock.Of<ILogger<UserManager<ApplicationUser>>>());

        _mockSecuritySettings = new Mock<IOptionsSnapshot<SecuritySettings>>();
        _mockSecuritySettings.Setup(x => x.Value).Returns(new SecuritySettings
        {
            EnablePinLogin = true,
            EnablePinLoginLockout = true,
            PinLoginLockoutDurationMinutes = 15,
            EnablePasswordLoginLockout = true,
            PasswordLoginLockoutMaxFailedAttempts = 5,
            PasswordLoginLockoutDurationMinutes = 5,
            PasswordComplexitySettings = new PasswordComplexitySettings
            {
                RequireDigit = true,
                RequireBothUpperAndLowerCase = true,
                RequireNonAlphanumeric = true,
                RequiredLength = 8,
                RequireNoUsernameInPassword = true
            }
        });

        _mockDateTime = new Mock<IDateTime>();
        _mockDateTime.Setup(x => x.Now).Returns(new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero));
        _memoryCache = new MemoryCache(new MemoryCacheOptions());

        _applicationUserManager = new ApplicationUserManager(
            _identityUserManager,
            _mockSecuritySettings.Object,
            _mockDateTime.Object,
            _memoryCache);

        _mockAuthenticationService = new Mock<IAuthenticationService>();
        _mockAuthenticationService
            .Setup(x => x.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties?>()))
            .Returns(Task.CompletedTask);

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(x => x.GetService(typeof(IAuthenticationService)))
            .Returns(_mockAuthenticationService.Object);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProviderMock.Object
        };

        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
        claimsFactory
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new ClaimsPrincipal(new ClaimsIdentity()));

        var schemeProvider = new Mock<IAuthenticationSchemeProvider>();
        var userConfirmation = new Mock<IUserConfirmation<ApplicationUser>>();
        userConfirmation
            .Setup(x => x.IsConfirmedAsync(It.IsAny<UserManager<ApplicationUser>>(), It.IsAny<ApplicationUser>()))
            .ReturnsAsync(true);

        _mockPinLockoutService = new Mock<IPinLockoutService>();
        _mockPinLockoutService.Setup(x => x.IsPinModeEnabled()).Returns(true);
        _mockPinLockoutService.Setup(x => x.IsPinLoginSuspended()).ReturnsAsync(false);

        _signInManager = new CustomSignInManager(
            _identityUserManager,
            httpContextAccessor.Object,
            claimsFactory.Object,
            Options.Create(new IdentityOptions
            {
                Lockout =
                {
                    AllowedForNewUsers = true,
                    MaxFailedAccessAttempts = 5,
                    DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5)
                }
            }),
            Mock.Of<ILogger<SignInManager<ApplicationUser>>>(),
            schemeProvider.Object,
            userConfirmation.Object,
            _applicationUserManager,
            _mockPinLockoutService.Object,
            _mockSecuritySettings.Object);

        _mockMediator = new Mock<IMediator>();
        _mockCurrentInstrumentService = new Mock<ICurrentInstrumentService>();
        _mockCurrentInstrumentService.Setup(x => x.InstrumentId).Returns(123);
        _mockLogger = new Mock<ILogger<LoginWithPin>>();

        _redirectManager = new TestIdentityRedirectManager();
    }

    [Fact]
    public async Task LoginUser_WhenPinLockedOut_RedirectsToLoginPage()
    {
        _mockPinLockoutService.Setup(x => x.IsPinLoginSuspended()).ReturnsAsync(true);

        await using var ctx = CreateTestContext();
        var cut = ctx.Render<LoginWithPin>();

        await cut.InvokeAsync(async () =>
        {
            await SetPrivateProperty(cut.Instance, "Action", "enter");
            await SetInputPin(cut.Instance, "1234");
            await cut.Instance.LoginUser();
        });

        _redirectManager.LastRedirectUri.Should().Be(AccountRoutes.Login);
    }

    [Fact]
    public async Task LoginUser_WhenPinFails_RecordsFailedAttempt()
    {
        _mockPinLockoutService.Setup(x => x.RecordFailedAttempt()).ReturnsAsync(false);

        await using var ctx = CreateTestContext();
        var cut = ctx.Render<LoginWithPin>();

        await cut.InvokeAsync(async () =>
        {
            await SetPrivateProperty(cut.Instance, "Action", "enter");
            await SetInputPin(cut.Instance, "9999");
            await cut.Instance.LoginUser();
        });

        _mockPinLockoutService.Verify(x => x.RecordFailedAttempt(), Times.Once);
    }

    [Fact]
    public async Task LoginUser_WhenPinFailureTriggersLockout_RedirectsToLoginPage()
    {
        _mockPinLockoutService.Setup(x => x.RecordFailedAttempt()).ReturnsAsync(true);

        await using var ctx = CreateTestContext();
        var cut = ctx.Render<LoginWithPin>();

        await cut.InvokeAsync(async () =>
        {
            await SetPrivateProperty(cut.Instance, "Action", "enter");
            await SetInputPin(cut.Instance, "9999");
            await cut.Instance.LoginUser();
        });

        _redirectManager.LastRedirectUri.Should().Be(AccountRoutes.Login);
        _mockPinLockoutService.Verify(x => x.RecordFailedAttempt(), Times.Once);
    }


    [Fact]
    public async Task Render_WhenPinLoginNotEnabled_RedirectsToLogin()
    {
        _mockPinLockoutService.Setup(x => x.IsPinModeEnabled()).Returns(false);

        await using var ctx = CreateTestContext();
        var cut = ctx.Render<LoginWithPin>();

        _redirectManager.LastRedirectUri.Should().Be(AccountRoutes.Login);
    }

    [Fact]
    public async Task Render_WhenInstrumentIdIsNullAndPinEnabled_RedirectsToLogin()
    {
        _mockCurrentInstrumentService.Setup(x => x.InstrumentId).Returns((int?)null);
        _mockPinLockoutService.Setup(x => x.IsPinModeEnabled()).Returns(false);

        await using var ctx = CreateTestContext();
        var cut = ctx.Render<LoginWithPin>();

        _redirectManager.LastRedirectUri.Should().Be(AccountRoutes.Login);
    }

    [Fact]
    public async Task Render_WhenPinLoginSuspended_RedirectsToLogin()
    {
        _mockPinLockoutService.Setup(x => x.IsPinModeEnabled()).Returns(true);
        _mockPinLockoutService.Setup(x => x.IsPinLoginSuspended()).ReturnsAsync(true);

        await using var ctx = CreateTestContext();
        var cut = ctx.Render<LoginWithPin>();

        _redirectManager.LastRedirectUri.Should().Be(AccountRoutes.Login);
    }

    [Fact]
    public async Task LoginUser_WhenPinIsValid_RedirectsToReturnUrl()
    {
        await CreateUserWithPinAsync("pin-success-user", "1234");

        await using var ctx = CreateTestContext();
        var cut = ctx.Render<LoginWithPin>();

        await cut.InvokeAsync(async () =>
        {
            await SetPrivateProperty(cut.Instance, "Action", "enter");
            await SetPrivateProperty(cut.Instance, "ReturnUrl", "/dashboard");
            await SetInputPin(cut.Instance, "1234");
            await cut.Instance.LoginUser();
        });

        _redirectManager.LastRedirectUri.Should().Be("/dashboard");
    }

    [Fact]
    public async Task Render_ShowsActivePasswordLoginLink()
    {
        await using var ctx = CreateTestContext();
        var cut = ctx.Render<LoginWithPin>();

        cut.Find(".login-with-pin__password-link .ph-link").TextContent
            .Should().Be(Translations.LoginWithPin_LoginWithPasswordLink);
        cut.Markup.Should().Contain($"href=\"{AccountRoutes.Login}\"");
    }

    public void Dispose()
    {
        _identityUserManager.Dispose();
        _dbContext.Dispose();
        _memoryCache.Dispose();
    }

    private BunitContext CreateTestContext()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton<IHostEnvironment>(new TestHostEnvironment());
        ctx.Services.AddSingleton(_applicationUserManager);
        ctx.Services.AddSingleton(_signInManager);
        ctx.Services.AddSingleton<SignInManager<ApplicationUser>>(_signInManager);
        ctx.Services.AddSingleton<IIdentityRedirectManager>(_redirectManager);
        ctx.Services.AddSingleton(_mockMediator.Object);
        ctx.Services.AddSingleton(_mockCurrentInstrumentService.Object);
        ctx.Services.AddSingleton(_mockSecuritySettings.Object);
        ctx.Services.AddSingleton(_mockDateTime.Object);
        ctx.Services.AddSingleton(_mockLogger.Object);
        ctx.Services.AddSingleton(_mockPinLockoutService.Object);
        return ctx;
    }

    private static async Task SetPrivateProperty(object instance, string propertyName, string? value)
    {
        var prop = instance.GetType().GetProperty(propertyName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        prop.Should().NotBeNull();
        prop!.SetValue(instance, value);
        await Task.CompletedTask;
    }

    private async Task CreateUserWithPinAsync(string userName, string pin)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            IsActive = true,
            SecurityStamp = Guid.NewGuid().ToString()
        };

        var createResult = await _identityUserManager.CreateAsync(user, "Test@1234");
        createResult.Succeeded.Should().BeTrue();

        var trackedUser = await _applicationUserManager.FindByGuidAsync(user.Id);
        trackedUser.Should().NotBeNull();

        var setPinResult = await _applicationUserManager.SetPinAsync(trackedUser!, pin);
        setPinResult.Succeeded.Should().BeTrue();
    }

    private static async Task SetInputPin(LoginWithPin instance, string pin)
    {
        var inputProp = typeof(LoginWithPin).GetProperty("Input", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        inputProp.Should().NotBeNull();
        var inputValue = inputProp!.GetValue(instance);
        inputValue.Should().NotBeNull();
        var pinProp = inputValue!.GetType().GetProperty("Pin");
        pinProp.Should().NotBeNull();
        pinProp!.SetValue(inputValue, pin);
        await Task.CompletedTask;
    }

    private sealed class TestIdentityRedirectManager : IIdentityRedirectManager
    {
        public string? LastRedirectUri { get; private set; }

        public bool IsPathIdentityPage(PathString path)
        {
            return path.StartsWithSegments("/Account");
        }

        public void RedirectTo(string? uri)
        {
            LastRedirectUri = uri;
        }

        public Task RedirectToRegisterIfNotInitialized(string? returnUrl)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;

        public string ApplicationName { get; set; } = "Pharos.Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
