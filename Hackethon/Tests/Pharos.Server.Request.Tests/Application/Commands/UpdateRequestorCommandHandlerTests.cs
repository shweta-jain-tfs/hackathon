using FluentValidation;
using Microsoft.Extensions.Options;
using Pharos.Contracts.Security;
using Pharos.Server.Application.Security;
using Pharos.Server.Application.Settings.Request;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Request.Application.Commands.UpdateRequestor;

namespace Pharos.Server.Request.Application.Commands;

public class UpdateRequestorCommandHandlerTests
{
    private readonly Mock<IRequestDbContext> _dbContextMock;
    private readonly Mock<IAuthorizationService> _authorizationServiceMock;
    private readonly Mock<IOptionsSnapshot<RequestSettings>> _requestSettingsMock;
    private readonly UpdateRequestorCommandHandler _sut;

    public UpdateRequestorCommandHandlerTests()
    {
        _dbContextMock = new Mock<IRequestDbContext>();
        _authorizationServiceMock = new Mock<IAuthorizationService>();
        _requestSettingsMock = new Mock<IOptionsSnapshot<RequestSettings>>();

        _sut = new UpdateRequestorCommandHandler(
            _dbContextMock.Object,
            _authorizationServiceMock.Object,
            _requestSettingsMock.Object
        );
    }

    [Fact]
    public async Task Handle_Should_UpdateRequestor_When_ValidCommand()
    {
        // Arrange
        var lastModified = DateTimeOffset.Now.AddMinutes(-10);
        var existingRequestor = new Requestor
        {
            Id = 1,
            RequestorId = "REQ001",
            Name = "Old Name",
            Address = "Old Address",
            Comment = "Old Comment",
            ZipCode = "00000",
            OrderingPhysicianCode = "OLD001",
            PhoneNumber = "000-0000",
            ReflexEnabled = false,
            ShortcutCode = "OLD",
            LastModified = lastModified
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

        var command = new UpdateRequestorCommand
        {
            Id = 1,
            RequestorId = "REQ001",
            Name = "Updated Name",
            Address = "Updated Address",
            Comment = "Updated Comment",
            ZipCode = "12345",
            OrderingPhysicianCode = "UPD001",
            PhoneNumber = "555-1234",
            ReflexEnabled = true,
            ShortcutCode = "UPD",
            LastModified = lastModified
        };

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        existingRequestor.Name.Should().Be(command.Name);
        existingRequestor.Address.Should().Be(command.Address);
        existingRequestor.Comment.Should().Be(command.Comment);
        existingRequestor.ZipCode.Should().Be(command.ZipCode);
        existingRequestor.OrderingPhysicianCode.Should().Be(command.OrderingPhysicianCode);
        existingRequestor.PhoneNumber.Should().Be(command.PhoneNumber);
        existingRequestor.ReflexEnabled.Should().Be(command.ReflexEnabled);
        existingRequestor.ShortcutCode.Should().Be(command.ShortcutCode);

        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
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

        var command = new UpdateRequestorCommand
        {
            Id = 1,
            RequestorId = "REQ001"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.Handle(command, TestContext.Current.CancellationToken));

        exception.Message.Should().Be("User is not authorized to update requestors.");
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

        var command = new UpdateRequestorCommand
        {
            Id = 1,
            RequestorId = "REQ001"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.Handle(command, TestContext.Current.CancellationToken));

        exception.Message.Should().Be("User is not authorized to update requestors.");
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFoundException_When_RequestorNotFound()
    {
        // Arrange
        var requestors = new List<Requestor>();
        _dbContextMock.Setup(x => x.Requestors).ReturnsDbSet(requestors);

        _authorizationServiceMock
            .Setup(x => x.AuthorizeCurrentUserAsync(Permissions.ViewPersonalAndHealthData))
            .ReturnsAsync(true);

        _requestSettingsMock.Setup(x => x.Value).Returns(new RequestSettings
        {
            IsRequestorInformationEnabled = true
        });

        var command = new UpdateRequestorCommand
        {
            Id = 999,
            RequestorId = "REQ999"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.Handle(command, TestContext.Current.CancellationToken));

        exception.Message.Should().Contain("Requestor");
        exception.Message.Should().Contain("999");
    }

    [Fact]
    public async Task Handle_Should_ThrowValidationException_When_RequestorIdAlreadyExists()
    {
        // Arrange
        var lastModified = DateTimeOffset.Now.AddMinutes(-10);
        var targetRequestor = new Requestor
        {
            Id = 1,
            RequestorId = "REQ001",
            Name = "Target Requestor",
            LastModified = lastModified
        };

        var conflictingRequestor = new Requestor
        {
            Id = 2,
            RequestorId = "REQ002",
            Name = "Conflicting Requestor"
        };

        var requestors = new List<Requestor> { targetRequestor, conflictingRequestor };
        _dbContextMock.Setup(x => x.Requestors).ReturnsDbSet(requestors);

        _authorizationServiceMock
            .Setup(x => x.AuthorizeCurrentUserAsync(Permissions.ViewPersonalAndHealthData))
            .ReturnsAsync(true);

        _requestSettingsMock.Setup(x => x.Value).Returns(new RequestSettings
        {
            IsRequestorInformationEnabled = true
        });

        var command = new UpdateRequestorCommand
        {
            Id = 1,
            RequestorId = "REQ002", // Trying to change to existing RequestorId
            LastModified = lastModified
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _sut.Handle(command, TestContext.Current.CancellationToken));

        exception.Message.Should().Be("Requestor with ID 'REQ002' already exists.");
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_AllowSameRequestorId_When_UpdatingSameRequestor()
    {
        // Arrange
        var lastModified = DateTimeOffset.Now.AddMinutes(-10);
        var existingRequestor = new Requestor
        {
            Id = 1,
            RequestorId = "REQ001",
            Name = "Test Requestor",
            LastModified = lastModified
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

        var command = new UpdateRequestorCommand
        {
            Id = 1,
            RequestorId = "REQ001", // Same RequestorId
            Name = "Updated Name",
            LastModified = lastModified
        };

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        existingRequestor.Name.Should().Be("Updated Name");
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}