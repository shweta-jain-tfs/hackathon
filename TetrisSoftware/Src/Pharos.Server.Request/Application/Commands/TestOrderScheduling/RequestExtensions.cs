using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Settings.Sample;
using TestOrder = Pharos.Server.Request.Domain.TestOrder;

namespace Pharos.Server.Request.Application.Commands.TestOrderScheduling;

internal static class RequestExtensions
{
    /// <summary>
    /// Gets not started test orders that have their methods and tests enabled in parameters and have not been deactivated by the user.
    /// </summary>
    public static async Task<List<TestOrder>> GetNotStartedEnabledTests(this Domain.Request request, IMethodsService methods)
    {
        var notStartedTests = request.TestOrders
            .Where(x => x.State == TestOrderState.NotStarted && x.IsActive)
            .ToList();

        if (notStartedTests.Count == 0)
        {
            return notStartedTests;
        }

        var methodNames = notStartedTests.Select(t => t.MethodName).ToHashSet();
        var testNames = notStartedTests.Select(t => t.TestName).ToHashSet();

        var enabledMethods = await methods.GetMethods(
            x => methodNames.Contains(x.MethodName),
            x => new EnabledMethod
            {
                MethodName = x.MethodName,
                Tests = x.Tests
                    .Where(t => t.Active && testNames.Contains(t.TestName))
                    .Select(t => t.TestName)
                    .ToList()
            });

        var enabledTests = notStartedTests
            .Where(testOrder =>
            {
                var method = enabledMethods.FirstOrDefault(m => m.MethodName == testOrder.MethodName);
                return method != null && method.Tests.Contains(testOrder.TestName);
            })
            .ToList();

        return enabledTests;
    }

    public static TubeType? GetTubeType(this Domain.Request request, TubeTypesSettings settings)
    {
        var tubeType = request.SampleTubeType == null
            ? settings.TubeTypeList.FirstOrDefault(x => x.TubeDefinitionId.ToString() == settings.NormalTubeDefinitionId)
            : settings.TubeTypeList.FirstOrDefault(x => x.TubeDefinitionId == request.SampleTubeType);
        return tubeType;
    }

    internal class EnabledMethod
    {
        public string MethodName { get; init; } = null!;
        public List<string> Tests { get; init; } = null!;
    }
}
