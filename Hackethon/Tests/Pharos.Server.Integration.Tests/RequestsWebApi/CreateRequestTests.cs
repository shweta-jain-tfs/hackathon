using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Parameters;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Localization;
using Pharos.Server.Parameters.Application.Interfaces;
using Pharos.Server.Request.Application.Commands.CreateUpdateRequest;
using Pharos.Server.Request.Application.Interfaces;

namespace Pharos.Server.Integration.Tests.RequestsWebApi;

[Trait("Category", "Integration")]
[Collection(nameof(IntegrationTestsCollection))]
public class CreateRequestTests : IntegrationTestBase
{
    private readonly Fixture _fixture;

    public CreateRequestTests(IntegrationTestsWaf factory) : base(factory)
    {
        _fixture = new Fixture();
    }

    [Fact]
    public async Task CreateRequest_WhenSampleIdAlreadyExists_ShouldFail()
    {
        //Arrange
        var db = ServiceProvider.GetService<IRequestDbContext>()!;

        var sample = new Request.Domain.Request
        {
            SampleId = _fixture.Create<string>()[..Constants.SampleIdMaxLength]
        };

        db.Requests.Add(sample);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        //Act
        var client = Factory.HttpClient;
        var requestUri = new Uri(Factory.Server.BaseAddress, "api/requests/");

        var command = new CreateRequestCommand
        {
            SampleId = sample.SampleId,
            PreDilutionFactor = 1,
            SampleTubeType = await GetValidSampleTubeType()
        };

        var response = await client.PostAsJsonAsync(requestUri, command, TestContext.Current.CancellationToken);

        //Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var validationProblem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestContext.Current.CancellationToken);
        validationProblem.Should().NotBeNull();
        validationProblem.Title.Should().Be("One or more validation errors occurred.");
        validationProblem.Errors.Count.Should().Be(1);
        validationProblem.Errors[""].Should().BeEquivalentTo($"Sample with ID {sample.SampleId} already exists.");
    }

