namespace Pharos.Client.Common.Models.Notifications;

/// <summary>
/// Defines a notification provider for server-side data modifications.
/// </summary>
/// <typeparam name="TModel">The type of the entity for which notifications a provided.</typeparam>
/// <typeparam name="TKey">The type of the ID field</typeparam>
public interface IEntityNotificationProvider<TModel, out TKey> : INotificationProvider
    where TModel : IEntityModel<TKey>
{
    event Func<TKey, Task> Created;
    event Func<TKey, Task> Updated;
    event Func<TKey, Task> Deleted;
}

public interface IEntityNotificationProvider<TModel> : IEntityNotificationProvider<TModel, int>
    where TModel : IEntityModel<int>
{ }