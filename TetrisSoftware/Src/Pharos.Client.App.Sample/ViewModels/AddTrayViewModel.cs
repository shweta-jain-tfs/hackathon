using System.Net;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.Sample.Models;
using Pharos.Client.Common.App.Services;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.ViewModels.Interfaces;

namespace Pharos.Client.App.Sample.ViewModels;

public class AddTrayViewModel
{
    private readonly ISampleTrayDataService _sampleTrayDataService;
    private readonly IExceptionHandlerService _exceptionHandler;
    private readonly IStringLocalizer<Translations> _loc;

    public AddTrayViewModel(ISampleTrayDataService sampleTrayDataService, IExceptionHandlerService exceptionHandler, IStringLocalizer<Translations> loc)
    {
        _sampleTrayDataService = sampleTrayDataService;
        _exceptionHandler = exceptionHandler;
        _loc = loc;
    }

    public string TrayId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the server-side validation error message. Cleared when the input receives focus.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    public async Task<bool> CreateTrayAsync()
    {
        ErrorMessage = string.Empty;

        try
        {
            var sampleTray = new SampleTray { Id = TrayId };

            await _sampleTrayDataService.CreateAsync(sampleTray);

            return true;
        }
        catch (ApiException ex)
        {
            if (ex.StatusCode == (int)HttpStatusCode.BadRequest)
            {
                // Extract and display server-side validation error inline
                ErrorMessage = ExtractValidationErrorMessage(ex.GetProblemDetails());
            }
            else
            {
                _exceptionHandler.ShowApiException(ex);
            }
        }
        catch (Exception ex)
        {
            _exceptionHandler.ShowExceptionMessage(ex);
        }

        return false;
    }

    private string ExtractValidationErrorMessage(Common.App.Services.ProblemDetails? validationProblemDetails)
    {
        var errorMessage = _loc["TraysPage_AddDialog_ValidationError"];

        if (validationProblemDetails?.Errors is not { Count: > 0 })
        {
            return errorMessage;
        }

        // Return the first error message from the Errors dictionary
        var firstError = validationProblemDetails.Errors
            .SelectMany(kvp => kvp.Value)
            .FirstOrDefault();

        return firstError ?? errorMessage;
    }
}