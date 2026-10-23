using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Sample.Application.Commands.CreateSampleTray;
using Pharos.Server.Sample.Application.Interfaces;
using Pharos.Server.Localization;
using Pharos.Server.Sample.Domain;

namespace Pharos.Server.Sample.Tests.Application.Commands;

[Collection(nameof(SampleTestsCollection))]
public class CreateSampleTrayCommandHandlerTests(SampleTestsWaf factory) : DatabaseTestBase<SampleTestsWaf>(factory)
{
    [Fact]
    public async Task Handle_ShouldCreateAnEmptyTray()
    {
        // Arrange
        var trayId = "0001";
        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();
        var command = new CreateSampleTrayCommand { TrayId = trayId };
        var handler = new CreateSampleTrayCommandHandler(dbContext);

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(trayId);

        var tray = await dbContext.SampleTrays.FirstOrDefaultAsync(t => t.Id == trayId, TestContext.Current.CancellationToken);

        tray.Should().NotBeNull();
        tray.Id.Should().Be(trayId);
        tray.SampleTubes.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldThrowValidationException_WhenTrayAlreadyExists()
    {
        // Arrange
        var existingTrayId = "9999";
        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();
        dbContext.SampleTrays.Add(new SampleTray
        {
            Id = existingTrayId
        });

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var command = new CreateSampleTrayCommand { TrayId = existingTrayId };
        var handler = new CreateSampleTrayCommandHandler(dbContext);

        // Act
        var act = async () => await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ValidationException>().WithMessage(Translations.SampleTrayAlreadyExists);
    }
}

