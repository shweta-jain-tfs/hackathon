using Pharos.Server.Application.Lis;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Infrastructure;
using Pharos.Server.Lis.Astm.Application.Messages;

namespace Pharos.Server.Lis.Astm.Application.Mappings;

internal static class AstmTestOrderRecordExtensions
{
    public static LisSample MapToSample(this AstmTestOrderRecord record)
    {
        var sample = new LisSample
        {
            SampleId = record.SpecimenId,
            RequestDate = record.RequestedDateAndTime,
            SampleCollectionDate = record.SpecimenCollectionDateAndTime,
            // TODO: Set SampleVolumeCheck depending on "Enable Volume Check" Setting?
            //SampleVolumeCheck = SampleVolumeCheck.Off,
            TestOrders = new List<LisTestOrder>(),
            TubeType = record.TubeType
        };

        return sample;
    }

    public static void MapFromSampleAndTestOrder(this AstmTestOrderRecord record, LisSample sample, LisTestOrder testOrder, AstmSettings settings, IDateTime dateTime)
    {
        if (sample.IsSampleLocallyCreated)
        {
            record.SpecimenId = settings.ExportSettings.UseLocalSampleIdAsLisSampleId ? sample.SampleId : string.Empty;
        }
        else
        {
            record.SpecimenId = sample.SampleId;
        }

        record.InstrumentSpecimenId = sample.SampleId;
        record.RequestedDateAndTime = sample.RequestDate;
        record.SpecimenCollectionDateAndTime = sample.SampleCollectionDate;
        record.Dilution = testOrder.PreDilutionFactor ?? 1;
        record.TubeType = sample.TubeType;
        record.RackId = testOrder.TrayId ?? string.Empty;
        record.RackPosition = testOrder.TubePosition ?? 0;

        var includeRequestorInfo = sample.Requestor != null && !settings.ExportSettings.SkipExportOfRequestorInformation;
        if (includeRequestorInfo)
        {
            record.RequestorId = sample.Requestor!.RequestorId;
            record.RequestorShortcutCode = sample.Requestor.ShortcutCode ?? string.Empty;
            record.RequestorPhone = sample.Requestor.PhoneNumber ?? string.Empty;
            record.RequestorComment = sample.Requestor.Comment ?? string.Empty;
            record.RequestorName = sample.Requestor.Name ?? string.Empty;
            record.RequestorReflexTesting = Convert.ToInt32(sample.Requestor.ReflexEnabled);
            SetRequestorAddressLines(record, sample.Requestor.Address, settings.Other.CombineRequestorAddressLinesOneToThree);
        }

        record.TestAction = AstmTestAction.NewTest;

        var methodTest = GetMethodTest(testOrder, settings);
        record.UniversalTestIds.Add(new AstmTestId(methodTest.TestName, methodTest.MethodName, testOrder.InstrumentDilutionFactor ?? 1, 0, "", testOrder.NumberOfReplicates ?? 1));  // todo: reflex ID
        record.ReportType = GetReportType(testOrder.TestResult);
        record.ResultsReportedOrLastModifiedDateAndTime = dateTime.Now;

        if (settings.ExportSettings.OverrideInstrumentName.Length > 0)
        {
            record.InstrumentSectionId = settings.ExportSettings.OverrideInstrumentName;
        }
        else
        {
            record.InstrumentSectionId = testOrder.TestResult?.InstrumentName ?? string.Empty;
        }
    }

    public static void MapToTestOrders(this AstmTestOrderRecord record, ICollection<LisTestOrder> testOrders, AstmImportSettings importSettings, LisImportSettings lisImportSettings)
    {
        foreach (var testId in record.UniversalTestIds)
        {
            var testOrder = new LisTestOrder
            {
                Action = record.TestAction switch
                {
                    AstmTestAction.Cancel => LisTestOrderAction.CancelTest,
                    AstmTestAction.QualityControl => LisTestOrderAction.QualityControl,
                    AstmTestAction.NewTest => LisTestOrderAction.NewTest,
                    AstmTestAction.ReTest => LisTestOrderAction.ReTest,
                    AstmTestAction.AddTest => LisTestOrderAction.NewTest,
                    _ => LisTestOrderAction.Undefined,
                },
                MethodName = testId.LisMethodId,
                TestName = testId.TestName,
                PanelExpansionEnabled = lisImportSettings.PanelExpansion,
                AutoPanelRecognitionFromOneMessage = lisImportSettings.AutomaticPanelRecognition == AutomaticPanelRecognition.OnlyFromOneMessage,
                AutoPanelRecognitionFromMultipleMessage = lisImportSettings.AutomaticPanelRecognition == AutomaticPanelRecognition.FromMultipleMessages,
                ForceUniqueLaboratoryTestNames = lisImportSettings.ForceUniqueLaboratoryTestNames,
                UseRequestPriorityA = lisImportSettings.RequestPriorityOrder == RequestPriorityOrder.UseOrderNumberA,
                UseRequestPriorityB = lisImportSettings.RequestPriorityOrder == RequestPriorityOrder.UseOrderNumberB,
                ReflexName = testId.ReflexName,
                SuppressReflex = testId.SuppressReflex == 1
            };

            if (!importSettings.IgnoreImportedPreDilutionFactor && record.Dilution > 0)
            {
                testOrder.PreDilutionFactor = record.Dilution;
            }

            if (!importSettings.IgnoreImportedNumberOfReplicates && testId.Replicates > 0)
            {
                testOrder.NumberOfReplicates = testId.Replicates;
            }

            if (!importSettings.IgnoreImportedInstrumentDilutionFactor && testId.InstrumentDilution > 0)
            {
                testOrder.InstrumentDilutionFactor = testId.InstrumentDilution;
            }

            testOrders.Add(testOrder);
        }
    }

