using Microsoft.Extensions.Options;
using Moq;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.TaskCard.Application.Services;
using Pharos.Server.TaskCard.Domain;
using Tetris.Protocol.Common;
using Tetris.Protocol.Instrument.Control;
using Xunit;
using FluentAssertions;
using Pharos.Server.Application.Settings.TaskCards;

namespace Pharos.Server.TaskCard.Tests;

public class SignalTowerManagerTests
{
    private readonly Mock<IOptionsMonitor<TaskCardSettings>> _taskCardSettingsMock;
    private readonly SignalTowerManager _sut;

    public SignalTowerManagerTests()
    {
        _taskCardSettingsMock = new Mock<IOptionsMonitor<TaskCardSettings>>();
        _sut = new SignalTowerManager(_taskCardSettingsMock.Object);
    }

    [Trait("ProductRequirement", "PR_83")]
    [Trait("ModuleRequirement", "MR_4107")]
    [Fact]
    public void GetControlSignalTowerRequests_WithEmptyTaskCards_ReturnsEmptyControlTower()
    {
        // Arrange
        var taskCards = new List<Domain.TaskCard>();
        var cardTypesThatControlsAllInstruments = new List<TaskCardType>();

        // Act
        var result = _sut.GetControlSignalTowerRequests(taskCards, cardTypesThatControlsAllInstruments);

        // Assert
        result.InstrumentSpecific.Should().BeEmpty();
        result.AllInstruments.Should().BeNull();
    }

    [Trait("ProductRequirement", "PR_194")]
    [Trait("ModuleRequirement", "MR_4096")]
    [Fact]
    public void GetControlSignalTowerRequests_WithInstrumentSpecificTaskCard_CreatesInstrumentSpecificRequest()
    {
        // Arrange
        var settings = new TaskCardSettings { Sound = true };
        _taskCardSettingsMock.Setup(x => x.CurrentValue).Returns(settings);

        var baseTime = GetTime();

        var taskCards = new List<Domain.TaskCard>
        {
            new(new List<TriggerState>{new(TaskCardState.Error, baseTime)})
            {
                InstrumentNumber = "INST001",
                CardType = TaskCardType.MaintenanceCleaning
            }
        };
        taskCards.ForEach(x => x.SetCurrentCardState(GetTime(baseTime, TimeSpan.FromMinutes(1))));

        var cardTypesThatControlsAllInstruments = new List<TaskCardType>();

        // Act
        var result = _sut.GetControlSignalTowerRequests(taskCards, cardTypesThatControlsAllInstruments);

        // Assert
        result.InstrumentSpecific.Should().HaveCount(1);
        result.InstrumentSpecific.Should().ContainKey("INST001");
        result.InstrumentSpecific["INST001"].SetLight.Should().Be(EnumSignalTowerLightState.Red);
        result.InstrumentSpecific["INST001"].SetSound.Should().Be(EnumSignalTowerControlSoundState.SoundContinuosOn);
        result.AllInstruments.Should().BeNull();
    }

    private static DateTimeOffset GetTime()
    {
        return new DateTimeOffset(1982, 01, 19, 0, 0, 0, TimeSpan.Zero);
    }

    private static DateTimeOffset GetTime(DateTimeOffset dateTime, TimeSpan timeToAdd)
    {
        return dateTime.Add(timeToAdd);
    }

    [Trait("ProductRequirement", "PR_194")]
    [Trait("ModuleRequirement", "MR_4096")]
    [Fact]
    public void GetControlSignalTowerRequests_WithAllInstrumentsTaskCard_CreatesAllInstrumentsRequest()
    {
        // Arrange
        var settings = new TaskCardSettings { Sound = true };
        _taskCardSettingsMock.Setup(x => x.CurrentValue).Returns(settings);

        var baseTime = GetTime();

        var taskCards = new List<Domain.TaskCard>
        {
            new(new List<TriggerState>{new(TaskCardState.Warning, baseTime)})
            {
                CardType = TaskCardType.Error
            }
        };
        taskCards.ForEach(x => x.SetCurrentCardState(GetTime(baseTime, TimeSpan.FromMinutes(1))));

        var cardTypesThatControlsAllInstruments = new List<TaskCardType> { TaskCardType.Error };

        // Act
        var result = _sut.GetControlSignalTowerRequests(taskCards, cardTypesThatControlsAllInstruments);

        // Assert
        result.InstrumentSpecific.Should().BeEmpty();
        result.AllInstruments.Should().NotBeNull();
        result.AllInstruments!.SetLight.Should().Be(EnumSignalTowerLightState.Yellow);
        result.AllInstruments.SetSound.Should().Be(EnumSignalTowerControlSoundState.SoundShortSignal);
    }

