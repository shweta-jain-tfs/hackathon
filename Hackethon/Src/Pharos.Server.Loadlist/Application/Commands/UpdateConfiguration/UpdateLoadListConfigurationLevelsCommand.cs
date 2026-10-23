using Microsoft.EntityFrameworkCore;
using Pharos.Server.LoadList.Application.Events;

namespace Pharos.Server.LoadList.Application.Commands.UpdateConfiguration;

public class UpdateLoadListConfigurationLevelsCommand : IRequest
{
    public List<int>? UpdatedConfigurationsIds { get; set; }
    public int? RefillLevel { get; set; }
    public int? TriggerLevel { get; set; }
}

internal class UpdateLoadListConfigurationLevelsCommandHandler(ILoadListDbContext dbContext, IPublisher publisher) : IRequestHandler<UpdateLoadListConfigurationLevelsCommand>
{
    public async Task Handle(UpdateLoadListConfigurationLevelsCommand request, CancellationToken cancellationToken)
    {
        if (request.UpdatedConfigurationsIds is null || request.UpdatedConfigurationsIds.Count == 0)
        {
            return;
        }

        if (request.RefillLevel is null && request.TriggerLevel is null)
        {
            return;
        }

        var query = dbContext.LoadListConfigurations
            .Where(c => request.UpdatedConfigurationsIds.Contains(c.Id));

        // Validate that refill level is not less than trigger level
        if (request.RefillLevel is not null && request.TriggerLevel is not null)
        {
            if (request.RefillLevel.Value < request.TriggerLevel.Value)
            {
                throw new InvalidOperationException("Refill level cannot be less than trigger level.");
            }
            
            await query.ExecuteUpdateAsync(setters => setters
                    .SetProperty(c => c.RefillLevel, request.RefillLevel.Value)
                    .SetProperty(c => c.TriggerLevel, request.TriggerLevel.Value),
                cancellationToken);
        }
        else
        {
            // If only one value is provided, validate against existing configurations
            var configurations = await dbContext.LoadListConfigurations
                .Where(c => request.UpdatedConfigurationsIds.Contains(c.Id))
                .ToListAsync(cancellationToken);

            if (request.RefillLevel is not null)
            {
                var invalidConfig = configurations.FirstOrDefault(c => request.RefillLevel.Value < c.TriggerLevel);
                if (invalidConfig is not null)
                {
                    throw new InvalidOperationException($"Refill level {request.RefillLevel.Value} cannot be less than existing trigger level {invalidConfig.TriggerLevel} for configuration {invalidConfig.Id}.");
                }

                await query.ExecuteUpdateAsync(setters => setters
                        .SetProperty(c => c.RefillLevel, request.RefillLevel.Value),
                    cancellationToken);
            }

            else if (request.TriggerLevel is not null)
            {
                var invalidConfig = configurations.FirstOrDefault(c => c.RefillLevel < request.TriggerLevel.Value);
                if (invalidConfig is not null)
                {
                    throw new InvalidOperationException($"Trigger level {request.TriggerLevel.Value} cannot be greater than existing refill level {invalidConfig.RefillLevel} for configuration {invalidConfig.Id}.");
                }

                await query.ExecuteUpdateAsync(setters => setters
                        .SetProperty(c => c.TriggerLevel, request.TriggerLevel.Value),
                    cancellationToken);
            }
        }

        await publisher.Publish(new LoadListConfigurationUpdatedEvent(request.UpdatedConfigurationsIds), cancellationToken);
    }
}