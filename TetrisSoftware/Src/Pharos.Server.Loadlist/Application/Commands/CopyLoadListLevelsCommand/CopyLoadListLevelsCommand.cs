using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pharos.Server.LoadList.Application.Events;

namespace Pharos.Server.LoadList.Application.Commands.CopyLoadListLevelsCommand;

public class CopyLoadListLevelsCommand : IRequest
{
    public required string SourceInstrument { get; set; }
    public required List<string> TargetInstruments { get; set; }
}

internal class CopyLoadListLevelsCommandHandler(ILoadListDbContext dbContext, ILogger<CopyLoadListLevelsCommandHandler> logger, IPublisher publisher) : IRequestHandler<CopyLoadListLevelsCommand>
{
    public async Task Handle(CopyLoadListLevelsCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Get source configurations with their refill and trigger levels
            var sourceLevels = await dbContext.LoadListConfigurations
                .Where(c => c.InstrumentSerial == request.SourceInstrument)
                .Select(c => new
                {
                    c.ArticleNumber,
                    c.ArticleType,
                    c.RefillLevel,
                    c.TriggerLevel
                })
                .ToListAsync(cancellationToken);

            if (sourceLevels.Count == 0)
            {
                logger.LogWarning(
                    "No load list configurations found for source instrument {SourceInstrument}",
                    request.SourceInstrument);
                return;
            }

            // Create dictionary for lookups
            var sourceLevelsDict = sourceLevels
                .ToDictionary(s => (s.ArticleType, s.ArticleNumber));

            // Load all target configurations
            var targetConfigurations = await dbContext.LoadListConfigurations
                .Where(c => request.TargetInstruments.Contains(c.InstrumentSerial))
                .ToListAsync(cancellationToken);

            if (targetConfigurations.Count == 0)
            {
                logger.LogWarning("No load list configurations found for any target instruments: {TargetInstruments}",
                    string.Join(", ", request.TargetInstruments));

                return;
            }

            // Update configurations using dictionary lookup
            var updatedConfigurationIds = new List<int>();
            foreach (var targetConfig in targetConfigurations)
            {
                if (!sourceLevelsDict.TryGetValue((targetConfig.ArticleType, targetConfig.ArticleNumber), out var sourceLevel))
                {
                    continue;
                }

                targetConfig.RefillLevel = sourceLevel.RefillLevel;
                targetConfig.TriggerLevel = sourceLevel.TriggerLevel;
                updatedConfigurationIds.Add(targetConfig.Id);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            if (updatedConfigurationIds.Count > 0)
            {
                await publisher.Publish(new LoadListConfigurationUpdatedEvent(updatedConfigurationIds), cancellationToken);
            }

            logger.LogInformation(
                "Successfully copied load list levels.");
        }
        catch(Exception ex)
        {
            logger.LogError(ex,
                "Error while copying load list levels from {SourceInstrument}",
                request.SourceInstrument);
            throw;
        }
    }
}