using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Settings.Security;
using Pharos.Server.Identity.Domain;
using Pharos.Server.Infrastructure;

namespace Pharos.Server.Identity.Infrastructure.Authorization;

public sealed class ApplicationUserManagerTests : IDisposable
{
    private readonly Mock<IOptionsSnapshot<SecuritySettings>> _mockSecuritySettings;
    private readonly Mock<IDateTime> _mockDateTime;
    private readonly MemoryCache _memoryCache;
    private readonly List<IdentityDbContext<ApplicationUser, ApplicationRole, Guid>> _dbContexts;

    public ApplicationUserManagerTests()
    {
        _dbContexts = new List<IdentityDbContext<ApplicationUser, ApplicationRole, Guid>>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());

        // Setup SecuritySettings mock
        _mockSecuritySettings = new Mock<IOptionsSnapshot<SecuritySettings>>();
        _mockSecuritySettings.Setup(x => x.Value).Returns(new SecuritySettings
        {
            PasswordComplexitySettings = new PasswordComplexitySettings
            {
                RequireDigit = true,
                RequireBothUpperAndLowerCase = true,
                RequireNonAlphanumeric = true,
                RequiredLength = 8,
                RequireNoUsernameInPassword = true
            }
        });

        _mockDateTime = new Mock<IDateTime>();
        _mockDateTime.Setup(x => x.Now).Returns(DateTimeOffset.UtcNow);
    }

    private (UserManager<ApplicationUser>, IdentityDbContext<ApplicationUser, ApplicationRole, Guid>) CreateUserManagerWithContext(List<ApplicationUser>? users = null)
    {
        users ??= [];

        var options = new DbContextOptionsBuilder<IdentityDbContext<ApplicationUser, ApplicationRole, Guid>>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var dbContext = new IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options);
        _dbContexts.Add(dbContext);

        if (users.Count > 0)
        {
            dbContext.Users.AddRange(users);
            dbContext.SaveChanges();
        }

        var passwordOptions = new PasswordOptions
        {
            RequireDigit = true,
            RequireLowercase = true,
            RequireUppercase = true,
            RequireNonAlphanumeric = true,
            RequiredLength = 8
        };

        var identityOptions = new IdentityOptions
        {
            Password = passwordOptions
        };

        // UserStore will be disposed when dbContext is disposed
#pragma warning disable CA2000 // Dispose objects before losing scope - UserStore lifetime is managed by dbContext
        var userStore = new UserStore<ApplicationUser, ApplicationRole, IdentityDbContext<ApplicationUser, ApplicationRole, Guid>, Guid>(dbContext);
