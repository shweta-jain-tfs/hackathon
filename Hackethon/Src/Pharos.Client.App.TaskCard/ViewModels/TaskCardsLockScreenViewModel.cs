using Pharos.Client.App.TaskCard.Models;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.App.Services;

namespace Pharos.Client.App.TaskCard.ViewModels;

public class TaskCardsLockScreenViewModel : TaskCardListViewModel
{
    public TaskCardsLockScreenViewModel(ILockScreenTaskCardItemSource itemSource, ITaskCardRefreshNotificationProvider refreshNotificationProvider, IDateTime dateTime, ITaskCardConnectedPage connectedPage, ITaskCardTranslator translator, ITimeCalculator timeCalculator, TimerService timerService) : base(itemSource, refreshNotificationProvider, dateTime, connectedPage, translator, timeCalculator, timerService)
    {
    }

    public IEnumerable<TaskCardViewModel> SortTaskCards()
    {
        return this.GroupBy(x => (x.CardState, x.Time))
                   .SelectMany(g => g.OrderBy(x => x.Title, StringComparer.OrdinalIgnoreCase));
    }
}