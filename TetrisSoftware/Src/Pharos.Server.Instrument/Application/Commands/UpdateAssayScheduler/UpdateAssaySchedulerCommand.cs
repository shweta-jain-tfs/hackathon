using System.Text.Json.Serialization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Pharos.Server.Instrument.Application.Events;
using Pharos.Server.Instrument.Application.Interfaces;

namespace Pharos.Server.Instrument.Application.Commands.UpdateAssayScheduler;

public class UpdateAssaySchedulerCommand : IRequest
{
    [JsonIgnore]
    public int InstrumentId { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset? PausedUntilDate { get; set; }
}

internal sealed class UpdateAssaySchedulerCommandHandler(IInstrumentDbContext dbContext) : IRequestHandler<UpdateAssaySchedulerCommand>
{
    public async Task Handle(UpdateAssaySchedulerCommand request, CancellationToken cancellationToken)
    {
        var assayScheduler = await dbContext.AssaySchedulers.FirstOrDefaultAsync(x => x.InstrumentId == request.InstrumentId, cancellationToken).ConfigureAwait(false);

        if (assayScheduler != null)
        {
            assayScheduler.IsActive = request.IsActive;
            assayScheduler.PausedUntilDate = request.IsActive ? null : request.PausedUntilDate;
            assayScheduler.AddDomainEvent(new AssaySchedulerChangedEvent(assayScheduler));
            assayScheduler.AddDomainEvent(new AssaySchedulerNextRunChangedEvent(request.InstrumentId));
            assayScheduler.AddDomainEvent(new ReCalculateAssayScheduling());
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}