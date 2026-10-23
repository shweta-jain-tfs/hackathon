using MediatR;
using Microsoft.EntityFrameworkCore;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Infrastructure;
using Pharos.Server.Maintenance.Application.Interfaces;
using Pharos.Server.Maintenance.Domain;

namespace Pharos.Server.Maintenance.Application.Commands;

internal record CreateMaintenanceTaskCardsCommand(int? ActivityId) : IRequest
{
    public CreateMaintenanceTaskCardsCommand() : this((int?)null)
    {
    }
}

internal class CreateMaintenanceTaskCardsCommandHandler : IRequestHandler<CreateMaintenanceTaskCardsCommand>
{
    private readonly IMaintenanceDbContext _dbContext;
    private readonly IMaintenanceDefinitionProvider _definitionProvider;
    private readonly ISender _sender;
    private readonly IDateTime _dateTime;

    public CreateMaintenanceTaskCardsCommandHandler(IMaintenanceDbContext dbContext, IMaintenanceDefinitionProvider definitionProvider, ISender sender, IDateTime dateTime)
    {
        _dbContext = dbContext;
        _definitionProvider = definitionProvider;
        _sender = sender;
        _dateTime = dateTime;
    }

    public async Task Handle(CreateMaintenanceTaskCardsCommand request, CancellationToken cancellationToken)
    {
        if (request.ActivityId.HasValue)
        {
            var activity = await _dbContext.Activities.FirstOrDefaultAsync(x => x.Id == request.ActivityId, cancellationToken);

            if (activity == null)
            {
                return;
            }

            await CreateTaskCard(activity, cancellationToken);
        }
        else
        {
            var activities = await _dbContext.Activities.ToListAsync(cancellationToken);
            foreach (var activity in activities)
            {
                await CreateTaskCard(activity, cancellationToken);
            }
        }
    }

    private async Task CreateTaskCard(MaintenanceActivity activity, CancellationToken cancellationToken)
    {
        if (activity.Next == null)
        {
            return;
        }

        var taskCardDefinition = _definitionProvider.GetTaskCardDefinition(activity.ActivityType);
        if (taskCardDefinition == null)
        {
            return;
        }

        var now = _dateTime.Now;
        var due = activity.Next.Value.Subtract(now);
        var rules = taskCardDefinition.EscalationRules.Select(rule => rule with { Trigger = TimeSpan.FromDays(activity.Next.Value.Subtract(rule.Trigger).Subtract(now).TotalDays) }).ToList();

        var taskCardCommand = new TaskCardCommand(taskCardDefinition.TaskCardType, rules, due, activity.InstrumentNumber);
        await _sender.Send(taskCardCommand, cancellationToken);
    }
}