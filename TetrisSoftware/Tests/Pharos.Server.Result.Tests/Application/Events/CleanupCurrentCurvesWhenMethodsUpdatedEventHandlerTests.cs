using MediatR;
using Moq.EntityFrameworkCore;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;
using System.Linq.Expressions;

namespace Pharos.Server.Result.Application.Events;

public class CleanupCurrentCurvesWhenMethodsUpdatedEventHandlerTests
{
    private readonly Mock<IResultDbContext> _dbMock;
    private readonly Mock<IMethodsService> _methodsServiceMock;
    private readonly Mock<IPublisher> _publisherMock;
    private readonly CleanupCurrentCurvesWhenMethodsUpdatedEventHandler _sut;

    public CleanupCurrentCurvesWhenMethodsUpdatedEventHandlerTests()
    {
        _dbMock = new Mock<IResultDbContext>();
        _methodsServiceMock = new Mock<IMethodsService>();
        _publisherMock = new Mock<IPublisher>();
        _sut = new CleanupCurrentCurvesWhenMethodsUpdatedEventHandler(_dbMock.Object, _methodsServiceMock.Object, _publisherMock.Object);
    }

    [Fact]
    public async Task Handle_WhenNoMethodsOfInterest_ShouldNotDeleteAnyCurrentCurvesOrPublishEvents()
    {
        // Arrange
        _methodsServiceMock
            .Setup(x => x.GetMethods(
                It.IsAny<Expression<Func<Method, bool>>>(),
                It.IsAny<Expression<Func<Method, string>>>()))
            .ReturnsAsync([]);

        _dbMock.Setup(db => db.CurrentCurves).ReturnsDbSet(new List<CurrentCurve>());

        var notification = new MethodsUpdatedEvent();

        // Act
        await _sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        _dbMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _publisherMock.Verify(p => p.Publish(It.IsAny<ValidCurvesUpdatedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenMethodsExistButNoMatchingCurrentCurves_ShouldNotDeleteAnythingOrPublishEvents()
    {
        // Arrange
        _methodsServiceMock
            .Setup(x => x.GetMethods(
                It.IsAny<Expression<Func<Method, bool>>>(),
                It.IsAny<Expression<Func<Method, string>>>()))
            .ReturnsAsync(["sIgE"]);

        _dbMock.Setup(db => db.CurrentCurves).ReturnsDbSet(new List<CurrentCurve>());

        var notification = new MethodsUpdatedEvent();

        // Act
        await _sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        _dbMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _publisherMock.Verify(p => p.Publish(It.IsAny<ValidCurvesUpdatedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenEachGroupHasOnlyOneCurve_ShouldNotDeleteAnythingOrPublishEvents()
    {
        // Arrange
        _methodsServiceMock
            .Setup(x => x.GetMethods(
                It.IsAny<Expression<Func<Method, bool>>>(),
                It.IsAny<Expression<Func<Method, string>>>()))
            .ReturnsAsync(["sIgE"]);

        var currentCurves = new List<CurrentCurve>
        {
            new()
            {
                CurveId = 1,
                MethodName = "sIgE",
                ConjugateLotNumber = "LOT001",
                Curve = new Curve { Id = 1, InstrumentNumber = "SN001", LastDayToUse = new DateOnly(2026, 04, 02) }
            }
        };

        _dbMock.Setup(db => db.CurrentCurves).ReturnsDbSet(currentCurves);

        var notification = new MethodsUpdatedEvent();

        // Act
        await _sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        _dbMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _publisherMock.Verify(p => p.Publish(It.IsAny<ValidCurvesUpdatedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTwoCurvesExistForSameGroup_ShouldDeleteOlderCurveAndPublishEvent()
    {
        // Arrange
        _methodsServiceMock
            .Setup(x => x.GetMethods(
                It.IsAny<Expression<Func<Method, bool>>>(),
                It.IsAny<Expression<Func<Method, string>>>()))
            .ReturnsAsync(["sIgE"]);

        var olderCurve = new CurrentCurve
        {
            CurveId = 1,
            MethodName = "sIgE",
            ConjugateLotNumber = "LOT001",
            Curve = new Curve { Id = 1, InstrumentNumber = "SN001", LastDayToUse = new DateOnly(2026, 04, 01) }
        };
        var newerCurve = new CurrentCurve
        {
            CurveId = 2,
            MethodName = "sIgE",
            ConjugateLotNumber = "LOT001",
            Curve = new Curve { Id = 2, InstrumentNumber = "SN001", LastDayToUse = new DateOnly(2026, 04, 02) }
        };

        _dbMock.Setup(db => db.CurrentCurves).ReturnsDbSet(new List<CurrentCurve> { olderCurve, newerCurve });

        var notification = new MethodsUpdatedEvent();

        // Act
        await _sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        _dbMock.Verify(db => db.CurrentCurves.RemoveRange(
                It.Is<IList<CurrentCurve>>(curves =>
                    curves.Count == 1 &&
                    curves[0].CurveId == olderCurve.CurveId)),
            Times.Once);
        _dbMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _publisherMock.Verify(p => p.Publish(
            It.Is<ValidCurvesUpdatedEvent>(e => e.InstrumentNumber == "SN001"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenThreeCurvesInSameGroup_ShouldKeepNewestAndDeleteOtherTwo()
    {
        // Arrange
        _methodsServiceMock
            .Setup(x => x.GetMethods(
                It.IsAny<Expression<Func<Method, bool>>>(),
                It.IsAny<Expression<Func<Method, string>>>()))
            .ReturnsAsync(["sIgE"]);

        var oldestCurve = new CurrentCurve
        {
            CurveId = 1,
            MethodName = "sIgE",
            ConjugateLotNumber = "LOT001",
            Curve = new Curve { Id = 1, InstrumentNumber = "SN001", LastDayToUse = new DateOnly(2026, 04, 01) }
        };
        var middleCurve = new CurrentCurve
        {
            CurveId = 2,
            MethodName = "sIgE",
            ConjugateLotNumber = "LOT001",
            Curve = new Curve { Id = 2, InstrumentNumber = "SN001", LastDayToUse = new DateOnly(2026, 04, 02) }
        };
        var newestCurve = new CurrentCurve
        {
            CurveId = 3,
            MethodName = "sIgE",
            ConjugateLotNumber = "LOT001",
            Curve = new Curve { Id = 3, InstrumentNumber = "SN001", LastDayToUse = new DateOnly(2026, 04, 03) }
        };

        _dbMock.Setup(db => db.CurrentCurves).ReturnsDbSet(new List<CurrentCurve> { oldestCurve, middleCurve, newestCurve });

        var notification = new MethodsUpdatedEvent();

        // Act
        await _sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        _dbMock.Verify(db => db.CurrentCurves.RemoveRange(
                It.Is<IList<CurrentCurve>>(curves =>
                    curves.Count == 2 &&
                    curves.All(c => c.CurveId != newestCurve.CurveId))),
            Times.Once);
        _dbMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _publisherMock.Verify(p => p.Publish(
            It.Is<ValidCurvesUpdatedEvent>(e => e.InstrumentNumber == "SN001"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenDuplicateCurvesExistForMultipleInstruments_ShouldPublishOneEventPerInstrument()
    {
        // Arrange
        _methodsServiceMock
            .Setup(x => x.GetMethods(
                It.IsAny<Expression<Func<Method, bool>>>(),
                It.IsAny<Expression<Func<Method, string>>>()))
            .ReturnsAsync(["sIgE"]);

        var currentCurves = new List<CurrentCurve>
        {
            new()
            {
                CurveId = 1,
                MethodName = "sIgE",
                ConjugateLotNumber = "LOT001",
                Curve = new Curve { Id = 1, InstrumentNumber = "SN001", LastDayToUse = new DateOnly(2026, 04, 01) }
            },
            new()
            {
                CurveId = 2,
                MethodName = "sIgE",
                ConjugateLotNumber = "LOT001",
                Curve = new Curve { Id = 2, InstrumentNumber = "SN001", LastDayToUse = new DateOnly(2026, 04, 02) }
            },
            new()
            {
                CurveId = 3,
                MethodName = "sIgE",
                ConjugateLotNumber = "LOT001",
                Curve = new Curve { Id = 3, InstrumentNumber = "SN002", LastDayToUse = new DateOnly(2026, 04, 01) }
            },
            new()
            {
                CurveId = 4,
                MethodName = "sIgE",
                ConjugateLotNumber = "LOT001",
                Curve = new Curve { Id = 4, InstrumentNumber = "SN002", LastDayToUse = new DateOnly(2026, 04, 02) }
            }
        };

        _dbMock.Setup(db => db.CurrentCurves).ReturnsDbSet(currentCurves);

        var notification = new MethodsUpdatedEvent();

        // Act
        await _sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        _publisherMock.Verify(p => p.Publish(
            It.Is<ValidCurvesUpdatedEvent>(e => e.InstrumentNumber == "SN001"),
            It.IsAny<CancellationToken>()), Times.Once);
        _publisherMock.Verify(p => p.Publish(
            It.Is<ValidCurvesUpdatedEvent>(e => e.InstrumentNumber == "SN002"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCurvesForSameMethodHaveDifferentConjugateLotNumbers_ShouldNotDeleteThem()
    {
        // Arrange
        _methodsServiceMock
            .Setup(x => x.GetMethods(
                It.IsAny<Expression<Func<Method, bool>>>(),
                It.IsAny<Expression<Func<Method, string>>>()))
            .ReturnsAsync(["sIgE"]);

        var currentCurves = new List<CurrentCurve>
        {
            new()
            {
                CurveId = 1,
                MethodName = "sIgE",
                ConjugateLotNumber = "LOT001",
                Curve = new Curve { Id = 1, InstrumentNumber = "SN001", LastDayToUse = new DateOnly(2026, 04, 02) }
            },
            new()
            {
                CurveId = 2,
                MethodName = "sIgE",
                ConjugateLotNumber = "LOT002",
                Curve = new Curve { Id = 2, InstrumentNumber = "SN001", LastDayToUse = new DateOnly(2026, 04, 02) }
            }
        };

        _dbMock.Setup(db => db.CurrentCurves).ReturnsDbSet(currentCurves);

        var notification = new MethodsUpdatedEvent();

        // Act
        await _sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        _dbMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _publisherMock.Verify(p => p.Publish(It.IsAny<ValidCurvesUpdatedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}