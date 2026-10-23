using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Pharos.Contracts.Security;
using Pharos.Server.Application.Settings.Security;
using Pharos.Server.Identity.Domain;
using Pharos.Server.Identity.Infrastructure.Authentication;
using Pharos.Server.Identity.Infrastructure.Authorization;
using Pharos.Server.Integration.Tests.Common;

namespace Pharos.Server.Integration.Tests;

[Trait("Category", "Integration")]
[Collection(nameof(IntegrationTestsCollection))]
public sealed class LoginWithPinIntegrationTests : IntegrationTestBase
{
    public LoginWithPinIntegrationTests(IntegrationTestsWaf factory) : base(factory)
    {
    }

    [Fact]
    public async Task LoginWithPin_WithValidPin_AuthenticatesUserSuccessfully()
    {
        // Arrange
        var pin = "1234";
        var user = await CreateTestUserWithPinAsync("loginwithpin_testuser", pin);

        await using var scope = Factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<IApplicationUserManager>();

        // Act - Verify user can be found by PIN
        var foundUser = await userManager.FindByPinAsync(pin);

        // Assert
        Assert.NotNull(foundUser);
        Assert.Equal(user.Id, foundUser.Id);
        Assert.True(foundUser.IsPinEnabled);
    }

    [Fact]
    public async Task FindByPinAsync_WithValidPin_ReturnsCorrectUser()
    {
        // Arrange
        var pin = "5678";
        var expectedUser = await CreateTestUserWithPinAsync("pinuser", pin);

        await using var scope = Factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<IApplicationUserManager>();

        // Act
        var foundUser = await userManager.FindByPinAsync(pin);

        // Assert
        Assert.NotNull(foundUser);
        Assert.Equal(expectedUser.Id, foundUser.Id);
        Assert.Equal(expectedUser.UserName, foundUser.UserName);
        Assert.True(foundUser.IsPinEnabled);
    }

    [Fact]
    public async Task FindByPinAsync_WithInvalidPin_ReturnsNull()
    {
        // Arrange
        var correctPin = "1112";
        var wrongPin = "9998";
        await CreateTestUserWithPinAsync("invalidpin_user1", correctPin);

        await using var scope = Factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<IApplicationUserManager>();

        // Act
        var foundUser = await userManager.FindByPinAsync(wrongPin);

        // Assert
        Assert.Null(foundUser);
    }

    [Fact]
    public async Task SetPinAsync_WithValidPin_StoresPinHashCorrectly()
    {
        // Arrange
        var pin = "4567";
        var user = await CreateTestUserAsync("newuser");

        await using var scope = Factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<IApplicationUserManager>();

        // Reload user in this scope to avoid EF tracking conflicts
        var trackedUser = await userManager.FindByGuidAsync(user.Id);
        Assert.NotNull(trackedUser);

        // Act
        var result = await userManager.SetPinAsync(trackedUser, pin);

        // Assert
        Assert.True(result.Succeeded);

        // Verify user can be found by the PIN
        var foundUser = await userManager.FindByPinAsync(pin);
        Assert.NotNull(foundUser);
        Assert.Equal(user.Id, foundUser.Id);
        Assert.True(foundUser.IsPinEnabled);
        Assert.NotNull(foundUser.PinHash);
    }

