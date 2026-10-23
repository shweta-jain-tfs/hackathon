# Common App Project

## Exception handling

Inject ExceptionHandling Service to you page, view, view model, or data source/service.
In you try catch exception block call ExceptionHandling.ShowException(ex).
ShowException will parse the information from the exception if it is a known exception type.
As of 25 11 17 we support the following exception types:
* ApiException<ValidationProblemDetails>
* ApiException
* Exceptions

### Exceptions can be shown as either a toast notification or a modal.

You can allso use the ExceptionHandling service to create known exceptions directly.
HandleValidationException(validationEx, displayMode);
* takes in a ValidationProblemDetails exception and display mode (toast or modal)

HandleApiException(apiEx, displayMode);
* takes in an ApiException and display mode (toast or modal)

HandleGeneralException(exception, displayMode);
* takes in a general exception and display mode (toast or modal)

### Display modes
Modal - shows a modal dialog with the exception information

ModalShowOnNavigation - shows a modal dialog with the exception information after navigation completes. Otherwise the modal will not show.

Toast - shows a toast notification with the exception information