using FluentAssertions;
using Moq;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Infrastructure;
using Pharos.Server.TaskCard.Application.Interfaces;
using Pharos.Server.TaskCard.Application.Services;
using Pharos.Server.TaskCard.Domain;
using Xunit;

namespace Pharos.Server.TaskCard.Tests;

public class TaskCardServiceTests
{
    private readonly TaskCardService _sut;
    private readonly Mock<IDateTime> _dateTimeMock = new();
    private readonly Mock<ITaskCardRules> _taskCardRulesMock = new();
    private readonly Mock<ISignalTowerManager> _signalTowerControllerMock = new();

    private const string _instrumentNumber = "INST001";

    public TaskCardServiceTests()
    {
        _sut = new TaskCardService(_dateTimeMock.Object, _taskCardRulesMock.Object, _signalTowerControllerMock.Object);
    }

    [Trait("ProductRequirement", "PR_83")]
    [Trait("ModuleRequirement", "MR_4107")]
    [Fact]
    public void Add_WhenTaskCardChanges_Should_RaiseTaskCardsChanged()
    {
        var eventRaised = false;
        _sut.TaskCardsChanged += () =>
        {
            eventRaised = true;
            return Task.CompletedTask;
        };

        var taskCard = new Domain.TaskCard([new TriggerState(TaskCardState.Information, DateTimeOffset.UtcNow)])
        {
            CardType = TaskCardType.SampleDrawerOpen,
            InstrumentNumber = _instrumentNumber
        };

        _sut.Add(taskCard);

        eventRaised.Should().BeTrue();
    }

    [Trait("ProductRequirement", "PR_83")]
    [Trait("ModuleRequirement", "MR_4107")]
    [Fact]
    public void Add_WhenExistingTaskCardIsUpdated_Should_RaiseTaskCardsChanged()
    {
        var eventCount = 0;
        _sut.TaskCardsChanged += () =>
        {
            eventCount++;
            return Task.CompletedTask;
        };

        var initialTaskCard = new Domain.TaskCard([new TriggerState(TaskCardState.Information, DateTimeOffset.UtcNow)])
        {
            CardType = TaskCardType.SampleDrawerOpen,
            InstrumentNumber = _instrumentNumber
        };

        var updatedTaskCard = new Domain.TaskCard([new TriggerState(TaskCardState.Warning, DateTimeOffset.UtcNow)])
        {
            CardType = TaskCardType.SampleDrawerOpen,
            InstrumentNumber = _instrumentNumber
        };

        _sut.Add(initialTaskCard);
        _sut.Add(updatedTaskCard);

        eventCount.Should().Be(2);
    }

    [Trait("ProductRequirement", "PR_83")]
    [Trait("ModuleRequirement", "MR_4107")]
    [Fact]
    public void Add_WhenExistingTaskCardHasNoMeaningfulChanges_Should_NotRaiseTaskCardsChangedAgain()
    {
        var eventCount = 0;
        _sut.TaskCardsChanged += () =>
        {
            eventCount++;
            return Task.CompletedTask;
        };

        var trigger = DateTimeOffset.UtcNow;

        var initialTaskCard = new Domain.TaskCard([new TriggerState(TaskCardState.Information, trigger)])
        {
            CardType = TaskCardType.SampleDrawerOpen,
            InstrumentNumber = _instrumentNumber
        };

        var unchangedTaskCard = new Domain.TaskCard([new TriggerState(TaskCardState.Information, trigger)])
        {
            CardType = TaskCardType.SampleDrawerOpen,
            InstrumentNumber = _instrumentNumber
        };

        _sut.Add(initialTaskCard);
        _sut.Add(unchangedTaskCard);

        eventCount.Should().Be(1);

        var taskCards = _sut.GetTaskCardsForInstrument(_instrumentNumber);
        taskCards.Should().HaveCount(1);
    }

    [Trait("ProductRequirement", "PR_83")]
    [Trait("ModuleRequirement", "MR_4107")]
    [Theory]
    [InlineData(TaskCardState.Information)]
    [InlineData(TaskCardState.Warning)]
    [InlineData(TaskCardState.Error)]
    [InlineData(TaskCardState.FlashingRed)]
    public void Add_WhenNewVisibleTaskCardIsAdded_Should_RaiseTaskCardCreatedOrChanged(TaskCardState taskCardState)
    {
        var raisedTaskCards = new List<Domain.TaskCard>();
        _sut.TaskCardCreatedOrChanged += (taskCard) =>
        {
            raisedTaskCards.Add(taskCard);
            return Task.CompletedTask;
        };

        var taskCard = new Domain.TaskCard([new TriggerState(taskCardState, DateTimeOffset.UtcNow)])
        {
            CardType = TaskCardType.SampleDrawerOpen,
            InstrumentNumber = _instrumentNumber
        };

        _sut.Add(taskCard);

        raisedTaskCards.Should().ContainSingle();
        raisedTaskCards[0].CurrentCardState.Should().Be(taskCardState);
    }

    [Trait("ProductRequirement", "PR_83")]
    [Trait("ModuleRequirement", "MR_4107")]
    [Fact]
    public void Add_WhenExistingWarningTaskCardChangesToError_Should_RaiseTaskCardCreatedOrChanged()
    {
        var raisedTaskCards = new List<Domain.TaskCard>();
        _sut.TaskCardCreatedOrChanged += (taskCard) =>
        {
            raisedTaskCards.Add(taskCard);
            return Task.CompletedTask;
        };

        var initialTaskCard = new Domain.TaskCard([new TriggerState(TaskCardState.Warning, DateTimeOffset.UtcNow)])
        {
            CardType = TaskCardType.SampleDrawerOpen,
            InstrumentNumber = _instrumentNumber
        };

        var updatedTaskCard = new Domain.TaskCard([new TriggerState(TaskCardState.Error, DateTimeOffset.UtcNow)])
        {
            CardType = TaskCardType.SampleDrawerOpen,
            InstrumentNumber = _instrumentNumber
        };

        _sut.Add(initialTaskCard);
        raisedTaskCards.Clear();

        _sut.Add(updatedTaskCard);

        raisedTaskCards.Should().ContainSingle();
        raisedTaskCards[0].CurrentCardState.Should().Be(TaskCardState.Error);
    }

