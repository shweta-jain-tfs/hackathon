using System.Linq.Expressions;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Security;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Parameters.Application.Interfaces;
using Method = Pharos.Server.Domain.Parameters.Methods.Method;

namespace Pharos.Server.Parameters.Application.Services;

internal class MethodsService(IParametersDbContext dbContext, IAuthorizationService authorizationService) : IMethodsService
{
    public Task<TResult?> GetMethod<TResult>(string methodName, Expression<Func<Method, TResult>> selector)
    {
        return dbContext.Methods
            .Where(x => x.MethodName == methodName)
            .AsNoTracking()
            .Select(selector)
            .FirstOrDefaultAsync();
    }

    public Task<List<TResult>> GetMethods<TResult>(Expression<Func<Method, bool>> filter, Expression<Func<Method, TResult>> selector)
    {
        return DefaultMethodsQuery
            .Where(filter)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.MethodName)
            .AsNoTracking()
            .Select(selector)
            .ToListAsync();
    }

    public Task<List<Method>> GetDetailedMethods()
    {
        return DefaultMethodsQuery
            .Include(x => x.CalibrationCurve)
                .ThenInclude(x => x.MethodTest)
            .Include(x => x.CalibrationCurve)
                .ThenInclude(x => x.CalibrationPoints)
            .Include(x => x.Tests)
            .Include(x => x.CurveControls)
            .Include(x => x.Reagents)
            .AsSplitQuery()
            .AsNoTracking()
            .ToListAsync();
    }

    public Task<TResult?> GetMethodTest<TResult>(string methodName, string testName, Expression<Func<MethodTest, TResult>> selector)
    {
        return dbContext.MethodTests
            .Where(x => x.TestName == testName && x.Method.MethodName == methodName)
            .AsNoTracking()
            .Select(selector)
            .FirstOrDefaultAsync();
    }

    public async Task<TResult?> GetMethodTestByLabNameOrTestName<TResult>(string? methodName, string testName, Expression<Func<MethodTest, TResult>> selector)
    {
        var result = await GetMethodTestByMethodAndLabName(methodName, testName, selector);

        return result ?? await GetMethodTestByMethodAndTestName(methodName, testName, selector);
    }

    public async Task<TResult?> GetMethodTestByLabName<TResult>(string? labMethodName, string labTestName, Expression<Func<MethodTest, TResult>> selector, bool forceUniqueLaboratoryTestNames = false)
    {
        if (forceUniqueLaboratoryTestNames)
        {
            return await GetMethodTestByLabNameOnly(labTestName, selector);
        }

        return await GetMethodTestByMethodAndLabName(labMethodName, labTestName, selector);
    }

    public Task<TResult?> GetQualityControl<TResult>(string methodName, string testName, string qualityControlName, Expression<Func<PhadiaQualityControlTest, TResult>> selector)
    {
        return dbContext.PhadiaQualityControlTests
            .Include(x => x.QualityControl)
            .Include(x => x.Test)
            .Where(qc => qc.Test.Method.MethodName == methodName && qc.QualityControl.Name == qualityControlName && qc.Test.TestName == testName)
            .AsNoTracking()
            .Select(selector)
            .FirstOrDefaultAsync();
    }

    public Task<TPoint?> GetMethodCalibratorPoint<TPoint>(string methodName, string calibratorName, Expression<Func<MethodCalibrationPoint, TPoint>> selector)
    {
        return dbContext.MethodCalibrationPoints
            .Where(p => p.CalibratorName == calibratorName && p.CalibrationCurve.Method.MethodName == methodName)
            .AsNoTracking()
            .Select(selector)
            .FirstOrDefaultAsync();
    }

    public Task<List<TPoint>> GetMethodCalibratorPoints<TPoint>(string methodName, Expression<Func<MethodCalibrationPoint, TPoint>> selector)
    {
        return dbContext.MethodCalibrationPoints
            .Where(p => p.CalibrationCurve.Method.MethodName == methodName)
            .OrderBy(p => p.ExpectedConcentration)
            .AsNoTracking()
            .Select(selector)
            .ToListAsync();
    }

    public async Task<(bool PreventDilutionFactorsBelowDefault, int DefaultDilutionFactor, AssayTechnology? Technology)> GetDefaultInstrumentDilutionFactor(string testName, string methodName)
    {
        var test = await dbContext.MethodTests.Include(x => x.Method).Where(x => x.TestName == testName && x.Method.MethodName == methodName).AsNoTracking().FirstOrDefaultAsync();
        if (test != null)
        {
            return test.DilutionMethod == DilutionMethod.PreDiluted
                ? (test.Method.PreventDilutionFactorsBelowDefault, 1, test.Method.Technology)
                : (test.Method.PreventDilutionFactorsBelowDefault, test.DilutionFactor, test.Method.Technology);
        }

        return (false, 1, null);
    }

    public Task<TResult?> GetCurveControl<TResult>(string methodName, string curveControlName, Expression<Func<MethodCurveControl, TResult>> selector)
    {
        return dbContext.MethodCurveControls
            .Where(x => x.Method.MethodName == methodName && x.CurveControlName == curveControlName)
            .AsNoTracking()
            .Select(selector)
            .FirstOrDefaultAsync();
    }

    public async Task<int> GetCalibrationCurveDaysValidity(string methodName)
    {
        var maxNumberOfDaysSettings = await GetMethod(methodName, x => new
        {
            x.CalibrationCurve.UserMaxNumberOfDays,
            x.CalibrationCurve.DefaultMaxNumberOfDays
        });
        var maxNumberOfDays = 28;
        if (maxNumberOfDaysSettings != null)
        {
            maxNumberOfDays = maxNumberOfDaysSettings.UserMaxNumberOfDays ?? maxNumberOfDaysSettings.DefaultMaxNumberOfDays;
        }

        return maxNumberOfDays;
    }

    public async Task<int> GetMethodCurveControlCount(string methodName)
    {
        return await dbContext.MethodCurveControls
            .Where(x => x.Method.MethodName == methodName && x.Active)
            .CountAsync();
    }

    public async Task<HashSet<string>> GetPhadiaQualityControlCartridgeNames()
    {
        return await dbContext.PhadiaQualityControl
            .Where(x => !x.IsQcInTube && x.Tests.Any(t => t.Enabled))
            .Select(x => x.Name)
            .ToHashSetAsync();
    }

    public Task<string?> GetMethodTestConcentrationUnit(string methodName, string testName)
    {
        return dbContext.MethodTests
            .Where(x => x.TestName == testName && x.Method.MethodName == methodName)
            .AsNoTracking()
            .Select(x => x.UserConcentrationUnit != null
                ? x.UserConcentrationUnit.ConcentrationUnit
                : x.DefaultConcentrationUnit.ConcentrationUnit)
            .FirstOrDefaultAsync();
    }

    public async Task<Dictionary<(string MethodName, string TestName), string?>> GetMethodTestConcentrationUnits(IEnumerable<(string MethodName, string TestName)> methodTestPairs)
    {
        var pairs = methodTestPairs.ToHashSet();
        var methodNames = pairs.Select(x => x.MethodName).Distinct().ToArray();
        var testNames = pairs.Select(x => x.TestName).Distinct().ToArray();

        var units = await dbContext.MethodTests
            .AsNoTracking()
            .Where(mt => methodNames.Contains(mt.Method.MethodName) && testNames.Contains(mt.TestName))
            .Select(mt => new
            {
                mt.Method.MethodName,
                mt.TestName,
                Unit = mt.UserConcentrationUnit != null
                    ? mt.UserConcentrationUnit.ConcentrationUnit
                    : mt.DefaultConcentrationUnit.ConcentrationUnit
            })
            .ToListAsync();

        return units.Where(x => pairs.Contains((x.MethodName, x.TestName)))
            .ToDictionary(x => (x.MethodName, x.TestName), x => (string?)x.Unit);
    }

    public async Task<Dictionary<string, int?>> GetMethodsSortOrder()
    {
        return await dbContext.Methods
            .AsNoTracking()
            .ToDictionaryAsync(x => x.MethodName, x => x.SortOrder);
    }

    private IQueryable<Method> DefaultMethodsQuery
    {
        get
        {
            var query = dbContext.Methods.AsQueryable().Where(x => x.Enabled && !x.IsTemplate);

            if (!authorizationService.RuoMode)
            {
                query = query.Where(x => !x.IsRuo);
            }
            return query;
        }
    }

    #region Get MethodTest Helpers

    private async Task<TResult?> GetMethodTestByLabNameOnly<TResult>(string labTestName, Expression<Func<MethodTest, TResult>> selector)
    {
        try
        {
            // check lab-defined test names first
            var result = await dbContext.MethodTestLabNames
                          .Where(x => x.TestName == labTestName && x.MethodTest.Active && x.MethodTest.Method.Enabled)
                          .OrderBy(x => x.MethodTest.Method.SortOrder)
                          .ThenBy(x => x.MethodTest.Method.MethodName)
                      .AsNoTracking()
                      .Select(x => x.MethodTest)
                      .Select(selector)
                      .FirstOrDefaultAsync();

            return result;
        }
        catch (InvalidOperationException)
        {
            // the lab test name is not unique
            return default;
        }
    }

    private async Task<TResult?> GetMethodTestByMethodAndLabName<TResult>(
    string? labMethodName,
    string labTestName,
    Expression<Func<MethodTest, TResult>> selector)
    {
        return await dbContext.MethodTestLabNames
            .Where(x => x.TestName == labTestName
                && (x.MethodTest.Method.MethodNameLis == labMethodName
                    || x.MethodTest.Method.MethodName == labMethodName)
                && x.MethodTest.Active
                && x.MethodTest.Method.Enabled)
            .AsNoTracking()
            .Select(x => x.MethodTest)
            .Select(selector)
            .FirstOrDefaultAsync();
    }

    private async Task<TResult?> GetMethodTestByMethodAndTestName<TResult>(
    string? labMethodName,
    string labTestName,
    Expression<Func<MethodTest, TResult>> selector)
    {
        return await dbContext.MethodTests
            .Where(x => x.TestName == labTestName
                && (x.Method.MethodNameLis == labMethodName
                    || x.Method.MethodName == labMethodName)
                && x.Active
                && x.Method.Enabled)
            .AsNoTracking()
            .Select(selector)
            .FirstOrDefaultAsync();
    }

    #endregion
}
