namespace Pharos.Client.Common.ViewModels.Interfaces;

/// <summary>
/// Defines an abstract "view-model" for an editor which can save data to a storage.
/// </summary>
public interface IEditorViewModel : ILoadingViewModel
{
    /// <summary>
    /// Indicates whether the model was modified.
    /// </summary>
    bool IsModified { get; }

    /// <summary>
    /// Asynchronously saves data from view-model to a storage. 
    /// </summary>
    /// <returns>True if operation was successful, otherwise, false.</returns>
    Task<bool> SaveAsync();
}