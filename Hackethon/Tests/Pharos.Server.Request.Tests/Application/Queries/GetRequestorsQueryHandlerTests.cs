using Microsoft.Extensions.Options;
using Pharos.Contracts.Security;
using Pharos.Server.Application.Security;
using Pharos.Server.Application.Settings.Request;
using Pharos.Server.Request.Application.Queries.GetRequestors;

namespace Pharos.Server.Request.Application.Queries;

public class GetRequestorsQueryHandlerTests
{
    private readonly Mock<IRequestDbContext> _dbContextMock;
    private readonly Mock<IAuthorizationService> _authorizationServiceMock;
    private readonly Mock<IOptionsSnapshot<RequestSettings>> _requestSettingsMock;
    private readonly GetRequestorsQueryHandler _sut;

    public GetRequestorsQueryHandlerTests()
    {
        _dbContextMock = new Mock<IRequestDbContext>();
        _authorizationServiceMock = new Mock<IAuthorizationService>();
        _requestSettingsMock = new Mock<IOptionsSnapshot<RequestSettings>>();

        _sut = new GetRequestorsQueryHandler(
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

        var query = new GetRequestorsQuery();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.Handle(query, TestContext.Current.CancellationToken));

        exception.Message.Should().Be("User is not authorized to view requestors.");
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

        var query = new GetRequestorsQuery();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.Handle(query, TestContext.Current.CancellationToken));

        exception.Message.Should().Be("User is not authorized to view requestors.");
    }
}