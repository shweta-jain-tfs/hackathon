using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Result;
using Pharos.Server.Application.Settings.Sample;
using Tetris.Protocol.Common;
using Tetris.Protocol.Pharos.TestResult;

namespace Pharos.Server.Request.Application.Commands;

public class PreProcessAssayResultCommandTests
{
    private readonly Mock<IRequestDbContext> _dbContextMock = new();
    private readonly Mock<ISender> _senderMock = new();
    private readonly PreProcessAssayResultCommand _sut;

    public PreProcessAssayResultCommandTests()
    {
        var tubeSettingsMock = new Mock<IOptionsSnapshot<TubeTypesSettings>>();
        tubeSettingsMock.Setup(x => x.Value).Returns(new TubeTypesSettings());

        var displayNameProviderMock = new Mock<ITestDisplayNameProvider>();
        displayNameProviderMock
            .Setup(x => x.GetTestDisplayName(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(string.Empty);

        _sut = new PreProcessAssayResultCommand(
            _dbContextMock.Object,
            tubeSettingsMock.Object,
            displayNameProviderMock.Object,
            _senderMock.Object);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_6033")]
    public async Task Process_SampleAssayResult_UsesGroupIdToFindTestOrder()
    {
        var testOrderId = Guid.NewGuid();
        var otherTestOrder = new TestOrder
        {
            Uid = Guid.NewGuid(),
            RequestId = 2,
            State = TestOrderState.Started
        };
        var matchingTestOrder = new TestOrder
        {
            Uid = testOrderId,
            RequestId = 1,
            State = TestOrderState.Started
        };
        var matchingRequest = CreateRequest(1, "sample-1", matchingTestOrder);
        var otherRequest = CreateRequest(2, "sample-2", otherTestOrder);
        SetupDbContext([matchingRequest, otherRequest], [otherTestOrder, matchingTestOrder]);

        await _sut.Process(CreateCommand(testOrderId, "sample-1", matchingRequest.Created), TestContext.Current.CancellationToken);

        matchingTestOrder.State.Should().Be(TestOrderState.Pipetted);
        otherTestOrder.State.Should().Be(TestOrderState.Started);
        _dbContextMock.Object.TestOrders.Should().HaveCount(2);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_6033")]
    public async Task Process_ExistingTestOrder_UsesTestOrderRequestIdToFindRequest()
    {
        var testOrderId = Guid.NewGuid();
        var existingTestOrder = new TestOrder
        {
            Uid = testOrderId,
            RequestId = 1,
            State = TestOrderState.Started
        };
        var requestFromTestOrder = CreateRequest(1, "sample-from-test-order", existingTestOrder);

        var sampleDate = DateTimeOffset.Now;
        var requestMatchingMessage = CreateRequest(
            2,
            "sample-from-message",
            new TestOrder { Uid = Guid.NewGuid(), RequestId = 2, State = TestOrderState.NotStarted });
        requestMatchingMessage.Created = sampleDate.ToLocalTime();

        SetupDbContext([requestFromTestOrder, requestMatchingMessage], [existingTestOrder]);

        await _sut.Process(CreateCommand(testOrderId, requestMatchingMessage.SampleId, sampleDate), TestContext.Current.CancellationToken);

        requestFromTestOrder.RequestState.Should().Be(RequestState.Started);
        requestMatchingMessage.RequestState.Should().Be(RequestState.NotStarted);
        _dbContextMock.Object.Requests.Should().HaveCount(2);
    }

    [Fact]
    public async Task Process_WithoutMatchingRequest_CreatesRequestWithIncomingSampleId()
    {
        var existingRequest = CreateRequest(
            1,
            "s1",
            new TestOrder { Uid = Guid.NewGuid(), RequestId = 1, State = TestOrderState.NotStarted });
        SetupDbContext([existingRequest], []);
        _senderMock
            .Setup(x => x.Send(It.IsAny<GetRequestIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RequestIdResponseDto(-1, DateTimeOffset.MinValue, false));

        await _sut.Process(
            CreateCommand(Guid.NewGuid(), "S1", existingRequest.Created),
            TestContext.Current.CancellationToken);

        _senderMock.Verify(
            x => x.Send(
                It.Is<GetRequestIdQuery>(query =>
                    query.SampleId == "S1" && query.RequestCreatedDate == existingRequest.Created),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _dbContextMock.Verify(
            x => x.Requests.Add(It.Is<Domain.Request>(request => request.SampleId == "S1")),
            Times.Once);
    }

    private void SetupDbContext(IList<Domain.Request> requests, IList<TestOrder> testOrders)
    {
        _dbContextMock.Setup(x => x.Requests).ReturnsDbSet(requests);
        _dbContextMock.Setup(x => x.TestOrders).ReturnsDbSet(testOrders);
        _dbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private static Domain.Request CreateRequest(int id, string sampleId, TestOrder testOrder)
    {
        var request = new Domain.Request
        {
            Id = id,
            SampleId = sampleId,
            Created = DateTimeOffset.Now.AddDays(-id),
            TestOrders = [testOrder]
        };
        testOrder.Request = request;
        return request;
    }

    private static ProcessAssayResultCommand CreateCommand(Guid testOrderId, string sampleId, DateTimeOffset sampleDate)
    {
        return new ProcessAssayResultCommand(
            "SN0001",
            new AssayResult
            {
                TestType = EnumTestType.Sample,
                Sample = new SampleInformation
                {
                    SampleId = sampleId,
                    SampleDate = Timestamp.FromDateTimeOffset(sampleDate)
                },
                Replicate = new ReplicateInfo
                {
                    GroupId = testOrderId.ToString()
                }
            });
    }
}