    [Trait("ProductRequirement", "PR_194")]
    [Trait("ModuleRequirement", "MR_4096")]
    [Fact]
    public void GetControlSignalTowerRequests_WithMultipleInstrumentSpecificTaskCards_UpdatesHighestSeverity()
    {
        // Arrange
        var settings = new TaskCardSettings { Sound = true };
        _taskCardSettingsMock.Setup(x => x.CurrentValue).Returns(settings);

        var baseTime = GetTime();

        var taskCards = new List<Domain.TaskCard>
        {
            new(new List<TriggerState>{new(TaskCardState.Warning, baseTime)})
            {
                InstrumentNumber = "INST001",
                CardType = TaskCardType.Error
            },
            new(new List<TriggerState>{new(TaskCardState.Error, baseTime)})
            {
                InstrumentNumber = "INST001",
                CardType = TaskCardType.Error
            }
        };
        taskCards.ForEach(x => x.SetCurrentCardState(GetTime(baseTime, TimeSpan.FromMinutes(1))));

        var cardTypesThatControlsAllInstruments = new List<TaskCardType>();

        // Act
        var result = _sut.GetControlSignalTowerRequests(taskCards, cardTypesThatControlsAllInstruments);

        // Assert
        result.InstrumentSpecific.Should().HaveCount(1);
        result.InstrumentSpecific.Should().ContainKey("INST001");
        result.InstrumentSpecific["INST001"].SetLight.Should().Be(EnumSignalTowerLightState.Red);
        result.InstrumentSpecific["INST001"].SetSound.Should().Be(EnumSignalTowerControlSoundState.SoundContinuosOn);
    }

    [Trait("ProductRequirement", "PR_194")]
    [Trait("ModuleRequirement", "MR_4096")]
    [Fact]
    public void GetControlSignalTowerRequests_WithMultipleAllInstrumentsTaskCards_UpdatesHighestSeverity()
    {
        // Arrange
        var settings = new TaskCardSettings { Sound = true };
        _taskCardSettingsMock.Setup(x => x.CurrentValue).Returns(settings);

        var baseTime = GetTime();

        var taskCards = new List<Domain.TaskCard>
        {
            new(new List<TriggerState>{new(TaskCardState.Information, baseTime)})
            {
                CardType = TaskCardType.Error
            },
            new(new List<TriggerState>{new(TaskCardState.FlashingRed, baseTime)})
            {
                CardType = TaskCardType.Error
            }
        };
        taskCards.ForEach(x => x.SetCurrentCardState(GetTime(baseTime, TimeSpan.FromMinutes(1))));

        var cardTypesThatControlsAllInstruments = new List<TaskCardType> { TaskCardType.Error };

        // Act
        var result = _sut.GetControlSignalTowerRequests(taskCards, cardTypesThatControlsAllInstruments);

        // Assert
        result.AllInstruments.Should().NotBeNull();
        result.AllInstruments!.SetLight.Should().Be(EnumSignalTowerLightState.RedBlink);
        result.AllInstruments.SetSound.Should().Be(EnumSignalTowerControlSoundState.SoundContinuosOn);
    }

