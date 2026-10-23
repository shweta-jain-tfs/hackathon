using Google.Protobuf.WellKnownTypes;
using MediatR;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Reagents;
using Pharos.Server.Reagents.Application.Queries;
using Pharos.Server.Reagents.Infrastructure.Database;
using Tetris.Protocol.Instrument.UpdateBottledReagent;

namespace Pharos.Server.Reagents.Application.Commands;

public record ProcessUpdateBottledReagentCommand(string ReagentUdi, string InstrumentNumber, ReagentInfo ReagentInfo) : IRequest;

internal class ProcessUpdateBottledReagentCommandHandler(ReagentsDbContext reagentDbContext, IMediator mediator) : IRequestHandler<ProcessUpdateBottledReagentCommand>
{
    public async Task Handle(ProcessUpdateBottledReagentCommand request, CancellationToken cancellationToken)
    {

        var loadingPositionQuery = new GetLoadingPositionQuery(request.InstrumentNumber, request.ReagentUdi);
        var loadingPositionResult = await mediator.Send(loadingPositionQuery, cancellationToken);
        if (loadingPositionResult.Status == LoadingPositionStatus.Found)
        {
            var updateBottleReagentInfo = new UpdateBottledReagentInformation
            {
                ReagentUdiBarcode = request.ReagentUdi,
                LotNumber = request.ReagentInfo.Lot.LotNumber,
                SerialNumber = request.ReagentInfo.SerialNumber ?? string.Empty,
                Name = request.ReagentInfo.Article.FriendlyName,
                ArticleNumber = request.ReagentInfo.Article.ArticleNumber,
                RemainingQuantity = request.ReagentInfo.Article.Quantity,
                UseRemainingQuantity = false,
                OpenTimestamp = null,
                LotExpiryDate = request.ReagentInfo.Lot?.ExpiryDate != null ? Timestamp.FromDateTimeOffset(request.ReagentInfo.Lot.ExpiryDate) : null,
                Position = loadingPositionResult.Position
            };

            var dbReagent = await reagentDbContext.Reagents.Include(r => r.ReagentContents).Where(r => r.Udi == request.ReagentUdi).FirstOrDefaultAsync(cancellationToken);

            if (dbReagent?.ReagentContents is not null && dbReagent.ReagentContents.Count > 0)
            {
                var reagentContent = dbReagent.ReagentContents.First();
                updateBottleReagentInfo.UseRemainingQuantity = true;
                updateBottleReagentInfo.RemainingQuantity = reagentContent.Quantity;
                updateBottleReagentInfo.OpenTimestamp = reagentContent.OpenTimestamp?.DateTime != null ? Timestamp.FromDateTimeOffset(reagentContent.OpenTimestamp.Value) : null;
            }

            await mediator.Send(new UpdateBottledReagentCommand(request.InstrumentNumber, updateBottleReagentInfo), cancellationToken);
        }
    }
}