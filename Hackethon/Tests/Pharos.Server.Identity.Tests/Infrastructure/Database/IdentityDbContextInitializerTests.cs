using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pharos.Contracts.Security;
using Pharos.Server.Identity.Domain;
using Pharos.Server.Identity.Infrastructure.Authorization;
using Pharos.Server.Infrastructure.Services;

namespace Pharos.Server.Identity.Infrastructure.Database;

public class IdentityDbContextInitializerTests
{
    private readonly ILogger<IdentityDbContextInitializer> _logger;
    private IdentityDbContextInitializer? _sut;

    public IdentityDbContextInitializerTests()
    {
        var factory = new LoggerFactory();
        _logger = factory.CreateLogger<IdentityDbContextInitializer>();
        factory.Dispose();
    }

    [Theory]
    [InlineData(BuiltInRoles.ServiceEngineer)]
    [InlineData(BuiltInRoles.RndUser)]
    public async Task BuiltInRoleIsCreatedWhenItDoesNotExist(string roleName)
    {
        var roleMangerMock = new Mock<IApplicationRoleManager>();

        var options = new DbContextOptionsBuilder<IdentityDbContext<ApplicationUser, ApplicationRole, Guid>>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var dbContext = new IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options);
        dbContext.Roles.Add(new ApplicationRole { Name = "existing-role", NormalizedName = "EXISTING-ROLE" });
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        roleMangerMock.Setup(x => x.Roles).Returns(dbContext.Roles);
        roleMangerMock.Setup(x => x.FindByNameAsync(It.IsAny<string>()))
            .ReturnsAsync((string name) => dbContext.Roles.FirstOrDefault(r => r.Name == name));

