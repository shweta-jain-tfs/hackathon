namespace Pharos.Common.Web.Components.Modal;
/// <summary>
/// Represents the result of a modal dialog.
/// </summary>
public class ModalResult
{
    /// <summary>
    /// Gets or sets the data returned from the modal dialog.
    /// </summary>
    public object? Data { get; set; }

    /// <summary>
    /// Gets a value indicating whether the modal dialog was canceled.
    /// </summary>
    public bool Canceled { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ModalResult"/> class.
    /// </summary>
    /// <param name="data">The data returned from the modal dialog.</param>
    /// <param name="canceled">A value indicating whether the modal dialog was canceled.</param>
    protected internal ModalResult(object? data, bool canceled)
    {
        Data = data;
        Canceled = canceled;
    }

    /// <summary>
    /// Creates a new <see cref="ModalResult"/> indicating a successful operation with no data.
    /// </summary>
    /// <returns>A new <see cref="ModalResult"/> instance.</returns>
    public static ModalResult Ok()
    {
        return new ModalResult(null, false);
    }

    /// <summary>
    /// Creates a new <see cref="ModalResult"/> indicating a successful operation with the specified data.
    /// </summary>
    /// <typeparam name="T">The type of the data.</typeparam>
    /// <param name="result">The data returned from the modal dialog.</param>
    /// <returns>A new <see cref="ModalResult"/> instance.</returns>
    public static ModalResult Ok<T>(T result)
    {
        return new ModalResult(result, false);
    }

    /// <summary>
    /// Creates a new <see cref="ModalResult"/> indicating that the modal dialog was canceled.
    /// </summary>
    /// <returns>A new <see cref="ModalResult"/> instance.</returns>
    public static ModalResult Cancel()
    {
        return new ModalResult(default, true);
    }
}