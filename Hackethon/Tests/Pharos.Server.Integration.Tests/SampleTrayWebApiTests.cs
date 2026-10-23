using Pharos.Server.Integration.Tests.Common;

namespace Pharos.Server.Integration.Tests;

//ToDo: Test cases should be updated once the actual implementation is done
[Trait("Category", "Integration")]
[Collection(nameof(IntegrationTestsCollection))]
public class SampleTrayWebApiTests : IntegrationTestBase
{
    //private readonly Fixture _fixture;

    public SampleTrayWebApiTests(IntegrationTestsWaf factory) : base(factory)
    {
        // _fixture = new Fixture();
    }

    /*[Fact]
    public async Task GetSampleTrayShouldReturnSeededTray()
    {
        //Arrange
        var db = ServiceProvider.GetService<ISampleDbContext>()!;

        var sampleTray = new SampleTray
        {
            TrayId = "1234",
            Positions = new List<SampleTrayPosition>()
        };

        db.SampleTrays.Add(sampleTray);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        //Act
        var client = Factory.HttpClient;
        var sampleTrayDto = await client.GetFromJsonAsync<SampleTrayDto>(new Uri(Factory.Server.BaseAddress, $"api/sampletrays/{sampleTray.Id}"));

        //Assert
        sampleTrayDto.Should().NotBeNull();
        sampleTrayDto!.TrayId.Should().Be(sampleTray.TrayId);

        var response = await client.GetAsync(new Uri(Factory.Server.BaseAddress, "api/sampletrays"));
        response.Should().NotBeNull();
        response.EnsureSuccessStatusCode().StatusCode.Should().Be(HttpStatusCode.OK);
    }


    

       public async Task UpdateSampleTrayCommand_ShouldUpdateDisplayName()
       {
           var client = Factory.HttpClient;

           var barcode = "1234";
           var displayName = _fixture.Create<string>()[..Constants.TrayIdLength];

           //Create new tray
           var newTray = new CreateSampleTrayCommand
           {
               Barcode = barcode,
               DisplayName = displayName
           };

           string newSampleTrayId;

           using (var createContent = new StringContent(JsonSerializer.Serialize(newTray), Encoding.UTF8, "application/json"))
           {
               var response = await client.PostAsync(new Uri(Factory.Server.BaseAddress, "api/sampletrays"), createContent);
               newSampleTrayId = await response.Content.ReadAsStringAsync();
               var requestUri = new Uri(Factory.Server.BaseAddress + $"api/sampletrays/{newSampleTrayId}");

               var sampleTray = await client.<SampleTrayDto>(requestUri, TestContext.Current.CancellationToken);
               sampleTray.Should().NotBeNull();
               sampleTray!.TrayId.Should().Be(barcode);
           }

           //Update tray
           var updatedDisplayName = _fixture.Create<string>()[..Constants.TrayIdLength];
           var updateTray = new UpdateSampleTrayCommand
           {
               DisplayName = updatedDisplayName
           };

           using (var content = new StringContent(JsonSerializer.Serialize(updateTray), Encoding.UTF8, "application/json"))
           {
               var requestUri = new Uri(Factory.Server.BaseAddress + $"api/sampletrays/{newSampleTrayId}");
               await client.PostAsync(requestUri, content, TestContext.Current.CancellationToken);

               var sampleTray = await client.<SampleTrayDto>(requestUri, TestContext.Current.CancellationToken);
               sampleTray.Should().NotBeNull();
           }
       }

    [Fact]
    public async Task DeleteSampleTray_Should_DeleteSampleTrayFromDatabase()
    {
        //Arrange
        var db = ServiceProvider.GetService<ISampleDbContext>()!;

        var sampleTray = new SampleTray
        {
            TrayId = "1234"
        };

        db.SampleTrays.Add(sampleTray);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        //Act
        var client = Factory.HttpClient;
        var sampleTrayUri = new Uri(Factory.Server.BaseAddress, $"api/sampletrays/{sampleTray.Id}");

        var requestDtoBeforeDelete = await client.<SampleTrayDto>(sampleTrayUri, TestContext.Current.CancellationToken);
        var countBeforeDelete = await db.SampleTrays.CountAsync(TestContext.Current.CancellationToken);

        var idPushedThroughSignalr = 0;
        await SetUpHubConnection(id => idPushedThroughSignalr = id, HubRoutes.SampleTray, nameof(IEntityHub.Deleted));

        var deleteResponse = await client.DeleteAsync(sampleTrayUri, TestContext.Current.CancellationToken);

        var requestDtoAfterDelete = await client.GetAsync(sampleTrayUri, TestContext.Current.CancellationToken);
        var countAfterDelete = await db.SampleTrays.CountAsync(TestContext.Current.CancellationToken);

        await WaitForHubResponse(() => idPushedThroughSignalr != 0);

        //Assert
        requestDtoBeforeDelete.Should().NotBeNull();
        countBeforeDelete.Should().Be(1);
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        countAfterDelete.Should().Be(0);
        requestDtoAfterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
        idPushedThroughSignalr.Should().Be(sampleTray.Id);
    }

    [Fact]
    public async Task DeleteSampleTray_With_InstrumentId_Should_NotBeDeleted()
    {
        //Arrange
        var db = ServiceProvider.GetService<ISampleDbContext>()!;
        var sampleTrayBarcode = "1234";

        var sampleTray = new SampleTray
        {
            TrayId = sampleTrayBarcode
        };

        db.SampleTrays.Add(sampleTray);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var (_, instrument) = await Factory.AddInstrumentConnection();
        instrument.Storage!.SampleTraysDrawer1.Add(new Tetris.Protocol.Common.SampleTray
        {
            TrayBarcode = sampleTrayBarcode,
            TrayPosition = 1
        });

        //Act
        var client = Factory.HttpClient;
        var sampleTrayUri = new Uri(Factory.Server.BaseAddress, $"api/sampletrays/{sampleTray.Id}");

        var requestDtoBeforeDelete = await client.<SampleTrayDto>(sampleTrayUri, TestContext.Current.CancellationToken);
        var countBeforeDelete = await db.SampleTrays.CountAsync(TestContext.Current.CancellationToken);

        var deleteResponse = await client.DeleteAsync(sampleTrayUri, TestContext.Current.CancellationToken);

        var requestDtoAfterDelete = await client.GetAsync(sampleTrayUri, TestContext.Current.CancellationToken);
        var countAfterDelete = await db.SampleTrays.CountAsync(TestContext.Current.CancellationToken);

        //Assert
        requestDtoBeforeDelete.Should().NotBeNull();
        countBeforeDelete.Should().Be(1);
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        countAfterDelete.Should().Be(1);
        requestDtoAfterDelete.StatusCode.Should().Be(HttpStatusCode.OK);
    }*/
}