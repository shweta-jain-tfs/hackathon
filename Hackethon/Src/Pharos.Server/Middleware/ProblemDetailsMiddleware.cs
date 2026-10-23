using System.Text;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Pharos.Server.Application.Exceptions;
using Pharos.Server.Domain.Exceptions;

namespace Pharos.Server.Middleware;

public static class ProblemDetailsMiddleware
{
    public static async Task HandleHttpException(HttpContext context)
    {
        var isDevelopment = context.RequestServices.GetService<IHostEnvironment>()?.IsDevelopment() ?? false;

        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

        // Prepare the ProblemDetails response.
        // NOTE that ProblemDetails.Title SHOULD NOT change from occurrence to occurrence
        // of the problem, except for purposes of localization (see[RFC7231], Section 3.4).
        ProblemDetails problemDetails;
        switch (exception)
        {
            case FluentValidation.ValidationException fluentEx:
                Dictionary<string, string[]> errors;
                if (fluentEx.Errors.Any())
                {
                    errors = fluentEx.Errors
                        .GroupBy(e => e.PropertyName ?? string.Empty, e => e.ErrorMessage)
                        .ToDictionary(failureGroup => failureGroup.Key, failureGroup =>
                        {
                            if (!isDevelopment)
                            {
                                return [];
                            }
                            return failureGroup.ToArray();
                        });
                }
                else
                {
                    // for Validation exceptions that do not have error collection we add one error with an empty key
                    errors = new Dictionary<string, string[]> { { "", [fluentEx.Message] } };
                }

                problemDetails = new ValidationProblemDetails(errors)
                {
                    Title = "One or more validation errors occurred.",
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Status = StatusCodes.Status400BadRequest
                };
                break;

            case System.ComponentModel.DataAnnotations.ValidationException systemValEx:
                // for Validation exceptions that do not have property names we add one error with an empty key
                problemDetails = new ValidationProblemDetails(new Dictionary<string, string[]> { { "", [systemValEx.Message] } })
                {
                    Title = "One or more validation errors occurred.",
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Status = StatusCodes.Status400BadRequest
                };
                break;

            case NotFoundException:
                problemDetails = CreateNotFound();
                break;

            case UnauthorizedAccessException:
                problemDetails = new ProblemDetails
                {
                    Title = "Unauthorized",
                    Type = "https://tools.ietf.org/html/rfc7235#section-3.1",
                    Status = StatusCodes.Status401Unauthorized
                };
                break;

            case NotAllowedException nae:
                problemDetails = new ProblemDetails
                {
                    Title = "This action is not allowed.",
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                    Status = StatusCodes.Status403Forbidden,
                    Detail = nae.Message
                };
                break;

            case ConflictException ce:
                problemDetails = new ProblemDetails
                {
                    Title = "A conflict occurred.",
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                    Status = StatusCodes.Status409Conflict,
                    Detail = ce.Message
                };
                break;


            case ObjectGoneException oge:
                problemDetails = new ProblemDetails
                {
                    Title = "The requested resource is no longer available.",
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.9",
                    Status = StatusCodes.Status410Gone,
                    Detail = oge.Message
                };
                break;

            default:
                problemDetails = new ProblemDetails
                {
                    Title = "Internal server error",
                    Type = "https://www.rfc-editor.org/rfc/rfc7231#section-6.6.1",
                    Status = StatusCodes.Status500InternalServerError
                };
                break;
        }

        // Add exception details in development mode
        if (exception != null)
        {
            if (isDevelopment)
            {
                var exceptionDetails = new StringBuilder();
                exceptionDetails.AppendLine(exception.Message);
                exceptionDetails.AppendLine(exception.StackTrace);
                problemDetails.Detail = exceptionDetails.ToString();
            }
        }

        await context.WriteProblemDetailsAsync(problemDetails);
    }

    public static ProblemDetails CreateNotFound()
    {
        var problemDetails = new ProblemDetails
        {
            Title = "The specified resource was not found.",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            Status = StatusCodes.Status404NotFound
        };

        return problemDetails;
    }

    public static async Task WriteProblemDetailsAsync(this HttpContext context, ProblemDetails problemDetails)
    {

        context.Response.StatusCode = (int)problemDetails.Status!;

        // Write the problem details to the response
        if (context.RequestServices.GetService<IProblemDetailsService>() is { } problemDetailsService)
        {
            await problemDetailsService.WriteAsync(new ProblemDetailsContext
            {
                HttpContext = context,
                ProblemDetails = problemDetails
            });
        }
    }
}