using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Contracts.Security;
using Pharos.Server.Application.Identity;
using Pharos.Server.Identity.Domain;
using Pharos.Server.Identity.Infrastructure.Authorization;
using Pharos.Server.Integration.Tests.Common;

namespace Pharos.Server.Integration.Tests;

/// <summary>
/// Integration tests for the GetCurrentUserDetails query handler.
/// Tests the backend query that powers the user account page.
/// </summary>
[Trait("Category", "Integration")]
[Collection(nameof(IntegrationTestsCollection))]
public class GetCurrentUserDetailsQueryHandlerTests : IntegrationTestBase
{
    public GetCurrentUserDetailsQueryHandlerTests(IntegrationTestsWaf factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetCurrentUserDetails_WithValidUser_ReturnsUserDetails()
    {
        // Arrange
        const string userName = "test_user";
        const string fullName = "Test User Full Name";
        var userId = Guid.NewGuid();
        await CreateTestUserAsync(userId, userName, fullName, BuiltInRoles.RndUser);

        // Set the current user ID
        FakeCurrentUserService.CurrentUserId = userId;

        await using var scope = Factory.Services.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<MediatR.IRequestHandler<GetCurrentUserDetailsQuery, ApplicationUserDto?>>();

        // Act
        var query = new GetCurrentUserDetailsQuery();
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(userId);
        result.UserName.Should().Be(userName);
        result.FullName.Should().Be(fullName);
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetCurrentUserDetails_WithRoleAndPermissions_ReturnsRoleData()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await CreateTestUserAsync(userId, "role_test", "Role Test User", BuiltInRoles.RndUser);

        // Set the current user ID
        FakeCurrentUserService.CurrentUserId = userId;

        await using var scope = Factory.Services.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<MediatR.IRequestHandler<GetCurrentUserDetailsQuery, ApplicationUserDto?>>();

        // Act
        var query = new GetCurrentUserDetailsQuery();
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result!.Role.Should().NotBeNull();
        result.Role!.Name.Should().Be(BuiltInRoles.RndUser);
        result.Role.DisplayName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetCurrentUserDetails_WithPinEnabled_ReturnsPinEnabledFlag()
    {
        // Arrange
        const string pin = "1234";
        var userId = Guid.NewGuid();
        await CreateTestUserAsync(userId, "pin_test", "PIN Test User", BuiltInRoles.RndUser);

        // Set PIN
        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<IApplicationUserManager>();
            var user = await userManager.FindByIdAsync(userId.ToString());
            user.Should().NotBeNull();
            var setPinResult = await userManager.SetPinAsync(user!, pin);
            setPinResult.Succeeded.Should().BeTrue();
        }

        // Set the current user ID
        FakeCurrentUserService.CurrentUserId = userId;

        await using var scope2 = Factory.Services.CreateAsyncScope();
        var handler = scope2.ServiceProvider.GetRequiredService<MediatR.IRequestHandler<GetCurrentUserDetailsQuery, ApplicationUserDto?>>();

        // Act
        var query = new GetCurrentUserDetailsQuery();
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result!.IsPinEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task GetCurrentUserDetails_WithPasswordEmitted_ReturnsEmittedDateAndComputedExpiration()
    {
        // Arrange
        var emittedDate = DateTimeOffset.UtcNow.AddDays(-5);
        var userId = Guid.NewGuid();
        await CreateTestUserAsync(userId, "expiration_test", "Expiration Test User", BuiltInRoles.RndUser);

        // Set password emitted timestamp
        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<IApplicationUserManager>();
            var user = await userManager.FindByIdAsync(userId.ToString());
            user.Should().NotBeNull();
            user!.PasswordEmittedDateTimeOffset = emittedDate;
            var updateResult = await userManager.UpdateAsync(user);
            updateResult.Succeeded.Should().BeTrue();
        }

        // Set the current user ID
        FakeCurrentUserService.CurrentUserId = userId;

        await using var scope2 = Factory.Services.CreateAsyncScope();
        var handler = scope2.ServiceProvider.GetRequiredService<MediatR.IRequestHandler<GetCurrentUserDetailsQuery, ApplicationUserDto?>>();
        var securitySettings = scope2.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptionsSnapshot<Pharos.Server.Application.Settings.Security.SecuritySettings>>().Value;

        // Act
        var query = new GetCurrentUserDetailsQuery();
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();

        if (securitySettings.PasswordExpirationEnabled)
        {
            result!.PasswordExpirationDateTimeOffset.Should().NotBeNull();
            result.PasswordExpirationDateTimeOffset!.Value.Date
                .Should().Be(emittedDate.AddDays(securitySettings.PasswordExpirationDays).Date);
        }
        else
        {
            result!.PasswordExpirationDateTimeOffset.Should().BeNull();
        }
    }

    [Fact]
    public async Task GetCurrentUserDetails_WithInactiveUser_ReturnsIsActiveFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await CreateTestUserAsync(userId, "inactive_test", "Inactive Test User", BuiltInRoles.RndUser);

        // Make user inactive
        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<IApplicationUserManager>();
            var user = await userManager.FindByIdAsync(userId.ToString());
            user.Should().NotBeNull();
            user!.IsActive = false;
            var updateResult = await userManager.UpdateAsync(user);
            updateResult.Succeeded.Should().BeTrue();
        }

