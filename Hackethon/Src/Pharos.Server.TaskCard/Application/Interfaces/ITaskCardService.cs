using Pharos.Server.Application.TaskCard;
using Tetris.Protocol.Instrument.Control;

namespace Pharos.Server.TaskCard.Application.Interfaces;

internal interface ITaskCardService
{
    void Initialize(List<Domain.TaskCard> taskCards);
    void Add(Domain.TaskCard taskCard);
    void Remove(TaskCardType cardType, string? instrumentNumber);
    event Func<Task> TaskCardsChanged;
    event Func<Domain.TaskCard, Task> TaskCardDeleted;
    event Func<Domain.TaskCard, Task> TaskCardAdded;
    event Func<Domain.TaskCard, Task> TaskCardCreatedOrChanged;
    void AutoEscalate();
    void RemoveTaskCardsForInstrument(string instrumentNumber);
    ControlTower GetControlSignalTowerRequests();
    bool Exists(TaskCardType taskCardType, string? instrumentNumber);
    List<Domain.TaskCard> GetTaskCardsForInstrument(string instrumentNumber);
    List<Domain.TaskCard> GetTaskCardsForInstrumentLockScreen(string instrumentNumber);
    List<Domain.TaskCard> GetTaskCardsForInstruments(List<string> instrumentNumbers);
}

internal record ControlTower(IDictionary<string, ControlSignalTowerRequest> InstrumentSpecific, ControlSignalTowerRequest? AllInstruments);
