using MediatR;
using Microsoft.EntityFrameworkCore;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Instrument.Application.Helpers;
using Pharos.Server.Instrument.Application.Interfaces;
using Pharos.Server.Instrument.Application.Mapping;

namespace Pharos.Server.Instrument.Application.Queries.GetInstruments;

internal record GetInstrumentsQuery : IRequest<InstrumentDto[]>;

internal class GetInstrumentsQueryHandler(
    IInstrumentManager instrumentManager,
    IInstrumentDbContext dbContext,
    IMethodsService methodsService) : IRequestHandler<GetInstrumentsQuery, InstrumentDto[]>
{
    public async Task<InstrumentDto[]> Handle(GetInstrumentsQuery request, CancellationToken cancellationToken)
    {
        // Get instruments.
        var instruments = instrumentManager.GetInstruments()
            .Select(x => ((Domain.Instrument)x).ToInstrumentDto())
            .ToArray();

        // Add assay scheduler information to instruments.
        var assayScheduler = await dbContext.AssaySchedulers.ToListAsync(cancellationToken).ConfigureAwait(false);
        await dbContext.AssaySchedulerDays.ToListAsync(cancellationToken).ConfigureAwait(false);
        var instrumentTechnologies = await dbContext.InstrumentTechnologies.ToListAsync(cancellationToken).ConfigureAwait(false);
        var availableTechnologies = (await methodsService.GetMethods(
                m => m.Enabled,
                x => x.Technology))
            .ToHashSet();
        foreach (var instrument in instruments)
        {
            var assaySchedulerForInstrument = assayScheduler.FirstOrDefault(x => x.InstrumentId == instrument.Id);
            if (assaySchedulerForInstrument != null)
            {
                instrument.AssaySchedulerIsActive = assaySchedulerForInstrument.IsActive;
                instrument.AssaySchedulerIsPausedUntilDate = assaySchedulerForInstrument.PausedUntilDate;

                var (nextRun, nextRunType) = AssaySchedulerHelper.GetNextRunAssayScheduler(assaySchedulerForInstrument.SchedulerDays);
                instrument.AssaySchedulerNextRun = nextRun;
                instrument.AssaySchedulerNextRunType = nextRunType;

                // Show assay scheduler info if any day has a start or end time.
                instrument.AssaySchedulerShowInfo = assaySchedulerForInstrument.SchedulerDays
                    .Any(day => day.StartTime.HasValue || day.EndTime.HasValue);
            }

            instrument.SelectedTechnologies = instrumentTechnologies
                .Where(x => x.InstrumentId == instrument.Id && x.IsActive && availableTechnologies.Contains(x.Technology))
                .OrderBy(x => x.Technology)
                .Select(x => x.Technology)
                .ToList();
        }

        return await Task.FromResult(instruments);
    }
}
