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
using Pharos.Contracts;
using Pharos.Contracts.Security;
using Pharos.Server.Application;
using Pharos.Server.Application.Identity;
using Pharos.Server.Application.Settings.Security;
using Pharos.Server.Identity.Application.Commands;
using Pharos.Server.Identity.Domain;
using Pharos.Server.Identity.Infrastructure.Authentication;
using Pharos.Server.Identity.Infrastructure.Authorization;
using Pharos.Server.Infrastructure;
using Pharos.Server.Localization;

namespace Pharos.Server.Identity.Web.Components.Account.Pages;

public sealed class LoginTests : IDisposable
{
    private readonly IdentityDbContext<ApplicationUser, ApplicationRole, Guid> _dbContext;
    private readonly UserManager<ApplicationUser> _identityUserManager;
    private readonly IApplicationUserManager _applicationUserManager;
    private readonly CustomSignInManager _signInManager;
    private readonly Mock<IAuthenticationService> _mockAuthenticationService;
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<IDateTime> _mockDateTime;
    private readonly Mock<IClientDeviceService> _mockClientDeviceService;
    private readonly Mock<ICurrentInstrumentService> _mockCurrentInstrumentService;
    private readonly Mock<IOptionsSnapshot<SecuritySettings>> _mockSecuritySettings;
    private readonly Mock<IPinLockoutService> _mockPinLockoutService;
    private readonly Mock<ILogger<Login>> _mockLogger;
    private readonly TestIdentityRedirectManager _redirectManager;
    private readonly TestChangePasswordTicketService _changePasswordTicketService;
    private readonly MemoryCache _memoryCache;

    public LoginTests()
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
            EnablePinLogin = false,
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
        _mockPinLockoutService = new Mock<IPinLockoutService>();
        _mockPinLockoutService.Setup(x => x.IsPinModeEnabled()).Returns(false);
        _mockPinLockoutService.Setup(x => x.IsPinLoginSuspended()).ReturnsAsync(false);

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
        _mockClientDeviceService = new Mock<IClientDeviceService>();
        _mockClientDeviceService.Setup(x => x.ClientDeviceType).Returns(ClientDeviceType.Desktop);
        _mockCurrentInstrumentService = new Mock<ICurrentInstrumentService>();
        _mockLogger = new Mock<ILogger<Login>>();

