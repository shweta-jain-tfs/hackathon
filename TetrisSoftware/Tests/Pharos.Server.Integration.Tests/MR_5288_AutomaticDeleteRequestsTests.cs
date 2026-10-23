using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Request;
using Pharos.Server.Application.Settings.Database;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Reflex;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Jobs;
using Pharos.Server.Request.Application.Interfaces;
using Pharos.Server.Request.Domain;
using Quartz;

namespace Pharos.Server.Integration.Tests;

[Trait("Category", "Integration")]
[Collection(nameof(IntegrationTestsCollection))]
public class AutomaticDeleteRequestsTests(IntegrationTestsWaf factory) : IntegrationTestBase(factory)
{
    private readonly Fixture _fixture = new();

    [Fact]
    [Trait("ModuleRequirement", "MR_5288")]
    public async Task Should_Delete_Aged_Request_With_Associated_RequestRows_When_AutomaticDeleteJob_Executes_And_Setting_Is_Enabled()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<IRequestDbContext>();
        var sender = ServiceProvider.GetRequiredService<ISender>();
        var logger = ServiceProvider.GetRequiredService<ILogger<AutomaticDeleteJob>>();

        var settings = new Mock<IOptionsSnapshot<AutomaticDeleteSettings>>();
        settings.Setup(x => x.Value).Returns(new AutomaticDeleteSettings
        {
            DeleteRequestsAutomatically = true,
            DeleteRequestsOlderThanDays = 1,
            DeleteResultsAutomatically = false,
            DeleteQualityAutomatically = false
        });

        var oldRequest = new Request.Domain.Request
        {
            SampleId = _fixture.Create<string>()[..Constants.SampleIdMaxLength],
            Source = RequestSource.Import,
            LastModified = DateTimeOffset.UtcNow.AddDays(-3)
        };

