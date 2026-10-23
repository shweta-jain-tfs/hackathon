using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Pharos.Contracts.Notifications;
using Pharos.Server.Application.AuditTrail;
using Pharos.Server.Application.AuditTrail.Enums;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Request;
using Pharos.Server.Application.Settings.Sample;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Domain.Entities;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Sample.Application.Commands.UpdateSampleTrayPosition;
using Pharos.Server.Sample.Application.Interfaces;
using Pharos.Server.Sample.Application.Services;
using Pharos.Server.Sample.Domain;

namespace Pharos.Server.Sample.Tests.Application.Commands;

[Collection(nameof(SampleTestsCollection))]
public class UpdateSampleTrayPositionCommandHandlerTests(SampleTestsWaf factory) : DatabaseTestBase<SampleTestsWaf>(factory)
{
    private readonly string _instrumentNumber = "SEED_01";
    private readonly string _instrumentNumber2 = "SEED_02";
    private readonly string _trayId = "1111";
    private readonly string _trayId2 = "2222";
    private readonly int _tubePosition = 1;
    private readonly int _tubePosition2 = 2;


    #region Add new tube | No Duplicate Tube Scenarios

    /// <summary>
    /// Tests the scenario where no existing tube is present at the position and no barcode is provided.
    /// </summary>
    /// <returns>
    /// Result: Create New sample tube
    /// </returns>
    [Fact]
    public async Task Handle_ShouldAddNewSampleTubeWithoutBarcode_WhenNoExistingTubeAndNoBarcodeProvided()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();

        await RemoveIfExists(dbContext, new List<string> { _trayId });

        // Create an empty tray (no existing tubes)
        dbContext.SampleTrays.Add(new SampleTray
        {
            Id = _trayId,
            Drawer = DrawerType.Sample1,
            InstrumentNumber = _instrumentNumber,
            SampleTubes = new List<SampleTube>()
        });

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handlerSetup = CreateHandler(dbContext);
        handlerSetup.Mediator
            .Setup(x => x.Send(It.IsAny<GetRequestIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RequestIdResponseDto(1, DateTimeOffset.UtcNow, true));
        var handler = handlerSetup.Handler;

        var command = new UpdateSampleTrayPositionCommand
        {
            TrayId = _trayId,
            TubePosition = _tubePosition,
            TubeBarcode = null,
            HighPriority = false
        };

        // Act
        await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        var sampleTray = await dbContext.SampleTrays.Include(x => x.SampleTubes).FirstAsync(x => x.Id == _trayId, TestContext.Current.CancellationToken);
        sampleTray.SampleTubes.Should().HaveCount(1);

        var addedTube = sampleTray.SampleTubes.First();
        addedTube.TubePosition.Should().Be(_tubePosition);
        addedTube.TrayId.Should().Be(_trayId);
        addedTube.InstrumentNumber.Should().Be(_instrumentNumber);
        addedTube.TubeBarcode.Should().BeNull();
        addedTube.TubeId.Should().BeNull();
        addedTube.SampleId.Should().BeNull();
        addedTube.SampleIdSuffix.Should().BeNull();
        addedTube.HighPriority.Should().Be(false);
        addedTube.Status.Should().Be(SampleTubeStatus.None);
        addedTube.IdentitySource.Should().Be(SampleTubeIdentitySource.BarcodeScanning);

    }

