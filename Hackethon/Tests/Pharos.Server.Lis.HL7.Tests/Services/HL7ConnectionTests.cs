using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Lis.Domain;
using Pharos.Server.Infrastructure;
using Pharos.Server.Lis.Application.Interfaces;
using Pharos.Server.Lis.HL7.Application.Interfaces;
using Pharos.Server.Lis.HL7.Application.Messages;
using Pharos.Server.Lis.HL7.Application.Services;
using System.Diagnostics;
using System.Linq.Expressions;
using Xunit;

namespace Pharos.Server.Lis.HL7.Tests.Services;

public class HL7ConnectionTests
{
    private readonly Mock<IDateTime> _mockDateTime;
    public HL7ConnectionTests()
    {
        _mockDateTime = new Mock<IDateTime>();
        _mockDateTime.Setup(dt => dt.Now).Returns(DateTimeOffset.Now);
        _mockDateTime.Setup(dt => dt.UtcNow).Returns(DateTimeOffset.UtcNow);
    }

    [Fact]
    [Trait("ProductRequirement", "PR_570")]
    public async Task ImportOrder_HL7ImportSetting_SampleIdInObxDisabled_SACSampleIdAvailable_ShouldWaitForMethodResolutionAndUseSACSampleId()
    {
        // Arrange
        var dataLink = new Mock<IHL7DataLink>();
        var methodLookupStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var methodLookupCompleted = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var methodsService = new Mock<IMethodsService>();
        methodsService
            .Setup(x => x.GetMethod("sIgE", It.IsAny<Expression<Func<Method, string>>>()))
            .Returns(() =>
            {
                methodLookupStarted.TrySetResult(true);
                return methodLookupCompleted.Task;
            });
        var sut = CreateConnection(dataLink, sampleIdInObx: false, methodsService: methodsService.Object);
        var cancellationToken = TestContext.Current.CancellationToken;
        TestOrderReceivedEventArgs? received = null;
        sut.TestOrderReceived += (_, e) => received = e;

        // Act
        await sut.OpenAsync();
        var receiveTask = Task.Run(() =>
            RaiseMessage(dataLink, BuildOrderMessage(sacSampleId: "SIDSAC", obxSampleId: "SIDOBX", obrRequestId: "OBR_RID1")), cancellationToken);
        await methodLookupStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
        var returnedBeforeLookupCompleted = await Task.WhenAny(receiveTask, Task.Delay(250, cancellationToken)) == receiveTask;

        // Assert
        returnedBeforeLookupCompleted.Should().BeFalse("message processing must wait for method resolution");
        received.Should().BeNull();

        methodLookupCompleted.SetResult("sIgE");
        await receiveTask.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
        received.Should().NotBeNull();
        received!.Patients.Single().Samples.Single().SampleId.Should().Be("SIDSAC");
    }

    [Fact]
    [Trait("ProductRequirement", "PR_570")]
    public async Task ImportOrder_HL7ImportSetting_SampleIdInObxEnabled_SACSampleIdAvailable_ShouldUseSACSampleId()
    {
        var dataLink = new Mock<IHL7DataLink>();
        var sut = CreateConnection(dataLink, sampleIdInObx: true);
        TestOrderReceivedEventArgs? received = null;
        sut.TestOrderReceived += (_, e) => received = e;

        await sut.OpenAsync();
        RaiseMessage(dataLink, BuildOrderMessage(sacSampleId: "SIDSAC", obxSampleId: "SIDOBX", obrRequestId: "OBR_RID1"));

        received.Should().NotBeNull();
        received!.Patients.Single().Samples.Single().SampleId.Should().Be("SIDSAC");
    }

