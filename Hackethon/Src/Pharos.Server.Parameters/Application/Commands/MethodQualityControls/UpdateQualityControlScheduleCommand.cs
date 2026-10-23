using MediatR;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Commands.MethodQualityControls;

public record UpdateQualityControlScheduleCommand(int Id, bool FirstInRun, bool LastInRun, bool AfterCalibration, Periodically Periodically, int StartAfter, int RunEvery) : IRequest;

internal class UpdateQualityControlScheduleCommandHandler : IRequestHandler<UpdateQualityControlScheduleCommand>
{
    private readonly IParametersDbContext _context;

    public UpdateQualityControlScheduleCommandHandler(IParametersDbContext context)
    {
        _context = context;
    }

    public async Task Handle(UpdateQualityControlScheduleCommand request, CancellationToken cancellationToken)
    {
        var schedule = await _context.QualityControlSchedules
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (schedule == null)
        {
            throw new NotFoundException();
        }

        schedule.FirstInRun = request.FirstInRun;
        schedule.LastInRun = request.LastInRun;
        schedule.AfterCalibration = request.AfterCalibration;
        schedule.Periodically = request.Periodically;
        schedule.StartAfter = request.StartAfter;
        schedule.RunEvery = request.RunEvery;

        if(schedule.Periodically == Periodically.No)
        {
            //reset to default values
            schedule.StartAfter = 0;
            schedule.RunEvery = 1;
        }

        schedule.AddDomainEvent(new QualityControlScheduleUpdatedEvent());

        await _context.SaveChangesAsync(cancellationToken);
    }
}