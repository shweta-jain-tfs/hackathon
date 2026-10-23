using Pharos.Client.App.ServiceTools.Services;
using Pharos.Client.Common.App.Constants;
using Pharos.Client.Common.Models.Notifications;
using Pharos.Contracts.Notifications;

namespace Pharos.Client.App.ServiceTools.ViewModels;

internal sealed class LogDumpViewModel : LoadingViewModel, IDisposable
{
    private readonly ILogDumpDataService _dataService;
    private readonly IJobNotificationProvider _jobNotificationProvider;

    public LogDumpViewModel(ILogDumpDataService dataService, IJobNotificationProvider jobNotificationProvider)
    {
        _dataService = dataService;
        _jobNotificationProvider = jobNotificationProvider;
        _jobNotificationProvider.JobCompleted += OnJobCompleted;
    }

    public Guid? FileId
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged(nameof(CanDownload));
        }
    }

    public string? FileCreatedDate
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged(nameof(FileCreatedDate));
        }
    }

    public string? FileName { get; private set; }

    public bool CollectionStarted
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged(nameof(CollectionStarted));
        }
    }

    public bool CanDownload => FileId.HasValue && FileId != Guid.Empty;

    public void Dispose()
    {
        _jobNotificationProvider.JobCompleted -= OnJobCompleted;
        GC.SuppressFinalize(this);
    }

    public async Task<bool> IsCollectionInProgressAsync()
    {
        return await _dataService.IsCollectionInProgressAsync();
    }

    public async Task<bool> StartCollectionAsync(LogDumpCollectionRequest request)
    {
        CollectionStarted = await _dataService.StartCollectionAsync(request);
        return CollectionStarted;
    }

    public async Task CancelCollectionAsync()
    {
        await _dataService.CancelCollectionAsync();
    }

    protected override async Task DoLoadAsync()
    {
        var result = await _dataService.GetCollectionResultAsync();
        FileId = result.FileId;
        FileName = result.FileName;
        FileCreatedDate = FormatDateTime(result.FileCreatedDateTime);
    }

    private async void OnJobCompleted(object? sender, JobCompletedEventArgs e)
    {
        if (e.JobName == JobNames.LogDump)
        {
            CollectionStarted = false;
            await DoLoadAsync();
        }
    }

    private static string FormatDateTime(DateTimeOffset? fileCreatedDateTime)
    {
        if (!fileCreatedDateTime.HasValue)
        {
            return string.Empty;
        }

        return fileCreatedDateTime.Value.ToString(FormatStrings.SortableDateTime);
    }
}