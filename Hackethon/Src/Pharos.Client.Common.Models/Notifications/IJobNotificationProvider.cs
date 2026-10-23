namespace Pharos.Client.Common.Models.Notifications;

public class JobCompletedEventArgs(string jobName, Guid startedBy, bool isSuccessful, string resultMessage)
    : EventArgs
{
    public string JobName { get; } = jobName;
    public Guid StartedBy { get; } = startedBy;
    public bool IsSuccessful { get; } = isSuccessful;
    public string ResultMessage { get; } = resultMessage;
}

public interface IJobNotificationProvider
{
    event EventHandler<JobCompletedEventArgs> JobCompleted;
}
