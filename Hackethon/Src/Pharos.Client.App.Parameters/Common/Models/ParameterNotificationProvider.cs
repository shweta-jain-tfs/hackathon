using Pharos.Client.Common.Models.Notifications;
using Pharos.Contracts.Notifications;

namespace Pharos.Client.App.Parameters.Common.Models;

internal class ParameterNotificationProvider : NotificationProviderBase, IParametersNotificationProvider
{
    public ParameterNotificationProvider(IJobNotificationProvider jobNotificationProvider)
    {
        jobNotificationProvider.JobCompleted += OnJobCompleted;
    }

    private void OnJobCompleted(object? sender, JobCompletedEventArgs e)
    {
        if (e.JobName == JobNames.ParametersImport)
        {
            GetHandler<EventHandler<bool>>(nameof(ParameterUpdateFinished))?.Invoke(this, e.IsSuccessful);
        }
    }

    public event EventHandler<bool> ParameterUpdateFinished
    {
        add => Subscribe(nameof(ParameterUpdateFinished), value);
        remove => Unsubscribe(nameof(ParameterUpdateFinished), value);
    }
}