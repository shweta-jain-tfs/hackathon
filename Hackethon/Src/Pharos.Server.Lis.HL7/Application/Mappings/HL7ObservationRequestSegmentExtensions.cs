using Pharos.Server.Application.Lis;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Lis.HL7.Application.Messages;

namespace Pharos.Server.Lis.HL7.Application.Mappings;

internal static class HL7ObservationRequestSegmentExtensions
{
    public static void MapFromSampleAndTestOrder(
        this HL7ObservationRequestSegment segment,
        LisSample sample,
        LisTestOrder testOrder,
        HL7ExportSettings exportSettings,
        bool forceUniqueLaboratoryTestNames)
    {
        var (methodName, testName) = GetMethodTest(testOrder, exportSettings);
        var universalServiceIdentifier = CreateUniversalServiceIdentifier(methodName, testName, forceUniqueLaboratoryTestNames);

        segment.PlacerOrderNumber = sample.RequestId ?? "";
        segment.UniversalServiceIdentifier = universalServiceIdentifier;
        segment.ObservationDateTime = testOrder.TestResult?.MeasurementDateTime ?? sample.SampleCollectionDate;
        segment.SpecimenSourceCode = HL7SpecimenType.Serum;
    }

    public static void MapFromQualityControlResult(this HL7ObservationRequestSegment segment, LisQualityControlTestResult qcTestResult, bool forceUniqueLaboratoryTestNames)
    {
        segment.UniversalServiceIdentifier = CreateUniversalServiceIdentifier(qcTestResult.MethodName, qcTestResult.TestName, forceUniqueLaboratoryTestNames);
        segment.ObservationDateTime = qcTestResult.MeasurementDateTime;
        segment.SpecimenSourceCode = HL7SpecimenType.QualityControl;
    }

    public static LisTestOrder MapToTestOrder(
        this HL7ObservationRequestSegment segment,
        LisImportSettings lisImportSettings,
        IMethodsService methodsService)
    {
        var (methodName, testName) = ResolveMethodTestIdentifier(segment.UniversalServiceIdentifier, methodsService).GetAwaiter().GetResult();

        return new LisTestOrder
        {
            MethodName = methodName,
            TestName = testName,
            Action = MapTestOrderAction(segment.SpecimenActionCode),
            PanelExpansionEnabled = lisImportSettings.PanelExpansion,
            AutoPanelRecognitionFromOneMessage = lisImportSettings.AutomaticPanelRecognition == AutomaticPanelRecognition.OnlyFromOneMessage,
            AutoPanelRecognitionFromMultipleMessage = lisImportSettings.AutomaticPanelRecognition == AutomaticPanelRecognition.FromMultipleMessages,
            UseRequestPriorityA = lisImportSettings.RequestPriorityOrder == RequestPriorityOrder.UseOrderNumberA,
            UseRequestPriorityB = lisImportSettings.RequestPriorityOrder == RequestPriorityOrder.UseOrderNumberB,
            ForceUniqueLaboratoryTestNames = lisImportSettings.ForceUniqueLaboratoryTestNames
        };
    }

    private static async Task<(string MethodName, string TestName)> ResolveMethodTestIdentifier(
        string universalServiceIdentifier,
        IMethodsService methodsService)
    {
        var identifierParts = universalServiceIdentifier.Split('-');
        if (identifierParts.Length == 1)
        {
            return ("", universalServiceIdentifier);
        }

        var methodName = identifierParts[0];
        if (await MethodExists(methodName, methodsService))
        {
            return (methodName, string.Join('-', identifierParts.Skip(1)));
        }

        if (identifierParts.Length > 2)
        {
            methodName = string.Join('-', identifierParts.Take(2));
            if (await MethodExists(methodName, methodsService))
            {
                return (methodName, string.Join('-', identifierParts.Skip(2)));
            }
        }

        return ("", universalServiceIdentifier);
    }

    private static async Task<bool> MethodExists(string methodName, IMethodsService methodsService)
    {
        return await methodsService.GetMethod(methodName, method => method.MethodName) != null;
    }

    private static LisTestOrderAction MapTestOrderAction(HL7SpecimenActionCode actionCode)
    {
        return actionCode switch
        {
            HL7SpecimenActionCode.Add => LisTestOrderAction.NewTest,
            HL7SpecimenActionCode.New => LisTestOrderAction.NewTest,
            _ => LisTestOrderAction.Undefined
        };
    }

    private static string CreateUniversalServiceIdentifier(string methodName, string testName, bool forceUniqueLaboratoryTestNames)
    {
        if (forceUniqueLaboratoryTestNames)
        {
            return testName;
        }

        return string.Join('-', new[] { methodName, testName }.Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static (string MethodName, string TestName) GetMethodTest(LisTestOrder testOrder, HL7ExportSettings exportSettings)
    {
        if (exportSettings.ExportPanelNameInOrderRecord && testOrder.PanelNames.Count > 0)
        {
            return (string.Empty, testOrder.PanelNames[0]);
        }

        return (testOrder.MethodName, testOrder.TestName);
    }
}