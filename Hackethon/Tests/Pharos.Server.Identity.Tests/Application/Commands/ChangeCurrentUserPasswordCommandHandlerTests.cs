using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Pharos.Server.Application;
using Pharos.Server.Identity.Domain;
using Pharos.Server.Identity.Infrastructure.Authorization;
using Pharos.Server.Infrastructure;
using Pharos.Server.Localization;

namespace Pharos.Server.Identity.Application.Commands;

public class ChangeCurrentUserPasswordCommandHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IApplicationUserManager> _userManager = new();
    private readonly Mock<IDateTime> _dateTime = new();

    [Fact]
    public async Task Handle_WhenCurrentPasswordIsInvalid_ThrowsValidationException()
    {
        var userId = Guid.NewGuid();
        var user = new ApplicationUser { Id = userId, UserName = "user1" };

        _currentUserService.SetupGet(x => x.UserId).Returns(userId);
        _userManager.Setup(x => x.FindByGuidAsync(userId)).ReturnsAsync(user);
        _userManager.Setup(x => x.CheckPasswordAsync(user, "old-password")).ReturnsAsync(false);

        var sut = new ChangeCurrentUserPasswordCommandHandler(_currentUserService.Object, _userManager.Object, _dateTime.Object);

        var act = () => sut.Handle(new ChangeCurrentUserPasswordCommand("old-password", "new-password"), TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<ValidationException>();
        exception.Which.Message.Should().Be(Translations.Login_InvalidUserNameOrPassword);
    }

    [Fact]
    public async Task Handle_WhenNewPasswordMatchesCurrent_ThrowsValidationException()
    {
        var userId = Guid.NewGuid();
        var user = new ApplicationUser { Id = userId, UserName = "user1" };

        _currentUserService.SetupGet(x => x.UserId).Returns(userId);
        _userManager.Setup(x => x.FindByGuidAsync(userId)).ReturnsAsync(user);
        _userManager.Setup(x => x.CheckPasswordAsync(user, "old-password")).ReturnsAsync(true);
        _userManager.Setup(x => x.CheckPasswordAsync(user, "new-password")).ReturnsAsync(true);

        var sut = new ChangeCurrentUserPasswordCommandHandler(_currentUserService.Object, _userManager.Object, _dateTime.Object);

        var act = () => sut.Handle(new ChangeCurrentUserPasswordCommand("old-password", "new-password"), TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<ValidationException>();
        exception.Which.Message.Should().Be(Translations.ChangePassword_PasswordsAreTheSame);
    }

    [Fact]
    public async Task Handle_WhenRequestIsValid_ResetsPasswordAndSetsEmittedTimestamp()
    {
        var userId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 7, 7, 12, 0, 0, TimeSpan.Zero);
        var user = new ApplicationUser { Id = userId, UserName = "user1", TemporaryPassword = true };

        _currentUserService.SetupGet(x => x.UserId).Returns(userId);
        _userManager.Setup(x => x.FindByGuidAsync(userId)).ReturnsAsync(user);
        _userManager.Setup(x => x.CheckPasswordAsync(user, "old-password")).ReturnsAsync(true);
        _userManager.Setup(x => x.CheckPasswordAsync(user, "new-password")).ReturnsAsync(false);
        _userManager.Setup(x => x.IsPasswordValid("new-password", user.UserName!)).Returns(true);
        _userManager.Setup(x => x.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("token");
        _userManager.Setup(x => x.ResetPasswordAsync(user, "token", "new-password")).ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _dateTime.Setup(x => x.Now).Returns(now);

        var sut = new ChangeCurrentUserPasswordCommandHandler(_currentUserService.Object, _userManager.Object, _dateTime.Object);

        await sut.Handle(new ChangeCurrentUserPasswordCommand("old-password", "new-password"), TestContext.Current.CancellationToken);

        user.TemporaryPassword.Should().BeFalse();
        // Emitted timestamp is set to now; expiration is computed dynamically from this + PasswordExpirationDays
        user.PasswordEmittedDateTimeOffset.Should().Be(now);
        _userManager.Verify(x => x.GeneratePasswordResetTokenAsync(user), Times.Once);
        _userManager.Verify(x => x.ResetPasswordAsync(user, "token", "new-password"), Times.Once);
        _userManager.Verify(x => x.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRequestIsValidWithPriorEmittedTimestamp_OverwritesEmittedTimestamp()
    {
        var userId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 7, 7, 12, 0, 0, TimeSpan.Zero);
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "admin1",
            TemporaryPassword = true,
            PasswordEmittedDateTimeOffset = now.AddMonths(-1)
        };

        _currentUserService.SetupGet(x => x.UserId).Returns(userId);
        _userManager.Setup(x => x.FindByGuidAsync(userId)).ReturnsAsync(user);
        _userManager.Setup(x => x.CheckPasswordAsync(user, "old-password")).ReturnsAsync(true);
        _userManager.Setup(x => x.CheckPasswordAsync(user, "new-password")).ReturnsAsync(false);
        _userManager.Setup(x => x.IsPasswordValid("new-password", user.UserName!)).Returns(true);
        _userManager.Setup(x => x.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("token");
        _userManager.Setup(x => x.ResetPasswordAsync(user, "token", "new-password")).ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _dateTime.Setup(x => x.Now).Returns(now);

        var sut = new ChangeCurrentUserPasswordCommandHandler(_currentUserService.Object, _userManager.Object, _dateTime.Object);

        await sut.Handle(new ChangeCurrentUserPasswordCommand("old-password", "new-password"), TestContext.Current.CancellationToken);

        user.TemporaryPassword.Should().BeFalse();
        user.PasswordEmittedDateTimeOffset.Should().Be(now);
        _userManager.Verify(x => x.UpdateAsync(user), Times.Once);
    }
    [Fact]
    public async Task Handle_AlwaysSetsEmittedTimestamp_RegardlessOfExpirationEnabled()
    {
        // Even when expiration is disabled in settings, the emitted timestamp is always recorded
        // so that enabling expiration later applies retroactively.
        var userId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 7, 7, 12, 0, 0, TimeSpan.Zero);
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "user1",
            TemporaryPassword = true,
        };

        _currentUserService.SetupGet(x => x.UserId).Returns(userId);
        _userManager.Setup(x => x.FindByGuidAsync(userId)).ReturnsAsync(user);
        _userManager.Setup(x => x.CheckPasswordAsync(user, "old-password")).ReturnsAsync(true);
        _userManager.Setup(x => x.CheckPasswordAsync(user, "new-password")).ReturnsAsync(false);
        _userManager.Setup(x => x.IsPasswordValid("new-password", user.UserName!)).Returns(true);
        _userManager.Setup(x => x.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("token");
        _userManager.Setup(x => x.ResetPasswordAsync(user, "token", "new-password")).ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _dateTime.Setup(x => x.Now).Returns(now);

        var sut = new ChangeCurrentUserPasswordCommandHandler(_currentUserService.Object, _userManager.Object, _dateTime.Object);

        await sut.Handle(new ChangeCurrentUserPasswordCommand("old-password", "new-password"), TestContext.Current.CancellationToken);

        user.PasswordEmittedDateTimeOffset.Should().Be(now);
    }
}
