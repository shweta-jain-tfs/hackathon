namespace Pharos.Common.Web.Components.Modal;

public interface IModalService
{
    /// <summary>
    /// Shows a modal containing the specified <typeparamref name="TComponent"/>.
    /// </summary>
    /// <typeparam name="TComponent">The type of the component to display in the modal.</typeparam>
    /// <returns>An <see cref="IModalReference"/> that represents the modal instance.</returns>
    IModalReference Show<TComponent>() where TComponent : IComponent;

    /// <summary>
    /// Shows a modal containing a <typeparamref name="TComponent"/> with the specified <paramref name="options"/>.
    /// </summary>
    /// <typeparam name="TComponent">The type of the component to display in the modal.</typeparam>
    /// <param name="options">Options to configure the modal.</param>
    /// <returns>An <see cref="IModalReference"/> that represents the modal instance.</returns>
    IModalReference Show<TComponent>(ModalOptions options) where TComponent : IComponent;

    /// <summary>
    /// Shows a modal containing a <typeparamref name="TComponent"/> with the specified <paramref name="parameters"/>.
    /// </summary>
    /// <typeparam name="TComponent">The type of the component to display in the modal.</typeparam>
    /// <param name="parameters">Parameters to pass to the modal component.</param>
    /// <returns>An <see cref="IModalReference"/> that represents the modal instance.</returns>
    IModalReference Show<TComponent>(ModalParameters parameters) where TComponent : IComponent;

    /// <summary>
    /// Shows a modal containing a <typeparamref name="TComponent"/> with the specified <paramref name="options"/> and <paramref name="parameters"/>.
    /// </summary>
    /// <typeparam name="TComponent">The type of the component to display in the modal.</typeparam>
    /// <param name="options">Options to configure the modal.</param>
    /// <param name="parameters">Parameters to pass to the modal component.</param>
    /// <returns>An <see cref="IModalReference"/> that represents the modal instance.</returns>
    IModalReference Show<TComponent>(ModalOptions options, ModalParameters parameters) where TComponent : IComponent;

    /// <summary>
    /// Shows a confirmation modal with the specified <paramref name="message"/>, <paramref name="ok"/>, and <paramref name="cancel"/> button text.
    /// </summary>
    /// <param name="message">The message to display in the confirmation modal.</param>
    /// <param name="cancel">The text for the Cancel button.</param>
    /// <param name="ok">The text for the OK button.</param>
    /// <param name="options"></param>
    /// <param name="centerText"></param>
    /// <returns>An <see cref="IModalReference"/> that represents the modal instance.</returns>
    IModalReference ShowConfirm(string message, string cancel, string ok, ModalOptions options, bool centerText = false);

    /// <summary>
    /// Shows an informational modal with the specified <paramref name="message"/>, and <paramref name="ok"/> button text.
    /// </summary>
    /// <param name="message">The message to display in the informational modal.</param>
    /// <param name="ok">The text for the OK button.</param>
    /// <param name="options"></param>
    /// <param name="centerText"></param>
    /// <returns>An <see cref="IModalReference"/> that represents the modal instance.</returns>
    IModalReference ShowInformation(string message, string ok, ModalOptions options, bool centerText = false);

    void CloseModal(IModalReference modalRef);

    /// <summary>
    /// Determines whether there are any open modal dialogs.
    /// </summary>
    /// <returns>True if there is at least one open modal dialog; otherwise, false.</returns>
    bool HasOpenModals();
}
