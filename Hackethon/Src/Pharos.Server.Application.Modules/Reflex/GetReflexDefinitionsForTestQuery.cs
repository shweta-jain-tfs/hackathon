using MediatR;
using Pharos.Server.Domain.Reflex;

namespace Pharos.Server.Application.Reflex;

/// <summary>
/// Gets active reflex definitions (including reflex tests) for a specific test based on the test name, method name, and optional requestor ID.
/// </summary>
/// <param name="TestName">The name of the test.</param>
/// <param name="MethodName">The name of the method used with the test.</param>
public record GetReflexDefinitionsForTestQuery(string TestName, string MethodName) : IRequest<List<IReflexDefinition>>;