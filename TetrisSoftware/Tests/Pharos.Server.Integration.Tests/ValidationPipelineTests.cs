using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Server.Application.Sample;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Request.Application.Commands.CreateUpdateRequest;
using Tetris.Protocol.Common;
using Tetris.Protocol.Pharos;

namespace Pharos.Server.Integration.Tests;

[Trait("Category", "Integration")]
[Collection(nameof(IntegrationTestsCollection))]
public class ValidationPipelineTests : IntegrationTestBase
{
    public ValidationPipelineTests(IntegrationTestsWaf factory) : base(factory)
    {
    }

    [Fact]
    public async Task GivenTooLongBarcodeProcessSampleBarcodeShouldThrow()
    {
        var mediator = ServiceProvider.GetService<IMediator>()!;

        var message = new ScannedSampleQcRequest
        {
            TubeBarcode = "123456789012345678901234567890123",
            CurrentPosition = new SampleTubePosition()
        };

        var ex = await Assert.ThrowsAsync<ValidationException>(
                     async () => await mediator.Send(new ProcessScannedTubeCommand("I_1", message), TestContext.Current.CancellationToken));

        Assert.Equal("The length of 'Message Tube Barcode' must be 32 characters or fewer. You entered 33 characters.", ex.Errors.FirstOrDefault()?.ToString());
    }

    [Fact]
    public async Task GivenTooLongSampleIdCreateSampleShouldThrow()
    {
        var mediator = ServiceProvider.GetService<IMediator>()!;

        var message = new CreateRequestCommand
        {
            SampleId = "123456789012345678901234567890123"
        };

        var ex = await Assert.ThrowsAsync<ValidationException>(
                     async () => await mediator.Send(message, TestContext.Current.CancellationToken));

        Assert.Equal("The length of 'Sample Id' must be 32 characters or fewer. You entered 33 characters.", ex.Errors.FirstOrDefault()?.ToString());
    }
}