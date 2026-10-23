using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Pharos.Server.Application;
using Pharos.Server.Application.Settings.Security;
using Pharos.Server.Infrastructure;

namespace Pharos.Server.Identity.Infrastructure.Authentication;

public sealed class PinLockoutServiceTests
{
    [Fact]
    public async Task RecordFailedAttempt_WhenOneInstrumentReachesThreshold_LocksOutAllInstruments()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var dateTime = new Mock<IDateTime>();
        dateTime.SetupGet(x => x.Now).Returns(() => now);

        var settings = new Mock<IOptionsSnapshot<SecuritySettings>>();
        settings.SetupGet(x => x.Value).Returns(new SecuritySettings
        {
            EnablePinLogin = true,
            EnablePinLoginLockout = true,
            PinLoginLockoutMaxFailedAttempts = 3,
            PinLoginLockoutDurationMinutes = 15
        });

        var instrumentService = new Mock<ICurrentInstrumentService>();
        instrumentService.SetupGet(x => x.InstrumentId).Returns(1);

        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new PinLockoutService(cache, settings.Object, dateTime.Object, instrumentService.Object);

        // Act
        await sut.RecordFailedAttempt();
        await sut.RecordFailedAttempt();
        var triggered = await sut.RecordFailedAttempt();

        var lockedInstrument1 = await sut.IsPinLoginSuspended();

        // Also test with different instrument ID
        instrumentService.SetupGet(x => x.InstrumentId).Returns(2);
        var lockedInstrument2 = await sut.IsPinLoginSuspended();

        // Assert
        triggered.Should().BeTrue();
        lockedInstrument1.Should().BeTrue();
        lockedInstrument2.Should().BeTrue();
    }

    [Fact]
    public async Task IsPinLoginSuspended_WhenGlobalLockoutExpires_ResetsAllInstrumentsToUnlockedState()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var dateTime = new Mock<IDateTime>();
        dateTime.SetupGet(x => x.Now).Returns(() => now);

        var settings = new Mock<IOptionsSnapshot<SecuritySettings>>();
        settings.SetupGet(x => x.Value).Returns(new SecuritySettings
        {
            EnablePinLogin = true,
            EnablePinLoginLockout = true,
            PinLoginLockoutMaxFailedAttempts = 3,
            PinLoginLockoutDurationMinutes = 15
        });

        var instrumentService = new Mock<ICurrentInstrumentService>();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new PinLockoutService(cache, settings.Object, dateTime.Object, instrumentService.Object);

        // Track attempts on multiple instruments and trigger global lockout from one instrument
        instrumentService.SetupGet(x => x.InstrumentId).Returns(2);
        await sut.RecordFailedAttempt();

        instrumentService.SetupGet(x => x.InstrumentId).Returns(3);
        await sut.RecordFailedAttempt();
        await sut.RecordFailedAttempt();
        await sut.RecordFailedAttempt();

        // Move time beyond lockout expiration
        now = now.AddMinutes(16);

        // Act - checking lockout should release global lockout and clear all tracked attempts
        instrumentService.SetupGet(x => x.InstrumentId).Returns(2);
        var instrument2Locked = await sut.IsPinLoginSuspended();

        instrumentService.SetupGet(x => x.InstrumentId).Returns(3);
        var instrument3Locked = await sut.IsPinLoginSuspended();

        // Assert
        instrument2Locked.Should().BeFalse();
        instrument3Locked.Should().BeFalse();
    }

    [Fact]
    public async Task IsPinLoginSuspended_WhenInstrumentIdIsNull_ReturnsFalse()
    {
        // Arrange
        var dateTime = new Mock<IDateTime>();
        var settings = new Mock<IOptionsSnapshot<SecuritySettings>>();
        settings.SetupGet(x => x.Value).Returns(new SecuritySettings
        {
            EnablePinLogin = true,
            EnablePinLoginLockout = true,
            PinLoginLockoutMaxFailedAttempts = 3,
            PinLoginLockoutDurationMinutes = 15
        });

        var instrumentService = new Mock<ICurrentInstrumentService>();
        instrumentService.SetupGet(x => x.InstrumentId).Returns((int?)null);

        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new PinLockoutService(cache, settings.Object, dateTime.Object, instrumentService.Object);

        // Act
        var result = await sut.IsPinLoginSuspended();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RecordFailedAttempt_WhenGlobalLockoutIsActive_DoesNotTriggerAdditionalLockout()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var dateTime = new Mock<IDateTime>();
        dateTime.SetupGet(x => x.Now).Returns(() => now);

        var settings = new Mock<IOptionsSnapshot<SecuritySettings>>();
        settings.SetupGet(x => x.Value).Returns(new SecuritySettings
        {
            EnablePinLogin = true,
            EnablePinLoginLockout = true,
            PinLoginLockoutMaxFailedAttempts = 3,
            PinLoginLockoutDurationMinutes = 15
        });

        var instrumentService = new Mock<ICurrentInstrumentService>();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new PinLockoutService(cache, settings.Object, dateTime.Object, instrumentService.Object);

        // Trigger global lockout via instrument 1
        instrumentService.SetupGet(x => x.InstrumentId).Returns(1);
        await sut.RecordFailedAttempt();
        await sut.RecordFailedAttempt();
        await sut.RecordFailedAttempt();

        // Act
        instrumentService.SetupGet(x => x.InstrumentId).Returns(2);
        var lockoutTriggeredAgain = await sut.RecordFailedAttempt();

        // Assert
        lockoutTriggeredAgain.Should().BeFalse();
        (await sut.IsPinLoginSuspended()).Should().BeTrue();
    }


}
