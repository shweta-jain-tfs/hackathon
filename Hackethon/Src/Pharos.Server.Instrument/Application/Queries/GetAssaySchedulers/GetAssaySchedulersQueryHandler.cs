using MediatR;
using Microsoft.EntityFrameworkCore;
using Pharos.Server.Instrument.Application.Interfaces;
using Pharos.Server.Instrument.Application.Mapping;

namespace Pharos.Server.Instrument.Application.Queries.GetAssaySchedulers;

internal record GetAssaySchedulersQuery : IRequest<AssaySchedulerDto[]>;

internal class GetAssaySchedulersQueryHandler(IInstrumentDbContext dbContext) : IRequestHandler<GetAssaySchedulersQuery, AssaySchedulerDto[]>
{
    public async Task<AssaySchedulerDto[]> Handle(GetAssaySchedulersQuery request, CancellationToken cancellationToken)
    {
        // Query the database for AssaySchedulers, including related data
        var assaySchedulerList = await dbContext.AssaySchedulers
            .Include(s => s.SchedulerDays)
            .ThenInclude(d => d.ScheduledMethods)
            .Include(s => s.Instrument)
            .ToListAsync(cancellationToken);

        var assaySchedulers = assaySchedulerList.Select(AssaySchedulerMapper.ToAssaySchedulerDto).ToArray();

        return assaySchedulers;
    }
}