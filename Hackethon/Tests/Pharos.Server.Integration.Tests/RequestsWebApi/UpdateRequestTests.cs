using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Parameters;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Localization;
using Pharos.Server.Parameters.Application.Interfaces;
using Pharos.Server.Request.Application.Commands.CreateUpdateRequest;
using Pharos.Server.Request.Application.Interfaces;
using Pharos.Server.Request.Domain;

namespace Pharos.Server.Integration.Tests.RequestsWebApi;

[Trait("Category", "Integration")]
[Collection(nameof(IntegrationTestsCollection))]
public class UpdateRequestTests : IntegrationTestBase
{
    private readonly Fixture _fixture;

    public UpdateRequestTests(IntegrationTestsWaf factory) : base(factory)
    {
        _fixture = new Fixture();
    }

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        await RequestTestsHelper.EnsureTubeTypeExists(ServiceProvider, 1);
    }

    [Fact]
    public async Task UpdateRequest_WhenDeletingOneTestOrderThatHasPanelButNotRemovingTheConnectionForTheOtherTest_ShouldFail()
    {
        //Arrange
        var panel = await CreatePanel("p1", ["c1", "c2"]);

        var db = ServiceProvider.GetRequiredService<IRequestDbContext>();

        var sample = new Request.Domain.Request
        {
            SampleId = _fixture.Create<string>()[..Constants.SampleIdMaxLength],
            TestOrders =
            [
                new TestOrder
                {
                    Uid = Guid.NewGuid(),
                    TestName = "c1",
                    MethodName = "sIgE",
                    InstrumentDilutionFactor = 1,
                    NumberOfReplicates = 1,
                    TestPanelIds = [panel.Id]
                },
                new TestOrder
                {
                    Uid = Guid.NewGuid(),
                    TestName = "c2",
                    MethodName = "sIgE",
                    InstrumentDilutionFactor = 1,
                    NumberOfReplicates = 1,
                    TestPanelIds = [panel.Id]
                }
            ]
        };

        db.Requests.Add(sample);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        //Act
        var client = Factory.HttpClient;
        var requestUri = new Uri(Factory.Server.BaseAddress, $"api/requests/{sample.Id}");

        var command = new UpdateRequestCommand
        {
            Id = sample.Id,
            PreDilutionFactor = 1,
            SampleTubeType = 1,
            RequestDetailsQueriedAt = DateTimeOffset.Now,
            TestOrders =
            [
                new NewUpdatedTest
                {
                    Id = sample.TestOrders[0].Id,
                    MethodName = "sIgE",
                    TestName = "c1",
                    TestPanelIds = [1],
                    InstrumentDilutionFactor = 1,
                    NumberOfReplicates = 1
                }
            ]
        };

        var response = await client.PutAsJsonAsync(requestUri, command, TestContext.Current.CancellationToken);

        //Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var validationProblem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestContext.Current.CancellationToken);
        validationProblem.Should().NotBeNull();
        validationProblem.Title.Should().Be("One or more validation errors occurred.");
        validationProblem.Errors.Count.Should().Be(1);
        validationProblem.Errors[""].Should().BeEquivalentTo(["Panel 'p1' contains test 'c2' which cannot be found in the list of tests"]);
    }

    [Fact]
    public async Task UpdateRequest_WhenUsingNonExistingPanel_ShouldFail()
    {
        //Arrange
        var parametersDb = ServiceProvider.GetRequiredService<IParametersDbContext>();
        await parametersDb.CreateMethod(testNames: ["c1"]);

        var db = ServiceProvider.GetService<IRequestDbContext>()!;

        var sample = new Request.Domain.Request
        {
            SampleId = _fixture.Create<string>()[..Constants.SampleIdMaxLength],
            TestOrders = []
        };

        db.Requests.Add(sample);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        //Act
        var client = Factory.HttpClient;
        var requestUri = new Uri(Factory.Server.BaseAddress, $"api/requests/{sample.Id}");

        var command = new UpdateRequestCommand
        {
            Id = sample.Id,
            PreDilutionFactor = 1,
            SampleTubeType = 1,
            RequestDetailsQueriedAt = DateTimeOffset.Now,
            TestOrders =
            [
                new NewUpdatedTest
                {
                    MethodName = "sIgE",
                    TestName = "c1",
                    TestPanelIds = [1], //<-- panel does not exist
                    InstrumentDilutionFactor = 1,
                    NumberOfReplicates = 1
                }
            ]
        };

        var response = await client.PutAsJsonAsync(requestUri, command, TestContext.Current.CancellationToken);

        //Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var validationProblem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestContext.Current.CancellationToken);
        validationProblem.Should().NotBeNull();
        validationProblem.Title.Should().Be("One or more validation errors occurred.");
        validationProblem.Errors.Count.Should().Be(1);
        validationProblem.Errors[""].Should().BeEquivalentTo(["Panel with id '1' cannot be found"]);
    }

    [Fact]
    public async Task UpdateRequest_WhenNotAllTestsAreTaggedWithPanel_ShouldFail()
    {
        //Arrange
        var panel = await CreatePanel("p1", ["c1", "c2"]);

        var db = ServiceProvider.GetService<IRequestDbContext>()!;

        var sample = new Request.Domain.Request
        {
            SampleId = _fixture.Create<string>()[..Constants.SampleIdMaxLength],
            TestOrders = []
        };

        db.Requests.Add(sample);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        //Act
        var client = Factory.HttpClient;
        var requestUri = new Uri(Factory.Server.BaseAddress, $"api/requests/{sample.Id}");

        var command = new UpdateRequestCommand
        {
            Id = sample.Id,
            PreDilutionFactor = 1,
            SampleTubeType = 1,
            RequestDetailsQueriedAt = DateTimeOffset.Now,
            TestOrders =
            [
                new NewUpdatedTest
                {
                    MethodName = "sIgE",
                    TestName = "c1",
                    TestPanelIds = [panel.Id],
                    InstrumentDilutionFactor = 1,
                    NumberOfReplicates = 1
                },
                new NewUpdatedTest
                {
                    MethodName = "sIgE",
                    TestName = "c2",
                    TestPanelIds = [],
                    InstrumentDilutionFactor = 1,
                    NumberOfReplicates = 1
                }
            ]
        };

        var response = await client.PutAsJsonAsync(requestUri, command, TestContext.Current.CancellationToken);

        //Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var validationProblem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestContext.Current.CancellationToken);
        validationProblem.Should().NotBeNull();
        validationProblem.Title.Should().Be("One or more validation errors occurred.");
        validationProblem.Errors.Count.Should().Be(1);
        validationProblem.Errors[""].Should().BeEquivalentTo(["Test 'c2' is missing panel id '1'"]);
    }

    [Fact]
    public async Task UpdateRequest_WhenNotAllTestsFromPanelHasBeenAdded_ShouldFail()
    {
        //Arrange
        var panel = await CreatePanel("p1", ["c1", "c2"]);

        var db = ServiceProvider.GetService<IRequestDbContext>()!;

        var sample = new Request.Domain.Request
        {
            SampleId = _fixture.Create<string>()[..Constants.SampleIdMaxLength],
            TestOrders = []
        };

        db.Requests.Add(sample);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        //Act
        var client = Factory.HttpClient;
        var requestUri = new Uri(Factory.Server.BaseAddress, $"api/requests/{sample.Id}");

        var command = new UpdateRequestCommand
        {
            Id = sample.Id,
            PreDilutionFactor = 1,
            SampleTubeType = 1,
            RequestDetailsQueriedAt = DateTimeOffset.Now,
            TestOrders =
            [
                new NewUpdatedTest
                {
                    MethodName = "sIgE",
                    TestName = "c1",
                    TestPanelIds = [panel.Id],
                    InstrumentDilutionFactor = 1,
                    NumberOfReplicates = 1
                }
            ]
        };

        var response = await client.PutAsJsonAsync(requestUri, command, TestContext.Current.CancellationToken);

        //Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var validationProblem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestContext.Current.CancellationToken);
        validationProblem.Should().NotBeNull();
        validationProblem.Title.Should().Be("One or more validation errors occurred.");
        validationProblem.Errors.Count.Should().Be(1);
        validationProblem.Errors[""].Should().BeEquivalentTo(["Panel 'p1' contains test 'c2' which cannot be found in the list of tests"]);
    }

    [Fact]
    public async Task UpdateRequest_AddDuplicateTest_ShouldFail()
    {
        //Arrange
        var parametersDb = ServiceProvider.GetRequiredService<IParametersDbContext>();
        await parametersDb.CreateMethod(testNames: ["c1"]);

        var db = ServiceProvider.GetService<IRequestDbContext>()!;
        var sample = new Request.Domain.Request
        {
            SampleId = _fixture.Create<string>()[..Constants.SampleIdMaxLength],
            TestOrders = []
        };

        db.Requests.Add(sample);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        //Act
        var client = Factory.HttpClient;
        var requestUri = new Uri(Factory.Server.BaseAddress, $"api/requests/{sample.Id}");

        var command = new UpdateRequestCommand
        {
            Id = sample.Id,
            PreDilutionFactor = 1,
            SampleTubeType = 1,
            RequestDetailsQueriedAt = DateTimeOffset.Now,
            TestOrders =
            [
                new NewUpdatedTest
                {
                    MethodName = "sIgE",
                    TestName = "c1", // c1 here
                    TestPanelIds = [],
                    InstrumentDilutionFactor = 1,
                    NumberOfReplicates = 1
                },
                new NewUpdatedTest
                {
                    MethodName = "sIgE",
                    TestName = "c1", // and c1 here
                    TestPanelIds = [],
                    InstrumentDilutionFactor = 1,
                    NumberOfReplicates = 1
                }
            ]
        };

        var response = await client.PutAsJsonAsync(requestUri, command, TestContext.Current.CancellationToken);

        //Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var validationProblem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestContext.Current.CancellationToken);
        validationProblem.Should().NotBeNull();
        validationProblem.Title.Should().Be("One or more validation errors occurred.");
        validationProblem.Errors.Count.Should().Be(1);
        validationProblem.Errors[""].Should().BeEquivalentTo([Translations.TestOrderDuplicate]);
    }

    [Fact]
    public async Task UpdateRequest_AddTestThatAlreadyExists_ShouldFail()
    {
        //Arrange
        var parametersDb = ServiceProvider.GetRequiredService<IParametersDbContext>();
        await parametersDb.CreateMethod(testNames: ["c1"]);

        var db = ServiceProvider.GetService<IRequestDbContext>()!;

        var sample = new Request.Domain.Request
        {
            SampleId = _fixture.Create<string>()[..Constants.SampleIdMaxLength],
            TestOrders =
            [
                new TestOrder
                {
                    TestName = "c1",
                    MethodName = "sIgE",
                    InstrumentDilutionFactor = 1,
                    NumberOfReplicates = 1,
                    TestPanelIds = []
                }
            ]
        };

        db.Requests.Add(sample);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        //Act
        var client = Factory.HttpClient;
        var requestUri = new Uri(Factory.Server.BaseAddress, $"api/requests/{sample.Id}");

        var command = new UpdateRequestCommand
        {
            Id = sample.Id,
            PreDilutionFactor = 1,
            SampleTubeType = 1,
            RequestDetailsQueriedAt = DateTimeOffset.Now,
            TestOrders =
            [
                new NewUpdatedTest
                {
                    Id = sample.TestOrders[0].Id,
                    MethodName = "sIgE",
                    TestName = "c1", // c1 here
                    TestPanelIds = [],
                    InstrumentDilutionFactor = 1,
                    NumberOfReplicates = 1
                },
                new NewUpdatedTest
                {
                    MethodName = "sIgE",
                    TestName = "c1", // and c1 here
                    TestPanelIds = [],
                    InstrumentDilutionFactor = 1,
                    NumberOfReplicates = 1
                }
            ]
        };

        var response = await client.PutAsJsonAsync(requestUri, command, TestContext.Current.CancellationToken);

        //Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var validationProblem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestContext.Current.CancellationToken);
        validationProblem.Should().NotBeNull();
        validationProblem.Title.Should().Be("One or more validation errors occurred.");
        validationProblem.Errors.Count.Should().Be(1);
        validationProblem.Errors[""].Should().BeEquivalentTo([Translations.TestOrderDuplicate]);
    }

    [Fact]
    public async Task UpdateRequest_UpdateAddAnotherTest_ShouldSucceed()
    {
        //Arrange
        var parametersDb = ServiceProvider.GetRequiredService<IParametersDbContext>();
        await parametersDb.CreateMethod(testNames: ["c1", "c2"]);

        var db = ServiceProvider.GetService<IRequestDbContext>()!;

        var sample = new Request.Domain.Request
        {
            SampleId = _fixture.Create<string>()[..Constants.SampleIdMaxLength],
            TestOrders =
            [
                new TestOrder
                {
                    TestName = "c1",
                    MethodName = "sIgE",
                    InstrumentDilutionFactor = 1,
                    NumberOfReplicates = 1,
                    TestPanelIds = []
                }
            ]
        };

        db.Requests.Add(sample);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        //Act
        var client = Factory.HttpClient;
        var requestUri = new Uri(Factory.Server.BaseAddress, $"api/requests/{sample.Id}");

        var command = new UpdateRequestCommand
        {
            Id = sample.Id,
            PreDilutionFactor = 1,
            SampleTubeType = 1,
            RequestDetailsQueriedAt = DateTimeOffset.Now,
            TestOrders =
            [
                new NewUpdatedTest
                {
                    Id = sample.TestOrders[0].Id,
                    MethodName = "sIgE",
                    TestName = "c1",
                    TestPanelIds = [],
                    InstrumentDilutionFactor = 1,
                    NumberOfReplicates = 1
                },
                new NewUpdatedTest
                {
                    MethodName = "sIgE",
                    TestName = "c2",
                    TestPanelIds = [],
                    InstrumentDilutionFactor = 1,
                    NumberOfReplicates = 1
                }
            ]
        };

        var response = await client.PutAsJsonAsync(requestUri, command, TestContext.Current.CancellationToken);

        //Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<Panel> CreatePanel(string panelName, string[] testNames)
    {
        var db = ServiceProvider.GetRequiredService<IParametersDbContext>();
        var method = await db.CreateMethod(testNames: testNames);

        var panel = new Panel
        {
            PanelLongName = panelName,
            PanelName = panelName,
            Tests = new List<PanelTest>()
        };
        foreach (var testName in testNames)
        {
            panel.Tests.Add(new PanelTest
            {
                MethodTest = method.Tests.First(t => t.TestName == testName)
            });
        }

        db.Panels.Add(panel);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        return panel;
    }
}
