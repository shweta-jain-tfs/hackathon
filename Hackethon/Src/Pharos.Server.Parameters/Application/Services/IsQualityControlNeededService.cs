using MediatR;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Services;

internal class IsQualityControlNeededService : IIsQualityControlNeededService
{
    private readonly ISender _sender;
    public IsQualityControlNeededService(ISender sender)
    {
        _sender = sender;
    }

    public async Task<QualityControlNeededDto> IsQualityControlNeeded(HashSet<string> methodNames, string instrumentNumber, CancellationToken cancellationToken)
    {
        var needed = await IsNeeded(methodNames, instrumentNumber, cancellationToken);
        return new QualityControlNeededDto(needed, 1); //TODO, for Phadia QC we currently do not have tube types, so we return 1 as default. This should be adjusted once tube types are implemented for Phadia QC.
    }

    private async Task<bool> IsNeeded(HashSet<string> methodNames, string instrumentNumber, CancellationToken cancellationToken)
    {
        var activeMethods = await _sender.Send(new GetActiveInstrumentMethodsQuery(instrumentNumber), cancellationToken);
        return methodNames.Overlaps(activeMethods);
    }

    public async Task<QualityControlNeededDto> IsQualityControlNeeded(LabQualityControl labQualityControl, string instrumentNumber, CancellationToken cancellationToken)
    {
        var methodNames = labQualityControl.Tests.Select(x => x.Test.Method.MethodName).ToHashSet();
        var needed = await IsNeeded(methodNames, instrumentNumber, cancellationToken);
        return new QualityControlNeededDto(needed, labQualityControl.TubeTypeId);
    }
}

internal record QualityControlNeededDto(bool IsNeeded, int TubeTypeId);