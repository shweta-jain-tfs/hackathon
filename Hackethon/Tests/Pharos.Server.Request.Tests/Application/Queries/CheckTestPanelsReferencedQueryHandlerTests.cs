namespace Pharos.Server.Request.Application.Queries;

public class CheckTestPanelsReferencedQueryHandlerTests
{
    private readonly List<TestOrder> _testOrders = [];
    private readonly Mock<IRequestDbContext> _mockContext = new();
    private readonly CheckTestPanelsReferencedQueryHandler _handler;

    public CheckTestPanelsReferencedQueryHandlerTests()
    {
        _mockContext.Setup(x => x.TestOrders).ReturnsDbSet(_testOrders);
        _handler = new CheckTestPanelsReferencedQueryHandler(_mockContext.Object);
    }

    [Fact]
    public async Task Handle_WhenPanelIdsAreReferenced_ReturnsTrue()
    {
        // Arrange
        _testOrders.AddRange([
            CreateTestOrder(1, [10, 20]),
            CreateTestOrder(2, [20, 30]),
            CreateTestOrder(3, [40])
        ]);

        // Act
        var result = await _handler.Handle(new CheckTestPanelsReferencedQuery([10, 20, 99]), TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenPanelIdsAreNotReferenced_ReturnsFalse()
    {
        // Arrange
        _testOrders.Add(CreateTestOrder(1, [10, 20]));

        // Act
        var result = await _handler.Handle(new CheckTestPanelsReferencedQuery([99]), TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeFalse();
    }

    private static TestOrder CreateTestOrder(int id, List<int> testPanelIds)
    {
        return new TestOrder
        {
            Id = id,
            MethodName = "Method",
            TestName = $"Test{id}",
            TestPanelIds = testPanelIds
        };
    }
}
