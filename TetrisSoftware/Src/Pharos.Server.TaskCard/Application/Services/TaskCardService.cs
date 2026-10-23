using System.Collections.Concurrent;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Infrastructure;
using Pharos.Server.TaskCard.Application.Interfaces;
using Pharos.Server.TaskCard.Domain;

namespace Pharos.Server.TaskCard.Application.Services;

internal sealed class TaskCardService : ITaskCardService
{
    private readonly IDateTime _dateTime;
    private readonly ITaskCardRules _taskCardRules;
    private readonly ISignalTowerManager _signalTowerManager;
    private readonly ConcurrentDictionary<(TaskCardType CardType, string InstrumentNumber), Domain.TaskCard> _taskCards;

    public TaskCardService(IDateTime dateTime, ITaskCardRules taskCardRules, ISignalTowerManager signalTowerManager)
    {
        _dateTime = dateTime;
        _taskCardRules = taskCardRules;
        _signalTowerManager = signalTowerManager;
        _taskCards = [];
    }

    public void Initialize(List<Domain.TaskCard> taskCards)
    {
        foreach (var taskCard in taskCards)
        {
            var key = (taskCard.CardType, taskCard.InstrumentNumber ?? string.Empty);
            _taskCards.TryAdd(key, taskCard);
        }

        AutoEscalate();
    }

    /// <summary>
    /// Adds or updates a task card in memory and triggers change notifications when behavior changes.
    /// </summary>
    /// <param name="taskCard">The incoming task card to evaluate and apply.</param>
    public void Add(Domain.TaskCard taskCard)
    {
        var key = (taskCard.CardType, taskCard.InstrumentNumber ?? string.Empty);
        var now = _dateTime.Now;
        // Keep the card's current state before checking its escalation rules.
        // Since Re-evaluating those rules could change the state before we check whether to send a toast.
        TaskCardState? previousState = _taskCards.TryGetValue(key, out var existingTaskCard)
            ? existingTaskCard.CurrentCardState
            : null;

        // Evaluate incoming behavior at the current time.
        var incomingState = taskCard.SetCurrentCardState(now);
        var incomingEvaluation = (taskCard.CurrentCardState, incomingState.CanBeRemoved);
        var shouldNotify = previousState != taskCard.CurrentCardState;

        // Skip update if behavior is unchanged.
        if (ShouldSkipTaskCardUpdate(key, taskCard, incomingEvaluation, now))
        {
            return;
        }

        var taskCardsChanged = ApplyTaskCardUpdate(key, taskCard, incomingEvaluation.CanBeRemoved);

        if (taskCardsChanged)
        {
            OnTaskCardsChanged();

            // Send a toast only for a new card or a state change.
            // Changes to escalation rules or the countdown alone do not trigger another toast.
            if (shouldNotify)
            {
                OnTaskCardCreatedOrChanged(taskCard);
            }
        }
    }

    /// <summary>
    /// Determines whether an incoming task card update can be skipped because it would not produce a meaningful change.
    /// </summary>
    /// <param name="key">The lookup key for task cards in the in-memory store.</param>
    /// <param name="incomingTaskCard">The incoming task card candidate.</param>
    /// <param name="incomingEvaluation">The evaluated state snapshot for the incoming task card.</param>
    /// <param name="now">The timestamp used to evaluate both existing and incoming cards consistently.</param>
    /// <returns><see langword="true"/> when the update can be skipped; otherwise <see langword="false"/>.</returns>
    private bool ShouldSkipTaskCardUpdate(
        (TaskCardType CardType, string InstrumentNumber) key,
        Domain.TaskCard incomingTaskCard,
        (TaskCardState CurrentState, bool CanBeRemoved) incomingEvaluation,
        DateTimeOffset now)
    {
        if (!_taskCards.TryGetValue(key, out var existingTaskCard))
        {
            return false;
        }

        var existingEvaluation = EvaluateTaskCard(existingTaskCard, now);
        var hasMeaningfulChanges = HasMeaningfulChanges(existingTaskCard, existingEvaluation, incomingTaskCard, incomingEvaluation);

        // Skip only when the incoming card should remain visible and behaves the same as the current one.
        // In this case, replacing/removing/re-adding would not change what users see or what the tower should show.
        return !incomingEvaluation.CanBeRemoved && !hasMeaningfulChanges;
    }

