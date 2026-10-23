using MediatR;
using Pharos.Server.Application.Request;
using Pharos.Server.Domain.Reflex;
using Pharos.Server.Request.Application.Interfaces;

namespace Pharos.Server.Request.Application.Queries;

internal record GetPendingReflexesQuery(IReadOnlyList<Guid> TestOrderIds) : IRequest<ICollection<PendingReflexDto>>;

public class PendingReflexDto
{
    public int Id { get; set; }
    public Guid TestOrderId { get; set; }
    public ResultType? ResultType { get; set; }
    public string? DefinitionName { get; set; }
    public string? SampleIdDisplay { get; set; }
    public string? ResultValue { get; set; }
    public string? ResultUnit { get; set; }
}

internal class GetPendingReflexesQueryHandler(IRequestDbContext context, ISampleIdFormatter sampleIdFormatter) : IRequestHandler<GetPendingReflexesQuery, ICollection<PendingReflexDto>>
{
    public async Task<ICollection<PendingReflexDto>> Handle(GetPendingReflexesQuery request, CancellationToken cancellationToken)
    {
        var result = await context.TestOrders
            .Where(r => request.TestOrderIds.Contains(r.Uid) && r.State == TestOrderState.ApprovedPendingReflex)
            .SelectMany(to => to.ReflexDefinitions!, (to, rd) => new PendingReflexDto
            {
                Id = rd.Id,
                TestOrderId = to.Uid,
                DefinitionName = rd.Name,
                SampleIdDisplay = sampleIdFormatter.Format(to.Request.GetRequestUid()),
                ResultType = rd.ReflexCondition.ResultType,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return result;
    }
}
