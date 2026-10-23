using Pharos.Client.Common.Models;

namespace Pharos.Client.Common.ViewModels.Interfaces;

/// <summary>
/// Defines a generic editor for a data-model (entity). An entity is typically persisted on the server side.
/// </summary>
public interface IEntityEditorViewModel<out TModel, in TKey> : IEditorViewModel, IDisposable where TModel : IEntityModel<TKey>
{
    /// <summary>
    /// True if the view-model contains the new entity, which is not yet saved to a data storage.
    /// </summary>
    bool IsNewEntity { get; }

    TModel Entity { get; }

    /// <summary>
    /// Asynchronously loads the existing data-model into the view-model. Call this method to initialize or reload the view-model.
    /// </summary>
    /// <param name="entityId">The ID of the entity to load.</param>
    /// <returns><see cref="Task"/> object.</returns>
    Task LoadAsync(TKey entityId);
}

public interface IEntityEditorViewModel<out TModel> : IEntityEditorViewModel<TModel, int> where TModel : IEntityModel<int>;