using MediatR;
using Microsoft.EntityFrameworkCore;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Instrument.Application.Interfaces;
using Pharos.Server.Instrument.Application.Mapping;

namespace Pharos.Server.Instrument.Application.Queries.GetAssaySchedulers;

internal record GetAssaySchedulerQuery(int SchedulerId) : IRequest<AssaySchedulerDto>;

internal class GetAssaySchedulerQueryHandler(IInstrumentDbContext dbContext)
    : IRequestHandler<GetAssaySchedulerQuery, AssaySchedulerDto>
{
    public Task<AssaySchedulerDto> Handle(GetAssaySchedulerQuery request, CancellationToken cancellationToken)
    {
        var assayScheduler = dbContext.AssaySchedulers
            .Include(s => s.SchedulerDays)
            .ThenInclude(d => d.ScheduledMethods)
            .Include(s => s.Instrument)
            .FirstOrDefault(x => x.Id == request.SchedulerId);

        if (assayScheduler is null)
        {
            throw new NotFoundException();
        }

        var dto = assayScheduler.ToAssaySchedulerDto();
        return Task.FromResult(dto);
    }
}