    /// <summary>
    /// Applies an incoming task card update by replacing/removing the existing card and persisting side effects.
    /// </summary>
    /// <param name="key">The lookup key for task cards in the in-memory store.</param>
    /// <param name="incomingTaskCard">The incoming task card to apply.</param>
    /// <param name="incomingCanBeRemoved">Indicates whether the incoming card should be removed instead of stored.</param>
    /// <returns><see langword="true"/> when task cards changed; otherwise <see langword="false"/>.</returns>
    private bool ApplyTaskCardUpdate(
        (TaskCardType CardType, string InstrumentNumber) key,
        Domain.TaskCard incomingTaskCard,
        bool incomingCanBeRemoved)
    {
        // Remove old instance (if any) before storing/removing the incoming effective one.
        var taskCardsChanged = Remove(key);

        if (incomingCanBeRemoved)
        {
            RemoveFromDatabase(incomingTaskCard);
            return taskCardsChanged;
        }

        _taskCards.TryAdd(key, incomingTaskCard);
        AddToDatabase(incomingTaskCard);

        return true;
    }

    private void RemoveFromDatabase(Domain.TaskCard taskCard)
    {
        if (_taskCardRules.StoreInDatabase(taskCard.CardType))
        {
            TaskCardDeleted?.Invoke(taskCard).Wait();
        }
    }

    private void AddToDatabase(Domain.TaskCard taskCard)
    {
        if (_taskCardRules.StoreInDatabase(taskCard.CardType))
        {
            TaskCardAdded?.Invoke(taskCard).Wait();
        }
    }

    public void Remove(TaskCardType cardType, string? instrumentNumber = null)
    {
        var key = (cardType, instrumentNumber ?? string.Empty);
        if (Remove(key))
        {
            OnTaskCardsChanged();
        }
    }

    private bool Remove((TaskCardType CardType, string InstrumentNumber) key)
    {
        if (_taskCards.TryRemove(key, out var taskCard))
        {
            RemoveFromDatabase(taskCard);
            return true;
        }
        return false;
    }

    public bool Exists(TaskCardType taskCardType, string? instrumentNumber)
    {
        return _taskCards.ContainsKey((taskCardType, instrumentNumber ?? string.Empty));
    }

    public List<Domain.TaskCard> GetTaskCardsForInstrument(string instrumentNumber)
    {
        var taskCards = _taskCards.Values.Where(x => (x.InstrumentNumber == instrumentNumber || (x.InstrumentNumber == null && _taskCardRules.ControlsAllInstruments(x.CardType))) && x.CurrentCardState != TaskCardState.Hidden).ToList();

        // If both Error and SystemError Task Cards are present, only show the one with the highest rank
        var errorTaskCard = taskCards.FirstOrDefault(x => x.CardType == TaskCardType.Error);
        var systemErrorTaskCard = taskCards.FirstOrDefault(x => x.CardType == TaskCardType.SystemError);

        if (errorTaskCard != null && systemErrorTaskCard != null)
        {
            // Get the lowest rank Task Card and remove it from the list
            taskCards.Remove(GetLowestRankedTaskCard(errorTaskCard, systemErrorTaskCard));
        }

        return SortTaskCards(taskCards);
    }

    public List<Domain.TaskCard> GetTaskCardsForInstruments(List<string> instrumentNumbers)
    {
        var taskCards = new List<Domain.TaskCard>();
        var taskCardsForAllInstruments = _taskCards.Values.Where(x => x.InstrumentNumber == null && x.CurrentCardState != TaskCardState.Hidden && _taskCardRules.ControlsAllInstruments(x.CardType)).ToList();

        foreach (var instrumentNumber in instrumentNumbers)
        {
            // Adding a "fake" Task Card for each Task Card that applies to all instruments, so that the UI can treat them the same way as the other Task Cards.
            // The "fake" Task Card will have the same properties as the original one, except for the InstrumentNumber
            foreach (var card in taskCardsForAllInstruments)
            {
                taskCards.Add(CopyCard(card, instrumentNumber, _dateTime));
            }

            taskCards.AddRange(_taskCards.Values.Where(x => x.InstrumentNumber == instrumentNumber && x.CurrentCardState != TaskCardState.Hidden));

            // If both Error and SystemError Task Cards are present, only show the one with the highest rank
            var errorTaskCard = taskCards.FirstOrDefault(x => x.CardType == TaskCardType.Error && x.InstrumentNumber == instrumentNumber);
            var systemErrorTaskCard = taskCards.FirstOrDefault(x => x.CardType == TaskCardType.SystemError && x.InstrumentNumber == instrumentNumber);

            if (errorTaskCard != null && systemErrorTaskCard != null)
            {
                // Get the lowest rank Task Card and remove it from the list
                taskCards.Remove(GetLowestRankedTaskCard(errorTaskCard, systemErrorTaskCard));
            }
        }

        return taskCards;
    }

