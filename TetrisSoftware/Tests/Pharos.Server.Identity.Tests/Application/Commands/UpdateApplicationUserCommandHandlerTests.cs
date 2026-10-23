using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pharos.Contracts.Security;
using Pharos.Server.Application;
using Pharos.Server.Application.Security;
using Pharos.Server.Application.Settings.Security;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Identity.Domain;
using Pharos.Server.Identity.Infrastructure.Authorization;
using Pharos.Server.Identity.Infrastructure.Hubs;
using Pharos.Server.Infrastructure;
using Pharos.Server.Localization;

#pragma warning disable CA2000
#pragma warning disable CA1863 // Use 'CompositeFormat'

namespace Pharos.Server.Identity.Application.Commands;

// ReSharper disable once InconsistentNaming
[Trait("ModuleRequirement", "MR_7")]
public sealed class MR_7_UpdateApplicationUserCommandHandlerTests : IDisposable
{
    private readonly MemoryCache _memoryCache = new(new MemoryCacheOptions());
    private readonly Mock<IDateTime> _dateTime = new();
    private TestHost _host = null!;

    public MR_7_UpdateApplicationUserCommandHandlerTests()
    {
        _dateTime.Setup(x => x.Now).Returns(DateTimeOffset.UtcNow);
        _dateTime.Setup(x => x.UtcNow).Returns(DateTimeOffset.UtcNow);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_7")]
    public async Task Handle_WhenUserDoesNotExist_ThrowsObjectGoneException()
    {
        CreateHost();
        await SeedRoleAsync(_host, PredefinedRoles.Operator, Guid.NewGuid());

        var request = new UpdateApplicationUserCommand("username", "Full Name", Guid.NewGuid(), true, null, false);

        var act = () => _host.Sut.Handle(request, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ObjectGoneException>();
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_7")]
    public async Task Handle_WhenDuplicateUsernameExists_ThrowsValidationException()
    {
        CreateHost();
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        var existingUser = await SeedUserAsync(_host, userId, "old-name");
        var duplicateUser = await SeedUserAsync(_host, Guid.NewGuid(), "username");
        await SeedRoleAsync(_host, PredefinedRoles.Operator, roleId);
        await AssignRoleAsync(_host, existingUser, PredefinedRoles.Operator);
        await AssignRoleAsync(_host, duplicateUser, PredefinedRoles.Operator);

        var request = new UpdateApplicationUserCommand("username", "Full Name", userId, true, roleId, false);

        var act = () => _host.Sut.Handle(request, TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<ValidationException>();
        exception.Which.Message.Should().Be(Translations.UsernameExists);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_7")]
    public async Task Handle_UpdatesAllUserProperties()
    {
        CreateHost(enablePinLogin: true);
        var userId = Guid.NewGuid();
        await SeedUserAsync(_host, userId, "old-username", isActive: true, isPinEnabled: false);

        var request = new UpdateApplicationUserCommand("new-username", "Updated Name", userId, false, null, true);

        await _host.Sut.Handle(request, TestContext.Current.CancellationToken);

        var updatedUser = await _host.IdentityUserManager.FindByIdAsync(userId.ToString());
        updatedUser.Should().NotBeNull();
        updatedUser.UserName.Should().Be("new-username");
        updatedUser.FullName.Should().Be("Updated Name");
        updatedUser.IsActive.Should().BeFalse();
        updatedUser.IsPinEnabled.Should().BeTrue();
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_7")]
    public async Task Handle_WhenChangingOwnRole_ThrowsValidationException()
    {
        var userId = Guid.NewGuid();
        CreateHost(currentUserId: userId);
        var oldRoleId = Guid.NewGuid();
        var newRoleId = Guid.NewGuid();

        var user = await SeedUserAsync(_host, userId, "username");
        await SeedRoleAsync(_host, PredefinedRoles.Operator, oldRoleId);
        await SeedRoleAsync(_host, PredefinedRoles.Administrator, newRoleId);
        await AssignRoleAsync(_host, user, PredefinedRoles.Operator);

        var request = new UpdateApplicationUserCommand("username", "Full Name", userId, true, newRoleId, false);

        var act = () => _host.Sut.Handle(request, TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<ValidationException>();
        exception.Which.Message.Should().Be(Translations.UpdateApplicationUser_CannotUpdateOwnRole);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_7")]
    public async Task Handle_WhenAssigningRndRoleAndRndModeDisabled_ThrowsValidationException()
    {
        CreateHost(rndMode: false);
        var userId = Guid.NewGuid();
        var oldRoleId = Guid.NewGuid();
        var rndRoleId = Guid.NewGuid();

        var user = await SeedUserAsync(_host, userId, "username");
        await SeedRoleAsync(_host, PredefinedRoles.Operator, oldRoleId);
        await SeedRoleAsync(_host, BuiltInRoles.RndUser, rndRoleId);
        await AssignRoleAsync(_host, user, PredefinedRoles.Operator);

        var request = new UpdateApplicationUserCommand("username", "Full Name", userId, true, rndRoleId, false);

        var act = () => _host.Sut.Handle(request, TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<ValidationException>();
        exception.Which.Message.Should().Be(Translations.UpdateApplicationUser_RndRoleRequiresRndMode);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_7")]
    public async Task Handle_WhenRoleIdProvidedAndRoleDoesNotExist_ThrowsValidationException()
    {
        CreateHost();
        var userId = Guid.NewGuid();

        await SeedUserAsync(_host, userId, "username");

        var request = new UpdateApplicationUserCommand("username", "Full Name", userId, true, Guid.NewGuid(), false);

        var act = () => _host.Sut.Handle(request, TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<ValidationException>();
        exception.Which.Message.Should().Be(Translations.UpdateApplicationUser_RoleDoesNotExist);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_7")]
    public async Task Handle_WhenAssigningServiceEngineerRoleDirectly_ThrowsValidationException()
    {
        CreateHost();
        var userId = Guid.NewGuid();
        var oldRoleId = Guid.NewGuid();
        var serviceEngineerRoleId = Guid.NewGuid();

        var user = await SeedUserAsync(_host, userId, "username");
        await SeedRoleAsync(_host, PredefinedRoles.Operator, oldRoleId);
        await SeedRoleAsync(_host, BuiltInRoles.ServiceEngineer, serviceEngineerRoleId);
        await AssignRoleAsync(_host, user, PredefinedRoles.Operator);

        var request = new UpdateApplicationUserCommand("username", "Full Name", userId, true, serviceEngineerRoleId, false);

        var act = () => _host.Sut.Handle(request, TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<ValidationException>();
        exception.Which.Message.Should().Be(string.Format(
            Translations.UpdateApplicationUser_ServiceEngineerRoleCannotBeAssignedDirectly,
            Translations.ApplicationRole_ServiceEngineer));
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_7")]
    public async Task Handle_WhenRevokingServiceEngineerRole_ThrowsValidationException()
    {
        CreateHost();
        var userId = Guid.NewGuid();
        var serviceEngineerRoleId = Guid.NewGuid();
        var newRoleId = Guid.NewGuid();

        var user = await SeedUserAsync(_host, userId, "username");
        await SeedRoleAsync(_host, BuiltInRoles.ServiceEngineer, serviceEngineerRoleId);
        await SeedRoleAsync(_host, PredefinedRoles.Operator, newRoleId);
        await AssignRoleAsync(_host, user, BuiltInRoles.ServiceEngineer);

        var request = new UpdateApplicationUserCommand("username", "Full Name", userId, true, newRoleId, false);

        var act = () => _host.Sut.Handle(request, TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<ValidationException>();
        exception.Which.Message.Should().Be(string.Format(
            Translations.UpdateApplicationUser_ServiceEngineerRoleCannotBeRevoked,
            Translations.ApplicationRole_ServiceEngineer));
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_7")]
    public async Task Handle_WhenPinEnabledForServiceEngineer_ThrowsValidationException()
    {
        CreateHost(enablePinLogin: true);
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        var user = await SeedUserAsync(_host, userId, "username", isPinEnabled: false);
        await SeedRoleAsync(_host, BuiltInRoles.ServiceEngineer, roleId);
        await AssignRoleAsync(_host, user, BuiltInRoles.ServiceEngineer);

        var request = new UpdateApplicationUserCommand("username", "Full Name", userId, true, roleId, true);

        var act = () => _host.Sut.Handle(request, TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<ValidationException>();
        exception.Which.Message.Should().Be(Translations.SetPin_ServiceEngineerPinNotAllowed);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_7")]
    public async Task Handle_WhenDeactivatingCurrentUser_ThrowsValidationException()
    {
        var userId = Guid.NewGuid();
        CreateHost(currentUserId: userId);
        var roleId = Guid.NewGuid();

        var user = await SeedUserAsync(_host, userId, "username", isActive: true);
        await SeedRoleAsync(_host, PredefinedRoles.Operator, roleId);
        await AssignRoleAsync(_host, user, PredefinedRoles.Operator);

        var request = new UpdateApplicationUserCommand("username", "Full Name", userId, false, roleId, false);

        var act = () => _host.Sut.Handle(request, TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<ValidationException>();
        exception.Which.Message.Should().Be(Translations.UpdateApplicationUser_CannotDeactivateUser);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_7")]
    public async Task Handle_WhenDeactivatingServiceEngineer_ThrowsValidationException()
    {
        CreateHost(currentUserId: Guid.NewGuid());
        var userId = Guid.NewGuid();
        var serviceEngineerRoleId = Guid.NewGuid();

        var user = await SeedUserAsync(_host, userId, "username", isActive: true);
        await SeedRoleAsync(_host, BuiltInRoles.ServiceEngineer, serviceEngineerRoleId);
        await AssignRoleAsync(_host, user, BuiltInRoles.ServiceEngineer);

        var request = new UpdateApplicationUserCommand("username", "Full Name", userId, false, serviceEngineerRoleId, false);

        var act = () => _host.Sut.Handle(request, TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<ValidationException>();
        exception.Which.Message.Should().Be(Translations.UpdateApplicationUser_CannotDeactivateUser);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_7")]
    public async Task Handle_WhenPinLoginDisabled_DoesNotChangePinFlag()
    {
        CreateHost(enablePinLogin: false);
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        var user = await SeedUserAsync(_host, userId, "username", isPinEnabled: true);
        await SeedRoleAsync(_host, PredefinedRoles.Operator, roleId);
        await AssignRoleAsync(_host, user, PredefinedRoles.Operator);

        var request = new UpdateApplicationUserCommand("username", "Full Name", userId, true, roleId, false);

        await _host.Sut.Handle(request, TestContext.Current.CancellationToken);

        var updatedUser = await _host.IdentityUserManager.FindByIdAsync(userId.ToString());
        updatedUser.Should().NotBeNull();
        updatedUser.IsPinEnabled.Should().BeTrue();
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_7")]
    public async Task Handle_WhenPinOnlyLoginIsDisabledForUser_ResetsPinHash()
    {
        CreateHost(enablePinLogin: true);
        var userId = Guid.NewGuid();

        var user = await SeedUserAsync(_host, userId, "username", isActive: true, isPinEnabled: true);
        user.PinHash = "existing-pin-hash";
        await _host.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = new UpdateApplicationUserCommand("username", "Full Name", userId, true, null, false);

        await _host.Sut.Handle(request, TestContext.Current.CancellationToken);

        var updatedUser = await _host.IdentityUserManager.FindByIdAsync(userId.ToString());
        updatedUser.Should().NotBeNull();
        updatedUser.IsPinEnabled.Should().BeFalse();
        updatedUser.PinHash.Should().BeNull();
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_7")]
    public async Task Handle_WhenRoleDidNotChange_LeavesRoleMembershipUntouched()
    {
        CreateHost();
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        var user = await SeedUserAsync(_host, userId, "username");
        await SeedRoleAsync(_host, PredefinedRoles.Operator, roleId);
        await AssignRoleAsync(_host, user, PredefinedRoles.Operator);

        var request = new UpdateApplicationUserCommand("username", "Full Name", userId, true, roleId, false);

        await _host.Sut.Handle(request, TestContext.Current.CancellationToken);

        var roles = await _host.IdentityUserManager.GetRolesAsync(user);
        roles.Should().ContainSingle(x => x == PredefinedRoles.Operator);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_7")]
    public async Task Handle_WhenRoleChangedAndExistingRolesPresent_ReplacesRole()
    {
        CreateHost(enablePinLogin: true);
        var userId = Guid.NewGuid();
        var oldRoleId = Guid.NewGuid();
        var newRoleId = Guid.NewGuid();

        var user = await SeedUserAsync(_host, userId, "username", isPinEnabled: false);
        await SeedRoleAsync(_host, PredefinedRoles.Operator, oldRoleId);
        await SeedRoleAsync(_host, PredefinedRoles.Administrator, newRoleId);
        await AssignRoleAsync(_host, user, PredefinedRoles.Operator);

        var request = new UpdateApplicationUserCommand("new-username", "New Name", userId, false, newRoleId, true);

        await _host.Sut.Handle(request, TestContext.Current.CancellationToken);

        var updatedUser = await _host.IdentityUserManager.FindByIdAsync(userId.ToString());
        updatedUser.Should().NotBeNull();
        updatedUser.UserName.Should().Be("new-username");
        updatedUser.FullName.Should().Be("New Name");
        updatedUser.IsActive.Should().BeFalse();
        updatedUser.IsPinEnabled.Should().BeTrue();

        var roles = await _host.IdentityUserManager.GetRolesAsync(updatedUser);
        roles.Should().ContainSingle(x => x == PredefinedRoles.Administrator);
        _host.CurrentUserHub.Verify(x => x.UserInfoChanged(userId), Times.Once);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_7")]
    public async Task Handle_WhenRoleChangedAndNoExistingRoles_AddsNewRole()
    {
        CreateHost();
        var userId = Guid.NewGuid();
        var newRoleId = Guid.NewGuid();

        var user = await SeedUserAsync(_host, userId, "username");
        await SeedRoleAsync(_host, PredefinedRoles.Administrator, newRoleId);

        var request = new UpdateApplicationUserCommand("username", "Full Name", userId, true, newRoleId, false);

        await _host.Sut.Handle(request, TestContext.Current.CancellationToken);

        var roles = await _host.IdentityUserManager.GetRolesAsync(user);
        roles.Should().ContainSingle(x => x == PredefinedRoles.Administrator);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_7")]
    public async Task Handle_WhenRoleChangedToNoRole_RemovesExistingRolesAndDoesNotAddNewRole()
    {
        CreateHost();
        var userId = Guid.NewGuid();
        var oldRoleId = Guid.NewGuid();

        var user = await SeedUserAsync(_host, userId, "username");
        await SeedRoleAsync(_host, PredefinedRoles.Operator, oldRoleId);
        await AssignRoleAsync(_host, user, PredefinedRoles.Operator);

        var request = new UpdateApplicationUserCommand("username", "Full Name", userId, true, null, false);

        await _host.Sut.Handle(request, TestContext.Current.CancellationToken);

        var roles = await _host.IdentityUserManager.GetRolesAsync(user);
        roles.Should().BeEmpty();
    }

    private void CreateHost(bool enablePinLogin = false, bool rndMode = true, Guid? currentUserId = null)
    {
        var dbOptions = new DbContextOptionsBuilder<IdentityDbContext<ApplicationUser, ApplicationRole, Guid>>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var dbContext = new IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(dbOptions);

        var identityOptions = new IdentityOptions
        {
            Password = new PasswordOptions
            {
                RequireDigit = false,
                RequireLowercase = false,
                RequireUppercase = false,
                RequireNonAlphanumeric = false,
                RequiredLength = 1
            }
        };

        var userStore = new UserStore<ApplicationUser, ApplicationRole, IdentityDbContext<ApplicationUser, ApplicationRole, Guid>, Guid>(dbContext);
        var roleStore = new RoleStore<ApplicationRole, IdentityDbContext<ApplicationUser, ApplicationRole, Guid>, Guid>(dbContext);

        var userLogger = new Mock<ILogger<UserManager<ApplicationUser>>>();
        var roleLogger = new Mock<ILogger<RoleManager<ApplicationRole>>>();

        var identityUserManager = new UserManager<ApplicationUser>(
            userStore,
            Options.Create(identityOptions),
            new PasswordHasher<ApplicationUser>(),
            null!,
            null!,
            null!,
            null!,
            null!,
            userLogger.Object);

        var identityRoleManager = new RoleManager<ApplicationRole>(
            roleStore,
            null!,
            null!,
            null!,
            roleLogger.Object);

        var securityOptions = new Mock<IOptionsSnapshot<SecuritySettings>>();
        securityOptions.Setup(x => x.Value).Returns(new SecuritySettings
        {
            EnablePinLogin = enablePinLogin,
            PasswordComplexitySettings = new PasswordComplexitySettings
            {
                RequireDigit = false,
                RequireBothUpperAndLowerCase = false,
                RequireNonAlphanumeric = false,
                RequiredLength = 1,
                RequireNoUsernameInPassword = false
            }
        });

        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.SetupGet(x => x.UserId).Returns(currentUserId);

        var authorizationService = new Mock<IAuthorizationService>();
        authorizationService.SetupGet(x => x.RndMode).Returns(rndMode);

        var currentUserHub = new Mock<ICurrentUserServerHub>();

        var appUserManager = new ApplicationUserManager(identityUserManager, securityOptions.Object, _dateTime.Object, _memoryCache);
        var appRoleManager = new ApplicationRoleManager(identityRoleManager);

        var sut = new UpdateApplicationUserCommandHandler(
            appUserManager,
            appRoleManager,
            currentUserService.Object,
            authorizationService.Object,
            securityOptions.Object,
            currentUserHub.Object);

        _host = new TestHost(dbContext, identityUserManager, identityRoleManager, currentUserHub, sut);
    }

    private static async Task<ApplicationUser> SeedUserAsync(TestHost host, Guid userId, string userName, bool isActive = true, bool isPinEnabled = false)
    {
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = userName,
            NormalizedUserName = userName,
            FullName = null,
            IsActive = isActive,
            TemporaryPassword = false,
            IsPinEnabled = isPinEnabled,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString()
        };

        host.DbContext.Users.Add(user);
        await host.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        return user;
    }

    private static async Task SeedRoleAsync(TestHost host, string roleName, Guid roleId)
    {
        var role = ApplicationRole.CreatePredefinedOrBuiltInRole(roleName);
        role.Id = roleId;
        role.NormalizedName = roleName;

        var createResult = await host.IdentityRoleManager.CreateAsync(role);
        createResult.Succeeded.Should().BeTrue();
    }

    private static async Task AssignRoleAsync(TestHost host, ApplicationUser user, string roleName)
    {
        var addToRoleResult = await host.IdentityUserManager.AddToRoleAsync(user, roleName);
        addToRoleResult.Succeeded.Should().BeTrue();
    }

    public void Dispose()
    {
        _host.IdentityUserManager.Dispose();
        _host.IdentityRoleManager.Dispose();
        _host.DbContext.Dispose();
        _memoryCache.Dispose();
    }

    private sealed record TestHost(
        IdentityDbContext<ApplicationUser, ApplicationRole, Guid> DbContext,
        UserManager<ApplicationUser> IdentityUserManager,
        RoleManager<ApplicationRole> IdentityRoleManager,
        Mock<ICurrentUserServerHub> CurrentUserHub,
        UpdateApplicationUserCommandHandler Sut);
}