    [Trait("ProductRequirement", "PR_83")]
    [Trait("ModuleRequirement", "MR_4107")]
    [Fact]
    public void Add_WhenExistingErrorTaskCardChangesToWarning_Should_RaiseTaskCardCreatedOrChanged()
    {
        var raisedTaskCards = new List<Domain.TaskCard>();
        _sut.TaskCardCreatedOrChanged += (taskCard) =>
        {
            raisedTaskCards.Add(taskCard);
            return Task.CompletedTask;
        };

        var initialTaskCard = new Domain.TaskCard([new TriggerState(TaskCardState.Error, DateTimeOffset.UtcNow)])
        {
            CardType = TaskCardType.SampleDrawerOpen,
            InstrumentNumber = _instrumentNumber
        };

        var updatedTaskCard = new Domain.TaskCard([new TriggerState(TaskCardState.Warning, DateTimeOffset.UtcNow)])
        {
            CardType = TaskCardType.SampleDrawerOpen,
            InstrumentNumber = _instrumentNumber
        };

        _sut.Add(initialTaskCard);
        raisedTaskCards.Clear();

        _sut.Add(updatedTaskCard);

        raisedTaskCards.Should().ContainSingle();
        raisedTaskCards[0].CurrentCardState.Should().Be(TaskCardState.Warning);
    }

    [Trait("ProductRequirement", "PR_83")]
    [Trait("ModuleRequirement", "MR_4107")]
    [Fact]
    public void Add_WhenExistingWarningTaskCardChangesToFlashingRed_Should_RaiseTaskCardCreatedOrChanged()
    {
        var raisedTaskCards = new List<Domain.TaskCard>();
        _sut.TaskCardCreatedOrChanged += (taskCard) =>
        {
            raisedTaskCards.Add(taskCard);
            return Task.CompletedTask;
        };

        var initialTaskCard = new Domain.TaskCard([new TriggerState(TaskCardState.Warning, DateTimeOffset.UtcNow)])
        {
            CardType = TaskCardType.SampleDrawerOpen,
            InstrumentNumber = _instrumentNumber
        };

        var updatedTaskCard = new Domain.TaskCard([new TriggerState(TaskCardState.FlashingRed, DateTimeOffset.UtcNow)])
        {
            CardType = TaskCardType.SampleDrawerOpen,
            InstrumentNumber = _instrumentNumber
        };

        _sut.Add(initialTaskCard);
        raisedTaskCards.Clear();

        _sut.Add(updatedTaskCard);

        raisedTaskCards.Should().ContainSingle();
        raisedTaskCards[0].CurrentCardState.Should().Be(TaskCardState.FlashingRed);
    }

    [Trait("ProductRequirement", "PR_83")]
    [Trait("ModuleRequirement", "MR_4107")]
    [Fact]
    public void Add_WhenExistingInformationTaskCardChangesToWarning_Should_RaiseTaskCardCreatedOrChanged()
    {
        var eventCount = 0;
        _sut.TaskCardCreatedOrChanged += (taskCard) =>
        {
            eventCount++;
            return Task.CompletedTask;
        };

        var initialTaskCard = new Domain.TaskCard([new TriggerState(TaskCardState.Information, DateTimeOffset.UtcNow)])
        {
            CardType = TaskCardType.SampleDrawerOpen,
            InstrumentNumber = _instrumentNumber
        };

        var updatedTaskCard = new Domain.TaskCard([new TriggerState(TaskCardState.Warning, DateTimeOffset.UtcNow)])
        {
            CardType = TaskCardType.SampleDrawerOpen,
            InstrumentNumber = _instrumentNumber
        };

        _sut.Add(initialTaskCard);
        eventCount = 0;

        _sut.Add(updatedTaskCard);

        eventCount.Should().Be(1);
    }

    [Trait("ProductRequirement", "PR_83")]
    [Trait("ModuleRequirement", "MR_4107")]
    [Fact]
    public void AutoEscalate_WhenExistingWarningTaskCardChangesToError_Should_RaiseTaskCardCreatedOrChanged()
    {
        var currentTime = new DateTimeOffset(2026, 7, 27, 10, 0, 0, TimeSpan.Zero);
        _dateTimeMock.Setup(x => x.Now).Returns(() => currentTime);

        var raisedTaskCards = new List<Domain.TaskCard>();
        _sut.TaskCardCreatedOrChanged += (taskCard) =>
        {
            raisedTaskCards.Add(taskCard);
            return Task.CompletedTask;
        };

        var taskCard = new Domain.TaskCard([
            new TriggerState(TaskCardState.Warning, currentTime),
            new TriggerState(TaskCardState.Error, currentTime.AddMinutes(1))
        ])
        {
            CardType = TaskCardType.SampleDrawerOpen,
            InstrumentNumber = _instrumentNumber
        };

        _sut.Add(taskCard);
        raisedTaskCards.Clear();

        currentTime = currentTime.AddMinutes(2);
        _sut.AutoEscalate();

        raisedTaskCards.Should().ContainSingle();
        raisedTaskCards[0].CurrentCardState.Should().Be(TaskCardState.Error);
    }

