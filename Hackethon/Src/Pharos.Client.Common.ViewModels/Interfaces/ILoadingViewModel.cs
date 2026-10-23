namespace Pharos.Client.Common.ViewModels.Interfaces;

/// <summary>
/// Defines an abstract "view-model" in the Model-View-ViewModel design pattern. View-models are registered as singleton services.
/// </summary>
/// <remarks>
/// 1. Because view-models are global, they must have 1-to-1 match with views.
/// 2. All properties you want to display should be defined in the class implementing the interface.
/// 3. Implement this interface directly only if you have non-editable singleton data object.
/// 4. If you need to view a list of objects implement <see cref="IEntityEditorViewModel{TModel}"/> interface.
/// 5. If you need a data editor, implement <see cref="IListViewModel{TModel}"/> or <see cref="IEditorViewModel"/> interface.
/// </remarks>
public interface ILoadingViewModel : IViewModel
{
    /// <summary>
    /// True if the view model is currently being loaded or updated, otherwise, false.
    /// </summary>
    bool IsBusy { get; }

    /// <summary>
    /// True if the view model needs to be reloaded manually by, otherwise, false.
    /// </summary>
    bool NeedsReload { get; }

    /// <summary>
    /// Asynchronously initializes the view model. See remarks for more information.
    /// </summary>
    /// <returns><see cref="Task"/> object.</returns>
    /// <remarks>
    /// For <see cref="IEntityEditorViewModel{TModel}"/> it sets <see cref="IEntityEditorViewModel{TModel, TKey}.IsNewEntity"/> to true.
    /// </remarks>
    Task LoadAsync();
}