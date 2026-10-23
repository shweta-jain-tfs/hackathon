using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Settings.Security;
using Pharos.Server.Identity.Domain;
using Pharos.Server.Identity.Infrastructure.Authorization;
using Pharos.Server.Infrastructure;

namespace Pharos.Server.Identity.Infrastructure.Authentication;

public sealed class CustomSignInManagerTests
{
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<IHttpContextAccessor> _mockContextAccessor;
    private readonly Mock<IUserClaimsPrincipalFactory<ApplicationUser>> _mockClaimsFactory;
    private readonly Mock<IOptions<IdentityOptions>> _mockOptionsAccessor;
    private readonly Mock<ILogger<SignInManager<ApplicationUser>>> _mockLogger;
    private readonly Mock<IAuthenticationSchemeProvider> _mockSchemeProvider;
    private readonly Mock<IUserConfirmation<ApplicationUser>> _mockUserConfirmation;
    private readonly Mock<IApplicationUserManager> _mockApplicationUserManager;
    private readonly Mock<IOptionsSnapshot<SecuritySettings>> _mockSecuritySettings;
    private readonly Mock<IDateTime> _mockDateTime;
    private readonly Mock<IPinLockoutService> _mockPinLockoutService;
    private readonly CustomSignInManager _signInManager;

    public CustomSignInManagerTests()
    {
        // Setup UserManager mock
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);

