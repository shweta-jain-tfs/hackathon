using System.Text;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Request;
using Pharos.Server.Application.Security;
using Pharos.Server.Application.Settings.Request;
using Pharos.Server.Application.Settings.Sample;
using Pharos.Server.Request.Application.Commands.TestOrderScheduling;
using Pharos.Server.Request.Application.Interfaces;
using Pharos.Server.Localization;
using Pharos.Server.Request.Domain;
using RequestCreatedEvent = Pharos.Server.Request.Application.Events.RequestCreatedEvent;
using TestOrder = Pharos.Server.Request.Domain.TestOrder;

namespace Pharos.Server.Request.Application.Commands.CreateUpdateRequest;

public class CreateRequestCommand : BaseUpdateRequestCommand
{
    public string SampleId { get; set; } = null!;
}

internal sealed class CreateRequestCommandHandler : BaseUpdateRequestCommandHandler<CreateRequestCommand>
{
    private static readonly CompositeFormat _requestExists = CompositeFormat.Parse(Translations.SampleAlreadyExists);

    public CreateRequestCommandHandler(
        IAuthorizationService authorizationService,
        IRequestDbContext dbContext,
        IOptionsSnapshot<RequestSettings> requestSettings,
        IOptionsSnapshot<TubeTypesSettings> tubeSettings,
        IMediator mediator,
        IMethodsService methods) : base(authorizationService, dbContext, requestSettings, tubeSettings, mediator, methods)
    {
    }

    protected override async Task<Domain.Request> GetRequest(CreateRequestCommand command, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetRequestIdQuery(command.SampleId), cancellationToken);
        if (result.RequestExists)
        {
            throw new ValidationException(string.Format(null, _requestExists, command.SampleId));
        }

        var request = new Domain.Request
        {
            SampleId = command.SampleId,
            TestOrders = new List<TestOrder>(),
            Source = RequestSource.Manual
        };
        DbContext.Requests.Add(request);
        return request;
    }

    protected override async Task UpdateTestOrders(Domain.Request request, CreateRequestCommand command, CancellationToken cancellationToken)
    {
        var tests = command.TestOrders;
        if (tests == null || tests.Count == 0)
        {
            return;
        }

        var testSortOrder = 1;
        foreach (var test in tests.OrderBy(x => x.TestSortOrder))
        {
            await AddTestOrder(request, test, testSortOrder, cancellationToken);
            testSortOrder++;
        }
    }

    protected override async Task PublishNotifications(Domain.Request request)
    {
        await Mediator.Send(new SyncTestOrdersWithInstrumentCommand(request));

        await Mediator.Publish(new RequestCreatedEvent(request));
    }
}
