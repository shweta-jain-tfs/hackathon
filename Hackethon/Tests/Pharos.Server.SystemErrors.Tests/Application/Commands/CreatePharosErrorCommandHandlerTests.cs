using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Moq;
using Moq.EntityFrameworkCore;
using Pharos.Server.Application.SystemErrors;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Infrastructure;
using Pharos.Server.SystemErrors.Application.Commands;
using Pharos.Server.SystemErrors.Application.Events;
using Pharos.Server.SystemErrors.Application.Interfaces;
using Pharos.Server.SystemErrors.Domain;
using Xunit;

namespace Pharos.Server.SystemErrors.Tests.Application.Commands;

public class CreatePharosErrorCommandHandlerTests
{
    private readonly Mock<ISystemErrorsDbContext> _dbContext;
    private readonly Mock<ISender> _sender;
    private readonly CreatePharosErrorCommandHandler _sut;
    private readonly List<SystemError> _systemErrors;
    private readonly DateTimeOffset _now;

    public CreatePharosErrorCommandHandlerTests()
    {
        _dbContext = new Mock<ISystemErrorsDbContext>();
        var dateTime = new Mock<IDateTime>();
        _sender = new Mock<ISender>();
        _systemErrors = [];
        _now = DateTimeOffset.UtcNow;

        _dbContext.Setup(x => x.SystemErrors).ReturnsDbSet(_systemErrors);
        var systemErrorsSetMock = Mock.Get(_dbContext.Object.SystemErrors);
        systemErrorsSetMock
            .Setup(x => x.AddAsync(It.IsAny<SystemError>(), It.IsAny<CancellationToken>()))
            .Callback<SystemError, CancellationToken>((entity, _) =>
            {
                if (!_systemErrors.Contains(entity))
                {
                    _systemErrors.Add(entity);
                }
            })
            .ReturnsAsync((EntityEntry<SystemError>)null!);

        _dbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        dateTime.Setup(x => x.Now).Returns(_now);

        _sut = new CreatePharosErrorCommandHandler(_dbContext.Object, dateTime.Object, _sender.Object);
    }

    [Fact]
    public async Task CreatesSystemError_WithCorrectProperties()
    {
        // Arrange
        var command = new CreatePharosErrorCommand(
            ErrorCode: 1000,
            Classification: ErrorClassification.Error,
            InstrumentNumber: "S-123",
            Parameters: new List<string> { "param1", "param2" }
        );

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _systemErrors.Should().HaveCount(1);
        var systemError = _systemErrors.Single();

        systemError.ErrorId.Should().NotBeEmpty();
        systemError.Code.Should().Be(1000);
        systemError.Classification.Should().Be(ErrorClassification.Error);
        systemError.SerialNumber.Should().Be("S-123");
        systemError.Parameters.Should().BeEquivalentTo(new[] { "param1", "param2" });
        systemError.Source.Should().Be(ErrorSource.Pharos);
        systemError.Count.Should().Be(1);
        systemError.Timestamp.Should().Be(_now);
        systemError.PossibleResponses.Should().ContainSingle()
            .Which.Should().Be(ResponseAction.Acknowledge);
    }

