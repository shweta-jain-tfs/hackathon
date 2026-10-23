using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Server.Application.Lis;
using Pharos.Server.Domain;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Request.Application.Interfaces;
using Pharos.Server.Settings.Application.Interfaces;

namespace Pharos.Server.Integration.Tests;

[Trait("Category", "Integration")]
[Collection(nameof(IntegrationTestsCollection))]
public class TubeDefinitionTests : IntegrationTestBase
{
    private readonly Fixture _fixture;

    public TubeDefinitionTests(IntegrationTestsWaf factory) : base(factory)
    {
        _fixture = _fixture = new Fixture();
    }

    [Fact]
    public async Task Given_UnknownTubeDefinitionId_ShouldSkipSample()
    {
        // Arrange
        var tubeDefinitionId = _fixture.Create<string>().Substring(0, 1);

        await using (var asyncScope = Factory.Services.CreateAsyncScope())
        {
            var mediator = asyncScope.ServiceProvider.GetService<IMediator>()!;
            var settingsDbContext = asyncScope.ServiceProvider.GetRequiredService<ISettingsDbContext>();
            var requestDbContext = asyncScope.ServiceProvider.GetRequiredService<IRequestDbContext>();

            var sampleBarcode = _fixture.Create<string>().Substring(0, Constants.SampleIdMaxLength);

            // Empty seeded tube types
            foreach (var setting in settingsDbContext.Settings)
            {
                settingsDbContext.Settings.Remove(setting);
            }

            await settingsDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            // Arrange the message
            var message = new ProcessLisTestOrdersCommand
            {
                LisChannelId = 0
            };

            message.Patients.Add(new LisPatient
            {
                Samples =
                [
                    new LisSample
                    {
                        SampleId = sampleBarcode,
                        TubeType = tubeDefinitionId
                    }
                ]
            });

            // Act
            await mediator.Send(message, CancellationToken.None);

            // Assert - The sample should not be created in the database
            var request = await requestDbContext.Requests.FirstOrDefaultAsync(r => r.SampleId == sampleBarcode, TestContext.Current.CancellationToken);
            request.Should().BeNull("sample with unknown tube definition should be skipped");
        }
    }
}