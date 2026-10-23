using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Reflex;
using Pharos.Server.Application.Sample;
using Pharos.Server.Application.Settings.Sample;
using Pharos.Server.Domain.Reflex;
using Pharos.Server.Infrastructure;

namespace Pharos.Server.Request.Application.Commands.RequestGenerator;

public class GenerateRequestsCommandHandlerTests
{
    private readonly Mock<IRequestDbContext> _dbContext = new();
    private readonly Mock<IMediator> _mediator = new();
    private readonly Mock<IOptionsSnapshot<TubeTypesSettings>> _tubeSettings = new();
    private readonly Mock<IDateTime> _dateTime = new();
    private readonly Mock<ILogger<GenerateRequestsCommandHandler>> _logger = new();
    private readonly List<Domain.Request> _requests = [];
    private readonly GenerateRequestsCommandHandler _sut;

    public GenerateRequestsCommandHandlerTests()
    {
        _dbContext.Setup(x => x.Requests).ReturnsDbSet(_requests);
        _dbContext
            .Setup(x => x.Requests.AddAsync(It.IsAny<Domain.Request>(), It.IsAny<CancellationToken>()))
            .Callback<Domain.Request, CancellationToken>((request, _) => _requests.Add(request))
            .Returns<Domain.Request, CancellationToken>((_, _) => default);
        _dbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _mediator
            .Setup(x => x.Send(It.IsAny<HasSampleTubeHighPriorityQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mediator
            .Setup(x => x.Send(It.IsAny<GetRequestIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RequestIdResponseDto(-1, DateTimeOffset.MinValue, false));
        _mediator
            .Setup(x => x.Send(It.IsAny<GetReflexDefinitionsForTestQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IReflexDefinition>());
        _mediator
            .Setup(x => x.Send(It.IsAny<SetSampleInSampleTrayPositionCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mediator
            .Setup(x => x.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _tubeSettings.Setup(x => x.Value).Returns(new TubeTypesSettings
        {
            NormalTubeDefinitionId = "1",
            TubeTypeList = [new TubeType { TubeDefinitionId = 1, Name = "Default" }]
        });
        _dateTime.Setup(x => x.Now).Returns(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));

        _sut = new GenerateRequestsCommandHandler(
            _dbContext.Object,
            _mediator.Object,
            _tubeSettings.Object,
            _dateTime.Object,
            _logger.Object);
    }

    [Trait("ModuleRequirement", "MR_5829")]
    [Trait("ProductRequirement", "PR_1721")]
    [Fact]
    public async Task Handle_ShouldExecuteGeneration()
    {
        var command = BuildValidCommand();

        await _sut.Handle(command, TestContext.Current.CancellationToken);

        _requests.Should().HaveCount(2);
    }

    [Trait("ModuleRequirement", "MR_5835")]
    [Trait("ProductRequirement", "PR_1721")]
    [Fact]
    public async Task Handle_When_RequestProcessingFails_ShouldLogClearChangesAndContinueWithRemainingSamples()
    {
        _mediator
            .Setup(x => x.Send(It.Is<GetRequestIdQuery>(q => q.SampleId == "S000001"), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        _mediator
            .Setup(x => x.Send(It.Is<GetRequestIdQuery>(q => q.SampleId == "S000002"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RequestIdResponseDto(-1, DateTimeOffset.MinValue, false));

        await _sut.Handle(BuildCommand(amountToGenerate: 2), TestContext.Current.CancellationToken);

        _requests.Select(x => x.SampleId).Should().BeEquivalentTo(["S000002"]);
        _dbContext.Verify(x => x.ClearChanges(), Times.Once);
        _dbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        VerifyLoggedError("Failed to process generated request for sample S000001", Times.Once());
    }

    [Trait("ModuleRequirement", "MR_5830")]
    [Trait("ProductRequirement", "PR_1721")]
    [Fact]
    public async Task Handle_When_TrayPlacementFails_ShouldLogAndContinueWithRemainingSamples()
    {
        _mediator
            .Setup(x => x.Send(It.IsAny<GetRequestIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RequestIdResponseDto(-1, DateTimeOffset.MinValue, false));
        _mediator
            .Setup(x => x.Send(
                It.Is<SetSampleInSampleTrayPositionCommand>(c => c.SampleId == "S000001"),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("tray failure"));

        await _sut.Handle(
            BuildCommand(amountToGenerate: 2, placeSamplesInTrays: true, samplesPerTray: 10),
            TestContext.Current.CancellationToken);

        _mediator.Verify(
            x => x.Send(It.IsAny<SetSampleInSampleTrayPositionCommand>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        VerifyLoggedError("Failed to place generated sample S000001", Times.Once());
    }

    [Trait("ModuleRequirement", "MR_5829")]
    [Trait("ProductRequirement", "PR_1721")]
    [Fact]
    public async Task Handle_When_CreatingNewRequests_SetsSourceToGenerator()
    {
        _mediator
            .Setup(x => x.Send(It.IsAny<GetRequestIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RequestIdResponseDto(-1, DateTimeOffset.MinValue, false));

        await _sut.Handle(BuildCommand(amountToGenerate: 1), TestContext.Current.CancellationToken);

        _requests.Should().ContainSingle();
        _requests.Single().Source.Should().Be(RequestSource.Generator);
        _mediator.Verify(
            x => x.Publish(
                It.Is<Events.RequestCreatedEvent>(e => e.NewEntity == _requests.Single()),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Trait("ModuleRequirement", "MR_5829")]
    [Trait("ProductRequirement", "PR_1721")]
    [Fact]
    public async Task Handle_When_RequestAlreadyExists_LogsAndSkipsSample()
    {
        _mediator
            .Setup(x => x.Send(It.Is<GetRequestIdQuery>(q => q.SampleId == "S000001"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RequestIdResponseDto(123, DateTimeOffset.MinValue, true));

        await _sut.Handle(BuildCommand(amountToGenerate: 1), TestContext.Current.CancellationToken);

        _dbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        VerifyLoggedError("Skipping existing sample S000001 because Request Generator does not update existing requests.", Times.Once());
    }

    private void VerifyLoggedError(string messageFragment, Times times)
    {
        _logger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((value, _) => value!.ToString()!.Contains(messageFragment)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);
    }

    private static GenerateRequestsCommand BuildValidCommand() => BuildCommand(amountToGenerate: 2);

    private static GenerateRequestsCommand BuildCommand(
        int amountToGenerate = 1,
        bool placeSamplesInTrays = false,
        int samplesPerTray = 10) => new(new GenerateRequestsDto
        {
            SampleIdHeadingCharacters = "S",
            SampleIdNumberOfDigits = 6,
            SampleIdStartOffset = 1,
            PlaceSamplesInTrays = placeSamplesInTrays,
            TraysStartOffset = 0,
            SamplesPerTray = samplesPerTray,
            AmountToGenerate = amountToGenerate,
            PreDilutionFactor = 1,
            Tests =
        [
            new TestDetails
            {
                MethodName = "MTH1",
                TestName = "TST1",
                Replicates = 1,
                InstrumentDilutionFactor = 1
            }
        ]
        });
}
