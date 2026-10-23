using System.Collections.Specialized;
using Pharos.Client.Common.Models.Notifications;

namespace Pharos.Client.Common.ViewModels.Interfaces;

/// <summary>
/// Defines an abstract view-model for a "list of objects".
/// </summary>
/// <remarks>
///  1. "Lists" are not intended to be editable. If you need a to add or edit objects,
///     implement the <see cref="IEditorViewModel"/> interface.
///  2. Views must subscribe to <see cref="INotifyCollectionChanged.CollectionChanged"/> event 
///     to get notifications about data changes.
///  3. View-models can subscribe to server notifications using <see cref="INotificationProvider"/>
/// </remarks>
public interface IListViewModel<TModel> : ILoadingViewModel, IReadOnlyCollection<TModel>, INotifyCollectionChanged
{
    /// <summary>
    /// Gets the data model by index.
    /// </summary>
    /// <param name="index">The index of the object in the list.</param>
    /// <returns>Instance of a data model.</returns>
    TModel this[int index] { get; set; }
}