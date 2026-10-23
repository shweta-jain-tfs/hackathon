using Pharos.Client.App.Parameters.Requestors.Models;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.App.Services;

namespace Pharos.Client.App.Parameters.Requestors.ViewModels;

public class RequestorDetailPageViewModel : DetailsPageViewModel<RequestorDetailsViewModel, RequestorDetails>
{
    private readonly UiSettingsService _uiSettingsService;
    private readonly IRequestorDetailsDataService _dataService;
    private readonly IDateTime _dateTime;

    public RequestorDetailPageViewModel(
        IRequestorDetailsDataService dataService,
        UiSettingsService uiSettingsService,
        IRequestorDetailsNotificationProvider notificationProvider,
        IDateTime dateTime) : base(dataService, notificationProvider)
    {
        _dataService = dataService;
        _uiSettingsService = uiSettingsService;
        _dateTime = dateTime;
    }

    protected override async Task<RequestorDetailsViewModel> CreateViewModel(RequestorDetails model)
    {
        var uiSettings = await _uiSettingsService.GetSettingsAsync();
        return new RequestorDetailsViewModel(model, _dataService, uiSettings, _dateTime);
    }

    protected override Task<RequestorDetails> CreateDefaultModel()
    {
        return Task.FromResult(new RequestorDetails
        {
            CounterResetDate = DateTimeOffset.Now
        });
    }
}