    [Trait("ProductRequirement", "PR_83")]
    [Trait("ModuleRequirement", "MR_4107")]
    [Theory]
    [InlineData(TaskCardType.SolidWasteEmpty)]
    [InlineData(TaskCardType.LiquidEmptyWaste)]
    [InlineData(TaskCardType.LiquidRefillRinse)]
    public void Add_WhenScheduledWarningIsRepeated_Should_NotifyOnlyOnFirstCreation(TaskCardType cardType)
    {
        var currentTime = new DateTimeOffset(2026, 10, 9, 10, 0, 0, TimeSpan.Zero);
        var stopTime = currentTime.AddHours(1);
        _dateTimeMock.Setup(x => x.Now).Returns(() => currentTime);
        var notifiedStates = RecordTaskCardNotifications();

        _sut.Add(CreateScheduledWarning(cardType, currentTime, stopTime));
        notifiedStates.Should().Equal(TaskCardState.Warning);

        currentTime = currentTime.AddMinutes(1);
        _sut.Add(CreateScheduledWarning(cardType, currentTime, stopTime));
        currentTime = currentTime.AddMinutes(1);
        _sut.Add(CreateScheduledWarning(cardType, currentTime, stopTime));

        notifiedStates.Should().Equal(TaskCardState.Warning);
        var storedCard = _sut.GetTaskCardsForInstrument(_instrumentNumber).Should().ContainSingle().Subject;
        storedCard.CurrentCardState.Should().Be(TaskCardState.Warning);
        storedCard.ExpiresAt.Should().Be(stopTime);
        storedCard.Rules.Last().Trigger.Should().Be(stopTime);
    }

    [Trait("ProductRequirement", "PR_83")]
    [Trait("ModuleRequirement", "MR_4107")]
    [Theory]
    [InlineData(TaskCardType.SolidWasteEmpty)]
    [InlineData(TaskCardType.LiquidEmptyWaste)]
    [InlineData(TaskCardType.LiquidRefillRinse)]
    public void Add_WhenWarningDeadlineChanges_Should_UpdateScheduleWithoutAnotherToastNotification(TaskCardType cardType)
    {
        var currentTime = new DateTimeOffset(2026, 10, 9, 10, 0, 0, TimeSpan.Zero);
        var stopTime = currentTime.AddHours(1);
        _dateTimeMock.Setup(x => x.Now).Returns(() => currentTime);
        var notifiedStates = RecordTaskCardNotifications();
        var refreshCount = 0;
        _sut.TaskCardsChanged += () =>
        {
            refreshCount++;
            return Task.CompletedTask;
        };

        _sut.Add(CreateScheduledWarning(cardType, currentTime, stopTime));
        currentTime = currentTime.AddMinutes(1);
        var updatedStopTime = stopTime.AddMinutes(15);
        _sut.Add(CreateScheduledWarning(cardType, currentTime, updatedStopTime));

        var storedCard = _sut.GetTaskCardsForInstrument(_instrumentNumber).Should().ContainSingle().Subject;
        storedCard.CurrentCardState.Should().Be(TaskCardState.Warning);
        storedCard.ExpiresAt.Should().Be(updatedStopTime);
        storedCard.Rules.Last().Trigger.Should().Be(updatedStopTime);
        refreshCount.Should().Be(2);
        notifiedStates.Should().Equal(TaskCardState.Warning);

        // The old deadline must no longer escalate the updated card.
        currentTime = stopTime;
        _sut.AutoEscalate();
        storedCard.CurrentCardState.Should().Be(TaskCardState.Warning);
        notifiedStates.Should().Equal(TaskCardState.Warning);

        currentTime = updatedStopTime;
        _sut.AutoEscalate();
        storedCard.CurrentCardState.Should().Be(TaskCardState.Error);
        notifiedStates.Should().Equal(TaskCardState.Warning, TaskCardState.Error);
    }

    [Trait("ProductRequirement", "PR_83")]
    [Trait("ModuleRequirement", "MR_4107")]
    [Theory]
    [InlineData(TaskCardType.SolidWasteEmpty)]
    [InlineData(TaskCardType.LiquidEmptyWaste)]
    [InlineData(TaskCardType.LiquidRefillRinse)]
    public void Add_WhenScheduledWarningChangesToReportedError_Should_NotifyErrorOnlyOnce(TaskCardType cardType)
    {
        var currentTime = new DateTimeOffset(2026, 10, 9, 10, 0, 0, TimeSpan.Zero);
        _dateTimeMock.Setup(x => x.Now).Returns(() => currentTime);
        var notifiedStates = RecordTaskCardNotifications();

        _sut.Add(CreateScheduledWarning(cardType, currentTime, currentTime.AddHours(1)));
        currentTime = currentTime.AddMinutes(1);
        _sut.Add(CreateReportedError(cardType, currentTime));
        currentTime = currentTime.AddMinutes(1);
        _sut.Add(CreateReportedError(cardType, currentTime));
        _sut.AutoEscalate();

        notifiedStates.Should().Equal(TaskCardState.Warning, TaskCardState.Error);
        _sut.GetTaskCardsForInstrument(_instrumentNumber).Should().ContainSingle()
            .Which.CurrentCardState.Should().Be(TaskCardState.Error);
    }

    [Trait("ProductRequirement", "PR_83")]
    [Trait("ModuleRequirement", "MR_4107")]
    [Theory]
    [InlineData(TaskCardType.SolidWasteEmpty)]
    [InlineData(TaskCardType.LiquidEmptyWaste)]
    [InlineData(TaskCardType.LiquidRefillRinse)]
    public void AutoEscalate_WhenWarningBecomesErrorAndErrorIsReportedAgain_Should_NotifyErrorOnlyOnce(TaskCardType cardType)
    {
        var currentTime = new DateTimeOffset(2026, 10, 9, 10, 0, 0, TimeSpan.Zero);
        var stopTime = currentTime.AddHours(1);
        _dateTimeMock.Setup(x => x.Now).Returns(() => currentTime);
        var notifiedStates = RecordTaskCardNotifications();

        _sut.Add(CreateScheduledWarning(cardType, currentTime, stopTime));
        currentTime = stopTime;
        _sut.AutoEscalate();
        _sut.AutoEscalate();
        notifiedStates.Should().Equal(TaskCardState.Warning, TaskCardState.Error);

        // ISW now reports the stopped condition as a single-state red card.
        _sut.Add(CreateReportedError(cardType, currentTime));
        currentTime = currentTime.AddMinutes(1);
        _sut.Add(CreateReportedError(cardType, currentTime));

        notifiedStates.Should().Equal(TaskCardState.Warning, TaskCardState.Error);
        _sut.GetTaskCardsForInstrument(_instrumentNumber).Should().ContainSingle()
            .Which.CurrentCardState.Should().Be(TaskCardState.Error);
    }