    [Fact]
    [Trait("ProductRequirement", "PR_570")]
    public async Task ImportOrder_HL7ImportSetting_SampleIdInObxEnabled_SACSampleIdNotAvailable_ShouldUseOBXSampleId()
    {
        // Arrange
        var dataLink = new Mock<IHL7DataLink>();
        var sut = CreateConnection(dataLink, sampleIdInObx: true);
        TestOrderReceivedEventArgs? received = null;
        sut.TestOrderReceived += (_, e) => received = e;

        await sut.OpenAsync();
        RaiseMessage(dataLink, BuildOrderMessage(sacSampleId: "", obxSampleId: "SIDOBX", obrRequestId: "OBR_RID1"));

        received.Should().NotBeNull();
        received!.Patients.Single().Samples.Single().SampleId.Should().Be("SIDOBX");
    }

    [Fact]
    public async Task ProcessAcknowledgementMessage_WithAckMessage_ShouldLogAcknowledgement()
    {
        var dataLink = new Mock<IHL7DataLink>();
        var logger = new Mock<ILogger<HL7Connection>>();
        var sut = CreateConnection(dataLink, sampleIdInObx: true, useRequestIdAsSampleId: true, logger: logger.Object);

        await sut.OpenAsync();
        var act = () => RaiseMessage(dataLink, BuildAcknowledgementMessage());

        act.Should().NotThrow();
        logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((_, _) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExportConnection_WhenOrderMessageIsReceived_ShouldNotRaiseTestOrderReceived()
    {
        var dataLink = new Mock<IHL7DataLink>();
        var sut = CreateExportConnection(dataLink, delayBetweenRequestInformationRecords: 0);
        TestOrderReceivedEventArgs? received = null;
        sut.TestOrderReceived += (_, e) => received = e;

        await sut.OpenAsync();
        RaiseMessage(dataLink, BuildOrderMessage(sacSampleId: "SIDSAC", obxSampleId: "SIDOBX", obrRequestId: "OBR_RID1"));

        received.Should().BeNull();
    }

    [Fact]
    public async Task ImportOrder_HL7ImportSetting_SampleIdInObxDisabled_UseRequestIdAsSampleIdEnabled_SACSampleIdNotAvailable_ShouldUseOBRSampleId()
    {
        var dataLink = new Mock<IHL7DataLink>();
        var sut = CreateConnection(dataLink, sampleIdInObx: false, useRequestIdAsSampleId: true);
        TestOrderReceivedEventArgs? received = null;
        sut.TestOrderReceived += (_, e) => received = e;

        await sut.OpenAsync();
        RaiseMessage(dataLink, BuildOrderMessage(sacSampleId: "", obxSampleId: "SIDOBX", obrRequestId: "OBR_RID1"));

        received.Should().NotBeNull();
        received!.Patients.Single().Samples.Single().SampleId.Should().Be("OBR_RID1");
    }

    [Fact]
    public async Task ImportOrder_HL7ImportSetting_SampleIdInObxDisabled_UseRequestIdAsSampleIdEnabled_SACAndOBXAndOBRSampleIdAvailable_ShouldUseSACSampleId()
    {
        var dataLink = new Mock<IHL7DataLink>();
        var sut = CreateConnection(dataLink, sampleIdInObx: false, useRequestIdAsSampleId: true);
        TestOrderReceivedEventArgs? received = null;
        sut.TestOrderReceived += (_, e) => received = e;

        await sut.OpenAsync();
        RaiseMessage(dataLink, BuildOrderMessage(sacSampleId: "SIDSAC", obxSampleId: "SIDOBX", obrRequestId: "OBR_RID1"));

        received.Should().NotBeNull();
        received!.Patients.Single().Samples.Single().SampleId.Should().Be("SIDSAC");
    }

    [Fact]
    [Trait("ProductRequirement", "PR_1785")]
    public async Task HL7CommSetting_DelayBetweenRequestInformationRecords_ZeroValue_ShouldNotDelayBetweenRequestInRecords()
    {
        var dataLink = new Mock<IHL7DataLink>();
        dataLink.Setup(x => x.SendMessageAsync(It.IsAny<string>())).ReturnsAsync(true);
        var sut = CreateExportConnection(dataLink, delayBetweenRequestInformationRecords: 0);

        await sut.OpenAsync();
        await sut.SendMessageAsync(LisMessageType.TestOrderQuery, "test");
        await sut.SendMessageAsync(LisMessageType.TestOrderQuery, "test");

        _mockDateTime.Verify(dt => dt.Now, Times.Never);
    }

    [Fact]
    [Trait("ProductRequirement", "PR_1785")]
    public async Task HL7CommSetting_DelayBetweenRequestInformationRecords_PositiveValue_ShouldDelayBetweenRequestInRecords()
    {
        var fixedTime = DateTimeOffset.UtcNow;
        _mockDateTime.Setup(dt => dt.Now).Returns(fixedTime);

        var dataLink = new Mock<IHL7DataLink>();
        dataLink.Setup(x => x.SendMessageAsync(It.IsAny<string>())).ReturnsAsync(true);
        var sut = CreateExportConnection(dataLink, delayBetweenRequestInformationRecords: 1);

        await sut.OpenAsync();
        await sut.SendMessageAsync(LisMessageType.TestOrderQuery, "test"); // first send: no delay (timestamp is default)

        var stopwatch = Stopwatch.StartNew();
        await sut.SendMessageAsync(LisMessageType.TestOrderQuery, "test"); // second send: same Now → triggers ~1s delay
        stopwatch.Stop();

        stopwatch.Elapsed.Should().BeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(900));
        _mockDateTime.Verify(dt => dt.Now, Times.AtLeast(2));
    }

    [Fact]
    public async Task ImportOrder_PanelExpansionEnabled_ShouldSetPanelExpansionEnabledTrue()
    {
        var dataLink = new Mock<IHL7DataLink>();
        var sut = CreateConnection(dataLink, sampleIdInObx: false, panelExpansion: true);
        TestOrderReceivedEventArgs? received = null;
        sut.TestOrderReceived += (_, e) => received = e;

        await sut.OpenAsync();
        RaiseMessage(dataLink, BuildOrderMessage(sacSampleId: "SIDSAC", obxSampleId: "SIDOBX", obrRequestId: "OBR_RID1"));

        received.Should().NotBeNull();
        received!.Patients.Single().Samples.Single().TestOrders.Single().PanelExpansionEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task ImportOrder_PanelExpansionDisabled_ShouldSetPanelExpansionEnabledFalse()
    {
        var dataLink = new Mock<IHL7DataLink>();
        var sut = CreateConnection(dataLink, sampleIdInObx: false, panelExpansion: false);
        TestOrderReceivedEventArgs? received = null;
        sut.TestOrderReceived += (_, e) => received = e;

        await sut.OpenAsync();
        RaiseMessage(dataLink, BuildOrderMessage(sacSampleId: "SIDSAC", obxSampleId: "SIDOBX", obrRequestId: "OBR_RID1"));

        received.Should().NotBeNull();
        received!.Patients.Single().Samples.Single().TestOrders.Single().PanelExpansionEnabled.Should().BeFalse();
    }

    [Theory]
    [InlineData(AutomaticPanelRecognition.NotUsed, false, false)]
    [InlineData(AutomaticPanelRecognition.OnlyFromOneMessage, true, false)]
    [InlineData(AutomaticPanelRecognition.FromMultipleMessages, false, true)]
    public async Task ImportOrder_AutomaticPanelRecognition_ShouldSetExpectedPanelRecognitionFlags(
        AutomaticPanelRecognition automaticPanelRecognition,
        bool expectedFromOneMessage,
        bool expectedFromMultipleMessages)
    {
        var dataLink = new Mock<IHL7DataLink>();
        var sut = CreateConnection(dataLink, sampleIdInObx: false, automaticPanelRecognition: automaticPanelRecognition);
        TestOrderReceivedEventArgs? received = null;
        sut.TestOrderReceived += (_, e) => received = e;

        await sut.OpenAsync();
        RaiseMessage(dataLink, BuildOrderMessage(sacSampleId: "SIDSAC", obxSampleId: "SIDOBX", obrRequestId: "OBR_RID1"));

        received.Should().NotBeNull();
        var testOrder = received!.Patients.Single().Samples.Single().TestOrders.Single();
        testOrder.AutoPanelRecognitionFromOneMessage.Should().Be(expectedFromOneMessage);
        testOrder.AutoPanelRecognitionFromMultipleMessage.Should().Be(expectedFromMultipleMessages);
    }

    [Theory]
    [InlineData(RequestPriorityOrder.NotUsed, false, false)]
    [InlineData(RequestPriorityOrder.UseOrderNumberA, true, false)]
    [InlineData(RequestPriorityOrder.UseOrderNumberB, false, true)]
    public async Task ImportOrder_RequestPriorityOrder_ShouldSetExpectedUseRequestPriorityFlags(
        RequestPriorityOrder requestPriorityOrder,
        bool expectedUseRequestPriorityA,
        bool expectedUseRequestPriorityB)
    {
        var dataLink = new Mock<IHL7DataLink>();
        var sut = CreateConnection(dataLink, sampleIdInObx: false, requestPriorityOrder: requestPriorityOrder);
        TestOrderReceivedEventArgs? received = null;
        sut.TestOrderReceived += (_, e) => received = e;

        await sut.OpenAsync();
        RaiseMessage(dataLink, BuildOrderMessage(sacSampleId: "SIDSAC", obxSampleId: "SIDOBX", obrRequestId: "OBR_RID1"));

        received.Should().NotBeNull();
        var testOrder = received!.Patients.Single().Samples.Single().TestOrders.Single();
        testOrder.UseRequestPriorityA.Should().Be(expectedUseRequestPriorityA);
        testOrder.UseRequestPriorityB.Should().Be(expectedUseRequestPriorityB);
    }

    #region Test Helpers

    private HL7Connection CreateConnection(
        Mock<IHL7DataLink> dataLink,
        bool sampleIdInObx,
        bool useRequestIdAsSampleId = false,
        bool panelExpansion = true,
        AutomaticPanelRecognition automaticPanelRecognition = AutomaticPanelRecognition.NotUsed,
        RequestPriorityOrder requestPriorityOrder = RequestPriorityOrder.NotUsed,
        ILogger<HL7Connection>? logger = null,
        IMethodsService? methodsService = null)
    {
        var lisChannel = new LisChannel
        {
            ProtocolType = LisProtocolType.HL7
        };
        lisChannel.HL7Settings!.ImportSettings.SampleIdInOBX = sampleIdInObx;
        lisChannel.HL7Settings.ImportSettings.UseRequestIdAsSampleId = useRequestIdAsSampleId;
        lisChannel.ImportSettings.PanelExpansion = panelExpansion;
        lisChannel.ImportSettings.AutomaticPanelRecognition = automaticPanelRecognition;
        lisChannel.ImportSettings.RequestPriorityOrder = requestPriorityOrder;

        var transportConnection = new Mock<ILisTransportConnection>();
        transportConnection.SetupGet(x => x.State).Returns(LisConnectionState.Open);

        var transportConnectionFactory = new Mock<ILisTransportConnectionFactory>();
        transportConnectionFactory
            .Setup(x => x.CreateTcpServerConnection(It.IsAny<int>()))
            .Returns(transportConnection.Object);

        var dataLinkFactory = new Mock<IHL7DataLinkFactory>();
        dataLinkFactory
            .Setup(x => x.CreateHL7DataLink(It.IsAny<HL7Settings>()))
            .Returns(dataLink.Object);

        var dateTime = new Mock<IDateTime>();
        var messageBuilder = new HL7MessageBuilder(dateTime.Object);

        return new HL7Connection(
            1,
            lisChannel,
            LisConnectionRole.Import,
            transportConnectionFactory.Object,
            dataLinkFactory.Object,
            logger ?? Mock.Of<ILogger<HL7Connection>>(),
            CreateServiceScopeFactory(methodsService),
            _mockDateTime.Object);
    }

    private HL7Connection CreateExportConnection(
        Mock<IHL7DataLink> dataLink,
        int delayBetweenRequestInformationRecords)
    {
        var lisChannel = new LisChannel
        {
            ProtocolType = LisProtocolType.HL7
        };
        lisChannel.HL7Settings!.CommunicationSettings.DelayBetweenRequestInformationRecords = delayBetweenRequestInformationRecords;
        lisChannel.HL7Settings.CommunicationSettings.TcpSettings.UseSeparateExportPort = true;
        lisChannel.HL7Settings.CommunicationSettings.TcpSettings.SocketTypeForSeparateExportPort = SocketType.Server;

        var transportConnection = new Mock<ILisTransportConnection>();
        transportConnection.SetupGet(x => x.State).Returns(LisConnectionState.Open);

        var transportConnectionFactory = new Mock<ILisTransportConnectionFactory>();
        transportConnectionFactory
            .Setup(x => x.CreateTcpServerConnection(It.IsAny<int>()))
            .Returns(transportConnection.Object);

        var dataLinkFactory = new Mock<IHL7DataLinkFactory>();
        dataLinkFactory
            .Setup(x => x.CreateHL7DataLink(It.IsAny<HL7Settings>()))
            .Returns(dataLink.Object);

        return new HL7Connection(
            1,
            lisChannel,
            LisConnectionRole.Export,
            transportConnectionFactory.Object,
            dataLinkFactory.Object,
            Mock.Of<ILogger<HL7Connection>>(),
            CreateServiceScopeFactory(),
            _mockDateTime.Object);
    }

    private static IServiceScopeFactory CreateServiceScopeFactory(IMethodsService? methodsService = null)
    {
        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider
            .Setup(x => x.GetService(typeof(IMethodsService)))
            .Returns(methodsService ?? Mock.Of<IMethodsService>());

        var scope = new Mock<IServiceScope>();
        scope.SetupGet(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);
        return scopeFactory.Object;
    }

    private static void RaiseMessage(Mock<IHL7DataLink> dataLink, IHL7Message message)
    {
        dataLink.Raise(x => x.MessageReceived += null, dataLink.Object, message);
    }

    private static IHL7Message BuildOrderMessage(string sacSampleId, string obxSampleId, string obrRequestId)
    {
        var messageString =
            "MSH|^&~\\|LIS||||20110116124500||ORM^O01|20110412165020|P|2.5|||AL\r" +
            "PID|1|PID1||12345|Patient Name||19610207|M|||Street^^City^State^ZIP^Country\r" +
            "NTE|1|O|Comment|O\r" +
            "ORC|NW|HL7RID1|||||A||20110116161600|||REQID1^Last Name^First Name^Middle\r" +
            "SPM|1|||SER\r" +
            $"SAC|||{sacSampleId}||||||||||RACKID|2\r" +
            $"OBR|1|{obrRequestId}||sIgE-f1|||20110116154433||||N||||P|\r" +
            $"OBX|1|CE|{obxSampleId}\r" +
            "OBX|2|CE|SY^Symptom||1\r" +
            "OBX|3|CE|SY^Symptom||5";

        return new HL7MessageParser().ParseMessage(messageString);
    }

    private static IHL7Message BuildAcknowledgementMessage()
    {
        const string messageString =
            "MSH|^~\\&|LIS||||20110116124500||ACK^R23|ACK1|P|2.5|||AL\r" +
            "MSA|AA|20110412165020";

        return new HL7MessageParser().ParseMessage(messageString);
    }

    #endregion
}
