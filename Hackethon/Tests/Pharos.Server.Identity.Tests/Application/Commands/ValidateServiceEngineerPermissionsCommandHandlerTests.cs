using System.Security.Claims;
using Pharos.Contracts.Security;
using Pharos.Server.Identity.Domain;
using Pharos.Server.Identity.Infrastructure.Authorization;
using Pharos.Server.Infrastructure;

namespace Pharos.Server.Identity.Application.Commands;

public sealed class ValidateServiceEngineerPermissionsCommandHandlerTests
{
    private static readonly Guid _serviceEngineerRoleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid _administratorRoleId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly Mock<IApplicationUserManager> _userManager = new();
    private readonly Mock<IApplicationRoleManager> _roleManager = new();
    private readonly Mock<IDateTime> _dateTime = new();

    [Fact]
    public async Task Handle_WhenServiceEngineerRoleDoesNotExist_ReturnsFalse()
    {
        _roleManager.Setup(x => x.FindByNameAsync(BuiltInRoles.ServiceEngineer)).ReturnsAsync((ApplicationRole?)null);

        var sut = new ValidateServiceEngineerPermissionsCommandHandler(_userManager.Object, _roleManager.Object, _dateTime.Object);

        var result = await sut.Handle(new ValidateServiceEngineerPermissionsCommand(), TestContext.Current.CancellationToken);

        result.Should().BeFalse();
        _roleManager.Verify(x => x.RemoveClaimAsync(It.IsAny<ApplicationRole>(), It.IsAny<Claim>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenServiceEngineerRoleHasNoManageUsersAndRolesClaim_ReturnsFalse()
    {
        var serviceEngineerRole = ApplicationRole.CreatePredefinedOrBuiltInRole(BuiltInRoles.ServiceEngineer);
        var serviceEngineerDefaultClaims = ApplicationRole.GetPredefinedOrBuiltInRolePermissions(BuiltInRoles.ServiceEngineer).ToList();

        _roleManager.Setup(x => x.FindByNameAsync(BuiltInRoles.ServiceEngineer)).ReturnsAsync(serviceEngineerRole);
        _roleManager.Setup(x => x.GetClaimsAsync(serviceEngineerRole)).ReturnsAsync(serviceEngineerDefaultClaims);

        var sut = new ValidateServiceEngineerPermissionsCommandHandler(_userManager.Object, _roleManager.Object, _dateTime.Object);

        var result = await sut.Handle(new ValidateServiceEngineerPermissionsCommand(), TestContext.Current.CancellationToken);

        result.Should().BeFalse();
        _roleManager.Verify(x => x.RemoveClaimAsync(It.IsAny<ApplicationRole>(), It.IsAny<Claim>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenEligibleNonServiceUserHasManageUsersAndRolesPermission_RemovesClaimAndReturnsTrue()
    {
        var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        _dateTime.Setup(x => x.UtcNow).Returns(now);

        var serviceEngineerRole = CreateRole(BuiltInRoles.ServiceEngineer, _serviceEngineerRoleId);
        var administratorRole = CreateRole(PredefinedRoles.Administrator, _administratorRoleId);
        var serviceEngineerClaims = GetPermissions(BuiltInRoles.ServiceEngineer);
        var manageClaim = new Claim(Permissions.ClaimType, Permissions.ManageUsersAndRoles);
        serviceEngineerClaims.Add(manageClaim);
        var administratorClaims = GetPermissions(PredefinedRoles.Administrator);

        SetupRoles(serviceEngineerRole, administratorRole, serviceEngineerClaims, administratorClaims);

        _userManager.Setup(x => x.GetUsersInRoleAsync(PredefinedRoles.Administrator)).ReturnsAsync([
            new ApplicationUser
            {
                IsActive = true,
                TemporaryPassword = false,
                LockoutEnd = null
            }
        ]);

        _roleManager.Setup(x => x.RemoveClaimAsync(serviceEngineerRole, manageClaim)).ReturnsAsync(Microsoft.AspNetCore.Identity.IdentityResult.Success);

        var sut = new ValidateServiceEngineerPermissionsCommandHandler(_userManager.Object, _roleManager.Object, _dateTime.Object);

        var result = await sut.Handle(new ValidateServiceEngineerPermissionsCommand(), TestContext.Current.CancellationToken);

        result.Should().BeTrue();
        _roleManager.Verify(x => x.RemoveClaimAsync(serviceEngineerRole, manageClaim), Times.Once);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    public async Task Handle_WhenOnlyIneligibleUserHasPermission_DoesNotRemoveClaimAndReturnsFalse(bool isActive, bool temporaryPassword, bool isLockedOut)
    {
        var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        _dateTime.Setup(x => x.UtcNow).Returns(now);

        var serviceEngineerRole = CreateRole(BuiltInRoles.ServiceEngineer, _serviceEngineerRoleId);
        var administratorRole = CreateRole(PredefinedRoles.Administrator, _administratorRoleId);
        var serviceEngineerClaims = GetPermissions(BuiltInRoles.ServiceEngineer);
        var manageClaim = new Claim(Permissions.ClaimType, Permissions.ManageUsersAndRoles);
        serviceEngineerClaims.Add(manageClaim);
        var administratorClaims = GetPermissions(PredefinedRoles.Administrator);

        SetupRoles(serviceEngineerRole, administratorRole, serviceEngineerClaims, administratorClaims);

        _userManager.Setup(x => x.GetUsersInRoleAsync(PredefinedRoles.Administrator)).ReturnsAsync([
            new ApplicationUser
            {
                IsActive = isActive,
                TemporaryPassword = temporaryPassword,
                LockoutEnd = isLockedOut ? now.AddMinutes(5) : null
            }
        ]);

        var sut = new ValidateServiceEngineerPermissionsCommandHandler(_userManager.Object, _roleManager.Object, _dateTime.Object);

        var result = await sut.Handle(new ValidateServiceEngineerPermissionsCommand(), TestContext.Current.CancellationToken);

        result.Should().BeFalse();
        _roleManager.Verify(x => x.RemoveClaimAsync(It.IsAny<ApplicationRole>(), It.IsAny<Claim>()), Times.Never);
    }

    private static ApplicationRole CreateRole(string roleName, Guid id)
    {
        var role = ApplicationRole.CreatePredefinedOrBuiltInRole(roleName);
        role.Id = id;
        return role;
    }

    private static List<Claim> GetPermissions(string roleName)
    {
        return ApplicationRole.GetPredefinedOrBuiltInRolePermissions(roleName).ToList();
    }

    private void SetupRoles(ApplicationRole serviceEngineerRole, ApplicationRole otherRole, List<Claim> serviceEngineerClaims, List<Claim> otherRoleClaims)
    {
        _roleManager.Setup(x => x.FindByNameAsync(BuiltInRoles.ServiceEngineer)).ReturnsAsync(serviceEngineerRole);
        _roleManager.Setup(x => x.GetClaimsAsync(serviceEngineerRole)).ReturnsAsync(serviceEngineerClaims);
        _roleManager.Setup(x => x.Roles).Returns(new[] { serviceEngineerRole, otherRole }.AsQueryable());
        _roleManager.Setup(x => x.GetClaimsAsync(otherRole)).ReturnsAsync(otherRoleClaims);
    }
}
