using Microsoft.Extensions.Options;
using Pharos.Contracts.Security;
using Pharos.Server.Application.Security;
using Pharos.Server.Application.Settings.Request;
using Pharos.Server.Request.Application.Queries.GetRequestorDetails;

namespace Pharos.Server.Request.Application.Queries;

public class GetRequestorDetailsQueryHandlerTests
{
    private readonly Mock<IRequestDbContext> _dbContextMock;
    private readonly Mock<IAuthorizationService> _authorizationServiceMock;
    private readonly Mock<IOptionsSnapshot<RequestSettings>> _requestSettingsMock;
    private readonly GetRequestorDetailsQueryHandler _sut;

    public GetRequestorDetailsQueryHandlerTests()
    {
        _dbContextMock = new Mock<IRequestDbContext>();
        _authorizationServiceMock = new Mock<IAuthorizationService>();
        _requestSettingsMock = new Mock<IOptionsSnapshot<RequestSettings>>();

        _sut = new GetRequestorDetailsQueryHandler(
            _dbContextMock.Object,
            _authorizationServiceMock.Object,
            _requestSettingsMock.Object
        );
    }

    [Fact]
    public async Task Handle_Should_ReturnRequestorDetails_When_ValidQuery()
    {
        // Arrange
        var lastModified = DateTimeOffset.Now.AddMinutes(-30);
        var latestSampleDate = DateTimeOffset.Now.AddDays(-5);
        var counterResetDate = DateTimeOffset.Now.AddDays(-30);

        var requestor = new Requestor
        {
            Id = 1,
            RequestorId = "REQ001",
            Name = "Test Requestor",
            Address = "123 Test Street",
            Comment = "Test Comment",
            ZipCode = "12345",
            OrderingPhysicianCode = "DR001",
            PhoneNumber = "555-1234",
            ReflexEnabled = true,
            ShortcutCode = "TST",
            LatestSampleDate = latestSampleDate,
            NumberOfReportedSamples = 25,
            NumberOfReportedTests = 75,
            CounterResetDate = counterResetDate,
            LastModified = lastModified
        };

        var requestors = new List<Requestor> { requestor };
        _dbContextMock.Setup(x => x.Requestors).ReturnsDbSet(requestors);

        _authorizationServiceMock
            .Setup(x => x.AuthorizeCurrentUserAsync(Permissions.ViewPersonalAndHealthData))
            .ReturnsAsync(true);

        _requestSettingsMock.Setup(x => x.Value).Returns(new RequestSettings
        {
            IsRequestorInformationEnabled = true
        });

        var query = new GetRequestorDetailsQuery(1);

        // Act
        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.RequestorId.Should().Be("REQ001");
        result.Name.Should().Be("Test Requestor");
        result.PhoneNumber.Should().Be("555-1234");
        result.Address.Should().Be("123 Test Street");
        result.ZipCode.Should().Be("12345");
        result.Comment.Should().Be("Test Comment");
    }

    [Fact]
    public async Task Handle_Should_ReturnNull_When_RequestorNotFound()
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

        var query = new GetRequestorDetailsQuery(999);

        // Act
        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeNull();
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

        var query = new GetRequestorDetailsQuery(1);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.Handle(query, TestContext.Current.CancellationToken));

        exception.Message.Should().Be("User is not authorized to view requestor details.");
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

        var query = new GetRequestorDetailsQuery(1);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.Handle(query, TestContext.Current.CancellationToken));

        exception.Message.Should().Be("User is not authorized to view requestor details.");
    }
}