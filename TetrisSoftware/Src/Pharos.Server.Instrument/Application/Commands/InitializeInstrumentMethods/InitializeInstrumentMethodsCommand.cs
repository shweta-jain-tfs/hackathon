using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Instrument.Application.Events;
using Pharos.Server.Instrument.Application.Interfaces;
using Pharos.Server.Instrument.Domain;

namespace Pharos.Server.Instrument.Application.Commands.InitializeInstrumentMethods;

/// <summary>
/// Call this command to create instrument methods and assay schedule (after a new instrument is added)
/// or update these after a change in method parameters (e.g, after a method update package import).
/// </summary>
internal record InitializeInstrumentMethodsCommand(int InstrumentId) : IRequest;

internal sealed class InitializeInstrumentMethodsCommandHandler(
    IInstrumentDbContext dbContext,
    IMethodsService methodsService,
    IConfiguration configuration) : IRequestHandler<InitializeInstrumentMethodsCommand>
{
    public async Task Handle(InitializeInstrumentMethodsCommand request, CancellationToken cancellationToken)
    {
        var activeTechnologies = await dbContext.InstrumentTechnologies
            .Where(x => x.InstrumentId == request.InstrumentId && x.IsActive)
            .Select(x => x.Technology)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var methodNames = await methodsService.GetMethods(
            m => activeTechnologies.Contains(m.Technology),
            x => x.MethodName);

        await InitializeMethods(request.InstrumentId, methodNames, cancellationToken);
        await InitializeAssaySchedule(request.InstrumentId, methodNames, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task InitializeMethods(int instrumentId, List<string> methodNames, CancellationToken cancellationToken)
    {
        // Get existing instrument methods for this instrument
        var existingMethods = await dbContext.InstrumentMethods
            .Where(x => x.InstrumentId == instrumentId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Create a lookup of existing methods by name for efficient lookups
        var existingMethodsByName = existingMethods.ToDictionary(m => m.MethodName, m => m);

        // Methods to remove (those that have been removed from parameters)
        var methodsToRemove = existingMethods
            .Where(m => !methodNames.Contains(m.MethodName))
            .ToList();

        if (methodsToRemove.Count > 0)
        {
            dbContext.InstrumentMethods.RemoveRange(methodsToRemove);
        }

        // For the testing purposes we may want to have all methods active by default.
        var instrumentSection = configuration.GetSection("Instrument");
        var methodActiveByDefault = instrumentSection.GetValue("MethodsActiveByDefault", false);

        // Add new methods (those that are only in the parameters)
        var methodsToAdd = methodNames
            .Where(name => !existingMethodsByName.ContainsKey(name))
            .Select(name => new InstrumentMethod
            {
                InstrumentId = instrumentId,
                MethodName = name,
                IsActive = methodActiveByDefault,
                Calibrator = true,
                ForceCalibrator = false,
                WhenToRun = InstrumentMethodWhenToRun.Directly
            })
            .ToList();

        if (methodsToAdd.Count > 0)
        {
            await dbContext.InstrumentMethods.AddRangeAsync(methodsToAdd, cancellationToken);
        }
    }

    private async Task InitializeAssaySchedule(int instrumentId, List<string> methodNames, CancellationToken cancellationToken)
    {
        // Get the existing assay scheduler with its days and methods
        var assayScheduler = await dbContext.AssaySchedulers
            .Where(x => x.InstrumentId == instrumentId)
            .Include(x => x.SchedulerDays)
            .ThenInclude(d => d.ScheduledMethods)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        // If no assay scheduler exists for this instrument, create a new one
        if (assayScheduler == null)
        {
            // Create new assay schedule with days and methods
            CreateAssaySchedule(instrumentId, methodNames);
            return; // Nothing more to do
        }

        // For an existing schedule, synchronize methods across all scheduler days
        foreach (var schedulerDay in assayScheduler.SchedulerDays)
        {
            // Create dictionary of existing methods for efficient lookups
            var existingMethodsByName = schedulerDay.ScheduledMethods.ToDictionary(m => m.MethodName, m => m);

            // Methods to remove (those in scheduler but not in the provided methodNames)
            var methodsToRemove = schedulerDay.ScheduledMethods
                .Where(m => !methodNames.Contains(m.MethodName))
                .ToList();

            // Remove methods that no longer exist in the parameters
            foreach (var method in methodsToRemove)
            {
                schedulerDay.ScheduledMethods.Remove(method);
            }

            // Add new methods (those in methodNames but not in the scheduler)
            foreach (var methodName in methodNames.Where(name => !existingMethodsByName.ContainsKey(name)))
            {
                var anyDayHasTime = assayScheduler.SchedulerDays
                    .Any(day => day.StartTime.HasValue || day.EndTime.HasValue);

                // Create new method with default values
                schedulerDay.ScheduledMethods.Add(new AssaySchedulerMethod
                {
                    MethodName = methodName,
                    IsActive = false, // Default to inactive
                    WhenToRun = InstrumentMethodWhenToRun.Directly,
                    SchedulerDay = schedulerDay
                });
                schedulerDay.AddDomainEvent(new AssaySchedulerDayChangedEvent(instrumentId, schedulerDay, anyDayHasTime));
            }
        }
    }

    private void CreateAssaySchedule(int instrumentId, List<string> methodNames)
    {
        // Create a new assay scheduler.
        var assayScheduler = new AssayScheduler
        {
            InstrumentId = instrumentId,
        };
        dbContext.AssaySchedulers.Add(assayScheduler);

        // Create Monday to Sunday.
        var schedulerDays = Enum.GetValues<DayOfWeek>()
            .Select(day => new AssaySchedulerDay
            {
                Scheduler = assayScheduler,
                Day = day,
                StartTime = null,
                EndTime = null
            })
            .ToList();
        dbContext.AssaySchedulerDays.AddRange(schedulerDays);

        // Create available methods for each day.
        foreach (var schedulerDay in schedulerDays)
        {
            foreach (var methodName in methodNames)
            {
                var assaySchedulerMethod = new AssaySchedulerMethod
                {
                    IsActive = false,
                    MethodName = methodName,
                    SchedulerDay = schedulerDay,
                    WhenToRun = InstrumentMethodWhenToRun.Directly
                };
                dbContext.AssaySchedulerMethods.Add(assaySchedulerMethod);
            }
        }
    }
}
