using MediatR;
using Microsoft.EntityFrameworkCore;
using Pharos.Server.Application;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Instrument.Application.Helpers;
using Pharos.Server.Instrument.Application.Interfaces;
using Pharos.Server.Instrument.Application.Mapping;

namespace Pharos.Server.Instrument.Application.Queries.GetInstruments;

internal record GetCurrentInstrumentQuery : IRequest<InstrumentDto>;

internal class GetCurrentInstrumentQueryHandler(
    IInstrumentManager instrumentManager,
    ICurrentInstrumentService currentInstrument,
    IInstrumentDbContext dbContext) : IRequestHandler<GetCurrentInstrumentQuery, InstrumentDto>
{
    public async Task<InstrumentDto> Handle(GetCurrentInstrumentQuery request, CancellationToken cancellationToken)
    {
        var currentInstrumentId = currentInstrument.InstrumentId ?? throw new NotFoundException();
        var instrument = (Domain.Instrument)instrumentManager.GetInstrument(currentInstrumentId);
        var dto = instrument.ToInstrumentDto();

        var assayScheduler = await dbContext.AssaySchedulers
            .Include(x => x.SchedulerDays)
            .FirstOrDefaultAsync(x => x.InstrumentId == currentInstrumentId, cancellationToken)
            .ConfigureAwait(false);

        if (assayScheduler != null)
        {
            dto.AssaySchedulerIsActive = assayScheduler.IsActive;
            dto.AssaySchedulerIsPausedUntilDate = assayScheduler.PausedUntilDate;

            var isSchedulerPaused = !assayScheduler.IsActive
                || (assayScheduler.PausedUntilDate.HasValue && assayScheduler.PausedUntilDate.Value > DateTimeOffset.Now);

            if (isSchedulerPaused)
            {
                dto.AssaySchedulerNextRun = null;
                dto.AssaySchedulerNextRunType = AssaySchedulerNextRunType.None;
            }
            else
            {
                var (nextRun, nextRunType) = AssaySchedulerHelper.GetNextRunAssayScheduler(assayScheduler.SchedulerDays);
                dto.AssaySchedulerNextRun = nextRun;
                dto.AssaySchedulerNextRunType = nextRunType;
            }

            dto.AssaySchedulerShowInfo = assayScheduler.SchedulerDays
                .Any(day => day.StartTime.HasValue || day.EndTime.HasValue);
        }

        return dto;
    }
}