#pragma warning restore CA2000 // Dispose objects before losing scope

        // Create a mock logger for UserManager
        var mockLogger = new Mock<Microsoft.Extensions.Logging.ILogger<UserManager<ApplicationUser>>>();

        var userManager = new UserManager<ApplicationUser>(
            userStore,
            Options.Create(identityOptions),
            new PasswordHasher<ApplicationUser>(),
            null!,
            null!,
            null!,
            null!,
            null!,
            mockLogger.Object);

        return (userManager, dbContext);
    }

    [Fact]
    public async Task SetPinAsync_WithValidPin_SetsHashedPin()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "testuser",
            IsPinEnabled = false,
            PinHash = null,
            SecurityStamp = Guid.NewGuid().ToString()
        };

        var pin = "1234";

        var (userManager, dbContext) = CreateUserManagerWithContext();
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var applicationUserManager = new ApplicationUserManager(
            userManager,
            _mockSecuritySettings.Object,
            _mockDateTime.Object,
            _memoryCache);

        // Act
        var result = await applicationUserManager.SetPinAsync(user, pin);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(user.PinHash);
        Assert.True(user.IsPinEnabled);
    }

    [Theory]
    [InlineData("123")]    // Too short
    [InlineData("12345")]  // Too long
    [InlineData("12a4")]   // Non-digit
    public async Task SetPinAsync_WithInvalidPin_ReturnsFailure(string invalidPin)
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "testuser"
        };

        var (userManager, _) = CreateUserManagerWithContext();
        var applicationUserManager = new ApplicationUserManager(
            userManager,
            _mockSecuritySettings.Object,
            _mockDateTime.Object,
            _memoryCache);

        // Act
        var result = await applicationUserManager.SetPinAsync(user, invalidPin);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code == "InvalidPin");
    }

    [Fact]
    public async Task SetPinAsync_WithDuplicatePin_ReturnsFailure()
    {
        // Arrange
        var existingUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "existinguser",
            IsPinEnabled = true
        };

        var newUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "newuser",
            IsPinEnabled = false
        };

        var pin = "1234";

        // Hash the PIN for the existing user
        var pinHasher = new PasswordHasher<ApplicationUser>();
        existingUser.PinHash = pinHasher.HashPassword(existingUser, "1234");

        var (userManager, dbContext) = CreateUserManagerWithContext();
        dbContext.Users.Add(existingUser);
        dbContext.Users.Add(newUser);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var applicationUserManager = new ApplicationUserManager(
            userManager,
            _mockSecuritySettings.Object,
            _mockDateTime.Object,
            _memoryCache);

        // Act
        var result = await applicationUserManager.SetPinAsync(newUser, pin);

        // Assert
        Assert.False(result.Succeeded);
        // DuplicatePin error is masked as InvalidPin to prevent PIN enumeration attacks
        Assert.Contains(result.Errors, e => e.Code == "InvalidPin");
    }

    [Fact]
    public async Task FindByPinAsync_WithValidPin_ReturnsUser()
    {
        // Arrange
        var pin = "1234";
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "testuser",
            IsPinEnabled = true
        };

        // Hash the PIN
        var pinHasher = new PasswordHasher<ApplicationUser>();
        user.PinHash = pinHasher.HashPassword(user, pin);

        var (userManager, dbContext) = CreateUserManagerWithContext();
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var applicationUserManager = new ApplicationUserManager(
            userManager,
            _mockSecuritySettings.Object,
            _mockDateTime.Object,
            _memoryCache);

        // Act
        var result = await applicationUserManager.FindByPinAsync(pin);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
    }

    [Fact]
    public async Task FindByPinAsync_WithInvalidPin_ReturnsNull()
    {
        // Arrange
        var correctPin = "1234";
        var wrongPin = "5678";
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "testuser",
            IsPinEnabled = true
        };

        // Hash the correct PIN
        var pinHasher = new PasswordHasher<ApplicationUser>();
        user.PinHash = pinHasher.HashPassword(user, correctPin);

        var (userManager, dbContext) = CreateUserManagerWithContext();
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var applicationUserManager = new ApplicationUserManager(
            userManager,
            _mockSecuritySettings.Object,
            _mockDateTime.Object,
            _memoryCache);

        // Act
        var result = await applicationUserManager.FindByPinAsync(wrongPin);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task FindByPinAsync_WithEmptyPin_ReturnsNull()
    {
        // Arrange
        var (userManager, _) = CreateUserManagerWithContext();
        var applicationUserManager = new ApplicationUserManager(
            userManager,
            _mockSecuritySettings.Object,
            _mockDateTime.Object,
            _memoryCache);

        // Act
        var result = await applicationUserManager.FindByPinAsync("");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task FindByPinAsync_WithNullPin_ReturnsNull()
    {
        // Arrange
        var (userManager, _) = CreateUserManagerWithContext();
        var applicationUserManager = new ApplicationUserManager(
            userManager,
            _mockSecuritySettings.Object,
            _mockDateTime.Object,
            _memoryCache);

        // Act
        var result = await applicationUserManager.FindByPinAsync(null!);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SetPinAsync_WhenUserHasExistingPin_UpdatesPin()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "testuser",
            IsPinEnabled = true,
            PinHash = "oldhash",
            SecurityStamp = Guid.NewGuid().ToString()
        };

        var newPin = "5678";
        var oldHash = user.PinHash;

        var (userManager, dbContext) = CreateUserManagerWithContext();
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var applicationUserManager = new ApplicationUserManager(
            userManager,
            _mockSecuritySettings.Object,
            _mockDateTime.Object,
            _memoryCache);

        // Act
        var result = await applicationUserManager.SetPinAsync(user, newPin);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(user.PinHash);
        Assert.NotEqual(oldHash, user.PinHash);
        Assert.True(user.IsPinEnabled);
    }

    [Fact]
    public async Task SetPinAsync_ForServiceEngineerUser_ReturnsFailure()
    {
        // Arrange
        var serviceEngineerRole = new ApplicationRole
        {
            Id = Guid.NewGuid(),
            Name = Contracts.Security.BuiltInRoles.ServiceEngineer,
            NormalizedName = Contracts.Security.BuiltInRoles.ServiceEngineer.ToUpperInvariant()
        };

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "serviceengineer",
            IsPinEnabled = false,
            PinHash = null,
            SecurityStamp = Guid.NewGuid().ToString(),
            NormalizedUserName = "SERVICEENGINEER"
        };

        var pin = "1234";

        var (userManager, dbContext) = CreateUserManagerWithContext();

        // Create RoleManager to properly add roles
        using var roleStore = new RoleStore<ApplicationRole, IdentityDbContext<ApplicationUser, ApplicationRole, Guid>, Guid>(dbContext);
        var mockRoleLogger = new Mock<Microsoft.Extensions.Logging.ILogger<RoleManager<ApplicationRole>>>();
        using var roleManager = new RoleManager<ApplicationRole>(
            roleStore,
            null!,
            null!,
            null!,
            mockRoleLogger.Object);

        // Add role using RoleManager
        await roleManager.CreateAsync(serviceEngineerRole);

        // Add user after role is created
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assign user to Service Engineer role
        await userManager.AddToRoleAsync(user, Contracts.Security.BuiltInRoles.ServiceEngineer);

        var applicationUserManager = new ApplicationUserManager(
            userManager,
            _mockSecuritySettings.Object,
            _mockDateTime.Object,
            _memoryCache);

        // Act
        var result = await applicationUserManager.SetPinAsync(user, pin);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code == "ServiceEngineerPinNotAllowed");
        Assert.Null(user.PinHash);
        Assert.False(user.IsPinEnabled);
    }

    [Fact]
    public async Task SetPinAsync_ForNonServiceEngineerUser_Succeeds()
    {
        // Arrange
        var operatorRole = new ApplicationRole
        {
            Id = Guid.NewGuid(),
            Name = Contracts.Security.PredefinedRoles.Operator,
            NormalizedName = Contracts.Security.PredefinedRoles.Operator.ToUpperInvariant()
        };

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "operator",
            IsPinEnabled = false,
            PinHash = null,
            SecurityStamp = Guid.NewGuid().ToString(),
            NormalizedUserName = "OPERATOR"
        };

        var pin = "1234";

        var (userManager, dbContext) = CreateUserManagerWithContext();

        // Create RoleManager to properly add roles
        using var roleStore = new RoleStore<ApplicationRole, IdentityDbContext<ApplicationUser, ApplicationRole, Guid>, Guid>(dbContext);
        var mockRoleLogger = new Mock<Microsoft.Extensions.Logging.ILogger<RoleManager<ApplicationRole>>>();
        using var roleManager = new RoleManager<ApplicationRole>(
            roleStore,
            null!,
            null!,
            null!,
            mockRoleLogger.Object);

        // Add role using RoleManager
        await roleManager.CreateAsync(operatorRole);

        // Add user after role is created
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assign user to Operator role
        await userManager.AddToRoleAsync(user, Contracts.Security.PredefinedRoles.Operator);

        var applicationUserManager = new ApplicationUserManager(
            userManager,
            _mockSecuritySettings.Object,
            _mockDateTime.Object,
            _memoryCache);

        // Act
        var result = await applicationUserManager.SetPinAsync(user, pin);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(user.PinHash);
        Assert.True(user.IsPinEnabled);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task IsActive_WhenActiveUserIsCreated_ReturnsCorrectValue(bool expectedIsActive)
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "active-create-user",
            NormalizedUserName = "ACTIVE-CREATE-USER",
            IsActive = expectedIsActive
        };

        var (userManager, _) = CreateUserManagerWithContext();
        var applicationUserManager = new ApplicationUserManager(
            userManager,
            _mockSecuritySettings.Object,
            _mockDateTime.Object,
            _memoryCache);

        // Act
        var createResult = await applicationUserManager.CreateAsync(user, "TempPass1!");
        var isActive = await applicationUserManager.IsActive(user.Id);

        // Assert
        Assert.True(createResult.Succeeded);
        Assert.Equal(expectedIsActive, isActive);
    }

    [Fact]
    public async Task IsActive_WhenUserIsDeleted_ReturnsFalse()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "active-delete-user",
            NormalizedUserName = "ACTIVE-DELETE-USER",
            IsActive = true
        };

        var (userManager, _) = CreateUserManagerWithContext();
        var applicationUserManager = new ApplicationUserManager(
            userManager,
            _mockSecuritySettings.Object,
            _mockDateTime.Object,
            _memoryCache);

        var createResult = await applicationUserManager.CreateAsync(user, "TempPass1!");

        // Act
        var deleteResult = await applicationUserManager.DeleteUserAsync(user.Id);
        var isActive = await applicationUserManager.IsActive(user.Id);

        // Assert
        Assert.True(createResult.Succeeded);
        Assert.True(deleteResult.Succeeded);
        Assert.False(isActive);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task IsActive_WhenUserIsUpdated_ReturnsCorrectValue(bool initialIsActive)
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "active-deactivate-user",
            NormalizedUserName = "ACTIVE-DEACTIVATE-USER",
            IsActive = initialIsActive
        };

        var (userManager, _) = CreateUserManagerWithContext();
        var applicationUserManager = new ApplicationUserManager(
            userManager,
            _mockSecuritySettings.Object,
            _mockDateTime.Object,
            _memoryCache);

        var createResult = await applicationUserManager.CreateAsync(user, "TempPass1!");
        user.IsActive = !initialIsActive;

        // Act
        var updateResult = await applicationUserManager.UpdateAsync(user);
        var isActive = await applicationUserManager.IsActive(user.Id);

        // Assert
        Assert.True(createResult.Succeeded);
        Assert.True(updateResult.Succeeded);
        Assert.Equal(initialIsActive, !isActive);
    }

    public void Dispose()
    {
        foreach (var context in _dbContexts)
        {
            context.Dispose();
        }
        _dbContexts.Clear();
        _memoryCache.Dispose();
    }
}