    [Trait("ProductRequirement", "PR_83")]
    [Trait("ModuleRequirement", "MR_4107")]
    [Theory]
    [InlineData(TaskCardType.SolidWasteEmpty)]
    [InlineData(TaskCardType.LiquidEmptyWaste)]
    [InlineData(TaskCardType.LiquidRefillRinse)]
    public void Add_WhenScheduleIsRepeatedAfterDeadlineBeforeAutoEscalation_Should_DeferErrorNotificationUntilAutoEscalation(TaskCardType cardType)
    {
        var currentTime = new DateTimeOffset(2026, 10, 9, 10, 0, 0, TimeSpan.Zero);
        var warningTime = currentTime;
        var stopTime = currentTime.AddHours(1);
        _dateTimeMock.Setup(x => x.Now).Returns(() => currentTime);
        var notifiedStates = RecordTaskCardNotifications();

        _sut.Add(CreateScheduledWarning(cardType, warningTime, stopTime));
        currentTime = stopTime;
        _sut.Add(CreateScheduledWarning(cardType, warningTime, stopTime));

        // An identical schedule is skipped by Add; the timer applies the pending state transition.
        _sut.GetTaskCardsForInstrument(_instrumentNumber).Should().ContainSingle()
            .Which.CurrentCardState.Should().Be(TaskCardState.Warning);
        notifiedStates.Should().Equal(TaskCardState.Warning);

        _sut.AutoEscalate();
        _sut.GetTaskCardsForInstrument(_instrumentNumber).Should().ContainSingle()
            .Which.CurrentCardState.Should().Be(TaskCardState.Error);
        notifiedStates.Should().Equal(TaskCardState.Warning, TaskCardState.Error);

        _sut.AutoEscalate();
        _sut.Add(CreateScheduledWarning(cardType, warningTime, stopTime));
        notifiedStates.Should().Equal(TaskCardState.Warning, TaskCardState.Error);
    }

    [Trait("ProductRequirement", "PR_83")]
    [Trait("ModuleRequirement", "MR_4107")]
    [Theory]
    [InlineData(TaskCardType.SolidWasteEmpty)]
    [InlineData(TaskCardType.LiquidEmptyWaste)]
    [InlineData(TaskCardType.LiquidRefillRinse)]
    public void Add_WhenWarningIsRaisedAfterRemoval_Should_NotifyAgain(TaskCardType cardType)
    {
        var currentTime = new DateTimeOffset(2026, 10, 9, 10, 0, 0, TimeSpan.Zero);
        var stopTime = currentTime.AddHours(1);
        _dateTimeMock.Setup(x => x.Now).Returns(() => currentTime);
        var notifiedStates = RecordTaskCardNotifications();

        _sut.Add(CreateScheduledWarning(cardType, currentTime, stopTime));
        _sut.Remove(cardType, _instrumentNumber);
        _sut.Exists(cardType, _instrumentNumber).Should().BeFalse();
        currentTime = currentTime.AddMinutes(1);
        _sut.Add(CreateScheduledWarning(cardType, currentTime, stopTime));

        notifiedStates.Should().Equal(TaskCardState.Warning, TaskCardState.Warning);
        _sut.GetTaskCardsForInstrument(_instrumentNumber).Should().ContainSingle();
    }

    [Trait("ProductRequirement", "PR_83")]
    [Trait("ModuleRequirement", "MR_4107")]
    [Theory]
    [InlineData(TaskCardType.SolidWasteEmpty)]
    [InlineData(TaskCardType.LiquidEmptyWaste)]
    [InlineData(TaskCardType.LiquidRefillRinse)]
    public void Add_WhenSameWarningIsRaisedForAnotherInstrument_Should_NotifyForEachInstrument(TaskCardType cardType)
    {
        var currentTime = new DateTimeOffset(2026, 10, 9, 10, 0, 0, TimeSpan.Zero);
        var stopTime = currentTime.AddHours(1);
        _dateTimeMock.Setup(x => x.Now).Returns(() => currentTime);
        var notifiedInstruments = new List<string?>();
        _sut.TaskCardCreatedOrChanged += taskCard =>
        {
            notifiedInstruments.Add(taskCard.InstrumentNumber);
            return Task.CompletedTask;
        };

        _sut.Add(CreateScheduledWarning(cardType, currentTime, stopTime));
        _sut.Add(CreateScheduledWarning(cardType, currentTime, stopTime, "INST002"));

        notifiedInstruments.Should().Equal(_instrumentNumber, "INST002");
        _sut.GetTaskCardsForInstrument(_instrumentNumber).Should().ContainSingle();
        _sut.GetTaskCardsForInstrument("INST002").Should().ContainSingle();
    }

    private List<TaskCardState> RecordTaskCardNotifications()
    {
        var states = new List<TaskCardState>();
        _sut.TaskCardCreatedOrChanged += taskCard =>
        {
            // Capture the state now: AutoEscalate later mutates the same card instance.
            states.Add(taskCard.CurrentCardState);
            return Task.CompletedTask;
        };
        return states;
    }

    private static Domain.TaskCard CreateScheduledWarning(
        TaskCardType cardType, DateTimeOffset warningTime, DateTimeOffset stopTime, string instrumentNumber = _instrumentNumber)
    {
        return new Domain.TaskCard([
            new TriggerState(TaskCardState.Warning, warningTime),
            new TriggerState(TaskCardState.Error, stopTime)
        ])
        {
            CardType = cardType,
            InstrumentNumber = instrumentNumber,
            ExpiresAt = stopTime
        };
    }

