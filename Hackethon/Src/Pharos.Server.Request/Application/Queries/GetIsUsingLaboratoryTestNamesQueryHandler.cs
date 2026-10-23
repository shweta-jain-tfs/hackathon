using MediatR;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Request;
using Pharos.Server.Application.Settings.Request;

namespace Pharos.Server.Request.Application.Queries;

internal class GetIsUsingLaboratoryTestNamesQueryHandler : IRequestHandler<GetIsUsingLaboratoryTestNamesQuery, bool>
{
    private readonly RequestSettings _settings;

    public GetIsUsingLaboratoryTestNamesQueryHandler(IOptionsSnapshot<RequestSettings> settingsMonitor)
    {
        _settings = settingsMonitor.Value;
    }

    public Task<bool> Handle(GetIsUsingLaboratoryTestNamesQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_settings.UseLaboratoryTestNames);
    }
}