        var createdRoles = new List<ApplicationRole>();
        roleMangerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationRole>()))
            .Callback<ApplicationRole>(r => createdRoles.Add(r))
            .ReturnsAsync(IdentityResult.Success);

        var addedClaims = new List<Claim>();
        roleMangerMock.Setup(x => x.AddClaimAsync(It.IsAny<ApplicationRole>(), It.IsAny<Claim>()))
            .Callback<ApplicationRole, Claim>((_, claim) => addedClaims.Add(claim))
            .ReturnsAsync(IdentityResult.Success);

        _sut = new IdentityDbContextInitializer(roleMangerMock.Object, null!, _logger, new Mock<IDbEncryptionService>().Object);

        await _sut.InitializeRoles();

        var defaultPermissions = ApplicationRole.GetPredefinedOrBuiltInRolePermissions(roleName);

        createdRoles.Should().ContainSingle(r => r.Name == roleName);
        roleMangerMock.Verify(x => x.CreateAsync(It.Is<ApplicationRole>(r => r.Name == roleName)), Times.Once);
        roleMangerMock.Verify(x => x.GetClaimsAsync(It.IsAny<ApplicationRole>()), Times.Never);
        foreach (var permission in defaultPermissions)
        {
            addedClaims.Should().Contain(c => c.Type == permission.Type && c.Value == permission.Value);
        }

        if (roleName == BuiltInRoles.ServiceEngineer)
        {
            addedClaims.Should().Contain(c => c.Type == Permissions.ClaimType && c.Value == Permissions.ManageUsersAndRoles);
        }
    }

    [Theory]
    [InlineData(BuiltInRoles.ServiceEngineer)]
    [InlineData(BuiltInRoles.RndUser)]
    public async Task BuildInRoleIsUpdatedInDatabaseWhenPermissionsChangeInCode(string roleName)
    {
        var roleMangerMock = new Mock<IApplicationRoleManager>();

        var builtInRole = ApplicationRole.CreatePredefinedOrBuiltInRole(roleName);
        var existingClaims = new List<Claim> { new(Permissions.ClaimType, "foo") };
        var defaultPermissions = ApplicationRole.GetPredefinedOrBuiltInRolePermissions(roleName);

        roleMangerMock.Setup(x => x.FindByNameAsync(roleName)).ReturnsAsync(builtInRole);
        roleMangerMock.Setup(x => x.GetClaimsAsync(builtInRole)).ReturnsAsync(existingClaims);
        roleMangerMock.Setup(x => x.RemoveClaimAsync(builtInRole, It.IsAny<Claim>())).ReturnsAsync(IdentityResult.Success);
        roleMangerMock.Setup(x => x.AddClaimAsync(builtInRole, It.IsAny<Claim>())).ReturnsAsync(IdentityResult.Success);

        _sut = new IdentityDbContextInitializer(roleMangerMock.Object, null!, _logger, new Mock<IDbEncryptionService>().Object);

        await _sut.InitializeBuiltInRole(roleName);

        roleMangerMock.Verify(x => x.RemoveClaimAsync(builtInRole, It.IsAny<Claim>()), Times.Exactly(existingClaims.Count));
        roleMangerMock.Verify(x => x.AddClaimAsync(builtInRole, It.IsAny<Claim>()), Times.Exactly(defaultPermissions.Count));
    }

    [Theory]
    [InlineData(PredefinedRoles.LabAssistant)]
    [InlineData(PredefinedRoles.Operator)]
    [InlineData(PredefinedRoles.SeniorOperator)]
    [InlineData(PredefinedRoles.Administrator)]
    public async Task PredefinedRolesAreNotRecreated_WhenAnyRoleAlreadyExists(string predefinedRole)
    {
        var roleMangerMock = new Mock<IApplicationRoleManager>();

        var options = new DbContextOptionsBuilder<IdentityDbContext<ApplicationUser, ApplicationRole, Guid>>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var dbContext = new IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options);
        dbContext.Roles.Add(ApplicationRole.CreatePredefinedOrBuiltInRole(predefinedRole));
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        roleMangerMock.Setup(x => x.Roles).Returns(dbContext.Roles);
        roleMangerMock.Setup(x => x.FindByNameAsync(It.IsAny<string>())).ReturnsAsync(() => null);
        roleMangerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationRole>())).ReturnsAsync(IdentityResult.Success);
        roleMangerMock.Setup(x => x.AddClaimAsync(It.IsAny<ApplicationRole>(), It.IsAny<Claim>())).ReturnsAsync(IdentityResult.Success);

        _sut = new IdentityDbContextInitializer(roleMangerMock.Object, null!, _logger, new Mock<IDbEncryptionService>().Object);

        await _sut.InitializeRoles();

        roleMangerMock.Verify(x => x.CreateAsync(It.Is<ApplicationRole>(r => r.Name == predefinedRole)), Times.Never);
    }

    [Fact]
    public Task CheckPermissionListPermissions()
    {
        // if the list of permissions is changed, verify builtin and predefined roles are updated
        Permissions.All.All(x => !string.IsNullOrEmpty(x)).Should().BeTrue();
        Permissions.All.Should().Contain(Permissions.ManageInstruments);
        Permissions.All.Should().Contain(Permissions.EditServiceSettings);
        Permissions.All.Should().Contain(Permissions.EditReportTemplates);
        Permissions.All.Should().Contain(Permissions.EditTestSettings);
        Permissions.All.Should().Contain(Permissions.EditQualityControlSettings);
        Permissions.All.Should().Contain(Permissions.EditSystemSettings);
        Permissions.All.Should().Contain(Permissions.ExportTestResults);
        Permissions.All.Should().Contain(Permissions.ManageInstruments);
        Permissions.All.Should().Contain(Permissions.ManageReagents);
        Permissions.All.Should().Contain(Permissions.ManageReagentStock);
        Permissions.All.Should().Contain(Permissions.ManageSamples);
        Permissions.All.Should().Contain(Permissions.ManageTestOrders);
        Permissions.All.Should().Contain(Permissions.ManageUsersAndRoles);
        Permissions.All.Should().Contain(Permissions.RunInstrumentMaintenance);
        Permissions.All.Should().Contain(Permissions.RunServiceTools);
        Permissions.All.Should().Contain(Permissions.ViewAuditTrail);
        Permissions.All.Should().Contain(Permissions.ViewPersonalAndHealthData);
        Permissions.All.Should().Contain(Permissions.ApproveTestResults);
        Permissions.All.Should().Contain(Permissions.AccessInstrumentServiceUi);

        var matchCount = Permissions.All.Count;
        matchCount.Should().Be(18);

        return Task.CompletedTask;
    }

    [Fact]
    public void InitializeBuiltInRole_ServiceEngineerRole_HasNoDefaultManageUsersAndRolesPermission()
    {
        ApplicationRole.GetPredefinedOrBuiltInRolePermissions(BuiltInRoles.ServiceEngineer)
            .Should().NotContain(c => c.Type == Permissions.ClaimType && c.Value == Permissions.ManageUsersAndRoles);
    }

    [Fact]
    public async Task InitializeBuiltInRole_WhenServiceEngineerRoleIsCreated_AddsManageUsersAndRolesClaim()
    {
        var roleManagerMock = new Mock<IApplicationRoleManager>();
        roleManagerMock.Setup(x => x.FindByNameAsync(BuiltInRoles.ServiceEngineer)).ReturnsAsync((ApplicationRole?)null);
        roleManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationRole>())).ReturnsAsync(IdentityResult.Success);

        var addedClaims = new List<Claim>();
        roleManagerMock.Setup(x => x.AddClaimAsync(It.IsAny<ApplicationRole>(), It.IsAny<Claim>()))
            .Callback<ApplicationRole, Claim>((_, claim) => addedClaims.Add(claim))
            .ReturnsAsync(IdentityResult.Success);

        _sut = new IdentityDbContextInitializer(roleManagerMock.Object, null!, _logger, new Mock<IDbEncryptionService>().Object);

        await _sut.InitializeBuiltInRole(BuiltInRoles.ServiceEngineer);

        addedClaims.Should().ContainSingle(c => c.Type == Permissions.ClaimType && c.Value == Permissions.ManageUsersAndRoles);
    }

    [Fact]
    public async Task InitializeBuiltInRole_WhenServiceEngineerRoleAlreadyHasManageUsersAndRoles_ItIsNotRemoved()
    {
        var roleManagerMock = new Mock<IApplicationRoleManager>();
        var serviceEngineerRole = ApplicationRole.CreatePredefinedOrBuiltInRole(BuiltInRoles.ServiceEngineer);
        var existingClaims = ApplicationRole.GetPredefinedOrBuiltInRolePermissions(BuiltInRoles.ServiceEngineer)
            .Append(new Claim(Permissions.ClaimType, Permissions.ManageUsersAndRoles))
            .Append(new Claim(Permissions.ClaimType, "random"))
            .ToList();

        roleManagerMock.Setup(x => x.FindByNameAsync(BuiltInRoles.ServiceEngineer)).ReturnsAsync(serviceEngineerRole);
        roleManagerMock.Setup(x => x.GetClaimsAsync(serviceEngineerRole)).ReturnsAsync(existingClaims);

        _sut = new IdentityDbContextInitializer(roleManagerMock.Object, null!, _logger, new Mock<IDbEncryptionService>().Object);

        await _sut.InitializeBuiltInRole(BuiltInRoles.ServiceEngineer);

        roleManagerMock.Verify(x => x.RemoveClaimAsync(It.IsAny<ApplicationRole>(), It.Is<Claim>(c => c.Value == Permissions.ManageUsersAndRoles)), Times.Never);
    }
}