    [Trait("ProductRequirement", "PR_194")]
    [Trait("ModuleRequirement", "MR_4096")]
    [Fact]
    public void GetControlSignalTowerRequests_WithSoundDisabled_ReturnsSoundNotSet()
    {
        // Arrange
        var settings = new TaskCardSettings { Sound = false };
        _taskCardSettingsMock.Setup(x => x.CurrentValue).Returns(settings);

        var baseTime = GetTime();

        var taskCards = new List<Domain.TaskCard>
        {
            new(new List<TriggerState>{new(TaskCardState.Error, baseTime)})
            {
                InstrumentNumber = "INST001",
                CardType = TaskCardType.Error
            }
        };
        taskCards.ForEach(x => x.SetCurrentCardState(GetTime(baseTime, TimeSpan.FromMinutes(1))));

        var cardTypesThatControlsAllInstruments = new List<TaskCardType>();

        // Act
        var result = _sut.GetControlSignalTowerRequests(taskCards, cardTypesThatControlsAllInstruments);

        // Assert
        result.InstrumentSpecific.Should().HaveCount(1);
        result.InstrumentSpecific["INST001"].SetLight.Should().Be(EnumSignalTowerLightState.Red);
        result.InstrumentSpecific["INST001"].SetSound.Should().Be(EnumSignalTowerControlSoundState.SoundNotSet);
    }

    [Trait("ProductRequirement", "PR_194")]
    [Trait("ModuleRequirement", "MR_4096")]
    [Fact]
    public void GetControlSignalTowerRequests_WithEmptyInstrumentNumber_SkipsTaskCard()
    {
        // Arrange
        var settings = new TaskCardSettings { Sound = true };
        _taskCardSettingsMock.Setup(x => x.CurrentValue).Returns(settings);

        var baseTime = GetTime();

        var taskCards = new List<Domain.TaskCard>
        {
            new(new List<TriggerState>{new(TaskCardState.Error, baseTime)})
            {
                InstrumentNumber = "",
                CardType = TaskCardType.Error
            },
            new(new List<TriggerState>{new(TaskCardState.Warning, baseTime)})
            {
                InstrumentNumber = null,
                CardType = TaskCardType.Error
            }
        };
        taskCards.ForEach(x => x.SetCurrentCardState(GetTime(baseTime, TimeSpan.FromMinutes(1))));

        var cardTypesThatControlsAllInstruments = new List<TaskCardType>();

        // Act
        var result = _sut.GetControlSignalTowerRequests(taskCards, cardTypesThatControlsAllInstruments);

        // Assert
        result.InstrumentSpecific.Should().BeEmpty();
        result.AllInstruments.Should().BeNull();
    }

    [Trait("ProductRequirement", "PR_194")]
    [Trait("ModuleRequirement", "MR_4096")]
    [Theory]
    [InlineData(TaskCardState.Hidden, EnumSignalTowerLightState.Off)]
    [InlineData(TaskCardState.Information, EnumSignalTowerLightState.Off)]
    [InlineData(TaskCardState.Warning, EnumSignalTowerLightState.Yellow)]
    [InlineData(TaskCardState.Error, EnumSignalTowerLightState.Red)]
    [InlineData(TaskCardState.FlashingRed, EnumSignalTowerLightState.RedBlink)]
    public void GetControlSignalTowerRequests_MapsTaskCardStatesToLightStatesCorrectly(TaskCardState cardState, EnumSignalTowerLightState expectedLightState)
    {
        // Arrange
        var settings = new TaskCardSettings { Sound = true };
        _taskCardSettingsMock.Setup(x => x.CurrentValue).Returns(settings);

        var baseTime = GetTime();

        var taskCards = new List<Domain.TaskCard>
        {
            new(new List<TriggerState>{new(cardState, baseTime)})
            {
                InstrumentNumber = "INST001",
                CardType = TaskCardType.Error
            }
        };
        taskCards.ForEach(x => x.SetCurrentCardState(GetTime(baseTime, TimeSpan.FromMinutes(1))));

        var cardTypesThatControlsAllInstruments = new List<TaskCardType>();

        // Act
        var result = _sut.GetControlSignalTowerRequests(taskCards, cardTypesThatControlsAllInstruments);

        // Assert
        result.InstrumentSpecific["INST001"].SetLight.Should().Be(expectedLightState);
    }