    private static Domain.TaskCard CopyCard(Domain.TaskCard card, string instrumentNumber, IDateTime dateTime)
    {
        var taskCard = new Domain.TaskCard(card.Rules)
        {
            CardType = card.CardType,
            ExpiresAt = card.ExpiresAt,
            InstrumentNumber = instrumentNumber
        };

        taskCard.SetCurrentCardState(dateTime.Now);

        return taskCard;
    }

    private static Domain.TaskCard GetLowestRankedTaskCard(Domain.TaskCard errorTaskCard, Domain.TaskCard systemErrorTaskCard)
    {
        if (errorTaskCard.CurrentCardState == systemErrorTaskCard.CurrentCardState)
        {
            // null ExpiresAt is treated as higher, so it is NOT the lowest
            if (errorTaskCard.ExpiresAt is null)
            {
                return systemErrorTaskCard;
            }

            if (systemErrorTaskCard.ExpiresAt is null)
            {
                return errorTaskCard;
            }

            return errorTaskCard.ExpiresAt <= systemErrorTaskCard.ExpiresAt
                ? errorTaskCard
                : systemErrorTaskCard;
        }

        if (errorTaskCard.CurrentCardState < systemErrorTaskCard.CurrentCardState)
        {
            return errorTaskCard;
        }

        return systemErrorTaskCard;
    }

    public List<Domain.TaskCard> GetTaskCardsForInstrumentLockScreen(string instrumentNumber)
    {
        var taskCards = GetTaskCardsForInstrument(instrumentNumber);
        return taskCards.Where(x => _taskCardRules.ShowOnLockScreen(x.CardType)).ToList();
    }

    public event Func<Task>? TaskCardsChanged;
    public event Func<Domain.TaskCard, Task>? TaskCardDeleted;
    public event Func<Domain.TaskCard, Task>? TaskCardAdded;
    public event Func<Domain.TaskCard, Task>? TaskCardCreatedOrChanged;

    public void AutoEscalate()
    {
        var taskCardChanged = false;
        var taskCardsToBeRemoved = new List<(TaskCardType CardType, string InstrumentNumber)>();

        //Loop through all the Task Cards
        foreach (var taskCard in _taskCards)
        {
            //Set current state
            var item = taskCard.Value.SetCurrentCardState(_dateTime.Now);
            taskCardChanged |= item.CardStateChanged;

            if (item.CardStateChanged)
            {
                OnTaskCardCreatedOrChanged(taskCard.Value);
            }

            //if the task card can be removed (if the last auto escalation state is hidden)
            if (item.CanBeRemoved)
            {
                taskCardsToBeRemoved.Add((taskCard.Value.CardType, taskCard.Value.InstrumentNumber ?? string.Empty));
            }
        }

        //remove task cards
        foreach (var taskCard in taskCardsToBeRemoved)
        {
            taskCardChanged |= Remove(taskCard);
        }

        //We only call OnTaskCardsChanged one time if a Task Card has changed its status or any task cards has been removed
        if (taskCardChanged || taskCardsToBeRemoved.Count > 0)
        {
            OnTaskCardsChanged();
        }
    }

    public void RemoveTaskCardsForInstrument(string instrumentNumber)
    {
        if (string.IsNullOrWhiteSpace(instrumentNumber))
        {
            return;
        }

        var taskCardsAffected = false;

        foreach (var taskCardType in _taskCardRules.TaskCardsToRemoveWhenInstrumentIsDeleted())
        {
            taskCardsAffected |= Remove((taskCardType, instrumentNumber));
        }

        //We only call OnTaskCardsChanged one time if a Task Card has been removed
        if (taskCardsAffected)
        {
            OnTaskCardsChanged();
        }
    }

    public ControlTower GetControlSignalTowerRequests()
    {
        return _signalTowerManager.GetControlSignalTowerRequests(_taskCards.Values.ToList(), _taskCardRules.ControlsAllInstruments());
    }

    private void OnTaskCardsChanged()
    {
        TaskCardsChanged?.Invoke().Wait();
    }

    private void OnTaskCardCreatedOrChanged(Domain.TaskCard taskCard)
    {
        TaskCardCreatedOrChanged?.Invoke(taskCard).Wait();
    }

