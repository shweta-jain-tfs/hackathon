using Moq;
using Moq.EntityFrameworkCore;
using Pharos.Server.Infrastructure;
using Pharos.Server.Instrument.Application.Commands.SkipNextScheduledAssayStart;
using Pharos.Server.Instrument.Application.Interfaces;

namespace Pharos.Server.Instrument.Application.Commands;

public class SkipNextScheduledAssayStartCommandHandlerTests
{
    /// <summary>
    /// Verifies that skipping pauses scheduling until next local midnight so the rest of today's starts are skipped.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldPauseUntilNextMidnight()
    {
        var now = new DateTimeOffset(2026, 1, 12, 14, 30, 00, TimeSpan.FromHours(1));

        var scheduler = new Server.Instrument.Domain.AssayScheduler
        {
            Id = 1,
            InstrumentId = 77,
            IsActive = true,
            SchedulerDays =
            [
                new Server.Instrument.Domain.AssaySchedulerDay { Day = now.DayOfWeek, StartTime = new TimeOnly(10, 0) },
                new Server.Instrument.Domain.AssaySchedulerDay { Day = now.DayOfWeek, StartTime = new TimeOnly(16, 0) }
            ]
        };

        var dbContext = new Mock<IInstrumentDbContext>();
        dbContext.Setup(x => x.AssaySchedulers).ReturnsDbSet([scheduler]);

        var dateTime = new Mock<IDateTime>();
        dateTime.Setup(x => x.Now).Returns(now);

        var sut = new SkipNextScheduledAssayStartCommandHandler(dbContext.Object, dateTime.Object);

        await sut.Handle(new SkipNextScheduledAssayStartCommand(77), TestContext.Current.CancellationToken);

        var expectedPauseUntil = new DateTimeOffset(now.Date.AddDays(1), now.Offset);

        Assert.False(scheduler.IsActive);
        Assert.Equal(expectedPauseUntil, scheduler.PausedUntilDate);
        dbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