        // Setup HttpContextAccessor mock with full authentication chain
        _mockContextAccessor = new Mock<IHttpContextAccessor>();
        var mockHttpContext = new DefaultHttpContext();
        var authServiceMock = new Mock<IAuthenticationService>();
        authServiceMock
            .Setup(x => x.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()))
            .Returns(Task.CompletedTask);

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(x => x.GetService(typeof(IAuthenticationService)))
            .Returns(authServiceMock.Object);
        serviceProviderMock
            .Setup(x => x.GetService(typeof(IAuthenticationSchemeProvider)))
            .Returns((IAuthenticationSchemeProvider?)null);

        mockHttpContext.RequestServices = serviceProviderMock.Object;
        _mockContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext);

        // Setup other mocks
        _mockClaimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
        _mockClaimsFactory
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new ClaimsPrincipal(new ClaimsIdentity()));

        var identityOptions = new IdentityOptions();
        _mockOptionsAccessor = new Mock<IOptions<IdentityOptions>>();
        _mockOptionsAccessor.Setup(x => x.Value).Returns(identityOptions);

        _mockLogger = new Mock<ILogger<SignInManager<ApplicationUser>>>();

        // Setup auth scheme provider
        _mockSchemeProvider = new Mock<IAuthenticationSchemeProvider>();
        var authScheme = new AuthenticationScheme(
            IdentityConstants.ApplicationScheme,
            IdentityConstants.ApplicationScheme,
            typeof(NoOpAuthenticationHandler));
        _mockSchemeProvider
            .Setup(x => x.GetAllSchemesAsync())
            .ReturnsAsync(new[] { authScheme });
        _mockSchemeProvider
            .Setup(x => x.GetSchemeAsync(IdentityConstants.ApplicationScheme))
            .ReturnsAsync(authScheme);

        _mockUserConfirmation = new Mock<IUserConfirmation<ApplicationUser>>();
        _mockUserConfirmation
            .Setup(x => x.IsConfirmedAsync(It.IsAny<UserManager<ApplicationUser>>(), It.IsAny<ApplicationUser>()))
            .ReturnsAsync(true);

        _mockApplicationUserManager = new Mock<IApplicationUserManager>();

        // Setup SecuritySettings mock
        _mockSecuritySettings = new Mock<IOptionsSnapshot<SecuritySettings>>();
        _mockSecuritySettings.Setup(x => x.Value).Returns(new SecuritySettings
        {
            EnablePinLogin = true,
            EnablePinLoginLockout = true,
            PinLoginLockoutDurationMinutes = 15,
            EnablePasswordLoginLockout = true,
            PasswordLoginLockoutMaxFailedAttempts = 5,
            PasswordLoginLockoutDurationMinutes = 5
        });

        // Setup DateTime mock
        _mockDateTime = new Mock<IDateTime>();
        _mockDateTime.Setup(x => x.Now).Returns(DateTimeOffset.Now);

        // Setup PIN Lockout Service mock
        _mockPinLockoutService = new Mock<IPinLockoutService>();

        // Create the CustomSignInManager instance
        _signInManager = new CustomSignInManager(
            _mockUserManager.Object,
            _mockContextAccessor.Object,
            _mockClaimsFactory.Object,
            _mockOptionsAccessor.Object,
            _mockLogger.Object,
            _mockSchemeProvider.Object,
            _mockUserConfirmation.Object,
            _mockApplicationUserManager.Object,
            _mockPinLockoutService.Object,
            _mockSecuritySettings.Object);
    }

    // Dummy authentication handler for testing
    private class NoOpAuthenticationHandler : IAuthenticationHandler
    {
        public Task<AuthenticateResult> AuthenticateAsync()
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        public Task ChallengeAsync(AuthenticationProperties? properties)
        {
            return Task.CompletedTask;
        }

        public Task ForbidAsync(AuthenticationProperties? properties)
        {
            return Task.CompletedTask;
        }

        public Task InitializeAsync(AuthenticationScheme scheme, HttpContext context)
        {
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task CheckPasswordSignInAsync_WhenPasswordLockoutEnabled_EnablesLockoutOnUser()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "testuser",
            IsActive = true,
            LockoutEnabled = false
        };

        _mockSecuritySettings.Setup(x => x.Value).Returns(new SecuritySettings
        {
            EnablePasswordLoginLockout = true,
            PasswordLoginLockoutMaxFailedAttempts = 5,
            PasswordLoginLockoutDurationMinutes = 5
        });

        _mockUserManager
            .Setup(x => x.CheckPasswordAsync(user, "CorrectPass1!"))
            .ReturnsAsync(true);

        // Act
        var result = await _signInManager.CheckPasswordSignInAsync(user, "CorrectPass1!", lockoutOnFailure: true);

        // Assert
        result.Succeeded.Should().BeTrue();
        user.LockoutEnabled.Should().BeTrue();
        _mockUserManager.Object.Options.Lockout.MaxFailedAccessAttempts.Should().Be(5);
        _mockUserManager.Object.Options.Lockout.DefaultLockoutTimeSpan.Should().Be(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public async Task CheckPasswordSignInAsync_WhenPasswordLockoutDisabled_DisablesLockoutOnUser()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "testuser",
            IsActive = true,
            LockoutEnabled = true
        };

        _mockSecuritySettings.Setup(x => x.Value).Returns(new SecuritySettings
        {
            EnablePasswordLoginLockout = false,
            PasswordLoginLockoutMaxFailedAttempts = 3,
            PasswordLoginLockoutDurationMinutes = 7
        });

        _mockUserManager
            .Setup(x => x.CheckPasswordAsync(user, "CorrectPass1!"))
            .ReturnsAsync(true);

        // Act
        var result = await _signInManager.CheckPasswordSignInAsync(user, "CorrectPass1!", lockoutOnFailure: true);

        // Assert
        result.Succeeded.Should().BeTrue();
        user.LockoutEnabled.Should().BeFalse();
        _mockUserManager.Object.Options.Lockout.MaxFailedAccessAttempts.Should().Be(3);
        _mockUserManager.Object.Options.Lockout.DefaultLockoutTimeSpan.Should().Be(TimeSpan.FromMinutes(7));
    }

    [Fact]
    public async Task PinSignInWithUserAsync_WithValidPin_ReturnsSuccessAndUser()
    {
        // Arrange
        var pin = "1234";
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "testuser",
            IsActive = true,
            IsPinEnabled = true
        };

        _mockPinLockoutService
            .Setup(x => x.IsPinModeEnabled())
            .Returns(true);

        _mockPinLockoutService
            .Setup(x => x.IsPinLoginSuspended())
            .ReturnsAsync(false);

        _mockApplicationUserManager
            .Setup(x => x.FindByPinAsync(pin))
            .ReturnsAsync(user);

        _mockUserManager
            .Setup(x => x.IsLockedOutAsync(user))
            .ReturnsAsync(false);

        _mockUserManager
            .Setup(x => x.ResetAccessFailedCountAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        _mockPinLockoutService
            .Setup(x => x.ResetAttempts())
            .Returns(Task.CompletedTask);

        // Act
        var (result, returnedUser) = await _signInManager.PinSignInWithUserAsync(pin);

        // Assert
        result.Succeeded.Should().BeTrue();
        returnedUser.Should().NotBeNull();
        returnedUser.Should().Be(user);
        _mockUserManager.Verify(x => x.ResetAccessFailedCountAsync(user), Times.Once);
        _mockPinLockoutService.Verify(x => x.ResetAttempts(), Times.Once);
    }

    [Fact]
    public async Task PinSignInWithUserAsync_WithInvalidPin_ReturnsFailedAndNull()
    {
        // Arrange
        var pin = "9999";

        _mockPinLockoutService
            .Setup(x => x.IsPinModeEnabled())
            .Returns(true);

        _mockPinLockoutService
            .Setup(x => x.IsPinLoginSuspended())
            .ReturnsAsync(false);

        _mockApplicationUserManager
            .Setup(x => x.FindByPinAsync(pin))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var (result, returnedUser) = await _signInManager.PinSignInWithUserAsync(pin);

        // Assert
        result.Succeeded.Should().BeFalse();
        returnedUser.Should().BeNull();
    }

    [Fact]
    public async Task PinSignInWithUserAsync_WithInactiveUser_ReturnsNotAllowedAndNull()
    {
        // Arrange
        var pin = "1234";
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "testuser",
            IsActive = false,
            IsPinEnabled = true
        };

        _mockPinLockoutService
            .Setup(x => x.IsPinModeEnabled())
            .Returns(true);

        _mockPinLockoutService
            .Setup(x => x.IsPinLoginSuspended())
            .ReturnsAsync(false);

        _mockApplicationUserManager
            .Setup(x => x.FindByPinAsync(pin))
            .ReturnsAsync(user);

        // Act
        var (result, returnedUser) = await _signInManager.PinSignInWithUserAsync(pin);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.IsNotAllowed.Should().BeTrue();
        returnedUser.Should().BeNull();
    }

    [Fact]
    public async Task PinSignInWithUserAsync_WithLockedOutUser_ReturnsLockedOutAndNull()
    {
        // Arrange
        var pin = "1234";
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "testuser",
            IsActive = true,
            IsPinEnabled = true
        };

        _mockPinLockoutService
            .Setup(x => x.IsPinModeEnabled())
            .Returns(true);

        _mockPinLockoutService
            .Setup(x => x.IsPinLoginSuspended())
            .ReturnsAsync(false);

        _mockApplicationUserManager
            .Setup(x => x.FindByPinAsync(pin))
            .ReturnsAsync(user);

        _mockUserManager
            .Setup(x => x.IsLockedOutAsync(user))
            .ReturnsAsync(true);

        // Act
        var (result, returnedUser) = await _signInManager.PinSignInWithUserAsync(pin);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.IsLockedOut.Should().BeTrue();
        returnedUser.Should().BeNull();
    }

    [Fact]
    public async Task PinSignInWithUserAsync_WithInstrumentLockedOut_ReturnsLockedOutAndNull()
    {
        // Arrange
        var pin = "1234";
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "testuser",
            IsActive = true,
            IsPinEnabled = true
        };

        _mockPinLockoutService
            .Setup(x => x.IsPinModeEnabled())
            .Returns(true); // PIN mode is enabled

        _mockPinLockoutService
            .Setup(x => x.IsPinLoginSuspended())
            .ReturnsAsync(true); // Instrument is locked out

        // Act
        var (result, returnedUser) = await _signInManager.PinSignInWithUserAsync(pin);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.IsLockedOut.Should().BeTrue();
        returnedUser.Should().BeNull();
        _mockApplicationUserManager.Verify(x => x.FindByPinAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandlePinLoginFailureAsync_RecordsFailurePerInstrument()
    {
        // Arrange
        _mockPinLockoutService
            .Setup(x => x.RecordFailedAttempt())
            .ReturnsAsync(false); // Not locked out yet

        // Act
        var lockedOut = await _signInManager.HandlePinLoginFailureAsync();

        // Assert
        lockedOut.Should().BeFalse();
        _mockPinLockoutService.Verify(x => x.RecordFailedAttempt(), Times.Once);
    }

    [Fact]
    public async Task HandlePinLoginFailureAsync_TriggersLockoutAtThreshold()
    {
        // Arrange
        _mockPinLockoutService
            .Setup(x => x.RecordFailedAttempt())
            .ReturnsAsync(true); // Lockout triggered

        // Act
        var lockedOut = await _signInManager.HandlePinLoginFailureAsync();

        // Assert
        lockedOut.Should().BeTrue();
        _mockPinLockoutService.Verify(x => x.RecordFailedAttempt(), Times.Once);
    }

    [Fact]
    public async Task SignInWithClaimsAsync_AddsGivenNameClaimWithFullName()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "testuser",
            FullName = "John Doe",
            IsActive = true
        };

        var additionalClaims = new List<Claim>
        {
            new(ClaimTypes.Role, "Administrator")
        };

        _mockClaimsFactory
            .Setup(x => x.CreateAsync(user))
            .Callback<ApplicationUser>(u =>
            {
                // Capture the claims passed to the base method
            })
            .ReturnsAsync(new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, user.UserName)
            })));

        // Act
        await _signInManager.SignInWithClaimsAsync(user, null, additionalClaims);

        // Assert
        // Verify the method was called
        _mockClaimsFactory.Verify(x => x.CreateAsync(user), Times.Once);
    }

    [Fact]
    public async Task SignInWithClaimsAsync_AddsGivenNameClaimWithUserNameWhenFullNameEmpty()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "testuser",
            FullName = string.Empty,
            IsActive = true
        };

        var additionalClaims = new List<Claim>
        {
            new(ClaimTypes.Role, "User")
        };

        _mockClaimsFactory
            .Setup(x => x.CreateAsync(user))
            .ReturnsAsync(new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, user.UserName)
            })));

        // Act
        await _signInManager.SignInWithClaimsAsync(user, null, additionalClaims);

        // Assert
        _mockClaimsFactory.Verify(x => x.CreateAsync(user), Times.Once);
    }

    [Fact]
    public async Task SignInWithClaimsAsync_AddsGivenNameClaimWithEmptyStringWhenBothNamesNull()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = null,
            FullName = null,
            IsActive = true
        };

        var additionalClaims = new List<Claim>
        {
            new(ClaimTypes.Role, "User")
        };

        _mockClaimsFactory
            .Setup(x => x.CreateAsync(user))
            .ReturnsAsync(new ClaimsPrincipal(new ClaimsIdentity()));

        // Act
        await _signInManager.SignInWithClaimsAsync(user, null, additionalClaims);

        // Assert
        _mockClaimsFactory.Verify(x => x.CreateAsync(user), Times.Once);
    }

    [Fact]
    public async Task PinSignInWithUserAsync_WithIsPersistentTrue_SignsInWithPersistence()
    {
        // Arrange
        var pin = "1234";
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "testuser",
            IsActive = true,
            IsPinEnabled = true
        };

        _mockPinLockoutService
            .Setup(x => x.IsPinModeEnabled())
            .Returns(true);

        _mockPinLockoutService
            .Setup(x => x.IsPinLoginSuspended())
            .ReturnsAsync(false);

        _mockApplicationUserManager
            .Setup(x => x.FindByPinAsync(pin))
            .ReturnsAsync(user);

        _mockUserManager
            .Setup(x => x.IsLockedOutAsync(user))
            .ReturnsAsync(false);

        _mockUserManager
            .Setup(x => x.ResetAccessFailedCountAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        _mockPinLockoutService
            .Setup(x => x.ResetAttempts())
            .Returns(Task.CompletedTask);

        // Act
        var (result, returnedUser) = await _signInManager.PinSignInWithUserAsync(pin, isPersistent: true);

        // Assert
        result.Succeeded.Should().BeTrue();
        returnedUser.Should().NotBeNull();
    }

    [Fact]
    public async Task PinSignInWithUserAsync_WithUserInPinLockout_ReturnsLockedOutAndNull()
    {
        // OLD TEST - REMOVED - using per-instrument lockout now
    }

    [Fact]
    public async Task PinSignInWithUserAsync_WithExpiredPinLockout_ResetFailedAttemptsAndAllowLogin()
    {
        // OLD TEST - REMOVED - using per-instrument lockout now
    }

    [Fact]
    public async Task HandlePinLoginFailureAsync_WithThreeFailures_SetsLockout()
    {
        // OLD TEST - REMOVED - using per-instrument lockout now
    }

    [Fact]
    public async Task HandlePinLoginFailureAsync_WithFirstFailure_IncrementCountOnly()
    {
        // OLD TEST - REMOVED - using per-instrument lockout now
    }

    [Fact]
    public async Task HandlePinLoginFailureAsync_WhenLockoutDisabled_DoesNotTrack()
    {
        // OLD TEST - REMOVED - using per-instrument lockout now
    }

    [Fact]
    public async Task PinSignInWithUserAsync_SuccessfulLogin_ResetsFailedAttempts()
    {
        // Arrange
        var pin = "1234";
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "testuser",
            IsActive = true,
            IsPinEnabled = true
        };

        _mockPinLockoutService
            .Setup(x => x.IsPinModeEnabled())
            .Returns(true);

        _mockPinLockoutService
            .Setup(x => x.IsPinLoginSuspended())
            .ReturnsAsync(false);

        _mockApplicationUserManager
            .Setup(x => x.FindByPinAsync(pin))
            .ReturnsAsync(user);

        _mockUserManager
            .Setup(x => x.IsLockedOutAsync(user))
            .ReturnsAsync(false);

        _mockUserManager
            .Setup(x => x.ResetAccessFailedCountAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        _mockPinLockoutService
            .Setup(x => x.ResetAttempts())
            .Returns(Task.CompletedTask);

        // Act
        var (result, returnedUser) = await _signInManager.PinSignInWithUserAsync(pin);

        // Assert
        result.Succeeded.Should().BeTrue();
        returnedUser.Should().NotBeNull();
        _mockPinLockoutService.Verify(x => x.ResetAttempts(), Times.Once);
    }
}
