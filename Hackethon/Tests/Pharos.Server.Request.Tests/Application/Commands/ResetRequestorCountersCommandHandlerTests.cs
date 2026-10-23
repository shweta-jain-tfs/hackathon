using Microsoft.Extensions.Options;
using Pharos.Contracts.Security;
using Pharos.Server.Application.Security;
using Pharos.Server.Application.Settings.Request;
using Pharos.Server.Infrastructure;
using Pharos.Server.Request.Application.Commands.UpdateRequestor;

namespace Pharos.Server.Request.Application.Commands;

public class ResetRequestorCountersCommandHandlerTests
{
    private readonly Mock<IRequestDbContext> _dbContextMock;
    private readonly Mock<IAuthorizationService> _authorizationServiceMock;
    private readonly Mock<IOptionsSnapshot<RequestSettings>> _requestSettingsMock;
    private readonly Mock<IDateTime> _dateTimeMock;
    private readonly ResetRequestorCountersCommandHandler _sut;

    public ResetRequestorCountersCommandHandlerTests()
    {
        _dbContextMock = new Mock<IRequestDbContext>();
        _authorizationServiceMock = new Mock<IAuthorizationService>();
        _requestSettingsMock = new Mock<IOptionsSnapshot<RequestSettings>>();
        _dateTimeMock = new Mock<IDateTime>();
        _dateTimeMock.Setup(x => x.Now).Returns(DateTimeOffset.UtcNow);

        _authorizationServiceMock
            .Setup(x => x.AuthorizeCurrentUserAsync(Permissions.ViewPersonalAndHealthData))
            .ReturnsAsync(true);

        _requestSettingsMock.Setup(x => x.Value).Returns(new RequestSettings
        {
            IsRequestorInformationEnabled = true
        });

        _sut = new ResetRequestorCountersCommandHandler(
            _dbContextMock.Object,
            _authorizationServiceMock.Object,
            _requestSettingsMock.Object,
            _dateTimeMock.Object
        );
    }

    [Fact]
    public async Task Handle_Should_ResetCounters_When_RequestorExists()
    {
        // Arrange
        var existingRequestor = new Requestor
        {
            Id = 1,
            RequestorId = "REQ001",
            NumberOfReportedSamples = 10,
            NumberOfReportedTests = 25,
            CounterResetDate = DateTimeOffset.UtcNow.AddDays(-30)
        };

        _dbContextMock.Setup(x => x.Requestors).ReturnsDbSet([existingRequestor]);

        // Act
        await _sut.Handle(new ResetRequestorCountersCommand(1), TestContext.Current.CancellationToken);

        // Assert
        existingRequestor.NumberOfReportedSamples.Should().Be(0);
        existingRequestor.NumberOfReportedTests.Should().Be(0);
        existingRequestor.CounterResetDate.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ThrowUnauthorizedAccessException_When_UserNotAuthorized()
    {
        // Arrange
        _authorizationServiceMock
            .Setup(x => x.AuthorizeCurrentUserAsync(Permissions.ViewPersonalAndHealthData))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.Handle(new ResetRequestorCountersCommand(1), TestContext.Current.CancellationToken));

        exception.Message.Should().Be("User is not authorized to reset requestor counters.");
    }
}
