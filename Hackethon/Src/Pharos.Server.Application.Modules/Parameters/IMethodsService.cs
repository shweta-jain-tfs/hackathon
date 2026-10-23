using System.Linq.Expressions;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Parameters.Methods;

namespace Pharos.Server.Application.Parameters;

public interface IMethodsService
{
    Task<TResult?> GetMethod<TResult>(string methodName, Expression<Func<Method, TResult>> selector);

    /// <summary>
    /// Gets the list of enabled (for the entire lab) non-template methods.
    /// </summary>
    Task<List<TResult>> GetMethods<TResult>(Expression<Func<Method, bool>> filter, Expression<Func<Method, TResult>> selector);

    /// <summary>
    /// Gets the list of enabled (for the entire lab) non-template methods with their details.
    /// The details include the calibration curve, tests, reagents, curve and quality controls
    /// (enough to update the method info on the instrument).
    /// </summary>
    Task<List<Method>> GetDetailedMethods();
    Task<TResult?> GetMethodTestByLabNameOrTestName<TResult>(string? methodName, string testName, Expression<Func<MethodTest, TResult>> selector);
    Task<TResult?> GetMethodTestByLabName<TResult>(string? labMethodName, string labTestName, Expression<Func<MethodTest, TResult>> selector, bool forceUniqueLaboratoryTestNames = false);
    Task<TResult?> GetMethodTest<TResult>(string methodName, string testName, Expression<Func<MethodTest, TResult>> selector);
    Task<TResult?> GetQualityControl<TResult>(string methodName, string testName, string qualityControlName, Expression<Func<PhadiaQualityControlTest, TResult>> selector);
    Task<TPoint?> GetMethodCalibratorPoint<TPoint>(string methodName, string calibratorName, Expression<Func<MethodCalibrationPoint, TPoint>> selector);
    Task<List<TPoint>> GetMethodCalibratorPoints<TPoint>(string methodName, Expression<Func<MethodCalibrationPoint, TPoint>> selector);
    Task<(bool PreventDilutionFactorsBelowDefault, int DefaultDilutionFactor, AssayTechnology? Technology)> GetDefaultInstrumentDilutionFactor(string testName, string methodName);
    Task<TResult?> GetCurveControl<TResult>(string methodName, string curveControlName, Expression<Func<MethodCurveControl, TResult>> selector);
    Task<int> GetCalibrationCurveDaysValidity(string methodName);
    Task<int> GetMethodCurveControlCount(string methodName);
    Task<HashSet<string>> GetPhadiaQualityControlCartridgeNames();
    Task<string?> GetMethodTestConcentrationUnit(string methodName, string testName);
    Task<Dictionary<(string MethodName, string TestName), string?>> GetMethodTestConcentrationUnits(IEnumerable<(string MethodName, string TestName)> methodTestPairs);
    Task<Dictionary<string, int?>> GetMethodsSortOrder();
}
