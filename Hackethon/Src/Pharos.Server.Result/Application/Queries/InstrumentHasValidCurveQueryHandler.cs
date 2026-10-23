using MediatR;
using Pharos.Server.Application.Result;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Queries;

internal class InstrumentHasValidCurveQueryHandler(IResultDbContext dbContext) : IRequestHandler<InstrumentHasValidCurveQuery, bool>
{
    public async Task<bool> Handle(InstrumentHasValidCurveQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.InstrumentNumber))
        {
            throw new InvalidOperationException("Instrument number is required to query a curve.");
        }

        if (string.IsNullOrEmpty(request.MethodName))
        {
            throw new InvalidOperationException("Method name is required to query a curve.");
        }

        var query = dbContext.Curves
            .Where(x => x.InstrumentNumber == request.InstrumentNumber && x.MethodName == request.MethodName && x.Judgment == CurveJudgment.OK);

        if (!string.IsNullOrEmpty(request.ConjugateLot))
        {
            query = query.Where(c => c.TestResults!
                .Any(t => t.Replicates!
                    .Any(rep => rep.TestReplicateReagents!
                        .Any(r => r.ReagentType == ArticleType.ConjugateCartridge && r.LotNumber == request.ConjugateLot))));
        }
        if (!string.IsNullOrEmpty(request.CarrierLot))
        {
            query = query.Where(c => c.TestResults!
                .Any(t => t.Replicates!
                    .Any(rep => rep.TestReplicateReagents!
                        .Any(r => (r.ReagentType == ArticleType.ImmunoCapCarrier || r.ReagentType == ArticleType.EliaWellCarrier) && r.LotNumber == request.CarrierLot))));
        }

        if (!string.IsNullOrEmpty(request.CalibratorLot))
        {
            query = query.Where(c => c.TestResults!
                .Any(t => t.Replicates!
                    .Any(rep => rep.TestReplicateReagents!
                        .Any(r => r.ReagentType == ArticleType.CalibratorCartridge && r.LotNumber == request.CalibratorLot))));
        }

        var curveExists = await query.AnyAsync(cancellationToken);

        return curveExists;
    }
}
