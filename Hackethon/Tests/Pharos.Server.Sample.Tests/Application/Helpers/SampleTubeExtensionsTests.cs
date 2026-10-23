using Pharos.Server.Application.Request;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Sample.Application.Helpers;
using Pharos.Server.Sample.Domain;

namespace Pharos.Server.Sample.Tests.Application.Helpers;

public class SampleTubeExtensionsTests
{
    private record TestOrder(Guid Uid, TestOrderState State, string? TubeId, bool HasReflexTests, int? LisChannelId = null, EntityState EntityState = EntityState.Unchanged, bool IsActive = true) : ITestOrder;

    [Fact]
    public void UpdateTubeCompletionStatus_ReturnsFalse_WhenTubeIdIsNull()
    {
        var tube = new SampleTube { TubeId = null, Status = SampleTubeStatus.InProgress };
        var testOrderStates = new List<ITestOrder>();

        var result = tube.UpdateTubeCompletionStatus(testOrderStates);

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(SampleTubeStatus.NotIdentified)]
    [InlineData(SampleTubeStatus.FollowUpReflex)]
    public void UpdateTubeCompletionStatus_ReturnsFalse_ForSpecialStatuses(SampleTubeStatus status)
    {
        var tube = new SampleTube { TubeId = "T1", Status = status };
        var testOrderStates = new List<ITestOrder>();

        var result = tube.UpdateTubeCompletionStatus(testOrderStates);

        result.Should().BeFalse();
    }

    [Fact]
    public void UpdateTubeCompletionStatus_SetsStatusToCompleted_WhenAllTestOrdersAreCompleted()
    {
        var tube = new SampleTube { TubeId = "T1", Status = SampleTubeStatus.InProgress };
        var testOrderStates = new List<ITestOrder>
        {
            new TestOrder(Guid.NewGuid(), TestOrderState.Pipetted, "T1", false),
            new TestOrder(Guid.NewGuid(), TestOrderState.PendingApproval, "T1", false),
            new TestOrder(Guid.NewGuid(), TestOrderState.Flagged, "T1", false),
            new TestOrder(Guid.NewGuid(), TestOrderState.Aborted, "T1", false),
            new TestOrder(Guid.NewGuid(), TestOrderState.Rejected, "T1", false),
            new TestOrder(Guid.NewGuid(), TestOrderState.Approved, "T1", false),
            new TestOrder(Guid.NewGuid(), TestOrderState.Reported, "T1", false)
        };

        var result = tube.UpdateTubeCompletionStatus(testOrderStates);

        result.Should().BeTrue();
        tube.Status.Should().Be(SampleTubeStatus.Completed);
    }

    [Fact]
    public void UpdateTubeCompletionStatus_SetsStatusToPartlyCompleted_WhenSomePipettedAndAnyNotStarted()
    {
        var tube = new SampleTube { TubeId = "T1", Status = SampleTubeStatus.InProgress };
        var testOrderStates = new List<ITestOrder>
        {
            new TestOrder(Guid.NewGuid(), TestOrderState.Pipetted, "T1", false),
            new TestOrder(Guid.NewGuid(), TestOrderState.NotStarted, "T1", false)
        };

        var result = tube.UpdateTubeCompletionStatus(testOrderStates);

        result.Should().BeTrue();
        tube.Status.Should().Be(SampleTubeStatus.PartlyCompleted);
    }

    [Fact]
    public void UpdateTubeCompletionStatus_SetsStatusToInProgress_WhenNoCompletedOrPartlyCompleted()
    {
        var tube = new SampleTube { TubeId = "T1", Status = SampleTubeStatus.InProgress };
        var testOrderStates = new List<ITestOrder>
        {
            new TestOrder(Guid.NewGuid(), TestOrderState.NotStarted, "T1", false),
            new TestOrder(Guid.NewGuid(), TestOrderState.NotStarted, "T1", false)
        };

        var result = tube.UpdateTubeCompletionStatus(testOrderStates);

        result.Should().BeTrue();
        tube.Status.Should().Be(SampleTubeStatus.InProgress);
    }

