using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Pharos.Server.Domain.Enums;
using Pharos.Server.LoadList.Application.Commands.CopyLoadListLevelsCommand;
using Pharos.Server.LoadList.Application.Commands.HandleProcessListCommand;
using Pharos.Server.LoadList.Application.Events;
using Pharos.Server.LoadList.Application.Interfaces;
using Pharos.Server.LoadList.Application.Services;
using Pharos.Server.LoadList.Domain;
using Pharos.Server.LoadList.Infrastructure.Database;

namespace Pharos.Server.LoadList.Tests.Application.Commands;

public class CopyLoadListLevelsCommandTest : IDisposable
{
    private readonly TestLoadListDbContext _dbContext;
    private readonly Mock<ILogger<CopyLoadListLevelsCommandHandler>> _loggerMock;
    private readonly Mock<IPublisher> _publisherMock = new();
    private readonly CopyLoadListLevelsCommandHandler _handler;

    private const string _sourceInstrument = "SEED_001";
    private const string _targetInstrument1 = "SEED_002";
    private const string _targetInstrument2 = "SEED_003";

    public CopyLoadListLevelsCommandTest()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        _dbContext = new TestLoadListDbContext(config);
        _dbContext.Database.EnsureCreated();

        _loggerMock = new Mock<ILogger<CopyLoadListLevelsCommandHandler>>();
        _handler = new CopyLoadListLevelsCommandHandler(_dbContext, _loggerMock.Object, _publisherMock.Object);
    }

    [Fact]
    public async Task Handle_CopiesLevelsFromSourceToTargetInstruments()
    {
        // Arrange
        await SeedConfigurations();

        var command = new CopyLoadListLevelsCommand
        {
            SourceInstrument = _sourceInstrument,
            TargetInstruments = [_targetInstrument1, _targetInstrument2]
        };

        // Act
        await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        var targetConfigs = await _dbContext.LoadListConfigurations
            .Where(c => c.InstrumentSerial == _targetInstrument1 || c.InstrumentSerial == _targetInstrument2)
            .ToListAsync(TestContext.Current.CancellationToken);

        // Verify carrier levels were copied
        var target1Carrier = targetConfigs.First(c => c is { InstrumentSerial: _targetInstrument1, ArticleType: ArticleType.ImmunoCapCarrier });
        target1Carrier.RefillLevel.Should().Be(100);
        target1Carrier.TriggerLevel.Should().Be(50);

        var target2Carrier = targetConfigs.First(c => c is { InstrumentSerial: _targetInstrument2, ArticleType: ArticleType.ImmunoCapCarrier });
        target2Carrier.RefillLevel.Should().Be(100);
        target2Carrier.TriggerLevel.Should().Be(50);

        // Verify cartridge levels were copied
        var target1Cartridge = targetConfigs.First(c => c is { InstrumentSerial: _targetInstrument1, ArticleType: ArticleType.CalibratorCartridge });
        target1Cartridge.RefillLevel.Should().Be(200);
        target1Cartridge.TriggerLevel.Should().Be(80);
    }

    [Fact]
    public async Task Handle_NoSourceConfigurations_LogsWarningAndReturns()
    {
        // Arrange
        var command = new CopyLoadListLevelsCommand
        {
            SourceInstrument = "NON_EXISTENT",
            TargetInstruments = [_targetInstrument1]
        };

        // Act
        await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v!.ToString()!.Contains("No load list configurations found for source instrument")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_NoTargetConfigurations_LogsWarningAndReturns()
    {
        // Arrange
        await SeedSourceConfigurationsOnly();

        var command = new CopyLoadListLevelsCommand
        {
            SourceInstrument = _sourceInstrument,
            TargetInstruments = ["NON_EXISTENT_1", "NON_EXISTENT_2"]
        };

        // Act
        await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v!.ToString()!.Contains("No load list configurations found for any target instruments")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_SavesChangesToDatabase()
    {
        // Arrange
        await SeedConfigurations();

        var command = new CopyLoadListLevelsCommand
        {
            SourceInstrument = _sourceInstrument,
            TargetInstruments = [_targetInstrument1]
        };

        // Act
        await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert - reload from database to verify persistence
        await using var verifyContext = new TestLoadListDbContext(new ConfigurationBuilder().AddInMemoryCollection().Build());
        await verifyContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var reloadedConfig = await _dbContext.LoadListConfigurations
            .AsNoTracking()
            .FirstAsync(c => c.InstrumentSerial == _targetInstrument1 
                && c.ArticleType == ArticleType.ImmunoCapCarrier, TestContext.Current.CancellationToken);

        reloadedConfig.RefillLevel.Should().Be(100);
        reloadedConfig.TriggerLevel.Should().Be(50);
    }

    [Fact]
    public async Task Handle_ExceptionThrown_LogsErrorAndRethrows()
    {
        // Arrange
        await _dbContext.DisposeAsync();

        var command = new CopyLoadListLevelsCommand
        {
            SourceInstrument = _sourceInstrument,
            TargetInstruments = [_targetInstrument1]
        };

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => 
            await _handler.Handle(command, TestContext.Current.CancellationToken));

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v!.ToString()!.Contains("Error while copying load list levels")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_HandlesMultipleTargetInstruments()
    {
        // Arrange
        await SeedConfigurations();

        var command = new CopyLoadListLevelsCommand
        {
            SourceInstrument = _sourceInstrument,
            TargetInstruments = [_targetInstrument1, _targetInstrument2]
        };

        // Act
        await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        var target1Configs = await _dbContext.LoadListConfigurations
            .Where(c => c.InstrumentSerial == _targetInstrument1)
            .ToListAsync(TestContext.Current.CancellationToken);

        var target2Configs = await _dbContext.LoadListConfigurations
            .Where(c => c.InstrumentSerial == _targetInstrument2)
            .ToListAsync(TestContext.Current.CancellationToken);

        target1Configs.Should().AllSatisfy(c =>
        {
            if (c.ArticleType == ArticleType.ImmunoCapCarrier)
            {
                c.RefillLevel.Should().Be(100);
                c.TriggerLevel.Should().Be(50);
            }
        });

        target2Configs.Should().AllSatisfy(c =>
        {
            if (c.ArticleType == ArticleType.ImmunoCapCarrier)
            {
                c.RefillLevel.Should().Be(100);
                c.TriggerLevel.Should().Be(50);
            }
        });
    }

    [Fact]
    public void Validator_ValidCommand_PassesValidation()
    {
        // Arrange
        var validator = new CopyLoadListLevelsCommandValidator();
        var command = new CopyLoadListLevelsCommand
        {
            SourceInstrument = _sourceInstrument,
            TargetInstruments = [_targetInstrument1, _targetInstrument2]
        };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_EmptySourceInstrument_FailsValidation()
    {
        // Arrange
        var validator = new CopyLoadListLevelsCommandValidator();
        var command = new CopyLoadListLevelsCommand
        {
            SourceInstrument = string.Empty,
            TargetInstruments = [_targetInstrument1]
        };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CopyLoadListLevelsCommand.SourceInstrument));
        result.Errors.Should().Contain(e => e.ErrorMessage == "Source instrument must be specified.");
    }

    [Fact]
    public async Task Handle_PublishesOnlyMatchedTargetIdsAfterSaving()
    {
        await SeedConfigurations();
        var unmatched = new LoadListConfiguration
        {
            InstrumentSerial = _targetInstrument1,
            ArticleNumber = "TARGET_ONLY",
            ArticleType = ArticleType.ImmunoCapCarrier,
            RefillLevel = 30,
            TriggerLevel = 10
        };
        _dbContext.LoadListConfigurations.Add(unmatched);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var expectedIds = await _dbContext.LoadListConfigurations
            .Where(c => c.InstrumentSerial != _sourceInstrument && c.Id != unmatched.Id)
            .Select(c => c.Id)
            .ToListAsync(TestContext.Current.CancellationToken);

        _publisherMock
            .Setup(p => p.Publish(It.IsAny<LoadListConfigurationUpdatedEvent>(), TestContext.Current.CancellationToken))
            .Returns(async (LoadListConfigurationUpdatedEvent notification, CancellationToken token) =>
            {
                notification.UpdatedLoadListConfigurationIds.Should().BeEquivalentTo(expectedIds);
                var saved = await _dbContext.LoadListConfigurations.AsNoTracking()
                    .Where(c => notification.UpdatedLoadListConfigurationIds.Contains(c.Id))
                    .ToListAsync(token);
                saved.Should().AllSatisfy(c =>
                {
                    c.RefillLevel.Should().Be(c.ArticleType == ArticleType.ImmunoCapCarrier ? 100 : 200);
                    c.TriggerLevel.Should().Be(c.ArticleType == ArticleType.ImmunoCapCarrier ? 50 : 80);
                });
            });

        await _handler.Handle(new CopyLoadListLevelsCommand
        {
            SourceInstrument = _sourceInstrument,
            TargetInstruments = [_targetInstrument1, _targetInstrument2]
        }, TestContext.Current.CancellationToken);

        _publisherMock.Verify(p => p.Publish(It.IsAny<LoadListConfigurationUpdatedEvent>(),
            TestContext.Current.CancellationToken), Times.Once);
        unmatched.RefillLevel.Should().Be(30);
        unmatched.TriggerLevel.Should().Be(10);
    }

    [Theory]
    [InlineData(0, 8)]
    [InlineData(120, 12)]
    public async Task Handle_RefreshesExistingLoadListItems(int plannedQuantity, int expectedQuantityToLoad)
    {
        await SeedConfigurations();
        var configs = await _dbContext.LoadListConfigurations.ToListAsync(TestContext.Current.CancellationToken);
        foreach (var config in configs)
        {
            config.Quantity = 10;
        }
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var service = new LoadListService();
        var keyService = new Mock<ILoadListKeyService>();
        keyService.Setup(k => k.GetKey(It.IsAny<LoadListConfiguration>()))
            .Returns((LoadListConfiguration c) => c.ArticleNumber);
        var eventHandler = new ApplicationStartedEventHandler(_dbContext, service, keyService.Object, _publisherMock.Object);
        var startTime = DateTimeOffset.Now.AddHours(1);
        var items = new List<LoadListCarrierDto>();
        foreach (var instrument in new[] { _targetInstrument1, _targetInstrument2 })
        {
            var item = new LoadListCarrierDto { QuantityInArticle = 10 };
            item.SetTrigger(0, 0, 20, "loaded-lot");
            if (plannedQuantity > 0)
            {
                item.SetPlanned(startTime, plannedQuantity, "planned-lot");
            }
            service.AddItemToCarrierDict(instrument, "C01", item);
            items.Add(item);
        }

        _publisherMock
            .Setup(p => p.Publish(It.IsAny<LoadListConfigurationUpdatedEvent>(), It.IsAny<CancellationToken>()))
            .Returns((LoadListConfigurationUpdatedEvent notification, CancellationToken token) =>
                eventHandler.Handle(notification, token));

        await _handler.Handle(new CopyLoadListLevelsCommand
        {
            SourceInstrument = _sourceInstrument,
            TargetInstruments = [_targetInstrument1, _targetInstrument2]
        }, TestContext.Current.CancellationToken);

        items.Should().AllSatisfy(item =>
        {
            item.Trigger.RefillLevel.Should().Be(100);
            item.Trigger.TriggerLevel.Should().Be(50);
            item.Trigger.Loaded.Should().Be(20);
            item.Trigger.Lot.Should().Be("loaded-lot");
            item.Planned.MissingQuantity.Should().Be(plannedQuantity);
            item.QuantityToLoad.Should().Be(expectedQuantityToLoad);
            item.Lot.Should().Be(plannedQuantity > 0 ? "planned-lot" : "loaded-lot");
            if (plannedQuantity > 0)
            {
                item.Planned.StartTime.Should().Be(startTime);
            }
        });
    }

    [Fact]
    public async Task Handle_NoMatchingArticles_DoesNotPublish()
    {
        await SeedSourceConfigurationsOnly();
        _dbContext.LoadListConfigurations.Add(new LoadListConfiguration
        {
            InstrumentSerial = _targetInstrument1,
            ArticleNumber = "TARGET_ONLY",
            ArticleType = ArticleType.ImmunoCapCarrier
        });
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await _handler.Handle(new CopyLoadListLevelsCommand
        {
            SourceInstrument = _sourceInstrument,
            TargetInstruments = [_targetInstrument1]
        }, TestContext.Current.CancellationToken);

        _publisherMock.Verify(p => p.Publish(It.IsAny<LoadListConfigurationUpdatedEvent>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    #region Helper Methods

    private async Task SeedConfigurations()
    {
        _dbContext.LoadListConfigurations.AddRange(
            // Source instrument
            new LoadListConfiguration
            {
                InstrumentSerial = _sourceInstrument,
                ArticleNumber = "C01",
                ArticleType = ArticleType.ImmunoCapCarrier,
                RefillLevel = 100,
                TriggerLevel = 50
            },
            new LoadListConfiguration
            {
                InstrumentSerial = _sourceInstrument,
                ArticleNumber = "AAA",
                ArticleType = ArticleType.CalibratorCartridge,
                RefillLevel = 200,
                TriggerLevel = 80
            },
            // Target instrument 1
            new LoadListConfiguration
            {
                InstrumentSerial = _targetInstrument1,
                ArticleNumber = "C01",
                ArticleType = ArticleType.ImmunoCapCarrier,
                RefillLevel = 0,
                TriggerLevel = 0
            },
            new LoadListConfiguration
            {
                InstrumentSerial = _targetInstrument1,
                ArticleNumber = "AAA",
                ArticleType = ArticleType.CalibratorCartridge,
                RefillLevel = 0,
                TriggerLevel = 0
            },
            // Target instrument 2
            new LoadListConfiguration
            {
                InstrumentSerial = _targetInstrument2,
                ArticleNumber = "C01",
                ArticleType = ArticleType.ImmunoCapCarrier,
                RefillLevel = 0,
                TriggerLevel = 0
            }
        );

        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private async Task SeedSourceConfigurationsOnly()
    {
        _dbContext.LoadListConfigurations.AddRange(
            new LoadListConfiguration
            {
                InstrumentSerial = _sourceInstrument,
                ArticleNumber = "C01",
                ArticleType = ArticleType.ImmunoCapCarrier,
                RefillLevel = 100,
                TriggerLevel = 50
            }
        );

        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    #endregion

    public void Dispose()
    {
        _dbContext?.Dispose();
        GC.SuppressFinalize(this);
    }

    private sealed class TestLoadListDbContext : LoadListDbContext
    {
        public TestLoadListDbContext(IConfiguration configuration)
            : base(configuration, null, null) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseInMemoryDatabase(Guid.NewGuid().ToString());
            }
        }
    }
}
