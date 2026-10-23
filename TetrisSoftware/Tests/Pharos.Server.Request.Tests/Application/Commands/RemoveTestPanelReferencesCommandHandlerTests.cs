namespace Pharos.Server.Request.Application.Commands;

public class RemoveTestPanelReferencesCommandHandlerTests
{
    private readonly List<TestOrder> _testOrders = [];
    private readonly Mock<IRequestDbContext> _mockContext = new();
    private readonly RemoveTestPanelReferencesCommandHandler _handler;

    public RemoveTestPanelReferencesCommandHandlerTests()
    {
        _mockContext.Setup(x => x.TestOrders).ReturnsDbSet(_testOrders);
        _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new RemoveTestPanelReferencesCommandHandler(_mockContext.Object);
    }

    [Fact]
    public async Task Handle_WhenPanelIdsAreReferenced_RemovesPanelIdsFromTestOrders()
    {
        // Arrange
        var firstTestOrder = CreateTestOrder(1, [10, 20, 30]);
        var secondTestOrder = CreateTestOrder(2, [10]);
        var unaffectedTestOrder = CreateTestOrder(3, [40]);
        _testOrders.AddRange([firstTestOrder, secondTestOrder, unaffectedTestOrder]);

        // Act
        await _handler.Handle(new RemoveTestPanelReferencesCommand([10, 20]), TestContext.Current.CancellationToken);

        // Assert
        firstTestOrder.TestPanelIds.Should().Equal(30);
        secondTestOrder.TestPanelIds.Should().BeEmpty();
        unaffectedTestOrder.TestPanelIds.Should().Equal(40);
        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPanelIdsAreNotReferenced_DoesNotSaveChanges()
    {
        // Arrange
        _testOrders.Add(CreateTestOrder(1, [10]));

        // Act
        await _handler.Handle(new RemoveTestPanelReferencesCommand([99]), TestContext.Current.CancellationToken);

        // Assert
        _testOrders.Single().TestPanelIds.Should().Equal(10);
        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
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