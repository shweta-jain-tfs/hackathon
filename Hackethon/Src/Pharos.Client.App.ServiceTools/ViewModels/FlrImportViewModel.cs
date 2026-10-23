using Pharos.Client.Common.Models.Notifications;
using Pharos.Contracts.Notifications;

namespace Pharos.Client.App.ServiceTools.ViewModels;

internal class FlrImportViewModel : LoadingViewModel, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly IJobNotificationProvider _jobNotificationProvider;

    public FlrImportViewModel(HttpClient httpClient, IJobNotificationProvider jobNotificationProvider)
    {
        _httpClient = httpClient;
        _jobNotificationProvider = jobNotificationProvider;
        _jobNotificationProvider.JobCompleted += OnJobCompleted;
    }

    private void OnJobCompleted(object? sender, JobCompletedEventArgs e)
    {
        if (e.JobName == JobNames.FlrImport)
        {
            IsImporting = false;
        }
    }

    public bool IsImporting
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged(nameof(IsImporting));
        }
    }

    protected override async Task DoLoadAsync()
    {
        var client = new FlrClient(_httpClient);
        IsImporting = await client.IsFlrImportInProgressAsync();
    }

    public async Task StartImportAsync(IBrowserFile file)
    {
        await using var fileStream = file.OpenReadStream(file.Size);
        var client = new FlrClient(_httpClient);
        await client.StartFlrImportAsync(new FileParameter(fileStream, file.Name));
        IsImporting = true;
    }

    public void Dispose()
    {
        _jobNotificationProvider.JobCompleted -= OnJobCompleted;
    }
}
