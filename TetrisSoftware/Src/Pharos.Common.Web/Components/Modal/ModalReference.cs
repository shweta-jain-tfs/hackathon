namespace Pharos.Common.Web.Components.Modal;
/// <summary>
/// Represents a reference to a modal dialog.
/// </summary>
public class ModalReference : IModalReference
{
    private readonly TaskCompletionSource<ModalResult> _resultCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Action<ModalResult> _closed;
    public event EventHandler? Close;

    /// <summary>
    /// Gets the unique identifier of the modal instance.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the render fragment representing the modal instance.
    /// </summary>
    internal RenderFragment ModalInstance { get; }

    /// <summary>
    /// Gets a value indicating whether the modal should close on navigation.
    /// Default is true.
    /// </summary>
    internal bool CloseModalOnNavigation { get; }

    /// <summary>
    /// Gets the type of the component displayed in the modal.
    /// </summary>
    internal Type ContentComponent { get; }

    /// <summary>
    /// Gets the parameters passed to the modal component.
    /// </summary>
    internal ModalParameters Parameters { get; }

    /// <summary>
    /// Gets the optional tag for grouping related modals.
    /// </summary>
    public string? Tag { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ModalReference"/> class.
    /// </summary>
    /// <param name="modalInstanceId">The unique identifier of the modal instance.</param>
    /// <param name="modalInstance">The render fragment representing the modal instance.</param>
    /// <param name="closeModalOnNavigation">A value indicating whether the modal should close on navigation.</param>
    /// <param name="contentComponent">The type of the component displayed in the modal.</param>
    /// <param name="parameters">The parameters passed to the modal component.</param>
    /// <param name="tag">An optional tag for grouping related modals.</param>
    public ModalReference(Guid modalInstanceId, RenderFragment modalInstance, bool closeModalOnNavigation, Type contentComponent, ModalParameters parameters, string? tag = null)
    {
        Id = modalInstanceId;
        _closed = HandleClosed;
        ModalInstance = modalInstance;
        CloseModalOnNavigation = closeModalOnNavigation;
        ContentComponent = contentComponent;
        Parameters = parameters;
        Tag = tag;
    }

    /// <summary>
    /// Closes the modal dialog.
    /// </summary>
    public void CloseHandler()
    {
        Close?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Gets the task that represents the result of the modal dialog.
    /// </summary>
    public Task<ModalResult> Result => _resultCompletion.Task;

    /// <summary>
    /// Dismisses the modal dialog with the specified result.
    /// </summary>
    /// <param name="result">The result to return when dismissing the modal dialog.</param>
    public void Dismiss(ModalResult result)
    {
        _closed.Invoke(result);
    }

    /// <summary>
    /// Checks if the provided parameters match the modal's parameters.
    /// </summary>
    /// <param name="parameters">The parameters to compare.</param>
    /// <returns>True if the parameters match; otherwise, false.</returns>
    internal bool ParametersMatch(ModalParameters parameters)
    {
        var thisParams = Parameters._parameters;
        var otherParams = parameters._parameters;

        if (thisParams.Count != otherParams.Count)
        {
            return false;
        }

        foreach (var (key, value) in thisParams)
        {
            if (!otherParams.TryGetValue(key, out var otherValue))
            {
                return false;
            }

            if (!Equals(value, otherValue))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Handles the closing of the modal dialog.
    /// </summary>
    /// <param name="obj">The result of the modal dialog.</param>
    private void HandleClosed(ModalResult obj)
    {
        _ = _resultCompletion.TrySetResult(obj);
    }
}