    private static List<Domain.TaskCard> SortTaskCards(IEnumerable<Domain.TaskCard> taskCards)
    {
        return taskCards.OrderByDescending(x => x.CurrentCardState).ThenBy(x => x.ExpiresAt is null).ThenBy(x => x.ExpiresAt).ToList();
    }

    /// <summary>
    /// Evaluates which escalation rule is active at the provided timestamp and whether the card can be removed.
    /// </summary>
    /// <param name="taskCard">The task card to evaluate.</param>
    /// <param name="now">The timestamp used to evaluate the active escalation rule.</param>
    /// <returns>The effective state and a flag indicating if the card can be removed.</returns>
    private static (TaskCardState CurrentState, bool CanBeRemoved) EvaluateTaskCard(Domain.TaskCard taskCard, DateTimeOffset now)
    {
        // Pick the rule currently in effect; if none has triggered yet, use the first rule.
        var actingRule = taskCard.Rules.LastOrDefault(x => x.Trigger <= now) ?? taskCard.Rules.First();

        // A card can be fully removed only when the final rule is Hidden.
        var canBeRemoved = actingRule.CardState == TaskCardState.Hidden && ReferenceEquals(actingRule, taskCard.Rules.Last());

        return (actingRule.CardState, canBeRemoved);
    }

    /// <summary>
    /// Determines whether an incoming task card would produce a meaningful behavioral change compared to the existing one.
    /// </summary>
    /// <param name="existingTaskCard">The currently stored task card.</param>
    /// <param name="existingTaskCardEvaluation">The evaluated current state for the existing task card.</param>
    /// <param name="newTaskCard">The incoming task card candidate.</param>
    /// <param name="newTaskCardEvaluation">The evaluated current state for the incoming task card.</param>
    /// <returns><see langword="true"/> when behaviorally relevant data differs; otherwise <see langword="false"/>.</returns>
    private static bool HasMeaningfulChanges(
        Domain.TaskCard existingTaskCard,
        (TaskCardState CurrentState, bool CanBeRemoved) existingTaskCardEvaluation,
        Domain.TaskCard newTaskCard,
        (TaskCardState CurrentState, bool CanBeRemoved) newTaskCardEvaluation)
    {
        // If the current visible state differs, users would see a different card, so we should update.
        if (existingTaskCardEvaluation.CurrentState != newTaskCardEvaluation.CurrentState)
        {
            return true;
        }

        // If one card should be removed while the other should not, this is a meaningful change that
        // should be applied to keep the UI/tower in sync with the intended card behavior.
        if (existingTaskCardEvaluation.CanBeRemoved != newTaskCardEvaluation.CanBeRemoved)
        {
            return true;
        }

        // If the expiration differs, the timing of when the card disappears or de-escalates would differ,
        // which is a meaningful change.
        if (existingTaskCard.ExpiresAt != newTaskCard.ExpiresAt)
        {
            return true;
        }

        // If escalation rules differ, behavior differs, so we must replace the card.
        if (!HasSameEscalationRules(existingTaskCard.Rules, newTaskCard.Rules))
        {
            return true;
        }

        // Reaching here means all behaviorally relevant values are the same, so we can skip replacing the card.
        return false;
    }

    /// <summary>
    /// Checks whether two rule lists produce the same task card behavior.
    /// </summary>
    /// <param name="existingRules">The existing escalation rules.</param>
    /// <param name="newRules">The incoming escalation rules.</param>
    /// <returns><see langword="true"/> when both rule lists are identical; otherwise <see langword="false"/>.</returns>
    private static bool HasSameEscalationRules(List<TriggerState> existingRules, List<TriggerState> newRules)
    {
        // If the counts differ, the rules differ.
        if (existingRules.Count != newRules.Count)
        {
            return false;
        }

        // Every step must match for cards to be considered equal.
        for (var i = 0; i < existingRules.Count; i++)
        {
            // CardState is the core of each step, so any difference means different behavior.
            if (existingRules[i].CardState != newRules[i].CardState)
            {
                return false;
            }

            // If there are multiple steps, the trigger of each step must also match,
            // otherwise the timing of state changes would differ, which is a behavioral change.
            if (existingRules.Count > 1 && existingRules[i].Trigger != newRules[i].Trigger)
            {
                return false;
            }
        }

        // Reaching here means every compared step matched, so behavior is the same.
        return true;
    }
}
