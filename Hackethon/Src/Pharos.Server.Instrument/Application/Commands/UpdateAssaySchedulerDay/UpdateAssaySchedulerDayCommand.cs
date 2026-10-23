using System.Text.Json.Serialization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Pharos.Contracts;
using Pharos.Server.Instrument.Application.Events;
using Pharos.Server.Instrument.Application.Interfaces;
using Pharos.Server.Instrument.Domain;

namespace Pharos.Server.Instrument.Application.Commands.UpdateAssaySchedulerDay;

public class UpdateAssaySchedulerDayCommand : IRequest
{
    [JsonIgnore]
    public int Id { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public List<AssaySchedulerMethodDto>? ScheduledMethods { get; set; }
}

internal sealed class UpdateAssaySchedulerDayCommandHandler(IInstrumentDbContext dbContext) : IRequestHandler<UpdateAssaySchedulerDayCommand>
{
    /// <summary>
    /// Updates one assay scheduler day.
    /// </summary>
    public async Task Handle(UpdateAssaySchedulerDayCommand request, CancellationToken cancellationToken)
    {
        var assaySchedulerDay = dbContext.AssaySchedulerDays
            .Include(x => x.Scheduler)
            .Include(x => x.ScheduledMethods)
            .FirstOrDefault(x => x.Id == request.Id);

        if (assaySchedulerDay != null)
        {
            assaySchedulerDay.StartTime = request.StartTime;
            assaySchedulerDay.EndTime = request.EndTime;

            if (request.ScheduledMethods != null)
            {
                assaySchedulerDay.ScheduledMethods.Clear();
                foreach (var methodDto in request.ScheduledMethods)
                {
                    assaySchedulerDay.ScheduledMethods.Add(new AssaySchedulerMethod
                    {
                        MethodName = methodDto.MethodName,
                        IsActive = methodDto.IsActive,
                        WhenToRun = (Server.Domain.Enums.InstrumentMethodWhenToRun)methodDto.WhenToRun
                    });
                }
            }

            var instrumentId = assaySchedulerDay.Scheduler.InstrumentId;
            var schedulerId = assaySchedulerDay.SchedulerId;

            // Check for any start/end time.
            var anyDayHasTime = assaySchedulerDay.Scheduler.SchedulerDays
                .Where(day => day.SchedulerId == schedulerId)
                .Any(day => day.StartTime.HasValue || day.EndTime.HasValue);

            assaySchedulerDay.AddDomainEvent(new AssaySchedulerDayChangedEvent(instrumentId, assaySchedulerDay, anyDayHasTime));
            assaySchedulerDay.AddDomainEvent(new AssaySchedulerNextRunChangedEvent(instrumentId));
            assaySchedulerDay.AddDomainEvent(new ReCalculateAssayScheduling());
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}