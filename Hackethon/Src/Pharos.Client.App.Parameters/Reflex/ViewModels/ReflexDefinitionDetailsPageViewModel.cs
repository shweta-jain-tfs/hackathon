using Pharos.Client.App.Parameters.Common.Models;
using Pharos.Client.App.Parameters.Reflex.Models;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.App.Services;
using Pharos.Contracts.Security;

namespace Pharos.Client.App.Parameters.Reflex.ViewModels;

public class ReflexDefinitionDetailsPageViewModel : DetailsPageViewModel<ReflexDefinitionDetailsViewModel, ReflexDefinition>
{
    private readonly IMethodTestDataSource _methodTestDataSource;
    private readonly IRequestorDataSource _requestorDataSource;
    private readonly IAuthorizationService _authorizationService;
    private readonly UiSettingsService _uiSettingsService;

    public ReflexDefinitionDetailsPageViewModel(IReflexDetailDataService dataService,
        IMethodTestDataSource methodTestDataSource,
        IReflexDetailNotificationProvider notificationProvider,
        IRequestorDataSource requestorDataSource,
        IAuthorizationService authorizationService,
        UiSettingsService uiSettingsService) : base(dataService, notificationProvider)
    {
        _methodTestDataSource = methodTestDataSource;
        _requestorDataSource = requestorDataSource;
        _authorizationService = authorizationService;
        _uiSettingsService = uiSettingsService;
    }

    protected override Task<ReflexDefinition> CreateDefaultModel()
    {
        return Task.FromResult(new ReflexDefinition
        {
            ReflexCondition = new ReflexCondition(),
            ReflexTests = new List<ReflexTest>(),
            Active = true,
            Automatic = true
        });
    }

    protected override async Task<ReflexDefinitionDetailsViewModel> CreateViewModel(ReflexDefinition model)
    {
        Requestor requestor;

        if (string.IsNullOrWhiteSpace(model.RequestorId))
        {
            requestor = new Requestor { Id = 0, Name = string.Empty, RequestorId = string.Empty };
        }
        else
        {
            try
            {
                var result = await _requestorDataSource.GetRequestorByRequestorId(model.RequestorId);
                requestor = result ?? new Requestor { Id = 0, Name = string.Empty, RequestorId = string.Empty };
            }
            catch
            {
                requestor = new Requestor { Id = 0, Name = string.Empty, RequestorId = string.Empty };
            }
        }

        var canEditReflexTask = _authorizationService.IsAuthorized(Permissions.EditTestSettings);
        var canViewPersonalAndHealthDataTask = _authorizationService.IsAuthorized(Permissions.ViewPersonalAndHealthData);
        var uiSettingsTask = _uiSettingsService.GetSettingsAsync();

        await Task.WhenAll(canEditReflexTask, canViewPersonalAndHealthDataTask, uiSettingsTask);

        var canEditReflex = canEditReflexTask.Result;
        var canViewPersonalAndHealthData = canViewPersonalAndHealthDataTask.Result;
        var uiSettings = uiSettingsTask.Result;

        var requestorViewModel = new RequestorViewModel(requestor, _requestorDataSource, canViewPersonalAndHealthData && canEditReflex, canViewPersonalAndHealthData);

        return new ReflexDefinitionDetailsViewModel(model, _methodTestDataSource, requestorViewModel, uiSettings, canEditReflex);
    }
}