    [Trait("ProductRequirement", "PR_194")]
    [Trait("ModuleRequirement", "MR_4096")]
    [Theory]
    [InlineData(TaskCardState.Hidden, true, EnumSignalTowerControlSoundState.SoundOff)]
    [InlineData(TaskCardState.Information, true, EnumSignalTowerControlSoundState.SoundOff)]
    [InlineData(TaskCardState.Warning, true, EnumSignalTowerControlSoundState.SoundShortSignal)]
    [InlineData(TaskCardState.Error, true, EnumSignalTowerControlSoundState.SoundContinuosOn)]
    [InlineData(TaskCardState.FlashingRed, true, EnumSignalTowerControlSoundState.SoundContinuosOn)]
    [InlineData(TaskCardState.Hidden, false, EnumSignalTowerControlSoundState.SoundNotSet)]
    [InlineData(TaskCardState.Information, false, EnumSignalTowerControlSoundState.SoundNotSet)]
    [InlineData(TaskCardState.Warning, false, EnumSignalTowerControlSoundState.SoundNotSet)]
    [InlineData(TaskCardState.Error, false, EnumSignalTowerControlSoundState.SoundNotSet)]
    [InlineData(TaskCardState.FlashingRed, false, EnumSignalTowerControlSoundState.SoundNotSet)]
    public void GetControlSignalTowerRequests_MapsTaskCardStatesToSoundStatesCorrectly(TaskCardState cardState, bool soundEnabled, EnumSignalTowerControlSoundState expectedSoundState)
    {
        // Arrange
        var settings = new TaskCardSettings { Sound = soundEnabled };
        _taskCardSettingsMock.Setup(x => x.CurrentValue).Returns(settings);

        var baseTime = GetTime();

        var taskCards = new List<Domain.TaskCard>
        {
            new(new List<TriggerState>{new(cardState, baseTime)})
            {
                InstrumentNumber = "INST001",
                CardType = TaskCardType.Error
            }
        };
        taskCards.ForEach(x => x.SetCurrentCardState(GetTime(baseTime, TimeSpan.FromMinutes(1))));

        var cardTypesThatControlsAllInstruments = new List<TaskCardType>();

        // Act
        var result = _sut.GetControlSignalTowerRequests(taskCards, cardTypesThatControlsAllInstruments);

        // Assert
        result.InstrumentSpecific["INST001"].SetSound.Should().Be(expectedSoundState);
    }

    [Trait("ProductRequirement", "PR_194")]
    [Trait("ModuleRequirement", "MR_4096")]
    [Fact]
    public void GetControlSignalTowerRequests_WithLightSeverityUpgrade_UpgradesFromYellowToRed()
    {
        // Arrange
        var settings = new TaskCardSettings { Sound = true };
        _taskCardSettingsMock.Setup(x => x.CurrentValue).Returns(settings);

        var baseTime = GetTime();

        var taskCards = new List<Domain.TaskCard>
        {
            new(new List<TriggerState>{new(TaskCardState.Warning, baseTime)})
            {
                InstrumentNumber = "INST001",
                CardType = TaskCardType.Error
            },
            new(new List<TriggerState>{new(TaskCardState.Error, baseTime)})
            {
                InstrumentNumber = "INST001",
                CardType = TaskCardType.Error
            }
        };
        taskCards.ForEach(x => x.SetCurrentCardState(GetTime(baseTime, TimeSpan.FromMinutes(1))));

        var cardTypesThatControlsAllInstruments = new List<TaskCardType>();

        // Act
        var result = _sut.GetControlSignalTowerRequests(taskCards, cardTypesThatControlsAllInstruments);

        // Assert
        result.InstrumentSpecific["INST001"].SetLight.Should().Be(EnumSignalTowerLightState.Red);
    }

