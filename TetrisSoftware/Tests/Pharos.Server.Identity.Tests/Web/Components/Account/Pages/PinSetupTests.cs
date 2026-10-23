using System.Text.RegularExpressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pharos.Contracts.Security;
using Pharos.Server.Application;
using Pharos.Server.Application.Settings.Security;
using Pharos.Server.Identity.Domain;
using Pharos.Server.Identity.Infrastructure.Authorization;
using Pharos.Server.Infrastructure;

namespace Pharos.Server.Identity.Web.Components.Account.Pages;

public sealed partial class PinSetupTests : IDisposable
{
    private readonly IdentityDbContext<ApplicationUser, ApplicationRole, Guid> _dbContext;
    private readonly UserManager<ApplicationUser> _identityUserManager;
    private readonly ApplicationUserManager _applicationUserManager;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<IOptionsSnapshot<SecuritySettings>> _mockSecuritySettings;
    private readonly Mock<ILogger<PinSetup>> _mockLogger;
    private readonly MemoryCache _memoryCache;

    public PinSetupTests()
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

        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockSecuritySettings = new Mock<IOptionsSnapshot<SecuritySettings>>();
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
        var mockDateTime = new Mock<IDateTime>();
        mockDateTime.Setup(x => x.Now).Returns(new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero));
        _mockLogger = new Mock<ILogger<PinSetup>>();

        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _applicationUserManager = new ApplicationUserManager(
            _identityUserManager,
            _mockSecuritySettings.Object,
            mockDateTime.Object,
            _memoryCache);
    }

    [Fact]
    public async Task OnInitialized_WhenPinLoginDisabled_RedirectsToHome()
    {
        await CreateUserAsync("pin-disabled-user");
        await using var ctx = CreateTestContext(enablePinLogin: false, userId: Guid.NewGuid());
        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();

        _ = ctx.Render<PinSetup>();

        navigationManager.Uri.Should().Be("http://localhost/");
    }

    [Fact]
    public async Task OnInitialized_WhenUserNeedsNewPinAndGenerationSucceeds_ShowsGeneratedPin()
    {
        var user = await CreateUserAsync("pin-user");
        user.IsPinEnabled = true;
        user.PinHash = null;
        var updateResult = await _identityUserManager.UpdateAsync(user);
        updateResult.Succeeded.Should().BeTrue();

        await using var ctx = CreateTestContext(enablePinLogin: true, userId: user.Id);
        var cut = ctx.Render<PinSetup>();
        var updatedUser = await _identityUserManager.FindByIdAsync(user.Id.ToString());

        updatedUser.Should().NotBeNull();
        updatedUser.PinHash.Should().NotBeNullOrWhiteSpace();
        updatedUser.IsPinEnabled.Should().BeTrue();

        var pinDisplayMatches = PinRegex().Matches(cut.Markup);
        pinDisplayMatches.Count.Should().BeGreaterThan(0);

        var pinMatch = PinRegex().Match(cut.Markup);
        pinMatch.Success.Should().BeTrue();
        var generatedPin = pinMatch.Groups[1].Value;
        var foundUser = await _applicationUserManager.FindByPinAsync(generatedPin);
        foundUser.Should().NotBeNull();
        foundUser.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task OnInitialized_WhenUserDoesNotNeedNewPin_RedirectsToReturnUrl()
    {
        var user = await CreateUserAsync("existing-pin-user");
        user.IsPinEnabled = false;
        user.PinHash = null;
        var updateResult = await _identityUserManager.UpdateAsync(user);
        updateResult.Succeeded.Should().BeTrue();

        await using var ctx = CreateTestContext(enablePinLogin: true, userId: user.Id);
        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("http://localhost/account/pinsetup?returnUrl=%2Fdashboard");

        _ = ctx.Render<PinSetup>();

        navigationManager.Uri.Should().Be("http://localhost/dashboard");
    }

    [Fact]
    public async Task OnInitialized_WhenPinGenerationFailsAfterMaxAttempts_RedirectsToHome()
    {
        var user = await CreateUserAsync("pin-fail-user");
        user.IsPinEnabled = true;
        user.PinHash = null;
        var updateResult = await _identityUserManager.UpdateAsync(user);
        updateResult.Succeeded.Should().BeTrue();
        await AddRoleToUserAsync(user, BuiltInRoles.ServiceEngineer);

        await using var ctx = CreateTestContext(enablePinLogin: true, userId: user.Id);
        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();

        _ = ctx.Render<PinSetup>();

        var updatedUser = await _identityUserManager.FindByIdAsync(user.Id.ToString());

        navigationManager.Uri.Should().Be("http://localhost/");
        updatedUser.Should().NotBeNull();
        updatedUser.PinHash.Should().BeNull();
    }

    [Fact]
    public async Task OnInitialized_WhenCurrentUserIsMissing_RedirectsToHome()
    {
        await CreateUserAsync("different-user");
        await using var ctx = CreateTestContext(enablePinLogin: true, userId: Guid.NewGuid());
        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();

        _ = ctx.Render<PinSetup>();

        navigationManager.Uri.Should().Be("http://localhost/");
    }

    public void Dispose()
    {
        _identityUserManager.Dispose();
        _dbContext.Dispose();
        _memoryCache.Dispose();
    }

    private BunitContext CreateTestContext(bool enablePinLogin, Guid? userId)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        _mockSecuritySettings.Setup(x => x.Value).Returns(new SecuritySettings
        {
            EnablePinLogin = enablePinLogin,
            PasswordComplexitySettings = new PasswordComplexitySettings
            {
                RequireDigit = true,
                RequireBothUpperAndLowerCase = true,
                RequireNonAlphanumeric = true,
                RequiredLength = 8,
                RequireNoUsernameInPassword = true
            }
        });
        _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);

        ctx.Services.AddSingleton<IApplicationUserManager>(_applicationUserManager);
        ctx.Services.AddSingleton(_mockCurrentUserService.Object);
        ctx.Services.AddSingleton(_mockSecuritySettings.Object);
        ctx.Services.AddSingleton(_mockLogger.Object);
        return ctx;
    }

    private async Task<ApplicationUser> CreateUserAsync(string username)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = username,
            IsActive = true,
            TemporaryPassword = false,
            IsPinEnabled = false,
            PinHash = null
        };

        var createResult = await _identityUserManager.CreateAsync(user, "CorrectPass1!");
        createResult.Succeeded.Should().BeTrue();
        return user;
    }

    private async Task AddRoleToUserAsync(ApplicationUser user, string roleName)
    {
        _dbContext.Roles.Add(new ApplicationRole
        {
            Id = Guid.NewGuid(),
            Name = roleName,
            NormalizedName = roleName.ToUpperInvariant()
        });
        await _dbContext.SaveChangesAsync(Xunit.TestContext.Current.CancellationToken);

        var addToRoleResult = await _identityUserManager.AddToRoleAsync(user, roleName);
        addToRoleResult.Succeeded.Should().BeTrue();
    }

    [GeneratedRegex(@">(\d{4})<")]
    private static partial Regex PinRegex();
}