    [Fact]
    public async Task CreateRequest_WhenPreDilutionFactorIsToLargeInProduction_ShouldNotGivebackGivenValue()
    {
        //Arrange
        var env = ServiceProvider.GetService<IHostEnvironment>()!;
        var originalEnvironmentName = env.EnvironmentName;
        env.EnvironmentName = Environments.Production;

        try
        {
            var db = ServiceProvider.GetService<IRequestDbContext>()!;

            var sample = new Request.Domain.Request
            {
                SampleId = _fixture.Create<string>()[..Constants.SampleIdMaxLength]
            };

            db.Requests.Add(sample);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);

            //Act
            var client = Factory.HttpClient;
            var requestUri = new Uri(Factory.Server.BaseAddress, "api/requests/");

            var command = new CreateRequestCommand
            {
                SampleId = sample.SampleId,
                PreDilutionFactor = 2000,
                SampleTubeType = await GetValidSampleTubeType()
            };

            var response = await client.PostAsJsonAsync(requestUri, command, TestContext.Current.CancellationToken);

            //Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var validationProblem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestContext.Current.CancellationToken);
            validationProblem.Should().NotBeNull();
            validationProblem.Title.Should().Be("One or more validation errors occurred.");
            validationProblem.Errors.Count.Should().Be(1);
            validationProblem.Errors["PreDilutionFactor"].Should().BeEquivalentTo();
        }
        finally
        {
            env.EnvironmentName = originalEnvironmentName;
        }
    }

    [Fact]
    public async Task CreateRequest_WhenPreDilutionFactorIsToLargeInDevelopment_ShouldGivebackGivenValue()
    {
        //Arrange
        var env = ServiceProvider.GetService<IHostEnvironment>()!;
        var originalEnvironmentName = env.EnvironmentName;
        env.EnvironmentName = Environments.Development;

        try
        {
            var db = ServiceProvider.GetService<IRequestDbContext>()!;

            var sample = new Request.Domain.Request
            {
                SampleId = _fixture.Create<string>()[..Constants.SampleIdMaxLength]
            };

            db.Requests.Add(sample);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);

            //Act
            var client = Factory.HttpClient;
            var requestUri = new Uri(Factory.Server.BaseAddress, "api/requests/");

            var command = new CreateRequestCommand
            {
                SampleId = sample.SampleId,
                PreDilutionFactor = 2000,
                SampleTubeType = await GetValidSampleTubeType()
            };

            var response = await client.PostAsJsonAsync(requestUri, command, TestContext.Current.CancellationToken);

            //Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var validationProblem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestContext.Current.CancellationToken);
            validationProblem.Should().NotBeNull();
            validationProblem.Title.Should().Be("One or more validation errors occurred.");
            validationProblem.Errors.Count.Should().Be(1);
            validationProblem.Errors["PreDilutionFactor"].Should().BeEquivalentTo("'Pre Dilution Factor' must be between 0 and 1000. You entered 2000.");
        }
        finally
        {
            env.EnvironmentName = originalEnvironmentName;
        }
    }

    [Fact]
    public async Task CreateRequest_WhenNotAllTestsFromPanelHasBeenAdded_ShouldFail()
    {
        //Arrange
        var panel = await CreatePanel("p1", ["c1", "c2"]);

        //Act
        var client = Factory.HttpClient;
        var requestUri = new Uri(Factory.Server.BaseAddress, "api/requests/");

        var command = new CreateRequestCommand
        {
            SampleId = _fixture.Create<string>()[..Constants.SampleIdMaxLength],
            PreDilutionFactor = 1,
            SampleTubeType = await GetValidSampleTubeType(),
            TestOrders =
            [
                new NewUpdatedTest
                {
                    MethodName = "sIgE",
                    TestName = "c1",
                    InstrumentDilutionFactor = 1,
                    NumberOfReplicates = 1,
                    TestPanelIds = [panel.Id]
                }
            ]
        };

        var response = await client.PostAsJsonAsync(requestUri, command, TestContext.Current.CancellationToken);

        //Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var validationProblem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestContext.Current.CancellationToken);
        validationProblem.Should().NotBeNull();
        validationProblem.Title.Should().Be("One or more validation errors occurred.");
        validationProblem.Errors.Count.Should().Be(1);
        validationProblem.Errors[""].Should().BeEquivalentTo("Panel 'p1' contains test 'c2' which cannot be found in the list of tests");
    }

    [Fact]
    public async Task CreateRequest_WhenAllTestsForPanelHasBeenAdded_ShouldSucceed()
    {
        //Arrange
        var panel = await CreatePanel("p1", ["c1", "c2"]);

        //Act
        var client = Factory.HttpClient;
        var requestUri = new Uri(Factory.Server.BaseAddress, "api/requests/");

        var command = new CreateRequestCommand
        {
            SampleId = _fixture.Create<string>()[..Constants.SampleIdMaxLength],
            PreDilutionFactor = 1,
            SampleTubeType = await GetValidSampleTubeType(),
            TestOrders =
            [
                new NewUpdatedTest
                {
                    MethodName = "sIgE",
                    TestName = "c1",
                    InstrumentDilutionFactor = 1,
                    NumberOfReplicates = 1,
                    TestPanelIds = [panel.Id]
                },
                new NewUpdatedTest
                {
                    MethodName = "sIgE",
                    TestName = "c2",
                    InstrumentDilutionFactor = 1,
                    NumberOfReplicates = 1,
                    TestPanelIds = [panel.Id]
                }
            ]
        };

        var response = await client.PostAsJsonAsync(requestUri, command, TestContext.Current.CancellationToken);

        //Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateRequest_WhenNotAllTestsAreTaggedWithPanel_ShouldFail()
    {
        //Arrange
        var panel = await CreatePanel("p1", ["c1", "c2"]);

        //Act
        var client = Factory.HttpClient;
        var requestUri = new Uri(Factory.Server.BaseAddress, "api/requests/");

        var command = new CreateRequestCommand
        {
            SampleId = _fixture.Create<string>()[..Constants.SampleIdMaxLength],
            PreDilutionFactor = 1,
            SampleTubeType = await GetValidSampleTubeType(),
            TestOrders =
            [
                new NewUpdatedTest
                {
                    MethodName = "sIgE",
                    TestName = "c1",
                    InstrumentDilutionFactor = 1,
                    NumberOfReplicates = 1,
                    TestPanelIds = [panel.Id]
                },
                new NewUpdatedTest
                {
                    MethodName = "sIgE",
                    TestName = "c2",
                    InstrumentDilutionFactor = 1,
                    NumberOfReplicates = 1,
                    TestPanelIds = []
                }
            ]
        };

        var response = await client.PostAsJsonAsync(requestUri, command, TestContext.Current.CancellationToken);

        //Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var validationProblem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestContext.Current.CancellationToken);
        validationProblem.Should().NotBeNull();
        validationProblem.Title.Should().Be("One or more validation errors occurred.");
        validationProblem.Errors.Count.Should().Be(1);
        validationProblem.Errors[""].Should().BeEquivalentTo("Test 'c2' is missing panel id '1'");
    }

    [Fact]
    public async Task CreateRequest_WhenUsingNonExistingPanel_ShouldFail()
    {
        // Arrange
        var db = ServiceProvider.GetRequiredService<IParametersDbContext>();
        await db.CreateMethod(testNames: ["c1"]);

        //Act
        var client = Factory.HttpClient;
        var requestUri = new Uri(Factory.Server.BaseAddress, "api/requests/");

        var command = new CreateRequestCommand
        {
            SampleId = _fixture.Create<string>()[..Constants.SampleIdMaxLength],
            PreDilutionFactor = 1,
            SampleTubeType = await GetValidSampleTubeType(),
            TestOrders =
            [
                new NewUpdatedTest
                {
                    MethodName = "sIgE",
                    TestName = "c1",
                    InstrumentDilutionFactor = 1,
                    NumberOfReplicates = 1,
                    TestPanelIds = [1] //<-- panel id 1 should not exist
                }
            ]
        };

        var response = await client.PostAsJsonAsync(requestUri, command, TestContext.Current.CancellationToken);

        //Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var validationProblem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestContext.Current.CancellationToken);
        validationProblem.Should().NotBeNull();
        validationProblem.Title.Should().Be("One or more validation errors occurred.");
        validationProblem.Errors.Count.Should().Be(1);
        validationProblem.Errors[""].Should().BeEquivalentTo("Panel with id '1' cannot be found");
    }

    [Fact]
    public async Task CreateRequest_AddDuplicateTest_ShouldFail()
    {
        // Arrange
        var parametersDb = ServiceProvider.GetRequiredService<IParametersDbContext>();
        await parametersDb.CreateMethod(testNames: ["c1"]);

        // Act
        var client = Factory.HttpClient;
        var requestUri = new Uri(Factory.Server.BaseAddress, "api/requests/");

        var command = new CreateRequestCommand
        {
            SampleId = _fixture.Create<string>()[..Constants.SampleIdMaxLength],
            PreDilutionFactor = 1,
            SampleTubeType = await GetValidSampleTubeType(),
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

        var response = await client.PostAsJsonAsync(requestUri, command, TestContext.Current.CancellationToken);

        //Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var validationProblem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestContext.Current.CancellationToken);
        validationProblem.Should().NotBeNull();
        validationProblem.Title.Should().Be("One or more validation errors occurred.");
        validationProblem.Errors.Count.Should().Be(1);
        validationProblem.Errors[""].Should().BeEquivalentTo(Translations.TestOrderDuplicate);
    }

    [Fact]
    public async Task CreateRequest_InvalidMethod_ShouldFail()
    {
        //Act
        var client = Factory.HttpClient;
        var requestUri = new Uri(Factory.Server.BaseAddress, "api/requests/");

        var command = new CreateRequestCommand
        {
            SampleId = _fixture.Create<string>()[..Constants.SampleIdMaxLength],
            PreDilutionFactor = 1,
            SampleTubeType = await GetValidSampleTubeType(),
            TestOrders =
            [
                new NewUpdatedTest
                {
                    MethodName = "sIgE",
                    TestName = "c1",
                    TestPanelIds = [],
                    InstrumentDilutionFactor = 1,
                    NumberOfReplicates = 1
                }
            ]
        };

        var response = await client.PostAsJsonAsync(requestUri, command, TestContext.Current.CancellationToken);

        //Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var validationProblem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestContext.Current.CancellationToken);
        validationProblem.Should().NotBeNull();
        validationProblem.Title.Should().Be("One or more validation errors occurred.");
        validationProblem.Errors.Count.Should().Be(1);
        validationProblem.Errors[""].Should().BeEquivalentTo("Invalid method name specified. Check that all needed methods are enabled in parameters.");
    }

    [Fact]
    public async Task CreateRequest_InvalidTest_ShouldFail()
    {
        // Arrange
        var parametersDb = ServiceProvider.GetRequiredService<IParametersDbContext>();
        await parametersDb.CreateMethod();

        //Act
        var client = Factory.HttpClient;
        var requestUri = new Uri(Factory.Server.BaseAddress, "api/requests/");

        var command = new CreateRequestCommand
        {
            SampleId = _fixture.Create<string>()[..Constants.SampleIdMaxLength],
            PreDilutionFactor = 1,
            SampleTubeType = await GetValidSampleTubeType(),
            TestOrders =
            [
                new NewUpdatedTest
                {
                    MethodName = "sIgE",
                    TestName = "c1",
                    TestPanelIds = [],
                    InstrumentDilutionFactor = 1,
                    NumberOfReplicates = 1
                }
            ]
        };

        var response = await client.PostAsJsonAsync(requestUri, command, TestContext.Current.CancellationToken);

        //Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var validationProblem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestContext.Current.CancellationToken);
        validationProblem.Should().NotBeNull();
        validationProblem.Title.Should().Be("One or more validation errors occurred.");
        validationProblem.Errors.Count.Should().Be(1);
        validationProblem.Errors[""].Should().BeEquivalentTo("Invalid test name specified. Check that all needed tests are enabled in parameters.");
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

    private Task<int> GetValidSampleTubeType() => RequestTestsHelper.GetValidSampleTubeType(ServiceProvider);
}