    [Trait("ProductRequirement", "PR_194")]
    [Trait("ModuleRequirement", "MR_4096")]
    [Fact]
    public void GetControlSignalTowerRequests_WithSoundSeverityUpgrade_UpgradesFromOffToContinuous()
    {
        // Arrange
        var settings = new TaskCardSettings { Sound = true };
        _taskCardSettingsMock.Setup(x => x.CurrentValue).Returns(settings);

        var baseTime = GetTime();

        var taskCards = new List<Domain.TaskCard>
        {
            new(new List<TriggerState>{new(TaskCardState.Information, baseTime)})
            {
                InstrumentNumber = "INST001",
                CardType = TaskCardType.Error
            },
            new(new List<TriggerState>{new(TaskCardState.FlashingRed, baseTime)})
            {
                InstrumentNumber = "INST001",
                CardType = TaskCardType.Error
            }
        };
        taskCards.ForEach(x => x.SetCurrentCardState(GetTime(baseTime, TimeSpan.FromMinutes(1))));

        var cardTypesThatControlsAllInstruments = new List<TaskCardType>();

        // Act
        var result = _sut.GetControlSignalTowerRequests(taskCards, cardTypesThatControlsAllInstruments);

        // Assert
        result.InstrumentSpecific["INST001"].SetSound.Should().Be(EnumSignalTowerControlSoundState.SoundContinuosOn);
    }

    [Trait("ProductRequirement", "PR_194")]
    [Trait("ModuleRequirement", "MR_4096")]
    [Fact]
    public void GetControlSignalTowerRequests_WithBothInstrumentSpecificAndAllInstruments_CreatesCorrectRequests()
    {
        // Arrange
        var settings = new TaskCardSettings { Sound = true };
        _taskCardSettingsMock.Setup(x => x.CurrentValue).Returns(settings);

        var baseTime = GetTime();

        var taskCards = new List<Domain.TaskCard>
        {
            new(new List<TriggerState>{new(TaskCardState.Warning, baseTime)})
            {
                InstrumentNumber = "INST001",
                CardType = TaskCardType.Error
            },
            new(new List<TriggerState>{new(TaskCardState.Error, baseTime)})
            {
                CardType = TaskCardType.ReagentDrawerOpen
            }
        };
        taskCards.ForEach(x => x.SetCurrentCardState(GetTime(baseTime, TimeSpan.FromMinutes(1))));

        var cardTypesThatControlsAllInstruments = new List<TaskCardType> { TaskCardType.ReagentDrawerOpen };

        // Act
        var result = _sut.GetControlSignalTowerRequests(taskCards, cardTypesThatControlsAllInstruments);

        // Assert
        result.InstrumentSpecific.Should().HaveCount(1);
        result.InstrumentSpecific["INST001"].SetLight.Should().Be(EnumSignalTowerLightState.Yellow);
        result.InstrumentSpecific["INST001"].SetSound.Should().Be(EnumSignalTowerControlSoundState.SoundShortSignal);

        result.AllInstruments.Should().NotBeNull();
        result.AllInstruments!.SetLight.Should().Be(EnumSignalTowerLightState.Red);
        result.AllInstruments.SetSound.Should().Be(EnumSignalTowerControlSoundState.SoundContinuosOn);
    }

    [Trait("ProductRequirement", "PR_194")]
    [Trait("ModuleRequirement", "MR_4096")]
    [Fact]
    public void GetControlSignalTowerRequests_WithMultipleInstruments_CreatesSeparateRequests()
    {
        // Arrange
        var settings = new TaskCardSettings { Sound = true };
        _taskCardSettingsMock.Setup(x => x.CurrentValue).Returns(settings);

        var baseTime = GetTime();

        var taskCards = new List<Domain.TaskCard>
        {
            new(new List<TriggerState>{new(TaskCardState.Warning, baseTime)})
            {
                InstrumentNumber = "INST001",
                CardType = TaskCardType.Error
            },
            new(new List<TriggerState>{new(TaskCardState.Error, baseTime)})
            {
                InstrumentNumber = "INST002",
                CardType = TaskCardType.Error
            }
        };
        taskCards.ForEach(x => x.SetCurrentCardState(GetTime(baseTime, TimeSpan.FromMinutes(1))));

        var cardTypesThatControlsAllInstruments = new List<TaskCardType>();

        // Act
        var result = _sut.GetControlSignalTowerRequests(taskCards, cardTypesThatControlsAllInstruments);

        // Assert
        result.InstrumentSpecific.Should().HaveCount(2);

        result.InstrumentSpecific["INST001"].SetLight.Should().Be(EnumSignalTowerLightState.Yellow);
        result.InstrumentSpecific["INST001"].SetSound.Should().Be(EnumSignalTowerControlSoundState.SoundShortSignal);

        result.InstrumentSpecific["INST002"].SetLight.Should().Be(EnumSignalTowerLightState.Red);
        result.InstrumentSpecific["INST002"].SetSound.Should().Be(EnumSignalTowerControlSoundState.SoundContinuosOn);
    }

