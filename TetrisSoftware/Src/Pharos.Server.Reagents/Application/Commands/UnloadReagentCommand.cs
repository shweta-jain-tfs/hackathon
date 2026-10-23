using MediatR;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Reagents.Application.Queries.Inventory;
using Tetris.Protocol.Instrument.Move;

namespace Pharos.Server.Reagents.Application.Commands;

/// <summary>
/// Represents a request to unload reagents, each with a barcode and isAbort flag.
/// </summary>
public record UnloadReagentCommand(IReadOnlyList<UnloadReagentRequestItemDto> UnloadReagents) : IRequest;

internal class UnloadReagentCommandHandler(ISender sender) : IRequestHandler<UnloadReagentCommand>
{
    public async Task Handle(UnloadReagentCommand request, CancellationToken cancellationToken)
    {
        var unloadReagents = new Dictionary<string, UnloadReagentRequest>();
        foreach (var item in request.UnloadReagents)
        {
            FillUnloadReagentRequest(unloadReagents, item);
        }

        await SendUnloadReagentRequestsAsync(unloadReagents, cancellationToken);
    }

    private async Task SendUnloadReagentRequestsAsync(Dictionary<string, UnloadReagentRequest> unloadReagents, CancellationToken cancellationToken)
    {
        foreach (var item in unloadReagents)
        {
            await sender.Send(new SendUnloadReagentRequestCommand(item.Key, item.Value), cancellationToken);
        }
    }

    private static void FillUnloadReagentRequest(Dictionary<string, UnloadReagentRequest> unloadReagents, UnloadReagentRequestItemDto item)
    {
        if (unloadReagents.TryGetValue(item.InstrumentNumber, out var unloadReagentRequest))
        {
            // If the instrument number already exists, add the item to the existing request
            unloadReagentRequest.ReagentsToUnload.Add(new UnloadReagentRequestItem
            {
                AbortUnload = item.AbortUnload,
                Position = item.Position,
                StorageArea = (EnumReagentAutoUnloadStorageArea)item.StorageArea
            });
            return;
        }
        unloadReagents.Add(item.InstrumentNumber, new UnloadReagentRequest
        {
            ReagentsToUnload = {
                new UnloadReagentRequestItem
                {
                    AbortUnload = item.AbortUnload,
                    Position = item.Position,
                    StorageArea = (EnumReagentAutoUnloadStorageArea)item.StorageArea
                }}
        });
    }
}
