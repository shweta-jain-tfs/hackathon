namespace Pharos.Client.Common.Models.Notifications;

/// <summary>
/// Represents a notification provider for details pages.
/// This interface is used to notify the DetailsPageViewModel when the underlying model is updated or deleted.
/// Only has events for ModelUpdated and ModelDeleted. No event for ModelCreated is needed. Since when you are looking at a details page, the model is already created and cannot be created again.
/// And the details page does not need to know when other models are created.
/// </summary>
/// <typeparam name="TKey">The key/id of the underlying model</typeparam>
public interface IDetailsNotificationProvider<TKey>
{
    event EventHandler<TKey> ModelUpdated;
    event EventHandler<TKey> ModelDeleted;
}