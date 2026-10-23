using MediatR;

namespace Pharos.Server.Application.Parameters;

public record GetTestResultInformationQuery(string TestName, string MethodName) : IRequest<ParameterInfo>;