using Pharos.Client.Common.Models;

namespace Pharos.Client.Common.ViewModels.Interfaces;

/// <summary>
/// Specifies how an exception should be displayed to the user.
/// </summary>
public enum ExceptionDisplayMode
{
    /// <summary>
    /// Display the exception in a modal dialog.
    /// </summary>
    Modal,

    /// <summary>
    /// Display the exception in a modal dialog. The modal will be kept open on navigation,
    /// allowing the user to see the exception even after navigating to a different page.
    /// </summary>
    ModalKeepOnNavigation,

    /// <summary>
    /// Display the exception as a toast notification. Toast will be shown briefly and then disappear automatically.
    /// </summary>
    Toast
}

public interface IExceptionHandlerService
{
    void ShowExceptionMessage(Exception exception, ExceptionDisplayMode displayMode = ExceptionDisplayMode.Modal);
    void ShowApiException(ApiException exception, ExceptionDisplayMode displayMode = ExceptionDisplayMode.Modal);
}
