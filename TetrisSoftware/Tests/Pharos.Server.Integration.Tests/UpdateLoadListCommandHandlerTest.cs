using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Parameters;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.LoadList.Application.Commands.UpdateConfiguration;
using Pharos.Server.LoadList.Application.Interfaces;
using Pharos.Server.LoadList.Domain;

namespace Pharos.Server.Integration.Tests;

[Trait("Category", "Integration")]
[Collection(nameof(IntegrationTestsCollection))]
public class UpdateLoadListCommandHandlerTest(IntegrationTestsWaf factory) : IntegrationTestBase(factory)
{
    private const string _instrumentSerial = "TEST-001";
    private const string _articleNumber = "ART";

    [Fact]
    public async Task Handle_ShouldUpdateBothLevels_WhenBothValuesProvidedAndValid()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<ILoadListDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();

        var config1 = await SeedConfiguration(dbContext, 1, 100, 50);
        var config2 = await SeedConfiguration(dbContext, 2, 150, 75);

        var command = new UpdateLoadListConfigurationLevelsCommand
        {
            UpdatedConfigurationsIds = [config1.Id, config2.Id],
            RefillLevel = 200,
            TriggerLevel = 100
        };

        // Act
        await mediator.Send(command, TestContext.Current.CancellationToken);

        // Assert
        var updatedConfig1 = await dbContext.LoadListConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == config1.Id, TestContext.Current.CancellationToken);
        var updatedConfig2 = await dbContext.LoadListConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == config2.Id, TestContext.Current.CancellationToken);

        updatedConfig1.Should().NotBeNull();
        updatedConfig1!.RefillLevel.Should().Be(200);
        updatedConfig1.TriggerLevel.Should().Be(100);

        updatedConfig2.Should().NotBeNull();
        updatedConfig2!.RefillLevel.Should().Be(200);
        updatedConfig2.TriggerLevel.Should().Be(100);
    }

    [Fact]
    public async Task Handle_ShouldUpdateOnlyRefillLevel_WhenOnlyRefillLevelProvided()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<ILoadListDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();

        var config = await SeedConfiguration(dbContext, 1, 100, 50);
        var originalTriggerLevel = config.TriggerLevel;

        var command = new UpdateLoadListConfigurationLevelsCommand
        {
            UpdatedConfigurationsIds = [config.Id],
            RefillLevel = 150,
            TriggerLevel = null
        };

        // Act
        await mediator.Send(command, TestContext.Current.CancellationToken);

        // Assert
        var updatedConfig = await dbContext.LoadListConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == config.Id, TestContext.Current.CancellationToken);

        updatedConfig.Should().NotBeNull();
        updatedConfig!.RefillLevel.Should().Be(150);
        updatedConfig.TriggerLevel.Should().Be(originalTriggerLevel);
    }

    [Fact]
    public async Task Handle_ShouldUpdateOnlyTriggerLevel_WhenOnlyTriggerLevelProvided()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<ILoadListDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();

        var config = await SeedConfiguration(dbContext, 1, 100, 50);
        var originalRefillLevel = config.RefillLevel;

        var command = new UpdateLoadListConfigurationLevelsCommand
        {
            UpdatedConfigurationsIds = [config.Id],
            RefillLevel = null,
            TriggerLevel = 60
        };

        // Act
        await mediator.Send(command, TestContext.Current.CancellationToken);

        // Assert
        var updatedConfig = await dbContext.LoadListConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == config.Id, TestContext.Current.CancellationToken);

        updatedConfig.Should().NotBeNull();
        updatedConfig!.RefillLevel.Should().Be(originalRefillLevel);
        updatedConfig.TriggerLevel.Should().Be(60);
    }

    [Fact]
    public async Task Handle_ShouldThrowValidationException_WhenRefillLevelLessThanTriggerLevel()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<ILoadListDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();

        var config = await SeedConfiguration(dbContext, 1, 100, 50);

        var command = new UpdateLoadListConfigurationLevelsCommand
        {
            UpdatedConfigurationsIds = [config.Id],
            RefillLevel = 50,
            TriggerLevel = 100
        };

        // Act & Assert - FluentValidation should catch this before handler
        await Assert.ThrowsAsync<ValidationException>(async () =>
            await mediator.Send(command, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_ShouldThrowInvalidOperationException_WhenNewRefillLevelLessThanExistingTriggerLevel()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<ILoadListDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();

        var config = await SeedConfiguration(dbContext, 1, 100, 75);

        var command = new UpdateLoadListConfigurationLevelsCommand
        {
            UpdatedConfigurationsIds = [config.Id],
            RefillLevel = 50,
            TriggerLevel = null
        };

        // Act & Assert - Handler logic should catch this since validator can't check existing DB values
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await mediator.Send(command, TestContext.Current.CancellationToken));

        exception.Message.Should().Contain("Refill level");
        exception.Message.Should().Contain("cannot be less than");
        exception.Message.Should().Contain("trigger level");
    }

    [Fact]
    public async Task Handle_ShouldThrowInvalidOperationException_WhenNewTriggerLevelGreaterThanExistingRefillLevel()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<ILoadListDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();

        var config = await SeedConfiguration(dbContext, 1, 100, 50);

        var command = new UpdateLoadListConfigurationLevelsCommand
        {
            UpdatedConfigurationsIds = [config.Id],
            RefillLevel = null,
            TriggerLevel = 150
        };

        // Act & Assert - Handler logic should catch this since validator can't check existing DB values
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await mediator.Send(command, TestContext.Current.CancellationToken));

        exception.Message.Should().Contain("Trigger level");
        exception.Message.Should().Contain("cannot be greater than");
        exception.Message.Should().Contain("refill level");
    }

    [Fact]
    public async Task Handle_ShouldThrowValidationException_WhenEmptyConfigurationIdsList()
    {
        // Arrange
        var mediator = ServiceProvider.GetRequiredService<IMediator>();

        var command = new UpdateLoadListConfigurationLevelsCommand
        {
            UpdatedConfigurationsIds = new List<int>(),
            RefillLevel = 200,
            TriggerLevel = 100
        };

        // Act & Assert - FluentValidation should catch this
        await Assert.ThrowsAsync<ValidationException>(async () =>
            await mediator.Send(command, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_ShouldThrowValidationException_WhenNullConfigurationIdsList()
    {
        // Arrange
        var mediator = ServiceProvider.GetRequiredService<IMediator>();

        var command = new UpdateLoadListConfigurationLevelsCommand
        {
            UpdatedConfigurationsIds = null,
            RefillLevel = 200,
            TriggerLevel = 100
        };

        // Act & Assert - FluentValidation should catch this
        await Assert.ThrowsAsync<ValidationException>(async () =>
            await mediator.Send(command, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_ShouldNotUpdateAnyConfiguration_WhenBothLevelsAreNull()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<ILoadListDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();

        var config = await SeedConfiguration(dbContext, 1, 100, 50);
        var originalRefillLevel = config.RefillLevel;
        var originalTriggerLevel = config.TriggerLevel;

        var command = new UpdateLoadListConfigurationLevelsCommand
        {
            UpdatedConfigurationsIds = [config.Id],
            RefillLevel = null,
            TriggerLevel = null
        };

        // Act
        await mediator.Send(command, TestContext.Current.CancellationToken);

        // Assert
        var unchangedConfig = await dbContext.LoadListConfigurations.FindAsync([config.Id], TestContext.Current.CancellationToken);

        unchangedConfig.Should().NotBeNull();
        unchangedConfig!.RefillLevel.Should().Be(originalRefillLevel);
        unchangedConfig.TriggerLevel.Should().Be(originalTriggerLevel);
    }

    [Fact]
    public async Task Handle_ShouldUpdateMultipleConfigurations_WithDifferentInitialValues()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<ILoadListDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();

        var config1 = await SeedConfiguration(dbContext, 1, 100, 50);
        var config2 = await SeedConfiguration(dbContext, 2, 200, 100);
        var config3 = await SeedConfiguration(dbContext, 3, 80, 40);

        var command = new UpdateLoadListConfigurationLevelsCommand
        {
            UpdatedConfigurationsIds = [config1.Id, config2.Id, config3.Id],
            RefillLevel = 250,
            TriggerLevel = 120
        };

        // Act
        await mediator.Send(command, TestContext.Current.CancellationToken);

        // Assert
        var updatedConfigs = await dbContext.LoadListConfigurations
            .Where(c => command.UpdatedConfigurationsIds.Contains(c.Id))
            .ToListAsync(TestContext.Current.CancellationToken);

        updatedConfigs.Should().HaveCount(3);
        updatedConfigs.Should().OnlyContain(c => c.RefillLevel == 250);
        updatedConfigs.Should().OnlyContain(c => c.TriggerLevel == 120);
    }

    [Fact]
    public async Task Handle_ShouldUpdateOnlySpecifiedConfigurations_LeavingOthersUnchanged()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<ILoadListDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();

        var config1 = await SeedConfiguration(dbContext, 1, 100, 50);
        var config2 = await SeedConfiguration(dbContext, 2, 150, 75);
        var config3 = await SeedConfiguration(dbContext, 3, 200, 100);

        var command = new UpdateLoadListConfigurationLevelsCommand
        {
            UpdatedConfigurationsIds = new List<int> { config1.Id, config2.Id },
            RefillLevel = 180,
            TriggerLevel = 90
        };

        // Act
        await mediator.Send(command, TestContext.Current.CancellationToken);

        // Assert
        var updatedConfig1 = await dbContext.LoadListConfigurations.FindAsync([config1.Id], TestContext.Current.CancellationToken);
        var updatedConfig2 = await dbContext.LoadListConfigurations.FindAsync([config2.Id], TestContext.Current.CancellationToken);
        var unchangedConfig3 = await dbContext.LoadListConfigurations.FindAsync([config3.Id], TestContext.Current.CancellationToken);

        updatedConfig1!.RefillLevel.Should().Be(180);
        updatedConfig1.TriggerLevel.Should().Be(90);

        updatedConfig2!.RefillLevel.Should().Be(180);
        updatedConfig2.TriggerLevel.Should().Be(90);

        unchangedConfig3!.RefillLevel.Should().Be(200);
        unchangedConfig3.TriggerLevel.Should().Be(100);
    }

    [Fact]
    public async Task Handle_ShouldValidateEachConfiguration_WhenUpdatingRefillLevelOnly()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<ILoadListDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();

        var config1 = await SeedConfiguration(dbContext, 1, 100, 50);
        var config2 = await SeedConfiguration(dbContext, 2, 150, 90);

        var command = new UpdateLoadListConfigurationLevelsCommand
        {
            UpdatedConfigurationsIds = new List<int> { config1.Id, config2.Id },
            RefillLevel = 80,
            TriggerLevel = null
        };

        // Act & Assert - Handler logic should catch this
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await mediator.Send(command, TestContext.Current.CancellationToken));

        exception.Message.Should().Contain("Refill level 80");
        exception.Message.Should().Contain("cannot be less than existing trigger level 90");
    }

    [Fact]
    public async Task Handle_ShouldValidateEachConfiguration_WhenUpdatingTriggerLevelOnly()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<ILoadListDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();

        var config1 = await SeedConfiguration(dbContext, 1, 100, 50);
        var config2 = await SeedConfiguration(dbContext, 2, 150, 75);

        var command = new UpdateLoadListConfigurationLevelsCommand
        {
            UpdatedConfigurationsIds = new List<int> { config1.Id, config2.Id },
            RefillLevel = null,
            TriggerLevel = 120
        };

        // Act & Assert - Handler logic should catch this
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await mediator.Send(command, TestContext.Current.CancellationToken));

        exception.Message.Should().Contain("Trigger level 120");
        exception.Message.Should().Contain("cannot be greater than existing refill level 100");
    }

    [Fact]
    public async Task Handle_ShouldThrowValidationException_WhenRefillLevelExceedsMaximum()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<ILoadListDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();

        var config = await SeedConfiguration(dbContext, 1, 100, 50);

        var command = new UpdateLoadListConfigurationLevelsCommand
        {
            UpdatedConfigurationsIds = [config.Id],
            RefillLevel = 100001, // Exceeds maximum of 100000
            TriggerLevel = null
        };

        // Act & Assert - FluentValidation should catch this (InclusiveBetween(0, 100000))
        await Assert.ThrowsAsync<ValidationException>(async () =>
            await mediator.Send(command, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_ShouldThrowValidationException_WhenTriggerLevelExceedsMaximum()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<ILoadListDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();

        var config = await SeedConfiguration(dbContext, 1, 100, 50);

        var command = new UpdateLoadListConfigurationLevelsCommand
        {
            UpdatedConfigurationsIds = [config.Id],
            RefillLevel = null,
            TriggerLevel = 100001 // Exceeds maximum of 100000
        };

        // Act & Assert - FluentValidation should catch this (InclusiveBetween(0, 100000))
        await Assert.ThrowsAsync<ValidationException>(async () =>
            await mediator.Send(command, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_ShouldThrowValidationException_WhenRefillLevelIsNegative()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<ILoadListDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();

        var config = await SeedConfiguration(dbContext, 1, 100, 50);

        var command = new UpdateLoadListConfigurationLevelsCommand
        {
            UpdatedConfigurationsIds = [config.Id],
            RefillLevel = -1,
            TriggerLevel = null
        };

        // Act & Assert - FluentValidation should catch this (InclusiveBetween(0, 100000))
        await Assert.ThrowsAsync<ValidationException>(async () =>
            await mediator.Send(command, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_ShouldThrowValidationException_WhenTriggerLevelIsNegative()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<ILoadListDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();

        var config = await SeedConfiguration(dbContext, 1, 100, 50);

        var command = new UpdateLoadListConfigurationLevelsCommand
        {
            UpdatedConfigurationsIds = [config.Id],
            RefillLevel = null,
            TriggerLevel = -1
        };

        // Act & Assert - FluentValidation should catch this (InclusiveBetween(0, 100000))
        await Assert.ThrowsAsync<ValidationException>(async () =>
            await mediator.Send(command, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_ShouldAllowZeroValues_WhenWithinValidRange()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<ILoadListDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();

        var config = await SeedConfiguration(dbContext, 1, 100, 50);

        var command = new UpdateLoadListConfigurationLevelsCommand
        {
            UpdatedConfigurationsIds = [config.Id],
            RefillLevel = 0,
            TriggerLevel = 0
        };

        // Act
        await mediator.Send(command, TestContext.Current.CancellationToken);

        // Assert
        var updatedConfig = await dbContext.LoadListConfigurations.FindAsync([config.Id], TestContext.Current.CancellationToken);

        updatedConfig.Should().NotBeNull();
        updatedConfig!.RefillLevel.Should().Be(0);
        updatedConfig.TriggerLevel.Should().Be(0);
    }

    [Theory]
    [InlineData(1001, 1000)]
    [InlineData(100000, 99999)]
    public async Task Handle_ShouldAllowMaximumValues_WhenWithinValidRange(int refillLevel, int triggerLevel)
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<ILoadListDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();

        var config = await SeedConfiguration(dbContext, 1, 100, 50);

        var command = new UpdateLoadListConfigurationLevelsCommand
        {
            UpdatedConfigurationsIds = [config.Id],
            RefillLevel = refillLevel,
            TriggerLevel = triggerLevel
        };

        // Act
        await mediator.Send(command, TestContext.Current.CancellationToken);

        // Assert
        var updatedConfig = await dbContext.LoadListConfigurations.FindAsync([config.Id], TestContext.Current.CancellationToken);

        updatedConfig.Should().NotBeNull();
        updatedConfig!.RefillLevel.Should().Be(refillLevel);
        updatedConfig.TriggerLevel.Should().Be(triggerLevel);
    }

    private static async Task<LoadListConfiguration> SeedConfiguration(
        ILoadListDbContext dbContext,
        int seed,
        int refillLevel,
        int triggerLevel)
    {
        var configuration = new LoadListConfiguration
        {
            InstrumentSerial = $"{_instrumentSerial}-{seed}",
            ArticleNumber = $"{_articleNumber}",
            RefillLevel = refillLevel,
            TriggerLevel = triggerLevel,
            Quantity = 1,
            ArticleType = ArticleType.CalibratorCartridge,
            Unit = ArticleQuantityUnit.Doses,
            Shape = ArticleIndicatorShape.Slanted
        };

        dbContext.LoadListConfigurations.Add(configuration);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        ((DbContext)dbContext).Entry(configuration).State = EntityState.Detached;

        return configuration;
    }
}