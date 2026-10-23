using System.Globalization;
using System.Text;
using FluentValidation.Results;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Pharos.Server.Instrument.Application.Interfaces;

namespace Pharos.Server.Instrument.Infrastructure.Grpc.Interceptors;

internal class ExceptionInterceptor : Interceptor
{
    private readonly ICommunicationSessionManager _sessionManager;

    public ExceptionInterceptor(ICommunicationSessionManager sessionManager)
    {
        _sessionManager = sessionManager;
    }

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        try
        {
            return continuation(request, context);
        }
        catch (RpcException e)
        {
            if (e.StatusCode is StatusCode.Unknown or StatusCode.DeadlineExceeded)
            {
                // the exception we've received from the instrument is "fatal" for the session
                // we need to close it.
                var sessionId = context.Options.Headers?.Get(CommunicationSession.SessionIdHeader)?.Value;
                if (sessionId != null)
                {
                    var task = _sessionManager.StopSessionAsync(Guid.Parse(sessionId));
                    task.Wait();
                }
            }
            throw;
        }

    }

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            return await continuation(request, context).ConfigureAwait(false);
        }
        catch (System.ComponentModel.DataAnnotations.ValidationException e)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, e.Message));
        }
        catch (FluentValidation.ValidationException e)
        {
            throw HandleFluentValidationException(e);
        }
    }

    private static RpcException HandleFluentValidationException(FluentValidation.ValidationException exception)
    {
        var failures = exception.Errors.ToList();
        var error = failures.Count > 0 ? GetErrorMessages(failures) : exception.Message;

        return new RpcException(new Status(StatusCode.InvalidArgument, error));
    }

    private static string GetErrorMessages(IEnumerable<ValidationFailure> failures)
    {
        var errors = failures
                     .Select(GetErrorMessage)
                     .ToList();

        return string.Join(Environment.NewLine, errors);
    }

    private static string GetErrorMessage(ValidationFailure failure)
    {
        var message = new StringBuilder();
        message.Append(CultureInfo.InvariantCulture, $"Property {failure.PropertyName} failed validation.");

        if (!string.IsNullOrEmpty(failure.ErrorCode))
        {
            message.Append(CultureInfo.InvariantCulture, $" Error code was: {failure.ErrorCode}");
        }

        if (!string.IsNullOrEmpty(failure.ErrorMessage))
        {
            message.Append(CultureInfo.InvariantCulture, $" Error was: {failure.ErrorMessage}");
        }

        return message.ToString();
    }
}