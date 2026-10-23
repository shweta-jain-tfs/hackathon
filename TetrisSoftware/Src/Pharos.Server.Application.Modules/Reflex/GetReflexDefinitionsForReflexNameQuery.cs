using MediatR;
using Pharos.Server.Domain.Reflex;

namespace Pharos.Server.Application.Reflex;

/// <summary>
/// Query to retrieve a reflex definition for a specific test, method, reflex name, and optional requestor.
/// Returns an <see cref="IReflexDefinition"/> with an empty ReflexTests list.
/// </summary>
/// <param name="ReflexName">The name of the reflex definition.</param>
/// <param name="TestName">The name of the test</param>
/// <param name="MethodName">The name of the method used with the test</param>
/// <param name="RequestorId">The optional requestor ID to filter by.</param>
public record GetReflexDefinitionsForReflexNameQuery(
    string ReflexName,
    string TestName,
    string MethodName,
    string? RequestorId
) : IRequest<IReflexDefinition?>;
