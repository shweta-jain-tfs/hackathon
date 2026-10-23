using MediatR;
using Microsoft.EntityFrameworkCore;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Instrument.Application.Interfaces;

namespace Pharos.Server.Instrument.Application.Queries.GetInstrumentMethods;

internal class GetActiveInstrumentMethodsQueryHandler(IInstrumentDbContext dbContext) : IRequestHandler<GetActiveInstrumentMethodsQuery, HashSet<string>>
{
    public async Task<HashSet<string>> Handle(GetActiveInstrumentMethodsQuery request, CancellationToken cancellationToken)
    {
        var methodNames = await dbContext.InstrumentMethods
            .Where(x => x.Instrument.SerialNumber == request.InstrumentNumber && x.IsActive)
            .Select(x => x.MethodName)
            .ToHashSetAsync(cancellationToken)
            .ConfigureAwait(false);

        return methodNames;
    }
}
