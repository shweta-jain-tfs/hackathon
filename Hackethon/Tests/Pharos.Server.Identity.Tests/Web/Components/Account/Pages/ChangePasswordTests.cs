using System.Reflection;
using System.Security.Claims;
using Bunit;
using MediatR;
using Microsoft.AspNetCore.Antiforgery;
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
using Pharos.Contracts;
using Pharos.Contracts.Security;
using Pharos.Server.Application;
using Pharos.Server.Application.Identity;
using Pharos.Server.Application.Settings.Security;
using Pharos.Server.Identity.Domain;
using Pharos.Server.Identity.Infrastructure.Authentication;
using Pharos.Server.Identity.Infrastructure.Authorization;
using Pharos.Server.Infrastructure;
using Pharos.Server.Localization;

namespace Pharos.Server.Identity.Web.Components.Account.Pages;

public sealed class ChangePasswordTests : IDisposable
{
    private readonly IdentityDbContext<ApplicationUser, ApplicationRole, Guid> _dbContext;
    private readonly UserManager<ApplicationUser> _identityUserManager;
    private readonly ApplicationUserManager _applicationUserManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly Mock<IAuthenticationService> _mockAuthenticationService;
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<IDateTime> _mockDateTime;
    private readonly Mock<IClientDeviceService> _mockClientDeviceService;
    private readonly Mock<ICurrentInstrumentService> _mockCurrentInstrumentService;
    private readonly Mock<IOptionsSnapshot<SecuritySettings>> _mockSecuritySettings;
    private readonly Mock<ILogger<ChangePassword>> _mockLogger;
    private readonly ChangePasswordTicketService _changePasswordTicketService;
    private readonly MemoryCache _memoryCache;
    private readonly TestIdentityRedirectManager _redirectManager;

