using MediatR;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Parameters.Application.Interfaces;
using Tetris.Protocol.Instrument;

namespace Pharos.Server.Parameters.Application.Queries.GetQualityControls;

internal class GetQualityControlSchedulingQueryHandler : IRequestHandler<GetQualityControlSchedulingQuery, QcSchedulingRequest>
{
    private readonly IParametersDbContext _dbContext;

    public GetQualityControlSchedulingQueryHandler(IParametersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<QcSchedulingRequest> Handle(GetQualityControlSchedulingQuery request, CancellationToken cancellationToken)
    {
        var items = await _dbContext.QualityControlSchedules
            .Include(x => x.QualityControlTest)
            .ThenInclude(x => x.Test)
            .ThenInclude(x => x.Method)
            .Include(x => x.QualityControlTest)
            .ThenInclude(x => x.QualityControl)
            .Where(x => x.QualityControlTest.Enabled && x.QualityControlTest.Test.Active && x.QualityControlTest.Test.Method.Enabled)
            .AsSplitQuery()
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var schedulingRequest = new QcSchedulingRequest();

        foreach (var item in items)
        {
            schedulingRequest.QualityControls.Add(new QualityControlDefinition
            {
                QualityControlName = item.QualityControlTest.QualityControl.Name,
                MethodName = item.QualityControlTest.Test.Method.MethodName,
                TestName = item.QualityControlTest.Test.TestName,
                FirstInAnalyticalRun = item.FirstInRun,
                LastInAnalyticalRun = item.LastInRun,
                PeriodicQcType = Map(item.Periodically),
                IntervalBetweenPeriodicQC = item.RunEvery,
                OffsetForFirstPeriodicQc = item.StartAfter,
                InstrumentDilutionFactor = item.QualityControlTest.InstrumentDilutionFactor,
                NumberOfReplicates = item.QualityControlTest.Replicates,
                PreDilutionFactor = item.QualityControlTest.PreDilutionFactor,
                StopDispensingTestsIfQcNotAvailable = false, //TODO
                AllowedLotNumbers = { } //TODO
            });
        }

        return schedulingRequest;
    }

    private static EnumPeriodicQcIntervalType Map(Periodically periodically)
    {
        return periodically switch
        {
            Periodically.No => EnumPeriodicQcIntervalType.None,
            Periodically.YesByNumberOfTests => EnumPeriodicQcIntervalType.TestsMethod,
            Periodically.YesByTime => EnumPeriodicQcIntervalType.TimeInterval,
            _ => throw new ArgumentOutOfRangeException(nameof(periodically), periodically, null)
        };
    }
}