using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Contracts.Notifications;
using Pharos.Server.Application.Queries;
using Pharos.Server.Domain;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Request.Application.Interfaces;
using Pharos.Server.Request.Application.Queries.GetRequests;
using Pharos.Server.Request.Domain;
using Pharos.Server.Settings.Application.Interfaces;
using Pharos.Server.Settings.Domain;

namespace Pharos.Server.Integration.Tests.RequestsWebApi;

[Trait("Category", "Integration")]
[Collection(nameof(IntegrationTestsCollection))]
public class DeleteRequestTests : IntegrationTestBase
{
    private readonly Fixture _fixture;

    public DeleteRequestTests(IntegrationTestsWaf factory) : base(factory)
    {
        _fixture = new Fixture();
    }

    [Fact]
    public async Task DeleteRequest_ShouldDeleteASample()
    {
        //Arrange
        var db = ServiceProvider.GetService<IRequestDbContext>()!;

        var request = new Request.Domain.Request
        {
            SampleId = _fixture.Create<string>()[..Constants.SampleIdMaxLength]
        };

        db.Requests.Add(request);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        //Act
        var client = Factory.HttpClient;
        var sampleUri = new Uri(Factory.Server.BaseAddress, "api/requests/");
        var sampleUriGet = new Uri(Factory.Server.BaseAddress, "api/requests/");

        // Before delete
        var requestSummaryDtoBeforeDelete = await client.GetFromJsonAsync<PartialQueryResult<RequestSummaryDto>>(sampleUriGet, TestContext.Current.CancellationToken);
        var countBeforeDelete = await db.Requests.CountAsync(TestContext.Current.CancellationToken);

        var idPushedThroughSignalr = 0;
        await SetUpHubConnection(id => idPushedThroughSignalr = id, HubRoutes.Request, nameof(IEntityHub.Deleted));

        // Delete
        int[] idsToDelete = { request.Id };
        var json = JsonSerializer.Serialize(idsToDelete);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var httpRequest = new HttpRequestMessage();
        httpRequest.Method = HttpMethod.Delete;
        httpRequest.RequestUri = sampleUri;
        httpRequest.Content = content;

        var responseBeforeDelete = await client.SendAsync(httpRequest, TestContext.Current.CancellationToken);

        // After delete
        var responseAfterDelete = await client.GetAsync(sampleUriGet, TestContext.Current.CancellationToken);
        var countAfterDelete = await db.Requests.CountAsync(TestContext.Current.CancellationToken);

        await WaitForHubResponse(() => idPushedThroughSignalr != 0);

        //Assert
        requestSummaryDtoBeforeDelete.Should().NotBeNull();
        requestSummaryDtoBeforeDelete.Items.Should().HaveCount(1);

        countBeforeDelete.Should().Be(1);
        responseBeforeDelete.StatusCode.Should().Be(HttpStatusCode.OK);

        countAfterDelete.Should().Be(0);
        responseAfterDelete.StatusCode.Should().Be(HttpStatusCode.OK);

        idPushedThroughSignalr.Should().Be(request.Id);
    }

