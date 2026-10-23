using MediatR;
using Moq.EntityFrameworkCore;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Result;
using Pharos.Server.Infrastructure;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Commands;

public class CleanupCurrentCurvesCommandHandlerTests
{
    private readonly Mock<IDateTime> _dateTimeMock;
    private readonly Mock<IResultDbContext> _dbMock;
    private readonly Mock<IPublisher> _publisherMock;
    private readonly CleanupCurrentCurvesCommandHandler _sut;

    public CleanupCurrentCurvesCommandHandlerTests()
    {
        _dbMock = new Mock<IResultDbContext>();
        _publisherMock = new Mock<IPublisher>();
        _dateTimeMock = new Mock<IDateTime>();

        _sut = new CleanupCurrentCurvesCommandHandler(_dbMock.Object, _publisherMock.Object, _dateTimeMock.Object);
    }

    [Fact]
    public async Task Handle_WhenNoCurrentCurves_ShouldNotRemoveAnything()
    {
        // Arrange
        _dbMock.Setup(db => db.CurrentCurves).ReturnsDbSet(new List<CurrentCurve>());

        var command = new CleanupCurrentCurvesCommand();

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _dbMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _publisherMock.Verify(p => p.Publish(It.IsAny<ValidCurvesUpdatedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenCurveIsStillValid_ShouldNotRemoveIt()
    {
        // Arrange
        var now = new DateTimeOffset(2025, 11, 10, 10, 0, 0, TimeSpan.Zero);
        var measurementDate = new DateTimeOffset(2025, 10, 20, 10, 0, 0, TimeSpan.Zero); // 21 days old
        const int validityDays = 28; // Still valid

        var currentCurves = new List<CurrentCurve>
        {
            new()
            {
                CurveId = 1,
                MethodName = "sIgE",
                Curve = new Curve
                {
                    Id = 1,
                    InstrumentNumber = "SN001",
                    LastDayToUse = CalculateLastDayToUse(measurementDate, validityDays)
                }
            }
        };

        _dbMock.Setup(db => db.CurrentCurves).ReturnsDbSet(currentCurves);
        _dateTimeMock.Setup(dt => dt.Now).Returns(now);

        var command = new CleanupCurrentCurvesCommand();

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _dbMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _publisherMock.Verify(p => p.Publish(It.IsAny<ValidCurvesUpdatedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static DateOnly CalculateLastDayToUse(DateTimeOffset measurementDate, int validityDays)
    {
        return DateOnly.FromDateTime(measurementDate.AddDays(validityDays - 1).DateTime);
    }

    [Fact]
    public async Task Handle_WhenCurveIsExpired_ShouldRemoveItAndPublishEvent()
    {
        // Arrange
        var now = new DateTimeOffset(2025, 11, 10, 10, 0, 0, TimeSpan.Zero);
        var measurementDate = new DateTimeOffset(2025, 10, 5, 10, 0, 0, TimeSpan.Zero); // 36 days old
        const int validityDays = 28; // Expired

        var currentCurves = new List<CurrentCurve>
        {
            new()
            {
                CurveId = 1,
                MethodName = "sIgE",
                Curve = new Curve
                {
                    Id = 1,
                    InstrumentNumber = "SN001",
                    LastDayToUse = CalculateLastDayToUse(measurementDate, validityDays)
                }
            }
        };

        _dbMock.Setup(db => db.CurrentCurves).ReturnsDbSet(currentCurves);
        _dateTimeMock.Setup(dt => dt.Now).Returns(now);

        var command = new CleanupCurrentCurvesCommand();

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _dbMock.Verify(db => db.CurrentCurves.RemoveRange(
                It.Is<IList<CurrentCurve>>(curves =>
                    curves.Count == 1 &&
                    curves[0].CurveId == 1)),
            Times.Once);
        _dbMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _publisherMock.Verify(p => p.Publish(
            It.Is<ValidCurvesUpdatedEvent>(e => e.InstrumentNumber == "SN001"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenMultipleCurvesWithMixedValidity_ShouldOnlyRemoveExpiredOnes()
    {
        // Arrange
        var now = new DateTimeOffset(2025, 11, 10, 10, 0, 0, TimeSpan.Zero);
        var validityDays = 28;

        var currentCurves = new List<CurrentCurve>
        {
            new()
            {
                CurveId = 1,
                MethodName = "sIgE",
                Curve = new Curve
                {
                    Id = 1,
                    InstrumentNumber = "SN001",
                    LastDayToUse = CalculateLastDayToUse(new DateTimeOffset(2025, 10, 5, 10, 0, 0, TimeSpan.Zero), validityDays) // Expired (36 days old)
                }
            },
            new()
            {
                CurveId = 2,
                MethodName = "sIgE",
                Curve = new Curve
                {
                    Id = 2,
                    InstrumentNumber = "SN001",
                    LastDayToUse = CalculateLastDayToUse(new DateTimeOffset(2025, 10, 20, 10, 0, 0, TimeSpan.Zero), validityDays) // Still valid (21 days old)
                }
            }
        };

        _dbMock.Setup(db => db.CurrentCurves).ReturnsDbSet(currentCurves);
        _dateTimeMock.Setup(dt => dt.Now).Returns(now);

        var command = new CleanupCurrentCurvesCommand();

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _dbMock.Verify(db => db.CurrentCurves.RemoveRange(
                It.Is<IList<CurrentCurve>>(curves =>
                    curves.Count == 1 &&
                    curves.First().CurveId == 1)),
            Times.Once);
        _dbMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _publisherMock.Verify(p => p.Publish(
            It.Is<ValidCurvesUpdatedEvent>(e => e.InstrumentNumber == "SN001"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenMultipleExpiredCurvesForSameInstrument_ShouldPublishEventOnce()
    {
        // Arrange
        var now = new DateTimeOffset(2025, 11, 10, 10, 0, 0, TimeSpan.Zero);
        const int validityDays = 28;

        var currentCurves = new List<CurrentCurve>
        {
            new()
            {
                CurveId = 1,
                MethodName = "sIgE",
                Curve = new Curve
                {
                    Id = 1,
                    InstrumentNumber = "SN001",
                    LastDayToUse = CalculateLastDayToUse(new DateTimeOffset(2025, 10, 1, 10, 0, 0, TimeSpan.Zero), validityDays) // Expired (40 days old)
                }
            },
            new()
            {
                CurveId = 2,
                MethodName = "tIgE",
                Curve = new Curve
                {
                    Id = 2,
                    InstrumentNumber = "SN001",
                    LastDayToUse = CalculateLastDayToUse(new DateTimeOffset(2025, 10, 5, 10, 0, 0, TimeSpan.Zero), validityDays) // Expired (36 days old)
                }
            }
        };

        _dbMock.Setup(db => db.CurrentCurves).ReturnsDbSet(currentCurves);
        _dateTimeMock.Setup(dt => dt.Now).Returns(now);

        var command = new CleanupCurrentCurvesCommand();

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _dbMock.Verify(db => db.CurrentCurves.RemoveRange(
                It.Is<IList<CurrentCurve>>(curves =>
                    curves.Count == 2 &&
                    curves[0].CurveId == 1 &&
                    curves[1].CurveId == 2)),
            Times.Once);
        _dbMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _publisherMock.Verify(p => p.Publish(
            It.Is<ValidCurvesUpdatedEvent>(e => e.InstrumentNumber == "SN001"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenExpiredCurvesOnMultipleInstruments_ShouldPublishEventForEachInstrument()
    {
        // Arrange
        var now = new DateTimeOffset(2025, 11, 10, 10, 0, 0, TimeSpan.Zero);
        const int validityDays = 28;

        var currentCurves = new List<CurrentCurve>
        {
            new()
            {
                CurveId = 1,
                MethodName = "sIgE",
                Curve = new Curve
                {
                    Id = 1,
                    InstrumentNumber = "SN001",
                    LastDayToUse = CalculateLastDayToUse(new DateTimeOffset(2025, 10, 5, 10, 0, 0, TimeSpan.Zero), validityDays) // Expired (36 days old)
                }
            },
            new()
            {
                CurveId = 2,
                MethodName = "sIgE",
                Curve = new Curve
                {
                    Id = 2,
                    InstrumentNumber = "SN002",
                    LastDayToUse = CalculateLastDayToUse(new DateTimeOffset(2025, 10, 5, 10, 0, 0, TimeSpan.Zero), validityDays) // Expired (36 days old)
                }
            }
        };

        _dbMock.Setup(db => db.CurrentCurves).ReturnsDbSet(currentCurves);
        _dateTimeMock.Setup(dt => dt.Now).Returns(now);

        var command = new CleanupCurrentCurvesCommand();

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _dbMock.Verify(db => db.CurrentCurves.RemoveRange(
                It.Is<IList<CurrentCurve>>(curves =>
                    curves.Count == 2 &&
                    curves[0].CurveId == 1 &&
                    curves[1].CurveId == 2)),
            Times.Once);
        _dbMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _publisherMock.Verify(p => p.Publish(
            It.Is<ValidCurvesUpdatedEvent>(e => e.InstrumentNumber == "SN001"),
            It.IsAny<CancellationToken>()), Times.Once);
        _publisherMock.Verify(p => p.Publish(
            It.Is<ValidCurvesUpdatedEvent>(e => e.InstrumentNumber == "SN002"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCurveIsExactlyAtValidityThreshold_ShouldRemoveIt()
    {
        // Arrange
        var now = new DateTimeOffset(2025, 11, 10, 0, 0, 0, TimeSpan.Zero);

        // With 28-day validity:
        // - Day 1: 2025-10-13 (measurement day)
        // - Day 28: 2025-11-09 (last valid day)
        // - Day 29: 2025-11-10 (expiration day - NOT valid)

        // Curve was measured on 2025-10-13 at 10:00 AM
        var measurementDate = new DateTimeOffset(2025, 10, 13, 10, 0, 0, TimeSpan.Zero);
        const int validityDays = 28;

        var currentCurves = new List<CurrentCurve>
        {
            new()
            {
                CurveId = 1,
                MethodName = "sIgE",
                Curve = new Curve
                {
                    Id = 1,
                    InstrumentNumber = "SN001",
                    LastDayToUse = CalculateLastDayToUse(measurementDate, validityDays)
                }
            }
        };

        _dbMock.Setup(db => db.CurrentCurves).ReturnsDbSet(currentCurves);
        _dateTimeMock.Setup(dt => dt.Now).Returns(now);

        var command = new CleanupCurrentCurvesCommand();

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        // The curve should be removed on day 29 (2025-11-10)
        // 2025-10-13 00:00 + 28 days = 2025-11-10 00:00
        // 2025-11-10 00:00 <= 2025-11-10 00:00? TRUE - curve IS removed
        _dbMock.Verify(db => db.CurrentCurves.RemoveRange(
                It.Is<IList<CurrentCurve>>(curves =>
                    curves.Count == 1 &&
                    curves[0].CurveId == 1)),
            Times.Once);
        _dbMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _publisherMock.Verify(p => p.Publish(
            It.Is<ValidCurvesUpdatedEvent>(e => e.InstrumentNumber == "SN001"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCurveStillValidOnLastDay_ShouldNotRemoveIt()
    {
        // Arrange
        // Curve was measured on 2025-10-13 at 08:00 AM
        // With 28-day validity, the curve is valid through 2025-11-09 (end of day)
        // Now is 2025-11-09 at 23:59 (one minute before expiration)
        var now = new DateTimeOffset(2025, 11, 9, 23, 59, 0, TimeSpan.Zero);
        var measurementDate = new DateTimeOffset(2025, 10, 13, 8, 0, 0, TimeSpan.Zero);
        const int validityDays = 28;

        // 2025-10-13 00:00 + 28 days = 2025-11-10 00:00
        // 2025-11-10 00:00 <= 2025-11-09 00:00? FALSE - curve is NOT removed (still valid)

        var currentCurves = new List<CurrentCurve>
        {
            new()
            {
                CurveId = 1,
                MethodName = "sIgE",
                Curve = new Curve
                {
                    Id = 1,
                    InstrumentNumber = "SN001",
                    LastDayToUse = CalculateLastDayToUse(measurementDate, validityDays)
                }
            }
        };

        _dbMock.Setup(db => db.CurrentCurves).ReturnsDbSet(currentCurves);
        _dateTimeMock.Setup(dt => dt.Now).Returns(now);

        var command = new CleanupCurrentCurvesCommand();

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        // The curve should NOT be removed - it's still valid on its last day (2025-11-09)
        // It will expire at midnight (2025-11-10 00:00)
        _dbMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _publisherMock.Verify(p => p.Publish(It.IsAny<ValidCurvesUpdatedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}