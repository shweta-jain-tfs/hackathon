using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Server.Parameters.Application.Interfaces;
using Pharos.Server.Result.Application.Interfaces;

namespace Pharos.Server.Integration.Tests.CalibratorCurveIntegration;

internal sealed class CalibrationTestSession : IAsyncDisposable
{
    private CalibrationTestSession(AsyncServiceScope scope)
    {
        Scope = scope;
        Mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        ResultDbContext = scope.ServiceProvider.GetRequiredService<IResultDbContext>();
        ParametersDbContext = scope.ServiceProvider.GetRequiredService<IParametersDbContext>();
    }

    internal AsyncServiceScope Scope { get; }
    internal IMediator Mediator { get; }
    internal IResultDbContext ResultDbContext { get; }
    internal IParametersDbContext ParametersDbContext { get; }

    internal static CalibrationTestSession Create(IServiceProvider serviceProvider)
        => new(serviceProvider.CreateAsyncScope());

    public ValueTask DisposeAsync() => Scope.DisposeAsync();
}
