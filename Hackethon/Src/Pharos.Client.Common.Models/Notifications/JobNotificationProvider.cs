using Microsoft.AspNetCore.SignalR.Client;
using Pharos.Contracts.Notifications;

namespace Pharos.Client.Common.Models.Notifications;

internal class JobNotificationProvider(
    IHubConnectionFactory hubConnectionFactory)
    : HubNotificationProvider(hubConnectionFactory), IJobNotificationProvider
{
    public event EventHandler<JobCompletedEventArgs>? JobCompleted
    {
        add => Subscribe(nameof(JobCompleted), value);
        remove => Unsubscribe(nameof(JobCompleted), value);
    }

    protected override string HubRoute => HubRoutes.Job;


    protected override void OnHubConnectionCreated(HubConnection hubConnection)
    {
        hubConnection.On<string, Guid, bool, string>(nameof(IJobHub.JobCompleted), OnJobCompleted);
    }

    private void OnJobCompleted(string jobName, Guid startedBy, bool isSuccessful, string resultMessage)
    {
        var handler = GetHandler<EventHandler<JobCompletedEventArgs>>(nameof(JobCompleted));

        handler?.Invoke(this, new JobCompletedEventArgs(jobName, startedBy, isSuccessful, resultMessage));
    }
}