    [Fact]
    public void UpdateTubeCompletionStatus_SetsStatusToPartlyCompleted_WhenConditionalTestApprovedAndReflexTestsNotStarted()
    {
        var tube = new SampleTube { TubeId = "T1", Status = SampleTubeStatus.InProgress };
        var testOrderStates = new List<ITestOrder>
            {
                new TestOrder(Guid.NewGuid(), TestOrderState.Approved, "fx1", false),
                new TestOrder(Guid.NewGuid(), TestOrderState.NotStarted, "T1", false),
                new TestOrder(Guid.NewGuid(), TestOrderState.NotStarted, null, false)
            };

        var result = tube.UpdateTubeCompletionStatus(testOrderStates);

        result.Should().BeTrue();
        tube.Status.Should().Be(SampleTubeStatus.PartlyCompleted);
    }


    [Fact]
    public void UpdateTubeCompletionStatus_SetsStatusToInProgress_WhenConditionalTestApprovedAndReflexTestsStarted()
    {
        var tube = new SampleTube { TubeId = "T1", Status = SampleTubeStatus.InProgress };
        var testOrderStates = new List<ITestOrder>
            {
                new TestOrder(Guid.NewGuid(), TestOrderState.Approved, "fx1", false),
                new TestOrder(Guid.NewGuid(), TestOrderState.Started, "T1", false),
                new TestOrder(Guid.NewGuid(), TestOrderState.Started, "T1", false)
            };

        var result = tube.UpdateTubeCompletionStatus(testOrderStates);

        result.Should().BeTrue();
        tube.Status.Should().Be(SampleTubeStatus.InProgress);
    }

    [Fact]
    public void UpdateTubeCompletionStatus_SetsStatusToInProgress_WhenReflexTestsAndSomeStarted()
    {
        var tube = new SampleTube { TubeId = "T1", Status = SampleTubeStatus.InProgress };
        var testOrderStates = new List<ITestOrder>
            {
                new TestOrder(Guid.NewGuid(), TestOrderState.Started, "T1", true),
                new TestOrder(Guid.NewGuid(), TestOrderState.NotStarted, "T1", false),
                new TestOrder(Guid.NewGuid(), TestOrderState.NotStarted, null, false)
            };
        var result = tube.UpdateTubeCompletionStatus(testOrderStates);
        result.Should().BeTrue();
        tube.Status.Should().Be(SampleTubeStatus.InProgress);
    }

    [Fact]
    public void UpdateTubeCompletionStatus_SetsStatusToCompleted_WhenConditionalTestApprovedAndReflexTestsAllPipetted()
    {
        var tube = new SampleTube { TubeId = "T1", Status = SampleTubeStatus.InProgress };
        var testOrderStates = new List<ITestOrder>
            {
                new TestOrder(Guid.NewGuid(), TestOrderState.Approved, "fx1", false),
                new TestOrder(Guid.NewGuid(), TestOrderState.Pipetted, "T1", false),
                new TestOrder(Guid.NewGuid(), TestOrderState.Pipetted, "T2", false)
            };
        var result = tube.UpdateTubeCompletionStatus(testOrderStates);
        result.Should().BeTrue();
        tube.Status.Should().Be(SampleTubeStatus.Completed);
    }

    [Fact]
    public void UpdateTubeCompletionStatus_SetsStatusToCompleted_WhenConditionalTestRejected()
    {
        var tube = new SampleTube { TubeId = "T1", Status = SampleTubeStatus.InProgress };
        var testOrderStates = new List<ITestOrder>
            {
                new TestOrder(Guid.NewGuid(), TestOrderState.Rejected, "fx1", false),
            };
        var result = tube.UpdateTubeCompletionStatus(testOrderStates);
        result.Should().BeTrue();
        tube.Status.Should().Be(SampleTubeStatus.Completed);
    }

    [Fact]
    public void UpdateTubeCompletionStatus_SetsStatusToInProgress_WhenConditionalReflexTestNotApproved()
    {
        var tube = new SampleTube { TubeId = "T1", Status = SampleTubeStatus.InProgress };
        var testOrderStates = new List<ITestOrder>
            {
                new TestOrder(Guid.NewGuid(), TestOrderState.Pipetted, "fx1", true),
            };

        var result = tube.UpdateTubeCompletionStatus(testOrderStates);

        result.Should().BeTrue();
        tube.Status.Should().Be(SampleTubeStatus.InProgress);
    }

}