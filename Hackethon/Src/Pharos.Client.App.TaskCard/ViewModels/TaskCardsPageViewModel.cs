using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.App.Services;
using TaskCardState = Pharos.Common.Web.Components.TaskCard.TaskCardState;

namespace Pharos.Client.App.TaskCard.ViewModels;

public class TaskCardsPageViewModel : TaskCardListViewModel
{
    public TaskCardsPageViewModel(ICurrentInstrumentTaskCardItemSource itemSource, ITaskCardRefreshNotificationProvider refreshNotificationProvider, IDateTime dateTime, ITaskCardConnectedPage connectedPage, ITaskCardTranslator translator, ITimeCalculator timeCalculator, TimerService timerService) : base(itemSource, refreshNotificationProvider, dateTime, connectedPage, translator, timeCalculator, timerService)
    {
    }
    public IEnumerable<IGrouping<int, TaskCardViewModel>> GetTaskCards()
    {
        return this.GroupBy(x => (GetTaskCardsSeverityLevel(x), x.Time))
                   .SelectMany(g => g.OrderBy(x => x.Title, StringComparer.OrdinalIgnoreCase))
                   .GroupBy(GetTaskCardsSeverityLevel)
                   .OrderBy(g => g.Key);
    }
    private static int GetTaskCardsSeverityLevel(TaskCardViewModel taskCard)
    {
        return taskCard.CardState switch
        {
            TaskCardState.FlashingRed or TaskCardState.Error => 0,
            TaskCardState.Warning => 1,
            TaskCardState.Information => 2,
            _ => int.MaxValue
        };
    }
}