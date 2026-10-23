using Pharos.Client.App.Request.Models;
using Pharos.Client.Common.App.Services;

namespace Pharos.Client.App.Request.ViewModels;

public class RequestDetailPageViewModel : DetailsPageViewModel<RequestDetailsViewModel, RequestDetails>
{
    private readonly IRequestDetailsNotificationProvider _detailsNotificationProvider;
    private readonly ISearchPatientDataSource _patientDataSource;
    private readonly IReflexDefinitionDataSource _reflexDefinitionDataSource;
    private readonly ISampleTubeNotificationProvider _sampleTubeNotificationProvider;
    private readonly IRequestorDataSource _requestorDataSource;
    private readonly ISampleTubeDataSource _sampleTubeDataSource;
    private readonly IEntityDataSource<TubeDefinition> _tubeDefinitionDataSource;
    private readonly UiSettingsService _uiSettingsService;
    private readonly Debouncer _refreshDebouncer = new(300);
    private readonly IStringLocalizer<Translations> _loc;
    private readonly IVolumeCalculationService _volumeCalculation;

    public RequestDetailPageViewModel(
        IRequestorDataSource requestorDataSource,
        ISearchPatientDataSource patientDataSource,
        UiSettingsService uiSettingsService,
        IEntityDataSource<TubeDefinition> tubeDefinitionDataSource,
        IRequestDetailsDataService dataService,
        IRequestDetailsNotificationProvider notificationProvider,
        ISampleTubeDataSource sampleTubeDataSource,
        IReflexDefinitionDataSource reflexDefinitionDataSource,
        ISampleTubeNotificationProvider sampleTubeNotificationProvider,
        IStringLocalizer<Translations> loc,
        IVolumeCalculationService volumeCalculation) : base(dataService, notificationProvider)
    {
        _requestorDataSource = requestorDataSource;
        _patientDataSource = patientDataSource;
        _uiSettingsService = uiSettingsService;
        _tubeDefinitionDataSource = tubeDefinitionDataSource;
        _volumeCalculation = volumeCalculation;
        _sampleTubeDataSource = sampleTubeDataSource;
        _reflexDefinitionDataSource = reflexDefinitionDataSource;
        _sampleTubeNotificationProvider = sampleTubeNotificationProvider;
        _loc = loc;
        _detailsNotificationProvider = notificationProvider;
    }

    protected override async Task<RequestDetails> CreateDefaultModel()
    {
        var uiSettings = await _uiSettingsService.GetSettingsAsync();

        return new RequestDetails
        {
            PreDilutionFactor = 1,
            RequestDate = DateTimeOffset.Now,
            SampleCollectionDate = DateTimeOffset.Now,
            RequestState = RequestState.NotStarted,
            Requestor = new Requestor(),
            Patient = new Patient(),
            TestOrders = new List<RequestTest>(),
            SampleVolumeCheck = uiSettings.RequestSettings.DefaultSampleVolumeCheck
        };
    }

    protected override async Task<RequestDetailsViewModel> CreateViewModel(RequestDetails model)
    {
        var tubeDefinitionsTask = _tubeDefinitionDataSource.GetAsync(new DataSourceRequest());
        var uiSettingsTask = _uiSettingsService.GetSettingsAsync();

        var existingRequest = !string.IsNullOrEmpty(model.SampleId);
        var sampleTubesTask = Task.FromResult<List<SampleTubeHistory>>(null!);
        var sampleVolumeNeededTask = Task.FromResult<int?>(null!);
        if (existingRequest)
        {
            sampleTubesTask = _sampleTubeDataSource.GetSampleTubes(model.Id);
            sampleVolumeNeededTask = _volumeCalculation.GetNeededSampleVolume(model.TestOrders);
        }

        await Task.WhenAll(tubeDefinitionsTask, uiSettingsTask, sampleTubesTask, sampleVolumeNeededTask);

        if (!existingRequest)
        {
            // Pre-select the default tube type (NormalTubeDefinitionId from settings) when creating a new request.
            model.SampleTubeType = uiSettingsTask.Result.RequestSettings.DefaultTubeTypeId;
        }

        var viewModel = new RequestDetailsViewModel(
            model,
            tubeDefinitionsTask.Result.Items,
            sampleVolumeNeededTask.Result,
            _requestorDataSource,
            _patientDataSource,
            uiSettingsTask.Result,
            _sampleTubeDataSource,
            _reflexDefinitionDataSource,
            _loc,
            _volumeCalculation);

        if (existingRequest)
        {
            viewModel.SetSampleTubes(sampleTubesTask.Result);
        }

        return viewModel;
    }

    protected override void OnSubscribeToNotifications()
    {
        _detailsNotificationProvider.RequestsDeleted += OnRequestsDeleted;
        _sampleTubeNotificationProvider.SampleTubeUpdated += OnSampleTubeUpdated;
    }

    private void OnSampleTubeUpdated(object? sender, int requestId)
    {
        if (CurrentDetailsViewModel != null && CurrentDetailsViewModel.Model.Id == requestId)
        {
            _refreshDebouncer.Debounce(async token =>
            {
                if (!token.IsCancellationRequested)
                {
                    var sampleTubes = await _sampleTubeDataSource.GetSampleTubes(CurrentDetailsViewModel.Model.Id);
                    CurrentDetailsViewModel.SetSampleTubes(sampleTubes);
                }
            });
        }
    }

    public event EventHandler<List<int>>? RequestsDeleted;

    private void OnRequestsDeleted(object? sender, List<int> e)
    {
        RequestsDeleted?.Invoke(this, e);
    }

    protected override void OnUnsubscribeFromNotifications()
    {
        _detailsNotificationProvider.RequestsDeleted -= OnRequestsDeleted;
        _sampleTubeNotificationProvider.SampleTubeUpdated -= OnSampleTubeUpdated;
        _refreshDebouncer.Dispose();
    }
}