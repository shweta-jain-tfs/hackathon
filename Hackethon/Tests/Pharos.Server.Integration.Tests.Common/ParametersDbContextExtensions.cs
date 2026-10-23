using AutoFixture;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Infrastructure.Database;
using Pharos.Server.Parameters.Application.Interfaces;
using Xunit;

namespace Pharos.Server.Integration.Tests.Common;

public static class ParametersDbContextExtensions
{
    public const string MethodName = "sIgE";

    public static async Task<Method> CreateMethod(this IDbContext dbContext, string methodName = MethodName, string fullMethodName = "ImmunoCAP Specific IgE 0-100", string[]? testNames = null)
    {
        var fixture = new Fixture();
        var db = (IParametersDbContext)dbContext;

        var concentrationUnit = new MethodConcentrationUnit
        {
            UnitId = 1,
            ConcentrationUnit = "kUA/l"
        };
        db.MethodConcentrationUnits.Add(concentrationUnit);

        var method = new Method
        {
            MethodName = methodName,
            FullMethodName = fullMethodName,
            MethodNameLis = methodName,
            ConcentrationUnits = [concentrationUnit],
            Enabled = true,
            AnalyticalRun = new MethodAnalyticalRun(),
            Calculation = new MethodCalculation(),
            Presentation = fixture.Create<MethodPresentation>(),
            Reporting = fixture.Create<MethodReporting>(),
            Reagents =
            [
                new MethodReagent
                {
                    ReagentType = ReagentType.Conjugate,
                    DefaultReagentIdentity = methodName
                }
            ],
            Tests = []
        };
        db.Methods.Add(method);

        if (testNames != null)
        {
            foreach (var testName in testNames)
            {
                method.Tests.Add(new MethodTest
                {
                    TestName = testName,
                    FullTestName = testName,
                    DefaultLabTestName = testName,
                    Method = method,
                    DefaultConcentrationUnit = concentrationUnit,
                    DiluentIdentity = methodName
                });
            }
        }

        db.Methods.Add(method);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        return method;
    }
}
