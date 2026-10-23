namespace Pharos.Server.Request.Domain;

public class RequestTests
{
    [Theory]
    [InlineData(RequestState.NotStarted)]
    [InlineData(RequestState.NotStarted, TestOrderState.NotStarted)]
    [InlineData(RequestState.Started, TestOrderState.Scheduled)]
    [InlineData(RequestState.Started, TestOrderState.UnableToStart)]
    [InlineData(RequestState.Started, TestOrderState.Started)]
    [InlineData(RequestState.Started, TestOrderState.Pipetted)]
    [InlineData(RequestState.Started, TestOrderState.Scheduled, TestOrderState.Started, TestOrderState.Pipetted)]
    [InlineData(RequestState.Started, TestOrderState.NotStarted, TestOrderState.Started)] // Priority 2: Has Started status
    [InlineData(RequestState.Started, TestOrderState.NotStarted, TestOrderState.Scheduled)] // Priority 2: Has Scheduled status
    [InlineData(RequestState.PartlyCompleted, TestOrderState.NotStarted, TestOrderState.Approved)] // Priority 3: NotStarted with completed state
    [InlineData(RequestState.PartlyCompleted, TestOrderState.NotStarted, TestOrderState.Rejected)] // Priority 3: NotStarted with completed state
    [InlineData(RequestState.PartlyCompleted, TestOrderState.NotStarted, TestOrderState.Reported)] // Priority 3: NotStarted with completed state
    [InlineData(RequestState.InReview, TestOrderState.PendingBlank)]
    [InlineData(RequestState.InReview, TestOrderState.PendingCalibration)]
    [InlineData(RequestState.InReview, TestOrderState.PendingApproval)]
    [InlineData(RequestState.InReview, TestOrderState.Aborted)]
    [InlineData(RequestState.InReview, TestOrderState.Flagged)]
    [InlineData(RequestState.InReview, TestOrderState.ApprovedPendingReflex)]
    [InlineData(RequestState.Completed, TestOrderState.Approved)]
    [InlineData(RequestState.Completed, TestOrderState.Rejected)]
    [InlineData(RequestState.Completed, TestOrderState.Reported)]
    [InlineData(RequestState.Completed, TestOrderState.Approved, TestOrderState.Rejected, TestOrderState.Reported)]
    public void UpdateRequestStateTests(RequestState expectedRequestState, params TestOrderState[] testOrderStates)
    {
        var request = new Request
        {
            RequestState = RequestState.NotStarted, // set initial state
            TestOrders = []
        };

        foreach (var status in testOrderStates)
        {
            request.TestOrders.Add(new TestOrder
            {
                State = status
            });
        }

        request.UpdateRequestState();

        request.RequestState.Should().Be(expectedRequestState);
    }

    [Theory]
    [InlineData(TestOrderState.NotStarted, true)] // Cannot add duplicate - test not finished
    [InlineData(TestOrderState.Scheduled, true)]
    [InlineData(TestOrderState.UnableToStart, true)]
    [InlineData(TestOrderState.Started, true)]
    [InlineData(TestOrderState.Pipetted, true)]
    [InlineData(TestOrderState.PendingBlank, false)]
    [InlineData(TestOrderState.PendingCalibration, false)]
    [InlineData(TestOrderState.PendingApproval, false)]
    [InlineData(TestOrderState.Flagged, false)]
    [InlineData(TestOrderState.Aborted, false)]
    [InlineData(TestOrderState.Rejected, false)]
    [InlineData(TestOrderState.Approved, false)]
    [InlineData(TestOrderState.ApprovedPendingReflex, false)]
    [InlineData(TestOrderState.Reported, false)]
    public void HasUnfinishedTestOrder_WithExistingTest_ReturnsCorrectValue(TestOrderState existingTestState, bool hasUnfinished)
    {
        // Arrange
        var request = new Request
        {
            TestOrders = new List<TestOrder>
            {
                new() { MethodName = "Method1", TestName = "Test1", State = existingTestState }
            }
        };

        // Act
        var result = request.HasUnfinishedTestOrder("Method1", "Test1");

        // Assert
        result.Should().Be(hasUnfinished);
    }

    [Fact]
    public void HasUnfinishedTestOrder_WithNoExistingTest_ReturnsFalse()
    {
        // Arrange
        var request = new Request { TestOrders = new List<TestOrder>() };

        // Act
        var result = request.HasUnfinishedTestOrder("Method1", "Test1");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasUnfinishedTestOrder_WithDifferentMethodName_ReturnsFalse()
    {
        // Arrange
        var request = new Request
        {
            TestOrders = new List<TestOrder>
            {
                new() { MethodName = "Method1", TestName = "Test1", State = TestOrderState.NotStarted }
            }
        };

        // Act
        var result = request.HasUnfinishedTestOrder("Method2", "Test1");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasUnfinishedTestOrder_WithDifferentTestName_ReturnsFalse()
    {
        // Arrange
        var request = new Request
        {
            TestOrders = new List<TestOrder>
            {
                new() { MethodName = "Method1", TestName = "Test1", State = TestOrderState.NotStarted }
            }
        };

        // Act
        var result = request.HasUnfinishedTestOrder("Method1", "Test2");

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(TestOrderState.Approved)]
    [InlineData(TestOrderState.Rejected)]
    [InlineData(TestOrderState.Reported)]
    [InlineData(TestOrderState.PendingApproval)]
    public void HasTestOrder_WithExistingFinishedOrReviewState_ReturnsTrue(TestOrderState existingTestState)
    {
        // Arrange
        var request = new Request
        {
            TestOrders = new List<TestOrder>
            {
                new() { MethodName = "Method1", TestName = "Test1", State = existingTestState }
            }
        };

        // Act
        var result = request.HasTestOrder("Method1", "Test1");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasTestOrder_WithNoExistingTest_ReturnsFalse()
    {
        // Arrange
        var request = new Request { TestOrders = new List<TestOrder>() };

        // Act
        var result = request.HasTestOrder("Method1", "Test1");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ExistingFinishedTest_WhenEvaluatedByLegacyAndDuplicateChecks_ReturnsExpectedDifference()
    {
        // Arrange
        var request = new Request
        {
            TestOrders = new List<TestOrder>
            {
                new() { MethodName = "Method1", TestName = "Test1", State = TestOrderState.Approved }
            }
        };

        // Act
        var hasUnfinished = request.HasUnfinishedTestOrder("Method1", "Test1");
        var hasAnyExisting = request.HasTestOrder("Method1", "Test1");

        // Assert
        hasUnfinished.Should().BeFalse();
        hasAnyExisting.Should().BeTrue();
    }
}
