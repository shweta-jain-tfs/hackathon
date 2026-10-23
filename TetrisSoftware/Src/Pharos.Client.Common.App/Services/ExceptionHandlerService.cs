using System.Net;
using System.Text;
using System.Text.Json;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.App.Views;
using Pharos.Client.Common.ViewModels.Interfaces;
using Pharos.Common.Web.Components.Modal;
using Pharos.Common.Web.Components.Toaster;

namespace Pharos.Client.Common.App.Services;


/// <summary>
/// Provides centralized exception handling and user notification services for Blazor applications.
/// Supports displaying exceptions through modals or toaster notifications based on exception type.
/// </summary>
internal class ExceptionHandlerService : IExceptionHandlerService
{
    // Cache JsonSerializerOptions instance
    internal static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IModalService _modal;
    private readonly IToasterService _toaster;
    private readonly IStringLocalizer<Translations> _loc;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExceptionHandlerService"/> class.
    /// </summary>
    /// <param name="modal">The modal service for displaying exception dialogs.</param>
    /// <param name="toaster">The toaster service for displaying exception notifications.</param>
    /// <param name="loc">The string localizer for translating exception messages.</param>
    public ExceptionHandlerService(IModalService modal, IToasterService toaster, IStringLocalizer<Translations> loc)
    {
        _modal = modal;
        _toaster = toaster;
        _loc = loc;
    }

    /// <summary>
    /// Single entry point for displaying any exception. Routes to appropriate handler based on exception type.
    /// </summary>
    /// <param name="exception">The exception to handle.</param>
    /// <param name="displayMode">How to display the exception (modal or toast). Defaults to modal.</param>
    public void ShowExceptionMessage(Exception exception, ExceptionDisplayMode displayMode = ExceptionDisplayMode.Modal)
    {
        switch (exception)
        {
            case ApiException apiEx:
                ShowApiException(apiEx, displayMode);
                break;
            case HttpRequestException:
                ShowHttpRequestException();
                break;
            default:
                ShowGeneralException(exception, displayMode);
                break;
        }
    }

    /// <summary>
    /// Displays an ApiException.
    /// </summary>
    /// <param name="exception">The API exception to handle.</param>
    /// <param name="displayMode">How to display the exception (modal or toaster). Defaults to modal.</param>
    public void ShowApiException(ApiException exception, ExceptionDisplayMode displayMode = ExceptionDisplayMode.Modal)
    {

        var problemDetails = exception.GetProblemDetails();

        string? errorMessage = null;
        if (!string.IsNullOrWhiteSpace(problemDetails?.Detail))
        {
            errorMessage = problemDetails.Detail.Trim();
        }

        LocalizedString? title;
        switch ((HttpStatusCode)exception.StatusCode)
        {
            case HttpStatusCode.BadRequest:
                title = _loc[nameof(Translations.ExceptionHandlerService_ValidationErrorTitle)];
                if (problemDetails?.Errors is { Count: > 0 })
                {
                    errorMessage = CombineErrorsToString(problemDetails);
                }
                else
                {
                    errorMessage ??= _loc[nameof(Translations.ExceptionHandlerService_ValidationErrorDefaultMessage)];
                }
                break;

            case HttpStatusCode.Forbidden:
                title = _loc[nameof(Translations.ExceptionHandlerService_AccessDeniedTitle)];
                errorMessage ??= _loc[nameof(Translations.ExceptionHandlerService_AccessDeniedDefaultMessage)];
                break;

            case HttpStatusCode.NotFound:
            case HttpStatusCode.Gone:
                title = _loc[nameof(Translations.ExceptionHandlerService_ResourceNotFoundTitle)];
                errorMessage ??= _loc[nameof(Translations.ExceptionHandlerService_ResourceNotFoundDefaultMessage)];
                break;

            case HttpStatusCode.Conflict:
                title = _loc[nameof(Translations.ExceptionHandlerService_ConflictTitle)];
                errorMessage ??= _loc[nameof(Translations.ExceptionHandlerService_ConflictDefaultMessage)];
                break;

            default:
                title = _loc[nameof(Translations.ExceptionHandlerService_GeneralErrorTitle)];
                errorMessage ??= _loc[nameof(Translations.ExceptionHandlerService_UnexpectedErrorOccurred)];
                break;
        }

        DisplayException(title, errorMessage, displayMode);
    }

