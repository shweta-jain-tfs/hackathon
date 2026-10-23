using MediatR;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Localization;
using Pharos.Server.Result.Application.Events;
using Pharos.Server.Result.Application.Events.TestResultReady;
using Pharos.Server.Result.Application.Interfaces;

namespace Pharos.Server.Result.Application.Commands;

public record UpdateSampleTestResultCommand : IRequest
{
    public int Id { get; set; }
    public string? Comment { get; set; }
    public List<ActiveSampleTestReplicateDto>? Replicates { get; set; }
}

public class ActiveSampleTestReplicateDto
{
    public int Id { get; set; }
    public bool Active { get; set; }
}

internal sealed class UpdateSampleTestResultCommandHandler(IResultDbContext context) : IRequestHandler<UpdateSampleTestResultCommand>
{
    public async Task Handle(UpdateSampleTestResultCommand request, CancellationToken cancellationToken)
    {
        var resultEntity = await context.SampleTestResults
            .Include(x => x.Replicates)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (resultEntity == null)
        {
            throw new ObjectGoneException(Translations.ObjectGone);
        }

        // Update the operator comment
        resultEntity.OperatorComment = request.Comment;

        var activeReplicatesChanged = false;

        // Update the Active flag in replicates based on request
        if (request.Replicates != null && resultEntity.Replicates != null)
        {
            foreach (var replicate in resultEntity.Replicates)
            {
                var updatedReplicate = request.Replicates.FirstOrDefault(r => r.Id == replicate.Id);
                if (updatedReplicate != null && replicate.Active != updatedReplicate.Active)
                {
                    replicate.Active = updatedReplicate.Active;
                    activeReplicatesChanged = true;
                }
            }
        }

        if (activeReplicatesChanged)
        {
            //add event to recalculate the result
            resultEntity.AddDomainEvent(new SampleTestResultReadyEvent(resultEntity));
        }
        else
        {
            resultEntity.AddDomainEvent(new TestResultUpdatedEvent(resultEntity));
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}