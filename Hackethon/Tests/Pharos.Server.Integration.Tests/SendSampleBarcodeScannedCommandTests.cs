using System.Net;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Sample;
using Pharos.Server.Application.Settings.Sample;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Instrument.Application.Interfaces;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Lis.Application.Interfaces;
using Pharos.Server.Lis.Domain;
using Pharos.Server.Sample.Application.Commands.ProcessScannedTube;
using Pharos.Server.Sample.Application.Interfaces;
using Pharos.Server.Sample.Domain;
using Tetris.Protocol.Common;
using Tetris.Protocol.Pharos;

namespace Pharos.Server.Integration.Tests;

[Trait("Category", "Integration")]
[Collection(nameof(IntegrationTestsCollection))]
public class SendSampleBarcodeScannedCommandTests : IntegrationTestBase
{
    private readonly Fixture _fixture;

    public SendSampleBarcodeScannedCommandTests(IntegrationTestsWaf factory) : base(factory)
    {
        _fixture = new Fixture();
    }

    [Fact(Skip = "Skipping this one for now")]
    public async Task SendSampleBarcodeScannedCommand_Should_EnqueueTestOrderQueryLisMessage_WithCorrectSampleId()
    {
        // Arrange
        var instrument = new Instrument.Domain.Instrument
        {
            SerialNumber = _fixture.Create<string>()[..Constants.InstrumentSerialNumberLength],
            IpAddress = IPAddress.Loopback
        };

        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<IInstrumentDbContext>();
            dbContext.Instruments.Add(instrument);
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var tubeBarcode = _fixture.Create<string>()[..Constants.SampleIdMaxLength];

        // Act
        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var instrumentServiceMock = new Mock<IInstrumentManager>();
            var mediatorMock = new Mock<IMediator>();

            // Setup instrument storage with test data
            var instrumentStorage = scope.ServiceProvider.GetRequiredService<IInstrumentSampleStorage>();
            AddTrayAndTubes(instrument.SerialNumber, instrumentStorage, DrawerType.Sample1, "Tray1", [1]);
            AddTrayAndTubes(instrument.SerialNumber, instrumentStorage, DrawerType.Sample2, "Tray2", [2]);

            instrumentServiceMock.Setup(s => s.GetInstrument(instrument.SerialNumber))
                .Returns(Mock.Of<IInstrument>(instrumentInfo => instrumentInfo.IsInAssayMode));

            var samplesSettingsMock = new Mock<IOptionsSnapshot<SamplesSettings>>();
            samplesSettingsMock.Setup(x => x.Value).Returns(new SamplesSettings());

            var handler = new ProcessScannedTubeCommandHandler(
                instrumentStorage,
                scope.ServiceProvider.GetRequiredService<ISampleQcIdProvider>(),
                scope.ServiceProvider.GetRequiredService<IPublisher>(),
                instrumentServiceMock.Object, samplesSettingsMock.Object, mediatorMock.Object
            );

            await handler.Handle(new ProcessScannedTubeCommand(instrument.SerialNumber, new ScannedSampleQcRequest
            {
                TubeBarcode = tubeBarcode,
                CurrentPosition = new SampleTubePosition
                {
                    TubePosition = 1,
                    TrayBarcode = "1234"
                }
            }), TestContext.Current.CancellationToken);
        }

        //sending to LIS is happening on background thread, need to wait some time
        await WaitForNotificationsAsync();

        // Assert
        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var lisDbContext = scope.ServiceProvider.GetRequiredService<ILisDbContext>();
            var queuedMessage = await lisDbContext.QueuedLisMessages
                .FirstOrDefaultAsync(m => m.MessageType == LisMessageType.TestOrderQuery && m.MessageData.Contains(tubeBarcode), TestContext.Current.CancellationToken);

            Assert.NotNull(queuedMessage);
        }
    }

    private static void AddTrayAndTubes(string instrumentNumber, IInstrumentSampleStorage storage, DrawerType drawerType, string trayBarcode, int[] occupiedSamplePositions)
    {
        var drawer = new SampleDrawer
        {
            DrawerType = drawerType
        };
        drawer.Trays.Add(new SampleTray
        {
            Id = trayBarcode,
            TrayPosition = 1,
        });

        storage.UpdateDrawer(instrumentNumber, drawer);

        var area = GetSampleArea(drawerType);

        foreach (var occupiedSamplePosition in occupiedSamplePositions)
        {
            var tube = new SampleTube
            {
                Area = area,
                TrayPosition = 1,
                TubePosition = occupiedSamplePosition,
                InstrumentNumber = instrumentNumber,
            };
            storage.AddOrUpdateTube(tube, _ => tube);
        }
    }

    private static SampleArea GetSampleArea(DrawerType drawerType)
    {
        return drawerType switch
        {
            DrawerType.Sample1 => SampleArea.SampleDrawer1,
            DrawerType.Sample2 => SampleArea.SampleDrawer2,
            _ => throw new ArgumentOutOfRangeException(nameof(drawerType), drawerType, null)
        };
    }
}