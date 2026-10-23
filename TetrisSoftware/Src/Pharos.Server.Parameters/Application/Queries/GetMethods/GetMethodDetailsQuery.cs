using MediatR;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Queries.GetMethods;

public record GetMethodDetailsQuery(int MethodId) : IRequest<MethodDetailsDto>;

internal sealed class GetMethodDetailsQueryHandler(IParametersDbContext context) : IRequestHandler<GetMethodDetailsQuery, MethodDetailsDto>
{
    public async Task<MethodDetailsDto> Handle(GetMethodDetailsQuery request, CancellationToken cancellationToken)
    {
        var methodDetailsDto = await context.Methods
            .Where(s => s.Id == request.MethodId)
            .AsNoTracking()
            .AsSplitQuery()
            .Select(s => new MethodDetailsDto
            {
                Id = request.MethodId,
                Enabled = s.Enabled,
                MethodName = s.MethodName,
                FullMethodName = s.FullMethodName,
                IsRuo = s.IsRuo,
                MethodVersion = s.MethodVersion,
                Technology = s.Technology,
                MethodNameLis = s.MethodNameLis ?? s.MethodName,
                DefaultLisChannelId = s.DefaultLisChannelId,
                AllowMonthlyCalibration = s.AnalyticalRun.AllowMonthlyCalibration,

                ClassTables = s.ClassTables
                    .GroupBy(x => x.ClassTableId)
                    .Where(g => s.ClassTableBelows.Any(b =>
                        b.ClassTableId == g.Key &&
                        !string.IsNullOrEmpty(b.UserTextBelowConcentration ?? b.DefaultTextBelowConcentration)))
                    .OrderBy(g => g.Key)
                    .Select(g => new MethodClassTableDto
                    {
                        ClassTableId = g.Key,
                        TextBelow = s.ClassTableBelows
                            .Where(b => b.ClassTableId == g.Key)
                            .Select(b => b.UserTextBelowConcentration ?? b.DefaultTextBelowConcentration)
                            .FirstOrDefault()!,
                        ClassTableRow = g
                            .OrderBy(x => x.ClassTableRowId)
                            .Select(x => new ClassTableRowInfoDto
                            {
                                Concentration = x.Concentration,
                                ClassTableRowId = x.ClassTableRowId,
                                TextAbove = x.TextAboveConcentration
                            }).ToList()
                    }).ToList(),

                // Map CurveControls directly in SQL
                CurveControls = s.CurveControls.Select(x => new MethodCurveControlDto
                {
                    CurveControlName = x.CurveControlName,
                    MethodTestName = x.MethodTest.TestName,
                    Replicates = x.Replicates,
                    ExpectedConcentration = x.ExpectedConcentration,
                    ExpectedStdDev = x.ExpectedStdDev,
                    InnerStdDevLimit = x.InnerStdDevLimit,
                    OuterStdDevLimit = x.OuterStdDevLimit
                }).ToList(),

                // Map CalibratorCurve directly in SQL
                CalibratorCurve = new MethodCalibratorCurveDto
                {
                    QuotientIndex = s.CalibrationCurve.QuotientIndex,
                    MaxNumberOfDays = s.CalibrationCurve.MaxNumberOfDays,
                    DefaultMaxNumberOfDays = s.CalibrationCurve.DefaultMaxNumberOfDays,
                    TestName = s.CalibrationCurve.MethodTest.TestName,
                    MethodCalibrationPoint = s.CalibrationCurve.CalibrationPoints.Select(x => new MethodCalibrationPointDto
                    {
                        CalibratorName = x.CalibratorName,
                        ExpectedConcentration = x.ExpectedConcentration,
                        Replicates = x.Replicates,
                        IdealResponse = x.IdealResponse,
                        ExpectedStdDev = x.ExpectedStdDev,
                        UpperResponseLimit = x.UpperResponseLimit,
                        LowerResponseLimit = x.LowerResponseLimit,
                        StdDevLimit = x.StdDevLimit,
                        StdDevPrecisionLimit = x.StdDevPrecisionLimit,
                        UpperLogarithmicLimit = x.UpperLogarithmicLimit,
                        LowerLogarithmicLimit = x.LowerLogarithmicLimit
                    }).ToList()
                },

                Report = new MethodReportingDto
                {
                    TextAboveCutOff = s.Reporting.TextAboveCutOff,
                    DefaultTextAboveCutOff = s.Reporting.DefaultTextAboveCutOff,
                    TextBelowCutOff = s.Reporting.TextBelowCutOff,
                    DefaultTextBelowCutOff = s.Reporting.DefaultTextBelowCutOff,
                    TextQuantitativeAbove = s.Reporting.TextQuantitativeAbove,
                    DefaultTextQuantitativeAbove = s.Reporting.DefaultTextQuantitativeAbove,
                    TextQuantitativeBelow = s.Reporting.TextQuantitativeBelow,
                    DefaultTextQuantitativeBelow = s.Reporting.DefaultTextQuantitativeBelow,
                    TextQuantitativeDoubtful = s.Reporting.TextQuantitativeDoubtful,
                    DefaultTextQuantitativeDoubtful = s.Reporting.DefaultTextQuantitativeDoubtful,
                    UseCalculatedTextQuantitativeAbove = s.Reporting.UseCalculatedTextQuantitativeAbove,
                    MinConcentrationToReport = s.Reporting.MinConcentrationToReport,
                    DefaultMinConcentrationToReport = s.Reporting.DefaultMinConcentrationToReport
                }
            })
            .SingleOrDefaultAsync(cancellationToken) ?? throw new NotFoundException();

        return methodDetailsDto;
    }
}