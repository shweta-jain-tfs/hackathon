using MediatR;
using Pharos.Server.Infrastructure;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Queries.QualityControl;

public record GetMethodQualityControlsOverviewQuery(List<string> QualityControlMethodNameList) : IRequest<List<MethodQualityControlsOverviewDto>>;

internal class GetMethodQualityControlsOverviewQueryHandler(IParametersDbContext context, IDateTime dateTime) : IRequestHandler<GetMethodQualityControlsOverviewQuery, List<MethodQualityControlsOverviewDto>>
{
    public async Task<List<MethodQualityControlsOverviewDto>> Handle(GetMethodQualityControlsOverviewQuery request, CancellationToken cancellationToken)
    {
        var qualityControlMethodNames = request.QualityControlMethodNameList;

        var query = context.QualityControlTests
            .Include(x => x.Test)
            .ThenInclude(x => x.Method)
            .Include(x => x.QualityControl)
            .Include(x => x.Lots)
            .AsNoTracking()
            .Where(qc => qc.Enabled && qc.Test.Method.Enabled);

        if (qualityControlMethodNames is { Count: > 0 })
        {
            qualityControlMethodNames = qualityControlMethodNames.Distinct().ToList();
            query = query.Where(qc => qualityControlMethodNames.Contains(qc.Test.Method.MethodName));
        }

        var methodQualityControls = await query
            .OrderBy(x => x.Test.Method.SortOrder ?? int.MaxValue)
            .ThenBy(x => x.Test.Method.MethodName)
            .ThenByDescending(x => x.SystemType)
            .ThenBy(x => x.QualityControl.Name)
            .ThenBy(x => x.Test.TestName)
            .SelectMany(qc => qc.Lots.Where(x => x.ExpirationDate == null || x.ExpirationDate.Value.Date >= dateTime.Now.Date).DefaultIfEmpty(), (qc, lot) => new
            {
                QualityControlTest = qc,
                Lot = lot
            })
            .Select(x => new MethodQualityControlsOverviewDto
            {
                Id = x.QualityControlTest.Id,
                MethodId = x.QualityControlTest.Test.MethodId,
                MethodName = x.QualityControlTest.Test.Method.MethodName,
                QcType = x.QualityControlTest.SystemType,
                QualityControlName = x.QualityControlTest.QualityControl.Name,
                TestName = x.QualityControlTest.Test.TestName,
                LotId = x.Lot != null ? x.Lot.Id : null,
                CurrentLot = x.Lot != null ? x.Lot.LotNumber : null,
                LimitDefinition = x.Lot != null ? x.Lot.LotType : null,
                CurrentIssue = null
            })
            .ToListAsync(cancellationToken);

        return methodQualityControls;
    }
}