    [Fact]
    public async Task DeleteRequest_WithPatient_ShouldDeleteThePatient_WhenNoOtherRequestPointsToPatient()
    {
        //Arrange
        // Add a request with a patient
        var dbRequests = ServiceProvider.GetService<IRequestDbContext>()!;
        var patientName = _fixture.Create<string>();
        var patient = new Patient
        {
            Name = patientName
        };

        var request = new Request.Domain.Request
        {
            SampleId = _fixture.Create<string>()[..Constants.SampleIdMaxLength],
            Patient = patient
        };

        dbRequests.Requests.Add(request);
        await dbRequests.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Add a setting to enable patient information
        var dbSettings = ServiceProvider.GetService<ISettingsDbContext>()!;
        const string settingKey = "Request:IsPatientInformationEnabled";
        var setting = dbSettings.Settings.FirstOrDefault(x => x.Key == settingKey);
        if (setting == null)
        {
            setting = new Setting
            {
                Key = settingKey
            };
            dbSettings.Settings.Add(setting);
        }
        setting.Value = "true";
        await dbSettings.SaveChangesAsync(TestContext.Current.CancellationToken);

        //Act
        var client = Factory.HttpClient;
        var sampleUri = new Uri(Factory.Server.BaseAddress, "api/requests/");
        var sampleUriGet = new Uri(Factory.Server.BaseAddress, "api/requests/");

        // Before delete
        var requestSummaryDtoBeforeDelete = await client.GetFromJsonAsync<PartialQueryResult<RequestSummaryDto>>(sampleUriGet, TestContext.Current.CancellationToken);
        var requestsBeforeDelete = await dbRequests.Requests.CountAsync(TestContext.Current.CancellationToken);
        var patientsBeforeDelete = await dbRequests.Patients.CountAsync(TestContext.Current.CancellationToken);

        // Delete
        int[] idsToDelete = { request.Id };
        var json = JsonSerializer.Serialize(idsToDelete);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var httpRequest = new HttpRequestMessage();
        httpRequest.Method = HttpMethod.Delete;
        httpRequest.RequestUri = sampleUri;
        httpRequest.Content = content;

        await client.SendAsync(httpRequest, TestContext.Current.CancellationToken);

        // After delete
        var requestsAfterDelete = await dbRequests.Requests.CountAsync(TestContext.Current.CancellationToken);
        var patientsAfterDelete = await dbRequests.Patients.CountAsync(TestContext.Current.CancellationToken);

        //Assert
        requestSummaryDtoBeforeDelete.Should().NotBeNull();
        requestSummaryDtoBeforeDelete.Items.First().PatientName.Should().Be(patientName);

        requestsBeforeDelete.Should().Be(1);
        requestsAfterDelete.Should().Be(0);

        patientsBeforeDelete.Should().Be(1);
        patientsAfterDelete.Should().Be(0);
    }

    [Fact]
    public async Task DeleteRequest_WithPatient_ShouldNotDeleteThePatient_SinceOtherRequestPointsToPatient()
    {
        //Arrange
        var db = ServiceProvider.GetService<IRequestDbContext>()!;

        var patientName = _fixture.Create<string>();
        var patient = new Patient
        {
            Name = patientName
        };

        var request1 = new Request.Domain.Request
        {
            SampleId = _fixture.Create<string>()[..Constants.SampleIdMaxLength],
            Patient = patient
        };

        var request2 = new Request.Domain.Request
        {
            SampleId = _fixture.Create<string>()[..Constants.SampleIdMaxLength],
            Patient = patient
        };

        db.Requests.Add(request1);
        db.Requests.Add(request2);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        //Act
        var client = Factory.HttpClient;
        var sampleUri = new Uri(Factory.Server.BaseAddress, "api/requests/");

        // Before delete
        var requestsBeforeDelete = await db.Requests.CountAsync(TestContext.Current.CancellationToken);
        var patientsBeforeDelete = await db.Patients.CountAsync(TestContext.Current.CancellationToken);

        // Delete
        int[] idsToDelete = { request1.Id };
        var json = JsonSerializer.Serialize(idsToDelete);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var httpRequest = new HttpRequestMessage();
        httpRequest.Method = HttpMethod.Delete;
        httpRequest.RequestUri = sampleUri;
        httpRequest.Content = content;

        await client.SendAsync(httpRequest, TestContext.Current.CancellationToken);

        // After delete
        var requestsAfterDelete = await db.Requests.CountAsync(TestContext.Current.CancellationToken);
        var patientsAfterDelete = await db.Patients.CountAsync(TestContext.Current.CancellationToken);

        //Assert
        requestsBeforeDelete.Should().Be(2);
        requestsAfterDelete.Should().Be(1);

        patientsBeforeDelete.Should().Be(1);
        patientsAfterDelete.Should().Be(1);
    }
}