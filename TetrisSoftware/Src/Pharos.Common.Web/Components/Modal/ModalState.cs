using System.Collections.ObjectModel;

namespace Pharos.Common.Web.Components.Modal;

/// <summary>
/// Represents the state of modals in the application.
/// </summary>
public class ModalState
{
    /// <summary>
    /// Occurs when a modal instance is added.
    /// </summary>
    public event Action? OnModalInstanceAdded;

    /// <summary>
    /// Occurs when a modal close is requested.
    /// </summary>
    public event Action? OnModalCloseRequested;

    /// <summary>
    /// Collection of modal references.
    /// </summary>
    internal readonly Collection<ModalReference> _modals = new();

    /// <summary>
    /// Gets a modal by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the modal.</param>
    /// <returns>The modal reference if found; otherwise, null.</returns>
    public ModalReference? GetModal(Guid id)
    {
        return _modals.FirstOrDefault(x => x.Id == id);
    }

    /// <summary>
    /// Checks if a modal with the specified component type and parameters already exists.
    /// </summary>
    /// <param name="contentComponent">The type of the component to check.</param>
    /// <param name="parameters">The parameters to compare.</param>
    /// <returns>The existing modal reference if found; otherwise, null.</returns>
    public ModalReference? FindExistingModal(Type contentComponent, ModalParameters parameters)
    {
        return _modals.FirstOrDefault(x => 
            x.ContentComponent == contentComponent && 
            x.ParametersMatch(parameters));
    }

    /// <summary>
    /// Adds a modal to the collection and triggers the OnModalInstanceAdded event.
    /// </summary>
    /// <param name="modalReference">The modal reference to add.</param>
    public void AddModal(ModalReference modalReference)
    {
        _modals.Add(modalReference);
        OnModalInstanceAdded?.Invoke();
    }

    /// <summary>
    /// Removes a modalRef from the collection and triggers the OnModalCloseRequested event.
    /// </summary>
    /// <param name="modalRef">The modalRef reference to remove.</param>
    public void RemoveModal(ModalReference modalRef)
    {
        _modals.Remove(modalRef);
        OnModalCloseRequested?.Invoke();
    }

    /// <summary>
    /// Cancels all modals and clears the collection.
    /// </summary>
    /// <returns>True if the operation was successful; otherwise, false.</returns>
    public bool CancelModals()
    {
        foreach (var modalReference in _modals.ToList())
        {
            modalReference.Dismiss(ModalResult.Cancel());
            modalReference.CloseHandler();
        }

        _modals.Clear();
        return true;
    }

    /// <summary>
    /// Cancels all modals with the specified tag.
    /// </summary>
    /// <param name="tag">The tag identifying the modals to cancel.</param>
    public void CancelModalsByTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return;
        }

        var modalsToCancel = _modals
            .Where(m => m.Tag == tag)
            .ToList();

        foreach (var modalReference in modalsToCancel)
        {
            modalReference.Dismiss(ModalResult.Cancel());
            modalReference.CloseHandler();
            _modals.Remove(modalReference);
        }
    }

    /// <summary>
    /// Determines whether there are any open modal dialogs.
    /// </summary>
    /// <returns>True if there is at least one open modal dialog; otherwise, false.</returns>
    public bool HasOpenModals()
    {
        return _modals.Count > 0;
    }
}
