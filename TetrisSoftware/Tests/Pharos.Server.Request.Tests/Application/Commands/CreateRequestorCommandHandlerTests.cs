using FluentValidation;
using Microsoft.Extensions.Options;
using Pharos.Contracts.Security;
using Pharos.Server.Application.Security;
using Pharos.Server.Application.Settings.Request;
using Pharos.Server.Request.Application.Commands.CreateRequestor;

namespace Pharos.Server.Request.Application.Commands;

public class CreateRequestorCommandHandlerTests
{
    private readonly Mock<IRequestDbContext> _dbContextMock;
    private readonly Mock<IAuthorizationService> _authorizationServiceMock;
    private readonly Mock<IOptionsSnapshot<RequestSettings>> _requestSettingsMock;
    private readonly CreateRequestorCommandHandler _sut;

    public CreateRequestorCommandHandlerTests()
    {
        _dbContextMock = new Mock<IRequestDbContext>();
        _authorizationServiceMock = new Mock<IAuthorizationService>();
        _requestSettingsMock = new Mock<IOptionsSnapshot<RequestSettings>>();

        _sut = new CreateRequestorCommandHandler(
            _dbContextMock.Object,
            _authorizationServiceMock.Object,
            _requestSettingsMock.Object
        );
    }

    [Fact]
    public async Task Handle_Should_ThrowUnauthorizedAccessException_When_UserNotAuthorized()
    {
        // Arrange
        _authorizationServiceMock
            .Setup(x => x.AuthorizeCurrentUserAsync(Permissions.ViewPersonalAndHealthData))
            .ReturnsAsync(false);

        _requestSettingsMock.Setup(x => x.Value).Returns(new RequestSettings
        {
            IsRequestorInformationEnabled = true
        });

        var command = new CreateRequestorCommand
        {
            RequestorId = "REQ001",
            Name = "Test Requestor"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.Handle(command, TestContext.Current.CancellationToken));

        exception.Message.Should().Be("User is not authorized to create requestors.");
    }

    [Fact]
    public async Task Handle_Should_ThrowUnauthorizedAccessException_When_RequestorInformationDisabled()
    {
        // Arrange
        _authorizationServiceMock
            .Setup(x => x.AuthorizeCurrentUserAsync(Permissions.ViewPersonalAndHealthData))
            .ReturnsAsync(true);

        _requestSettingsMock.Setup(x => x.Value).Returns(new RequestSettings
        {
            IsRequestorInformationEnabled = false
        });

        var command = new CreateRequestorCommand
        {
            RequestorId = "REQ001",
            Name = "Test Requestor"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.Handle(command, TestContext.Current.CancellationToken));

        exception.Message.Should().Be("User is not authorized to create requestors.");
    }

    [Fact]
    public async Task Handle_Should_ThrowValidationException_When_RequestorIdAlreadyExists()
    {
        // Arrange
        var existingRequestor = new Requestor
        {
            Id = 1,
            RequestorId = "REQ001",
            Name = "Existing Requestor"
        };

        var requestors = new List<Requestor> { existingRequestor };
        _dbContextMock.Setup(x => x.Requestors).ReturnsDbSet(requestors);

        _authorizationServiceMock
            .Setup(x => x.AuthorizeCurrentUserAsync(Permissions.ViewPersonalAndHealthData))
            .ReturnsAsync(true);

        _requestSettingsMock.Setup(x => x.Value).Returns(new RequestSettings
        {
            IsRequestorInformationEnabled = true
        });

        var command = new CreateRequestorCommand
        {
            RequestorId = "REQ001",
            Name = "Test Requestor"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _sut.Handle(command, TestContext.Current.CancellationToken));

        exception.Message.Should().Be("Requestor with ID 'REQ001' already exists.");
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}