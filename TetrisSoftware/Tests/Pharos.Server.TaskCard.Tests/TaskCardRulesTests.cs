using FluentAssertions;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.TaskCard.Application.Services;
using Xunit;

namespace Pharos.Server.TaskCard.Tests;

public class TaskCardRulesTests
{
    private readonly TaskCardRules _sut = new();

    [Trait("ProductRequirement", "PR_950")]
    [Fact]
    public void InstrumentSoftwareOffline_Should_BeValidForInstrumentAndShownOnLockScreen()
    {
        _sut.ValidateInstrumentNumberAndCardType("INST001", TaskCardType.InstrumentSoftwareOffline).Should().BeTrue();
        _sut.ShowOnLockScreen(TaskCardType.InstrumentSoftwareOffline).Should().BeTrue();
    }

    [Trait("ProductRequirement", "PR_950")]
    [Fact]
    public void InstrumentSoftwareOffline_Should_BeRemovedWhenInstrumentIsDeleted()
    {
        _sut.TaskCardsToRemoveWhenInstrumentIsDeleted().Should().Contain(TaskCardType.InstrumentSoftwareOffline);
    }
}