    private static Domain.TaskCard CreateReportedError(TaskCardType cardType, DateTimeOffset currentTime)
    {
        return new Domain.TaskCard([new TriggerState(TaskCardState.Error, currentTime)])
        {
            CardType = cardType,
            InstrumentNumber = _instrumentNumber
        };
    }

    [Trait("ProductRequirement", "PR_83")]
    [Trait("ModuleRequirement", "MR_4107")]
    [Fact]
    public void Create_new_task_card()
    {
        //Arrange

        //Set up escalation schedule
        var escalationRules = new List<TriggerState>
        {
            new(TaskCardState.Information, DateTimeOffset.Now)
        };

        //Set up task card
        var taskCard = new Domain.TaskCard(escalationRules)
        {
            CardType = TaskCardType.SampleDrawerOpen,
            InstrumentNumber = _instrumentNumber
        };

        //Add task card to list
        _sut.Add(taskCard);

        //Act

        //Get current task cards
        var taskCards = _sut.GetTaskCardsForInstrument(_instrumentNumber);

        //Assert

        //Check that there is a task card in the list and that the task card is of correct type
        taskCards.FirstOrDefault().Should().NotBe(null);

        taskCards.FirstOrDefault()!.CardType.Should().Be(TaskCardType.SampleDrawerOpen);
    }

    [Trait("ProductRequirement", "PR_83")]
    [Trait("ModuleRequirement", "MR_4108")]
    [Fact]
    public void Remove_existing_task_card()
    {
        //Arrange

        //Set up escalation schedule
        var escalationRules = new List<TriggerState>
        {
            new(TaskCardState.Information, DateTimeOffset.Now)
        };

        //Set up task card
        var taskCard = new Domain.TaskCard(escalationRules)
        {
            CardType = TaskCardType.SampleDrawerOpen,
            InstrumentNumber = _instrumentNumber
        };

        //Add task card to list
        _sut.Add(taskCard);


        //Act

        //Get task card and see that it exists
        var taskCards = _sut.GetTaskCardsForInstrument(_instrumentNumber);

        taskCards.FirstOrDefault().Should().NotBe(null);

        //Remove task card and see that the list is empty
        _sut.Remove(TaskCardType.SampleDrawerOpen, _instrumentNumber);

        taskCards = _sut.GetTaskCardsForInstrument(_instrumentNumber);

        taskCards.FirstOrDefault().Should().Be(null);
    }

    [Trait("ProductRequirement", "PR_83")]
    [Trait("ModuleRequirement", "MR_4107")]
    [Fact]
    public void AutoEscalation_should_escalate_according_to_schedule()
    {
        //Arrange
        var scheduleDate = new DateTimeOffset(2024, 12, 11, 7, 0, 0, new TimeSpan(0));

        //Set up escalation schedule
        var escalationRules = new List<TriggerState>
        {
            new(TaskCardState.Information, scheduleDate),
            new(TaskCardState.Warning, scheduleDate.AddMinutes(2)),
            new(TaskCardState.Error, scheduleDate.AddMinutes(4)),
            new(TaskCardState.Warning, scheduleDate.AddMinutes(6))
        };

        //Set up task card
        var taskCard = new Domain.TaskCard(escalationRules)
        {
            CardType = TaskCardType.SampleDrawerOpen,
            InstrumentNumber = _instrumentNumber
        };

        //Add task card to list
        _sut.Add(taskCard);

        //Act / Assert

        //Setup time and run auto escalation
        _dateTimeMock.Setup(x => x.Now).Returns(scheduleDate);
        _sut.AutoEscalate();

        //See that the CurrentState is Information at the current time
        var taskCards = _sut.GetTaskCardsForInstrument(_instrumentNumber);
        taskCards.First().CurrentCardState.Should().Be(TaskCardState.Information);

        //See that the CurrentState has increased to Warning after 3 minutes
        _dateTimeMock.Setup(x => x.Now).Returns(scheduleDate.AddMinutes(3));
        _sut.AutoEscalate();
        taskCards = _sut.GetTaskCardsForInstrument(_instrumentNumber);
        taskCards.First().CurrentCardState.Should().Be(TaskCardState.Warning);

        //See that the CurrentState has increased to Error after 5 minutes
        _dateTimeMock.Setup(x => x.Now).Returns(scheduleDate.AddMinutes(5));
        _sut.AutoEscalate();
        taskCards = _sut.GetTaskCardsForInstrument(_instrumentNumber);
        taskCards.First().CurrentCardState.Should().Be(TaskCardState.Error);

        //See that the CurrentState has decreased to Warning after 7 minutes
        _dateTimeMock.Setup(x => x.Now).Returns(scheduleDate.AddMinutes(7));
        _sut.AutoEscalate();
        taskCards = _sut.GetTaskCardsForInstrument(_instrumentNumber);
        taskCards.First().CurrentCardState.Should().Be(TaskCardState.Warning);
    }

