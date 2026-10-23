using Microsoft.Extensions.DependencyInjection;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Localization;
using Pharos.Server.Sample.Application.Interfaces;
using Pharos.Server.Sample.Application.Queries.GetSampleTrayDetails;
using Pharos.Server.Sample.Domain;

namespace Pharos.Server.Sample.Tests.Application.Queries;

[Collection(nameof(SampleTestsCollection))]
public class GetSampleTrayLocationQueryHandlerTests(SampleTestsWaf factory) : DatabaseTestBase<SampleTestsWaf>(factory)
{
    private readonly string _instrumentNumber = "S001";
    private readonly string _instrumentDisplayName = "Billy";
    private readonly string _trayId = "TR01";

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenTrayDoesNotExist()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();
        var instrumentServiceMock = new Mock<IInstrumentManager>();
        var handler = new GetSampleTrayLocationQueryHandler(instrumentServiceMock.Object, dbContext);

        var query = new GetSampleTrayLocationQuery(_trayId);

        // Act
        var result = async () => await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        await result.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldReturnNotLoadedLocation_WhenTrayIsNotLoadedInInstrument()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();
        var instrumentServiceMock = new Mock<IInstrumentManager>();

        // Add a tray to the database without an instrument number
        dbContext.SampleTrays.Add(new SampleTray
        {
            Id = _trayId,
            InstrumentNumber = null,
            Drawer = null,
            TrayPosition = null
        });
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetSampleTrayLocationQueryHandler(instrumentServiceMock.Object, dbContext);
        var query = new GetSampleTrayLocationQuery(_trayId);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(Translations.TrayLocation_NotLoaded);
        instrumentServiceMock.Verify(x => x.GetInstrument(It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("TR01", DrawerType.Sample1, 1)]
    [InlineData("TR02", DrawerType.Sample2, 2)]
    public async Task Handle_ShouldReturnTrayLocation_WhenTrayIsLoadedInInstrument(string trayId, DrawerType drawerType, int trayPosition)
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();
        var instrumentServiceMock = new Mock<IInstrumentManager>();

        instrumentServiceMock
            .Setup(x => x.FindInstrument(_instrumentNumber))
            .Returns(new TestInstrument(_instrumentNumber, InstrumentType.Medium, _instrumentDisplayName));

        // Add a tray to the database
        dbContext.SampleTrays.Add(new SampleTray
        {
            Id = trayId,
            InstrumentNumber = _instrumentNumber,
            Drawer = drawerType,
            TrayPosition = trayPosition
        });

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetSampleTrayLocationQueryHandler(instrumentServiceMock.Object, dbContext);

        // Act
        var result = await handler.Handle(new GetSampleTrayLocationQuery(trayId), TestContext.Current.CancellationToken);

        // Assert
        var drawerName = drawerType == DrawerType.Sample1 ? Translations.Drawer_LeftSampleDrawer : Translations.Drawer_RightSampleDrawer;
        var expectedLocation = $"{_instrumentDisplayName}, {drawerName}, {Translations.TrayPosition} {trayPosition}";

        result.Should().Be(expectedLocation);
    }
}
