using MediatR;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Reagents;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Queries.QualityControl;

internal class GetQualityControlLotRangeQueryHandler(
    IParametersDbContext dbContext,
    IReagentBarcodeService barcodeService) : IRequestHandler<GetQualityControlLotRangeQuery, QualityControlLotRangeDto?>
{
    public async Task<QualityControlLotRangeDto?> Handle(GetQualityControlLotRangeQuery request, CancellationToken cancellationToken)
    {
        string? lotNumber = null;

        if (!string.IsNullOrEmpty(request.QualityControlBarcode))
        {
            lotNumber = barcodeService.GetReagentLotInformation(ArticleType.QualityControlCartridge, request.QualityControlBarcode)?.LotNumber;
        }

        var result = await dbContext.MethodQualityControlLot
            .Where(l => l.LotNumber == lotNumber &&
                        l.QualityControlTest.Test.Method.MethodName == request.MethodName &&
                        l.QualityControlTest.Test.TestName == request.TestName &&
                        l.QualityControlTest.QualityControl.Name == request.QualityControlName)
            .Select(l => l.LotType == LotType.UseDefaultQcValues // Use default QC values from QualityControlTest
                ? new QualityControlLotRangeDto
                {
                    QualityControlType = l.QualityControlTest.QualityControlType,
                    ExpectedConcentration = null,
                    //ExpectedConcentration = l.QualityControlTest.LotExpectedConcentration,
                    //ExpectedStdDev = l.QualityControlTest.LotExpectedStdDev,
                    ExpectedStdDev = null,
                    StdDevLimit = null,
                    //StdDevLimit = l.QualityControlTest.LotStdDevLimit,
                    RangeMin = null,
                    RangeMax = null,
                    //RangeMin = l.QualityControlTest.LotRangeMin,
                    //RangeMax = l.QualityControlTest.LotRangeMax,
                    ExcludeRangeMax = false,
                    ExcludeRangeMin = false
                }
                : new QualityControlLotRangeDto
                {
                    QualityControlType = l.QualityControlTest.QualityControlType,
                    ExpectedConcentration = l.ExpectedConcentration,
                    ExpectedStdDev = l.ExpectedStdDev,
                    StdDevLimit = l.StdDevLimit,
                    RangeMin = l.RangeMin,
                    RangeMax = l.RangeMax,
                    ExcludeRangeMax = l.ExcludeRangeMax,
                    ExcludeRangeMin = l.ExcludeRangeMin
                })
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (result != null &&
            (result is { ExpectedConcentration: not null, ExpectedStdDev: not null, StdDevLimit: not null } ||
             result.RangeMin.HasValue || result.RangeMax.HasValue))
        {
            return result;
        }

        return await GetDefaultQualityControlRange(request, cancellationToken);
    }

    private async Task<QualityControlLotRangeDto?> GetDefaultQualityControlRange(GetQualityControlLotRangeQuery request, CancellationToken cancellationToken)
    {
        var result = await dbContext.PhadiaQualityControlTests
             .Where(qc => qc.Test.Method.MethodName == request.MethodName &&
                         qc.Test.TestName == request.TestName &&
                         qc.QualityControl.Name == request.QualityControlName)
             .Select(qc => new QualityControlLotRangeDto
             {
                 QualityControlType = qc.QualityControlType,
                 //ExpectedConcentration = qc.LotExpectedConcentration,
                 ExpectedConcentration = null,
                 ExpectedStdDev = null,
                 //ExpectedStdDev = qc.LotExpectedStdDev,
                 StdDevLimit = null,
                 //StdDevLimit = qc.LotStdDevLimit,
                 RangeMin = null,
                 RangeMax = null,
                 //RangeMin = qc.LotRangeMin,
                 //RangeMax = qc.LotRangeMax,
                 ExcludeRangeMax = false,
                 ExcludeRangeMin = false
             })
             .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return result;
    }
}