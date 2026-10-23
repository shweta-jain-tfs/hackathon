using Moq;
using Moq.EntityFrameworkCore;
using Pharos.Server.Application;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Instrument.Application.Interfaces;
using Pharos.Server.Instrument.Domain;

namespace Pharos.Server.Instrument.Application.Queries.GetInstruments;

public class GetCurrentInstrumentQueryHandlerTests
{
    [Fact]
    [Trait("ModuleRequirement", "MR_5819")]
    public async Task Handle_ShouldReturnNextRun_WhenSchedulerIsActiveAndNotPaused()
    {
        // Arrange
        const int instrumentId = 77;
        var now = DateTimeOffset.Now;

        var instrument = CreateInstrument(instrumentId);
        var scheduler = CreateScheduler(
            instrumentId,
            isActive: true,
            pausedUntilDate: null,
            day: now.DayOfWeek,
            startTime: TimeOnly.FromDateTime(now.AddMinutes(30).DateTime));

        var stateManager = new Mock<IInstrumentManager>();
        stateManager
            .Setup(x => x.GetInstrument(instrumentId))
            .Returns(instrument);

        var currentInstrument = new Mock<ICurrentInstrumentService>();
        currentInstrument
            .Setup(x => x.InstrumentId)
            .Returns(instrumentId);

        var dbContext = new Mock<IInstrumentDbContext>();
        dbContext
            .Setup(x => x.AssaySchedulers)
            .ReturnsDbSet([scheduler]);

        var sut = new GetCurrentInstrumentQueryHandler(stateManager.Object, currentInstrument.Object, dbContext.Object);

        // Act
        var result = await sut.Handle(new GetCurrentInstrumentQuery(), TestContext.Current.CancellationToken);

        // Assert
        result.AssaySchedulerNextRunType.Should().Be(AssaySchedulerNextRunType.Start);
        result.AssaySchedulerNextRun.Should().NotBeNull();
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5819")]
    public async Task Handle_ShouldHideNextRun_WhenSchedulerIsInactive()
    {
        // Arrange
        const int instrumentId = 78;
        var now = DateTimeOffset.Now;

        var instrument = CreateInstrument(instrumentId);
        var scheduler = CreateScheduler(
            instrumentId,
            isActive: false,
            pausedUntilDate: now.AddHours(4),
            day: now.DayOfWeek,
            startTime: TimeOnly.FromDateTime(now.AddMinutes(30).DateTime));

        var stateManager = new Mock<IInstrumentManager>();
        stateManager
            .Setup(x => x.GetInstrument(instrumentId))
            .Returns(instrument);

        var currentInstrument = new Mock<ICurrentInstrumentService>();
        currentInstrument
            .Setup(x => x.InstrumentId)
            .Returns(instrumentId);

        var dbContext = new Mock<IInstrumentDbContext>();
        dbContext
            .Setup(x => x.AssaySchedulers)
            .ReturnsDbSet([scheduler]);

        var sut = new GetCurrentInstrumentQueryHandler(stateManager.Object, currentInstrument.Object, dbContext.Object);

        // Act
        var result = await sut.Handle(new GetCurrentInstrumentQuery(), TestContext.Current.CancellationToken);

        // Assert
        result.AssaySchedulerNextRunType.Should().Be(AssaySchedulerNextRunType.None);
        result.AssaySchedulerNextRun.Should().BeNull();
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5819")]
    public async Task Handle_ShouldHideNextRun_WhenSchedulerIsPausedInFuture()
    {
        // Arrange
        const int instrumentId = 79;
        var now = DateTimeOffset.Now;

        var instrument = CreateInstrument(instrumentId);
        var scheduler = CreateScheduler(
            instrumentId,
            isActive: true,
            pausedUntilDate: now.AddHours(2),
            day: now.DayOfWeek,
            startTime: TimeOnly.FromDateTime(now.AddMinutes(30).DateTime));

        var stateManager = new Mock<IInstrumentManager>();
        stateManager
            .Setup(x => x.GetInstrument(instrumentId))
            .Returns(instrument);

        var currentInstrument = new Mock<ICurrentInstrumentService>();
        currentInstrument
            .Setup(x => x.InstrumentId)
            .Returns(instrumentId);

        var dbContext = new Mock<IInstrumentDbContext>();
        dbContext
            .Setup(x => x.AssaySchedulers)
            .ReturnsDbSet([scheduler]);

        var sut = new GetCurrentInstrumentQueryHandler(stateManager.Object, currentInstrument.Object, dbContext.Object);

        // Act
        var result = await sut.Handle(new GetCurrentInstrumentQuery(), TestContext.Current.CancellationToken);

        // Assert
        result.AssaySchedulerNextRunType.Should().Be(AssaySchedulerNextRunType.None);
        result.AssaySchedulerNextRun.Should().BeNull();
    }

    private static Domain.Instrument CreateInstrument(int id)
    {
        return new Domain.Instrument
        {
            Id = id,
            DisplayName = $"Instrument {id}",
            Enabled = true,
            SerialNumber = $"S{id:000}",
            InstrumentType = InstrumentType.Unknown,
            IpAddress = System.Net.IPAddress.Loopback,
            State = InstrumentState.Standby,
            ProcessStatus = new Tetris.Protocol.Pharos.ProcessStatus.ProcessStatus()
        };
    }

    private static AssayScheduler CreateScheduler(
        int instrumentId,
        bool isActive,
        DateTimeOffset? pausedUntilDate,
        DayOfWeek day,
        TimeOnly startTime)
    {
        return new AssayScheduler
        {
            InstrumentId = instrumentId,
            IsActive = isActive,
            PausedUntilDate = pausedUntilDate,
            SchedulerDays =
            [
                new AssaySchedulerDay
                {
                    Day = day,
                    StartTime = startTime
                }
            ]
        };
    }
}
