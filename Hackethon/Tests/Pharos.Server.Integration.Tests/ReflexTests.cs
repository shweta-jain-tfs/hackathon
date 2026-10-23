using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Server.Application.Result;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Reflex;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Reflex.Interfaces;
using Pharos.Server.Request.Application.Interfaces;
using Pharos.Server.Request.Domain;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Integration.Tests;

[Trait("Category", "Integration")]
[Collection(nameof(IntegrationTestsCollection))]
public class ReflexTests(IntegrationTestsWaf factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task ReflexManualApprove_Should_TriggerReflexAdd()
    {
        // Arrange
        var sampleId = "sample123"; // Sample ID for the test
        var requestDbContext = ServiceProvider.GetRequiredService<IRequestDbContext>();
        requestDbContext.Requests.Count().Should().Be(0);

        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var reflexDbContext = scope.ServiceProvider.GetRequiredService<IReflexDbContext>();

            reflexDbContext.ReflexDefinitions.Count().Should().Be(0);

            var testOrderId = Guid.NewGuid(); // Unique identifier for the test order

            // Add a sample request
            var request = new Request.Domain.Request
            {
                SampleId = sampleId,
                TestOrders = new List<TestOrder>
                {
                    new()
                    {
                        Uid = testOrderId,
                        TestName = "sIgE",
                        MethodName = "c1",
                        State = TestOrderState.Started,
                        InstrumentNumber = "SEED_001",
                        InstrumentDilutionFactor = 1,
                        Created = DateTimeOffset.UtcNow
                    }
                }
            };

            // Setup a reflex definition that will trigger on a specific condition
            var reflexDefinition = new Reflex.Domain.ReflexDefinition
            {
                Name = "TestReflexDefinition",
                Active = true,
                Automatic = true,
                RequestorId = null,
                ReflexCondition = new()
                {
                    TestName = "c1",
                    MethodName = "sIgE",
                    ResultType = ResultType.Concentration,
                    CompareType = CompareType.Above,
                    ConcentrationCompareValue = 3
                },
                ReflexTests = new List<ReflexTest>
                {
                    new()
                    {
                        TestName = "c2",
                        MethodName = "sIgE",
                    }
                }
            };

            // Convert the reflexDefinition to the correct type if needed
            var requestTestOrder = request.TestOrders.First();

            // Ensure ReflexDefinitions is initialized
            requestTestOrder.ReflexDefinitions ??= new List<ReflexDefinition>();

            // Map the properties from the Pharos.Server.Reflex.Domain.ReflexDefinition to Pharos.Server.Request.Domain.ReflexDefinition
            var mappedReflexDefinition = new ReflexDefinition
            {
                Name = reflexDefinition.Name,
                Automatic = reflexDefinition.Automatic,
                ReflexCondition = reflexDefinition.ReflexCondition, // If types are compatible
                ReflexTests = new List<ReflexTest>
                {
                    new()
                    {
                        TestName = "c2",
                        MethodName = "sIgE",
                    }
                }
                // Map other properties as needed
            };

            requestTestOrder.ReflexDefinitions.Add(mappedReflexDefinition);

            reflexDbContext.ReflexDefinitions.Add(reflexDefinition);
            requestDbContext.Requests.Add(request);

            await reflexDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
            await requestDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            // Act
            var mediator = scope.ServiceProvider.GetService<IMediator>()!;

            var message = new TestResultStatusChangedEvent(new[]
            {
                new SampleTestResult
                {
                    AnalyticalRunId = 1,
                    TestOrderId = testOrderId,
                    ResultType = TestResultType.Sample,
                    InstrumentNumber = "SEED_001",
                    MethodName = "c1",
                    TestName = "sIgE",
                    InstrumentDilutionFactor = 1,
                    PreDilutionFactor = 1,
                    Status = TestResultStatus.Approved,
                    MeanConcentration = 4,
                    NumberOfReplicates = 1,
                    ApprovalStatus = ResultApprovalStatus.NotApproved
                }
            });

            await mediator.Publish(message, TestContext.Current.CancellationToken);
        }

        await WaitForNotificationsAsync();

        // Assert 
        var reflexDbContext1 = ServiceProvider.GetRequiredService<IReflexDbContext>();
        (await reflexDbContext1.ReflexDefinitions.CountAsync(TestContext.Current.CancellationToken)).Should().Be(1);

        var requestDbContext1 = ServiceProvider.GetRequiredService<IRequestDbContext>();
        var requestEntity = await requestDbContext1.Requests
            .Where(x => x.SampleId == sampleId)
            .Include(r => r.TestOrders).FirstOrDefaultAsync(TestContext.Current.CancellationToken);
        requestEntity.Should().NotBeNull("A request with the specified sampleId should exist");

        var reflexTestOrder = requestEntity.TestOrders.FirstOrDefault(y => y.TestName == "c2" && y.MethodName == "sIgE");
        reflexTestOrder.Should().NotBeNull("A reflex test order with TestName 'c2' and MethodName 'sIgE' should exist");
    }

    [Fact]
    public async Task ReflexApprove_WithMatchingRequestorConstraint_Should_TriggerReflexAdd()
    {
        // Arrange
        var sampleId = "sample_matching_requestor";
        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var requestDbContext = scope.ServiceProvider.GetRequiredService<IRequestDbContext>();

            var testOrderId = Guid.NewGuid();
            var requestorId = 1; // Specific requestor ID

            // Add the requestor entity to the database first
            var requestor = new Requestor
            {
                RequestorId = requestorId.ToString(),
                Name = "Test Requestor"
            };
            requestDbContext.Requestors.Add(requestor);
            await requestDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            // Add a sample request with a specific requestor ID
            var request = new Request.Domain.Request
            {
                SampleId = sampleId,
                RequestorId = requestorId, // Set the requestor ID
                TestOrders = new List<TestOrder>
                {
                    new()
                    {
                        Uid = testOrderId,
                        TestName = "sIgE",
                        MethodName = "c1",
                        State = TestOrderState.Started,
                        InstrumentNumber = "SEED_001",
                        InstrumentDilutionFactor = 1,
                        Created = DateTimeOffset.UtcNow
                    }
                }
            };

            var requestTestOrder = request.TestOrders.First();
            requestTestOrder.ReflexDefinitions ??= new List<ReflexDefinition>();

            // Create a reflex definition with matching requestor constraint
            var mappedReflexDefinition = new ReflexDefinition
            {
                Name = "RequestorConstrainedReflex",
                Automatic = true,
                RequestorId = requestorId.ToString(), // Set requestor constraint to match the request
                ReflexCondition = new()
                {
                    TestName = "c1",
                    MethodName = "sIgE",
                    ResultType = ResultType.Concentration,
                    CompareType = CompareType.Above,
                    ConcentrationCompareValue = 3
                },
                ReflexTests = new List<ReflexTest>
                {
                    new()
                    {
                        TestName = "c2",
                        MethodName = "sIgE",
                    }
                }
            };

            requestTestOrder.ReflexDefinitions.Add(mappedReflexDefinition);
            requestDbContext.Requests.Add(request);
            await requestDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            // Act
            var mediator = scope.ServiceProvider.GetService<IMediator>()!;

            var message = new TestResultStatusChangedEvent(new[]
            {
                new SampleTestResult
                {
                    AnalyticalRunId = 1,
                    TestOrderId = testOrderId,
                    ResultType = TestResultType.Sample,
                    InstrumentNumber = "SEED_001",
                    MethodName = "c1",
                    TestName = "sIgE",
                    InstrumentDilutionFactor = 1,
                    PreDilutionFactor = 1,
                    Status = TestResultStatus.Approved,
                    MeanConcentration = 4, // Above the threshold (3)
                    NumberOfReplicates = 1,
                    ApprovalStatus = ResultApprovalStatus.NotApproved
                }
            });

            await mediator.Publish(message, TestContext.Current.CancellationToken);

        }

        await WaitForNotificationsAsync();

        // Assert 
        var requestDbContext1 = ServiceProvider.GetRequiredService<IRequestDbContext>();
        var requestEntity = requestDbContext1.Requests
            .Include(x => x.TestOrders)
            .FirstOrDefault(x => x.SampleId == sampleId);

        requestEntity.Should().NotBeNull("A request with the specified sampleId should exist");

        var reflexTestOrder = requestEntity.TestOrders.FirstOrDefault(y => y.TestName == "c2" && y.MethodName == "sIgE");
        reflexTestOrder.Should().NotBeNull("A reflex test order should be added because requestor constraint is fulfilled");
    }

    [Fact]
    public async Task ReflexApprove_WithNonMatchingRequestorConstraint_Should_NotTriggerReflexAdd()
    {
        // Arrange
        var requestDbContext = ServiceProvider.GetRequiredService<IRequestDbContext>();

        var sampleId = "sample_non_matching_requestor";
        var testOrderId = Guid.NewGuid();
        var requestRequestorId = 123; // Request is from requestor 123
        var reflexRequestorId = 456; // But reflex is constrained to requestor 456

        // Add both requestor entities to the database first
        var requestRequestor = new Requestor
        {
            RequestorId = requestRequestorId.ToString(),
            Name = "Test Requestor 123"
        };
        var reflexRequestor = new Requestor
        {
            RequestorId = reflexRequestorId.ToString(),
            Name = "Test Requestor 456"
        };
        requestDbContext.Requestors.Add(requestRequestor);
        requestDbContext.Requestors.Add(reflexRequestor);
        await requestDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Add a sample request with a specific requestor ID
        var request = new Request.Domain.Request
        {
            SampleId = sampleId,
            RequestorId = 1, // Set the requestor ID
            TestOrders = new List<TestOrder>
                {
                    new()
                    {
                        Uid = testOrderId,
                        TestName = "sIgE",
                        MethodName = "c1",
                        State = TestOrderState.Started,
                        InstrumentNumber = "SEED_001",
                        InstrumentDilutionFactor = 1,
                        Created = DateTimeOffset.UtcNow
                    }
                }
        };

        var requestTestOrder = request.TestOrders.First();
        requestTestOrder.ReflexDefinitions ??= new List<ReflexDefinition>();

        // Create a reflex definition with non-matching requestor constraint
        var mappedReflexDefinition = new ReflexDefinition
        {
            Name = "RequestorConstrainedReflex",
            Automatic = true,
            RequestorId = reflexRequestorId.ToString(), // Set requestor constraint to NOT match the request
            ReflexCondition = new()
            {
                TestName = "c1",
                MethodName = "sIgE",
                ResultType = ResultType.Concentration,
                CompareType = CompareType.Above,
                ConcentrationCompareValue = 3
            },
            ReflexTests = new List<ReflexTest>
                {
                    new()
                    {
                        TestName = "c2",
                        MethodName = "sIgE",
                    }
                }
        };

        requestTestOrder.ReflexDefinitions.Add(mappedReflexDefinition);
        requestDbContext.Requests.Add(request);
        await requestDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var mediator = scope.ServiceProvider.GetService<IMediator>()!;

            var message = new TestResultStatusChangedEvent(new[]
            {
                new SampleTestResult
                {
                    AnalyticalRunId = 1,
                    TestOrderId = testOrderId,
                    ResultType = TestResultType.Sample,
                    InstrumentNumber = "SEED_001",
                    MethodName = "c1",
                    TestName = "sIgE",
                    InstrumentDilutionFactor = 1,
                    PreDilutionFactor = 1,
                    Status = TestResultStatus.Approved,
                    MeanConcentration = 4, // Above the threshold (3)
                    NumberOfReplicates = 1,
                    ApprovalStatus = ResultApprovalStatus.NotApproved
                }
            });

            await mediator.Publish(message, TestContext.Current.CancellationToken);
        }

        await WaitForNotificationsAsync();

        // Assert
        var requestEntity = requestDbContext.Requests
            .Include(x => x.TestOrders)
            .FirstOrDefault(x => x.SampleId == sampleId);

        requestEntity.Should().NotBeNull("A request with the specified sampleId should exist");

        var reflexTestOrder = requestEntity.TestOrders.FirstOrDefault(y => y.TestName == "c2" && y.MethodName == "sIgE");
        reflexTestOrder.Should().BeNull("A reflex test order should NOT be added because requestor constraint is not fulfilled");

        // The original test order should still be there
        var originalTestOrder = requestEntity.TestOrders.FirstOrDefault(y => y.TestName == "sIgE" && y.MethodName == "c1");
        originalTestOrder.Should().NotBeNull("The original test order should still exist");
    }
}