        _redirectManager = new TestIdentityRedirectManager();
        _changePasswordTicketService = new TestChangePasswordTicketService();
    }

    [Fact]
    public async Task OnSubmit_WhenUsernameIsEmpty_SetsRequiredFieldError()
    {
        await using var ctx = CreateTestContext();
        var cut = ctx.Render<Login>();

        await cut.InvokeAsync(async () =>
        {
            cut.Instance.UserName = "";
            cut.Instance.Password = "any";
            await cut.Instance.OnSubmit();
        });
        cut.Render();

        cut.Markup.Should().Contain(Translations.Login_RequiredFieldEmpty);
        _mockAuthenticationService.Verify(
            x => x.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties?>()),
            Times.Never);
        _redirectManager.LastRedirectUri.Should().BeNull();

    }

    [Fact]
    public async Task OnSubmit_WhenPasswordIsEmpty_SetsRequiredFieldError()
    {
        await using var ctx = CreateTestContext();
        var cut = ctx.Render<Login>();

        await cut.InvokeAsync(async () =>
        {
            cut.Instance.UserName = "any";
            cut.Instance.Password = "";
            await cut.Instance.OnSubmit();
        });
        cut.Render();

        cut.Markup.Should().Contain(Translations.Login_RequiredFieldEmpty);
        _mockAuthenticationService.Verify(
            x => x.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties?>()),
            Times.Never);
        _redirectManager.LastRedirectUri.Should().BeNull();
    }

    [Fact]
    public async Task OnSubmit_WhenUserNotFound_KeepsUserNameClearsPasswordAndSetsInvalidUserOrPasswordError()
    {
        await using var ctx = CreateTestContext();
        var cut = ctx.Render<Login>();

        await cut.InvokeAsync(async () =>
        {
            cut.Instance.UserName = "nonexistent-user";
            cut.Instance.Password = "any";
            await cut.Instance.OnSubmit();
        });
        cut.Render();

        cut.Instance.UserName.Should().Be("nonexistent-user");
        cut.Instance.Password.Should().BeEmpty();
        cut.Markup.Should().Contain(Translations.Login_InvalidUserNameOrPassword);
        _mockAuthenticationService.Verify(
            x => x.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties?>()),
            Times.Never);
        _redirectManager.LastRedirectUri.Should().BeNull();

    }

    [Fact]
    public async Task OnSubmit_WhenUserIsInactive_KeepsUserNameClearsPasswordAndSetsInactiveError()
    {
        var user = await CreateUserAsync("inactive-user", "TempPass1!", isActive: false);
        await using var ctx = CreateTestContext();
        var cut = ctx.Render<Login>();

        await cut.InvokeAsync(async () =>
        {
            cut.Instance.UserName = user.UserName!;
            cut.Instance.Password = "TempPass1!";
            await cut.Instance.OnSubmit();
        });
        cut.Render();

        cut.Instance.UserName.Should().Be("inactive-user");
        cut.Instance.Password.Should().BeEmpty();
        cut.Markup.Should().Contain(Translations.Login_UserIsInactive);
        _mockAuthenticationService.Verify(
            x => x.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties?>()),
            Times.Never);
        _redirectManager.LastRedirectUri.Should().BeNull();
    }

    [Fact]
    public async Task OnSubmit_WhenPasswordIsInvalid_KeepsUserNameClearsPasswordAndSetsInvalidError()
    {
        await CreateUserAsync("valid-user", "CorrectPass1!");
        await using var ctx = CreateTestContext();
        var cut = ctx.Render<Login>();

        await cut.InvokeAsync(async () =>
        {
            cut.Instance.UserName = "valid-user";
            cut.Instance.Password = "WrongPass1!";
            await cut.Instance.OnSubmit();
        });
        cut.Render();

        cut.Instance.UserName.Should().Be("valid-user");
        cut.Instance.Password.Should().BeEmpty();
        cut.Markup.Should().Contain(Translations.Login_InvalidUserNameOrPassword);
        _mockAuthenticationService.Verify(
            x => x.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties?>()),
            Times.Never);
        _redirectManager.LastRedirectUri.Should().BeNull();
    }

    [Fact]
    public async Task OnSubmit_WhenTemporaryPasswordRequired_RedirectsToChangePassword()
    {
        await CreateUserAsync("temp-user", "TempPass1!", isActive: true, temporaryPassword: true);
        await using var ctx = CreateTestContext();
        var cut = ctx.Render<Login>();

        await cut.InvokeAsync(async () =>
        {
            cut.Instance.UserName = "temp-user";
            cut.Instance.Password = "TempPass1!";
            cut.Instance.ReturnUrl = "/dashboard";
            await cut.Instance.OnSubmit();
        });

        _changePasswordTicketService.LastUserName.Should().Be("temp-user");
        _changePasswordTicketService.LastTicketType.Should().Be(ChangePasswordTicketType.Initial);
        _redirectManager.LastRedirectUri.Should().NotBeNull();
        _redirectManager.LastRedirectUri!.Should().Contain(AccountRoutes.ChangePassword);
        _redirectManager.LastRedirectUri.Should().Contain("ticket=ticket-value");
        _redirectManager.LastRedirectUri.Should().Contain("returnUrl=%2Fdashboard");
        _mockAuthenticationService.Verify(
            x => x.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties?>()),
            Times.Never);
    }

    [Fact]
    public async Task OnSubmit_WhenPasswordExpiredAndExpirationEnabled_RedirectsToChangePassword()
    {
        // Password emitted 91 days ago; with PasswordExpirationDays=90 this is expired
        var emittedDate = _mockDateTime.Object.Now.AddDays(-91);
        await CreateUserAsync("expired-user", "CorrectPass1!", isActive: true, temporaryPassword: false, passwordEmittedDateTimeOffset: emittedDate);

        _mockSecuritySettings.Setup(x => x.Value).Returns(new SecuritySettings
        {
            PasswordExpirationEnabled = true,
            PasswordExpirationDays = 90,
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

        await using var ctx = CreateTestContext();
        var cut = ctx.Render<Login>();

        await cut.InvokeAsync(async () =>
        {
            cut.Instance.UserName = "expired-user";
            cut.Instance.Password = "CorrectPass1!";
            cut.Instance.ReturnUrl = "/dashboard";
            await cut.Instance.OnSubmit();
        });

        _redirectManager.LastRedirectUri.Should().NotBeNull();
        _redirectManager.LastRedirectUri!.Should().Contain(AccountRoutes.ChangePassword);
        _mockAuthenticationService.Verify(
            x => x.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties?>()),
            Times.Never);
    }

    [Fact]
    public async Task OnSubmit_WhenPasswordExpiredAndExpirationUnlimited_AllowsLogin()
    {
        // Password emitted 91 days ago; expiration is disabled so login is allowed
        var emittedDate = _mockDateTime.Object.Now.AddDays(-91);
        var user = await CreateUserAsync("expired-unlimited-user", "CorrectPass1!", isActive: true, temporaryPassword: false, passwordEmittedDateTimeOffset: emittedDate);

        _mockSecuritySettings.Setup(x => x.Value).Returns(new SecuritySettings
        {
            PasswordExpirationEnabled = false,
            PasswordExpirationDays = 90,
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

        await using var ctx = CreateTestContext();
        var cut = ctx.Render<Login>();

        await cut.InvokeAsync(async () =>
        {
            cut.Instance.UserName = "expired-unlimited-user";
            cut.Instance.Password = "CorrectPass1!";
            cut.Instance.ReturnUrl = "/dashboard";
            await cut.Instance.OnSubmit();
        });

        _mockAuthenticationService.Verify(
            x => x.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties?>()),
            Times.Once);

        _mockMediator.Verify(
            x => x.Publish(
                It.Is<UserLoggedInEvent>(evt => evt.UserId == user.Id && evt.InstrumentNumber == null),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _redirectManager.LastRedirectUri.Should().Be("/dashboard");
    }

    [Fact]
    public async Task OnSubmit_WhenMaxFailedAttemptsReached_SetsLockedOutError()
    {
        var user = await CreateUserAsync("locked-user", "CorrectPass1!");
        user.LockoutEnabled = true;
        var updateResult = await _identityUserManager.UpdateAsync(user);
        updateResult.Succeeded.Should().BeTrue();
        await using var ctx = CreateTestContext();
        var cut = ctx.Render<Login>();

        for (var i = 0; i < 5; i++)
        {
            await cut.InvokeAsync(async () =>
            {
                cut.Instance.UserName = "locked-user";
                cut.Instance.Password = "WrongPass1!";
                await cut.Instance.OnSubmit();
            });
        }
        cut.Render();

        cut.Instance.UserName.Should().Be("locked-user");
        cut.Instance.Password.Should().BeEmpty();
        cut.Markup.Should().Contain(Translations.Login_UserLockedout);
        _mockAuthenticationService.Verify(
            x => x.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties?>()),
            Times.Never);
        _redirectManager.LastRedirectUri.Should().BeNull();
    }

    [Fact]
    public async Task OnSubmit_WhenPasswordLockoutEnabled_EnablesUserLockoutAndLocksOutAfterConfiguredFailures()
    {
        var user = await CreateUserAsync("lockout-setting-user", "CorrectPass1!");
        user.LockoutEnabled = false;
        var setupUpdateResult = await _identityUserManager.UpdateAsync(user);
        setupUpdateResult.Succeeded.Should().BeTrue();

        _mockSecuritySettings.Setup(x => x.Value).Returns(new SecuritySettings
        {
            EnablePinLogin = false,
            EnablePasswordLoginLockout = true,
            PasswordLoginLockoutMaxFailedAttempts = 3,
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

        await using var ctx = CreateTestContext();
        var cut = ctx.Render<Login>();

        for (var i = 0; i < 3; i++)
        {
            await cut.InvokeAsync(async () =>
            {
                cut.Instance.UserName = "lockout-setting-user";
                cut.Instance.Password = "WrongPass1!";
                await cut.Instance.OnSubmit();
            });
        }
        cut.Render();

        var updatedUser = await _identityUserManager.FindByNameAsync("lockout-setting-user");
        updatedUser.Should().NotBeNull();
        updatedUser!.LockoutEnabled.Should().BeTrue();
        cut.Markup.Should().Contain(Translations.Login_UserLockedout);
    }

    [Fact]
    public async Task OnSubmit_WhenPasswordLockoutDisabled_DoesNotLockOutAfterRepeatedFailures()
    {
        var user = await CreateUserAsync("lockout-disabled-user", "CorrectPass1!");
        user.LockoutEnabled = true;
        var setupUpdateResult = await _identityUserManager.UpdateAsync(user);
        setupUpdateResult.Succeeded.Should().BeTrue();

        _mockSecuritySettings.Setup(x => x.Value).Returns(new SecuritySettings
        {
            EnablePinLogin = false,
            EnablePasswordLoginLockout = false,
            PasswordLoginLockoutMaxFailedAttempts = 3,
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

        await using var ctx = CreateTestContext();
        var cut = ctx.Render<Login>();

        for (var i = 0; i < 5; i++)
        {
            await cut.InvokeAsync(async () =>
            {
                cut.Instance.UserName = "lockout-disabled-user";
                cut.Instance.Password = "WrongPass1!";
                await cut.Instance.OnSubmit();
            });
        }
        cut.Render();

        var updatedUser = await _identityUserManager.FindByNameAsync("lockout-disabled-user");
        updatedUser.Should().NotBeNull();
        updatedUser!.LockoutEnabled.Should().BeFalse();
        (await _identityUserManager.IsLockedOutAsync(updatedUser)).Should().BeFalse();
        cut.Markup.Should().Contain(Translations.Login_InvalidUserNameOrPassword);
        cut.Markup.Should().NotContain(Translations.Login_UserLockedout);
    }

    [Fact]
    public async Task OnSubmit_WhenLoginSucceeds_UpdatesUserPublishesEventAndRedirects()
    {
        var user = await CreateUserAsync("active-user", "CorrectPass1!", temporaryPassword: false);
        await using var ctx = CreateTestContext();
        var cut = ctx.Render<Login>();

        await cut.InvokeAsync(async () =>
        {
            cut.Instance.UserName = "active-user";
            cut.Instance.Password = "CorrectPass1!";
            cut.Instance.RememberMe = true;
            cut.Instance.ReturnUrl = "/worklist";
            await cut.Instance.OnSubmit();
        });

        _mockAuthenticationService.Verify(
            x => x.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties?>()),
            Times.Once);

        _mockMediator.Verify(
            x => x.Publish(
                It.Is<UserLoggedInEvent>(evt => evt.UserId == user.Id && evt.InstrumentNumber == null),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mockMediator.Verify(
            x => x.Send(It.IsAny<ValidateServiceEngineerPermissionsCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);

        var updatedUser = await _identityUserManager.FindByNameAsync("active-user");
        updatedUser.Should().NotBeNull();
        updatedUser.LastLoginDateTimeOffset.Should().Be(_mockDateTime.Object.Now);
        _redirectManager.LastRedirectUri.Should().Be("/worklist");
    }

    [Fact]
    public async Task OnSubmit_WhenServiceEngineerRole_SendsValidateServiceEngineerPermissionsCommand()
    {
        var user = await CreateUserAsync("service-user-order", "CorrectPass1!", temporaryPassword: false);
        await AddRoleToUserAsync(user, BuiltInRoles.ServiceEngineer);

        var sequence = new MockSequence();
        _mockMediator
            .InSequence(sequence)
            .Setup(x => x.Send(It.IsAny<ValidateServiceEngineerPermissionsCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockAuthenticationService
            .InSequence(sequence)
            .Setup(x => x.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties?>()))
            .Returns(Task.CompletedTask);

        await using var ctx = CreateTestContext();
        var cut = ctx.Render<Login>();

        await cut.InvokeAsync(async () =>
        {
            cut.Instance.UserName = "service-user-order";
            cut.Instance.Password = "CorrectPass1!";
            await cut.Instance.OnSubmit();
        });

        _mockMediator.Verify(
            x => x.Send(It.IsAny<ValidateServiceEngineerPermissionsCommand>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _mockAuthenticationService.Verify(
            x => x.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties?>()),
            Times.Once);
    }

    [Fact]
    public async Task OnSubmit_WhenClientDeviceIsInstrument_PublishesEventWithInstrumentNumber()
    {
        var user = await CreateUserAsync("instrument-user", "CorrectPass1!", temporaryPassword: false);
        int? instrumentId = 123;
        var instrumentNumber = "SN123";
        _mockCurrentInstrumentService.Setup(x => x.InstrumentId).Returns(instrumentId);
        _mockCurrentInstrumentService.Setup(x => x.InstrumentNumber).Returns(instrumentNumber);

        await using var ctx = CreateTestContext();
        var cut = ctx.Render<Login>();

        await cut.InvokeAsync(async () =>
        {
            cut.Instance.UserName = "instrument-user";
            cut.Instance.Password = "CorrectPass1!";
            await cut.Instance.OnSubmit();
        });

        _mockMediator.Verify(
            x => x.Publish(
                It.Is<UserLoggedInEvent>(evt => evt.UserId == user.Id && evt.InstrumentNumber == instrumentNumber),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task OnSubmit_WhenPinLoginEnabledAndUserHasNoPin_RedirectsToPinSetup()
    {
        _mockPinLockoutService.Setup(x => x.IsPinModeEnabled()).Returns(true);

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

        var user = await CreateUserAsync("pin-user", "CorrectPass1!", temporaryPassword: false);
        user.IsPinEnabled = true;
        user.PinHash = null;
        var updateResult = await _identityUserManager.UpdateAsync(user);
        updateResult.Succeeded.Should().BeTrue();

        await using var ctx = CreateTestContext();
        var cut = ctx.Render<Login>();

        await cut.InvokeAsync(async () =>
        {
            cut.Instance.UserName = "pin-user";
            cut.Instance.Password = "CorrectPass1!";
            cut.Instance.ReturnUrl = "/dashboard";
            await cut.Instance.OnSubmit();
        });

        _redirectManager.LastRedirectUri.Should().NotBeNull();
        _redirectManager.LastRedirectUri!.Should().Contain(AccountRoutes.PinSetup);
        _redirectManager.LastRedirectUri.Should().Be(AccountRoutes.PinSetup);
    }

    [Fact]
    public async Task OnSubmit_WhenPinGenerationFails_RedirectsToPinSetupWithError()
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "pin-fail-user",
            IsActive = true,
            TemporaryPassword = false,
            IsPinEnabled = true,
            PinHash = null
        };
        user.PasswordHash = new PasswordHasher<ApplicationUser>().HashPassword(user, "CorrectPass1!");

        var mockUserManager = new Mock<IApplicationUserManager>();
        mockUserManager.Setup(x => x.FindByNameAsync("pin-fail-user")).ReturnsAsync(user);
        mockUserManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync([]);
        mockUserManager.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsPasswordValid("CorrectPass1!", "pin-fail-user")).Returns(true);
        mockUserManager.Setup(x => x.SetPinAsync(user, It.IsAny<string>())).ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "DuplicatePin" }));

        _mockPinLockoutService.Setup(x => x.IsPinModeEnabled()).Returns(true);

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

        await using var ctx = CreateTestContext(mockUserManager.Object);
        var cut = ctx.Render<Login>();

        await cut.InvokeAsync(async () =>
        {
            cut.Instance.UserName = "pin-fail-user";
            cut.Instance.Password = "CorrectPass1!";
            await cut.Instance.OnSubmit();
        });

        _redirectManager.LastRedirectUri.Should().Be(AccountRoutes.PinSetup);
    }

    [Fact]
    public async Task OnSubmit_WhenPinGenerationThrowsException_RedirectsToPinSetupWithError()
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "pin-throw-user",
            IsActive = true,
            TemporaryPassword = false,
            IsPinEnabled = true,
            PinHash = null
        };
        user.PasswordHash = new PasswordHasher<ApplicationUser>().HashPassword(user, "CorrectPass1!");

        var mockUserManager = new Mock<IApplicationUserManager>();
        mockUserManager.Setup(x => x.FindByNameAsync("pin-throw-user")).ReturnsAsync(user);
        mockUserManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync([]);
        mockUserManager.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsPasswordValid("CorrectPass1!", "pin-throw-user")).Returns(true);
        mockUserManager.Setup(x => x.SetPinAsync(user, It.IsAny<string>())).ThrowsAsync(new InvalidOperationException("pin failure"));

        _mockPinLockoutService.Setup(x => x.IsPinModeEnabled()).Returns(true);

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

        await using var ctx = CreateTestContext(mockUserManager.Object);
        var cut = ctx.Render<Login>();

        await cut.InvokeAsync(async () =>
        {
            cut.Instance.UserName = "pin-throw-user";
            cut.Instance.Password = "CorrectPass1!";
            await cut.Instance.OnSubmit();
        });

        _redirectManager.LastRedirectUri.Should().Be(AccountRoutes.PinSetup);
    }

    [Fact]
    public async Task OnSubmit_WhenPinLoginEnabledAndPinDisabled_DoesNotRedirectToPinSetup()
    {
        var user = await CreateUserAsync("pin-disabled-user", "CorrectPass1!", temporaryPassword: false);
        user.IsPinEnabled = false;
        user.PinHash = null;
        var updateResult = await _identityUserManager.UpdateAsync(user);
        updateResult.Succeeded.Should().BeTrue();

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

        await using var ctx = CreateTestContext();
        var cut = ctx.Render<Login>();

        await cut.InvokeAsync(async () =>
        {
            cut.Instance.UserName = "pin-disabled-user";
            cut.Instance.Password = "CorrectPass1!";
            cut.Instance.ReturnUrl = "/dashboard";
            await cut.Instance.OnSubmit();
        });

        _redirectManager.LastRedirectUri.Should().Be("/dashboard");
        _redirectManager.LastRedirectUri.Should().NotContain(AccountRoutes.PinSetup);
    }

    [Fact]
    public async Task OnInitialized_WhenInstrumentAndPinLoginEnabledAndNotLockedOut_ShowsPinLoginLink()
    {
        _mockCurrentInstrumentService.Setup(x => x.InstrumentId).Returns(123);
        _mockPinLockoutService.Setup(x => x.IsPinModeEnabled()).Returns(true);
        _mockPinLockoutService.Setup(x => x.IsPinLoginSuspended()).ReturnsAsync(false);

        await using var ctx = CreateTestContext();
        var cut = ctx.Render<Login>();

        cut.Markup.Should().Contain(Translations.Login_LoginWithPinLink);
        cut.Markup.Should().Contain($"href=\"{AccountRoutes.LoginWithPin}\"");
        cut.Markup.Should().NotContain("Temporarily disabled");
    }

    [Fact]
    public async Task OnInitialized_WhenInstrumentAndPinLoginEnabledAndLockedOut_ShowsTemporarilyDisabledPinLoginText()
    {
        _mockCurrentInstrumentService.Setup(x => x.InstrumentId).Returns(321);
        _mockPinLockoutService.Setup(x => x.IsPinModeEnabled()).Returns(true);
        _mockPinLockoutService.Setup(x => x.IsPinLoginSuspended()).ReturnsAsync(true);

        await using var ctx = CreateTestContext();
        var cut = ctx.Render<Login>();

        cut.Markup.Should().Contain(Translations.Login_LoginWithPinLink_Disabled);
        cut.Markup.Should().NotContain($"href=\"{AccountRoutes.LoginWithPin}\"");
    }

    [Fact]
    public async Task OnSubmit_WhenPasswordMeetsCurrentComplexity_DoesNotRedirectToChangePassword()
    {
        await CreateUserAsync("normal-user", "TempPass1!", isActive: true, temporaryPassword: false);

        await using var ctx = CreateTestContext();
        var cut = ctx.Render<Login>();

        await cut.InvokeAsync(async () =>
        {
            cut.Instance.UserName = "normal-user";
            cut.Instance.Password = "TempPass1!";
            cut.Instance.ReturnUrl = "/dashboard";
            await cut.Instance.OnSubmit();
        });

        _redirectManager.LastRedirectUri.Should().NotContain(AccountRoutes.ChangePassword);
        _mockAuthenticationService.Verify(
            x => x.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties?>()),
            Times.Once);
    }

    [Fact]
    public async Task OnSubmit_WhenPasswordDoesNotMeetCurrentComplexity_RedirectsToChangePassword()
    {
        // Create user with a weak password by temporarily relaxing complexity rules
        _mockSecuritySettings.Setup(x => x.Value).Returns(new SecuritySettings
        {
            EnablePinLogin = false,
            EnablePasswordLoginLockout = true,
            PasswordLoginLockoutMaxFailedAttempts = 5,
            PasswordLoginLockoutDurationMinutes = 5,
            PasswordComplexitySettings = new PasswordComplexitySettings
            {
                RequireDigit = false,
                RequireBothUpperAndLowerCase = false,
                RequireNonAlphanumeric = false,
                RequiredLength = 4,
                RequireNoUsernameInPassword = false
            }
        });

        await CreateUserAsync("policy-weak-user", "weak", isActive: true, temporaryPassword: false);

        // Now tighten the policy so the existing password no longer complies
        _mockSecuritySettings.Setup(x => x.Value).Returns(new SecuritySettings
        {
            EnablePinLogin = false,
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

        await using var ctx = CreateTestContext();
        var cut = ctx.Render<Login>();

        await cut.InvokeAsync(async () =>
        {
            cut.Instance.UserName = "policy-weak-user";
            cut.Instance.Password = "weak";
            cut.Instance.ReturnUrl = "/dashboard";
            await cut.Instance.OnSubmit();
        });

        _redirectManager.LastRedirectUri.Should().Contain(AccountRoutes.ChangePassword);
        _mockAuthenticationService.Verify(
            x => x.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties?>()),
            Times.Never);
    }

    public void Dispose()
    {
        _identityUserManager.Dispose();
        _dbContext.Dispose();
        _memoryCache.Dispose();
    }

    private BunitContext CreateTestContext(IApplicationUserManager? userManager = null)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton<IHostEnvironment>(new TestHostEnvironment());
        ctx.Services.AddSingleton(userManager ?? _applicationUserManager);
        ctx.Services.AddSingleton(_signInManager);
        ctx.Services.AddSingleton<SignInManager<ApplicationUser>>(_signInManager);
        ctx.Services.AddSingleton<IIdentityRedirectManager>(_redirectManager);
        ctx.Services.AddSingleton<IChangePasswordTicketService>(_changePasswordTicketService);
        ctx.Services.AddSingleton(_mockMediator.Object);
        ctx.Services.AddSingleton(_mockClientDeviceService.Object);
        ctx.Services.AddSingleton(_mockCurrentInstrumentService.Object);
        ctx.Services.AddSingleton(_mockSecuritySettings.Object);
        ctx.Services.AddSingleton(_mockPinLockoutService.Object);
        ctx.Services.AddSingleton(_mockDateTime.Object);
        ctx.Services.AddSingleton(_mockLogger.Object);
        return ctx;
    }

    private async Task<ApplicationUser> CreateUserAsync(
        string username,
        string password,
        bool isActive = true,
        bool temporaryPassword = false,
        DateTimeOffset? passwordEmittedDateTimeOffset = null)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = username,
            IsActive = isActive,
            TemporaryPassword = temporaryPassword,
            PasswordEmittedDateTimeOffset = passwordEmittedDateTimeOffset,
            IsPinEnabled = false
        };

        var createResult = await _identityUserManager.CreateAsync(user, password);
        createResult.Succeeded.Should().BeTrue();
        return user;
    }

    private async Task AddRoleToUserAsync(ApplicationUser user, string roleName)
    {
        _dbContext.Roles.Add(new ApplicationRole
        {
            Id = Guid.NewGuid(),
            Name = roleName,
            NormalizedName = roleName
        });
        await _dbContext.SaveChangesAsync(Xunit.TestContext.Current.CancellationToken);

        var addToRoleResult = await _identityUserManager.AddToRoleAsync(user, roleName);
        addToRoleResult.Succeeded.Should().BeTrue();
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

    private sealed class TestChangePasswordTicketService : IChangePasswordTicketService
    {
        public string? LastUserName { get; private set; }
        public ChangePasswordTicketType LastTicketType { get; private set; }

        public string CreateTicket(string username, ChangePasswordTicketType ticketType)
        {
            LastUserName = username;
            LastTicketType = ticketType;
            return "ticket-value";
        }

        public bool TryConsumeTicket(string username, string ticket, ChangePasswordTicketType ticketType)
        {
            return true;
        }
    }
}
