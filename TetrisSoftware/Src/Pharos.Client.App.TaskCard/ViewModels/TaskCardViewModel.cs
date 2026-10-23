using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.App.Services;
using Pharos.Common.Web.Components.TaskCard;
using TaskCardModel = Pharos.Client.Common.Models.TaskCard;
using TaskCardState = Pharos.Common.Web.Components.TaskCard.TaskCardState;

namespace Pharos.Client.App.TaskCard.ViewModels;

public class TaskCardViewModel : ViewModelItem<TaskCardModel>
{
    private readonly IDateTime _dateTime;
    private readonly ITimeCalculator _timeCalculator;
    private readonly TimerService _timerService;

    public TaskCardViewModel(TaskCardModel model, IDateTime dateTime, ITaskCardConnectedPage connectedPage, ITaskCardTranslator translator, ITimeCalculator timeCalculator, TimerService timerService) : base(model)
    {
        _dateTime = dateTime;
        _timeCalculator = timeCalculator;
        _timerService = timerService;
        _timerService.OnTimerTick += TimerTick;

        CardState = SetCardState(Model.CardState);
        Title = translator.GetTitle(Model.CardType);
        Description = translator.GetDescription(Model.CardType);

        CardType = Model.CardType;
        HasHelp = true;

        var connectedPageInfo = connectedPage.GetConnectedPageInfo(CardType);
        ConnectedPageUrl = connectedPageInfo.Url;
        ConnectedPageTitle = connectedPageInfo.Title;

        SetTime(_dateTime.Now, Model.ExpiresAt);
    }

    public TaskCardState CardState { get; }
    public string Title { get; }
    public string? Description { get; }
    public string? Time { get; private set; }
    public bool DisplayTime => CardState is TaskCardState.Information or TaskCardState.Warning;
    public TaskCardType CardType { get; }
    public TaskCardImageType ImageType => Enum.TryParse<TaskCardImageType>(CardType.ToString(), out var result) ? result : TaskCardImageType.Generic;
    public bool HasHelp { get; }
    public string ConnectedPageUrl { get; }
    public string ConnectedPageTitle { get; }

    private void TimerTick()
    {
        SetTime(_dateTime.Now, Model.ExpiresAt);
    }

    private void SetTime(DateTimeOffset utcNow, DateTimeOffset? expiresAt)
    {
        var time = _timeCalculator.GetDifference(utcNow, expiresAt);

        if (Time != time)
        {
            Time = time;
            OnPropertyChanged(nameof(Time));
        }
    }

    private static TaskCardState SetCardState(Common.Models.TaskCardState cardState)
    {
        return cardState switch
        {
            Common.Models.TaskCardState.FlashingRed => TaskCardState.FlashingRed,
            Common.Models.TaskCardState.Error => TaskCardState.Error,
            Common.Models.TaskCardState.Warning => TaskCardState.Warning,
            Common.Models.TaskCardState.Information => TaskCardState.Information,
            _ => throw new ArgumentOutOfRangeException(nameof(cardState), cardState, null)
        };
    }

    protected override void Disposing()
    {
        _timerService.OnTimerTick -= TimerTick;
    }
}