    [Theory]
    [InlineData("123")]      // Too short (3 digits)
    [InlineData("12345")]    // Too long (5 digits)
    [InlineData("12a4")]     // Non-digit
    public async Task SetPinAsync_WithInvalidPinLength_ReturnsFailure(string invalidPin)
    {
        // Arrange
        var user = await CreateTestUserAsync($"invalidpin_testuser_{invalidPin}");

        await using var scope = Factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<IApplicationUserManager>();

        // Reload user in this scope to avoid EF tracking conflicts
        var trackedUser = await userManager.FindByGuidAsync(user.Id);
        Assert.NotNull(trackedUser);

        // Act
        var result = await userManager.SetPinAsync(trackedUser, invalidPin);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code == "InvalidPin");
    }

    [Fact]
    public async Task SetPinAsync_WithDuplicatePin_ReturnsFailure()
    {
        // Arrange
        var duplicatePin = "7777";
        var user1 = await CreateTestUserWithPinAsync("duplicatepin_user1", duplicatePin);
        var user2 = await CreateTestUserAsync("duplicatepin_user2");

        await using var scope = Factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<IApplicationUserManager>();

        // Reload user2 in this scope to avoid EF tracking conflicts
        var trackedUser2 = await userManager.FindByGuidAsync(user2.Id);
        Assert.NotNull(trackedUser2);

        // Act
        var result = await userManager.SetPinAsync(trackedUser2, duplicatePin);

        // Assert
        Assert.False(result.Succeeded);
        // Note: Returns InvalidPin instead of DuplicatePin to prevent PIN enumeration attacks
        Assert.Contains(result.Errors, e => e.Code == "InvalidPin");
    }

    [Fact]
    public async Task SetPinAsync_ForServiceEngineerRole_ReturnsFailure()
    {
        // Arrange
        var pin = "3333";
        var serviceEngineer = await CreateTestUserAsync("serviceengineer");

        await using var scope = Factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<IApplicationUserManager>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        // Create the Service Engineer role if it doesn't exist
        if (!await roleManager.RoleExistsAsync(BuiltInRoles.ServiceEngineer))
        {
            await roleManager.CreateAsync(new ApplicationRole
            {
                Id = Guid.NewGuid(),
                Name = BuiltInRoles.ServiceEngineer,
                NormalizedName = BuiltInRoles.ServiceEngineer.ToUpperInvariant()
            });
        }

        // Reload user in this scope to avoid EF tracking conflicts
        var trackedUser = await userManager.FindByGuidAsync(serviceEngineer.Id);
        Assert.NotNull(trackedUser);

        // Add user to Service Engineer role
        await userManager.AddToRoleAsync(trackedUser, BuiltInRoles.ServiceEngineer);

        // Act
        var result = await userManager.SetPinAsync(trackedUser, pin);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code == "ServiceEngineerPinNotAllowed");
    }

    [Fact]
    public async Task SetPinAsync_UpdateExistingPin_ChangesPin()
    {
        // Arrange
        var oldPin = "1001";
        var newPin = "2001";
        var user = await CreateTestUserWithPinAsync("updateuser", oldPin);

        await using var scope = Factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<IApplicationUserManager>();

        // Reload user in this scope to avoid EF tracking conflicts
        var trackedUser = await userManager.FindByGuidAsync(user.Id);
        Assert.NotNull(trackedUser);

        // Act
        var result = await userManager.SetPinAsync(trackedUser, newPin);

        // Assert
        Assert.True(result.Succeeded);

        // Verify old PIN doesn't work
        var foundWithOldPin = await userManager.FindByPinAsync(oldPin);
        Assert.Null(foundWithOldPin);

        // Verify new PIN works
        var foundWithNewPin = await userManager.FindByPinAsync(newPin);
        Assert.NotNull(foundWithNewPin);
        Assert.Equal(user.Id, foundWithNewPin.Id);
    }

    [Fact]
    public async Task FindByPinAsync_WithMultipleUsers_ReturnsOnlyMatchingUser()
    {
        // Arrange
        var user1Pin = "1111";
        var user2Pin = "2222";
        var user3Pin = "3333";

        await CreateTestUserWithPinAsync("multipleusers_user1", user1Pin);
        var user2 = await CreateTestUserWithPinAsync("multipleusers_user2", user2Pin);
        await CreateTestUserWithPinAsync("multipleusers_user3", user3Pin);

        await using var scope = Factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<IApplicationUserManager>();

        // Act
        var foundUser = await userManager.FindByPinAsync(user2Pin);

        // Assert
        Assert.NotNull(foundUser);
        Assert.Equal(user2.Id, foundUser.Id);
        Assert.Equal(user2.UserName, foundUser.UserName);
    }

    [Fact]
    public async Task FindByPinAsync_WithDisabledUser_ReturnsUserButNotAllowed()
    {
        // Arrange
        var pin = "6666";
        var user = await CreateTestUserWithPinAsync("disableduser", pin);

        // Disable the user
        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<IApplicationUserManager>();

            // Reload user in this scope to avoid EF tracking conflicts
            var trackedUser = await userManager.FindByGuidAsync(user.Id);
            Assert.NotNull(trackedUser);

            trackedUser.IsActive = false;
            await userManager.UpdateAsync(trackedUser);
        }

        await using var scope2 = Factory.Services.CreateAsyncScope();
        var userManager2 = scope2.ServiceProvider.GetRequiredService<IApplicationUserManager>();

        // Act
        var foundUser = await userManager2.FindByPinAsync(pin);

        // Assert
        Assert.NotNull(foundUser);
        Assert.False(foundUser.IsActive);
    }

    [Theory]
    [InlineData("0000")]
    [InlineData("9999")]
    public async Task SetPinAsync_WithBoundaryPinLength_Succeeds(string validPin)
    {
        // Arrange
        var user = await CreateTestUserAsync($"boundary_testuser_{validPin}");

        await using var scope = Factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<IApplicationUserManager>();

        // Reload user in this scope to avoid EF tracking conflicts
        var trackedUser = await userManager.FindByGuidAsync(user.Id);
        Assert.NotNull(trackedUser);

        // Act
        var result = await userManager.SetPinAsync(trackedUser, validPin);

        // Assert
        Assert.True(result.Succeeded);
        Assert.True(trackedUser.IsPinEnabled);
        Assert.NotNull(trackedUser.PinHash);

        // Verify user can be found by the PIN
        var foundUser = await userManager.FindByPinAsync(validPin);
        Assert.NotNull(foundUser);
        Assert.Equal(user.Id, foundUser.Id);
    }

    [Fact]
    public async Task SetPinAsync_WithLeadingZeros_HandlesCorrectly()
    {
        // Arrange
        var user = await CreateTestUserAsync("leadingzeros_testuser");
        var pinWithLeadingZero = "0123";

        await using var scope = Factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<IApplicationUserManager>();

        // Reload user in this scope to avoid EF tracking conflicts
        var trackedUser = await userManager.FindByGuidAsync(user.Id);
        Assert.NotNull(trackedUser);

        // Act
        var result = await userManager.SetPinAsync(trackedUser, pinWithLeadingZero);

        // Assert
        Assert.True(result.Succeeded);
        var foundUser = await userManager.FindByPinAsync(pinWithLeadingZero);
        Assert.NotNull(foundUser);
        Assert.Equal(user.Id, foundUser.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task FindByPinAsync_WithEmptyOrWhitespacePin_ReturnsNull(string invalidPin)
    {
        // Arrange
        await using var scope = Factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<IApplicationUserManager>();

        // Act
        var result = await userManager.FindByPinAsync(invalidPin);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task EnablePinLogin_FeatureFlag_AffectsAvailability()
    {
        // Arrange
        await using var scope = Factory.Services.CreateAsyncScope();
        var identitySettings = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<SecuritySettings>>();

        // Act
        var pinLoginEnabled = identitySettings.Value.EnablePinLogin;

        // Assert
        Assert.False(pinLoginEnabled);
    }

    [Fact]
    public async Task PinSignInWithUserAsync_WhenPinLoginDisabled_ReturnsNotAllowed()
    {
        // Arrange
        var pin = "1357";
        await CreateTestUserWithPinAsync("disabled_flow_user", pin);

        await using var scope = Factory.Services.CreateAsyncScope();
        var signInManager = (CustomSignInManager)scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();

        // Act
        var (result, user) = await signInManager.PinSignInWithUserAsync(pin);

        // Assert
        Assert.True(result.IsNotAllowed);
        Assert.Null(user);
    }

    [Fact]
    public async Task CheckPasswordSignInAsync_WhenPasswordLockoutDisabled_DoesNotLockOutUserAfterRepeatedFailures()
    {
        // Arrange
        var createdUser = await CreateTestUserAsync("password_lockout_disabled_user");

        await using var scope = Factory.Services.CreateAsyncScope();
        var securitySettings = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<SecuritySettings>>().Value;
        securitySettings.EnablePasswordLoginLockout = false;
        securitySettings.PasswordLoginLockoutMaxFailedAttempts = 1;
        securitySettings.PasswordLoginLockoutDurationMinutes = 15;

        var signInManager = (CustomSignInManager)scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByIdAsync(createdUser.Id.ToString());
        Assert.NotNull(user);

        // Act
        await signInManager.CheckPasswordSignInAsync(user, "WrongPass1!", lockoutOnFailure: true);
        await signInManager.CheckPasswordSignInAsync(user, "WrongPass1!", lockoutOnFailure: true);

        // Assert
        Assert.False(await userManager.IsLockedOutAsync(user));
    }

    [Fact]
    public async Task PinSignInWithUserAsync_WhenPasswordLockoutTriggered_ReturnsLockedOut()
    {
        // Arrange
        const string pin = "2468";
        var createdUser = await CreateTestUserWithPinAsync("pin_blocked_by_password_lockout", pin);

        await using var scope = Factory.Services.CreateAsyncScope();
        var securitySettings = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<SecuritySettings>>().Value;
        securitySettings.EnablePinLogin = true;
        securitySettings.EnablePasswordLoginLockout = true;
        securitySettings.PasswordLoginLockoutMaxFailedAttempts = 1;
        securitySettings.PasswordLoginLockoutDurationMinutes = 15;

        var signInManager = (CustomSignInManager)scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByIdAsync(createdUser.Id.ToString());
        Assert.NotNull(user);

        // Trigger password lockout
        await signInManager.CheckPasswordSignInAsync(user, "WrongPass1!", lockoutOnFailure: true);
        Assert.True(await userManager.IsLockedOutAsync(user));

        // Act
        var (result, returnedUser) = await signInManager.PinSignInWithUserAsync(pin);

        // Assert
        Assert.True(result.IsLockedOut);
        Assert.Null(returnedUser);
    }

    #region Helper Methods

    private async Task<ApplicationUser> CreateTestUserAsync(string userName)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            IsActive = true,
            SecurityStamp = Guid.NewGuid().ToString()
        };

        await using var scope = Factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var result = await userManager.CreateAsync(user, "Test@1234");
        Assert.True(result.Succeeded, $"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");

        return user;
    }

    private async Task<ApplicationUser> CreateTestUserWithPinAsync(string userName, string pin)
    {
        var user = await CreateTestUserAsync(userName);

        await using var scope = Factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<IApplicationUserManager>();

        // Reload user in this scope to avoid EF tracking conflicts
        var trackedUser = await userManager.FindByGuidAsync(user.Id);
        Assert.NotNull(trackedUser);

        var result = await userManager.SetPinAsync(trackedUser, pin);
        Assert.True(result.Succeeded, $"Failed to set PIN: {string.Join(", ", result.Errors.Select(e => e.Description))}");

        return trackedUser;
    }

    #endregion

    [Fact]
    public async Task FindByPinAsync_WithPinDisabledOnUser_ReturnsNull()
    {
        // Arrange
        var pin = "4321";
        var user = await CreateTestUserWithPinAsync("pindisabled_user", pin);

        // Disable PIN for the user
        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<IApplicationUserManager>();
            var trackedUser = await userManager.FindByGuidAsync(user.Id);
            Assert.NotNull(trackedUser);
            trackedUser.IsPinEnabled = false;
            await userManager.UpdateAsync(trackedUser);
        }

        await using var scope2 = Factory.Services.CreateAsyncScope();
        var userManager2 = scope2.ServiceProvider.GetRequiredService<IApplicationUserManager>();

        // Act
        var foundUser = await userManager2.FindByPinAsync(pin);

        // Assert
        Assert.Null(foundUser);
    }

    [Fact]
    public async Task FindByPinAsync_UserWithPinEnabled_AllowsAccess_AndWithPinDisabled_DeniesAccess()
    {
        // Arrange
        var pin = "2468";
        var user = await CreateTestUserWithPinAsync("pin_toggle_user", pin);

        // Step 1: PIN enabled, should allow access
        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<IApplicationUserManager>();
            var foundUser = await userManager.FindByPinAsync(pin);
            Assert.NotNull(foundUser);
            Assert.True(foundUser.IsPinEnabled);
        }

        // Step 2: Disable PIN for the user
        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<IApplicationUserManager>();
            var trackedUser = await userManager.FindByGuidAsync(user.Id);
            Assert.NotNull(trackedUser);
            trackedUser.IsPinEnabled = false;
            await userManager.UpdateAsync(trackedUser);
        }

        // Step 3: PIN disabled, should deny access
        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<IApplicationUserManager>();
            var foundUser = await userManager.FindByPinAsync(pin);
            Assert.Null(foundUser);
        }
    }
}
