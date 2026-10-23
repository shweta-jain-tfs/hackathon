using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Server.Identity.Domain;
using Pharos.Server.Identity.Infrastructure.Authentication;
using Pharos.Server.Integration.Tests.Common;

namespace Pharos.Server.Integration.Tests;

[Trait("Category", "Integration")]
[Collection(nameof(IntegrationTestsCollection))]
public sealed class PinLockoutIntegrationTests : IntegrationTestBase
{
    private const int _maxFailedAttempts = 3;

    public PinLockoutIntegrationTests(IntegrationTestsWaf factory) : base(factory)
    {
    }

    [Fact]
    public async Task PinLockout_BeforeThreshold_DoesNotLockOut()
    {
        // Arrange
        await using var scope = Factory.Services.CreateAsyncScope();
        ResetPinLockoutState(scope.ServiceProvider);

        var signInManager = (CustomSignInManager)scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
        var pinLockoutService = scope.ServiceProvider.GetRequiredService<IPinLockoutService>();

        // Act
        await signInManager.HandlePinLoginFailureAsync();
        await signInManager.HandlePinLoginFailureAsync();

        // Assert
        Assert.False(await pinLockoutService.IsPinLoginSuspended());
    }

    [Fact]
    public async Task PinLockout_WhenReachingThreshold_LocksOut()
    {
        // Arrange
        await using var scope = Factory.Services.CreateAsyncScope();
        ResetPinLockoutState(scope.ServiceProvider);

        var signInManager = (CustomSignInManager)scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
        var pinLockoutService = scope.ServiceProvider.GetRequiredService<IPinLockoutService>();

        // Act - Trigger lockout
        for (var i = 0; i < _maxFailedAttempts; i++)
        {
            await signInManager.HandlePinLoginFailureAsync();
        }

        // Assert
        Assert.True(await pinLockoutService.IsPinLoginSuspended());
    }

    #region Helper Methods

    private static void ResetPinLockoutState(IServiceProvider services)
    {
        var cache = services.GetRequiredService<IMemoryCache>();

        if (cache.TryGetValue<HashSet<string>>("PIN_LOCKOUT:TRACKED_KEYS", out var trackedKeys) && trackedKeys != null)
        {
            foreach (var trackedKey in trackedKeys)
            {
                cache.Remove(trackedKey);
            }
        }

        cache.Remove("PIN_LOCKOUT:default");
        cache.Remove("PIN_LOCKOUT:GLOBAL");
        cache.Remove("PIN_LOCKOUT:TRACKED_KEYS");
    }

    #endregion
}
