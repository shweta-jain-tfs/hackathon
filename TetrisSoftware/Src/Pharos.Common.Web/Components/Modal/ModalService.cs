using Pharos.Common.Web.Components.Modal.Views;

namespace Pharos.Common.Web.Components.Modal;
/// <summary>
/// Service to manage the display and interaction of modal dialogs.
/// </summary>
public class ModalService : IModalService
{
    private readonly ModalState _modalState;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModalService"/> class.
    /// </summary>
    /// <param name="modalState">The state of the modal dialogs.</param>
    public ModalService(ModalState modalState)
    {
        _modalState = modalState;
    }

    /// <summary>
    /// Shows a modal dialog with the specified component.
    /// </summary>
    /// <typeparam name="TComponent">The type of the component to display in the modal.</typeparam>
    /// <returns>A reference to the displayed modal dialog.</returns>
    public IModalReference Show<TComponent>() where TComponent : IComponent
    {
        return Show<TComponent>(new ModalOptions(), new ModalParameters());
    }

    /// <summary>
    /// Shows a modal dialog with the specified component and options.
    /// </summary>
    /// <typeparam name="TComponent">The type of the component to display in the modal.</typeparam>
    /// <param name="options">The options for the modal dialog.</param>
    /// <returns>A reference to the displayed modal dialog.</returns>
    public IModalReference Show<TComponent>(ModalOptions options) where TComponent : IComponent
    {
        return Show<TComponent>(options, new ModalParameters());
    }

    /// <summary>
    /// Shows a modal dialog with the specified component and parameters.
    /// </summary>
    /// <typeparam name="TComponent">The type of the component to display in the modal.</typeparam>
    /// <param name="parameters">The parameters for the modal dialog.</param>
    /// <returns>A reference to the displayed modal dialog.</returns>
    public IModalReference Show<TComponent>(ModalParameters parameters) where TComponent : IComponent
    {
        return Show<TComponent>(new ModalOptions(), parameters);
    }

    /// <summary>
    /// Shows a modal dialog with the specified component, options, and parameters.
    /// </summary>
    /// <typeparam name="TComponent">The type of the component to display in the modal.</typeparam>
    /// <param name="options">The options for the modal dialog.</param>
    /// <param name="parameters">The parameters for the modal dialog.</param>
    /// <returns>A reference to the displayed modal dialog.</returns>
    public IModalReference Show<TComponent>(ModalOptions options, ModalParameters parameters) where TComponent : IComponent
    {
        return Show(typeof(TComponent), options, parameters);
    }

    /// <summary>
    /// Shows a confirmation modal dialog with the specified title, message, and button texts.
    /// </summary>
    /// <param name="message">The message to display in the modal dialog.</param>
    /// <param name="cancel">The text for the Cancel button.</param>
    /// <param name="ok">The text for the OK button.</param>
    /// <param name="options"></param>
    /// <param name="centerText"></param>
    /// <returns>A reference to the displayed modal dialog.</returns>
    public IModalReference ShowConfirm(string message, string cancel, string ok, ModalOptions options, bool centerText = false)
    {
        var parameters = new ModalParameters
        {
            {nameof(ConfirmView.Message), message},
            {nameof(ConfirmView.OkButton), ok},
            {nameof(ConfirmView.CancelButton), cancel},
        };

        options.CenterText = centerText;

        return Show<ConfirmView>(options, parameters);
    }

    /// <summary>
    /// Shows an information modal dialog with the specified title, message, and button text.
    /// </summary>
    /// <param name="message">The message to display in the modal dialog.</param>
    /// <param name="ok">The text for the OK button.</param>
    /// <param name="options"></param>
    /// <param name="centerText"></param>
    /// <returns>A reference to the displayed modal dialog.</returns>
    public IModalReference ShowInformation(string message, string ok, ModalOptions options, bool centerText = false)
    {
        var parameters = new ModalParameters
        {
            {nameof(ConfirmView.Message), message},
            {nameof(ConfirmView.OkButton), ok},
        };

        options.CenterText = centerText;

        return Show<InformationView>(options, parameters);
    }

    /// <summary>
    /// Shows a modal dialog with the specified component type, options, and parameters.
    /// </summary>
    /// <param name="contentComponent">The type of the component to display in the modal.</param>
    /// <param name="options">The options for the modal dialog.</param>
    /// <param name="parameters">The parameters for the modal dialog.</param>
    /// <returns>A reference to the displayed modal dialog.</returns>
    /// <exception cref="ArgumentException">Thrown when the specified component type is not a Blazor component.</exception>
    public IModalReference Show(Type contentComponent, ModalOptions options, ModalParameters parameters)
    {
        if (!typeof(IComponent).IsAssignableFrom(contentComponent))
        {
            throw new ArgumentException($"{contentComponent.FullName} must be a Blazor Component");
        }

        // Check if a modal with the same component and parameters already exists
        var existingModal = _modalState.FindExistingModal(contentComponent, parameters);
        if (existingModal != null)
        {
            return existingModal;
        }

        var modalInstanceId = Guid.NewGuid();

        var modalContent = new RenderFragment(builder =>
        {
            var i = 0;
            builder.OpenComponent(i++, contentComponent);
            foreach (var (name, value) in parameters._parameters)
            {
                builder.AddAttribute(i++, name, value);
            }
            builder.CloseComponent();
        });

        var modalInstance = new RenderFragment(builder =>
        {
            builder.OpenComponent<ModalInstance>(0);
            builder.SetKey("PhModalInstance_" + modalInstanceId);
            builder.AddAttribute(1, "Options", options);
            builder.AddAttribute(3, "Content", modalContent);
            builder.AddAttribute(5, "Id", modalInstanceId);
            builder.CloseComponent();
        });

        var modalReference = new ModalReference(modalInstanceId, modalInstance, options.CloseOnNavigation, contentComponent, parameters, options.Tag);

        modalReference.Close += ModalReference_Close;

        _modalState.AddModal(modalReference);

        return modalReference;
    }

    /// <summary>
    /// Handles the close event of a modal reference.
    /// </summary>
    /// <param name="sender">The source of the event, expected to be a <see cref="ModalReference"/>.</param>
    /// <param name="e">The event data.</param>
    private void ModalReference_Close(object? sender, EventArgs e)
    {
        if (sender is ModalReference modalReference)
        {
            CloseAndRemove(modalReference);
        }
    }

    /// <summary>
    /// Closes and removes the specified modal reference from the modal state.
    /// </summary>
    /// <param name="modalRef">The modal reference to close and remove.</param>
    private void CloseAndRemove(ModalReference modalRef)
    {
        modalRef.Close -= ModalReference_Close;
        CloseModal(modalRef);
    }

    public void CloseModal(IModalReference modalRef)
    {
        if (modalRef is ModalReference mr)
        {
            _modalState.RemoveModal(mr);
        }
    }

    /// <summary>
    /// Determines whether there are any open modal dialogs.
    /// </summary>
    /// <returns>True if there is at least one open modal dialog; otherwise, false.</returns>
    public bool HasOpenModals()
    {
        return _modalState.HasOpenModals();
    }
}
