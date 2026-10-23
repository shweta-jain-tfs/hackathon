using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pharos.Contracts.Security;
using Pharos.Server.Application;
using Pharos.Server.Application.Settings.RnD;
using Pharos.Server.Identity.Application.Helpers;
using Pharos.Server.Identity.Domain;
using Pharos.Server.Infrastructure;

namespace Pharos.Server.Identity.Infrastructure.Authorization;

public class PharosPolicyAuthorizationHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<ICurrentInstrumentService> _currentInstrumentService = new();
    private readonly Mock<IApplicationUserManager> _userManagerMock = new();
    private readonly Mock<IApplicationRoleManager> _roleManagerMock = new();
    private readonly Mock<IPermissionProvider> _permissionProvider = new();
    private readonly Mock<IDateTime> _dateTimeMock = new();
    private readonly PharosPolicyAuthorizationHandler _sut;

    public PharosPolicyAuthorizationHandlerTests()
    {
        using var factory = new LoggerFactory();

        var rndSettings = new Mock<IOptionsSnapshot<RndSettings>>();
        rndSettings.Setup(x => x.Value).Returns(new RndSettings());
        var authService = new AuthorizationService(_userManagerMock.Object, _currentUserService.Object, _roleManagerMock.Object, _permissionProvider.Object, _dateTimeMock.Object, rndSettings.Object, factory.CreateLogger<AuthorizationService>());
        _sut = new PharosPolicyAuthorizationHandler(_currentUserService.Object, _currentInstrumentService.Object, authService, factory.CreateLogger<PharosPolicyAuthorizationHandler>());
    }

    [Fact]
    public async Task AnonymousUserCannotBeAuthorized()
    {
        var context = new AuthorizationHandlerContext([new PharosPolicyRequirement("Policy does not matter")], new ClaimsPrincipal(), null);
        await _sut.HandleAsync(context);
        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task UnknownUserCannotBeAuthorized()
    {
        _currentUserService.Setup(x => x.UserId).Returns(Guid.NewGuid);
        _userManagerMock.Setup(x => x.FindByIdAsync(It.IsAny<string>())).ReturnsAsync(() => null);

        var context = new AuthorizationHandlerContext([new PharosPolicyRequirement("Policy does not matter")], new ClaimsPrincipal(), null);
        await _sut.HandleAsync(context);
        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task LockedUserCannotBeAuthorized()
    {
        var userId = Guid.NewGuid();
        _currentUserService.Setup(x => x.UserId).Returns(userId);

        var now = DateTime.UtcNow;
        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(
            new ApplicationUser
            {
                LockoutEnabled = true,
                LockoutEnd = now.AddMinutes(1)
            });

        _dateTimeMock.Setup(x => x.UtcNow).Returns(now);

        var context = new AuthorizationHandlerContext([new PharosPolicyRequirement("Policy does not matter")], new ClaimsPrincipal(), null);
        await _sut.HandleAsync(context);
        context.HasSucceeded.Should().BeFalse();
    }


    [Fact]
    public async Task RequestWithInstrumentOnlyAllowAnonymousPolicyDoesNotNeedUser()
    {
        _currentInstrumentService.Setup(s => s.InstrumentId).Returns(1);
        var context = new AuthorizationHandlerContext([new PharosPolicyRequirement(Policies.InstrumentOnlyAllowAnonymous)], new ClaimsPrincipal(), null);
        await _sut.HandleAsync(context);
        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task RequestWithInstrumentOnlyPolicyNeedsUser()
    {
        _currentInstrumentService.Setup(s => s.InstrumentId).Returns(1);
        var context = new AuthorizationHandlerContext([new PharosPolicyRequirement(Policies.InstrumentOnly)], new ClaimsPrincipal(), null);
        await _sut.HandleAsync(context);
        context.HasSucceeded.Should().BeFalse();

        _currentUserService.Setup(x => x.UserId).Returns(Guid.NewGuid);
        context = new AuthorizationHandlerContext([new PharosPolicyRequirement(Policies.InstrumentOnly)], new ClaimsPrincipal(), null);
        await _sut.HandleAsync(context);
        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task RequestWithInstrumentOnlyAllowAnonymousPolicyWorkOnlyOnInstruments()
    {
        var context = new AuthorizationHandlerContext([new PharosPolicyRequirement(Policies.InstrumentOnlyAllowAnonymous)], new ClaimsPrincipal(), null);
        await _sut.HandleAsync(context);
        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task UserPermissionPositiveCheck()
    {
        const string permission = "foo";
        var claim = new Claim(Permissions.ClaimType, "foo");

        _currentUserService.Setup(x => x.UserId).Returns(Guid.NewGuid);
        _userManagerMock.Setup(x => x.FindByGuidAsync(It.IsAny<Guid>())).ReturnsAsync(new ApplicationUser());
        _userManagerMock.Setup(x => x.FindByIdAsync(It.IsAny<string>())).ReturnsAsync(new ApplicationUser());
        _userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(["role"]);

        _roleManagerMock.Setup(x => x.FindByNameAsync(It.IsAny<string>())).ReturnsAsync(new ApplicationRole());
        _roleManagerMock.Setup(x => x.GetClaimsAsync(It.IsAny<ApplicationRole>())).ReturnsAsync([claim]);

        _permissionProvider.Setup(x => x.GetPermissionClaims(It.IsAny<ApplicationRole>())).ReturnsAsync(new List<Claim> { claim });

        var context = new AuthorizationHandlerContext([new PharosPolicyRequirement(permission)], new ClaimsPrincipal(), null);
        await _sut.HandleAsync(context);
        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task UserPermissionNegativeCheck()
    {
        const string permission = "foo";

        _currentUserService.Setup(x => x.UserId).Returns(Guid.NewGuid);
        _userManagerMock.Setup(x => x.FindByIdAsync(It.IsAny<string>())).ReturnsAsync(new ApplicationUser());
        _userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(new List<string> { "role" });

        _roleManagerMock.Setup(x => x.FindByNameAsync(It.IsAny<string>())).ReturnsAsync(new ApplicationRole());
        _roleManagerMock.Setup(x => x.GetClaimsAsync(It.IsAny<ApplicationRole>())).ReturnsAsync(new List<Claim>());

        _permissionProvider.Setup(x => x.GetPermissionClaims(It.IsAny<ApplicationRole>())).ReturnsAsync(new List<Claim>());

        var context = new AuthorizationHandlerContext(new[] { new PharosPolicyRequirement(permission) }, new ClaimsPrincipal(), null);
        await _sut.HandleAsync(context);
        context.HasSucceeded.Should().BeFalse();
    }
}