    [Trait("ProductRequirement", "PR_194")]
    [Trait("ModuleRequirement", "MR_4096")]
    [Fact]
    public void GetControlSignalTowerRequests_WithInformationAndHidden_ReturnsOff()
    {
        // Arrange
        var settings = new TaskCardSettings { Sound = true };
        _taskCardSettingsMock.Setup(x => x.CurrentValue).Returns(settings);

        var baseTime = GetTime();

        var taskCards = new List<Domain.TaskCard>
        {
            new(new List<TriggerState>{new(TaskCardState.Information, baseTime)})
            {
                InstrumentNumber = "INST001",
                CardType = TaskCardType.Error
            },
            new(new List<TriggerState>{new(TaskCardState.Hidden, baseTime)})
            {
                InstrumentNumber = "INST001",
                CardType = TaskCardType.Error
            }
        };
        taskCards.ForEach(x => x.SetCurrentCardState(GetTime(baseTime, TimeSpan.FromMinutes(1))));

        var cardTypesThatControlsAllInstruments = new List<TaskCardType>();

        // Act
        var result = _sut.GetControlSignalTowerRequests(taskCards, cardTypesThatControlsAllInstruments);

        // Assert
        result.InstrumentSpecific["INST001"].SetLight.Should().Be(EnumSignalTowerLightState.Off);
    }

    [Trait("ProductRequirement", "PR_194")]
    [Trait("ModuleRequirement", "MR_4096")]
    [Fact]
    public void GetControlSignalTowerRequests_VerifiesSoundSeverityOrdering()
    {
        // Test that the sound severity ordering works correctly
        // SoundContinuous (10) > SoundShortSignal (9) > SoundOff (8) > SoundNotSet (7)

        // Arrange
        var settings = new TaskCardSettings { Sound = true };
        _taskCardSettingsMock.Setup(x => x.CurrentValue).Returns(settings);

        var baseTime = GetTime();

        var taskCards = new List<Domain.TaskCard>
        {
            new(new List<TriggerState>{new(TaskCardState.Error, baseTime)})
            {
                InstrumentNumber = "INST001",
                CardType = TaskCardType.Error
            },
            new(new List<TriggerState>{new(TaskCardState.Information, baseTime)})
            {
                InstrumentNumber = "INST001",
                CardType = TaskCardType.Error
            }
        };
        taskCards.ForEach(x => x.SetCurrentCardState(GetTime(baseTime, TimeSpan.FromMinutes(1))));

        var cardTypesThatControlsAllInstruments = new List<TaskCardType>();

        // Act
        var result = _sut.GetControlSignalTowerRequests(taskCards, cardTypesThatControlsAllInstruments);

        // Assert
        result.InstrumentSpecific["INST001"].SetSound.Should().Be(EnumSignalTowerControlSoundState.SoundContinuosOn);
    }

    [Trait("ProductRequirement", "PR_194")]
    [Trait("ModuleRequirement", "MR_4096")]
    [Fact]
    public void GetControlSignalTowerRequests_WithEscalatingTaskCard_ShowsCorrectStateBasedOnTime()
    {
        // Test that demonstrates escalation through multiple states over time
        // This test shows how a TaskCard can escalate from Information -> Warning -> Error based on time

        // Arrange
        var settings = new TaskCardSettings { Sound = true };
        _taskCardSettingsMock.Setup(x => x.CurrentValue).Returns(settings);

        var baseTime = GetTime();

        var taskCards = new List<Domain.TaskCard>
        {
            new(new List<TriggerState>
            {
                new(TaskCardState.Information, baseTime),                                  // T+0: Information
                new(TaskCardState.Warning, GetTime(baseTime, TimeSpan.FromMinutes(5))),   // T+5: Warning
                new(TaskCardState.Error, GetTime(baseTime, TimeSpan.FromMinutes(10)))     // T+10: Error
            })
            {
                InstrumentNumber = "INST001",
                CardType = TaskCardType.MaintenanceCleaning
            }
        };

        // Test at T+7 minutes - should be in Warning state
        taskCards.ForEach(x => x.SetCurrentCardState(GetTime(baseTime, TimeSpan.FromMinutes(7))));

        var cardTypesThatControlsAllInstruments = new List<TaskCardType>();

        // Act
        var result = _sut.GetControlSignalTowerRequests(taskCards, cardTypesThatControlsAllInstruments);

        // Assert
        result.InstrumentSpecific["INST001"].SetLight.Should().Be(EnumSignalTowerLightState.Yellow);
        result.InstrumentSpecific["INST001"].SetSound.Should().Be(EnumSignalTowerControlSoundState.SoundShortSignal);
    }

