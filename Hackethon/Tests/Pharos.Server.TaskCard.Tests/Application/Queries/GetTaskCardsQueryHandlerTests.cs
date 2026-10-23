using FluentAssertions;
using Moq;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Infrastructure;
using Pharos.Server.TaskCard.Application.Interfaces;
using Pharos.Server.TaskCard.Application.Queries;
using Pharos.Server.TaskCard.Application.Services;
using Pharos.Server.TaskCard.Domain;
using Xunit;

namespace Pharos.Server.TaskCard.Tests.Application.Queries;

public class GetTaskCardsQueryHandlerTests
{
    private readonly GetTaskCardsQueryHandler _sut;
    private readonly TaskCardService _taskCardService;
    private readonly Mock<IDateTime> _dateTimeMock = new();
    private readonly Mock<ITaskCardRules> _taskCardRulesMock = new();
    private readonly Mock<ISignalTowerManager> _signalTowerManagerMock = new();

    private const string _instrumentNumber = "INST001";
    private static readonly DateTimeOffset _baseTime = new(2024, 12, 11, 7, 0, 0, TimeSpan.Zero);

    public GetTaskCardsQueryHandlerTests()
    {
        _dateTimeMock.Setup(x => x.Now).Returns(_baseTime);
        _taskCardRulesMock.Setup(x => x.ShowOnLockScreen(It.IsAny<TaskCardType>())).Returns(true);

        _taskCardService = new TaskCardService(_dateTimeMock.Object, _taskCardRulesMock.Object, _signalTowerManagerMock.Object);
        _sut = new GetTaskCardsQueryHandler(_taskCardService);
    }

    [Trait("ProductRequirement", "PR_83")]
    [Trait("ModuleRequirement", "MR_4107")]
    [Trait("ProductRequirement", "PR_2633")]
    [Trait("ModuleRequirement", "MR_5889")]
    [Fact]
    public async Task Handle_GetTaskCardsForLockScreenQuery_ReturnsTaskCardsSortedByCardStateDescending()
    {
        //Arrange
        _taskCardService.Add(CreateTaskCard(TaskCardState.Warning,     TaskCardType.SampleDrawerOpen, _baseTime.AddHours(1)));
        _taskCardService.Add(CreateTaskCard(TaskCardState.Error,       TaskCardType.Error,             _baseTime.AddHours(2)));
        _taskCardService.Add(CreateTaskCard(TaskCardState.Information, TaskCardType.ReagentDrawerOpen, _baseTime.AddHours(3)));

        //Act
        var result = await _sut.Handle(new GetTaskCardsForLockScreenQuery(_instrumentNumber), TestContext.Current.CancellationToken);

        //Assert
        result.Should().HaveCount(3);
        result[0].CardState.Should().Be(TaskCardState.Error);
        result[1].CardState.Should().Be(TaskCardState.Warning);
        result[2].CardState.Should().Be(TaskCardState.Information);
    }

    [Trait("ProductRequirement", "PR_83")]
    [Trait("ModuleRequirement", "MR_4110")]
    [Trait("ProductRequirement", "PR_2633")]
    [Trait("ModuleRequirement", "MR_5889")]
    [Fact]
    public async Task Handle_GetTaskCardsForLockScreenQuery_WhenStatesAreEqual_ReturnsTaskCardsSortedByExpiresAtAscending()
    {
        //Arrange
        _taskCardService.Add(CreateTaskCard(TaskCardState.Warning, TaskCardType.SampleDrawerOpen, _baseTime.AddHours(3)));
        _taskCardService.Add(CreateTaskCard(TaskCardState.Warning, TaskCardType.Error,             _baseTime.AddHours(1)));
        _taskCardService.Add(CreateTaskCard(TaskCardState.Warning, TaskCardType.ReagentDrawerOpen, _baseTime.AddHours(2)));

        //Act
        var result = await _sut.Handle(new GetTaskCardsForLockScreenQuery(_instrumentNumber), TestContext.Current.CancellationToken);

        //Assert
        result.Should().HaveCount(3);
        result[0].ExpiresAt.Should().Be(_baseTime.AddHours(1));
        result[1].ExpiresAt.Should().Be(_baseTime.AddHours(2));
        result[2].ExpiresAt.Should().Be(_baseTime.AddHours(3));
    }

    [Trait("ProductRequirement", "PR_83")]
    [Trait("ModuleRequirement", "MR_4107")]
    [Fact]
    public async Task Handle_GetTaskCardsQuery_WhenGlobalAndInstrumentErrorTaskCardsExist_ReturnsOneMergedErrorTaskCard()
    {
        // Arrange
        _taskCardService.Add(CreateTaskCard(TaskCardState.Warning, TaskCardType.SystemError, _baseTime.AddHours(2), instrumentNumber: null));
        _taskCardService.Add(CreateTaskCard(TaskCardState.Error, TaskCardType.Error, _baseTime.AddHours(1)));

        // Act
        var result = await _sut.Handle(new GetTaskCardsQuery(_instrumentNumber), TestContext.Current.CancellationToken);

        // Assert
        result.Should().HaveCount(1);
        result[0].CardType.Should().Be(TaskCardType.Error);
        result[0].CardState.Should().Be(TaskCardState.Error);
    }

    [Trait("ProductRequirement", "PR_83")]
    [Trait("ModuleRequirement", "MR_4107")]
    [Fact]
    public async Task Handle_GetTaskCardInformationQuery_WhenGlobalAndInstrumentErrorTaskCardsExist_CountsMergedErrorTaskCardOnce()
    {
        // Arrange
        _taskCardService.Add(CreateTaskCard(TaskCardState.Warning, TaskCardType.SystemError, _baseTime.AddHours(2), instrumentNumber: null));
        _taskCardService.Add(CreateTaskCard(TaskCardState.Error, TaskCardType.Error, _baseTime.AddHours(1)));

        // Act
        var result = await _sut.Handle(new GetTaskCardInformationQuery(_instrumentNumber), TestContext.Current.CancellationToken);

        // Assert
        result.NoOfTaskCards.Should().Be(1);
        result.HighestTaskCardState.Should().Be(TaskCardState.Error);
    }

    private static Domain.TaskCard CreateTaskCard(TaskCardState state, TaskCardType cardType, DateTimeOffset? expiresAt, string? instrumentNumber = _instrumentNumber)
    {
        return new Domain.TaskCard([new TriggerState(state, _baseTime)])
        {
            CardType = cardType,
            ExpiresAt = expiresAt,
            InstrumentNumber = instrumentNumber
        };
    }
}