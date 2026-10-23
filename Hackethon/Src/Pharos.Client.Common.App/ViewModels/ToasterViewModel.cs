using Microsoft.AspNetCore.Components.Authorization;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.ViewModels;
using Pharos.Common.Web.Components.Toaster;

namespace Pharos.Client.Common.App.ViewModels;

internal class ToasterViewModel : LoadingViewModel, IToasterService, IDisposable
{
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly IJobNotificationProvider _jobNotifications;

    public ToasterViewModel(AuthenticationStateProvider authStateProvider, IJobNotificationProvider jobNotifications)
    {
        _authStateProvider = authStateProvider;
        _jobNotifications = jobNotifications;

    }

    public event Action<ToastType, string>? OnShowToast;

    public void ShowToast(ToastType type, string text)
    {
        OnShowToast?.Invoke(type, text);
    }

    public void Dispose()
    {
        _jobNotifications.JobCompleted -= OnJobCompleted;
    }

    protected override async Task DoLoadAsync()
    {
        var state = await _authStateProvider.GetAuthenticationStateAsync();

        // When the view is rendered on the lock screen, the user is not authenticated.
        // We only want to subscribe to job notifications if the user is authenticated (to avoid getting HTTP 401 error).
        if (state.User.Identity?.IsAuthenticated ?? false)
        {
            _jobNotifications.JobCompleted += OnJobCompleted;
        }
    }

    private void OnJobCompleted(object? sender, JobCompletedEventArgs e)
    {
        ShowToast(e.IsSuccessful ? ToastType.Information : ToastType.Alarm, e.ResultMessage);
    }
}