    [Trait("ProductRequirement", "PR_194")]
    [Trait("ModuleRequirement", "MR_4096")]
    [Fact]
    public void GetControlSignalTowerRequests_WithEscalatingTaskCard_AtErrorTime_ShowsErrorState()
    {
        // Test the same escalating TaskCard but at error time

        // Arrange
        var settings = new TaskCardSettings { Sound = true };
        _taskCardSettingsMock.Setup(x => x.CurrentValue).Returns(settings);

        var baseTime = GetTime();

        var taskCards = new List<Domain.TaskCard>
        {
            new(new List<TriggerState>
            {
                new(TaskCardState.Information, baseTime),                                  // T+0: Information
                new(TaskCardState.Warning, GetTime(baseTime, TimeSpan.FromMinutes(5))),   // T+5: Warning
                new(TaskCardState.Error, GetTime(baseTime, TimeSpan.FromMinutes(10)))     // T+10: Error
            })
            {
                InstrumentNumber = "INST001",
                CardType = TaskCardType.MaintenanceCleaning
            }
        };

        // Test at T+15 minutes - should be in Error state
        taskCards.ForEach(x => x.SetCurrentCardState(GetTime(baseTime, TimeSpan.FromMinutes(15))));

        var cardTypesThatControlsAllInstruments = new List<TaskCardType>();

        // Act
        var result = _sut.GetControlSignalTowerRequests(taskCards, cardTypesThatControlsAllInstruments);

        // Assert
        result.InstrumentSpecific["INST001"].SetLight.Should().Be(EnumSignalTowerLightState.Red);
        result.InstrumentSpecific["INST001"].SetSound.Should().Be(EnumSignalTowerControlSoundState.SoundContinuosOn);
    }

    [Fact]
    public void GetControlSignalTowerRequests_WithEscalatingTaskCard_BeforeFirstTrigger_ShowsFirstState()
    {
        // Test that if we check before any trigger time, it uses the first state

        // Arrange
        var settings = new TaskCardSettings { Sound = true };
        _taskCardSettingsMock.Setup(x => x.CurrentValue).Returns(settings);

        var baseTime = GetTime();

        var taskCards = new List<Domain.TaskCard>
        {
            new(new List<TriggerState>
            {
                new(TaskCardState.Information, GetTime(baseTime, TimeSpan.FromMinutes(5))),   // T+5: Information
                new(TaskCardState.Warning, GetTime(baseTime, TimeSpan.FromMinutes(10))),      // T+10: Warning
                new(TaskCardState.Error, GetTime(baseTime, TimeSpan.FromMinutes(15)))         // T+15: Error
            })
            {
                InstrumentNumber = "INST001",
                CardType = TaskCardType.MaintenanceCleaning
            }
        };

        // Test at T+2 minutes - before first trigger, should use first state (Information)
        taskCards.ForEach(x => x.SetCurrentCardState(GetTime(baseTime, TimeSpan.FromMinutes(2))));

        var cardTypesThatControlsAllInstruments = new List<TaskCardType>();

        // Act
        var result = _sut.GetControlSignalTowerRequests(taskCards, cardTypesThatControlsAllInstruments);

        // Assert
        result.InstrumentSpecific["INST001"].SetLight.Should().Be(EnumSignalTowerLightState.Off);
        result.InstrumentSpecific["INST001"].SetSound.Should().Be(EnumSignalTowerControlSoundState.SoundOff);
    }
}
