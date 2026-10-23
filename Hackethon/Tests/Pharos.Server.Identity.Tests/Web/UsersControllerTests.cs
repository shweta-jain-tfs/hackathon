using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pharos.Server.Application;
using Pharos.Server.Identity.Application.Commands;
using Pharos.Server.Identity.Domain;
using Pharos.Server.Identity.Infrastructure.Authorization;
using IAuthorizationService = Pharos.Server.Application.Security.IAuthorizationService;

namespace Pharos.Server.Identity.Web;

public class UsersControllerTests
{
    [Fact]
    public async Task ChangeCurrentUserPassword_WhenCalled_SendsCommandAndRefreshesSignIn()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "test.user"
        };

        var senderMock = new Mock<ISender>();
        senderMock
            .Setup(x => x.Send(It.IsAny<ChangeCurrentUserPasswordCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        var applicationUserManagerMock = new Mock<IApplicationUserManager>();
        applicationUserManagerMock
            .Setup(x => x.FindByGuidAsync(userId))
            .ReturnsAsync(user);

        var signInManagerMock = CreateSignInManagerMock();
        signInManagerMock
            .Setup(x => x.RefreshSignInAsync(user))
            .Returns(Task.CompletedTask);

        var authorizationServiceMock = new Mock<IAuthorizationService>();

        var controller = new UsersController(
            senderMock.Object,
            currentUserServiceMock.Object,
            applicationUserManagerMock.Object,
            signInManagerMock.Object,
            authorizationServiceMock.Object,
            Options.Create(new IdentityOptions()));

        var command = new ChangeCurrentUserPasswordCommand("CurrentPassword123!", "NewPassword123!");

        // Act
        await controller.ChangeCurrentUserPassword(command, TestContext.Current.CancellationToken);

        // Assert
        senderMock.Verify(x => x.Send(command, It.IsAny<CancellationToken>()), Times.Once);
        applicationUserManagerMock.Verify(x => x.FindByGuidAsync(userId), Times.Once);
        signInManagerMock.Verify(x => x.RefreshSignInAsync(user), Times.Once);
    }

    [Fact]
    public async Task ChangeCurrentUserPassword_WhenUserIdIsMissing_ThrowsInvalidOperationException()
    {
        // Arrange
        var senderMock = new Mock<ISender>();
        senderMock
            .Setup(x => x.Send(It.IsAny<ChangeCurrentUserPasswordCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);

        var applicationUserManagerMock = new Mock<IApplicationUserManager>();
        var signInManagerMock = CreateSignInManagerMock();
        var authorizationServiceMock = new Mock<IAuthorizationService>();

        var controller = new UsersController(
            senderMock.Object,
            currentUserServiceMock.Object,
            applicationUserManagerMock.Object,
            signInManagerMock.Object,
            authorizationServiceMock.Object,
            Options.Create(new IdentityOptions()));

        var command = new ChangeCurrentUserPasswordCommand("CurrentPassword123!", "NewPassword123!");

        // Act
        var act = () => controller.ChangeCurrentUserPassword(command, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        signInManagerMock.Verify(x => x.RefreshSignInAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task ChangeCurrentUserPassword_WhenUserIsNotFound_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var senderMock = new Mock<ISender>();
        senderMock
            .Setup(x => x.Send(It.IsAny<ChangeCurrentUserPasswordCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        var applicationUserManagerMock = new Mock<IApplicationUserManager>();
        applicationUserManagerMock
            .Setup(x => x.FindByGuidAsync(userId))
            .ReturnsAsync((ApplicationUser?)null);

        var signInManagerMock = CreateSignInManagerMock();
        var authorizationServiceMock = new Mock<IAuthorizationService>();

        var controller = new UsersController(
            senderMock.Object,
            currentUserServiceMock.Object,
            applicationUserManagerMock.Object,
            signInManagerMock.Object,
            authorizationServiceMock.Object,
            Options.Create(new IdentityOptions()));

        var command = new ChangeCurrentUserPasswordCommand("CurrentPassword123!", "NewPassword123!");

        // Act
        var act = () => controller.ChangeCurrentUserPassword(command, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        signInManagerMock.Verify(x => x.RefreshSignInAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    private static Mock<SignInManager<ApplicationUser>> CreateSignInManagerMock()
    {
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        var userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);

        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock
            .Setup(x => x.HttpContext)
            .Returns(new DefaultHttpContext());

        var userClaimsPrincipalFactoryMock = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
        var loggerMock = new Mock<ILogger<SignInManager<ApplicationUser>>>();
        var authenticationSchemeProviderMock = new Mock<IAuthenticationSchemeProvider>();
        var userConfirmationMock = new Mock<IUserConfirmation<ApplicationUser>>();

        return new Mock<SignInManager<ApplicationUser>>(
            userManagerMock.Object,
            httpContextAccessorMock.Object,
            userClaimsPrincipalFactoryMock.Object,
            Options.Create(new IdentityOptions()),
            loggerMock.Object,
            authenticationSchemeProviderMock.Object,
            userConfirmationMock.Object);
    }
}