        dbContext.Requests.Add(oldRequest);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // SaveChanges interceptor updates LastModified to now; explicitly backdate for retention test scenario.
        await dbContext.Requests
            .Where(r => r.Id == oldRequest.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.LastModified, DateTimeOffset.UtcNow.AddDays(-3)), TestContext.Current.CancellationToken);

        var reportedOrder = new TestOrder
        {
            RequestId = oldRequest.Id,
            Uid = Guid.NewGuid(),
            MethodName = "m1",
            TestName = "t1",
            State = TestOrderState.Reported,
            TestSortOrder = 1
        };

        var secondReportedOrder = new TestOrder
        {
            RequestId = oldRequest.Id,
            Uid = Guid.NewGuid(),
            MethodName = "m1",
            TestName = "t2",
            State = TestOrderState.Reported,
            TestSortOrder = 2
        };

        dbContext.TestOrders.AddRange(reportedOrder, secondReportedOrder);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        dbContext.TestOrderCustomParameters.Add(new TestOrderCustomParameter
        {
            TestOrderId = reportedOrder.Id,
            CarrierLot = "L0001",
            DiluentLot = "L0002",
            ConjugateLot = "L0003",
            DevelopmentLot = "L0004",
            CustomRunId = "RUN-01",
            CustomTestId = "TEST-01"
        });

        dbContext.ReflexDefinitions.Add(new ReflexDefinition
        {
            TestOrderId = reportedOrder.Uid,
            Name = "RFX1",
            Automatic = true,
            ReflexCondition = new ReflexCondition
            {
                MethodName = reportedOrder.MethodName,
                TestName = reportedOrder.TestName
            },
            ReflexTests =
            [
                new ReflexTest
                {
                    MethodName = "m1",
                    TestName = "t3",
                    NumberOfReplicates = 1,
                    InstrumentDilutionFactor = 1,
                    TestSortOrder = 3
                }
            ]
        });

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var job = new AutomaticDeleteJob(sender, settings.Object, logger);

        // Act
        await job.Execute(Mock.Of<IJobExecutionContext>());

        // Assert
        (await dbContext.Requests.CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
        (await dbContext.TestOrders.CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
        (await dbContext.TestOrderCustomParameters.CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
        (await dbContext.ReflexTests.CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5288")]
    public async Task Should_Delete_Aged_Request_With_Associated_RequestRows_When_Invoking_CleanupRequestsCommand()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<IRequestDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();

        var oldRequest = new Pharos.Server.Request.Domain.Request
        {
            SampleId = _fixture.Create<string>()[..Constants.SampleIdMaxLength],
            Source = RequestSource.Import,
            LastModified = DateTimeOffset.UtcNow.AddDays(-3)
        };

        dbContext.Requests.Add(oldRequest);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // SaveChanges interceptor updates LastModified to now; explicitly backdate for retention test scenario.
        await dbContext.Requests
            .Where(r => r.Id == oldRequest.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.LastModified, DateTimeOffset.UtcNow.AddDays(-3)), TestContext.Current.CancellationToken);

        var reportedOrder = new TestOrder
        {
            RequestId = oldRequest.Id,
            Uid = Guid.NewGuid(),
            MethodName = "m1",
            TestName = "t1",
            State = TestOrderState.Reported,
            TestSortOrder = 1
        };

        var secondReportedOrder = new TestOrder
        {
            RequestId = oldRequest.Id,
            Uid = Guid.NewGuid(),
            MethodName = "m1",
            TestName = "t2",
            State = TestOrderState.Reported,
            TestSortOrder = 2
        };

        dbContext.TestOrders.AddRange(reportedOrder, secondReportedOrder);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var customParameter = new TestOrderCustomParameter
        {
            TestOrderId = reportedOrder.Id,
            CarrierLot = "L0001",
            DiluentLot = "L0002",
            ConjugateLot = "L0003",
            DevelopmentLot = "L0004",
            CustomRunId = "RUN-01",
            CustomTestId = "TEST-01"
        };

        dbContext.TestOrderCustomParameters.Add(customParameter);

        var reflexDefinition = new ReflexDefinition
        {
            TestOrderId = reportedOrder.Uid,
            Name = "RFX1",
            Automatic = true,
            ReflexCondition = new ReflexCondition
            {
                MethodName = reportedOrder.MethodName,
                TestName = reportedOrder.TestName
            },
            ReflexTests =
            [
                new ReflexTest
                {
                    MethodName = "m1",
                    TestName = "t3",
                    NumberOfReplicates = 1,
                    InstrumentDilutionFactor = 1,
                    TestSortOrder = 3
                }
            ]
        };

        dbContext.ReflexDefinitions.Add(reflexDefinition);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var requestsBefore = await dbContext.Requests.CountAsync(TestContext.Current.CancellationToken);
        var testOrdersBefore = await dbContext.TestOrders.CountAsync(TestContext.Current.CancellationToken);
        var customParametersBefore = await dbContext.TestOrderCustomParameters.CountAsync(TestContext.Current.CancellationToken);
        var reflexTestsBefore = await dbContext.ReflexTests.CountAsync(TestContext.Current.CancellationToken);

        // Act
        await mediator.Send(new CleanupRequestsCommand(1), TestContext.Current.CancellationToken);

        // Assert
        requestsBefore.Should().Be(1);
        testOrdersBefore.Should().Be(2);
        customParametersBefore.Should().Be(1);
        reflexTestsBefore.Should().Be(1);

        (await dbContext.Requests.CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
        (await dbContext.TestOrders.CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
        (await dbContext.TestOrderCustomParameters.CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
        (await dbContext.ReflexTests.CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5288")]
    public async Task Should_Not_Delete_Request_When_Age_Is_Inside_Retention()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<IRequestDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();

        var freshRequest = new Pharos.Server.Request.Domain.Request
        {
            SampleId = _fixture.Create<string>()[..Constants.SampleIdMaxLength],
            Source = RequestSource.Import,
            LastModified = DateTimeOffset.UtcNow.AddHours(-12)
        };

        dbContext.Requests.Add(freshRequest);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var order = new TestOrder
        {
            RequestId = freshRequest.Id,
            Uid = Guid.NewGuid(),
            MethodName = "m1",
            TestName = "t1",
            State = TestOrderState.Reported,
            TestSortOrder = 1
        };

        dbContext.TestOrders.Add(order);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        dbContext.TestOrderCustomParameters.Add(new TestOrderCustomParameter
        {
            TestOrderId = order.Id,
            CarrierLot = "L0001",
            DiluentLot = "L0002",
            ConjugateLot = "L0003",
            DevelopmentLot = "L0004",
            CustomRunId = "RUN-01",
            CustomTestId = "TEST-01"
        });

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        await mediator.Send(new CleanupRequestsCommand(1), TestContext.Current.CancellationToken);

        // Assert
        (await dbContext.Requests.CountAsync(TestContext.Current.CancellationToken)).Should().Be(1);
        (await dbContext.TestOrders.CountAsync(TestContext.Current.CancellationToken)).Should().Be(1);
        (await dbContext.TestOrderCustomParameters.CountAsync(TestContext.Current.CancellationToken)).Should().Be(1);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5288")]
    public async Task Should_Not_Delete_Request_When_Not_All_TestOrders_Are_Reported()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<IRequestDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();

        var oldRequest = new Pharos.Server.Request.Domain.Request
        {
            SampleId = _fixture.Create<string>()[..Constants.SampleIdMaxLength],
            Source = RequestSource.Import,
            LastModified = DateTimeOffset.UtcNow.AddDays(-3)
        };

        dbContext.Requests.Add(oldRequest);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // SaveChanges interceptor updates LastModified to now; explicitly backdate for retention test scenario.
        await dbContext.Requests
            .Where(r => r.Id == oldRequest.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.LastModified, DateTimeOffset.UtcNow.AddDays(-3)), TestContext.Current.CancellationToken);

        var reportedOrder = new TestOrder
        {
            RequestId = oldRequest.Id,
            Uid = Guid.NewGuid(),
            MethodName = "m1",
            TestName = "t1",
            State = TestOrderState.Reported,
            TestSortOrder = 1
        };

        var pendingOrder = new TestOrder
        {
            RequestId = oldRequest.Id,
            Uid = Guid.NewGuid(),
            MethodName = "m1",
            TestName = "t2",
            State = TestOrderState.PendingApproval,
            TestSortOrder = 2
        };

        dbContext.TestOrders.AddRange(reportedOrder, pendingOrder);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        dbContext.TestOrderCustomParameters.Add(new TestOrderCustomParameter
        {
            TestOrderId = reportedOrder.Id,
            CarrierLot = "L0001",
            DiluentLot = "L0002",
            ConjugateLot = "L0003",
            DevelopmentLot = "L0004",
            CustomRunId = "RUN-01",
            CustomTestId = "TEST-01"
        });
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var requestsBefore = await dbContext.Requests.CountAsync(TestContext.Current.CancellationToken);
        var testOrdersBefore = await dbContext.TestOrders.CountAsync(TestContext.Current.CancellationToken);
        var customParametersBefore = await dbContext.TestOrderCustomParameters.CountAsync(TestContext.Current.CancellationToken);

        // Act
        await mediator.Send(new CleanupRequestsCommand(1), TestContext.Current.CancellationToken);

        // Assert
        (await dbContext.Requests.CountAsync(TestContext.Current.CancellationToken)).Should().Be(requestsBefore);
        (await dbContext.TestOrders.CountAsync(TestContext.Current.CancellationToken)).Should().Be(testOrdersBefore);
        (await dbContext.TestOrderCustomParameters.CountAsync(TestContext.Current.CancellationToken)).Should().Be(customParametersBefore);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5288")]
    public async Task Should_Not_Delete_Request_When_AutomaticDeleteJob_Executes_But_Age_Is_Inside_Retention()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<IRequestDbContext>();
        var sender = ServiceProvider.GetRequiredService<ISender>();
        var logger = ServiceProvider.GetRequiredService<ILogger<AutomaticDeleteJob>>();

        var settings = new Mock<IOptionsSnapshot<AutomaticDeleteSettings>>();
        settings.Setup(x => x.Value).Returns(new AutomaticDeleteSettings
        {
            DeleteRequestsAutomatically = true,
            DeleteRequestsOlderThanDays = 1,
            DeleteResultsAutomatically = false,
            DeleteQualityAutomatically = false
        });

        var freshRequest = new Pharos.Server.Request.Domain.Request
        {
            SampleId = _fixture.Create<string>()[..Constants.SampleIdMaxLength],
            Source = RequestSource.Import,
            LastModified = DateTimeOffset.UtcNow.AddHours(-12)
        };

        dbContext.Requests.Add(freshRequest);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // SaveChanges interceptor updates LastModified to now; keep request explicitly inside retention window.
        await dbContext.Requests
            .Where(r => r.Id == freshRequest.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.LastModified, DateTimeOffset.UtcNow.AddHours(-12)), TestContext.Current.CancellationToken);

        var reportedOrder = new TestOrder
        {
            RequestId = freshRequest.Id,
            Uid = Guid.NewGuid(),
            MethodName = "m1",
            TestName = "t1",
            State = TestOrderState.Reported,
            TestSortOrder = 1
        };

        dbContext.TestOrders.Add(reportedOrder);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        dbContext.TestOrderCustomParameters.Add(new TestOrderCustomParameter
        {
            TestOrderId = reportedOrder.Id,
            CarrierLot = "L0001",
            DiluentLot = "L0002",
            ConjugateLot = "L0003",
            DevelopmentLot = "L0004",
            CustomRunId = "RUN-01",
            CustomTestId = "TEST-01"
        });

        dbContext.ReflexDefinitions.Add(new ReflexDefinition
        {
            TestOrderId = reportedOrder.Uid,
            Name = "RFX1",
            Automatic = true,
            ReflexCondition = new ReflexCondition
            {
                MethodName = reportedOrder.MethodName,
                TestName = reportedOrder.TestName
            },
            ReflexTests =
            [
                new ReflexTest
                {
                    MethodName = "m1",
                    TestName = "t3",
                    NumberOfReplicates = 1,
                    InstrumentDilutionFactor = 1,
                    TestSortOrder = 2
                }
            ]
        });

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var requestsBefore = await dbContext.Requests.CountAsync(TestContext.Current.CancellationToken);
        var testOrdersBefore = await dbContext.TestOrders.CountAsync(TestContext.Current.CancellationToken);
        var customParametersBefore = await dbContext.TestOrderCustomParameters.CountAsync(TestContext.Current.CancellationToken);
        var reflexTestsBefore = await dbContext.ReflexTests.CountAsync(TestContext.Current.CancellationToken);

        var job = new AutomaticDeleteJob(sender, settings.Object, logger);

        // Act
        await job.Execute(Mock.Of<IJobExecutionContext>());

        // Assert
        (await dbContext.Requests.CountAsync(TestContext.Current.CancellationToken)).Should().Be(requestsBefore);
        (await dbContext.TestOrders.CountAsync(TestContext.Current.CancellationToken)).Should().Be(testOrdersBefore);
        (await dbContext.TestOrderCustomParameters.CountAsync(TestContext.Current.CancellationToken)).Should().Be(customParametersBefore);
        (await dbContext.ReflexTests.CountAsync(TestContext.Current.CancellationToken)).Should().Be(reflexTestsBefore);
    }
}