    [Trait("ProductRequirement", "PR_83")]
    [Trait("ModuleRequirement", "MR_4107")]
    [Fact]
    public void Task_cards_should_return_sorted_by_severity_then_time()
    {
        //Arrange
        var scheduleDate = new DateTimeOffset(2024, 12, 11, 7, 0, 0, new TimeSpan(0));
        var dueDate = new DateTimeOffset(2024, 12, 11, 9, 0, 0, new TimeSpan(0));

        //Set up escalation schedules
        var escalationRules1 = new List<TriggerState>
        {
            new(TaskCardState.Information, scheduleDate),
            new(TaskCardState.Warning, scheduleDate.AddMinutes(2)),
            new(TaskCardState.Error, scheduleDate.AddMinutes(4))
        };

        var escalationRules2 = new List<TriggerState>
        {
            new(TaskCardState.Information, scheduleDate),
            new(TaskCardState.Warning, scheduleDate.AddMinutes(3))
        };

        var escalationRules3 = new List<TriggerState>
        {
            new(TaskCardState.Information, scheduleDate),
            new(TaskCardState.Error, scheduleDate.AddMinutes(5))
        };

        //Set up task cards
        var taskCard1 = new Domain.TaskCard(escalationRules1)
        {
            CardType = TaskCardType.SampleDrawerOpen,
            ExpiresAt = dueDate.AddMinutes(2),
            InstrumentNumber = _instrumentNumber
        };

        var taskCard2 = new Domain.TaskCard(escalationRules2)
        {
            CardType = TaskCardType.Error,
            ExpiresAt = dueDate.AddMinutes(1),
            InstrumentNumber = _instrumentNumber
        };

        var taskCard3 = new Domain.TaskCard(escalationRules3)
        {
            CardType = TaskCardType.BottleDrawerOpen,
            ExpiresAt = dueDate,
            InstrumentNumber = _instrumentNumber
        };

        //Expected lists
        var expectedTaskCardList1 = new List<Domain.TaskCard> { taskCard3, taskCard2, taskCard1 };
        var expectedTaskCardList2 = new List<Domain.TaskCard> { taskCard1, taskCard3, taskCard2 };
        var expectedTaskCardList3 = new List<Domain.TaskCard> { taskCard2, taskCard1, taskCard3 };
        var expectedTaskCardList4 = new List<Domain.TaskCard> { taskCard1, taskCard2, taskCard3 };
        var expectedTaskCardList5 = new List<Domain.TaskCard> { taskCard3, taskCard1, taskCard2 };

        //Add task cards to list
        _sut.Add(taskCard1);
        _sut.Add(taskCard2);
        _sut.Add(taskCard3);

        //Act / Assert

        //See that the task cards are sorted according to first severity and then due time
        _dateTimeMock.Setup(x => x.Now).Returns(scheduleDate);
        _sut.AutoEscalate();
        var taskCards = _sut.GetTaskCardsForInstrument(_instrumentNumber);
        expectedTaskCardList1.Should().BeEquivalentTo(taskCards, o => o.WithStrictOrdering());

        //See that the task cards are sorted after escalation 2 minutes
        _dateTimeMock.Setup(x => x.Now).Returns(scheduleDate.AddMinutes(2));
        _sut.AutoEscalate();
        taskCards = _sut.GetTaskCardsForInstrument(_instrumentNumber);
        expectedTaskCardList2.Should().BeEquivalentTo(taskCards, o => o.WithStrictOrdering());

        //See that the task cards are sorted after escalation 3 minutes
        _dateTimeMock.Setup(x => x.Now).Returns(scheduleDate.AddMinutes(3));
        _sut.AutoEscalate();
        taskCards = _sut.GetTaskCardsForInstrument(_instrumentNumber);
        expectedTaskCardList3.Should().BeEquivalentTo(taskCards, o => o.WithStrictOrdering());

        //See that the task cards are sorted after escalation 4 minutes
        _dateTimeMock.Setup(x => x.Now).Returns(scheduleDate.AddMinutes(4));
        _sut.AutoEscalate();
        taskCards = _sut.GetTaskCardsForInstrument(_instrumentNumber);
        expectedTaskCardList4.Should().BeEquivalentTo(taskCards, o => o.WithStrictOrdering());

        //See that the task cards are sorted after escalation 5 minutes
        _dateTimeMock.Setup(x => x.Now).Returns(scheduleDate.AddMinutes(5));
        _sut.AutoEscalate();
        taskCards = _sut.GetTaskCardsForInstrument(_instrumentNumber);
        expectedTaskCardList5.Should().BeEquivalentTo(taskCards, o => o.WithStrictOrdering());
    }

    [Trait("ProductRequirement", "PR_83")]
    [Trait("ModuleRequirement", "MR_4107")]
    [Fact]
    public void AddTaskCardOfStateHidden_Should_NotAddTheTaskCard()
    {
        var taskCard = new Domain.TaskCard([new(TaskCardState.Hidden, DateTimeOffset.Now)])
        {
            CardType = TaskCardType.Error,
            InstrumentNumber = _instrumentNumber
        };

        _sut.Add(taskCard);

        var taskCards = _sut.GetTaskCardsForInstrument(_instrumentNumber);

        taskCards.Should().BeEmpty();
    }

    [Trait("ProductRequirement", "PR_83")]
    [Trait("ModuleRequirement", "MR_4107")]
    [Fact]
    public void AutoEscalate_When_TheLastStateIsHidden_Should_RemoveTheTaskCard()
    {
        var currentTime = new DateTimeOffset(1982, 1, 19, 0, 0, 0, TimeSpan.Zero);

        _dateTimeMock.Setup(x => x.Now).Returns(currentTime);

        var taskCard = new Domain.TaskCard([
            new(TaskCardState.Information, currentTime),
            new(TaskCardState.Hidden, currentTime.AddMinutes(1))
        ])
        {
            CardType = TaskCardType.Error,
            InstrumentNumber = _instrumentNumber
        };

        _sut.Add(taskCard);

        var taskCards = _sut.GetTaskCardsForInstrument(_instrumentNumber);
        taskCards.Should().HaveCount(1);
        taskCards[0].CurrentCardState.Should().Be(TaskCardState.Information);

        _dateTimeMock.Setup(x => x.Now).Returns(currentTime.AddMinutes(1));
        _sut.AutoEscalate();

        taskCards = _sut.GetTaskCardsForInstrument(_instrumentNumber);
        taskCards.Should().BeEmpty();
    }

