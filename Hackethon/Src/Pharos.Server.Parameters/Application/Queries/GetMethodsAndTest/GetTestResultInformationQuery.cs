using MediatR;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Queries.GetMethodsAndTest;

internal sealed class GetTestResultInformationQueryHandler(IParametersDbContext context) : IRequestHandler<GetTestResultInformationQuery, ParameterInfo>
{
    public async Task<ParameterInfo> Handle(GetTestResultInformationQuery request, CancellationToken cancellationToken)
    {
        var parameterInfo = new ParameterInfo();

        var methodTest = await context.MethodTests
            .Include(x => x.Method).ThenInclude(x => x.ClassTables)
            .Include(x => x.Method).ThenInclude(x => x.ClassTableBelows)
            .FirstOrDefaultAsync(x => x.TestName == request.TestName && x.Method.MethodName == request.MethodName, cancellationToken);

        if (methodTest == null)
        {
            return new();
        }

        // Add class options
        parameterInfo.ClassOptions.AddRange(methodTest.Method.ClassTableBelows.Where(x => x.ClassTableId == methodTest.ReportClassId).Select(x => x.TextBelowConcentration).ToList());
        parameterInfo.ClassOptions.AddRange(methodTest.Method.ClassTables.Where(x => x.ClassTableId == methodTest.ReportClassId).Select(x => x.TextAboveConcentration).ToList());

        // Add cut off options
        parameterInfo.CutOffOptions.Add(methodTest.Method.Reporting.TextAboveCutOff);
        parameterInfo.CutOffOptions.Add(methodTest.Method.Reporting.TextBelowCutOff);

        // Add reporting information
        parameterInfo.ReportConcentration = methodTest.ReportConcentration;
        parameterInfo.ReportClass = methodTest.ReportClass;
        parameterInfo.ReportCutOff = methodTest.ReportCutOff;
        if (methodTest.ReportQuotient != QuotientReportMode.NoReport)
        {
            parameterInfo.ReportQuotient = true;
        }

        return parameterInfo;
    }
}