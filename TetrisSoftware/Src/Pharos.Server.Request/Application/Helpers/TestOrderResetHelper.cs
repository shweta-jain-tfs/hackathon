using Pharos.Server.Domain.Reflex;
using Pharos.Server.Request.Domain;

namespace Pharos.Server.Request.Application.Helpers;

/// <summary>
/// Helper class containing shared logic for test order reset operations.
/// Consolidates the rules for which test orders can be reset based on their status.
/// </summary>
public static class TestOrderResetHelper
{
    /// <summary>
    /// Determines whether a test order can be reset based on its current status.
    /// </summary>
    /// <param name="status">The current status of the test order.</param>
    /// <param name="includeAborted">Whether to include tests with Aborted status.</param>
    /// <returns>True if the test order can be reset, otherwise false.</returns>
    public static bool CanReset(TestOrderState status, bool includeAborted)
    {
        return status switch
        {
            TestOrderState.Aborted => includeAborted,
            TestOrderState.Pipetted => true,
            TestOrderState.Scheduled => true,
            TestOrderState.Started => true,
            TestOrderState.UnableToStart => true,
            _ => false
        };
    }

    /// <summary>
    /// Resets a test order by creating a new instance with a fresh UID.
    /// All test properties are preserved from the original test order, but status is reset to NotStarted
    /// and the TubeId is cleared. ReflexDefinitions must be cloned separately using CloneReflexDefinitions.
    /// </summary>
    /// <param name="originalTestOrder">The original test order to reset.</param>
    /// <returns>A new ITestOrder instance with a fresh UID and reset status.</returns>
    internal static TestOrder ResetTestOrder(TestOrder originalTestOrder)
    {
        return new TestOrder
        {
            Uid = Guid.NewGuid(),
            State = TestOrderState.NotStarted,
            MethodName = originalTestOrder.MethodName,
            TestName = originalTestOrder.TestName,
            LabTestName = originalTestOrder.LabTestName,
            NumberOfReplicates = originalTestOrder.NumberOfReplicates,
            InstrumentDilutionFactor = originalTestOrder.InstrumentDilutionFactor,
            TestSortOrder = originalTestOrder.TestSortOrder,
            TestPanelIds = originalTestOrder.TestPanelIds,
            LisChannelId = originalTestOrder.LisChannelId,
            IsReflexTest = originalTestOrder.IsReflexTest,
            IsActive = originalTestOrder.IsActive,
            RequestId = originalTestOrder.RequestId,
            TestOrderCustomParameters = originalTestOrder.TestOrderCustomParameters,
            InstrumentNumber = null,
            TubeId = null
        };
    }

    /// <summary>
    /// Deep clones reflex definitions from an original test order to a new test order.
    /// Creates new ReflexDefinition entities with new IDs that are properly associated with the new test order.
    /// </summary>
    /// <param name="originalReflexDefinitions">The reflex definitions to clone.</param>
    /// <returns>A new list of cloned reflex definitions.</returns>
    internal static List<ReflexDefinition> CloneReflexDefinitions(IList<ReflexDefinition>? originalReflexDefinitions)
    {
        if (originalReflexDefinitions == null || originalReflexDefinitions.Count == 0)
        {
            return new List<ReflexDefinition>();
        }

        var clonedDefinitions = new List<ReflexDefinition>();
        foreach (var original in originalReflexDefinitions)
        {
            var cloned = new ReflexDefinition
            {
                Name = original.Name,
                Automatic = original.Automatic,
                RequestorId = original.RequestorId,
                ReflexCondition = new ReflexCondition
                {
                    TestName = original.ReflexCondition.TestName,
                    MethodName = original.ReflexCondition.MethodName,
                    ResultType = original.ReflexCondition.ResultType,
                    CompareType = original.ReflexCondition.CompareType,
                    ConcentrationCompareValue = original.ReflexCondition.ConcentrationCompareValue,
                    ConcentrationCompareValueUpperLimit = original.ReflexCondition.ConcentrationCompareValueUpperLimit,
                    ConcentrationRawCompareValue = original.ReflexCondition.ConcentrationRawCompareValue,
                    ConcentrationRawCompareValueUpperLimit = original.ReflexCondition.ConcentrationRawCompareValueUpperLimit,
                    ClassCompareValue = original.ReflexCondition.ClassCompareValue,
                    QuotientCompareValue = original.ReflexCondition.QuotientCompareValue,
                    QuotientCompareValueUpperLimit = original.ReflexCondition.QuotientCompareValueUpperLimit,
                    CutOffCompareValue = original.ReflexCondition.CutOffCompareValue
                },
                ReflexTests = original.ReflexTests.Select(test => new ReflexTest
                {
                    TestName = test.TestName,
                    MethodName = test.MethodName,
                    NumberOfReplicates = test.NumberOfReplicates,
                    InstrumentDilutionFactor = test.InstrumentDilutionFactor,
                    TestSortOrder = test.TestSortOrder,
                }).ToList()
            };
            clonedDefinitions.Add(cloned);
        }

        return clonedDefinitions;
    }
}
