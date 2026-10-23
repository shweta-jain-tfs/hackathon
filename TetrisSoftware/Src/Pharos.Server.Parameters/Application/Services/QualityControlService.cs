using Pharos.Server.Application.Parameters;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Services;

internal class QualityControlService(IParametersDbContext dbContext) : IQualityControlService
{
    public async Task<IReadOnlyList<PhadiaQualityControlDto>> GetPhadiaQualityControlTests(string articleNumber, CancellationToken cancellationToken)
    {
        return await (
            from a in dbContext.Articles.AsNoTracking()
            where a.ArticleNumber == articleNumber
            join qc in dbContext.PhadiaQualityControl.AsNoTracking()
                on a.Identity equals qc.Name
            join t in dbContext.PhadiaQualityControlTests.AsNoTracking()
                on qc.Id equals t.QualityControlId
            select new PhadiaQualityControlDto
            {
                Id = t.Id,
                TestName = t.Test.TestName,
                Enabled = t.Enabled
            }
        ).ToListAsync(cancellationToken);
    }

    public async Task<bool> CheckIfQcLotExistsForTest(int qualityControlId, string lotNumber, CancellationToken cancellationToken)
    {
        return await dbContext.MethodQualityControlLot.AsNoTracking()
            .AnyAsync(l => l.MethodQualityControlId == qualityControlId && l.LotNumber == lotNumber, cancellationToken);
    }
}