    /// <summary>
    /// Handles HttpRequestException. This exception is thrown for network issues (DNS failure, connection refused,
    /// TLS, fetch blocked in Blazor WASM), malformed responses, or protocol violations.
    /// </summary>
    private void ShowHttpRequestException()
    {
        // We don't want to show the exception details, they are not useful for end users (to low-level).
        var modalTitle = _loc[nameof(Translations.ExceptionHandlerService_GeneralErrorTitle)];
        var errorMessage = _loc[nameof(Translations.ExceptionHandlerService_FailedToSendRequest)];

        // This exception will trigger "connection lost" modal. We don't show two dialogs for the same error.
        DisplayException(modalTitle, errorMessage, ExceptionDisplayMode.Toast);
    }


    /// <summary>
    /// Handles general exceptions (non-API).
    /// Can be called directly from try-catch blocks.
    /// </summary>
    /// <param name="generalException">The general exception to handle.</param>
    /// <param name="displayMode">How to display the exception (modal or toaster). Defaults to modal.</param>
    private void ShowGeneralException(Exception generalException, ExceptionDisplayMode displayMode = ExceptionDisplayMode.Modal)
    {
        ExceptionContent? exceptionContent = null;

        if (!string.IsNullOrWhiteSpace(generalException.Message))
        {
            try
            {
                exceptionContent = ExceptionDetailsParser.FromExceptionMessage(generalException.Message);
            }
            catch
            {
                // Any other exception ignored; fallback below.
            }
            if (exceptionContent == null)
            {
                // Fallback: use first the default title, and the first line as detail
                var lines = generalException.Message.Split('\n')
                    .Select(l => l.Trim())
                    .Where(l => !string.IsNullOrWhiteSpace(l))
                    .ToList();

                exceptionContent = new ExceptionContent(null, lines[0]);
            }
        }

        var modalTitle = exceptionContent?.Title;
        var errorMessage = exceptionContent?.Detail;

        if (string.IsNullOrWhiteSpace(modalTitle))
        {
            modalTitle = _loc[nameof(Translations.ExceptionHandlerService_GeneralErrorTitle)];
        }
        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            errorMessage = _loc[nameof(Translations.ExceptionHandlerService_UnexpectedErrorOccurred)];
        }

        DisplayException(modalTitle, errorMessage, displayMode);
    }

    /// <summary>
    /// Displays the exception using the specified display mode.
    /// </summary>
    /// <param name="title">The error title.</param>
    /// <param name="message">The error message.</param>
    /// <param name="displayMode">How to display the exception (modal or toaster).</param>
    private void DisplayException(string title, string message, ExceptionDisplayMode displayMode)
    {
        switch (displayMode)
        {
            case ExceptionDisplayMode.Modal:
                ShowModal(title, message);
                break;
            case ExceptionDisplayMode.ModalKeepOnNavigation:
                ShowModal(title, message, false);
                break;
            case ExceptionDisplayMode.Toast:
                ShowToaster(title, message);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(displayMode), displayMode, null);
        }
    }

    /// <summary>
    /// Shows the exception in a modal dialog.
    /// </summary>
    /// <param name="title">The title to display in the modal header.</param>
    /// <param name="message">The error message to display in the modal body.</param>
    /// <param name="closeOnNavigation">Whether to close the modal on navigation.</param>
    private void ShowModal(string title, string message, bool closeOnNavigation = true)
    {
        var closeButtonText = _loc.GetString("Validation_CloseButtonText").Value;

        _modal.Show<ExceptionHandlerServiceModalView>(new ModalOptions
        {
            Title = title,
            ModalHeight = ModalSize.Small,
            ModalWidth = ModalSize.Small,
            Status = ModalStatus.Warning,
            CloseOnNavigation = closeOnNavigation
        },
            new ModalParameters
        {
            { nameof(ExceptionHandlerServiceModalView.Message), message },
            { nameof(ExceptionHandlerServiceModalView.OkButton), closeButtonText }
        });
    }

    /// <summary>
    /// Shows the exception as a toaster notification.
    /// </summary>
    /// <param name="title">The title of the toaster notification.</param>
    /// <param name="message">The error message to display in the toaster.</param>
    private void ShowToaster(string title, string message)
    {
        // Combine title and message for toaster display
        var toasterMessage = !string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(message)
            ? $"{title}: {message}"
            : !string.IsNullOrWhiteSpace(title) ? title : message;

        _toaster.ShowToast(ToastType.Warning, toasterMessage);
    }

    /// <summary>
    /// Combines all validation errors from a <see cref="ProblemDetails"/> object into a single formatted string.
    /// </summary>
    /// <param name="validationProblemDetails">The validation problem details containing the errors.</param>
    /// <returns>A formatted string containing all validation error messages separated by new lines.</returns>
    private static string CombineErrorsToString(ProblemDetails validationProblemDetails)
    {
        StringBuilder output = new();

        output.Append(string.Join("\r\n", validationProblemDetails.Errors.SelectMany(x => x.Value)));
        output.Append(Environment.NewLine);

        return output.ToString();
    }
}