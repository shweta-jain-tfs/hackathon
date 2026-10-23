using MediatR;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Reagents;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Parameters;
using Pharos.Server.Reagents.Application.Commands;

namespace Pharos.Server.Reagents.Application.Events;

public class BarcodeScannedEventHandlerTests
{
    private const string _testInstrumentNumber = "INST001";
    private const int _testInstrumentId = 1;
    private const string _testBarcode = "|01073330660041291722033110WA101|991234";
    private const string _testPrimaryBarcode = "07333066004129";

    [Fact]
    public async Task Handle_DoesNothing_WhenEventAlreadyHandled()
    {
        // Arrange
        var hub = new Mock<ICurrentInstrumentServerHub>();
        var barcodeService = new Mock<IReagentBarcodeService>();
        var instrumentService = new Mock<IInstrumentManager>();
        var mediator = new Mock<IMediator>();

        var handler = CreateHandler(hub, barcodeService, instrumentService, mediator);
        var notification = new BarcodeScannedEvent(_testInstrumentNumber, _testBarcode)
        {
            Handled = true
        };

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        instrumentService.Verify(x => x.GetInstrument(It.IsAny<string>()), Times.Never);
        barcodeService.Verify(x => x.DetectArticleType(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Throws_WhenInstrumentIsNotFound()
    {
        // Arrange
        var hub = new Mock<ICurrentInstrumentServerHub>();
        var barcodeService = new Mock<IReagentBarcodeService>();
        var instrumentService = new Mock<IInstrumentManager>();
        var mediator = new Mock<IMediator>();

        instrumentService.Setup(x => x.GetInstrument(_testInstrumentNumber))
            .Throws<InvalidOperationException>();

        var handler = CreateHandler(hub, barcodeService, instrumentService, mediator);
        var notification = new BarcodeScannedEvent(_testInstrumentNumber, _testBarcode);

        // Act
        var act = () => handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        barcodeService.Verify(x => x.DetectArticleType(It.IsAny<string>()), Times.Never);
        notification.Handled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_DoesNothing_WhenArticleTypeIsNone()
    {
        // Arrange
        var hub = new Mock<ICurrentInstrumentServerHub>();
        var barcodeService = new Mock<IReagentBarcodeService>();
        var instrumentService = new Mock<IInstrumentManager>();
        var mediator = new Mock<IMediator>();

        var instrument = CreateInstrumentMock(instrumentService);
        instrument.Setup(x => x.Id).Returns(_testInstrumentId);

        barcodeService.Setup(x => x.DetectArticleType(_testBarcode))
            .ReturnsAsync((ArticleType.None, BarcodeFormat.Secondary));

        var handler = CreateHandler(hub, barcodeService, instrumentService, mediator);
        var notification = new BarcodeScannedEvent(_testInstrumentNumber, _testBarcode);

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        hub.Verify(x => x.ReagentBarcodeScanned(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        notification.Handled.Should().BeFalse();
    }

    [Theory]
    [InlineData(ArticleType.CalibratorCartridge)]
    [InlineData(ArticleType.ConjugateCartridge)]
    [InlineData(ArticleType.DiluentCartridge)]
    public async Task Handle_DoesNothing_WhenArticleTypeIsNotBottle(ArticleType articleType)
    {
        // Arrange
        var hub = new Mock<ICurrentInstrumentServerHub>();
        var barcodeService = new Mock<IReagentBarcodeService>();
        var instrumentService = new Mock<IInstrumentManager>();
        var mediator = new Mock<IMediator>();

        var instrument = CreateInstrumentMock(instrumentService);
        instrument.Setup(x => x.Id).Returns(_testInstrumentId);

        barcodeService.Setup(x => x.DetectArticleType(_testBarcode))
            .ReturnsAsync((articleType, BarcodeFormat.Secondary));

        var handler = CreateHandler(hub, barcodeService, instrumentService, mediator);
        var notification = new BarcodeScannedEvent(_testInstrumentNumber, _testBarcode);

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        hub.Verify(x => x.ReagentBarcodeScanned(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        notification.Handled.Should().BeFalse();
    }

    [Trait("ProductRequirement", "PR_2253")]
    [Trait("ModuleRequirement", "MR_5085")]
    [Theory]
    [InlineData(ArticleType.StopSolutionBottle)]
    [InlineData(ArticleType.WashAdditiveBottle)]
    [InlineData(ArticleType.WashConcentrateBottle)]
    [Trait("ProductRequirement", "PR_2475")]
    [Trait("ModuleRequirement", "MR_5539")]
    public async Task Handle_SendsIncorrectBarcodeMessage_WhenPrimaryBarcodeScannedAndDrawerOpen(ArticleType articleType)
    {
        // Arrange
        var hub = new Mock<ICurrentInstrumentServerHub>();
        var barcodeService = new Mock<IReagentBarcodeService>();
        var instrumentService = new Mock<IInstrumentManager>();
        var mediator = new Mock<IMediator>();

        var instrument = CreateInstrumentMock(instrumentService);
        instrument.Setup(x => x.Id).Returns(_testInstrumentId);
        instrument.Setup(x => x.DrawersLockState[DrawerType.BottledReagent])
            .Returns(new DrawerLockState(DrawerType.BottledReagent, LockState.Open, false));

        barcodeService.Setup(x => x.DetectArticleType(_testPrimaryBarcode))
            .ReturnsAsync((articleType, BarcodeFormat.Primary));

        var handler = CreateHandler(hub, barcodeService, instrumentService, mediator);
        var notification = new BarcodeScannedEvent(_testInstrumentNumber, _testPrimaryBarcode);

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        hub.Verify(x => x.IncorrectBarcodeScannedForBottles(_testInstrumentId), Times.Once);
        notification.Handled.Should().BeTrue();
    }

    [Trait("ProductRequirement", "PR_2253")]
    [Trait("ModuleRequirement", "MR_5085")]
    [Fact]
    [Trait("ProductRequirement", "PR_2475")]
    [Trait("ModuleRequirement", "MR_5538")]
    public async Task Handle_DoesNotSendIncorrectBarcodeMessage_WhenPrimaryBarcodeScannedButDrawerClosed()
    {
        // Arrange
        var hub = new Mock<ICurrentInstrumentServerHub>();
        var barcodeService = new Mock<IReagentBarcodeService>();
        var instrumentService = new Mock<IInstrumentManager>();
        var mediator = new Mock<IMediator>();

        var instrument = CreateInstrumentMock(instrumentService);
        instrument.Setup(x => x.Id).Returns(_testInstrumentId);
        instrument.Setup(x => x.DrawersLockState[DrawerType.BottledReagent])
            .Returns(new DrawerLockState(DrawerType.BottledReagent, LockState.ClosedLocked, false));

        barcodeService.Setup(x => x.DetectArticleType(_testPrimaryBarcode))
            .ReturnsAsync((ArticleType.StopSolutionBottle, BarcodeFormat.Primary));

        var handler = CreateHandler(hub, barcodeService, instrumentService, mediator);
        var notification = new BarcodeScannedEvent(_testInstrumentNumber, _testPrimaryBarcode);

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        hub.Verify(x => x.IncorrectBarcodeScannedForBottles(It.IsAny<int>()), Times.Never);
        notification.Handled.Should().BeTrue();
    }

    [Trait("ProductRequirement", "PR_1373")]
    [Trait("ModuleRequirement", "MR_5396")]
    [Theory]
    [InlineData(ArticleType.StopSolutionBottle)]
    [InlineData(ArticleType.WashAdditiveBottle)]
    [InlineData(ArticleType.WashConcentrateBottle)]
    [Trait("ProductRequirement", "PR_2475")]
    [Trait("ModuleRequirement", "MR_5538")]
    public async Task Handle_ProcessesSecondaryBarcode_WhenReagentInfoFound(ArticleType articleType)
    {
        // Arrange
        var hub = new Mock<ICurrentInstrumentServerHub>();
        var barcodeService = new Mock<IReagentBarcodeService>();
        var instrumentService = new Mock<IInstrumentManager>();
        var mediator = new Mock<IMediator>();

        var reagentInfo = CreateReagentInfo(articleType);

        var instrument = CreateInstrumentMock(instrumentService);
        instrument.Setup(x => x.Id).Returns(_testInstrumentId);

        barcodeService.Setup(x => x.DetectArticleType(_testBarcode))
            .ReturnsAsync((articleType, BarcodeFormat.Secondary));
        barcodeService.Setup(x => x.GetReagentInfo(It.IsAny<ArticleType>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(reagentInfo);

        var handler = CreateHandler(hub, barcodeService, instrumentService, mediator);
        var notification = new BarcodeScannedEvent(_testInstrumentNumber, _testBarcode);

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        hub.Verify(x => x.ReagentBarcodeScanned(_testInstrumentId, _testBarcode), Times.Once);
        mediator.Verify(x => x.Send(
            It.Is<ProcessUpdateBottledReagentCommand>(cmd =>
                cmd.ReagentUdi == _testBarcode &&
                cmd.InstrumentNumber == _testInstrumentNumber &&
                cmd.ReagentInfo == reagentInfo),
            TestContext.Current.CancellationToken), Times.Once);
        notification.Handled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ThrowsException_WhenSecondaryBarcodeScannedButReagentInfoNotFound()
    {
        // Arrange
        var hub = new Mock<ICurrentInstrumentServerHub>();
        var barcodeService = new Mock<IReagentBarcodeService>();
        var instrumentService = new Mock<IInstrumentManager>();
        var instrument = CreateInstrumentMock(instrumentService);
        var mediator = new Mock<IMediator>();

        instrument.Setup(x => x.Id).Returns(_testInstrumentId);
        barcodeService.Setup(x => x.DetectArticleType(_testBarcode))
            .ReturnsAsync((ArticleType.StopSolutionBottle, BarcodeFormat.Secondary));
        barcodeService.Setup(x => x.GetReagentInfo(It.IsAny<ArticleType>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync((ReagentInfo?)null);

        var handler = CreateHandler(hub, barcodeService, instrumentService, mediator);
        var notification = new BarcodeScannedEvent(_testInstrumentNumber, _testBarcode);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.Handle(notification, TestContext.Current.CancellationToken));

        exception.Message.Should().Contain($"Reagent info not found for barcode: {_testBarcode}");
    }

    [Trait("ProductRequirement", "PR_1373")]
    [Trait("ModuleRequirement", "MR_5396")]
    [Fact]
    [Trait("ProductRequirement", "PR_2475")]
    [Trait("ModuleRequirement", "MR_5538")]
    public async Task Handle_SetsHandledToTrue_WhenBottledReagentProcessed()
    {
        // Arrange
        var hub = new Mock<ICurrentInstrumentServerHub>();
        var barcodeService = new Mock<IReagentBarcodeService>();
        var instrumentService = new Mock<IInstrumentManager>();
        var instrument = CreateInstrumentMock(instrumentService);
        var mediator = new Mock<IMediator>();

        var reagentInfo = CreateReagentInfo(ArticleType.StopSolutionBottle);

        instrument.Setup(x => x.Id).Returns(_testInstrumentId);
        barcodeService.Setup(x => x.DetectArticleType(_testBarcode))
            .ReturnsAsync((ArticleType.StopSolutionBottle, BarcodeFormat.Secondary));
        barcodeService.Setup(x => x.GetReagentInfo(It.IsAny<ArticleType>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(reagentInfo);

        var handler = CreateHandler(hub, barcodeService, instrumentService, mediator);
        var notification = new BarcodeScannedEvent(_testInstrumentNumber, _testBarcode);

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        notification.Handled.Should().BeTrue();
    }

    [Trait("ProductRequirement", "PR_1373")]
    [Trait("ModuleRequirement", "MR_5396")]
    [Fact]
    [Trait("ProductRequirement", "PR_2475")]
    [Trait("ModuleRequirement", "MR_5538")]
    public async Task Handle_PassesCancellationToken_ToProcessUpdateBottledReagentCommand()
    {
        // Arrange
        var hub = new Mock<ICurrentInstrumentServerHub>();
        var barcodeService = new Mock<IReagentBarcodeService>();
        var instrumentService = new Mock<IInstrumentManager>();
        var instrument = CreateInstrumentMock(instrumentService);
        var mediator = new Mock<IMediator>();

        var reagentInfo = CreateReagentInfo(ArticleType.StopSolutionBottle);
        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        instrument.Setup(x => x.Id).Returns(_testInstrumentId);
        barcodeService.Setup(x => x.DetectArticleType(_testBarcode))
            .ReturnsAsync((ArticleType.StopSolutionBottle, BarcodeFormat.Secondary));
        barcodeService.Setup(x => x.GetReagentInfo(It.IsAny<ArticleType>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(reagentInfo);

        var handler = CreateHandler(hub, barcodeService, instrumentService, mediator);
        var notification = new BarcodeScannedEvent(_testInstrumentNumber, _testBarcode);

        // Act
        await handler.Handle(notification, cancellationToken);

        // Assert
        mediator.Verify(x => x.Send(
            It.IsAny<ProcessUpdateBottledReagentCommand>(),
            cancellationToken), Times.Once);
    }

    [Trait("ProductRequirement", "PR_2253")]
    [Trait("ModuleRequirement", "MR_5085")]
    [Theory]
    [InlineData(LockState.ClosedLocked)]
    [InlineData(LockState.ClosedUnlocked)]
    [Trait("ProductRequirement", "PR_2475")]
    [Trait("ModuleRequirement", "MR_5538")]
    public async Task Handle_DoesNotSendIncorrectBarcodeMessage_ForNonOpenLockStates(LockState lockState)
    {
        // Arrange
        var hub = new Mock<ICurrentInstrumentServerHub>();
        var barcodeService = new Mock<IReagentBarcodeService>();
        var instrumentService = new Mock<IInstrumentManager>();
        var mediator = new Mock<IMediator>();

        var instrument = CreateInstrumentMock(instrumentService);
        instrument.Setup(x => x.Id).Returns(_testInstrumentId);
        instrument.Setup(x => x.DrawersLockState[DrawerType.BottledReagent])
            .Returns(new DrawerLockState(DrawerType.BottledReagent, lockState, false));
        barcodeService.Setup(x => x.DetectArticleType(_testPrimaryBarcode))
            .ReturnsAsync((ArticleType.StopSolutionBottle, BarcodeFormat.Primary));

        var handler = CreateHandler(hub, barcodeService, instrumentService, mediator);
        var notification = new BarcodeScannedEvent(_testInstrumentNumber, _testPrimaryBarcode);

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        hub.Verify(x => x.IncorrectBarcodeScannedForBottles(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CallsDetectArticleType_WithCorrectBarcode()
    {
        // Arrange
        var hub = new Mock<ICurrentInstrumentServerHub>();
        var barcodeService = new Mock<IReagentBarcodeService>();
        var instrumentService = new Mock<IInstrumentManager>();
        var mediator = new Mock<IMediator>();

        var instrument = CreateInstrumentMock(instrumentService);
        instrument.Setup(x => x.Id).Returns(_testInstrumentId);
        barcodeService.Setup(x => x.DetectArticleType(_testBarcode))
            .ReturnsAsync((ArticleType.None, BarcodeFormat.Secondary));

        var handler = CreateHandler(hub, barcodeService, instrumentService, mediator);
        var notification = new BarcodeScannedEvent(_testInstrumentNumber, _testBarcode);

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        barcodeService.Verify(x => x.DetectArticleType(_testBarcode), Times.Once);
    }

    [Trait("ProductRequirement", "PR_1373")]
    [Trait("ModuleRequirement", "MR_5396")]
    [Fact]
    public async Task Handle_CallsGetReagentInfo_WithSecondaryBarcode()
    {
        // Arrange
        var hub = new Mock<ICurrentInstrumentServerHub>();
        var barcodeService = new Mock<IReagentBarcodeService>();
        var instrumentService = new Mock<IInstrumentManager>();
        var instrument = CreateInstrumentMock(instrumentService);
        var mediator = new Mock<IMediator>();

        var reagentInfo = CreateReagentInfo(ArticleType.WashAdditiveBottle);

        instrument.Setup(x => x.Id).Returns(_testInstrumentId);
        barcodeService.Setup(x => x.DetectArticleType(_testBarcode))
            .ReturnsAsync((ArticleType.WashAdditiveBottle, BarcodeFormat.Secondary));
        barcodeService.Setup(x => x.GetReagentInfo(It.IsAny<ArticleType>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(reagentInfo);

        var handler = CreateHandler(hub, barcodeService, instrumentService, mediator);
        var notification = new BarcodeScannedEvent(_testInstrumentNumber, _testBarcode);

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        barcodeService.Verify(x => x.GetReagentInfo(
            It.IsAny<ArticleType>(),
            It.IsAny<string?>(),
            It.IsAny<string?>()), Times.Once);
    }

    [Trait("ProductRequirement", "PR_2253")]
    [Trait("ModuleRequirement", "MR_5085")]
    [Fact]
    public async Task Handle_ChecksCorrectDrawerType_ForBottledReagent()
    {
        // Arrange
        var hub = new Mock<ICurrentInstrumentServerHub>();
        var barcodeService = new Mock<IReagentBarcodeService>();
        var instrumentService = new Mock<IInstrumentManager>();
        var instrument = CreateInstrumentMock(instrumentService);
        var mediator = new Mock<IMediator>();

        instrument.Setup(x => x.Id).Returns(_testInstrumentId);
        instrument.Setup(x => x.DrawersLockState[DrawerType.BottledReagent])
            .Returns(new DrawerLockState(DrawerType.BottledReagent, LockState.Open, false));
        barcodeService.Setup(x => x.DetectArticleType(_testPrimaryBarcode))
            .ReturnsAsync((ArticleType.WashConcentrateBottle, BarcodeFormat.Primary));

        var handler = CreateHandler(hub, barcodeService, instrumentService, mediator);
        var notification = new BarcodeScannedEvent(_testInstrumentNumber, _testPrimaryBarcode);

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        instrument.Verify(x => x.DrawersLockState[DrawerType.BottledReagent], Times.Once);
    }

    private static BarcodeScannedEventHandler CreateHandler(
        Mock<ICurrentInstrumentServerHub> hub,
        Mock<IReagentBarcodeService> barcodeService,
        Mock<IInstrumentManager> instrumentService,
        Mock<IMediator> mediator)
    {
        return new BarcodeScannedEventHandler(
            hub.Object,
            barcodeService.Object,
            instrumentService.Object,
            mediator.Object);
    }

    private static Mock<IInstrument> CreateInstrumentMock(Mock<IInstrumentManager> instrumentService)
    {
        var instrumentMock = new Mock<IInstrument>();
        instrumentService.Setup(x => x.GetInstrument(_testInstrumentNumber)).Returns(instrumentMock.Object);
        return instrumentMock;
    }

    private static ReagentInfo CreateReagentInfo(ArticleType articleType)
    {
        return new ReagentInfo
        {
            Article = new Article
            {
                ArticleType = articleType,
                ArticleNumber = "ART123",
                Identity = "Test Identity",
                Quantity = 1000
            },
            Lot = new LotInformation
            {
                LotNumber = "LOT123",
                ExpiryDate = DateTimeOffset.UtcNow.AddMonths(6)
            },
            SerialNumber = "SER123"
        };
    }
}
