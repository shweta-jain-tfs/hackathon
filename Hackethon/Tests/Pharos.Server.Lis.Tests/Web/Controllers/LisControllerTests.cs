using MediatR;
using Pharos.Server.Application.Lis;
using Pharos.Server.Lis.Application.Commands;
using Pharos.Server.Lis.Application.Commands.LisStatisticsCommands;
using Pharos.Server.Lis.Application.Queries;
using Pharos.Server.Lis.Application.Queries.GetLisConnectionSummary;
using Pharos.Server.Lis.Web.Controllers;

namespace Pharos.Server.Lis.Tests.Web.Controllers;

public class LisControllerTests
{
    private readonly Mock<ISender> _senderMock;
    private readonly LisController _sut;

    public LisControllerTests()
    {
        _senderMock = new Mock<ISender>();
        _sut = new LisController(_senderMock.Object);
    }

    [Trait("ProductRequirement", "PR_2134")]
    [Trait("ModuleRequirement", "MR_5379")]
    [Fact]
    [Trait("ProductRequirement", "PR_564")]
    [Trait("ModuleRequirement", "MR_5850")]
    public async Task GetAll_Should_ReturnLisConnections_When_Called()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var expected = new LisConnectionDto[]
        {
            new() { Id = 0 },
            new() { Id = 1 }
        };
        _senderMock
            .Setup(x => x.Send(It.IsAny<GetLisQuery>(), cancellationToken))
            .ReturnsAsync(expected);

        // Act
        var result = await _sut.GetAll(cancellationToken);

        // Assert
        result.Should().BeEquivalentTo(expected);
        _senderMock.Verify(x => x.Send(It.IsAny<GetLisQuery>(), cancellationToken), Times.Once);
    }

    [Trait("ProductRequirement", "PR_2134")]
    [Trait("ModuleRequirement", "MR_5379")]
    [Fact]
    [Trait("ProductRequirement", "PR_564")]
    [Trait("ModuleRequirement", "MR_5850")]
    public async Task GetAllSummary_Should_ReturnSummaryList_When_Called()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var expected = new List<LisConnectionSummaryDto>
        {
            new() { Id = 0, Name = "Channel 0" },
            new() { Id = 1, Name = "Channel 1" }
        };
        _senderMock
            .Setup(x => x.Send(It.IsAny<GetAllLisConnectionSummaryQuery>(), cancellationToken))
            .ReturnsAsync(expected);

        // Act
        var result = await _sut.GetAllSummary(cancellationToken);

        // Assert
        result.Should().BeEquivalentTo(expected);
        _senderMock.Verify(x => x.Send(It.IsAny<GetAllLisConnectionSummaryQuery>(), cancellationToken), Times.Once);
    }

    [Fact]
    [Trait("ProductRequirement", "PR_564")]
    [Trait("ModuleRequirement", "MR_5850")]
    public async Task GetSummary_Should_ReturnSummary_When_ChannelIdProvided()
    {
        // Arrange
        const int channelId = 2;
        var cancellationToken = TestContext.Current.CancellationToken;
        var expected = new LisConnectionSummaryDto { Id = channelId, Name = "Channel 2" };
        _senderMock
            .Setup(x => x.Send(It.Is<GetLisConnectionSummaryQuery>(q => q.ChannelId == channelId), cancellationToken))
            .ReturnsAsync(expected);

        // Act
        var result = await _sut.GetSummary(channelId, cancellationToken);

        // Assert
        result.Should().BeEquivalentTo(expected);
        _senderMock.Verify(
            x => x.Send(It.Is<GetLisConnectionSummaryQuery>(q => q.ChannelId == channelId), cancellationToken),
            Times.Once);
    }

    [Fact]
    [Trait("ProductRequirement", "PR_564")]
    [Trait("ModuleRequirement", "MR_5865")]
    public async Task ResetStatistics_Should_SendCommand_When_ChannelIdProvided()
    {
        // Arrange
        const int channelId = 3;
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        await _sut.ResetStatistics(channelId, cancellationToken);

        // Assert
        _senderMock.Verify(
            x => x.Send(
                It.Is<ResetLisStatisticsCommand>(cmd => cmd.ChannelId == channelId),
                cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task QueryTestOrdersForSamples_Should_SendCommand_When_SampleIdsProvided()
    {
        // Arrange
        string[] sampleIds = ["SAMPLE1", "SAMPLE2", "SAMPLE3"];
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        await _sut.QueryTestOrdersForSamples(sampleIds, cancellationToken);

        // Assert
        _senderMock.Verify(
            x => x.Send(
                It.Is<SendManualTestOrderRequestsToLisCommand>(
                    cmd => cmd.SampleIds!.SequenceEqual(sampleIds)),
                cancellationToken),
            Times.Once);
    }

    [Fact]
    [Trait("ProductRequirement", "PR_1796")]
    [Trait("ModuleRequirement", "MR_5854")]
    public async Task QueryRefreshExistingRequest_Should_SendCommand_When_SampleIdProvided()
    {
        // Arrange
        const string sampleId = "SAMPLE123";
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        await _sut.QueryRefreshExistingRequest(sampleId, cancellationToken);

        // Assert
        _senderMock.Verify(
            x => x.Send(
                It.Is<RefreshExistingRequestsCommand>(
                    cmd => cmd.SampleIDs.Count == 1 &&
                           cmd.SampleIDs.First() == sampleId &&
                           cmd.ManualScan),
                cancellationToken),
            Times.Once);
    }

    [Fact]
    [Trait("ProductRequirement", "PR_1796")]
    [Trait("ModuleRequirement", "MR_5853")]
    public async Task QueryMissingRequest_Should_SendCommand_When_SampleIdProvided()
    {
        // Arrange
        const string sampleId = "SAMPLE789";
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        await _sut.QueryAskMissingRequest(sampleId, cancellationToken);

        // Assert
        _senderMock.Verify(
            x => x.Send(
                It.Is<AskForMissingTestOrdersCommand>(
                    cmd => cmd.SampleIDs.Count == 1 &&
                           cmd.SampleIDs.First() == sampleId &&
                           cmd.ManualScan),
                cancellationToken),
            Times.Once);
    }
}