    public ChangePasswordTests()
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
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!,
            Mock.Of<ILogger<UserManager<ApplicationUser>>>());
        _identityUserManager.RegisterTokenProvider(TokenOptions.DefaultProvider, new TestPasswordResetTokenProvider());

        _mockSecuritySettings = new Mock<IOptionsSnapshot<SecuritySettings>>();
        _mockSecuritySettings.Setup(x => x.Value).Returns(new SecuritySettings
        {
            EnablePinLogin = false,
            PasswordComplexitySettings = new PasswordComplexitySettings
            {
                RequireDigit = true,
                RequireBothUpperAndLowerCase = true,
                RequireNonAlphanumeric = true,
                RequiredLength = 8,
                RequireNoUsernameInPassword = true
            }
        });

        _memoryCache = new MemoryCache(Options.Create(new MemoryCacheOptions()));
        _mockDateTime = new Mock<IDateTime>();
        _mockDateTime.Setup(x => x.Now).Returns(new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero));

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

        _signInManager = new SignInManager<ApplicationUser>(
            _identityUserManager,
            httpContextAccessor.Object,
            claimsFactory.Object,
            Options.Create(new IdentityOptions()),
            Mock.Of<ILogger<SignInManager<ApplicationUser>>>(),
            schemeProvider.Object,
            userConfirmation.Object);

        _mockMediator = new Mock<IMediator>();
        _mockClientDeviceService = new Mock<IClientDeviceService>();
        _mockClientDeviceService.Setup(x => x.ClientDeviceType).Returns(ClientDeviceType.Desktop);
        _mockCurrentInstrumentService = new Mock<ICurrentInstrumentService>();
        _mockLogger = new Mock<ILogger<ChangePassword>>();
        _changePasswordTicketService = new ChangePasswordTicketService(_mockDateTime.Object);

        _redirectManager = new TestIdentityRedirectManager();
    }

    [Fact]
    public async Task OnInitialized_WhenNoTicketsProvided_RedirectsToLogin()
    {
        await using var ctx = CreateTestContext();

        _ = ctx.Render<ChangePassword>(parameters =>
            parameters.Add(x => x.Username, "any-user"));

        _redirectManager.LastRedirectUri.Should().Be(AccountRoutes.Login);
    }

    [Fact]
    public async Task OnInitialized_WhenRetryTicketIsReplayed_RedirectsToLogin()
    {
        var leakedRetryTicket = _changePasswordTicketService.CreateTicket("any-user", ChangePasswordTicketType.Retry);

        await using var ctx = CreateTestContext();
        var cut = ctx.Render<ChangePassword>(parameters =>
            parameters.Add(x => x.Username, "any-user"));
        _redirectManager.Reset();

        cut.Instance.RetryTicket = leakedRetryTicket;
        await InvokeOnInitializedAsync(cut.Instance);

        cut.Instance.RetryTicket.Should().NotBeNullOrWhiteSpace();
        cut.Instance.RetryTicket.Should().NotBe(leakedRetryTicket);
        _redirectManager.LastRedirectUri.Should().BeNull();

        cut.Instance.RetryTicket = leakedRetryTicket;
        await InvokeOnInitializedAsync(cut.Instance);

        cut.Instance.RetryTicket.Should().Be(leakedRetryTicket);
        _redirectManager.LastRedirectUri.Should().Be(AccountRoutes.Login);
    }

    [Fact]
    public async Task OnSubmit_WhenPasswordIsEmpty_SetsRequiredFieldError()
    {
        await using var ctx = CreateTestContext();
        var cut = ctx.Render<ChangePassword>(parameters =>
            parameters.Add(x => x.Username, "any-user"));
        _redirectManager.Reset();

        await cut.InvokeAsync(async () =>
        {
            cut.Instance.NewPassword = "";
            cut.Instance.ConfirmationPassword = "any";
            await cut.Instance.OnSubmit();
        });
        cut.Render();

        cut.Markup.Should().Contain(Translations.PasswordRequired);
        _mockAuthenticationService.Verify(
            x => x.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties?>()),
            Times.Never);
    }

    [Fact]
    public async Task OnSubmit_WhenPasswordsDoNotMatch_ClearsPasswordsAndSetsMismatchError()
    {
        await using var ctx = CreateTestContext();
        var cut = ctx.Render<ChangePassword>(parameters =>
            parameters.Add(x => x.Username, "any-user"));
        _redirectManager.Reset();

        await cut.InvokeAsync(async () =>
        {
            cut.Instance.NewPassword = "ValidPass1!";
            cut.Instance.ConfirmationPassword = "DifferentPass1!";
            await cut.Instance.OnSubmit();
        });
        cut.Render();

        cut.Instance.NewPassword.Should().BeEmpty();
        cut.Instance.ConfirmationPassword.Should().BeEmpty();
        cut.Markup.Should().Contain(Translations.ChangePassword_PasswordsDoNotMatch);
        _mockAuthenticationService.Verify(
            x => x.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties?>()),
            Times.Never);
    }

    [Fact]
    public async Task OnSubmit_WhenUserNotFound_RedirectsToLogin()
    {
        await using var ctx = CreateTestContext();
        var cut = ctx.Render<ChangePassword>(parameters =>
            parameters.Add(x => x.Username, "missing-user"));
        _redirectManager.Reset();

        await cut.InvokeAsync(async () =>
        {
            cut.Instance.NewPassword = "ValidPass1!";
            cut.Instance.ConfirmationPassword = "ValidPass1!";
            await cut.Instance.OnSubmit();
        });
        cut.Render();

        _mockAuthenticationService.Verify(
            x => x.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties?>()),
            Times.Never);
        _redirectManager.LastRedirectUri.Should().Be(AccountRoutes.Login);
    }

    [Fact]
    public async Task OnSubmit_WhenPasswordDoesNotMeetComplexity_SetsComplexityError()
    {
        await CreateUserAsync("active-user", "CorrectPass1!");
        await using var ctx = CreateTestContext();
        var cut = ctx.Render<ChangePassword>(parameters =>
            parameters.Add(x => x.Username, "active-user"));
        _redirectManager.Reset();

        await cut.InvokeAsync(async () =>
        {
            cut.Instance.NewPassword = "short";
            cut.Instance.ConfirmationPassword = "short";
            await cut.Instance.OnSubmit();
        });
        cut.Render();

        cut.Markup.Should().Contain(Translations.ChangePassword_PasswordComplexityError);
        _mockAuthenticationService.Verify(
            x => x.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties?>()),
            Times.Never);
    }

    [Fact]
    public async Task OnSubmit_WhenPasswordMatchesCurrentPassword_ClearsPasswordsAndSetsSamePasswordError()
    {
        await CreateUserAsync("active-user", "CorrectPass1!");
        await using var ctx = CreateTestContext();
        var cut = ctx.Render<ChangePassword>(parameters =>
            parameters.Add(x => x.Username, "active-user"));
        _redirectManager.Reset();

        await cut.InvokeAsync(async () =>
        {
            cut.Instance.NewPassword = "CorrectPass1!";
            cut.Instance.ConfirmationPassword = "CorrectPass1!";
            await cut.Instance.OnSubmit();
        });
        cut.Render();

        cut.Instance.NewPassword.Should().BeEmpty();
        cut.Instance.ConfirmationPassword.Should().BeEmpty();
        cut.Markup.Should().Contain(Translations.ChangePassword_PasswordsAreTheSame);
        _mockAuthenticationService.Verify(
            x => x.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties?>()),
            Times.Never);
    }

    [Fact]
    public async Task ChangePassword_WithPinLoginEnabled_AndPinDisabled_DoesNotRedirectToPinSetup()
    {
        var user = await CreateUserAsync("active-user", "CorrectPass1!", temporaryPassword: true);

        await using var ctx = CreateTestContext();
        var cut = ctx.Render<ChangePassword>(parameters =>
            parameters.Add(x => x.Username, "active-user"));
        _redirectManager.Reset();

        await cut.InvokeAsync(async () =>
        {
            cut.Instance.NewPassword = "NewPass1!";
            cut.Instance.ConfirmationPassword = "NewPass1!";
            cut.Instance.ReturnUrl = "/worklist";
            await cut.Instance.OnSubmit();
        });
        cut.Render();

        _mockMediator.Verify(
            x => x.Publish(
                It.Is<UserLoggedInEvent>(evt => evt.UserId == user.Id && evt.InstrumentNumber == null),
                It.IsAny<CancellationToken>()),
            Times.Once);

        user.TemporaryPassword.Should().BeFalse();
        user.LastLoginDateTimeOffset.Should().Be(_mockDateTime.Object.Now);
        user.PasswordEmittedDateTimeOffset.Should().Be(_mockDateTime.Object.Now);
        _redirectManager.LastRedirectUri.Should().Be("/worklist");
    }

    [Fact]
    public async Task OnSubmit_WhenPinLoginEnabledAndNoPinSet_RedirectsToPinSetup()
    {
        var user = await CreateUserAsync("active-user", "CorrectPass1!", temporaryPassword: true);
        user.IsPinEnabled = true;
        user.PinHash = null;

        _mockSecuritySettings.Setup(x => x.Value).Returns(new SecuritySettings
        {
            EnablePinLogin = true,
            PasswordComplexitySettings = new PasswordComplexitySettings
            {
                RequireDigit = true,
                RequireBothUpperAndLowerCase = true,
                RequireNonAlphanumeric = true,
                RequiredLength = 8,
                RequireNoUsernameInPassword = true
            }
        });

        await _applicationUserManager.UpdateAsync(user);

        await using var ctx = CreateTestContext();
        var cut = ctx.Render<ChangePassword>(parameters =>
            parameters.Add(x => x.Username, "active-user"));
        _redirectManager.Reset();

        await cut.InvokeAsync(async () =>
        {
            cut.Instance.NewPassword = "NewPass1!";
            cut.Instance.ConfirmationPassword = "NewPass1!";
            cut.Instance.ReturnUrl = "/worklist";
            await cut.Instance.OnSubmit();
        });
        cut.Render();

        _redirectManager.LastRedirectUri.Should().NotBeNull();
        _redirectManager.LastRedirectUri.Should().Be(AccountRoutes.PinSetup);
    }

    [Fact]
    public async Task OnSubmit_WhenPinLoginEnabledAndPinAlreadySet_RedirectsToReturnUrl()
    {
        var user = await CreateUserAsync("active-user", "CorrectPass1!", temporaryPassword: true);
        user.IsPinEnabled = true;
        user.PinHash = "existing-pin-hash";

        _mockSecuritySettings.Setup(x => x.Value).Returns(new SecuritySettings
        {
            EnablePinLogin = true,
            PasswordComplexitySettings = new PasswordComplexitySettings
            {
                RequireDigit = true,
                RequireBothUpperAndLowerCase = true,
                RequireNonAlphanumeric = true,
                RequiredLength = 8,
                RequireNoUsernameInPassword = true
            }
        });

        await _applicationUserManager.UpdateAsync(user);

        await using var ctx = CreateTestContext();
        var cut = ctx.Render<ChangePassword>(parameters =>
            parameters.Add(x => x.Username, "active-user"));
        _redirectManager.Reset();

        await cut.InvokeAsync(async () =>
        {
            cut.Instance.NewPassword = "NewPass1!";
            cut.Instance.ConfirmationPassword = "NewPass1!";
            cut.Instance.ReturnUrl = "/worklist";
            await cut.Instance.OnSubmit();
        });
        cut.Render();

        _redirectManager.LastRedirectUri.Should().Be("/worklist");
        user.PinHash.Should().Be("existing-pin-hash");
    }

    [Fact]
    public async Task OnSubmit_WhenPinLoginEnabledAndPinDisabled_DoesNotRedirectToPinSetup()
    {
        var user = await CreateUserAsync("active-user", "CorrectPass1!", temporaryPassword: true);
        user.IsPinEnabled = false;
        user.PinHash = null;

        _mockSecuritySettings.Setup(x => x.Value).Returns(new SecuritySettings
        {
            EnablePinLogin = true,
            PasswordComplexitySettings = new PasswordComplexitySettings
            {
                RequireDigit = true,
                RequireBothUpperAndLowerCase = true,
                RequireNonAlphanumeric = true,
                RequiredLength = 8,
                RequireNoUsernameInPassword = true
            }
        });

        await _applicationUserManager.UpdateAsync(user);

        await using var ctx = CreateTestContext();
        var cut = ctx.Render<ChangePassword>(parameters =>
            parameters.Add(x => x.Username, "active-user"));
        _redirectManager.Reset();

        await cut.InvokeAsync(async () =>
        {
            cut.Instance.NewPassword = "NewPass1!";
            cut.Instance.ConfirmationPassword = "NewPass1!";
            cut.Instance.ReturnUrl = "/worklist";
            await cut.Instance.OnSubmit();
        });
        cut.Render();

        _redirectManager.LastRedirectUri.Should().Be("/worklist");
        _redirectManager.LastRedirectUri.Should().NotContain(AccountRoutes.PinSetup);
        user.IsPinEnabled.Should().BeFalse();
        user.PinHash.Should().BeNull();

        var updatedUser = await _identityUserManager.FindByNameAsync("active-user");
        updatedUser.Should().NotBeNull();
        (await _identityUserManager.CheckPasswordAsync(updatedUser, "NewPass1!")).Should().BeTrue();
        (await _identityUserManager.CheckPasswordAsync(updatedUser, "CorrectPass1!")).Should().BeFalse();
    }

    [Fact]
    public async Task OnSubmit_WhenClientDeviceIsInstrument_PublishesEventWithInstrumentNumber()
    {
        var user = await CreateUserAsync("active-user", "CorrectPass1!", temporaryPassword: true);
        var instrumentId = 42;
        var instrumentNumber = "SN42";

        _mockClientDeviceService.Setup(x => x.ClientDeviceType).Returns(ClientDeviceType.Instrument);
        _mockCurrentInstrumentService.Setup(x => x.InstrumentId).Returns(instrumentId);
        _mockCurrentInstrumentService.Setup(x => x.InstrumentNumber).Returns(instrumentNumber);

        await using var ctx = CreateTestContext();
        var cut = ctx.Render<ChangePassword>(parameters =>
            parameters.Add(x => x.Username, "active-user"));
        _redirectManager.Reset();

        await cut.InvokeAsync(async () =>
        {
            cut.Instance.NewPassword = "NewPass1!";
            cut.Instance.ConfirmationPassword = "NewPass1!";
            await cut.Instance.OnSubmit();
        });
        cut.Render();

        _mockMediator.Verify(
            x => x.Publish(
                It.Is<UserLoggedInEvent>(evt => evt.UserId == user.Id && evt.InstrumentNumber == instrumentNumber),
                It.IsAny<CancellationToken>()),
            Times.Once);
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
        ctx.Services.AddSingleton<IApplicationUserManager>(_applicationUserManager);
        ctx.Services.AddSingleton(_signInManager);
        ctx.Services.AddSingleton<IIdentityRedirectManager>(_redirectManager);
        ctx.Services.AddSingleton<IChangePasswordTicketService>(_changePasswordTicketService);
        ctx.Services.AddSingleton(_mockMediator.Object);
        ctx.Services.AddSingleton(_mockClientDeviceService.Object);
        ctx.Services.AddSingleton(_mockCurrentInstrumentService.Object);
        ctx.Services.AddSingleton(_mockSecuritySettings.Object);
        ctx.Services.AddSingleton(_mockDateTime.Object);
        ctx.Services.AddSingleton(_mockLogger.Object);
        ctx.Services.AddSingleton(Mock.Of<IAntiforgery>());
        return ctx;
    }

    private async Task<ApplicationUser> CreateUserAsync(
        string username,
        string password,
        bool temporaryPassword = false)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = username,
            NormalizedUserName = username.ToUpperInvariant(),
            Email = $"{username}@test.local",
            IsActive = true,
            TemporaryPassword = temporaryPassword,
            IsPinEnabled = false
        };

        var createResult = await _identityUserManager.CreateAsync(user, password);
        createResult.Succeeded.Should().BeTrue();
        return user;
    }

    private static async Task InvokeOnInitializedAsync(ChangePassword component)
    {
        var method = typeof(ChangePassword).GetMethod("OnInitializedAsync", BindingFlags.Instance | BindingFlags.NonPublic);
        method.Should().NotBeNull();
        var result = method.Invoke(component, null);
        result.Should().BeAssignableTo<Task>();
        await (Task)result;
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

        public void Reset()
        {
            LastRedirectUri = null;
        }
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;

        public string ApplicationName { get; set; } = "Pharos.Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }

    private sealed class TestPasswordResetTokenProvider : IUserTwoFactorTokenProvider<ApplicationUser>
    {
        public Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<ApplicationUser> manager, ApplicationUser user)
        {
            return Task.FromResult(false);
        }

        public Task<string> GenerateAsync(string purpose, UserManager<ApplicationUser> manager, ApplicationUser user)
        {
            return Task.FromResult("test-reset-token");
        }

        public Task<bool> ValidateAsync(string purpose, string token, UserManager<ApplicationUser> manager, ApplicationUser user)
        {
            return Task.FromResult(token == "test-reset-token");
        }
    }
}