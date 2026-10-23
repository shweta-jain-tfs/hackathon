using MediatR;
using Microsoft.EntityFrameworkCore;
using Pharos.Server.Infrastructure;
using Pharos.Server.Instrument.Application.Events;
using Pharos.Server.Instrument.Application.Interfaces;

namespace Pharos.Server.Instrument.Application.Commands.SkipNextScheduledAssayStart;

internal record SkipNextScheduledAssayStartCommand(int InstrumentId) : IRequest;

internal sealed class SkipNextScheduledAssayStartCommandHandler(IInstrumentDbContext dbContext, IDateTime dateTime) : IRequestHandler<SkipNextScheduledAssayStartCommand>
{
    /// <summary>
    /// Skips today's scheduled automatic assay starts by pausing scheduling until next local midnight.
    /// </summary>
    public async Task Handle(SkipNextScheduledAssayStartCommand request, CancellationToken cancellationToken)
    {
        var scheduler = await dbContext.AssaySchedulers
            .FirstOrDefaultAsync(x => x.InstrumentId == request.InstrumentId, cancellationToken)
            .ConfigureAwait(false);

        if (scheduler == null || !scheduler.IsActive)
        {
            return;
        }

        var now = dateTime.Now;

        // Pause until the next local day boundary so all remaining starts for today are skipped.
        scheduler.IsActive = false;
        scheduler.PausedUntilDate = new DateTimeOffset(now.Date.AddDays(1), now.Offset);

        scheduler.AddDomainEvent(new AssaySchedulerChangedEvent(scheduler));
        scheduler.AddDomainEvent(new AssaySchedulerNextRunChangedEvent(request.InstrumentId));
        scheduler.AddDomainEvent(new ReCalculateAssayScheduling());

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
