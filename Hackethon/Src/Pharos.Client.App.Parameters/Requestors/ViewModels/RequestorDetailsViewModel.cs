using Pharos.Client.App.Parameters.Requestors.Models;
using Pharos.Client.Common.App.Interfaces;

namespace Pharos.Client.App.Parameters.Requestors.ViewModels;

public sealed class RequestorDetailsViewModel : ViewModelItem<RequestorDetails>
{
    private readonly IRequestorDetailsDataService _requestorDataService;
    private readonly IDateTime _dateTime;

    public RequestorDetailsViewModel(RequestorDetails model,
        IRequestorDetailsDataService requestorDataService,
        UiSettings uiSettings,
        IDateTime dateTime) : base(model)
    {
        _requestorDataService = requestorDataService;
        _dateTime = dateTime;
        UiSettings = uiSettings;
    }

    public bool IsNewEntity => Model.Id == 0;

    public string RequestorId
    {
        get => Model.RequestorId;
        set => SetProperty(Model.RequestorId, value, v => Model.RequestorId = v);
    }

    public string? Name
    {
        get => Model.Name;
        set => SetProperty(Model.Name, value, v => Model.Name = v);
    }

    public string? Address
    {
        get => Model.Address;
        set => SetProperty(Model.Address, value, v => Model.Address = v);
    }

    public string? Comment
    {
        get => Model.Comment;
        set => SetProperty(Model.Comment, value, v => Model.Comment = v);
    }

    public string? ZipCode
    {
        get => Model.ZipCode;
        set => SetProperty(Model.ZipCode, value, v => Model.ZipCode = v);
    }

    public string? OrderingPhysicianCode
    {
        get => Model.OrderingPhysicianCode;
        set => SetProperty(Model.OrderingPhysicianCode, value, v => Model.OrderingPhysicianCode = v);
    }

    public string? PhoneNumber
    {
        get => Model.PhoneNumber;
        set => SetProperty(Model.PhoneNumber, value, v => Model.PhoneNumber = v);
    }

    public bool ReflexEnabled
    {
        get => Model.ReflexEnabled;
        set => SetProperty(Model.ReflexEnabled, value, v => Model.ReflexEnabled = v);
    }

    public string? ShortcutCode
    {
        get => Model.ShortcutCode;
        set => SetProperty(Model.ShortcutCode, value, v => Model.ShortcutCode = v);
    }

    public DateTimeOffset? LatestSampleDate
    {
        get => Model.LatestSampleDate;
        set => SetProperty(Model.LatestSampleDate, value, v => Model.LatestSampleDate = v);
    }

    public int NumberOfReportedSamples
    {
        get => Model.NumberOfReportedSamples;
        set => SetProperty(Model.NumberOfReportedSamples, value, v => Model.NumberOfReportedSamples = v);
    }

    public int NumberOfReportedTests
    {
        get => Model.NumberOfReportedTests;
        set => SetProperty(Model.NumberOfReportedTests, value, v => Model.NumberOfReportedTests = v);
    }

    public DateTimeOffset? CounterResetDate
    {
        get => Model.CounterResetDate;
        set => SetProperty(Model.CounterResetDate, value, v => Model.CounterResetDate = v);
    }

    public async Task ResetCountersAsync()
    {
        await _requestorDataService.ResetCountersAsync(Model.Id);

        NumberOfReportedSamples = 0;
        NumberOfReportedTests = 0;
        CounterResetDate = _dateTime.Now;

        SetAsUnmodified();
    }

    public UiSettings UiSettings { get; }
}