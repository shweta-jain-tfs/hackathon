using System.ComponentModel;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.App.Services;

namespace Pharos.Client.App.TaskCard.ViewModels;

public abstract class TaskCardListViewModel : ListViewModel<TaskCardViewModel, Common.Models.TaskCard>
{
    private readonly IDateTime _dateTime;
    private readonly ITaskCardConnectedPage _connectedPage;
    private readonly ITaskCardTranslator _translator;
    private readonly ITimeCalculator _timeCalculator;
    private readonly TimerService _timerService;
    
    protected TaskCardListViewModel(IItemsSource<Common.Models.TaskCard> itemSource,
                                    ITaskCardRefreshNotificationProvider refreshNotificationProvider,
                                    IDateTime dateTime, ITaskCardConnectedPage connectedPage,
                                    ITaskCardTranslator translator,
                                    ITimeCalculator timeCalculator,
                                    TimerService timerService) : base(itemSource, refreshNotificationProvider)
    {
        _dateTime = dateTime;
        _connectedPage = connectedPage;
        _translator = translator;
        _timeCalculator = timeCalculator;
        _timerService = timerService;
    }

    protected override TaskCardViewModel CreateViewModel(Common.Models.TaskCard model)
    {
        var viewModelItem = new TaskCardViewModel(model, _dateTime, _connectedPage, _translator, _timeCalculator, _timerService);
        viewModelItem.PropertyChanged += ViewModelItemOnPropertyChanged;
        return viewModelItem;
    }

    private void ViewModelItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TaskCardViewModel.Time))
        {
            OnViewRefreshNeeded();
        }
    }

    protected override void Disposing()
    {
        foreach (var item in this)
        {
            item.PropertyChanged -= ViewModelItemOnPropertyChanged;
            item.Dispose();
        }
    }
}