using Microsoft.Extensions.Options;
using Pharos.Contracts.Security;
using Pharos.Server.Application.Reflex;
using Pharos.Server.Application.Security;
using Pharos.Server.Application.Settings.Request;
using Pharos.Server.Request.Application.Commands.DeleteRequestor;

namespace Pharos.Server.Request.Application.Commands;

public class DeleteRequestorCommandHandlerTests
{
    private readonly Mock<IRequestDbContext> _dbContextMock;
    private readonly Mock<IAuthorizationService> _authorizationServiceMock;
    private readonly Mock<IOptionsSnapshot<RequestSettings>> _requestSettingsMock;
    private readonly Mock<ISender> _senderMock;
    private readonly DeleteRequestorCommandHandler _sut;

    public DeleteRequestorCommandHandlerTests()
    {
        _dbContextMock = new Mock<IRequestDbContext>();
        _authorizationServiceMock = new Mock<IAuthorizationService>();
        _requestSettingsMock = new Mock<IOptionsSnapshot<RequestSettings>>();
        _senderMock = new Mock<ISender>();

        // Setup default authorization
        _authorizationServiceMock
            .Setup(x => x.AuthorizeCurrentUserAsync(Permissions.ViewPersonalAndHealthData))
            .ReturnsAsync(true);

        _requestSettingsMock.Setup(x => x.Value).Returns(new RequestSettings
        {
            IsRequestorInformationEnabled = true
        });

        _senderMock.Setup(x => x.Send(It.IsAny<GetRequestorsWithReflexQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);

        _sut = new DeleteRequestorCommandHandler(
            _dbContextMock.Object,
            _authorizationServiceMock.Object,
            _requestSettingsMock.Object,
            _senderMock.Object
        );
    }

    [Fact]
    public async Task Handle_Should_DeleteRequestors_When_ValidIds()
    {
        // Arrange
        var requestor1 = new Requestor
        {
            Id = 1,
            RequestorId = "REQ001",
            Name = "Requestor 1"
        };

        var requestor2 = new Requestor
        {
            Id = 2,
            RequestorId = "REQ002",
            Name = "Requestor 2"
        };

        var requestors = new List<Requestor> { requestor1, requestor2 };
        var requests = new List<Domain.Request>(); // No connected requests

        _dbContextMock.Setup(x => x.Requestors).ReturnsDbSet(requestors);
        _dbContextMock.Setup(x => x.Requests).ReturnsDbSet(requests);

        var command = new DeleteRequestorCommand([1, 2]);

        // Act
        var result = await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Deleted.Should().Be(2);
        result.Skipped.Should().Be(0);

        _dbContextMock.Verify(x => x.Requestors.RemoveRange(It.IsAny<IEnumerable<Requestor>>()), Times.Once);
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_SkipRequestorsWithConnectedRequests_When_RequestsExist()
    {
        // Arrange
        var requestorWithRequests = new Requestor
        {
            Id = 1,
            RequestorId = "REQ001",
            Name = "Requestor With Requests"
        };

        var requestorWithoutRequests = new Requestor
        {
            Id = 2,
            RequestorId = "REQ002",
            Name = "Requestor Without Requests"
        };

        var connectedRequest = new Domain.Request
        {
            Id = 100,
            SampleId = "SAMPLE001",
            RequestorId = 1 // Connected to requestor 1
        };

        var requestors = new List<Requestor> { requestorWithRequests, requestorWithoutRequests };
        var requests = new List<Domain.Request> { connectedRequest };

        _dbContextMock.Setup(x => x.Requestors).ReturnsDbSet(requestors);
        _dbContextMock.Setup(x => x.Requests).ReturnsDbSet(requests);

        var command = new DeleteRequestorCommand([1, 2]);

        // Act
        var result = await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Deleted.Should().Be(1); // Only the one without requests
        result.Skipped.Should().Be(1); // The one with requests

        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ReturnCorrectCounts_When_SomeRequestorsNotFound()
    {
        // Arrange
        var existingRequestor = new Requestor
        {
            Id = 1,
            RequestorId = "REQ001",
            Name = "Existing Requestor"
        };

        var requestors = new List<Requestor> { existingRequestor };
        var requests = new List<Domain.Request>(); // No connected requests

        _dbContextMock.Setup(x => x.Requestors).ReturnsDbSet(requestors);
        _dbContextMock.Setup(x => x.Requests).ReturnsDbSet(requests);

        var command = new DeleteRequestorCommand([1, 999]); // 999 doesn't exist

        // Act
        var result = await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Deleted.Should().Be(1); // Only the existing one
        result.Skipped.Should().Be(0); // The non-existing one doesn't count as skipped

        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_HandleEmptyResult_When_NoRequestorsFound()
    {
        // Arrange
        var requestors = new List<Requestor>();
        var requests = new List<Domain.Request>();

        _dbContextMock.Setup(x => x.Requestors).ReturnsDbSet(requestors);
        _dbContextMock.Setup(x => x.Requests).ReturnsDbSet(requests);

        var command = new DeleteRequestorCommand([999, 998]);

        // Act
        var result = await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Deleted.Should().Be(0);
        result.Skipped.Should().Be(0);

        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ThrowUnauthorizedAccessException_When_UserNotAuthorized()
    {
        // Arrange
        _authorizationServiceMock
            .Setup(x => x.AuthorizeCurrentUserAsync(Permissions.ViewPersonalAndHealthData))
            .ReturnsAsync(false);

        var command = new DeleteRequestorCommand([1]);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.Handle(command, TestContext.Current.CancellationToken));

        exception.Message.Should().Be("User is not authorized to delete requestors.");
    }

    [Fact]
    public async Task Handle_Should_ThrowUnauthorizedAccessException_When_RequestorInformationDisabled()
    {
        // Arrange
        _requestSettingsMock.Setup(x => x.Value).Returns(new RequestSettings
        {
            IsRequestorInformationEnabled = false
        });

        var command = new DeleteRequestorCommand([1]);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.Handle(command, TestContext.Current.CancellationToken));

        exception.Message.Should().Be("User is not authorized to delete requestors.");
    }

    [Fact]
    public async Task Handle_Should_HandleMixedScenario_When_SomeHaveRequestsAndSomeNotFound()
    {
        // Arrange
        var requestorWithRequests = new Requestor
        {
            Id = 1,
            RequestorId = "REQ001",
            Name = "Has Requests"
        };

        var deletableRequestor = new Requestor
        {
            Id = 2,
            RequestorId = "REQ002",
            Name = "Can Delete"
        };

        var connectedRequest = new Domain.Request
        {
            Id = 100,
            SampleId = "SAMPLE001",
            RequestorId = 1 // Connected to requestor 1
        };

        var requestors = new List<Requestor> { requestorWithRequests, deletableRequestor };
        var requests = new List<Domain.Request> { connectedRequest };

        _dbContextMock.Setup(x => x.Requestors).ReturnsDbSet(requestors);
        _dbContextMock.Setup(x => x.Requests).ReturnsDbSet(requests);

        // Try to delete IDs: 1 (has requests), 2 (can delete), 3 (not found)
        var command = new DeleteRequestorCommand([1, 2, 3]);

        // Act
        var result = await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Deleted.Should().Be(1); // Only ID 2
        result.Skipped.Should().Be(1); // Only ID 1 (ID 3 not found, so not counted)

        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(new int[] { })]
    [InlineData(new int[] { 1 })]
    [InlineData(new int[] { 1, 2, 3, 4, 5 })]
    public async Task Handle_Should_HandleDifferentArraySizes_When_ValidInput(int[] ids)
    {
        // Arrange
        var requestors = new List<Requestor>();
        var requests = new List<Domain.Request>();

        _dbContextMock.Setup(x => x.Requestors).ReturnsDbSet(requestors);
        _dbContextMock.Setup(x => x.Requests).ReturnsDbSet(requests);

        var command = new DeleteRequestorCommand(ids.ToList());

        // Act
        var result = await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Deleted.Should().Be(0);
        result.Skipped.Should().Be(0);

        if (ids.Length > 0)
        {
            _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }

    [Fact]
    public async Task Handle_WhenRequestorIsUsedByReflex_ShouldNotDeleteRequestor()
    {
        // Arrange
        var requestors = new List<Requestor>
        {
            new()
            {
                Id = 1,
                RequestorId = "R001"
            }
        };

        var requests = new List<Domain.Request>();

        _dbContextMock.Setup(x => x.Requestors).ReturnsDbSet(requestors);
        _dbContextMock.Setup(x => x.Requests).ReturnsDbSet(requests);

        _senderMock.Setup(x => x.Send(It.IsAny<GetRequestorsWithReflexQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(["R001"]);

        var command = new DeleteRequestorCommand([1]);

        // Act
        var result = await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Deleted.Should().Be(0);
        result.Skipped.Should().Be(1);
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}