    [Fact]
    public async Task UpdatesLatestMatchingUnresolvedPharosError_WhenMultipleErrorsAreNotAllowed()
    {
        // Arrange
        var olderError = new SystemError
        {
            ErrorId = Guid.NewGuid(),
            Code = 1000,
            Classification = ErrorClassification.Warning,
            Timestamp = _now.AddMinutes(-1),
            Source = ErrorSource.Pharos,
            Count = 1,
            SerialNumber = "S-123",
            Parameters = ["old"],
            PossibleResponses = [ResponseAction.Acknowledge]
        };
        var latestError = new SystemError
        {
            ErrorId = Guid.NewGuid(),
            Code = 1000,
            Classification = ErrorClassification.Warning,
            Timestamp = _now.AddSeconds(-1),
            Source = ErrorSource.Pharos,
            Count = 1,
            SerialNumber = "S-123",
            Parameters = ["previous"],
            PossibleResponses = [ResponseAction.Acknowledge]
        };
        _systemErrors.AddRange([olderError, latestError]);
        var command = new CreatePharosErrorCommand(
            ErrorCode: 1000,
            Classification: ErrorClassification.Error,
            InstrumentNumber: "S-123",
            Parameters: ["current"],
            AllowMultiple: false);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _systemErrors.Should().HaveCount(2);
        latestError.Classification.Should().Be(ErrorClassification.Error);
        latestError.Timestamp.Should().Be(_now);
        latestError.Parameters.Should().BeEquivalentTo(["current"]);
        latestError.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<SystemErrorUpdated>()
            .Which.UpdatedEntity.Should().BeSameAs(latestError);
        olderError.Timestamp.Should().Be(_now.AddMinutes(-1));
        _dbContext.Verify(
            x => x.SystemErrors.AddAsync(It.IsAny<SystemError>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreatesMatchingPharosError_WhenMultipleErrorsAreAllowed()
    {
        // Arrange
        var existingError = new SystemError
        {
            ErrorId = Guid.NewGuid(),
            Code = 1000,
            Classification = ErrorClassification.Warning,
            Timestamp = _now.AddMinutes(-1),
            Source = ErrorSource.Pharos,
            Count = 1,
            SerialNumber = "S-123",
            PossibleResponses = [ResponseAction.Acknowledge]
        };
        _systemErrors.Add(existingError);
        var command = new CreatePharosErrorCommand(
            ErrorCode: 1000,
            Classification: ErrorClassification.Error,
            InstrumentNumber: "S-123",
            AllowMultiple: true);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _systemErrors.Should().HaveCount(2);
        _systemErrors.Should().Contain(existingError);
        _systemErrors.Single(error => error != existingError).Classification.Should().Be(ErrorClassification.Error);
        _dbContext.Verify(
            x => x.SystemErrors.AddAsync(It.IsAny<SystemError>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddsSystemErrorCreatedDomainEvent()
    {
        // Arrange
        var command = new CreatePharosErrorCommand(
            ErrorCode: 1000,
            Classification: ErrorClassification.Fatal
        );

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        var systemError = _systemErrors.Single();
        systemError.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<SystemErrorCreated>()
            .Which.NewEntity.Should().BeSameAs(systemError);
    }

    [Theory]
    [InlineData(ErrorClassification.Fatal, TaskCardState.FlashingRed)]
    [InlineData(ErrorClassification.Error, TaskCardState.Error)]
    [InlineData(ErrorClassification.Warning, TaskCardState.Warning)]
    [InlineData(ErrorClassification.Information, TaskCardState.Information)]
    public async Task SendsTaskCardCommand_WithCorrectTaskCardState(ErrorClassification classification, TaskCardState expectedState)
    {
        // Arrange
        var command = new CreatePharosErrorCommand(
            ErrorCode: 1000,
            Classification: classification,
            InstrumentNumber: "S-123"
        );

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _sender.Verify(
            x => x.Send(
                It.Is<TaskCardCommand>(c =>
                    c.CardType == TaskCardType.Error &&
                    c.InstrumentNumber == "S-123" &&
                    c.Rules.Any(r => r.CardState == expectedState)
                ),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task ExecutesOperationsInCorrectOrder()
    {
        // Arrange
        var command = new CreatePharosErrorCommand(
            ErrorCode: 1000,
            Classification: ErrorClassification.Error,
            InstrumentNumber: "S-123"
        );

        var callOrder = new List<string>();
        var testSystemErrors = new List<SystemError>();

        var dbContext = new Mock<ISystemErrorsDbContext>();
        var dateTime = new Mock<IDateTime>();
        var sender = new Mock<ISender>();

        dbContext.Setup(x => x.SystemErrors).ReturnsDbSet(testSystemErrors);
        var mockSet = Mock.Get(dbContext.Object.SystemErrors);
        mockSet
            .Setup(x => x.AddAsync(It.IsAny<SystemError>(), It.IsAny<CancellationToken>()))
            .Callback<SystemError, CancellationToken>((entity, _) =>
            {
                if (!testSystemErrors.Contains(entity))
                {
                    testSystemErrors.Add(entity);
                }

                callOrder.Add("AddAsync");
            })
            .ReturnsAsync((EntityEntry<SystemError>)null!);

        dbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("SaveChanges"))
            .ReturnsAsync(1);

        sender.Setup(x => x.Send(It.IsAny<TaskCardCommand>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("SendTaskCard"))
            .Returns(Task.CompletedTask);

        dateTime.Setup(x => x.Now).Returns(DateTimeOffset.UtcNow);

        var handler = new CreatePharosErrorCommandHandler(dbContext.Object, dateTime.Object, sender.Object);

        // Act
        await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        callOrder.Should().ContainInOrder("AddAsync", "SaveChanges", "SendTaskCard");
    }

    [Fact]
    public async Task UsesProvidedCancellationToken()
    {
        // Arrange
        var command = new CreatePharosErrorCommand(
            ErrorCode: 1000,
            Classification: ErrorClassification.Error
        );
        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        // Act
        await _sut.Handle(command, cancellationToken);

        // Assert
        _dbContext.Verify(
            x => x.SaveChangesAsync(It.Is<CancellationToken>(ct => ct == cancellationToken)),
            Times.Once
        );
        _sender.Verify(
            x => x.Send(It.IsAny<TaskCardCommand>(), It.Is<CancellationToken>(ct => ct == cancellationToken)),
            Times.Once
        );
    }
}
