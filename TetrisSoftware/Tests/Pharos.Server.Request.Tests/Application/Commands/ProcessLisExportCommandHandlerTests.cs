using System.Linq.Expressions;
using Microsoft.Extensions.Options;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Request.Application.Commands.ProcessLisExport;
using TestOrder = Pharos.Server.Request.Domain.TestOrder;

namespace Pharos.Server.Request.Application.Commands;

public class ProcessLisExportCommandHandlerTests
{
    private readonly Mock<IOptionsSnapshot<LisSettings>> _lisSettingsMock;
    private readonly LisSettings _lisSettings;
    private readonly Dictionary<string, int?> _methodDefaultChannels = new();
    private readonly Mock<ISender> _senderMock;
    private readonly ProcessLisExportCommandHandler _sut;

    public ProcessLisExportCommandHandlerTests()
    {
        _lisSettingsMock = new Mock<IOptionsSnapshot<LisSettings>>();
        _lisSettings = new LisSettings();
        _lisSettingsMock.SetupGet(x => x.Value).Returns(_lisSettings);
        _senderMock = new Mock<ISender>();
        _sut = new ProcessLisExportCommandHandler(
            _lisSettingsMock.Object,
            new TestMethodsService(_methodDefaultChannels),
            _senderMock.Object);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task Handle_Should_DoNothing_When_InputIsEmpty(bool emptyTestOrderUids, bool emptyRequests)
    {
        // Arrange
        var testOrderUids = emptyTestOrderUids ? new List<Guid>() : new List<Guid> { Guid.NewGuid() };
        var requests = emptyRequests ? new List<Domain.Request>() : new List<Domain.Request> { CreateRequest(1) };

        var command = new ProcessLisExportCommand(requests, testOrderUids);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _lisSettingsMock.VerifyGet(x => x.Value, Times.Never);
        _senderMock.Verify(x => x.Send(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ProcessTestsWithoutLisChannel_UsingDefaultChannel()
    {
        // Arrange
        var testOrderUid = Guid.NewGuid();
        var request = CreateRequest(1);
        var testOrder = CreateTestOrder(1, testOrderUid, null, TestOrderState.Approved);
        request.TestOrders.Add(testOrder);

        var command = new ProcessLisExportCommand(
            Requests: new List<Domain.Request> { request },
            TestOrderUids: new List<Guid> { testOrderUid });

        // Tests without LIS channel are processed with default channel ID 0
        ConfigureLisChannel(
            channelId: 0,
            automaticSampleExport: LisAutoExportTrigger.WhenTestIsApprovedOrRejected,
            includeOtherTestsInAutomaticExports: IncludeOtherTestsInAutomaticExports.NotUsed,
            whatToExport: WhatToExport.AllStatuses,
            includeLocallyEnteredSamples: true);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _senderMock.Verify(x => x.Send(
            It.Is<SendSampleTestResultsToLisCommand>(cmd =>
                cmd.TestOrderIds != null &&
                cmd.TestOrderIds.Count == 1 &&
                cmd.TestOrderIds.Contains(1) &&
                cmd.LisChannelId == 0 &&
                cmd.IsManualExport == false),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ProcessTestsWithoutLisChannel_UsingMethodDefaultChannel()
    {
        // Arrange
        var testOrderUid = Guid.NewGuid();
        var request = CreateRequest(1);
        var testOrder = CreateTestOrder(1, testOrderUid, null, TestOrderState.Approved);
        request.TestOrders.Add(testOrder);
        ConfigureMethodDefaultChannel(testOrder.MethodName, 2);

        var command = new ProcessLisExportCommand(
            Requests: new List<Domain.Request> { request },
            TestOrderUids: new List<Guid> { testOrderUid });

        ConfigureLisChannel(
            channelId: 2,
            automaticSampleExport: LisAutoExportTrigger.WhenTestIsApprovedOrRejected,
            includeOtherTestsInAutomaticExports: IncludeOtherTestsInAutomaticExports.NotUsed,
            whatToExport: WhatToExport.AllStatuses,
            includeLocallyEnteredSamples: true);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _senderMock.Verify(x => x.Send(
            It.Is<SendSampleTestResultsToLisCommand>(cmd =>
                cmd.TestOrderIds != null &&
                cmd.TestOrderIds.Count == 1 &&
                cmd.TestOrderIds.Contains(1) &&
                cmd.LisChannelId == 2 &&
                cmd.IsManualExport == false),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ProcessTestsWithExplicitLisChannel_OverMethodDefaultChannel()
    {
        // Arrange
        var testOrderUid = Guid.NewGuid();
        var request = CreateRequest(1);
        var testOrder = CreateTestOrder(1, testOrderUid, 1, TestOrderState.Approved);
        request.TestOrders.Add(testOrder);
        ConfigureMethodDefaultChannel(testOrder.MethodName, 2);

        var command = new ProcessLisExportCommand(
            Requests: new List<Domain.Request> { request },
            TestOrderUids: new List<Guid> { testOrderUid });

        ConfigureLisChannel(
            channelId: 1,
            automaticSampleExport: LisAutoExportTrigger.WhenTestIsApprovedOrRejected,
            includeOtherTestsInAutomaticExports: IncludeOtherTestsInAutomaticExports.NotUsed,
            whatToExport: WhatToExport.AllStatuses,
            includeLocallyEnteredSamples: true);
        ConfigureLisChannel(
            channelId: 2,
            automaticSampleExport: LisAutoExportTrigger.WhenTestIsApprovedOrRejected,
            includeOtherTestsInAutomaticExports: IncludeOtherTestsInAutomaticExports.NotUsed,
            whatToExport: WhatToExport.AllStatuses,
            includeLocallyEnteredSamples: true);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _senderMock.Verify(x => x.Send(
            It.Is<SendSampleTestResultsToLisCommand>(cmd =>
                cmd.TestOrderIds != null &&
                cmd.TestOrderIds.Count == 1 &&
                cmd.TestOrderIds.Contains(1) &&
                cmd.LisChannelId == 1),
            It.IsAny<CancellationToken>()), Times.Once);

        _senderMock.Verify(x => x.Send(
            It.Is<SendSampleTestResultsToLisCommand>(cmd => cmd.LisChannelId == 2),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Trait("ProductRequirement", "PR_1916")]
    [Trait("ModuleRequirement", "MR_5580")]
    [Fact]
    [Trait("ModuleRequirement", "MR_5580")]
    [Trait("ProductRequirement", "PR_1916")]
    public async Task Handle_Should_DoNothing_When_AutomaticSampleExportIsNotUsed()
    {
        // Arrange
        var testOrderUid = Guid.NewGuid();
        var request = CreateRequest(1);
        var testOrder = CreateTestOrder(1, testOrderUid, 0, TestOrderState.Approved);
        request.TestOrders.Add(testOrder);

        var command = new ProcessLisExportCommand(
            Requests: new List<Domain.Request> { request },
            TestOrderUids: new List<Guid> { testOrderUid });

        ConfigureLisChannel(
            channelId: 0,
            automaticSampleExport: LisAutoExportTrigger.NotUsed);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _senderMock.Verify(x => x.Send(It.IsAny<SendSampleTestResultsToLisCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Trait("ProductRequirement", "PR_1063")]
    [Trait("ModuleRequirement", "MR_5583")]
    [Fact]
    [Trait("ModuleRequirement", "MR_5580")]
    [Trait("ProductRequirement", "PR_1916")]
    public async Task Handle_Should_ExportAllChannelStatuses_When_TestApprovedOrRejectedAndAllStatuses()
    {
        // Arrange
        var triggeredTestUid = Guid.NewGuid();
        var otherTestUid = Guid.NewGuid();

        var request = CreateRequest(1);
        var triggeredTest = CreateTestOrder(1, triggeredTestUid, 0, TestOrderState.Approved);
        var otherTest = CreateTestOrder(2, otherTestUid, 0, TestOrderState.PendingApproval);
        request.TestOrders.Add(triggeredTest);
        request.TestOrders.Add(otherTest);

        var command = new ProcessLisExportCommand(
            Requests: new List<Domain.Request> { request },
            TestOrderUids: new List<Guid> { triggeredTestUid });

        ConfigureLisChannel(
            channelId: 0,
            automaticSampleExport: LisAutoExportTrigger.WhenTestIsApprovedOrRejected,
            includeOtherTestsInAutomaticExports: IncludeOtherTestsInAutomaticExports.NotUsed,
            whatToExport: WhatToExport.AllStatuses,
            includeLocallyEnteredSamples: true);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        VerifyExport(0, 1);
    }

    [Trait("ProductRequirement", "PR_1916")]
    [Trait("ModuleRequirement", "MR_5587")]
    [Fact]
    public async Task Handle_Should_ExpandToAllTestsForSample()
    {
        // Arrange
        var triggeredTestUid = Guid.NewGuid();
        var otherTestUid = Guid.NewGuid();

        var request = CreateRequest(1);
        var triggeredTest = CreateTestOrder(1, triggeredTestUid, 0, TestOrderState.Approved);
        var otherTest = CreateTestOrder(2, otherTestUid, 0, TestOrderState.PendingApproval);
        request.TestOrders.Add(triggeredTest);
        request.TestOrders.Add(otherTest);

        var command = new ProcessLisExportCommand(
            Requests: new List<Domain.Request> { request },
            TestOrderUids: new List<Guid> { triggeredTestUid });

        ConfigureLisChannel(
            channelId: 0,
            automaticSampleExport: LisAutoExportTrigger.WhenTestIsApprovedOrRejected,
            includeOtherTestsInAutomaticExports: IncludeOtherTestsInAutomaticExports.AllTestsForSample,
            whatToExport: WhatToExport.AllStatuses,
            includeLocallyEnteredSamples: true);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _senderMock.Verify(x => x.Send(
            It.Is<SendSampleTestResultsToLisCommand>(cmd =>
                cmd.TestOrderIds != null &&
                cmd.TestOrderIds.Count == 2 &&
                cmd.TestOrderIds.Contains(1) &&
                cmd.TestOrderIds.Contains(2)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Trait("ProductRequirement", "PR_1916")]
    [Trait("ModuleRequirement", "MR_5587")]
    [Fact]
    public async Task Handle_Should_ExpandToAllTestsExceptReported()
    {
        // Arrange
        var triggeredTestUid = Guid.NewGuid();
        var pendingTestUid = Guid.NewGuid();
        var reportedTestUid = Guid.NewGuid();

        var request = CreateRequest(1);
        var triggeredTest = CreateTestOrder(1, triggeredTestUid, 0, TestOrderState.Approved);
        var pendingTest = CreateTestOrder(2, pendingTestUid, 0, TestOrderState.PendingApproval);
        var reportedTest = CreateTestOrder(3, reportedTestUid, 0, TestOrderState.Reported);
        request.TestOrders.Add(triggeredTest);
        request.TestOrders.Add(pendingTest);
        request.TestOrders.Add(reportedTest);

        var command = new ProcessLisExportCommand(
            Requests: new List<Domain.Request> { request },
            TestOrderUids: new List<Guid> { triggeredTestUid });

        ConfigureLisChannel(
            channelId: 0,
            automaticSampleExport: LisAutoExportTrigger.WhenTestIsApprovedOrRejected,
            includeOtherTestsInAutomaticExports: IncludeOtherTestsInAutomaticExports.AllTestsForSampleExceptAlreadyReported,
            whatToExport: WhatToExport.AllStatuses,
            includeLocallyEnteredSamples: true);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _senderMock.Verify(x => x.Send(
            It.Is<SendSampleTestResultsToLisCommand>(cmd =>
                cmd.TestOrderIds != null &&
                cmd.TestOrderIds.Count == 2 &&
                cmd.TestOrderIds.Contains(1) &&
                cmd.TestOrderIds.Contains(2) &&
                !cmd.TestOrderIds.Contains(3)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Trait("ProductRequirement", "PR_1063")]
    [Trait("ModuleRequirement", "MR_5583")]
    [Fact]
    public async Task Handle_Should_FilterByOnlyApprovedResults()
    {
        // Arrange
        var approvedTestUid = Guid.NewGuid();
        var rejectedTestUid = Guid.NewGuid();

        var request = CreateRequest(1);
        var approvedTest = CreateTestOrder(1, approvedTestUid, 0, TestOrderState.Approved);
        var rejectedTest = CreateTestOrder(2, rejectedTestUid, 0, TestOrderState.Rejected);
        request.TestOrders.Add(approvedTest);
        request.TestOrders.Add(rejectedTest);

        var command = new ProcessLisExportCommand(
            Requests: new List<Domain.Request> { request },
            TestOrderUids: new List<Guid> { approvedTestUid, rejectedTestUid });

        ConfigureLisChannel(
            channelId: 0,
            automaticSampleExport: LisAutoExportTrigger.WhenTestIsApprovedOrRejected,
            includeOtherTestsInAutomaticExports: IncludeOtherTestsInAutomaticExports.NotUsed,
            whatToExport: WhatToExport.OnlyApprovedResults,
            includeLocallyEnteredSamples: true);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _senderMock.Verify(x => x.Send(
            It.Is<SendSampleTestResultsToLisCommand>(cmd =>
                cmd.TestOrderIds != null &&
                cmd.TestOrderIds.Count == 1 &&
                cmd.TestOrderIds.Contains(1) &&
                !cmd.TestOrderIds.Contains(2)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Trait("ProductRequirement", "PR_1063")]
    [Trait("ModuleRequirement", "MR_5583")]
    [Fact]
    public async Task Handle_Should_FilterByOnlyApprovedAndRejectedResults()
    {
        // Arrange
        var approvedTestUid = Guid.NewGuid();
        var rejectedTestUid = Guid.NewGuid();
        var pendingTestUid = Guid.NewGuid();

        var request = CreateRequest(1);
        var approvedTest = CreateTestOrder(1, approvedTestUid, 0, TestOrderState.Approved);
        var rejectedTest = CreateTestOrder(2, rejectedTestUid, 0, TestOrderState.Rejected);
        var pendingTest = CreateTestOrder(3, pendingTestUid, 0, TestOrderState.PendingApproval);
        request.TestOrders.Add(approvedTest);
        request.TestOrders.Add(rejectedTest);
        request.TestOrders.Add(pendingTest);

        var command = new ProcessLisExportCommand(
            Requests: new List<Domain.Request> { request },
            TestOrderUids: new List<Guid> { approvedTestUid, rejectedTestUid, pendingTestUid });

        ConfigureLisChannel(
            channelId: 0,
            automaticSampleExport: LisAutoExportTrigger.WhenTestIsApprovedOrRejected,
            includeOtherTestsInAutomaticExports: IncludeOtherTestsInAutomaticExports.NotUsed,
            whatToExport: WhatToExport.OnlyApprovedAndRejectedResults,
            includeLocallyEnteredSamples: true);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _senderMock.Verify(x => x.Send(
            It.Is<SendSampleTestResultsToLisCommand>(cmd =>
                cmd.TestOrderIds != null &&
                cmd.TestOrderIds.Count == 2 &&
                cmd.TestOrderIds.Contains(1) &&
                cmd.TestOrderIds.Contains(2) &&
                !cmd.TestOrderIds.Contains(3)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Trait("ProductRequirement", "PR_1916")]
    [Trait("ModuleRequirement", "MR_5580")]
    [Theory]
    [InlineData(true, 2)]
    [InlineData(false, 0)]
    [Trait("ModuleRequirement", "MR_5580")]
    [Trait("ProductRequirement", "PR_1916")]
    public async Task Handle_Should_ExportCompletedSamples_When_WhenSampleIsApprovedOrRejected(
        bool isCompleted,
        int expectedTestCount)
    {
        // Arrange
        var testUid = Guid.NewGuid();

        var request = CreateRequest(1);
        var test1 = CreateTestOrder(1, testUid, 0, TestOrderState.Approved);
        var test2 = CreateTestOrder(2, Guid.NewGuid(), 0,
            isCompleted ? TestOrderState.Rejected : TestOrderState.PendingApproval);
        request.TestOrders.Add(test1);
        request.TestOrders.Add(test2);

        var command = new ProcessLisExportCommand(
            Requests: new List<Domain.Request> { request },
            TestOrderUids: new List<Guid> { testUid });

        ConfigureLisChannel(
            channelId: 0,
            automaticSampleExport: LisAutoExportTrigger.WhenSampleIsApprovedOrRejected,
            whatToExport: WhatToExport.AllStatuses,
            includeLocallyEnteredSamples: true);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        if (expectedTestCount > 0)
        {
            _senderMock.Verify(x => x.Send(
                It.Is<SendSampleTestResultsToLisCommand>(cmd =>
                    cmd.TestOrderIds != null &&
                    cmd.TestOrderIds.Count == expectedTestCount),
                It.IsAny<CancellationToken>()), Times.Once);
        }
        else
        {
            _senderMock.Verify(x => x.Send(It.IsAny<SendSampleTestResultsToLisCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }

    [Trait("ProductRequirement", "PR_1916")]
    [Trait("ModuleRequirement", "MR_5587")]
    [Theory]
    [InlineData(IncludeOtherTestsInAutomaticExports.AllTestsForSample, new[] { 1, 2, 3 })]
    [InlineData(IncludeOtherTestsInAutomaticExports.AllTestsForSampleExceptAlreadyReported, new[] { 1, 2 })]
    [Trait("ModuleRequirement", "MR_5587")]
    [Trait("ProductRequirement", "PR_1916")]
    public async Task Handle_Should_ApplyIncludeOtherTests_When_SampleIsApprovedOrRejected(
        IncludeOtherTestsInAutomaticExports includeOtherTests,
        int[] expectedTestOrderIds)
    {
        // Arrange
        var triggeredTestUid = Guid.NewGuid();

        var request = CreateRequest(1);
        request.TestOrders.Add(CreateTestOrder(1, triggeredTestUid, 0, TestOrderState.Approved));
        request.TestOrders.Add(CreateTestOrder(2, Guid.NewGuid(), 0, TestOrderState.Rejected));
        request.TestOrders.Add(CreateTestOrder(3, Guid.NewGuid(), 0, TestOrderState.Reported));

        var command = new ProcessLisExportCommand(
            Requests: new List<Domain.Request> { request },
            TestOrderUids: new List<Guid> { triggeredTestUid });

        ConfigureLisChannel(
            channelId: 0,
            automaticSampleExport: LisAutoExportTrigger.WhenSampleIsApprovedOrRejected,
            includeOtherTestsInAutomaticExports: includeOtherTests,
            whatToExport: WhatToExport.AllStatuses,
            includeLocallyEnteredSamples: true);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        VerifyExport(0, expectedTestOrderIds);
    }

    [Fact]
    public async Task Handle_Should_ProcessMultipleChannelsSeparately()
    {
        // Arrange
        var channel0TestUid = Guid.NewGuid();
        var channel1TestUid = Guid.NewGuid();

        var request = CreateRequest(1);
        var channel0Test = CreateTestOrder(1, channel0TestUid, 0, TestOrderState.Approved);
        var channel1Test = CreateTestOrder(2, channel1TestUid, 1, TestOrderState.Approved);
        request.TestOrders.Add(channel0Test);
        request.TestOrders.Add(channel1Test);

        var command = new ProcessLisExportCommand(
            Requests: new List<Domain.Request> { request },
            TestOrderUids: new List<Guid> { channel0TestUid, channel1TestUid });

        ConfigureLisChannel(
            channelId: 0,
            automaticSampleExport: LisAutoExportTrigger.WhenTestIsApprovedOrRejected,
            includeOtherTestsInAutomaticExports: IncludeOtherTestsInAutomaticExports.NotUsed,
            whatToExport: WhatToExport.AllStatuses,
            includeLocallyEnteredSamples: true);
        ConfigureLisChannel(
            channelId: 1,
            automaticSampleExport: LisAutoExportTrigger.WhenTestIsApprovedOrRejected,
            includeOtherTestsInAutomaticExports: IncludeOtherTestsInAutomaticExports.NotUsed,
            whatToExport: WhatToExport.AllStatuses,
            includeLocallyEnteredSamples: true);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _senderMock.Verify(x => x.Send(
            It.Is<SendSampleTestResultsToLisCommand>(cmd =>
                cmd.LisChannelId == 0 &&
                cmd.TestOrderIds != null &&
                cmd.TestOrderIds.Count == 1 &&
                cmd.TestOrderIds.Contains(1)),
            It.IsAny<CancellationToken>()), Times.Once);

        _senderMock.Verify(x => x.Send(
            It.Is<SendSampleTestResultsToLisCommand>(cmd =>
                cmd.LisChannelId == 1 &&
                cmd.TestOrderIds != null &&
                cmd.TestOrderIds.Count == 1 &&
                cmd.TestOrderIds.Contains(2)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Trait("ProductRequirement", "PR_1916")]
    [Trait("ModuleRequirement", "MR_5587")]
    [Fact]
    public async Task Handle_Should_OnlyExpandToAllTestsForSampleWithinTriggerLisChannel()
    {
        // Arrange
        var triggeredTestUid = Guid.NewGuid();
        var sameChannelTestUid = Guid.NewGuid();
        var differentChannelTestUid = Guid.NewGuid();

        var request = CreateRequest(1);
        var triggeredTest = CreateTestOrder(1, triggeredTestUid, 0, TestOrderState.Approved);
        var sameChannelTest = CreateTestOrder(2, sameChannelTestUid, 0, TestOrderState.PendingApproval);
        var differentChannelTest = CreateTestOrder(3, differentChannelTestUid, 1, TestOrderState.NotStarted);
        request.TestOrders.Add(triggeredTest);
        request.TestOrders.Add(sameChannelTest);
        request.TestOrders.Add(differentChannelTest);

        var command = new ProcessLisExportCommand(
            Requests: new List<Domain.Request> { request },
            TestOrderUids: new List<Guid> { triggeredTestUid });

        ConfigureLisChannel(
            channelId: 0,
            automaticSampleExport: LisAutoExportTrigger.WhenTestIsApprovedOrRejected,
            includeOtherTestsInAutomaticExports: IncludeOtherTestsInAutomaticExports.AllTestsForSample,
            whatToExport: WhatToExport.AllStatuses,
            includeLocallyEnteredSamples: true);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        VerifyExport(0, 1, 2);
        VerifyNoExport(1);
    }

    [Trait("ProductRequirement", "PR_1916")]
    [Trait("ModuleRequirement", "MR_5587")]
    [Fact]
    public async Task Handle_Should_OnlyExpandNullChannelTests_WhenResolvedMethodDefaultMatchesTriggerChannel()
    {
        // Arrange
        var triggeredTestUid = Guid.NewGuid();

        var request = CreateRequest(1);
        var triggeredTest = CreateTestOrder(1, triggeredTestUid, 2, TestOrderState.Approved);
        var sameDefaultChannelTest = CreateTestOrder(2, Guid.NewGuid(), null, TestOrderState.PendingApproval);
        var differentDefaultChannelTest = CreateTestOrder(3, Guid.NewGuid(), null, TestOrderState.NotStarted);
        request.TestOrders.Add(triggeredTest);
        request.TestOrders.Add(sameDefaultChannelTest);
        request.TestOrders.Add(differentDefaultChannelTest);
        ConfigureMethodDefaultChannel(sameDefaultChannelTest.MethodName, 2);
        ConfigureMethodDefaultChannel(differentDefaultChannelTest.MethodName, 1);

        var command = new ProcessLisExportCommand(
            Requests: new List<Domain.Request> { request },
            TestOrderUids: new List<Guid> { triggeredTestUid });

        ConfigureLisChannel(
            channelId: 2,
            automaticSampleExport: LisAutoExportTrigger.WhenTestIsApprovedOrRejected,
            includeOtherTestsInAutomaticExports: IncludeOtherTestsInAutomaticExports.AllTestsForSample,
            whatToExport: WhatToExport.AllStatuses,
            includeLocallyEnteredSamples: true);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        VerifyExport(2, 1, 2);
        VerifyNoExport(1);
    }

    [Trait("ProductRequirement", "PR_1916")]
    [Trait("ModuleRequirement", "MR_5587")]
    [Fact]
    public async Task Handle_Should_ExportCompletedSampleNullChannelTestsAcrossResolvedDefaultChannels()
    {
        // Arrange
        var triggeredTestUid = Guid.NewGuid();

        var request = CreateRequest(1);
        var triggeredTest = CreateTestOrder(1, triggeredTestUid, 2, TestOrderState.Approved);
        var sameDefaultChannelTest = CreateTestOrder(2, Guid.NewGuid(), null, TestOrderState.Rejected);
        var differentDefaultChannelTest = CreateTestOrder(3, Guid.NewGuid(), null, TestOrderState.Approved);
        request.TestOrders.Add(triggeredTest);
        request.TestOrders.Add(sameDefaultChannelTest);
        request.TestOrders.Add(differentDefaultChannelTest);
        ConfigureMethodDefaultChannel(sameDefaultChannelTest.MethodName, 2);
        ConfigureMethodDefaultChannel(differentDefaultChannelTest.MethodName, 1);

        var command = new ProcessLisExportCommand(
            Requests: new List<Domain.Request> { request },
            TestOrderUids: new List<Guid> { triggeredTestUid });

        ConfigureLisChannel(
            channelId: 2,
            automaticSampleExport: LisAutoExportTrigger.WhenSampleIsApprovedOrRejected,
            includeOtherTestsInAutomaticExports: IncludeOtherTestsInAutomaticExports.AllTestsForSample,
            whatToExport: WhatToExport.AllStatuses,
            includeLocallyEnteredSamples: true);
        ConfigureLisChannel(
            channelId: 1,
            automaticSampleExport: LisAutoExportTrigger.WhenSampleIsApprovedOrRejected,
            whatToExport: WhatToExport.AllStatuses,
            includeLocallyEnteredSamples: true);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        VerifyExport(2, 1, 2);
        VerifyExport(1, 3);
    }

    [Trait("ProductRequirement", "PR_1916")]
    [Trait("ModuleRequirement", "MR_5580")]
    [Theory]
    [InlineData(TestOrderState.Approved)]
    [InlineData(TestOrderState.Rejected)]
    public async Task Handle_Should_ExportEligibleTestLevelChannelsInAffectedRequest_When_ChannelTestIsApprovedOrRejected(TestOrderState triggerState)
    {
        // Arrange
        var triggeredTestUid = Guid.NewGuid();
        var request = CreateRequest(1);
        request.TestOrders.Add(CreateTestOrder(1, triggeredTestUid, 1, triggerState));
        request.TestOrders.Add(CreateTestOrder(2, Guid.NewGuid(), 1, TestOrderState.Approved));
        request.TestOrders.Add(CreateTestOrder(3, Guid.NewGuid(), 1, TestOrderState.Rejected));
        request.TestOrders.Add(CreateTestOrder(4, Guid.NewGuid(), 2, TestOrderState.PendingApproval));
        request.TestOrders.Add(CreateTestOrder(5, Guid.NewGuid(), 2, TestOrderState.Reported));
        request.TestOrders.Add(CreateTestOrder(6, Guid.NewGuid(), 2, TestOrderState.PendingApproval));

        var command = new ProcessLisExportCommand(
            Requests: new List<Domain.Request> { request },
            TestOrderUids: new List<Guid> { triggeredTestUid });

        ConfigureLisChannel(
            channelId: 1,
            automaticSampleExport: LisAutoExportTrigger.WhenTestIsApprovedOrRejected,
            includeOtherTestsInAutomaticExports: IncludeOtherTestsInAutomaticExports.AllTestsForSample,
            whatToExport: WhatToExport.AllStatuses,
            includeLocallyEnteredSamples: true);
        ConfigureLisChannel(
            channelId: 2,
            automaticSampleExport: LisAutoExportTrigger.WhenTestIsApprovedOrRejected,
            whatToExport: WhatToExport.AllStatuses,
            includeLocallyEnteredSamples: true);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        VerifyExport(1, 1, 2, 3);
        VerifyNoExport(2);
    }

    [Trait("ProductRequirement", "PR_1916")]
    [Trait("ModuleRequirement", "MR_5580")]
    [Theory]
    [InlineData(TestOrderState.Approved)]
    [InlineData(TestOrderState.Rejected)]
    public async Task Handle_Should_ExportCompletedSampleTestsAcrossLisChannels_When_ChannelSampleIsApprovedOrRejected(TestOrderState triggerState)
    {
        // Arrange
        var triggeredTestUid = Guid.NewGuid();
        var request = CreateRequest(1);
        request.TestOrders.Add(CreateTestOrder(1, triggeredTestUid, 1, triggerState));
        request.TestOrders.Add(CreateTestOrder(2, Guid.NewGuid(), 1, TestOrderState.Approved));
        request.TestOrders.Add(CreateTestOrder(3, Guid.NewGuid(), 1, TestOrderState.Rejected));
        request.TestOrders.Add(CreateTestOrder(4, Guid.NewGuid(), 2, TestOrderState.Approved));
        request.TestOrders.Add(CreateTestOrder(5, Guid.NewGuid(), 2, TestOrderState.Rejected));
        request.TestOrders.Add(CreateTestOrder(6, Guid.NewGuid(), 2, TestOrderState.Reported));

        var command = new ProcessLisExportCommand(
            Requests: new List<Domain.Request> { request },
            TestOrderUids: new List<Guid> { triggeredTestUid });

        ConfigureLisChannel(
            channelId: 1,
            automaticSampleExport: LisAutoExportTrigger.WhenSampleIsApprovedOrRejected,
            includeOtherTestsInAutomaticExports: IncludeOtherTestsInAutomaticExports.AllTestsForSample,
            whatToExport: WhatToExport.AllStatuses,
            includeLocallyEnteredSamples: true);
        ConfigureLisChannel(
            channelId: 2,
            automaticSampleExport: LisAutoExportTrigger.WhenSampleIsApprovedOrRejected,
            includeOtherTestsInAutomaticExports: IncludeOtherTestsInAutomaticExports.AllTestsForSample,
            whatToExport: WhatToExport.AllStatuses,
            includeLocallyEnteredSamples: true);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        VerifyExport(1, 1, 2, 3);
        VerifyExport(2, 4, 5, 6);
    }

    [Fact]
    public async Task Handle_Should_NotExportPulledChannel_When_DestinationChannelWasNotCurrentTrigger()
    {
        // Arrange
        var channel2TriggeredTestUid = Guid.NewGuid();
        var request = CreateRequest(1);
        request.TestOrders.Add(CreateTestOrder(1, Guid.NewGuid(), 1, TestOrderState.Approved));
        request.TestOrders.Add(CreateTestOrder(2, channel2TriggeredTestUid, 2, TestOrderState.Approved));
        request.TestOrders.Add(CreateTestOrder(3, Guid.NewGuid(), 1, TestOrderState.PendingApproval));

        var command = new ProcessLisExportCommand(
            Requests: new List<Domain.Request> { request },
            TestOrderUids: new List<Guid> { channel2TriggeredTestUid });

        ConfigureLisChannel(
            channelId: 1,
            automaticSampleExport: LisAutoExportTrigger.WhenSampleIsApprovedOrRejected,
            whatToExport: WhatToExport.OnlyApprovedResults,
            includeLocallyEnteredSamples: true);
        ConfigureLisChannel(
            channelId: 2,
            automaticSampleExport: LisAutoExportTrigger.WhenTestIsApprovedOrRejected,
            includeOtherTestsInAutomaticExports: IncludeOtherTestsInAutomaticExports.AllTestsForSample,
            whatToExport: WhatToExport.AllStatuses,
            includeLocallyEnteredSamples: true);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        VerifyNoExport(1);
        VerifyExport(2, 2);
    }

    [Fact]
    public async Task Handle_Should_NotRunSampleLevelExport_When_TriggeredChannelUsesTestLevelExport()
    {
        // Arrange
        var triggeredTestUid = Guid.NewGuid();
        var request = CreateRequest(1);
        request.TestOrders.Add(CreateTestOrder(1, triggeredTestUid, 1, TestOrderState.Approved));
        request.TestOrders.Add(CreateTestOrder(2, Guid.NewGuid(), 1, TestOrderState.Rejected));
        request.TestOrders.Add(CreateTestOrder(3, Guid.NewGuid(), 2, TestOrderState.Approved));
        request.TestOrders.Add(CreateTestOrder(4, Guid.NewGuid(), 2, TestOrderState.Rejected));

        var command = new ProcessLisExportCommand(
            Requests: new List<Domain.Request> { request },
            TestOrderUids: new List<Guid> { triggeredTestUid });

        ConfigureLisChannel(
            channelId: 1,
            automaticSampleExport: LisAutoExportTrigger.WhenTestIsApprovedOrRejected,
            includeOtherTestsInAutomaticExports: IncludeOtherTestsInAutomaticExports.AllTestsForSample,
            whatToExport: WhatToExport.AllStatuses,
            includeLocallyEnteredSamples: true);
        ConfigureLisChannel(
            channelId: 2,
            automaticSampleExport: LisAutoExportTrigger.WhenSampleIsApprovedOrRejected,
            includeOtherTestsInAutomaticExports: IncludeOtherTestsInAutomaticExports.AllTestsForSample,
            whatToExport: WhatToExport.AllStatuses,
            includeLocallyEnteredSamples: true);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        VerifyExport(1, 1, 2);
        VerifyNoExport(2);
    }

    [Trait("ProductRequirement", "PR_1063")]
    [Trait("ModuleRequirement", "MR_5583")]
    [Theory]
    [InlineData(LisAutoExportTrigger.WhenTestIsApprovedOrRejected)]
    [InlineData(LisAutoExportTrigger.WhenSampleIsApprovedOrRejected)]
    public async Task Handle_Should_ApplyDestinationChannelStatusSettings_When_AffectedRequestIncludesStricterChannel(
        LisAutoExportTrigger automaticSampleExport)
    {
        // Arrange
        var triggeredTestUid = Guid.NewGuid();
        var request = CreateRequest(1);
        request.TestOrders.Add(CreateTestOrder(1, triggeredTestUid, 1, TestOrderState.Approved));
        request.TestOrders.Add(CreateTestOrder(2, Guid.NewGuid(), 1, TestOrderState.Rejected));
        request.TestOrders.Add(CreateTestOrder(3, Guid.NewGuid(), 1, TestOrderState.Reported));
        request.TestOrders.Add(CreateTestOrder(4, Guid.NewGuid(), 2, TestOrderState.Approved));
        request.TestOrders.Add(CreateTestOrder(5, Guid.NewGuid(), 2, TestOrderState.Rejected));
        request.TestOrders.Add(CreateTestOrder(6, Guid.NewGuid(), 2, TestOrderState.Reported));

        var command = new ProcessLisExportCommand(
            Requests: new List<Domain.Request> { request },
            TestOrderUids: new List<Guid> { triggeredTestUid });

        ConfigureLisChannel(
            channelId: 1,
            automaticSampleExport: automaticSampleExport,
            includeOtherTestsInAutomaticExports: IncludeOtherTestsInAutomaticExports.AllTestsForSample,
            whatToExport: WhatToExport.AllStatuses,
            includeLocallyEnteredSamples: true);
        ConfigureLisChannel(
            channelId: 2,
            automaticSampleExport: automaticSampleExport,
            whatToExport: WhatToExport.OnlyApprovedResults,
            includeLocallyEnteredSamples: true);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        VerifyExport(1, 1, 2, 3);

        if (automaticSampleExport == LisAutoExportTrigger.WhenSampleIsApprovedOrRejected)
        {
            VerifyExport(2, 4);
        }
        else
        {
            VerifyNoExport(2);
        }
    }

    [Trait("ProductRequirement", "PR_1063")]
    [Trait("ModuleRequirement", "MR_5583")]
    [Theory]
    [InlineData(LisAutoExportTrigger.WhenTestIsApprovedOrRejected)]
    [InlineData(LisAutoExportTrigger.WhenSampleIsApprovedOrRejected)]
    public async Task Handle_Should_ApplyDestinationChannelStatusSettings_When_AffectedRequestIncludesMorePermissiveChannel(
        LisAutoExportTrigger automaticSampleExport)
    {
        // Arrange
        var triggeredTestUid = Guid.NewGuid();
        var request = CreateRequest(1);
        request.TestOrders.Add(CreateTestOrder(1, triggeredTestUid, 1, TestOrderState.Approved));
        request.TestOrders.Add(CreateTestOrder(2, Guid.NewGuid(), 1, TestOrderState.Rejected));
        request.TestOrders.Add(CreateTestOrder(3, Guid.NewGuid(), 1, TestOrderState.Reported));
        request.TestOrders.Add(CreateTestOrder(4, Guid.NewGuid(), 2, TestOrderState.Approved));
        request.TestOrders.Add(CreateTestOrder(5, Guid.NewGuid(), 2, TestOrderState.Rejected));
        request.TestOrders.Add(CreateTestOrder(6, Guid.NewGuid(), 2, TestOrderState.Reported));

        var command = new ProcessLisExportCommand(
            Requests: new List<Domain.Request> { request },
            TestOrderUids: new List<Guid> { triggeredTestUid });

        ConfigureLisChannel(
            channelId: 1,
            automaticSampleExport: automaticSampleExport,
            includeOtherTestsInAutomaticExports: IncludeOtherTestsInAutomaticExports.AllTestsForSample,
            whatToExport: WhatToExport.OnlyApprovedResults,
            includeLocallyEnteredSamples: true);
        ConfigureLisChannel(
            channelId: 2,
            automaticSampleExport: automaticSampleExport,
            whatToExport: WhatToExport.AllStatuses,
            includeLocallyEnteredSamples: true);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        VerifyExport(1, 1);

        if (automaticSampleExport == LisAutoExportTrigger.WhenSampleIsApprovedOrRejected)
        {
            VerifyExport(2, 4, 5, 6);
        }
        else
        {
            VerifyNoExport(2);
        }
    }

    [Trait("ProductRequirement", "PR_1063")]
    [Trait("ModuleRequirement", "MR_5583")]
    [Fact]
    public async Task Handle_Should_ExportTestLevelChannels_When_SampleTriggerChannelExportsOnlyApprovedAndRejectedResults()
    {
        // Arrange
        var triggeredTestUid = Guid.NewGuid();
        var request = CreateRequest(1);
        request.TestOrders.Add(CreateTestOrder(1, triggeredTestUid, 1, TestOrderState.Approved));
        request.TestOrders.Add(CreateTestOrder(2, Guid.NewGuid(), 1, TestOrderState.Rejected));
        request.TestOrders.Add(CreateTestOrder(3, Guid.NewGuid(), 1, TestOrderState.Reported));
        request.TestOrders.Add(CreateTestOrder(4, Guid.NewGuid(), 2, TestOrderState.Approved));
        request.TestOrders.Add(CreateTestOrder(5, Guid.NewGuid(), 2, TestOrderState.Rejected));

        var command = new ProcessLisExportCommand(
            Requests: new List<Domain.Request> { request },
            TestOrderUids: new List<Guid> { triggeredTestUid });

        ConfigureLisChannel(
            channelId: 1,
            automaticSampleExport: LisAutoExportTrigger.WhenSampleIsApprovedOrRejected,
            includeOtherTestsInAutomaticExports: IncludeOtherTestsInAutomaticExports.AllTestsForSample,
            whatToExport: WhatToExport.OnlyApprovedAndRejectedResults,
            includeLocallyEnteredSamples: true);
        ConfigureLisChannel(
            channelId: 2,
            automaticSampleExport: LisAutoExportTrigger.WhenTestIsApprovedOrRejected,
            whatToExport: WhatToExport.AllStatuses,
            includeLocallyEnteredSamples: true);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        VerifyExport(1, 1, 2);
        VerifyNoExport(2);
    }

    [Trait("ProductRequirement", "PR_1916")]
    [Trait("ModuleRequirement", "MR_5587")]
    [Fact]
    public async Task Handle_Should_DeduplicateExpandedExports_When_MultipleSelectedTestOrdersTriggerDifferentChannels()
    {
        // Arrange
        var channel1TriggeredTestUid = Guid.NewGuid();
        var channel2TriggeredTestUid = Guid.NewGuid();
        var request = CreateRequest(1);
        request.TestOrders.Add(CreateTestOrder(1, channel1TriggeredTestUid, 1, TestOrderState.Approved));
        request.TestOrders.Add(CreateTestOrder(2, Guid.NewGuid(), 1, TestOrderState.Rejected));
        request.TestOrders.Add(CreateTestOrder(3, channel2TriggeredTestUid, 2, TestOrderState.Approved));
        request.TestOrders.Add(CreateTestOrder(4, Guid.NewGuid(), 2, TestOrderState.Rejected));

        var command = new ProcessLisExportCommand(
            Requests: new List<Domain.Request> { request },
            TestOrderUids: new List<Guid> { channel1TriggeredTestUid, channel2TriggeredTestUid });

        ConfigureLisChannel(
            channelId: 1,
            automaticSampleExport: LisAutoExportTrigger.WhenTestIsApprovedOrRejected,
            includeOtherTestsInAutomaticExports: IncludeOtherTestsInAutomaticExports.AllTestsForSample,
            whatToExport: WhatToExport.AllStatuses,
            includeLocallyEnteredSamples: true);
        ConfigureLisChannel(
            channelId: 2,
            automaticSampleExport: LisAutoExportTrigger.WhenTestIsApprovedOrRejected,
            includeOtherTestsInAutomaticExports: IncludeOtherTestsInAutomaticExports.AllTestsForSample,
            whatToExport: WhatToExport.AllStatuses,
            includeLocallyEnteredSamples: true);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        VerifyExport(1, 1, 2);
        VerifyExport(2, 3, 4);

        _senderMock.Verify(x => x.Send(It.IsAny<SendSampleTestResultsToLisCommand>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Trait("ProductRequirement", "PR_1063")]
    [Trait("ModuleRequirement", "MR_5583")]
    [Fact]
    public async Task Handle_Should_NotExport_When_AllTestsFilteredOut()
    {
        // Arrange
        var testOrderUid = Guid.NewGuid();
        var request = CreateRequest(1);
        var testOrder = CreateTestOrder(1, testOrderUid, 0, TestOrderState.Rejected);
        request.TestOrders.Add(testOrder);

        var command = new ProcessLisExportCommand(
            Requests: new List<Domain.Request> { request },
            TestOrderUids: new List<Guid> { testOrderUid });

        ConfigureLisChannel(
            channelId: 0,
            automaticSampleExport: LisAutoExportTrigger.WhenTestIsApprovedOrRejected,
            includeOtherTestsInAutomaticExports: IncludeOtherTestsInAutomaticExports.NotUsed,
            whatToExport: WhatToExport.OnlyApprovedResults,
            includeLocallyEnteredSamples: true);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _senderMock.Verify(x => x.Send(It.IsAny<SendSampleTestResultsToLisCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Trait("ProductRequirement", "PR_576")]
    [Trait("ModuleRequirement", "MR_5581")]
    [Fact]
    [Trait("ModuleRequirement", "MR_5581")]
    [Trait("ProductRequirement", "PR_576")]
    public async Task Handle_Should_ExportImportedSamples_When_LocallyEnteredSamplesAreExcluded()
    {
        // Arrange
        var testOrderUid = Guid.NewGuid();
        var request = CreateRequest(1, RequestSource.Import);
        var testOrder = CreateTestOrder(1, testOrderUid, 0, TestOrderState.Approved);
        request.TestOrders.Add(testOrder);

        var command = new ProcessLisExportCommand(
            Requests: new List<Domain.Request> { request },
            TestOrderUids: new List<Guid> { testOrderUid });

        ConfigureLisChannel(
            channelId: 0,
            automaticSampleExport: LisAutoExportTrigger.WhenTestIsApprovedOrRejected,
            includeOtherTestsInAutomaticExports: IncludeOtherTestsInAutomaticExports.NotUsed,
            whatToExport: WhatToExport.AllStatuses,
            includeLocallyEnteredSamples: false);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        VerifyExport(0, 1);
    }

    [Trait("ProductRequirement", "PR_576")]
    [Trait("ModuleRequirement", "MR_5581")]
    [Fact]
    [Trait("ModuleRequirement", "MR_5581")]
    [Trait("ProductRequirement", "PR_576")]
    public async Task Handle_Should_NotExportManualSamples_When_LocallyEnteredSamplesAreExcluded()
    {
        // Arrange
        var testOrderUid = Guid.NewGuid();
        var request = CreateRequest(1, RequestSource.Manual);
        var testOrder = CreateTestOrder(1, testOrderUid, 0, TestOrderState.Approved);
        request.TestOrders.Add(testOrder);

        var command = new ProcessLisExportCommand(
            Requests: new List<Domain.Request> { request },
            TestOrderUids: new List<Guid> { testOrderUid });

        ConfigureLisChannel(
            channelId: 0,
            automaticSampleExport: LisAutoExportTrigger.WhenTestIsApprovedOrRejected,
            includeOtherTestsInAutomaticExports: IncludeOtherTestsInAutomaticExports.NotUsed,
            whatToExport: WhatToExport.AllStatuses,
            includeLocallyEnteredSamples: false);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _senderMock.Verify(x => x.Send(It.IsAny<SendSampleTestResultsToLisCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ApplyExpansionAndFilteringInCorrectOrder()
    {
        // Arrange -
        var triggeredTestUid = Guid.NewGuid();

        var request = CreateRequest(1);
        var triggeredTest = CreateTestOrder(1, triggeredTestUid, 0, TestOrderState.Approved);
        var pendingTest = CreateTestOrder(2, Guid.NewGuid(), 0, TestOrderState.PendingApproval);
        var reportedTest = CreateTestOrder(3, Guid.NewGuid(), 0, TestOrderState.Reported);
        request.TestOrders.Add(triggeredTest);
        request.TestOrders.Add(pendingTest);
        request.TestOrders.Add(reportedTest);

        var command = new ProcessLisExportCommand(
            Requests: new List<Domain.Request> { request },
            TestOrderUids: new List<Guid> { triggeredTestUid });

        ConfigureLisChannel(
            channelId: 0,
            automaticSampleExport: LisAutoExportTrigger.WhenTestIsApprovedOrRejected,
            includeOtherTestsInAutomaticExports: IncludeOtherTestsInAutomaticExports.AllTestsForSampleExceptAlreadyReported,
            whatToExport: WhatToExport.OnlyApprovedResults,
            includeLocallyEnteredSamples: true);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _senderMock.Verify(x => x.Send(
            It.Is<SendSampleTestResultsToLisCommand>(cmd =>
                cmd.TestOrderIds != null &&
                cmd.TestOrderIds.Count == 1 &&
                cmd.TestOrderIds.Contains(1)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Trait("ProductRequirement", "PR_576")]
    [Trait("ModuleRequirement", "MR_5584")]
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [Trait("ModuleRequirement", "MR_5584")]
    [Trait("ProductRequirement", "PR_576")]
    public async Task Handle_Should_ApplyRetestFilters_WhenRejectedTestHasNewRetest(
        bool includeRejectedResults,
        bool includeNewTests)
    {
        // Arrange
        var rejectedTestUid = Guid.NewGuid();
        var newRetestUid = Guid.NewGuid();

        var request = CreateRequest(1);
        var rejectedTest = new TestOrder
        {
            Id = 1,
            Uid = rejectedTestUid,
            LisChannelId = 0,
            State = TestOrderState.Rejected,
            MethodName = "MethodA",
            TestName = "TestA"
        };
        var newRetest = new TestOrder
        {
            Id = 2,
            Uid = newRetestUid,
            LisChannelId = 0,
            State = TestOrderState.NotStarted,
            MethodName = "MethodA",
            TestName = "TestA"
        };
        request.TestOrders.Add(rejectedTest);
        request.TestOrders.Add(newRetest);

        var command = new ProcessLisExportCommand(
            Requests: new List<Domain.Request> { request },
            TestOrderUids: new List<Guid> { rejectedTestUid });

        ConfigureLisChannel(
            channelId: 0,
            automaticSampleExport: LisAutoExportTrigger.WhenTestIsApprovedOrRejected,
            includeOtherTestsInAutomaticExports: IncludeOtherTestsInAutomaticExports.NotUsed,
            whatToExport: WhatToExport.AllStatuses,
            includeLocallyEnteredSamples: true,
            duringRetestIncludeRejectedResults: includeRejectedResults,
            duringRetestIncludeNewResults: includeNewTests);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        if (!includeRejectedResults && !includeNewTests)
        {
            _senderMock.Verify(x => x.Send(It.IsAny<SendSampleTestResultsToLisCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        }
        else
        {
            _senderMock.Verify(x => x.Send(
                It.Is<SendSampleTestResultsToLisCommand>(cmd =>
                    cmd.TestOrderIds != null &&
                    cmd.TestOrderIds.Contains(1) == includeRejectedResults &&
                    cmd.TestOrderIds.Contains(2) == includeNewTests),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    [Trait("ProductRequirement", "PR_576")]
    [Trait("ModuleRequirement", "MR_5584")]
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [Trait("ModuleRequirement", "MR_5585")]
    [Trait("ProductRequirement", "PR_1307")]
    public async Task Handle_Should_ApplyRetestFilters_WhenNewRetestIsExpanded(
        bool includeRejectedResults,
        bool includeNewTests)
    {
        // Arrange - expanding to all tests brings in the new retest; rejected original is also present
        var approvedTestUid = Guid.NewGuid();
        var rejectedTestUid = Guid.NewGuid();
        var newRetestUid = Guid.NewGuid();

        var request = CreateRequest(1);
        var approvedTest = new TestOrder
        {
            Id = 1,
            Uid = approvedTestUid,
            LisChannelId = 0,
            State = TestOrderState.Approved,
            MethodName = "MethodB",
            TestName = "TestB"
        };
        var rejectedTest = new TestOrder
        {
            Id = 2,
            Uid = rejectedTestUid,
            LisChannelId = 0,
            State = TestOrderState.Rejected,
            MethodName = "MethodA",
            TestName = "TestA"
        };
        var newRetest = new TestOrder
        {
            Id = 3,
            Uid = newRetestUid,
            LisChannelId = 0,
            State = TestOrderState.NotStarted,
            MethodName = "MethodA",
            TestName = "TestA"
        };
        request.TestOrders.Add(approvedTest);
        request.TestOrders.Add(rejectedTest);
        request.TestOrders.Add(newRetest);

        var command = new ProcessLisExportCommand(
            Requests: new List<Domain.Request> { request },
            TestOrderUids: new List<Guid> { approvedTestUid });

        ConfigureLisChannel(
            channelId: 0,
            automaticSampleExport: LisAutoExportTrigger.WhenTestIsApprovedOrRejected,
            includeOtherTestsInAutomaticExports: IncludeOtherTestsInAutomaticExports.AllTestsForSample,
            whatToExport: WhatToExport.AllStatuses,
            includeLocallyEnteredSamples: true,
            duringRetestIncludeRejectedResults: includeRejectedResults,
            duringRetestIncludeNewResults: includeNewTests);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert - test 1 (approved, no retest) is always exported
        _senderMock.Verify(x => x.Send(
            It.Is<SendSampleTestResultsToLisCommand>(cmd =>
                cmd.TestOrderIds != null &&
                cmd.TestOrderIds.Contains(1) &&
                cmd.TestOrderIds.Contains(2) == includeRejectedResults &&
                cmd.TestOrderIds.Contains(3) == includeNewTests),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Trait("ProductRequirement", "PR_1063")]
    [Trait("ModuleRequirement", "MR_5583")]
    [Fact]
    public async Task Handle_Should_ApplyWhatToExport_When_ManualExport()
    {
        // Arrange
        var approvedTestUid = Guid.NewGuid();
        var rejectedTestUid = Guid.NewGuid();
        var pendingTestUid = Guid.NewGuid();

        var request = CreateRequest(1);
        request.TestOrders.Add(CreateTestOrder(1, approvedTestUid, 0, TestOrderState.Approved));
        request.TestOrders.Add(CreateTestOrder(2, rejectedTestUid, 0, TestOrderState.Rejected));
        request.TestOrders.Add(CreateTestOrder(3, pendingTestUid, 0, TestOrderState.PendingApproval));

        var command = new ProcessLisExportCommand(
            Requests: new List<Domain.Request> { request },
            TestOrderUids: new List<Guid> { approvedTestUid, rejectedTestUid, pendingTestUid },
            IsManualExport: true);

        ConfigureLisChannel(
            channelId: 0,
            automaticSampleExport: LisAutoExportTrigger.NotUsed,
            whatToExport: WhatToExport.OnlyApprovedResults,
            includeLocallyEnteredSamples: true);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _senderMock.Verify(x => x.Send(
            It.Is<SendSampleTestResultsToLisCommand>(cmd =>
                cmd.TestOrderIds != null &&
                cmd.TestOrderIds.Count == 1 &&
                cmd.TestOrderIds.Contains(1) &&
                cmd.LisChannelId == 0 &&
                cmd.IsManualExport),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Trait("ProductRequirement", "PR_1916")]
    [Trait("ModuleRequirement", "MR_5587")]
    [Fact]
    public async Task Handle_Should_NotApplyIncludeOtherTestsExpansion_When_ManualExport()
    {
        // Arrange
        var selectedTestUid = Guid.NewGuid();
        var otherTestUid = Guid.NewGuid();

        var request = CreateRequest(1);
        request.TestOrders.Add(CreateTestOrder(1, selectedTestUid, 0, TestOrderState.Approved));
        request.TestOrders.Add(CreateTestOrder(2, otherTestUid, 0, TestOrderState.PendingApproval));

        var command = new ProcessLisExportCommand(
            Requests: new List<Domain.Request> { request },
            TestOrderUids: new List<Guid> { selectedTestUid },
            IsManualExport: true);

        ConfigureLisChannel(
            channelId: 0,
            automaticSampleExport: LisAutoExportTrigger.WhenTestIsApprovedOrRejected,
            includeOtherTestsInAutomaticExports: IncludeOtherTestsInAutomaticExports.AllTestsForSample,
            whatToExport: WhatToExport.AllStatuses,
            includeLocallyEnteredSamples: true);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _senderMock.Verify(x => x.Send(
            It.Is<SendSampleTestResultsToLisCommand>(cmd =>
                cmd.TestOrderIds != null &&
                cmd.TestOrderIds.Count == 1 &&
                cmd.TestOrderIds.Contains(1) &&
                !cmd.TestOrderIds.Contains(2) &&
                cmd.IsManualExport),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Trait("ProductRequirement", "PR_576")]
    [Trait("ModuleRequirement", "MR_5584")]
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Handle_Should_ApplyRetestFilters_When_ManualExport(
        bool includeRejectedResults,
        bool includeNewTests)
    {
        // Arrange
        var rejectedTestUid = Guid.NewGuid();
        var newRetestUid = Guid.NewGuid();

        var request = CreateRequest(1);
        request.TestOrders.Add(new TestOrder
        {
            Id = 1,
            Uid = rejectedTestUid,
            LisChannelId = 0,
            State = TestOrderState.Rejected,
            MethodName = "MethodA",
            TestName = "TestA"
        });
        request.TestOrders.Add(new TestOrder
        {
            Id = 2,
            Uid = newRetestUid,
            LisChannelId = 0,
            State = TestOrderState.NotStarted,
            MethodName = "MethodA",
            TestName = "TestA"
        });

        var command = new ProcessLisExportCommand(
            Requests: new List<Domain.Request> { request },
            TestOrderUids: new List<Guid> { rejectedTestUid, newRetestUid },
            IsManualExport: true);

        ConfigureLisChannel(
            channelId: 0,
            automaticSampleExport: LisAutoExportTrigger.NotUsed,
            whatToExport: WhatToExport.AllStatuses,
            includeLocallyEnteredSamples: true,
            duringRetestIncludeRejectedResults: includeRejectedResults,
            duringRetestIncludeNewResults: includeNewTests);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        if (!includeRejectedResults && !includeNewTests)
        {
            _senderMock.Verify(x => x.Send(It.IsAny<SendSampleTestResultsToLisCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        }
        else
        {
            _senderMock.Verify(x => x.Send(
                It.Is<SendSampleTestResultsToLisCommand>(cmd =>
                    cmd.TestOrderIds != null &&
                    cmd.TestOrderIds.Count == (includeRejectedResults ? 1 : 0) + (includeNewTests ? 1 : 0) &&
                    cmd.TestOrderIds.Contains(1) == includeRejectedResults &&
                    cmd.TestOrderIds.Contains(2) == includeNewTests &&
                    cmd.IsManualExport),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    // Helper methods
    private static Domain.Request CreateRequest(int id, RequestSource source = RequestSource.Lis)
    {
        return new Domain.Request
        {
            Id = id,
            Source = source,
            TestOrders = new List<TestOrder>()
        };
    }

    private static TestOrder CreateTestOrder(int id, Guid uid, int? lisChannelId, TestOrderState state)
    {
        return new TestOrder
        {
            Id = id,
            Uid = uid,
            LisChannelId = lisChannelId,
            State = state,
            MethodName = $"Method{id}",
            TestName = $"Test{id}"
        };
    }

    private void ConfigureLisChannel(
        int channelId,
        LisAutoExportTrigger automaticSampleExport,
        IncludeOtherTestsInAutomaticExports includeOtherTestsInAutomaticExports = IncludeOtherTestsInAutomaticExports.NotUsed,
        WhatToExport whatToExport = WhatToExport.AllStatuses,
        bool includeLocallyEnteredSamples = true,
        bool exportTestsInAlphabeticalOrder = false,
        bool duringRetestIncludeRejectedResults = false,
        bool duringRetestIncludeNewResults = false)
    {
        EnsureChannelExists(channelId);

        _lisSettings.LisChannels[channelId].ExportSettings.Sample = new LisExportSampleSettings
        {
            AutomaticSampleExport = automaticSampleExport,
            IncludeOtherTestsInAutomaticExports = includeOtherTestsInAutomaticExports,
            WhatToExport = whatToExport,
            IncludeLocallyEnteredSamples = includeLocallyEnteredSamples,
            ExportTestsInAlphabeticalOrder = exportTestsInAlphabeticalOrder,
            DuringRetestIncludeRejectedResults = duringRetestIncludeRejectedResults,
            DuringRetestIncludeNewResults = duringRetestIncludeNewResults
        };
    }

    private void ConfigureMethodDefaultChannel(string methodName, int? channelId)
    {
        _methodDefaultChannels[methodName] = channelId;
    }

    private void VerifyExport(int channelId, params int[] expectedTestOrderIds)
    {
        var matchingCommands = _senderMock.Invocations
            .Select(i => i.Arguments[0])
            .OfType<SendSampleTestResultsToLisCommand>()
            .Where(cmd => cmd.LisChannelId == channelId)
            .Select(cmd => cmd.TestOrderIds is null ? "<null>" : string.Join(",", cmd.TestOrderIds))
            .ToList();

        _senderMock.Verify(x => x.Send(
            It.Is<SendSampleTestResultsToLisCommand>(cmd =>
                cmd.LisChannelId == channelId &&
                cmd.TestOrderIds != null &&
                cmd.TestOrderIds.Count == expectedTestOrderIds.Length &&
                expectedTestOrderIds.All(cmd.TestOrderIds.Contains)),
            It.IsAny<CancellationToken>()), Times.Once, $"Channel {channelId} commands: {string.Join(" | ", matchingCommands)}");
    }

    private void VerifyNoExport(int channelId)
    {
        _senderMock.Verify(x => x.Send(
            It.Is<SendSampleTestResultsToLisCommand>(cmd => cmd.LisChannelId == channelId),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    private void EnsureChannelExists(int channelId)
    {
        while (_lisSettings.LisChannels.Count <= channelId)
        {
            _lisSettings.LisChannels.Add(new LisChannel());
        }
    }

    private sealed class TestMethodsService(IReadOnlyDictionary<string, int?> methodDefaultChannels) : IMethodsService
    {
        public Task<TResult?> GetMethod<TResult>(string methodName, Expression<Func<Method, TResult>> selector)
        {
            throw new NotSupportedException();
        }

        public Task<List<TResult>> GetMethods<TResult>(
            Expression<Func<Method, bool>> filter,
            Expression<Func<Method, TResult>> selector)
        {
            var compiledFilter = filter.Compile();
            var compiledSelector = selector.Compile();

            var methods = methodDefaultChannels
                .Select(x => new Method
                {
                    MethodName = x.Key,
                    DefaultLisChannelId = x.Value
                })
                .Where(compiledFilter)
                .Select(compiledSelector)
                .ToList();

            return Task.FromResult(methods);
        }

        public Task<List<Method>> GetDetailedMethods()
        {
            throw new NotSupportedException();
        }

        public Task<TResult?> GetMethodTestByLabNameOrTestName<TResult>(string? labMethodName, string labTestName, Expression<Func<MethodTest, TResult>> selector)
        {
            throw new NotSupportedException();
        }

        public Task<TResult?> GetMethodTestByLabName<TResult>(string? labMethodName, string labTestName, Expression<Func<MethodTest, TResult>> selector, bool forceUniqueLaboratoryTestNames = false)
        {
            throw new NotSupportedException();
        }

        public Task<TResult?> GetMethodTest<TResult>(string methodName, string testName, Expression<Func<MethodTest, TResult>> selector)
        {
            throw new NotSupportedException();
        }

        public Task<TResult?> GetQualityControl<TResult>(string methodName, string testName, string qualityControlName, Expression<Func<PhadiaQualityControlTest, TResult>> selector)
        {
            throw new NotSupportedException();
        }

        public Task<TPoint?> GetMethodCalibratorPoint<TPoint>(string methodName, string calibratorName, Expression<Func<MethodCalibrationPoint, TPoint>> selector)
        {
            throw new NotSupportedException();
        }

        public Task<List<TPoint>> GetMethodCalibratorPoints<TPoint>(string methodName, Expression<Func<MethodCalibrationPoint, TPoint>> selector)
        {
            throw new NotSupportedException();
        }

        public Task<(bool PreventDilutionFactorsBelowDefault, int DefaultDilutionFactor, AssayTechnology? Technology)> GetDefaultInstrumentDilutionFactor(string testName, string methodName)
        {
            throw new NotSupportedException();
        }

        public Task<TResult?> GetCurveControl<TResult>(string methodName, string curveControlName, Expression<Func<MethodCurveControl, TResult>> selector)
        {
            throw new NotSupportedException();
        }

        public Task<int> GetCalibrationCurveDaysValidity(string methodName)
        {
            throw new NotSupportedException();
        }

        public Task<int> GetMethodCurveControlCount(string methodName)
        {
            throw new NotSupportedException();
        }

        public Task<HashSet<string>> GetPhadiaQualityControlCartridgeNames()
        {
            throw new NotSupportedException();
        }

        public Task<string?> GetMethodTestConcentrationUnit(string methodName, string testName)
        {
            throw new NotSupportedException();
        }

        public Task<Dictionary<(string MethodName, string TestName), string?>> GetMethodTestConcentrationUnits(IEnumerable<(string MethodName, string TestName)> methodTestPairs)
        {
            throw new NotSupportedException();
        }

        public Task<Dictionary<string, int?>> GetMethodsSortOrder()
        {
            throw new NotImplementedException();
        }

    }
}
