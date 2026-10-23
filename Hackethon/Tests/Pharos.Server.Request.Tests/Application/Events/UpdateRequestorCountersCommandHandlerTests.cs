using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Server.Application.Lis;
using Pharos.Server.Integration.Tests.Common;
using TestOrder = Pharos.Server.Request.Domain.TestOrder;

namespace Pharos.Server.Request.Application.Events;

/// <summary>
/// Integration tests for UpdateRequestorCountersCommandHandler using a real database.
/// </summary>
[Collection(nameof(RequestTestsCollection))]
public class UpdateRequestorCountersCommandHandlerTests(RequestTestsWaf factory)
    : DatabaseTestBase<RequestTestsWaf>(factory)
{
    private IRequestDbContext _dbContext = null!;
    private UpdateRequestorCountersCommandHandler _sut = null!;

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();

        _dbContext = ServiceProvider.GetRequiredService<IRequestDbContext>();
        _sut = new UpdateRequestorCountersCommandHandler(_dbContext);
    }

    #region Helper Methods

    private async Task<Domain.Request> SeedRequestAsync(int? requestorId, bool isCountedInRequestor, params (Guid uid, TestOrderState state)[] testOrders)
    {
        var request = new Domain.Request
        {
            SampleId = Guid.NewGuid().ToString("N")[..32],
            RequestorId = requestorId,
            IsCountedInRequestor = isCountedInRequestor
        };

        _dbContext.Requests.Add(request);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        foreach (var (uid, state) in testOrders)
        {
            _dbContext.TestOrders.Add(new TestOrder
            {
                Uid = uid,
                RequestId = request.Id,
                State = state,
                TestName = "IgE",
                MethodName = "Im-F"
            });
        }

        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        return await _dbContext.Requests
            .Include(r => r.TestOrders)
            .FirstAsync(r => r.Id == request.Id, TestContext.Current.CancellationToken);
    }

    private async Task<Requestor> SeedRequestorAsync(int numberOfReportedSamples = 0, int numberOfReportedTests = 0)
    {
        var requestor = new Requestor
        {
            RequestorId = Guid.NewGuid().ToString("N")[..32],
            NumberOfReportedSamples = numberOfReportedSamples,
            NumberOfReportedTests = numberOfReportedTests
        };
        _dbContext.Requestors.Add(requestor);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        return requestor;
    }

    private async Task<T> RunInNewScope<T>(Func<IRequestDbContext, Task<T>> action)
    {
        await using var scope = ServiceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IRequestDbContext>();
        return await action(dbContext);
    }

    #endregion

    #region Handle - No matching test orders

    [Fact]
    public async Task Handle_WhenNoTestOrdersBelongToARequestorRequest_ShouldNotUpdateCounters()
    {
        // Arrange
        var requestor = await SeedRequestorAsync();
        var nonExistentId = Guid.NewGuid();

        var notification = new UpdateRequestorCountersCommand([nonExistentId]);

        // Act
        await _sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        var updated = await RunInNewScope(db => db.Requestors.FindAsync(requestor.Id).AsTask()!);
        updated!.NumberOfReportedTests.Should().Be(0);
        updated.NumberOfReportedSamples.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WhenTestOrdersExistButRequestHasNoRequestor_ShouldNotUpdateCounters()
    {
        // Arrange
        var testOrderId = Guid.NewGuid();
        await SeedRequestAsync(requestorId: null, isCountedInRequestor: false, (testOrderId, TestOrderState.Reported));

        var notification = new UpdateRequestorCountersCommand([testOrderId]);

        // Act
        await _sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert — no requestors should be affected
        var requestors = await RunInNewScope(db => db.Requestors.ToListAsync());
        requestors.Should().BeEmpty();
    }

    #endregion

    #region Handle - Test order counter

    [Fact]
    public async Task Handle_WhenSingleTestOrderReported_ShouldIncrementNumberOfReportedTestsByOne()
    {
        // Arrange
        var requestor = await SeedRequestorAsync();
        var testOrderId = Guid.NewGuid();
        await SeedRequestAsync(requestor.Id, isCountedInRequestor: false, (testOrderId, TestOrderState.Reported));

        var notification = new UpdateRequestorCountersCommand([testOrderId]);

        // Act
        await _sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        var updated = await RunInNewScope(db => db.Requestors.FindAsync(requestor.Id).AsTask()!);
        updated!.NumberOfReportedTests.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WhenMultipleTestOrdersReportedForSameRequestor_ShouldIncrementByCorrectCount()
    {
        // Arrange
        var requestor = await SeedRequestorAsync();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        await SeedRequestAsync(requestor.Id, isCountedInRequestor: false,
            (id1, TestOrderState.Reported),
            (id2, TestOrderState.Reported));

        var notification = new UpdateRequestorCountersCommand([id1, id2]);

        // Act
        await _sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        var updated = await RunInNewScope(db => db.Requestors.FindAsync(requestor.Id).AsTask()!);
        updated!.NumberOfReportedTests.Should().Be(2);
    }

    [Fact]
    public async Task Handle_WhenTestOrdersFromDifferentRequestors_ShouldIncrementEachRequestorSeparately()
    {
        // Arrange
        var requestor1 = await SeedRequestorAsync();
        var requestor2 = await SeedRequestorAsync();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        await SeedRequestAsync(requestor1.Id, isCountedInRequestor: false, (id1, TestOrderState.Reported));
        await SeedRequestAsync(requestor2.Id, isCountedInRequestor: false, (id2, TestOrderState.Reported));

        var notification = new UpdateRequestorCountersCommand([id1, id2]);

        // Act
        await _sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        var updated1 = await RunInNewScope(db => db.Requestors.FindAsync(requestor1.Id).AsTask()!);
        var updated2 = await RunInNewScope(db => db.Requestors.FindAsync(requestor2.Id).AsTask()!);
        updated1!.NumberOfReportedTests.Should().Be(1);
        updated2!.NumberOfReportedTests.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WhenRequestorAlreadyHasExistingCounts_ShouldAddToExistingValue()
    {
        // Arrange
        var requestor = await SeedRequestorAsync(numberOfReportedTests: 5);
        var testOrderId = Guid.NewGuid();
        await SeedRequestAsync(requestor.Id, isCountedInRequestor: false, (testOrderId, TestOrderState.Reported));

        var notification = new UpdateRequestorCountersCommand([testOrderId]);

        // Act
        await _sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        var updated = await RunInNewScope(db => db.Requestors.FindAsync(requestor.Id).AsTask()!);
        updated!.NumberOfReportedTests.Should().Be(6);
    }

    #endregion

    #region Handle - Sample counter

    [Fact]
    public async Task Handle_WhenAllTestOrdersInRequestAreReported_ShouldIncrementNumberOfReportedSamples()
    {
        // Arrange
        var requestor = await SeedRequestorAsync();
        var testOrderId = Guid.NewGuid();
        await SeedRequestAsync(requestor.Id, isCountedInRequestor: false, (testOrderId, TestOrderState.Reported));

        var notification = new UpdateRequestorCountersCommand([testOrderId]);

        // Act
        await _sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        var updated = await RunInNewScope(db => db.Requestors.FindAsync(requestor.Id).AsTask()!);
        updated!.NumberOfReportedSamples.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WhenNotAllTestOrdersInRequestAreReported_ShouldNotIncrementNumberOfReportedSamples()
    {
        // Arrange
        var requestor = await SeedRequestorAsync();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        await SeedRequestAsync(requestor.Id, isCountedInRequestor: false,
            (id1, TestOrderState.Reported),
            (id2, TestOrderState.Approved)); // not yet reported

        var notification = new UpdateRequestorCountersCommand([id1]); // only one of two

        // Act
        await _sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        var updated = await RunInNewScope(db => db.Requestors.FindAsync(requestor.Id).AsTask()!);
        updated!.NumberOfReportedSamples.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WhenRequestIsAlreadyCountedInRequestor_ShouldNotIncrementNumberOfReportedSamplesAgain()
    {
        // Arrange
        var requestor = await SeedRequestorAsync(numberOfReportedSamples: 1);
        var testOrderId = Guid.NewGuid();
        await SeedRequestAsync(requestor.Id, isCountedInRequestor: true, (testOrderId, TestOrderState.Reported)); // already counted

        var notification = new UpdateRequestorCountersCommand([testOrderId]);

        // Act
        await _sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        var updated = await RunInNewScope(db => db.Requestors.FindAsync(requestor.Id).AsTask()!);
        updated!.NumberOfReportedSamples.Should().Be(1); // unchanged
    }

    [Fact]
    public async Task Handle_WhenRequestBecomesFullyReported_ShouldMarkIsCountedInRequestor()
    {
        // Arrange
        var requestor = await SeedRequestorAsync();
        var testOrderId = Guid.NewGuid();
        var seededRequest = await SeedRequestAsync(requestor.Id, isCountedInRequestor: false, (testOrderId, TestOrderState.Reported));
        var notification = new UpdateRequestorCountersCommand([testOrderId]);

        // Act
        await _sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        var updatedRequest = await RunInNewScope(db => db.Requests.FindAsync(seededRequest.Id).AsTask()!);
        updatedRequest!.IsCountedInRequestor.Should().BeTrue();
    }

    #endregion

    #region Handle - Combined counters

    [Fact]
    public async Task Handle_WhenRequestFullyReported_ShouldIncrementBothTestAndSampleCounters()
    {
        // Arrange
        var requestor = await SeedRequestorAsync();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        await SeedRequestAsync(requestor.Id, isCountedInRequestor: false,
            (id1, TestOrderState.Reported),
            (id2, TestOrderState.Reported));

        var notification = new UpdateRequestorCountersCommand([id1, id2]);

        // Act
        await _sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        var updated = await RunInNewScope(db => db.Requestors.FindAsync(requestor.Id).AsTask()!);
        updated!.NumberOfReportedTests.Should().Be(2);
        updated.NumberOfReportedSamples.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WhenRequestPartiallyReported_ShouldOnlyIncrementTestCounter()
    {
        // Arrange
        var requestor = await SeedRequestorAsync();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        await SeedRequestAsync(requestor.Id, isCountedInRequestor: false,
            (id1, TestOrderState.Reported),
            (id2, TestOrderState.Approved)); // not yet reported

        var notification = new UpdateRequestorCountersCommand([id1]);

        // Act
        await _sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        var updated = await RunInNewScope(db => db.Requestors.FindAsync(requestor.Id).AsTask()!);
        updated!.NumberOfReportedTests.Should().Be(1);
        updated.NumberOfReportedSamples.Should().Be(0);
    }

    #endregion

    #region End-to-end — SampleTestResultsSentToLisEvent triggers counter update

    [Fact]
    public async Task Handle_WhenSampleTestResultsSentToLisEventPublished_ShouldUpdateRequestorCounters()
    {
        // Arrange
        var requestor = await SeedRequestorAsync();
        var testOrderId = Guid.NewGuid();
        await SeedRequestAsync(requestor.Id, isCountedInRequestor: false, (testOrderId, TestOrderState.Approved));

        // Act — publish inside a dedicated scope so DelayedPublisher enqueues to the background on disposal
        await using (var scope = ServiceProvider.CreateAsyncScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Publish(new SampleTestResultsSentToLisEvent([testOrderId], LisChannelId: 0), TestContext.Current.CancellationToken);
        }

        await WaitForNotificationsAsync();

        // Assert — counter updated end-to-end by UpdateRequestorCountersCommandHandler
        var updated = await RunInNewScope(db => db.Requestors.FindAsync(requestor.Id).AsTask()!);
        updated!.NumberOfReportedTests.Should().Be(1);
    }

    #endregion
}
