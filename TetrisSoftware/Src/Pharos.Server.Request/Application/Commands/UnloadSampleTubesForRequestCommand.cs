using MediatR;
using Pharos.Server.Application.Sample;
using Pharos.Server.Request.Application.Interfaces;

namespace Pharos.Server.Request.Application.Commands;

internal record UnloadSampleTubesForRequestCommand(int Id) : IRequest;

internal class UnloadSampleTubesForRequestCommandHandler : IRequestHandler<UnloadSampleTubesForRequestCommand>
{
    private readonly IRequestDbContext _dbContext;
    private readonly ISender _sender;

    public UnloadSampleTubesForRequestCommandHandler(IRequestDbContext dbContext, ISender sender)
    {
        _dbContext = dbContext;
        _sender = sender;
    }

    public async Task Handle(UnloadSampleTubesForRequestCommand request, CancellationToken cancellationToken)
    {
        var requestEntity = await _dbContext.Requests.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        if (requestEntity != null)
        {
            await _sender.Send(new UnloadSampleTubesCommand(requestEntity.Id, requestEntity.GetRequestUid()), cancellationToken);
        }
    }
}