    [Trait("ProductRequirement", "PR_83")]
    [Trait("ModuleRequirement", "MR_4107")]
    [Fact]
    public void AutoEscalate_When_TheMiddleStateIsHidden_Should_NotRemoveTheTaskCard()
    {
        var currentTime = new DateTimeOffset(1982, 1, 19, 0, 0, 0, TimeSpan.Zero);

        _dateTimeMock.Setup(x => x.Now).Returns(currentTime);

        var taskCard = new Domain.TaskCard([
            new(TaskCardState.Information, currentTime),
            new(TaskCardState.Hidden, currentTime.AddMinutes(1)),
            new(TaskCardState.Error, currentTime.AddMinutes(2))
        ])
        {
            CardType = TaskCardType.Error,
            InstrumentNumber = _instrumentNumber
        };

        _sut.Add(taskCard);

        var taskCards = _sut.GetTaskCardsForInstrument(_instrumentNumber);
        taskCards.Should().HaveCount(1);
        taskCards[0].CurrentCardState.Should().Be(TaskCardState.Information);

        _dateTimeMock.Setup(x => x.Now).Returns(currentTime.AddMinutes(1));
        _sut.AutoEscalate();
        taskCards = _sut.GetTaskCardsForInstrument(_instrumentNumber);
        taskCards.Should().HaveCount(0);

        _dateTimeMock.Setup(x => x.Now).Returns(currentTime.AddMinutes(2));
        _sut.AutoEscalate();
        taskCards = _sut.GetTaskCardsForInstrument(_instrumentNumber);
        taskCards.Should().HaveCount(1);
        taskCards[0].CurrentCardState.Should().Be(TaskCardState.Error);
    }

    [Trait("ProductRequirement", "PR_83")]
    [Trait("ModuleRequirement", "MR_4110")]
    [Fact]
    public void RemoveTaskCardsForInstrument_When_NoInstrumentNumberIsApplied_Should_NotRemoveTaskCards()
    {
        _taskCardRulesMock.Setup(x => x.TaskCardsToRemoveWhenInstrumentIsDeleted()).Returns(
        [
            TaskCardType.Error
        ]);

        var taskCard = new Domain.TaskCard([new TriggerState(TaskCardState.Error, DateTimeOffset.UtcNow)])
        {
            CardType = TaskCardType.Error,
            InstrumentNumber = _instrumentNumber
        };

        _sut.Add(taskCard);

        _sut.RemoveTaskCardsForInstrument(string.Empty);

        var taskCards = _sut.GetTaskCardsForInstrument(_instrumentNumber);
        taskCards.Should().HaveCount(1);
    }

    [Trait("ProductRequirement", "PR_83")]
    [Trait("ModuleRequirement", "MR_4107")]
    [Fact]
    public void RemoveTaskCardsForInstrument_When_InstrumentNumberIsApplied_Should_RemoveTaskCards()
    {
        _taskCardRulesMock.Setup(x => x.TaskCardsToRemoveWhenInstrumentIsDeleted()).Returns(
        [
            TaskCardType.Error
        ]);

        var taskCard = new Domain.TaskCard([new TriggerState(TaskCardState.Error, DateTimeOffset.UtcNow)])
        {
            CardType = TaskCardType.Error,
            InstrumentNumber = _instrumentNumber
        };

        _sut.Add(taskCard);

        _sut.RemoveTaskCardsForInstrument(_instrumentNumber);

        var taskCards = _sut.GetTaskCardsForInstrument(_instrumentNumber);
        taskCards.Should().HaveCount(0);
    }

    [Trait("ProductRequirement", "PR_83")]
    [Trait("ModuleRequirement", "MR_4110")]
    [Fact]
    public void RemoveTaskCardsForInstrument_When_InstrumentNumberIsAppliedButCardTypeNotAffected_Should_NotRemoveTaskCards()
    {
        _taskCardRulesMock.Setup(x => x.TaskCardsToRemoveWhenInstrumentIsDeleted()).Returns(
        [
            TaskCardType.SampleDrawerOpen
        ]);

        var taskCard = new Domain.TaskCard([new TriggerState(TaskCardState.Error, DateTimeOffset.UtcNow)])
        {
            CardType = TaskCardType.Error,
            InstrumentNumber = _instrumentNumber
        };

        _sut.Add(taskCard);

        _sut.RemoveTaskCardsForInstrument(_instrumentNumber);

        var taskCards = _sut.GetTaskCardsForInstrument(_instrumentNumber);
        taskCards.Should().HaveCount(1);
    }