    /// <summary>
    /// Tests the scenario where no existing tube is present at the position and barcode is provided But no duplicate tube with Provided Barcode is Present
    /// </summary>
    /// <returns>
    /// Result: Create New sample tube
    /// </returns>
    [Fact]
    public async Task Handle_ShouldAddNewSampleTubeWithBarcode_WhenNoExistingTubeAndBarcodeProvidedAndNoDuplicateTubewithProvidedbarcodePresent()
    {
        // Arrange
        var tubeBarcode = "NEW_BCODE_1";
        var tubeId = "NEW_TUID_1";
        var highPriority = true;

        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();

        await RemoveIfExists(dbContext, new List<string> { _trayId });

        dbContext.SampleTrays.Add(new SampleTray
        {
            Id = _trayId,
            Drawer = DrawerType.Sample1,
            InstrumentNumber = _instrumentNumber,
            SampleTubes = new List<SampleTube>()
        });

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handlerSetup = CreateHandler(dbContext);
        handlerSetup.SampleQcIdProvider
            .Setup(x => x.GetSampleQcTubeKey(tubeBarcode))
            .ReturnsAsync(CreateSampleQcTubeKey(tubeBarcode, tubeId));

        var handler = handlerSetup.Handler;
        var mediator = handlerSetup.Mediator;

        // Mock that no tube exists in storage with the provided tube ID (no duplicate in closed drawer)

        var command = new UpdateSampleTrayPositionCommand
        {
            TrayId = _trayId,
            TubePosition = _tubePosition,
            TubeBarcode = tubeBarcode,
            HighPriority = highPriority
        };

        // Act
        await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        var sampleTray = await dbContext.SampleTrays.Include(x => x.SampleTubes).FirstAsync(x => x.Id == _trayId, TestContext.Current.CancellationToken);
        sampleTray.SampleTubes.Should().HaveCount(1);

        var addedTube = sampleTray.SampleTubes.First();
        addedTube.TubePosition.Should().Be(_tubePosition);
        addedTube.TrayId.Should().Be(_trayId);
        addedTube.TubeBarcode.Should().Be(tubeBarcode);
        addedTube.HighPriority.Should().Be(highPriority);
        addedTube.Status.Should().Be(SampleTubeStatus.InProgress);
        addedTube.IdentitySource.Should().Be(SampleTubeIdentitySource.ManualInput);

        // Verify that an audit trail command was sent with specific parameters
        mediator.Verify(x => x.Send(
            It.Is<CreateAuditTrailRecordCommand>(cmd =>
                cmd.EventId == EventId.ManualEntry &&
                cmd.EventData.Any(d =>
                    d.Data.ContainsKey(EventDataFields.InstrumentSerialNumber.ToString()) &&
                    d.Data[EventDataFields.InstrumentSerialNumber.ToString()].ToString() == _instrumentNumber) &&
                cmd.EventData.Any(d =>
                    d.Data.ContainsKey(EventDataFields.TrayId.ToString()) &&
                    d.Data[EventDataFields.TrayId.ToString()].ToString() == _trayId) &&
                cmd.EventData.Any(d =>
                    d.Data.ContainsKey(EventDataFields.TubePosition.ToString()) &&
                    d.Data[EventDataFields.TubePosition.ToString()].ToString() == _tubePosition.ToString()) &&
                cmd.EventData.Any(d =>
                    d.Data.ContainsKey(EventDataFields.TubeBarcode.ToString()) &&
                    d.Data[EventDataFields.TubeBarcode.ToString()].ToString() == tubeBarcode)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(TestOrderState.Pipetted, SampleTubeStatus.Completed)]
    [InlineData(TestOrderState.NotStarted, SampleTubeStatus.PartlyCompleted)]
    public async Task Handle_ShouldSetCompletionStatus_WhenManuallyEnteringTubeWithExistingTestOrders(
        TestOrderState secondOrderState, SampleTubeStatus expectedStatus)
    {
        // Arrange
        const string tubeBarcode = "MANUAL_TUBE";
        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();
        await RemoveIfExists(dbContext, new List<string> { _trayId });

        dbContext.SampleTrays.Add(new SampleTray
        {
            Id = _trayId,
            Drawer = DrawerType.Sample1,
            InstrumentNumber = _instrumentNumber,
            SampleTubes = new List<SampleTube>()
        });
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handlerSetup = CreateHandler(dbContext);
        handlerSetup.Mediator
            .Setup(x => x.Send(It.IsAny<GetTestOrderStatesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, GetTestOrderStatesQueryResultItem>
            {
                [tubeBarcode] = new(new RequestUid { SampleId = tubeBarcode, RequestCreatedDate = DateTimeOffset.UtcNow },
                [
                    Mock.Of<ITestOrder>(order => order.State == TestOrderState.Pipetted),
                    Mock.Of<ITestOrder>(order => order.State == secondOrderState)
                ])
            });

        var command = new UpdateSampleTrayPositionCommand
        {
            TrayId = _trayId,
            TubePosition = _tubePosition,
            TubeBarcode = tubeBarcode
        };

        // Act
        await handlerSetup.Handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        var tube = await dbContext.SampleTubes.SingleAsync(
            x => x.TrayId == _trayId && x.TubePosition == _tubePosition, TestContext.Current.CancellationToken);
        tube.Status.Should().Be(expectedStatus);
    }

    #endregion


    #region Update existing Tube scenarios | Duplicate/No Duplicate Tube Scenarios

    /// <summary>
    /// Compares the properties of existing and incoming tubes and determines the action to be taken.
    /// </summary>
    /// <remarks>
    /// Existing Tube: Barcode: Null | High Priority: TRUE | Identity Source: Not Identified
    /// Incoming Tube: Barcode: Null | High Priority: FALSE | Identity Source: None
    /// </remarks>
    /// <returns>
    /// Result: Remove existing tube
    /// </returns>
    [Fact]
    public async Task UpdateSampleTrayPosition_ShouldRemoveTube_ExistingTubeNoBarcode_HasHighPriority_RequestNoBarcodeNoPriority()
    {
        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();

        await RemoveIfExists(dbContext, new List<string> { _trayId });

        dbContext.SampleTrays.Add(new SampleTray
        {
            Id = _trayId,
            Drawer = DrawerType.Sample1,
            InstrumentNumber = _instrumentNumber,
            SampleTubes = new List<SampleTube>
            {
                new() {
                    TubePosition = _tubePosition,
                    TubeBarcode = null, // No barcode
                    HighPriority = true, // Has high priority
                    Status = SampleTubeStatus.NotIdentified,
                    IdentitySource = SampleTubeIdentitySource.BarcodeScanning,
                    TrayId = _trayId,
                    InstrumentNumber = _instrumentNumber
                }
            }
        });

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handlerSetup = CreateHandler(dbContext);
        handlerSetup.Mediator
            .Setup(x => x.Send(It.IsAny<GetRequestIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RequestIdResponseDto(1, DateTimeOffset.UtcNow, true));
        var handler = handlerSetup.Handler;
        var command = new UpdateSampleTrayPositionCommand
        {
            TrayId = _trayId,
            TubePosition = _tubePosition,
            TubeBarcode = null, // No barcode provided
            HighPriority = false // No high priority set
        };

        // Act
        await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        var sampleTray = await dbContext.SampleTrays.Include(x => x.SampleTubes).FirstAsync(x => x.Id == _trayId, TestContext.Current.CancellationToken);
        sampleTray.SampleTubes.Should().BeEmpty(); // Tube should be removed
    }


    /// <summary>
    /// Compares the properties of existing and incoming tubes and determines the action to be taken.
    /// </summary>
    /// <remarks>
    /// Scenario 1:
    /// Existing Tube: Barcode: Null | High Priority: TRUE | Identity Source: Not Identified
    /// Incoming Tube: Barcode: NEW_BCODE_123 | High Priority: FALSE | Identity Source: Manual Input
    /// Result: Remove existing tube and add new tube
    /// Scenario 2:
    /// Existing Tube: Barcode: Null | High Priority: TRUE | Identity Source: Not Identified
    /// Incoming Tube: Barcode: NEW_BCODE_123 | High Priority: TRUE | Identity Source: Manual Input
    /// Result: Remove existing tube and add new tube
    /// </remarks>
    /// <returns>
    /// Result: Remove existing tube and add new tube with incoming tube data
    /// </returns>
    [Fact]
    public async Task UpdateSampleTrayPosition_ShouldReplaceWithNewTube_WhenExistingTubeNoBarcode_HasHighPriority_RequestHasBarcode()
    {
        var tubeBarcode = "NEW_BCODE_123";
        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();

        await RemoveIfExists(dbContext, new List<string> { _trayId });

        dbContext.SampleTrays.Add(new SampleTray
        {
            Id = _trayId,
            Drawer = DrawerType.Sample1,
            InstrumentNumber = _instrumentNumber,
            SampleTubes = new List<SampleTube>
            {
                new() {
                    TubePosition = _tubePosition,
                    TubeBarcode = null, // No barcode
                    HighPriority = true, // Has high priority
                    Status = SampleTubeStatus.NotIdentified,
                    IdentitySource = SampleTubeIdentitySource.BarcodeScanning,
                    TrayId = _trayId,
                    InstrumentNumber = _instrumentNumber
                }
            }
        });

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handlerSetup = CreateHandler(dbContext);
        handlerSetup.SampleQcIdProvider
            .Setup(x => x.GetSampleQcTubeKey(tubeBarcode))
            .ReturnsAsync(CreateSampleQcTubeKey(tubeBarcode, tubeBarcode));

        var handler = handlerSetup.Handler;
        var command = new UpdateSampleTrayPositionCommand
        {
            TrayId = _trayId,
            TubePosition = _tubePosition,
            TubeBarcode = tubeBarcode, // Barcode provided
            HighPriority = true // Can be true or false for this scenario
        };

        // Act
        await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        var sampleTray = await dbContext.SampleTrays.Include(x => x.SampleTubes).FirstAsync(x => x.Id == _trayId, TestContext.Current.CancellationToken);
        sampleTray.SampleTubes.Should().HaveCount(1);

        var newTube = sampleTray.SampleTubes.First();
        newTube.TubeBarcode.Should().Be(tubeBarcode);
        newTube.Status.Should().Be(SampleTubeStatus.InProgress);
        newTube.IdentitySource.Should().Be(SampleTubeIdentitySource.ManualInput);
    }


    /// <summary>
    /// Compares the properties of existing and incoming tubes and determines the action to be taken.
    /// </summary>
    /// <remarks>
    /// Existing Tube: Barcode: EXIST_BCODE_123 | High Priority: FALSE | Identity Source: Manual Input
    /// Incoming Tube: Barcode: EXIST_BCODE_123 | High Priority: TRUE | Identity Source: Manual Input
    /// </remarks>
    /// <returns>
    /// Result: Update existing tube with incoming tube data
    /// </returns>
    [Fact]
    public async Task UpdateSampleTrayPosition_ShouldUpdatePriority_WhenExistingTubeHasBarcode_NoHighPriority_SameBarcode_SetHighPriority()
    {
        var tubeBarcode = "EXIST_BCODE_123";
        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();

        await RemoveIfExists(dbContext, new List<string> { _trayId });

        dbContext.SampleTrays.Add(new SampleTray
        {
            Id = _trayId,
            Drawer = DrawerType.Sample1,
            InstrumentNumber = _instrumentNumber,
            SampleTubes = new List<SampleTube>
            {
                new() {
                    TubePosition = _tubePosition,
                    TubeBarcode = tubeBarcode, // Has barcode
                    HighPriority = false, // No high priority
                    Status = SampleTubeStatus.InProgress,
                    IdentitySource = SampleTubeIdentitySource.ConfirmManualInput,
                    TrayId = _trayId,
                    InstrumentNumber = _instrumentNumber,
                    TubeId = tubeBarcode,
                    SampleId = tubeBarcode
                }
            }
        });

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handlerSetup = CreateHandler(dbContext);
        handlerSetup.SampleQcIdProvider
            .Setup(x => x.GetSampleQcTubeKey(tubeBarcode))
            .ReturnsAsync(CreateSampleQcTubeKey(tubeBarcode, tubeBarcode));

        var handler = handlerSetup.Handler;
        var command = new UpdateSampleTrayPositionCommand
        {
            TrayId = _trayId,
            TubePosition = _tubePosition,
            TubeBarcode = tubeBarcode, // Same barcode
            HighPriority = true // Set high priority
        };

        // Act
        await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        var sampleTray = await dbContext.SampleTrays.Include(x => x.SampleTubes).FirstAsync(x => x.Id == _trayId, TestContext.Current.CancellationToken);
        sampleTray.SampleTubes.Should().HaveCount(1);

        var updatedTube = sampleTray.SampleTubes.First();
        updatedTube.HighPriority.Should().Be(true); // Priority updated
        updatedTube.IdentitySource.Should().Be(SampleTubeIdentitySource.ManualInput); // Reset from ConfirmManualInput
        updatedTube.TubeBarcode.Should().Be(tubeBarcode); // Same barcode
    }


    /// <summary>
    /// Compares the properties of existing and incoming tubes and determines the action to be taken.
    /// </summary>
    /// <remarks>
    /// Existing Tube: Barcode: EXIST_BCODE_456 | High Priority: FALSE | Identity Source: Manual Input
    /// Incoming Tube: Barcode: NULL/EMPTY | High Priority: TRUE | Identity Source: Not Identified
    /// </remarks>
    /// <returns>
    /// Result: Unmap existing tube and add new tube with incoming tube data
    /// </returns>
    [Fact]
    public async Task UpdateSampleTrayPosition_ShouldReplaceWithNewTube_WhenExistingTubeHasBarcode_NoHighPriority_NoBarcode_SetHighPriority()
    {
        // Arrange - Row 5: existingTubeHasBarcode && !existingTubeHasHighPriority && !hasBarcodeProvided && hasHighPrioritySet
        var existingBarcode = "EXIST_BCODE_456";
        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();

        await RemoveIfExists(dbContext, new List<string> { _trayId });

        dbContext.SampleTrays.Add(new SampleTray
        {
            Id = _trayId,
            Drawer = DrawerType.Sample1,
            InstrumentNumber = _instrumentNumber,
            SampleTubes = new List<SampleTube>
            {
                new() {
                    TubePosition = _tubePosition,
                    TubeBarcode = existingBarcode, // Has barcode
                    HighPriority = false, // No high priority
                    Status = SampleTubeStatus.InProgress,
                    IdentitySource = SampleTubeIdentitySource.ManualInput,
                    TrayId = _trayId,
                    InstrumentNumber = _instrumentNumber,
                    TubeId = existingBarcode,
                    SampleId = existingBarcode
                }
            }
        });

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handlerSetup = CreateHandler(dbContext);
        handlerSetup.Mediator
            .Setup(x => x.Send(It.IsAny<GetRequestIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RequestIdResponseDto(1, DateTimeOffset.UtcNow, true));
        var handler = handlerSetup.Handler;
        var command = new UpdateSampleTrayPositionCommand
        {
            TrayId = _trayId,
            TubePosition = _tubePosition,
            TubeBarcode = null, // No barcode provided
            HighPriority = true // Set high priority
        };

        // Act
        await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        var sampleTray = await dbContext.SampleTrays.Include(x => x.SampleTubes).FirstAsync(x => x.Id == _trayId, TestContext.Current.CancellationToken);
        sampleTray.SampleTubes.Should().HaveCount(1);

        var newTube = sampleTray.SampleTubes.First();
        newTube.TubeBarcode.Should().BeNull(); // No barcode
        newTube.HighPriority.Should().Be(true); // High priority set
        newTube.Status.Should().Be(SampleTubeStatus.NotIdentified); // Waiting for identification
        newTube.IdentitySource.Should().Be(SampleTubeIdentitySource.BarcodeScanning);

        // Original tube should be unmapped (but still exists in database with null position)
        var allTubes = await dbContext.SampleTubes.ToListAsync(TestContext.Current.CancellationToken);
        var unmappedTube = allTubes.FirstOrDefault(t => t.TubeBarcode == existingBarcode);
        unmappedTube.Should().NotBeNull();
        unmappedTube.TubePosition.Should().BeNull();
        unmappedTube.TrayId.Should().BeNull();

        handlerSetup.Mediator.Verify(x => x.Send(
            It.IsAny<CreateAuditTrailRecordCommand>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }


    /// <summary>
    /// Compares the properties of existing and incoming tubes and determines the action to be taken.
    /// </summary>
    /// <remarks>
    /// Scenario 1:
    /// Existing Tube: Barcode: EXIST_BCODE_789 | High Priority: FALSE | Identity Source: Manual Input
    /// Incoming Tube: Barcode: NEW_BCODE_789 | High Priority: FALSE | Identity Source: Manual Input
    /// Result: Unmap existing tube and add new tube with incoming tube data
    ///
    /// Scenario 2:
    /// Existing Tube: Barcode: EXIST_BCODE_789 | High Priority: FALSE | Identity Source: Manual Input
    /// Incoming Tube: Barcode: NEW_BCODE_789 | High Priority: TRUE | Identity Source: Manual Input
    /// Result: Remove existing tube and add new tube
    /// </remarks>
    /// <returns>
    /// Result: Unmap existing tube and add new tube with incoming tube data
    /// </returns>
    [Fact]
    public async Task UpdateSampleTrayPosition_ShouldReplaceWithNewTube_WhenExistingTubeHasBarcode_NoHighPriority_DifferentBarcode()
    {
        var existingBarcode = "EXIST_BCODE_789";
        var newBarcode = "NEW_BCODE_789";
        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();

        await RemoveIfExists(dbContext, new List<string> { _trayId });

        dbContext.SampleTrays.Add(new SampleTray
        {
            Id = _trayId,
            Drawer = DrawerType.Sample1,
            InstrumentNumber = _instrumentNumber,
            SampleTubes = new List<SampleTube>
            {
                new() {
                    TubePosition = _tubePosition,
                    TubeBarcode = existingBarcode, // No barcode
                    HighPriority = false, // No high priority
                    Status = SampleTubeStatus.InProgress,
                    IdentitySource = SampleTubeIdentitySource.ManualInput,
                    TrayId = _trayId,
                    InstrumentNumber = _instrumentNumber,
                    TubeId = existingBarcode,
                    SampleId = existingBarcode,
                    RequestCreatedDate = DateTimeOffset.UtcNow
                }
            }
        });

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handlerSetup = CreateHandler(dbContext);
        handlerSetup.SampleQcIdProvider
            .Setup(x => x.GetSampleQcTubeKey(newBarcode))
            .ReturnsAsync(CreateSampleQcTubeKey(newBarcode, newBarcode));
        handlerSetup.Mediator
            .Setup(x => x.Send(It.IsAny<GetRequestIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RequestIdResponseDto(1, DateTimeOffset.UtcNow, true));

        var handler = handlerSetup.Handler;
        var mediator = handlerSetup.Mediator;
        var command = new UpdateSampleTrayPositionCommand
        {
            TrayId = _trayId,
            TubePosition = _tubePosition,
            TubeBarcode = newBarcode, // Different barcode
            HighPriority = false
        };

        // Act
        await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        var sampleTray = await dbContext.SampleTrays.Include(x => x.SampleTubes).FirstAsync(x => x.Id == _trayId, TestContext.Current.CancellationToken);
        sampleTray.SampleTubes.Should().HaveCount(1);

        var newTube = sampleTray.SampleTubes.First();
        newTube.TubeBarcode.Should().Be(newBarcode); // New barcode
        newTube.Status.Should().Be(SampleTubeStatus.InProgress);
        newTube.IdentitySource.Should().Be(SampleTubeIdentitySource.ManualInput);

        // Original tube should be unmapped (but still exists in database with null position)
        var allTubes = await dbContext.SampleTubes.ToListAsync(TestContext.Current.CancellationToken);
        var unmappedTube = allTubes.FirstOrDefault(t => t.TubeBarcode == existingBarcode);
        unmappedTube.Should().NotBeNull();
        unmappedTube.TubePosition.Should().BeNull();
        unmappedTube.TrayId.Should().BeNull();

        mediator.Verify(x => x.Send(
           It.Is<CreateAuditTrailRecordCommand>(cmd =>
               cmd.EventId == EventId.ManualEntry &&
               cmd.EventData.Any(d =>
                   d.Data.ContainsKey(EventDataFields.InstrumentSerialNumber.ToString()) &&
                   d.Data[EventDataFields.InstrumentSerialNumber.ToString()].ToString() == _instrumentNumber) &&
               cmd.EventData.Any(d =>
                   d.Data.ContainsKey(EventDataFields.TrayId.ToString()) &&
                   d.Data[EventDataFields.TrayId.ToString()].ToString() == _trayId) &&
               cmd.EventData.Any(d =>
                   d.Data.ContainsKey(EventDataFields.TubePosition.ToString()) &&
                   d.Data[EventDataFields.TubePosition.ToString()].ToString() == _tubePosition.ToString()) &&
               cmd.EventData.Any(d =>
                   d.Data.ContainsKey(EventDataFields.TubeBarcode.ToString()) &&
                   d.Data[EventDataFields.TubeBarcode.ToString()].ToString() == newBarcode)),
           It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Compares the properties of existing and incoming tubes and determines the action to be taken.
    /// </summary>
    /// <remarks>
    /// Existing Tube: Barcode: Null | High Priority: FALSE | Identity Source: Manual Input
    /// Incoming Tube: Barcode: Null/EMPTY | High Priority: FALSE | Identity Source: None
    /// </remarks>
    /// <returns>
    /// Result: Unmap existing tube
    /// </returns>
    [Fact]
    public async Task UpdateSampleTrayPosition_ShouldUnmapTube_WhenExistingTubeHasBarcode_NoHighPriority_NoBarcode_NoHighPriority()
    {
        var existingBarcode = "EXISTING_BARCODE_UNMAP";
        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();

        await RemoveIfExists(dbContext, new List<string> { _trayId });

        dbContext.SampleTrays.Add(new SampleTray
        {
            Id = _trayId,
            Drawer = DrawerType.Sample1,
            InstrumentNumber = _instrumentNumber,
            SampleTubes = new List<SampleTube>
            {
                new() {
                    TubePosition = _tubePosition,
                    TubeBarcode = existingBarcode, // Has barcode
                    HighPriority = false, // No high priority
                    Status = SampleTubeStatus.InProgress,
                    IdentitySource = SampleTubeIdentitySource.ManualInput,
                    TrayId = _trayId,
                    InstrumentNumber = _instrumentNumber,
                    TubeId = existingBarcode,
                    SampleId = existingBarcode,
                    RequestCreatedDate = DateTimeOffset.UtcNow
                }
            }
        });

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handlerSetup = CreateHandler(dbContext);
        handlerSetup.Mediator
            .Setup(x => x.Send(It.IsAny<GetRequestIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RequestIdResponseDto(1, DateTimeOffset.UtcNow, true));
        var handler = handlerSetup.Handler;
        var command = new UpdateSampleTrayPositionCommand
        {
            TrayId = _trayId,
            TubePosition = _tubePosition,
            TubeBarcode = null, // No barcode provided
            HighPriority = false // No high priority set
        };

        // Act
        await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        var sampleTray = await dbContext.SampleTrays.Include(x => x.SampleTubes).FirstAsync(x => x.Id == _trayId, TestContext.Current.CancellationToken);
        sampleTray.SampleTubes.Should().BeEmpty(); // No tubes in tray

        // Original tube should be unmapped (but still exists in database with null position)
        var allTubes = await dbContext.SampleTubes.ToListAsync(TestContext.Current.CancellationToken);
        var unmappedTube = allTubes.FirstOrDefault(t => t.TubeBarcode == existingBarcode);
        unmappedTube.Should().NotBeNull();
        unmappedTube.TubePosition.Should().BeNull();
        unmappedTube.TrayId.Should().BeNull();
    }

    [Fact]
    public async Task UpdateSampleTrayPosition_ShouldUpdatePriorityAndResetIdentitySource_WhenBarcodeUnchanged()
    {
        // Arrange - Fall-through case: !isBarcodeChanged
        var tubeBarcode = "UNCHANGED_BARCODE";
        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();

        await RemoveIfExists(dbContext, new List<string> { _trayId });

        dbContext.SampleTrays.Add(new SampleTray
        {
            Id = _trayId,
            Drawer = DrawerType.Sample1,
            InstrumentNumber = _instrumentNumber,
            SampleTubes = new List<SampleTube>
            {
                new() {
                    TubePosition = _tubePosition,
                    TubeBarcode = tubeBarcode,
                    HighPriority = false,
                    Status = SampleTubeStatus.InProgress,
                    IdentitySource = SampleTubeIdentitySource.ConfirmManualInput, // Will be reset
                    TrayId = _trayId,
                    InstrumentNumber = _instrumentNumber,
                    TubeId = tubeBarcode,
                    SampleId = tubeBarcode
                }
            }
        });

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handlerSetup = CreateHandler(dbContext);
        handlerSetup.SampleQcIdProvider
            .Setup(x => x.GetSampleQcTubeKey(tubeBarcode))
            .ReturnsAsync(CreateSampleQcTubeKey(tubeBarcode));

        var handler = handlerSetup.Handler;
        var command = new UpdateSampleTrayPositionCommand
        {
            TrayId = _trayId,
            TubePosition = _tubePosition,
            TubeBarcode = tubeBarcode, // Same barcode
            HighPriority = true // Change priority
        };

        // Act
        await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        var sampleTray = await dbContext.SampleTrays.Include(x => x.SampleTubes).FirstAsync(x => x.Id == _trayId, TestContext.Current.CancellationToken);
        sampleTray.SampleTubes.Should().HaveCount(1);

        var updatedTube = sampleTray.SampleTubes.First();
        updatedTube.HighPriority.Should().Be(true); // Priority updated
        updatedTube.IdentitySource.Should().Be(SampleTubeIdentitySource.ManualInput); // Reset from ConfirmManualInput
        updatedTube.TubeBarcode.Should().Be(tubeBarcode); // Same barcode
    }

    [Fact]
    public async Task Handle_ShouldNotPublishTubePriorityChangedEvent_WhenPriorityUnchanged()
    {
        var tubeBarcode = "NO_EVENT_TUBE";
        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();

        await RemoveIfExists(dbContext, new List<string> { _trayId });

        dbContext.SampleTrays.Add(new SampleTray
        {
            Id = _trayId,
            Drawer = DrawerType.Sample1,
            InstrumentNumber = _instrumentNumber,
            SampleTubes = new List<SampleTube>
            {
                new()
                {
                    TubePosition = _tubePosition,
                    TubeBarcode = tubeBarcode,
                    HighPriority = true,
                    Status = SampleTubeStatus.InProgress,
                    IdentitySource = SampleTubeIdentitySource.ManualInput,
                    TrayId = _trayId,
                    InstrumentNumber = _instrumentNumber,
                    TubeId = tubeBarcode,
                    SampleId = tubeBarcode
                }
            }
        });

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handlerSetup = CreateHandler(dbContext);
        handlerSetup.SampleQcIdProvider
            .Setup(x => x.GetSampleQcTubeKey(tubeBarcode))
            .ReturnsAsync(CreateSampleQcTubeKey(tubeBarcode));
        handlerSetup.Mediator
            .Setup(x => x.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = handlerSetup.Handler;
        var command = new UpdateSampleTrayPositionCommand
        {
            TrayId = _trayId,
            TubePosition = _tubePosition,
            TubeBarcode = tubeBarcode,
            HighPriority = true
        };

        await handler.Handle(command, TestContext.Current.CancellationToken);

        var sampleTray = await dbContext.SampleTrays.Include(x => x.SampleTubes).FirstAsync(x => x.Id == _trayId, TestContext.Current.CancellationToken);
        sampleTray.SampleTubes.Should().HaveCount(1);
        sampleTray.SampleTubes.First().HighPriority.Should().BeTrue();

        handlerSetup.Mediator.Verify(x => x.Publish(
                It.IsAny<Pharos.Server.Application.Sample.TubePriorityChangedEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldPublishTubePriorityChangedEvent_WhenAddingNewTubeWithHighPriority()
    {
        // Arrange
        var tubeBarcode = "NEW_PRIORITY_TUBE";
        var tubeId = "NEW_PRIORITY_TUBE_ID";
        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();

        await RemoveIfExists(dbContext, new List<string> { _trayId });

        // Create tray without any tubes at the position
        dbContext.SampleTrays.Add(new SampleTray
        {
            Id = _trayId,
            Drawer = DrawerType.Sample1,
            InstrumentNumber = null,
            SampleTubes = new List<SampleTube>()
        });

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handlerSetup = CreateHandler(dbContext);
        handlerSetup.SampleQcIdProvider
            .Setup(x => x.GetSampleQcTubeKey(tubeBarcode))
            .ReturnsAsync(CreateSampleQcTubeKey(tubeBarcode, tubeId));

        var handler = handlerSetup.Handler;
        var command = new UpdateSampleTrayPositionCommand
        {
            TrayId = _trayId,
            TubePosition = _tubePosition,
            TubeBarcode = tubeBarcode,
            HighPriority = true
        };

        // Act
        await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        var sampleTray = await dbContext.SampleTrays.Include(x => x.SampleTubes).FirstAsync(x => x.Id == _trayId, TestContext.Current.CancellationToken);
        sampleTray.SampleTubes.Should().HaveCount(1);
        sampleTray.SampleTubes.First().HighPriority.Should().BeTrue();

        // Verify that TubePriorityChangedEvent was published with instrumentNumber = null (tube didn't exist)
        handlerSetup.Mediator.Verify(x => x.Publish(
                It.Is<Pharos.Server.Application.Sample.TubePriorityChangedEvent>(e =>
                    e.InstrumentNumber == null &&
                    e.SampleTubeId.TubeId == tubeId &&
                    e.HighPriority),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldPublishTubePriorityChangedEvent_WhenChangingExistingTubePriorityFromFalseToTrue()
    {
        // Arrange
        var tubeBarcode = "PRIORITY_CHANGE_TUBE";
        var tubeId = "PRIORITY_CHANGE_TUBE_ID";
        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();

        await RemoveIfExists(dbContext, new List<string> { _trayId });

        // Create tray with existing tube at position with HighPriority = false
        dbContext.SampleTrays.Add(new SampleTray
        {
            Id = _trayId,
            Drawer = DrawerType.Sample1,
            InstrumentNumber = _instrumentNumber,
            SampleTubes = new List<SampleTube>
            {
                new()
                {
                    TubePosition = _tubePosition,
                    TubeBarcode = tubeBarcode,
                    TubeId = tubeId,
                    SampleId = tubeBarcode,
                    HighPriority = false,
                    Status = SampleTubeStatus.InProgress,
                    IdentitySource = SampleTubeIdentitySource.ManualInput,
                    TrayId = _trayId,
                    InstrumentNumber = _instrumentNumber
                }
            }
        });

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handlerSetup = CreateHandler(dbContext);
        handlerSetup.SampleQcIdProvider
            .Setup(x => x.GetSampleQcTubeKey(tubeBarcode))
            .ReturnsAsync(CreateSampleQcTubeKey(tubeBarcode, tubeId));

        var handler = handlerSetup.Handler;
        var command = new UpdateSampleTrayPositionCommand
        {
            TrayId = _trayId,
            TubePosition = _tubePosition,
            TubeBarcode = tubeBarcode,
            HighPriority = true
        };

        // Act
        await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        var sampleTray = await dbContext.SampleTrays.Include(x => x.SampleTubes).FirstAsync(x => x.Id == _trayId, TestContext.Current.CancellationToken);
        sampleTray.SampleTubes.Should().HaveCount(1);
        sampleTray.SampleTubes.First().HighPriority.Should().BeTrue();

        // Verify that TubePriorityChangedEvent was published with instrumentNumber from existing tube
        handlerSetup.Mediator.Verify(x => x.Publish(
                It.Is<Pharos.Server.Application.Sample.TubePriorityChangedEvent>(e =>
                    e.InstrumentNumber == _instrumentNumber &&
                    e.SampleTubeId.TubeId == tubeId &&
                    e.HighPriority),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldPublishTubePriorityChangedEvent_WhenChangingExistingTubePriorityFromTrueToFalse()
    {
        // Arrange
        var tubeBarcode = "PRIORITY_CHANGE_TUBE_2";
        var tubeId = "PRIORITY_CHANGE_TUBE_ID_2";
        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();

        await RemoveIfExists(dbContext, new List<string> { _trayId });

        // Create tray with existing tube at position with HighPriority = true
        dbContext.SampleTrays.Add(new SampleTray
        {
            Id = _trayId,
            Drawer = DrawerType.Sample1,
            InstrumentNumber = _instrumentNumber,
            SampleTubes = new List<SampleTube>
            {
                new()
                {
                    TubePosition = _tubePosition,
                    TubeBarcode = tubeBarcode,
                    TubeId = tubeId,
                    SampleId = tubeBarcode,
                    HighPriority = true,
                    Status = SampleTubeStatus.InProgress,
                    IdentitySource = SampleTubeIdentitySource.ManualInput,
                    TrayId = _trayId,
                    InstrumentNumber = _instrumentNumber
                }
            }
        });

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handlerSetup = CreateHandler(dbContext);
        handlerSetup.SampleQcIdProvider
            .Setup(x => x.GetSampleQcTubeKey(tubeBarcode))
            .ReturnsAsync(CreateSampleQcTubeKey(tubeBarcode, tubeId));

        var handler = handlerSetup.Handler;
        var command = new UpdateSampleTrayPositionCommand
        {
            TrayId = _trayId,
            TubePosition = _tubePosition,
            TubeBarcode = tubeBarcode,
            HighPriority = false
        };

        // Act
        await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        var sampleTray = await dbContext.SampleTrays.Include(x => x.SampleTubes).FirstAsync(x => x.Id == _trayId, TestContext.Current.CancellationToken);
        sampleTray.SampleTubes.Should().HaveCount(1);
        sampleTray.SampleTubes.First().HighPriority.Should().BeFalse();

        // Verify that TubePriorityChangedEvent was published with instrumentNumber from existing tube
        handlerSetup.Mediator.Verify(x => x.Publish(
                It.Is<Pharos.Server.Application.Sample.TubePriorityChangedEvent>(e =>
                    e.InstrumentNumber == _instrumentNumber &&
                    e.SampleTubeId.TubeId == tubeId &&
                    !e.HighPriority),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldNotPublishTubePriorityChangedEvent_WhenBarcodeIsEmpty()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();

        await RemoveIfExists(dbContext, new List<string> { _trayId });

        // Create tray without any tubes at the position
        dbContext.SampleTrays.Add(new SampleTray
        {
            Id = _trayId,
            Drawer = DrawerType.Sample1,
            InstrumentNumber = _instrumentNumber,
            SampleTubes = new List<SampleTube>()
        });

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handlerSetup = CreateHandler(dbContext);
        handlerSetup.Mediator
            .Setup(x => x.Send(It.IsAny<GetRequestIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RequestIdResponseDto(1, DateTimeOffset.UtcNow, true));

        var handler = handlerSetup.Handler;
        var command = new UpdateSampleTrayPositionCommand
        {
            TrayId = _trayId,
            TubePosition = _tubePosition,
            TubeBarcode = null, // No barcode
            HighPriority = true
        };

        // Act
        await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        var sampleTray = await dbContext.SampleTrays.Include(x => x.SampleTubes).FirstAsync(x => x.Id == _trayId, TestContext.Current.CancellationToken);
        sampleTray.SampleTubes.Should().HaveCount(1);

        // Verify that TubePriorityChangedEvent was NOT published when barcode is empty
        handlerSetup.Mediator.Verify(x => x.Publish(
                It.IsAny<Pharos.Server.Application.Sample.TubePriorityChangedEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateSampleTrayPosition_ShouldSetNotIdentifiedStatus_WhenHighPrioritySetWithoutBarcode()
    {
        var tubeBarcode = "EXISTING_BARCODE_SPECIAL";
        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();

        await RemoveIfExists(dbContext, new List<string> { _trayId });

        dbContext.SampleTrays.Add(new SampleTray
        {
            Id = _trayId,
            Drawer = DrawerType.Sample1,
            InstrumentNumber = _instrumentNumber,
            SampleTubes = new List<SampleTube>
            {
                new() {
                    TubePosition = _tubePosition,
                    TubeBarcode = tubeBarcode,
                    HighPriority = false,
                    Status = SampleTubeStatus.InProgress,
                    IdentitySource = SampleTubeIdentitySource.ManualInput,
                    TrayId = _trayId,
                    InstrumentNumber = _instrumentNumber,
                    TubeId = tubeBarcode,
                    SampleId = tubeBarcode,
                    RequestCreatedDate = DateTimeOffset.UtcNow
                }
            }
        });

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(dbContext).Handler;
        var command = new UpdateSampleTrayPositionCommand
        {
            TrayId = _trayId,
            TubePosition = _tubePosition,
            TubeBarcode = null, // No barcode provided
            HighPriority = true // High priority set
        };

        // Act
        await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        var sampleTray = await dbContext.SampleTrays.Include(x => x.SampleTubes).FirstAsync(x => x.Id == _trayId, TestContext.Current.CancellationToken);
        sampleTray.SampleTubes.Should().HaveCount(1);

        var updatedTube = sampleTray.SampleTubes.First();
        updatedTube.HighPriority.Should().Be(true);
        updatedTube.Status.Should().Be(SampleTubeStatus.NotIdentified);
        updatedTube.IdentitySource.Should().Be(SampleTubeIdentitySource.BarcodeScanning);
    }

    [Fact]
    public async Task UpdateSampleTrayPosition_ShouldReplaceExistingTube_WhenBarcodeChanged()
    {
        // Arrange - Fall-through case: isBarcodeChanged
        var existingBarcode = "OLD_BARCODE";
        var newBarcode = "NEW_BARCODE";
        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();

        await RemoveIfExists(dbContext, new List<string> { _trayId });

        dbContext.SampleTrays.Add(new SampleTray
        {
            Id = _trayId,
            Drawer = DrawerType.Sample1,
            InstrumentNumber = _instrumentNumber,
            SampleTubes = new List<SampleTube>
            {
                new() {
                    TubePosition = _tubePosition,
                    TubeBarcode = existingBarcode,
                    HighPriority = true, // Has high priority
                    Status = SampleTubeStatus.InProgress,
                    IdentitySource = SampleTubeIdentitySource.ManualInput,
                    TrayId = _trayId,
                    InstrumentNumber = _instrumentNumber,
                    TubeId = existingBarcode,
                    SampleId = existingBarcode,
                    RequestCreatedDate = DateTimeOffset.UtcNow
                }
            }
        });

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handlerSetup = CreateHandler(dbContext);
        handlerSetup.SampleQcIdProvider
            .Setup(x => x.GetSampleQcTubeKey(newBarcode))
            .ReturnsAsync(CreateSampleQcTubeKey(newBarcode));
        handlerSetup.Mediator
            .Setup(x => x.Send(It.IsAny<GetRequestIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RequestIdResponseDto(-1, DateTimeOffset.UtcNow, false));

        var handler = handlerSetup.Handler;
        var command = new UpdateSampleTrayPositionCommand
        {
            TrayId = _trayId,
            TubePosition = _tubePosition,
            TubeBarcode = newBarcode, // Different barcode
            HighPriority = false
        };

        // Act
        await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        var sampleTray = await dbContext.SampleTrays.Include(x => x.SampleTubes).FirstAsync(x => x.Id == _trayId, TestContext.Current.CancellationToken);
        sampleTray.SampleTubes.Should().HaveCount(1);

        var newTube = sampleTray.SampleTubes.First();
        newTube.TubeBarcode.Should().Be(newBarcode); // New barcode
        newTube.Status.Should().Be(SampleTubeStatus.InProgress);
        newTube.IdentitySource.Should().Be(SampleTubeIdentitySource.ManualInput);
        newTube.HighPriority.Should().Be(false);

        var originalTubeExists = await dbContext.SampleTubes.AnyAsync(x => x.TubeBarcode == existingBarcode, TestContext.Current.CancellationToken);
        originalTubeExists.Should().BeFalse();

    }

    #endregion


    #region Dynamic removal of tube | Duplicate tube scenarios

    [Fact]
    public async Task Handle_ShouldRelocateTubeWithinSameTray_WhenDuplicateTubeExistsInSameTray()
    {
        // Arrange
        var tubeBarcode = "DUPLICATE_TUBE_01";
        var tubeId = "TUBE_01";

        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();

        // Clean up existing data for this test to avoid conflicts
        await RemoveIfExists(dbContext, new List<string> { _trayId });

        dbContext.SampleTrays.Add(new SampleTray
        {
            Id = _trayId,
            Drawer = DrawerType.Sample1,
            InstrumentNumber = _instrumentNumber,
            SampleTubes = new List<SampleTube>
            {
                // Existing tube at position 2
                new() {
                    TubePosition = _tubePosition2,
                    TubeBarcode = tubeBarcode,
                    TubeId = tubeId,
                    SampleId = tubeId,
                    TrayId = _trayId,
                    HighPriority = false,
                    Status = SampleTubeStatus.InProgress,
                    IdentitySource = SampleTubeIdentitySource.ManualInput
                }
            }
        });

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handlerSetup = CreateHandler(dbContext);
        handlerSetup.SampleQcIdProvider
            .Setup(x => x.GetSampleQcTubeKey(tubeBarcode))
            .ReturnsAsync(CreateSampleQcTubeKey(tubeBarcode, tubeId));

        // Mock drawer state as open to allow relocation
        handlerSetup.Instrument.Setup(x => x.DrawersLockState[DrawerType.Sample1])
            .Returns(new DrawerLockState(DrawerType.Sample1, LockState.Open, false));

        var handler = handlerSetup.Handler;
        var command = new UpdateSampleTrayPositionCommand
        {
            TrayId = _trayId,
            TubePosition = _tubePosition, // Moving to position 1
            TubeBarcode = tubeBarcode,
            HighPriority = true
        };

        // Act
        await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        var sampleTray = await dbContext.SampleTrays.Include(x => x.SampleTubes).FirstAsync(x => x.Id == _trayId, TestContext.Current.CancellationToken);

        var relocatedTube = sampleTray.SampleTubes.First();// x => x.IsTubePositionCleared == false
        relocatedTube.TubePosition.Should().Be(_tubePosition); // Should be at new position
        relocatedTube.TubeId.Should().Be(tubeId);
        relocatedTube.TubeBarcode.Should().Be(tubeBarcode);
        relocatedTube.HighPriority.Should().Be(true);
        relocatedTube.TrayId.Should().Be(_trayId);

        // Verify hub notification includes affected tray
        handlerSetup.SampleTrayHub.Verify(x => x.PositionStatusUpdated(It.Is<List<string>>(list =>
            list.Contains(_trayId))), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldRelocateTubeBetweenTraysInSameDrawer_WhenDuplicateTubeExistsInDifferentTray()
    {
        // Unmap the duplicate tube and add new tube
        // Arrange
        var tubeBarcode = "DUPLICATE_TUBE_02";
        var tubeId = "TUBE_02";

        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();

        await RemoveIfExists(dbContext, new List<string> { _trayId, _trayId2 });

        dbContext.SampleTrays.AddRange(
        [
            new SampleTray
            {
                Id = _trayId,
                Drawer = DrawerType.Sample1,
                InstrumentNumber = _instrumentNumber,
                SampleTubes = new List<SampleTube>()
            },
            new SampleTray
            {
                Id = _trayId2,
                Drawer = DrawerType.Sample1,
                InstrumentNumber = _instrumentNumber,
                SampleTubes = new List<SampleTube>
                {
                    // Existing tube in different tray
                    new() {
                        TubePosition = _tubePosition,
                        TubeBarcode = tubeBarcode,
                        TubeId = tubeId,
                        SampleId = tubeId,
                        TrayId = _trayId2,
                        HighPriority = false,
                        Status = SampleTubeStatus.InProgress,
                        IdentitySource = SampleTubeIdentitySource.ManualInput
                    }
                }
            }
        ]);

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handlerSetup = CreateHandler(dbContext);
        handlerSetup.SampleQcIdProvider
            .Setup(x => x.GetSampleQcTubeKey(tubeBarcode))
            .ReturnsAsync(CreateSampleQcTubeKey(tubeBarcode, tubeId));

        // Mock drawer state as open to allow relocation
        handlerSetup.Instrument.Setup(x => x.DrawersLockState[DrawerType.Sample1])
            .Returns(new DrawerLockState(DrawerType.Sample1, LockState.Open, false));

        var handler = handlerSetup.Handler;
        var command = new UpdateSampleTrayPositionCommand
        {
            TrayId = _trayId,
            TubePosition = _tubePosition2,
            TubeBarcode = tubeBarcode,
            HighPriority = true
        };

        // Act
        await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        await dbContext.SampleTrays.Include(x => x.SampleTubes).FirstAsync(x => x.Id == _trayId2, TestContext.Current.CancellationToken);
        var targetTray = await dbContext.SampleTrays.Include(x => x.SampleTubes).FirstAsync(x => x.Id == _trayId, TestContext.Current.CancellationToken);

        //originalTray.SampleTubes.Should().BeEmpty(); // Tube removed from original tray
        //targetTray.SampleTubes.Should().HaveCount(1); // Tube relocated to target tray

        var relocatedTube = targetTray.SampleTubes.First();// x => x.IsTubePositionCleared == false
        relocatedTube.TubePosition.Should().Be(_tubePosition2);
        relocatedTube.TubeId.Should().Be(tubeId);
        relocatedTube.TrayId.Should().Be(_trayId);
        relocatedTube.HighPriority.Should().Be(true);

        // Verify hub notification includes both affected trays
        handlerSetup.SampleTrayHub.Verify(x => x.PositionStatusUpdated(It.Is<List<string>>(list =>
            list.Contains(_trayId) && list.Contains(_trayId2))), Times.Once);

    }

    [Fact]
    public async Task Handle_ShouldRelocateTubeBetweenSiblingDrawers_WhenDuplicateTubeExistsInSiblingDrawer()
    {
        // Arrange
        var tubeBarcode = "DUPLICATE_TUBE_03";
        var tubeId = "TUBE_03";

        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();

        await RemoveIfExists(dbContext, new List<string> { _trayId, _trayId2 });

        dbContext.SampleTrays.AddRange(
        [
            new SampleTray
            {
                Id = _trayId,
                Drawer = DrawerType.Sample1, // Target drawer
                InstrumentNumber = _instrumentNumber,
                SampleTubes = new List<SampleTube>()
            },
            new SampleTray
            {
                Id = _trayId2,
                Drawer = DrawerType.Sample2, // Sibling drawer
                InstrumentNumber = _instrumentNumber,
                SampleTubes = new List<SampleTube>
                {
                    // Existing tube in sibling drawer
                    new() {
                        TubePosition = _tubePosition,
                        TubeBarcode = tubeBarcode,
                        TubeId = tubeId,
                        SampleId = tubeId,
                        TrayId = _trayId2,
                        InstrumentNumber = _instrumentNumber,
                        HighPriority = false,
                        Status = SampleTubeStatus.InProgress,
                        IdentitySource = SampleTubeIdentitySource.ManualInput
                    }
                }
            }
        ]);

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handlerSetup = CreateHandler(dbContext);
        handlerSetup.SampleQcIdProvider
            .Setup(x => x.GetSampleQcTubeKey(tubeBarcode))
            .ReturnsAsync(CreateSampleQcTubeKey(tubeBarcode, tubeId));

        // Mock tube exists in storage with sibling drawer area
        var tube = new SampleTube
        {
            TubeId = tubeId,
            InstrumentNumber = _instrumentNumber,
            Area = SampleArea.SampleDrawer2,
            TrayPosition = 1,
            TubePosition = 1
        };
        handlerSetup.SampleStorage.AddOrUpdateTube(tube, _ => tube);

        // Mock both drawer states as open to allow relocation
        handlerSetup.Instrument.Setup(x => x.DrawersLockState[DrawerType.Sample1])
            .Returns(new DrawerLockState(DrawerType.Sample1, LockState.Open, false));
        handlerSetup.Instrument.Setup(x => x.DrawersLockState[DrawerType.Sample2])
            .Returns(new DrawerLockState(DrawerType.Sample2, LockState.Open, false));

        var handler = handlerSetup.Handler;
        var command = new UpdateSampleTrayPositionCommand { TrayId = _trayId, TubePosition = _tubePosition, TubeBarcode = tubeBarcode, HighPriority = true };

        // Act
        await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        await dbContext.SampleTrays.Include(x => x.SampleTubes).FirstAsync(x => x.Id == _trayId2, TestContext.Current.CancellationToken);
        var targetTray = await dbContext.SampleTrays.Include(x => x.SampleTubes).FirstAsync(x => x.Id == _trayId, TestContext.Current.CancellationToken);

        //originalTray.SampleTubes.Should().BeEmpty(); // Tube removed from original tray
        //targetTray.SampleTubes.Should().HaveCount(1); // Tube relocated to target tray

        var relocatedTube = targetTray.SampleTubes.First();
        relocatedTube.TubePosition.Should().Be(_tubePosition);
        relocatedTube.TubeId.Should().Be(tubeId);
        relocatedTube.TrayId.Should().Be(_trayId);
        //relocatedTube.InstrumentNumber.Should().Be(_instrumentNumber);
        relocatedTube.HighPriority.Should().Be(true);
    }

    [Fact]
    public async Task Handle_ShouldRelocateTubeBetweenInstruments_WhenDuplicateTubeExistsInDifferentInstrument()
    {
        // Arrange
        var tubeBarcode = "DUPLICATE_TUBE_04";
        var tubeId = "TUBE_04";

        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();

        await RemoveIfExists(dbContext, new List<string> { _trayId, _trayId2 });

        dbContext.SampleTrays.AddRange(
        [
            new SampleTray
            {
                Id = _trayId,
                Drawer = DrawerType.Sample1,
                InstrumentNumber = _instrumentNumber, // Target instrument
                SampleTubes = new List<SampleTube>()
            },
            new SampleTray
            {
                Id = _trayId2,
                Drawer = DrawerType.Sample1,
                InstrumentNumber = _instrumentNumber2, // Different instrument
                SampleTubes = new List<SampleTube>
                {
                    // Existing tube in different instrument
                    new() {
                        TubePosition = _tubePosition,
                        TubeBarcode = tubeBarcode,
                        TubeId = tubeId,
                        SampleId = tubeId,
                        TrayId = _trayId2,
                        InstrumentNumber = _instrumentNumber2,
                        HighPriority = false,
                        Status = SampleTubeStatus.InProgress,
                        IdentitySource = SampleTubeIdentitySource.ManualInput
                    }
                }
            }
        ]);

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handlerSetup = CreateHandler(dbContext);
        handlerSetup.SampleQcIdProvider
            .Setup(x => x.GetSampleQcTubeKey(tubeBarcode))
            .ReturnsAsync(CreateSampleQcTubeKey(tubeBarcode, tubeId));

        // Mock tube exists in storage with different instrument
        var tube = new SampleTube
        {
            TubeId = tubeId,
            InstrumentNumber = _instrumentNumber2,
            Area = SampleArea.SampleDrawer1,
            TrayPosition = 1,
            TubePosition = 1
        };
        handlerSetup.SampleStorage.AddOrUpdateTube(tube, _ => tube);

        // Mock drawer states as open to allow relocation
        handlerSetup.Instrument.Setup(x => x.DrawersLockState[DrawerType.Sample1])
            .Returns(new DrawerLockState(DrawerType.Sample1, LockState.Open, false));
        var handler = handlerSetup.Handler;
        var command = new UpdateSampleTrayPositionCommand
        {
            TrayId = _trayId,
            TubePosition = _tubePosition2,
            TubeBarcode = tubeBarcode,
            HighPriority = true
        };

        // Act
        await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        await dbContext.SampleTrays.Include(x => x.SampleTubes).FirstAsync(x => x.Id == _trayId2, TestContext.Current.CancellationToken);
        var targetTray = await dbContext.SampleTrays.Include(x => x.SampleTubes).FirstAsync(x => x.Id == _trayId, TestContext.Current.CancellationToken);

        //originalTray.SampleTubes.Should().BeEmpty(); // Tube removed from original instrument's tray
        //targetTray.SampleTubes.Should().HaveCount(1); // Tube relocated to target instrument's tray

        var relocatedTube = targetTray.SampleTubes.First();
        relocatedTube.TubePosition.Should().Be(_tubePosition2);
        relocatedTube.TubeId.Should().Be(tubeId);
        relocatedTube.TrayId.Should().Be(_trayId);
        //relocatedTube.InstrumentNumber.Should().Be(_instrumentNumber); // Updated to target instrument
        relocatedTube.HighPriority.Should().Be(true);

        // Verify hub notification includes both affected trays
        handlerSetup.SampleTrayHub.Verify(x => x.PositionStatusUpdated(It.Is<List<string>>(list =>
            list.Contains(_trayId) && list.Contains(_trayId2))), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenRelocatingTubeFromClosedDrawer()
    {
        // Arrange
        var tubeBarcode = "CLOSED_DRAWER_TUBE";
        var tubeId = "TUBE_05";

        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();

        await RemoveIfExists(dbContext, new List<string> { _trayId, _trayId2 });

        dbContext.SampleTrays.AddRange(
        [
            new SampleTray
            {
                Id = _trayId,
                Drawer = DrawerType.Sample1,
                InstrumentNumber = _instrumentNumber,
                SampleTubes = new List<SampleTube>()
            },
            new SampleTray
            {
                Id = _trayId2,
                Drawer = DrawerType.Sample2,
                InstrumentNumber = _instrumentNumber,
                SampleTubes = new List<SampleTube>
                {
                    // Existing tube in closed drawer
                    new() {
                        TubePosition = _tubePosition,
                        TubeBarcode = tubeBarcode,
                        TubeId = tubeId,
                        SampleId = tubeId,
                        TrayId = _trayId2,
                        InstrumentNumber = _instrumentNumber,
                        HighPriority = false,
                        Status = SampleTubeStatus.InProgress,
                        IdentitySource = SampleTubeIdentitySource.ManualInput
                    }
                }
            }
        ]);

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handlerSetup = CreateHandler(dbContext);
        handlerSetup.SampleQcIdProvider
            .Setup(x => x.GetSampleQcTubeKey(tubeBarcode))
            .ReturnsAsync(CreateSampleQcTubeKey(tubeBarcode, tubeId));

        // Mock tube exists in storage with closed drawer
        var tube = new SampleTube
        {
            TubeId = tubeId,
            InstrumentNumber = _instrumentNumber,
            Area = SampleArea.SampleDrawer2,
            TrayPosition = 1,
            TubePosition = 1
        };
        handlerSetup.SampleStorage.AddOrUpdateTube(tube, _ => tube);

        // Mock target drawer as open, but source drawer as closed
        handlerSetup.Instrument.Setup(x => x.DrawersLockState[DrawerType.Sample1])
            .Returns(new DrawerLockState(DrawerType.Sample1, LockState.Open, false));
        handlerSetup.Instrument.Setup(x => x.DrawersLockState[DrawerType.Sample2])
            .Returns(new DrawerLockState(DrawerType.Sample2, LockState.ClosedLocked, false)); // Source drawer is closed
        var handler = handlerSetup.Handler;
        var command = new UpdateSampleTrayPositionCommand
        {
            TrayId = _trayId,
            TubePosition = _tubePosition,
            TubeBarcode = tubeBarcode,
            HighPriority = true
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(async () =>
            await handler.Handle(command, TestContext.Current.CancellationToken));

        // Verify the exception message
        exception.Message.Should().Contain("Duplicate Tube Barcode");
        exception.Message.Should().Contain("closed and locked drawer");
    }

    [Fact]
    public async Task Handle_ShouldUpdateCompletedTubeToInProgress_WhenCompletedTubeIsManuallyReEnteredWithDifferentBarcode()
    {
        // Arrange
        var originalTubeBarcode = "1234";
        var newTubeBarcode = "9899";
        var originalTubeId = "TUBE_COMPLETED_02";
        var newTubeId = "TUBE_NEW_02";
        var originalSampleId = "SAMPLE_COMPLETED_02";
        //var newSampleId = "SAMPLE_NEW_02";

        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();

        await RemoveIfExists(dbContext, new List<string> { _trayId, _trayId2 });

        dbContext.SampleTrays.Add(new SampleTray
        {
            Id = _trayId,
            Drawer = DrawerType.Sample1,
            InstrumentNumber = _instrumentNumber,
            SampleTubes = new List<SampleTube>
            {
                // Existing completed tube that was previously unloaded and now being updated with different barcode
                new()
                {
                    TubePosition = _tubePosition,
                    TubeBarcode = originalTubeBarcode,
                    TubeId = originalTubeId,
                    SampleId = originalSampleId,
                    TrayId = _trayId,
                    InstrumentNumber = _instrumentNumber,
                    HighPriority = false,
                    Status = SampleTubeStatus.Completed,
                    IdentitySource = SampleTubeIdentitySource.BarcodeScanning,
                    RequestCreatedDate = DateTimeOffset.UtcNow
                }
            }
        });

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handlerSetup = CreateHandler(dbContext);
        handlerSetup.SampleQcIdProvider
            .Setup(x => x.GetSampleQcTubeKey(newTubeBarcode))
            .ReturnsAsync(CreateSampleQcTubeKey(newTubeBarcode, newTubeId));
        handlerSetup.Mediator
            .Setup(x => x.Send(It.IsAny<GetRequestIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RequestIdResponseDto(-1, DateTimeOffset.MinValue, false));

        handlerSetup.Instrument.Setup(x => x.DrawersLockState[DrawerType.Sample1])
            .Returns(new DrawerLockState(DrawerType.Sample1, LockState.Open, false));
        var handler = handlerSetup.Handler;
        var command = new UpdateSampleTrayPositionCommand
        {
            TrayId = _trayId,
            TubePosition = _tubePosition,
            TubeBarcode = newTubeBarcode, // Different barcode than original
            HighPriority = false
        };

        // Act
        await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        var sampleTray = await dbContext.SampleTrays.Include(x => x.SampleTubes).FirstAsync(x => x.Id == _trayId, TestContext.Current.CancellationToken);

        var updatedTube = sampleTray.SampleTubes.First(x => x.TubePosition == _tubePosition);// x => x.IsTubePositionCleared == false
        updatedTube.TubeBarcode.Should().Be(newTubeBarcode);
        updatedTube.TubeId.Should().Be(newTubeId);
        updatedTube.SampleId.Should().Be(newTubeId); // SampleId should be updated to new tube's ID
        updatedTube.HighPriority.Should().Be(false);

        // The tube should change to InProgress status when re-entered with different barcode
        updatedTube.Status.Should().Be(SampleTubeStatus.InProgress);
        updatedTube.IdentitySource.Should().Be(SampleTubeIdentitySource.ManualInput); // Should be ManualInput since barcode changed

        // Verify hub notification
        handlerSetup.SampleTrayHub.Verify(x => x.PositionStatusUpdated(It.Is<List<string>>(list =>
            list.Contains(_trayId))), Times.Once);
    }

    #endregion

    #region Enable/sidable editing Sample ID  Tests

    /// <summary>
    /// Tests that when user is enabled to edit sample ID, they can update tube barcode.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldAllowBarcodeUpdate_WhenUserHasEditingSampleIDEnabled()
    {
        // Arrange
        var existingBarcode = "OLD_BARCODE_123";
        var newBarcode = "NEW_BARCODE_456";
        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();

        await RemoveIfExists(dbContext, new List<string> { _trayId });

        dbContext.SampleTrays.Add(new SampleTray
        {
            Id = _trayId,
            Drawer = DrawerType.Sample1,
            InstrumentNumber = _instrumentNumber,
            SampleTubes = new List<SampleTube>
            {
                new() {
                    TubePosition = _tubePosition,
                    TubeBarcode = existingBarcode,
                    TubeId = existingBarcode,
                    SampleId = existingBarcode,
                    HighPriority = false,
                    Status = SampleTubeStatus.InProgress,
                    IdentitySource = SampleTubeIdentitySource.ManualInput,
                    TrayId = _trayId,
                    InstrumentNumber = _instrumentNumber
                }
            }
        });

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handlerSetup = CreateHandler(dbContext);
        handlerSetup.SampleQcIdProvider
            .Setup(x => x.GetSampleQcTubeKey(newBarcode))
            .ReturnsAsync(CreateSampleQcTubeKey(newBarcode));

        var handler = handlerSetup.Handler;
        var command = new UpdateSampleTrayPositionCommand
        {
            TrayId = _trayId,
            TubePosition = _tubePosition,
            TubeBarcode = newBarcode, // Different barcode from existing
            HighPriority = false
        };

        // Act
        await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        var sampleTray = await dbContext.SampleTrays.Include(x => x.SampleTubes).FirstAsync(x => x.Id == _trayId, TestContext.Current.CancellationToken);
        sampleTray.SampleTubes.Should().HaveCount(1);

        var updatedTube = sampleTray.SampleTubes.First();
        updatedTube.TubeBarcode.Should().Be(newBarcode);
        updatedTube.Status.Should().Be(SampleTubeStatus.InProgress);
        updatedTube.IdentitySource.Should().Be(SampleTubeIdentitySource.ManualInput);
    }

    /// <summary>
    /// Tests that when user is disabled to edit sample ID, they can only remove tube barcode (set to empty/null).
    /// </summary>
    [Fact]
    public async Task Handle_ShouldAllowBarcodeRemoval_WhenUserHasEditingDisabled()
    {
        // Arrange
        var existingBarcode = "EXISTING_BARCODE_789";
        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();

        await RemoveIfExists(dbContext, new List<string> { _trayId });

        dbContext.SampleTrays.Add(new SampleTray
        {
            Id = _trayId,
            Drawer = DrawerType.Sample1,
            InstrumentNumber = _instrumentNumber,
            SampleTubes = new List<SampleTube>
            {
                new() {
                    TubePosition = _tubePosition,
                    TubeBarcode = existingBarcode,
                    TubeId = existingBarcode,
                    SampleId = existingBarcode,
                    HighPriority = false,
                    Status = SampleTubeStatus.InProgress,
                    IdentitySource = SampleTubeIdentitySource.ManualInput,
                    TrayId = _trayId,
                    InstrumentNumber = _instrumentNumber
                }
            }
        });

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handlerSetup = CreateHandler(dbContext, enableEditionOfSampleId: false);
        handlerSetup.Mediator
            .Setup(x => x.Send(It.IsAny<GetRequestIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RequestIdResponseDto(1, DateTimeOffset.UtcNow, true));
        var handler = handlerSetup.Handler;
        var command = new UpdateSampleTrayPositionCommand
        {
            TrayId = _trayId,
            TubePosition = _tubePosition,
            TubeBarcode = null, // Removing barcode (setting to null/empty)
            HighPriority = true
        };

        // Act
        await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        var sampleTray = await dbContext.SampleTrays.Include(x => x.SampleTubes).FirstAsync(x => x.Id == _trayId, TestContext.Current.CancellationToken);
        sampleTray.SampleTubes.Should().HaveCount(1);

        var updatedTube = sampleTray.SampleTubes.First();
        updatedTube.TubeBarcode.Should().BeNull(); // Barcode should be removed
        updatedTube.HighPriority.Should().Be(true); // High priority should be set
        updatedTube.Status.Should().Be(SampleTubeStatus.NotIdentified); // State should be NotIdentified since no barcode

        // Verify that the original tube with barcode is unmapped (still exists with null position)
        var allTubes = await dbContext.SampleTubes.ToListAsync(TestContext.Current.CancellationToken);
        var unmappedTube = allTubes.FirstOrDefault(t => t.TubeBarcode == existingBarcode);
        unmappedTube.Should().NotBeNull();
        unmappedTube.TubePosition.Should().BeNull();
        unmappedTube.TrayId.Should().BeNull();
    }

    /// <summary>
    /// Tests that when user is disabled to edit sample ID, attempting to change barcode throws ValidationException.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldThrowValidationException_WhenUserHasEditingDiasbledAndAttemptsToChangeBarcodes()
    {
        // Arrange
        var existingBarcode = "EXISTING_BARCODE_999";
        var newBarcode = "NEW_BARCODE_999";
        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();

        await RemoveIfExists(dbContext, new List<string> { _trayId });

        dbContext.SampleTrays.Add(new SampleTray
        {
            Id = _trayId,
            Drawer = DrawerType.Sample1,
            InstrumentNumber = _instrumentNumber,
            SampleTubes = new List<SampleTube>
            {
                new() {
                    TubePosition = _tubePosition,
                    TubeBarcode = existingBarcode,
                    TubeId = existingBarcode,
                    SampleId = existingBarcode,
                    HighPriority = false,
                    Status = SampleTubeStatus.InProgress,
                    IdentitySource = SampleTubeIdentitySource.ManualInput,
                    TrayId = _trayId,
                    InstrumentNumber = _instrumentNumber,
                    RequestCreatedDate = DateTimeOffset.UtcNow
                }
            }
        });

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handlerSetup = CreateHandler(dbContext, enableEditionOfSampleId: false);
        handlerSetup.SampleQcIdProvider
            .Setup(x => x.GetSampleQcTubeKey(newBarcode))
            .ReturnsAsync(CreateSampleQcTubeKey(newBarcode));

        var handler = handlerSetup.Handler;
        var command = new UpdateSampleTrayPositionCommand
        {
            TrayId = _trayId,
            TubePosition = _tubePosition,
            TubeBarcode = newBarcode, // Attempting to change to different barcode
            HighPriority = false
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(async () =>
            await handler.Handle(command, TestContext.Current.CancellationToken));

        exception.Message.Should().Contain("Editing of Sample ID is not allowed");
    }

    [Fact]
    public async Task Handle_ShouldRemoveSampleTubeErrorTaskCard_WhenNoTubesWithErrorsExistAfterUpdate()
    {
        // Arrange
        var tubeBarcode = "ERROR_TUBE_BARCODE";
        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();

        await RemoveIfExists(dbContext, new List<string> { _trayId });

        // Create tray with tube that has an error
        dbContext.SampleTrays.Add(new SampleTray
        {
            Id = _trayId,
            Drawer = DrawerType.Sample1,
            InstrumentNumber = _instrumentNumber,
            SampleTubes = new List<SampleTube>
            {
                new() {
                    TubePosition = _tubePosition,
                    TubeBarcode = tubeBarcode,
                    TubeId = tubeBarcode,
                    SampleId = tubeBarcode,
                    HighPriority = false,
                    Status = SampleTubeStatus.InProgress,
                    IdentitySource = SampleTubeIdentitySource.ManualInput,
                    TrayId = _trayId,
                    InstrumentNumber = _instrumentNumber,
                    InternalError = SampleTubeError.MissingTestOrders // Tube has error
                }
            }
        });

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handlerSetup = CreateHandler(dbContext);
        handlerSetup.Mediator
            .Setup(x => x.Send(It.IsAny<GetRequestIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RequestIdResponseDto(1, DateTimeOffset.UtcNow, true));

        // Mock drawer state as open
        handlerSetup.Instrument
            .Setup(x => x.DrawersLockState[DrawerType.Sample1])
            .Returns(new DrawerLockState(DrawerType.Sample1, LockState.Open, false));
        handlerSetup.Instrument
            .Setup(x => x.DrawersLockState[DrawerType.Sample2])
            .Returns(new DrawerLockState(DrawerType.Sample2, LockState.Open, false));

        var handler = handlerSetup.Handler;
        var command = new UpdateSampleTrayPositionCommand
        {
            TrayId = _trayId,
            TubePosition = _tubePosition,
            TubeBarcode = null, // Remove barcode to unmap the tube with error
            HighPriority = false
        };

        // Act
        await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        var sampleTray = await dbContext.SampleTrays.Include(x => x.SampleTubes).FirstAsync(x => x.Id == _trayId, TestContext.Current.CancellationToken);
        sampleTray.SampleTubes.Should().BeEmpty(); // Tube with error should be unmapped

        // Verify that RemoveTaskCardCommand was sent for SampleTubeError
        handlerSetup.Mediator.Verify(x => x.Send(
            It.Is<RemoveTaskCardCommand>(cmd =>
                cmd.CardType == TaskCardType.SampleTubeError &&
                cmd.InstrumentNumber == _instrumentNumber),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldRemoveRemoveSamplesFromDrawerTaskCard_WhenNoTubesWithUnloadRequestedStatusExistAfterUpdate()
    {
        // Arrange
        var tubeBarcode = "UNLOAD_REQUESTED_TUBE_BARCODE";
        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();

        await RemoveIfExists(dbContext, new List<string> { _trayId });

        // Create tray with tube that has an error
        dbContext.SampleTrays.Add(new SampleTray
        {
            Id = _trayId,
            Drawer = DrawerType.Sample1,
            InstrumentNumber = _instrumentNumber,
            SampleTubes = new List<SampleTube>
            {
                new() {
                    TubePosition = _tubePosition,
                    TubeBarcode = tubeBarcode,
                    TubeId = tubeBarcode,
                    SampleId = tubeBarcode,
                    HighPriority = false,
                    Status = SampleTubeStatus.Completed,
                    UnloadRequested = true,
                    IdentitySource = SampleTubeIdentitySource.ManualInput,
                    TrayId = _trayId,
                    InstrumentNumber = _instrumentNumber,
                    InternalError = SampleTubeError.None
                }
            }
        });

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handlerSetup = CreateHandler(dbContext);
        handlerSetup.Mediator
            .Setup(x => x.Send(It.IsAny<GetRequestIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RequestIdResponseDto(1, DateTimeOffset.UtcNow, true));

        // Mock drawer state as open
        handlerSetup.Instrument
            .Setup(x => x.DrawersLockState[DrawerType.Sample1])
            .Returns(new DrawerLockState(DrawerType.Sample1, LockState.Open, false));
        handlerSetup.Instrument
            .Setup(x => x.DrawersLockState[DrawerType.Sample2])
            .Returns(new DrawerLockState(DrawerType.Sample2, LockState.Open, false));

        var handler = handlerSetup.Handler;
        var command = new UpdateSampleTrayPositionCommand
        {
            TrayId = _trayId,
            TubePosition = _tubePosition,
            TubeBarcode = null,
            HighPriority = false
        };

        // Act
        await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        var sampleTray = await dbContext.SampleTrays.Include(x => x.SampleTubes).FirstAsync(x => x.Id == _trayId, TestContext.Current.CancellationToken);
        sampleTray.SampleTubes.Should().BeEmpty();

        // Verify that RemoveTaskCardCommand was sent
        handlerSetup.Mediator.Verify(x => x.Send(
            It.Is<RemoveTaskCardCommand>(cmd =>
                cmd.CardType == TaskCardType.RemoveSamplesFromDrawer &&
                cmd.InstrumentNumber == _instrumentNumber),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    private static HandlerDependencies CreateHandler(ISampleDbContext dbContext, bool enableEditionOfSampleId = true)
    {
        var sampleQcIdProvider = CreateSampleQcIdProvider();
        var sampleTrayHub = new Mock<ISampleTrayHub>();
        var sampleStorage = new InstrumentSampleStorage();
        var instrumentService = new Mock<IInstrumentManager>();
        var instrument = new Mock<IInstrument>();
        var mediator = new Mock<IMediator>();

        instrumentService
            .Setup(x => x.GetInstrument(It.IsAny<string>()))
            .Returns(instrument.Object);

        instrument
            .Setup(x => x.DrawersLockState[DrawerType.Sample1])
            .Returns(new DrawerLockState(DrawerType.Sample1, LockState.Open, false));
        instrument
            .Setup(x => x.DrawersLockState[DrawerType.Sample2])
            .Returns(new DrawerLockState(DrawerType.Sample2, LockState.Open, false));

        mediator.Setup(x => x.Send(It.IsAny<GetRequestIdQuery>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(new RequestIdResponseDto(-1, DateTimeOffset.MinValue, false));
        mediator.Setup(x => x.Send(It.IsAny<GetTestOrderStatesQuery>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(new Dictionary<string, GetTestOrderStatesQueryResultItem>());

        var samplesSettings = CreateSamplesSettingsMock(enableEditionOfSampleId);

        var handler = new UpdateSampleTrayPositionCommandHandler(dbContext, sampleQcIdProvider.Object, sampleTrayHub.Object, sampleStorage, instrumentService.Object, mediator.Object, samplesSettings.Object);

        return new HandlerDependencies(handler, sampleQcIdProvider, sampleTrayHub, sampleStorage, instrumentService, instrument, mediator);
    }

    private static Mock<ISampleQcIdProvider> CreateSampleQcIdProvider()
    {
        var provider = new Mock<ISampleQcIdProvider>();

        provider.Setup(x => x.GetSampleQcTubeKey(It.IsAny<string?>()))
            .ReturnsAsync((string? barcode) =>
            {
                if (string.IsNullOrWhiteSpace(barcode))
                {
                    return CreateSampleQcTubeKey(null, unreadable: true);
                }

                return CreateSampleQcTubeKey(barcode);
            });

        provider.Setup(x => x.GetSampleTubeKeyNoBarcode(It.IsAny<string>()))
            .Returns((string sampleId) => CreateSampleQcTubeKey(sampleId));

        return provider;
    }

    private static SampleQcTubeKey CreateSampleQcTubeKey(string? barcode, string? tubeId = null, bool unreadable = false)
    {
        if (unreadable)
        {
            return new SampleQcTubeKey(barcode, SampleType.Sample, string.Empty, string.Empty, true, true, string.Empty, -1);
        }

        var resolvedId = tubeId ?? barcode ?? string.Empty;
        return new SampleQcTubeKey(barcode, SampleType.Sample, resolvedId, string.Empty, false, true, string.Empty, 0); //TODO, what should the TubeTypeId be?
    }

    private static async Task RemoveIfExists(ISampleDbContext dbContext, List<string> trayIds)
    {
        var existingTrays = await dbContext.SampleTrays
         .Include(x => x.SampleTubes)
         .Where(t => trayIds.Contains(t.Id))
         .ToListAsync(TestContext.Current.CancellationToken);

        if (existingTrays.Count != 0)
        {
            dbContext.SampleTrays.RemoveRange(existingTrays);
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
    }

    private static Mock<IOptionsSnapshot<SamplesSettings>> CreateSamplesSettingsMock(bool enableEditionOfSampleId = true)
    {
        var samplesSettingsMock = new Mock<IOptionsSnapshot<SamplesSettings>>();
        var samplesSettings = new SamplesSettings
        {
            Tray = new Tray
            {
                EnableEditingOfSampleId = enableEditionOfSampleId
            }
        };
        samplesSettingsMock.Setup(x => x.Value).Returns(samplesSettings);
        return samplesSettingsMock;
    }

    private sealed record HandlerDependencies(
        UpdateSampleTrayPositionCommandHandler Handler,
        Mock<ISampleQcIdProvider> SampleQcIdProvider,
        Mock<ISampleTrayHub> SampleTrayHub,
        InstrumentSampleStorage SampleStorage,
        Mock<IInstrumentManager> InstrumentService,
        Mock<IInstrument> Instrument,
        Mock<IMediator> Mediator);
}
