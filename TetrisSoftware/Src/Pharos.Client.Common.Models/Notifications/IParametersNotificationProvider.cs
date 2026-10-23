namespace Pharos.Client.Common.Models.Notifications;

public interface IParametersNotificationProvider : INotificationProvider
{
    event EventHandler<bool> ParameterUpdateFinished;
}