    [Trait("ProductRequirement", "PR_83")]
    [Trait("ModuleRequirement", "MR_4107")]
    [Fact]
    public void Exists_When_TaskCardExists_Should_ReturnTrue()
    {
        //Arrange
        var instrumentNumber = Guid.NewGuid().ToString();
        var taskCard = new Domain.TaskCard([new TriggerState(TaskCardState.Information, DateTimeOffset.UtcNow)])
        {
            CardType = TaskCardType.SampleDrawerOpen,
            InstrumentNumber = instrumentNumber
        };

        _sut.Add(taskCard);

        //Act
        var exists = _sut.Exists(TaskCardType.SampleDrawerOpen, instrumentNumber);

        //Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public void Exists_When_TaskCardDoesNotExist_Should_ReturnFalse()
    {
        //Arrange
        var instrumentNumber = Guid.NewGuid().ToString();

        //Act
        var exists = _sut.Exists(TaskCardType.SampleDrawerOpen, instrumentNumber);

        //Assert
        exists.Should().BeFalse();
    }

    [Trait("ProductRequirement", "PR_83")]
    [Trait("ModuleRequirement", "MR_4107")]
    [Fact]
    public void Exists_When_TaskCardExistsWithNullInstrumentNumber_Should_ReturnTrue()
    {
        //Arrange
        var taskCard = new Domain.TaskCard([new TriggerState(TaskCardState.Information, DateTimeOffset.UtcNow)])
        {
            CardType = TaskCardType.SystemError,
            InstrumentNumber = null
        };

        _sut.Add(taskCard);

        //Act
        var exists = _sut.Exists(TaskCardType.SystemError, null);

        //Assert
        exists.Should().BeTrue();
    }

    [Trait("ProductRequirement", "PR_83")]
    [Trait("ModuleRequirement", "MR_4107")]
    [Fact]
    public void Exists_When_TaskCardExistsWithEmptyStringInstrumentNumber_Should_ReturnTrue()
    {
        //Arrange
        var taskCard = new Domain.TaskCard([new TriggerState(TaskCardState.Information, DateTimeOffset.UtcNow)])
        {
            CardType = TaskCardType.Error,
            InstrumentNumber = string.Empty
        };

        _sut.Add(taskCard);

        //Act
        var exists = _sut.Exists(TaskCardType.Error, string.Empty);

        //Assert
        exists.Should().BeTrue();
    }

    [Trait("ProductRequirement", "PR_83")]
    [Trait("ModuleRequirement", "MR_4107")]
    [Fact]
    public void Exists_When_TaskCardExistsWithNullInstrumentNumberButSearchingWithEmptyString_Should_ReturnTrue()
    {
        //Arrange
        var taskCard = new Domain.TaskCard([new TriggerState(TaskCardState.Information, DateTimeOffset.UtcNow)])
        {
            CardType = TaskCardType.Error,
            InstrumentNumber = null
        };

        _sut.Add(taskCard);

        //Act
        var exists = _sut.Exists(TaskCardType.Error, string.Empty);

        //Assert
        exists.Should().BeTrue();
    }

    [Trait("ProductRequirement", "PR_83")]
    [Trait("ModuleRequirement", "MR_4107")]
    [Fact]
    public void Exists_When_TaskCardExistsWithEmptyStringInstrumentNumberButSearchingWithNull_Should_ReturnTrue()
    {
        //Arrange
        var taskCard = new Domain.TaskCard([new TriggerState(TaskCardState.Information, DateTimeOffset.UtcNow)])
        {
            CardType = TaskCardType.Error,
            InstrumentNumber = string.Empty
        };

        _sut.Add(taskCard);

        //Act
        var exists = _sut.Exists(TaskCardType.Error, null);

        //Assert
        exists.Should().BeTrue();
    }

    [Trait("ProductRequirement", "PR_83")]
    [Trait("ModuleRequirement", "MR_4107")]
    [Fact]
    public void Exists_When_SameCardTypeButDifferentInstrumentNumber_Should_ReturnFalse()
    {
        //Arrange
        var instrumentNumber1 = Guid.NewGuid().ToString();
        var instrumentNumber2 = Guid.NewGuid().ToString();

        var taskCard = new Domain.TaskCard([new TriggerState(TaskCardState.Information, DateTimeOffset.UtcNow)])
        {
            CardType = TaskCardType.SampleDrawerOpen,
            InstrumentNumber = instrumentNumber1
        };

        _sut.Add(taskCard);

        //Act
        var exists = _sut.Exists(TaskCardType.SampleDrawerOpen, instrumentNumber2);

        //Assert
        exists.Should().BeFalse();
    }

    [Trait("ProductRequirement", "PR_83")]
    [Trait("ModuleRequirement", "MR_4107")]
    [Fact]
    public void Exists_When_SameInstrumentNumberButDifferentCardType_Should_ReturnFalse()
    {
        //Arrange
        var instrumentNumber = Guid.NewGuid().ToString();

        var taskCard = new Domain.TaskCard([new TriggerState(TaskCardState.Information, DateTimeOffset.UtcNow)])
        {
            CardType = TaskCardType.SampleDrawerOpen,
            InstrumentNumber = instrumentNumber
        };

        _sut.Add(taskCard);

        //Act
        var exists = _sut.Exists(TaskCardType.Error, instrumentNumber);

        //Assert
        exists.Should().BeFalse();
    }

    [Trait("ProductRequirement", "PR_83")]
    [Trait("ModuleRequirement", "MR_4107")]
    [Fact]
    public void Exists_When_TaskCardIsRemovedAfterAdding_Should_ReturnFalse()
    {
        //Arrange
        var instrumentNumber = Guid.NewGuid().ToString();
        var taskCard = new Domain.TaskCard([new TriggerState(TaskCardState.Information, DateTimeOffset.UtcNow)])
        {
            CardType = TaskCardType.SampleDrawerOpen,
            InstrumentNumber = instrumentNumber
        };

        _sut.Add(taskCard);

        //Act
        var existsBeforeRemoval = _sut.Exists(TaskCardType.SampleDrawerOpen, instrumentNumber);
        _sut.Remove(TaskCardType.SampleDrawerOpen, instrumentNumber);
        var existsAfterRemoval = _sut.Exists(TaskCardType.SampleDrawerOpen, instrumentNumber);

        //Assert
        existsBeforeRemoval.Should().BeTrue();
        existsAfterRemoval.Should().BeFalse();
    }

    [Trait("ProductRequirement", "PR_83")]
    [Trait("ModuleRequirement", "MR_4107")]
    [Fact]
    public void Add_WhenSingleStateRuleHasSameStateButDifferentTrigger_Should_NotRaiseTaskCardsChangedAgain()
    {
        var eventCount = 0;
        _sut.TaskCardsChanged += () =>
        {
            eventCount++;
            return Task.CompletedTask;
        };

        var initialTaskCard = new Domain.TaskCard([new TriggerState(TaskCardState.Error, DateTimeOffset.UtcNow)])
        {
            CardType = TaskCardType.Error,
            InstrumentNumber = _instrumentNumber
        };

        var unchangedStateNewTriggerTaskCard = new Domain.TaskCard([new TriggerState(TaskCardState.Error, DateTimeOffset.UtcNow.AddSeconds(5))])
        {
            CardType = TaskCardType.Error,
            InstrumentNumber = _instrumentNumber
        };

        _sut.Add(initialTaskCard);
        _sut.Add(unchangedStateNewTriggerTaskCard);

        eventCount.Should().Be(1);
    }
}