    public static void MapToRequestor(this AstmTestOrderRecord record, LisSample curSample, AstmSettings astmSettings)
    {
        curSample.Requestor = new LisRequestor
        {
            RequestorId = record.RequestorId,
            Name = record.RequestorName,
            Address = CombineRequestorAddressFields(record, astmSettings.Other.CombineRequestorAddressLinesOneToThree),
            Comment = record.RequestorComment,
            ZipCode = record.SectionCode,
            OrderingPhysicianCode = record.HospitalCode,
            PhoneNumber = record.RequestorPhone,
            ShortcutCode = record.RequestorShortcutCode,
            ReflexEnabled = record.RequestorReflexTesting != 0,
            HasNonUniqueRequestorId = astmSettings.Other.NonUniqueOrderingPhysicianCode,
        };
    }

    public static void MapFromQualityControlResult(this AstmTestOrderRecord record, LisQualityControlTestResult qcTestResult, AstmExportSettings exportSettings, IDateTime dateTime)
    {
        if (exportSettings.UseLocalSampleIdAsLisSampleId)
        {
            record.SpecimenId = qcTestResult.QualityControlName;
        }

        record.InstrumentSpecimenId = qcTestResult.QualityControlName;
        record.TubeType = "N";
        record.UniversalTestIds.Add(new AstmTestId(qcTestResult.TestName, qcTestResult.MethodName, qcTestResult.InstrumentDilutionFactor, 0, "", 0));
        record.TestAction = AstmTestAction.QualityControl;
        record.Dilution = qcTestResult.PreDilutionFactor;
        record.ResultsReportedOrLastModifiedDateAndTime = dateTime.Now;

        record.InstrumentSectionId = exportSettings.OverrideInstrumentName.Length > 0
           ? exportSettings.OverrideInstrumentName
           : qcTestResult.InstrumentName ?? string.Empty;

        record.ReportType = qcTestResult.Concentration == null
            ? AstmReportType.Error
            : AstmReportType.FinalResult;
    }

    private static string CombineRequestorAddressFields(AstmTestOrderRecord record, bool CombineRequestorAddressLine1To3)
    {
        var addressLines = new[]
                            {
                                record.RequestorAddressLine1,
                                record.RequestorAddressLine2,
                                record.RequestorAddressLine3,
                                record.RequestorAddressLine4,
                                record.RequestorAddressLine5
                            };

        if (CombineRequestorAddressLine1To3)
        {
            var firstThreeAddressLines = addressLines.Take(3);
            var remainingAddressLines = addressLines.Skip(3);

            var combinedFirstThree = string.Join(",", firstThreeAddressLines);
            var combinedRemaining = string.Join("\n", remainingAddressLines);
            var combinedAddress = string.Join("\n", combinedFirstThree, combinedRemaining);
            return combinedAddress;
        }

        return string.Join("\n", addressLines);
    }

    private static void SetRequestorAddressLines(AstmTestOrderRecord record, string? requestorAddress, bool CombineRequestorAddressLine1To3)
    {
        if (string.IsNullOrEmpty(requestorAddress))
        {
            return;
        }

        var addressLines = requestorAddress.Split('\n');

        if (CombineRequestorAddressLine1To3)
        {
            var splitLine = addressLines[0].Split(',');
            if (splitLine.Length > 0) { record.RequestorAddressLine1 = splitLine[0]; }
            if (splitLine.Length > 1) { record.RequestorAddressLine2 = splitLine[1]; }
            if (splitLine.Length > 2) { record.RequestorAddressLine3 = splitLine[2]; }

            if (addressLines.Length > 1) { record.RequestorAddressLine4 = addressLines[1]; }
            if (addressLines.Length > 2) { record.RequestorAddressLine5 = addressLines[2]; }
        }
        else
        {
            if (addressLines.Length > 0) { record.RequestorAddressLine1 = addressLines[0]; }
            if (addressLines.Length > 1) { record.RequestorAddressLine2 = addressLines[1]; }
            if (addressLines.Length > 2) { record.RequestorAddressLine3 = addressLines[2]; }
            if (addressLines.Length > 3) { record.RequestorAddressLine4 = addressLines[3]; }
            if (addressLines.Length > 4) { record.RequestorAddressLine5 = addressLines[4]; }
        }
    }

    private static AstmReportType GetReportType(LisSampleTestResult? sampleTestResult)
    {
        if (sampleTestResult == null)
        {
            return AstmReportType.ResultPending;
        }

        if (sampleTestResult.HasError)
        {
            return AstmReportType.Error;
        }

        if (sampleTestResult.Pending)
        {
            return AstmReportType.ResultPending;
        }

        return AstmReportType.FinalResult;
    }

    private static (string MethodName, string TestName) GetMethodTest(LisTestOrder testOrder, AstmSettings settings)
    {
        return settings.ExportSettings.ExportPanelNameInOrderRecord switch
        {
            ExportPanelNameInOrderRecord.Used when testOrder.PanelNames.Count > 0
                => (string.Empty, testOrder.PanelNames[0]),

            ExportPanelNameInOrderRecord.UsedWithResultsForAllPanels when testOrder.PanelNames.Count > 0
                => throw new NotImplementedException(""),

            _ => (testOrder.MethodName, testOrder.TestName)
        };
    }
}
