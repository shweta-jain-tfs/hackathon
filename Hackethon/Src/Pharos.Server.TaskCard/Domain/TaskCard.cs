using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Domain;

namespace Pharos.Server.TaskCard.Domain;

internal class TaskCard
{
    //EF Core constructor
    private TaskCard() : this([])
    {
    }

    public TaskCard(List<TriggerState> rules)
    {
        Rules = rules.OrderBy(x => x.Trigger).ToList();
    }

    public int Id { get; set; } //id is not to be trusted, not everything is stored in database

    public DateTimeOffset? ExpiresAt { get; set; }

    [MaxLength(Constants.InstrumentSerialNumberLength)]
    public string? InstrumentNumber { get; set; }

    public TaskCardType CardType { get; set; }

    public List<TriggerState> Rules { get; }

    [NotMapped]
    public TaskCardState CurrentCardState { get; private set; }

    public (bool CardStateChanged, bool CanBeRemoved) SetCurrentCardState(DateTimeOffset dateTime)
    {
        //The indicator to if the state has changed for the task card
        var stateHasChanged = false;

        //Then we want to get the rule with the latest time that is before the current time
        var actingRule = Rules.LastOrDefault(x => x.Trigger <= dateTime);

        //If we do not find a rule that meets the criteria we will set the first rule in the sorted list as the acting rule
        actingRule ??= Rules.First();

        //If the state is different from the previous current state, we update it
        if (actingRule.CardState != CurrentCardState)
        {
            CurrentCardState = actingRule.CardState;
            stateHasChanged = true;
        }

        //if the acting rule has state hidden, and this is the last rule in the escalation. This card can be removed (since it will stay hidden forever)
        var taskCardCanBeRemoved = actingRule.CardState == TaskCardState.Hidden && Rules.LastOrDefault() == actingRule;

        return (stateHasChanged, taskCardCanBeRemoved);
    }
}

internal class TriggerState
{
    //EF Core constructor
    private TriggerState()
    {
    }

    public TriggerState(TaskCardState cardState, DateTimeOffset trigger)
    {
        CardState = cardState;
        Trigger= trigger;
    }

    public int Id { get; set; } //id is not to be trusted, not everything is stored in database
    public TaskCardState CardState { get; set; }
    public DateTimeOffset Trigger { get; set; }
}