        // Set the current user ID
        FakeCurrentUserService.CurrentUserId = userId;

        await using var scope2 = Factory.Services.CreateAsyncScope();
        var handler = scope2.ServiceProvider.GetRequiredService<MediatR.IRequestHandler<GetCurrentUserDetailsQuery, ApplicationUserDto?>>();

        // Act
        var query = new GetCurrentUserDetailsQuery();
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task GetCurrentUserDetails_WithServiceEngineerRole_ReturnsServiceEngineerRole()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await CreateTestUserAsync(userId, "se_test", "Service Engineer Test", BuiltInRoles.ServiceEngineer);

        // Set the current user ID
        FakeCurrentUserService.CurrentUserId = userId;

        await using var scope = Factory.Services.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<MediatR.IRequestHandler<GetCurrentUserDetailsQuery, ApplicationUserDto?>>();

        // Act
        var query = new GetCurrentUserDetailsQuery();
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result!.Role!.Name.Should().Be(BuiltInRoles.ServiceEngineer);
        result.Role.IsBuiltInRole.Should().BeTrue();
    }

    [Fact]
    public async Task GetCurrentUserDetails_WithNonExistentUser_ReturnsNull()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();

        // Set the current user ID to a non-existent user
        FakeCurrentUserService.CurrentUserId = nonExistentUserId;

        await using var scope = Factory.Services.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<MediatR.IRequestHandler<GetCurrentUserDetailsQuery, ApplicationUserDto?>>();

        // Act
        var query = new GetCurrentUserDetailsQuery();
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeNull();
    }

    #region Helper Methods

    /// <summary>
    /// Creates a test user with a specific role.
    /// </summary>
    private async Task<ApplicationUser> CreateTestUserAsync(Guid userId, string userName, string fullName, string roleName)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<IApplicationRoleManager>();

        var user = new ApplicationUser
        {
            Id = userId,
            UserName = userName,
            FullName = fullName,
            IsActive = true,
            NormalizedUserName = userName.ToUpper(),
            SecurityStamp = Guid.NewGuid().ToString()
        };

        var createResult = await userManager.CreateAsync(user, "TempPassword123!");
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException($"Failed to create user: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
        }

        // Assign role
        var role = await roleManager.FindByNameAsync(roleName);
        if (role == null)
        {
            role = ApplicationRole.CreatePredefinedOrBuiltInRole(roleName);
            var createRoleResult = await roleManager.CreateAsync(role);
            if (!createRoleResult.Succeeded)
            {
                throw new InvalidOperationException($"Failed to create role '{roleName}': {string.Join(", ", createRoleResult.Errors.Select(e => e.Description))}");
            }
        }

        var addToRoleResult = await userManager.AddToRoleAsync(user, roleName);
        if (!addToRoleResult.Succeeded)
        {
            throw new InvalidOperationException($"Failed to assign role '{roleName}' to user '{userName}': {string.Join(", ", addToRoleResult.Errors.Select(e => e.Description))}");
        }

        return user;
    }

    #endregion
}
