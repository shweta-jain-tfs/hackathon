using MediatR;
using Pharos.Server.Instrument.Application.Events;
using Pharos.Server.Instrument.Application.Interfaces;

namespace Pharos.Server.Instrument.Application.Commands.SetAllAssaySchedulersActive;

internal record SetAllAssaySchedulersActiveCommand(bool IsActive) : IRequest;

internal sealed class SetAllAssaySchedulersActiveCommandHandler(IInstrumentDbContext dbContext) : IRequestHandler<SetAllAssaySchedulersActiveCommand>
{
    public async Task Handle(SetAllAssaySchedulersActiveCommand request, CancellationToken cancellationToken)
    {
        var assaySchedulers = dbContext.AssaySchedulers.ToList();

        for (var i = 0; i < assaySchedulers.Count; i++)
        {
            var assayScheduler = assaySchedulers[i];

            assayScheduler.IsActive = request.IsActive;
            assayScheduler.PausedUntilDate = request.IsActive ? null : assayScheduler.PausedUntilDate;
            assayScheduler.AddDomainEvent(new AssaySchedulerChangedEvent(assayScheduler));
            assayScheduler.AddDomainEvent(new AssaySchedulerNextRunChangedEvent(assayScheduler.InstrumentId));

            // Only add the ReCalculateAssayScheduling event once.
            if (i == 0)
            {
                assayScheduler.AddDomainEvent(new ReCalculateAssayScheduling());
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}