using Microsoft.Extensions.Logging;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Parameters.Application.Interfaces;
using Method = Pharos.Server.Domain.Parameters.Methods.Method;

namespace Pharos.Server.Parameters.Application.Commands.ImportMethodsAndTests;

/// <summary>
/// Handler for updating method parameters in the database from imported methods and tests
/// </summary>
internal class UpdateMethodParameterHandler
{
    private readonly IParametersDbContext _dbContext;
    private readonly ILogger<UpdateMethodParameterHandler> _logger;
    private HashSet<int>? _methodTestIdsWithLabNames;
    private const int _maxNumberOfClassTables = 20;

    public UpdateMethodParameterHandler(IParametersDbContext dbContext, ILogger<UpdateMethodParameterHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Update all domain method parameters from import
    /// </summary>
    public async Task Handle(List<Method> importedMethods, List<MethodTest> importedMethodTests)
    {
        foreach (var importedMethod in importedMethods)
        {
            try
            {
                _logger.LogInformation("Updating parameters and test for the method {MethodName}", importedMethod.MethodName);
                _methodTestIdsWithLabNames ??= await _dbContext.MethodTestLabNames
                    .Select(x => x.MethodTestId)
                    .Distinct()
                    .ToHashSetAsync();
                await UpdateMethodFromImport(importedMethod, importedMethodTests);

                await _dbContext.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
            }
            finally
            {
                importedMethodTests.RemoveAll(x => x.Method.MethodName == importedMethod.MethodName);
            }
        }

        // now we import tests without methods
        var existingMethodsNames = importedMethodTests.Select(x => x.Method.MethodName).Distinct().ToList();
        foreach (var methodName in existingMethodsNames)
        {
            _logger.LogInformation("Updating tests for an existing method {MethodName}", methodName);
            var method = await GetExistingMethod(methodName);
            if (method == null)
            {
                throw new InvalidOperationException($"There is not {methodName} method in the database. Importing tests is impossible");
            }

            UpdateMethodTestsFromImport(method, importedMethodTests);

            await _dbContext.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
        }

        await HandlePhadiaQualityControls(importedMethods);

        await _dbContext.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
    }
    
    #region Method Parameters

    private async Task UpdateMethodFromImport(Method importedMethod, List<MethodTest> importedTests)
    {
        var isNewMethod = false;
        var method = await GetExistingMethod(importedMethod.MethodName);
        if (method == null)
        {
            isNewMethod = true;
            method = CreateNewMethod(importedMethod.MethodName);
        }

        if (method.MethodVersion > importedMethod.MethodVersion)
        {
            _logger.LogInformation("Skipping import of method {MethodName}, since the existing method has a higher method version {MethodVersion}", importedMethod.MethodName, method.MethodVersion);
            return;
        }

        UpdateMethodParametersFromImport(method, importedMethod, importedTests, isNewMethod);
    }

    private Task<Method?> GetExistingMethod(string methodName)
    {
        return _dbContext.Methods
            .Where(m => m.MethodName == methodName)
            .Include(m => m.AnalyticalRun)
            .Include(m => m.Calculation)
            .Include(m => m.CalibrationCurve)
            .ThenInclude(m => m.CalibrationPoints)
            .Include(m => m.ClassTableBelows)
            .Include(m => m.ClassTables)
            .Include(m => m.ConcentrationUnits)
            .Include(m => m.CurveControls)
            .Include(m => m.Presentation)
            .Include(m => m.Reagents)
            .Include(m => m.Reporting)
            .Include(m => m.Tests)
            .AsSplitQuery()
            .FirstOrDefaultAsync();
    }

    private Method CreateNewMethod(string methodName)
    {
        var method = new Method
        {
            MethodName = methodName,

            AnalyticalRun = new MethodAnalyticalRun(),
            Calculation = new MethodCalculation(),
            CalibrationCurve = new MethodCalibrationCurve
            {
                CalibrationPoints = new List<MethodCalibrationPoint>()
            },
            ClassTableBelows = new List<MethodClassTableBelow>(),
            ClassTables = new List<MethodClassTable>(),
            ConcentrationUnits = new List<MethodConcentrationUnit>(),
            CurveControls = new List<MethodCurveControl>(),
            Presentation = new MethodPresentation(),
            Reagents = new List<MethodReagent>(),
            Reporting = new MethodReporting(),
            Tests = new List<MethodTest>()
        };
        _dbContext.Methods.Add(method);

        return method;
    }

    private void UpdateMethodParametersFromImport(
        Method method,
        Method importedMethod,
        List<MethodTest> importedTests,
        bool isNewMethod)
    {
        method.FullMethodName = importedMethod.FullMethodName;
        method.MethodVersion = importedMethod.MethodVersion;
        method.IsRuo = importedMethod.IsRuo;
        method.IsSpecialMethod = importedMethod.IsSpecialMethod;

        // If the method is new and is either RUO or a special method, we disable it by default
        if (isNewMethod && (method.IsRuo || method.IsSpecialMethod))
        {
            method.Enabled = false;
        }

        method.IsTemplate = importedMethod.IsTemplate;
        method.Technology = importedMethod.Technology;
        method.PreventDilutionFactorsBelowDefault = importedMethod.PreventDilutionFactorsBelowDefault;
        method.CalibratorPipettingOrder = importedMethod.CalibratorPipettingOrder;
        method.CalibratorPiercingOrder = importedMethod.CalibratorPiercingOrder;
        method.SortOrder = importedMethod.SortOrder;
        method.DefaultConcentrationFactor = importedMethod.DefaultConcentrationFactor;
        method.Allow50PercentWash = importedMethod.Allow50PercentWash;
        method.AllowDilutionInPipette = importedMethod.AllowDilutionInPipette;
        method.SubtractBlank = importedMethod.SubtractBlank;

        method.AnalyticalRun.DefaultAllowMonthlyCalibration = importedMethod.AnalyticalRun.DefaultAllowMonthlyCalibration;
        method.AnalyticalRun.DefaultMaxAnalyticalRunSize = importedMethod.AnalyticalRun.DefaultMaxAnalyticalRunSize;
        method.AnalyticalRun.RunCalibrationOnCarrierLotChange = importedMethod.AnalyticalRun.RunCalibrationOnCarrierLotChange;

        method.Calculation.CalculationType = importedMethod.Calculation.CalculationType;
        method.Calculation.CalibratorsForLeastSquareCalc = importedMethod.Calculation.CalibratorsForLeastSquareCalc;
        method.Calculation.CalibratorStartingPoint = importedMethod.Calculation.CalibratorStartingPoint;
        method.Calculation.MinCoefficientOfCorrelation = importedMethod.Calculation.MinCoefficientOfCorrelation;
        method.Calculation.PredictedVarianceA = importedMethod.Calculation.PredictedVarianceA;
        method.Calculation.PredictedVarianceB = importedMethod.Calculation.PredictedVarianceB;
        method.Calculation.RobustWeight = importedMethod.Calculation.RobustWeight;
        method.Calculation.NormalWeight = importedMethod.Calculation.NormalWeight;

        method.Presentation.XAxisScale = importedMethod.Presentation.XAxisScale;
        method.Presentation.XAxisText = importedMethod.Presentation.XAxisText;
        method.Presentation.XAxisUnit = importedMethod.Presentation.XAxisUnit;
        method.Presentation.YAxisScale = importedMethod.Presentation.YAxisScale;
        method.Presentation.YAxisText = importedMethod.Presentation.YAxisText;
        method.Presentation.YAxisUnit = importedMethod.Presentation.YAxisUnit;
        method.Presentation.FixedYAxis = importedMethod.Presentation.FixedYAxis;
        method.Presentation.FixedYAxisLowerLimit = importedMethod.Presentation.FixedYAxisLowerLimit;
        method.Presentation.FixedYAxisUpperLimit = importedMethod.Presentation.FixedYAxisUpperLimit;

        method.Reporting.DefaultTextAboveCutOff = importedMethod.Reporting.DefaultTextAboveCutOff;
        method.Reporting.DefaultTextBelowCutOff = importedMethod.Reporting.DefaultTextBelowCutOff;
        method.Reporting.DefaultTextQuantitativeAbove = importedMethod.Reporting.DefaultTextQuantitativeAbove;
        method.Reporting.DefaultTextQuantitativeBelow = importedMethod.Reporting.DefaultTextQuantitativeBelow;
        method.Reporting.DefaultTextQuantitativeDoubtful = importedMethod.Reporting.DefaultTextQuantitativeDoubtful;
        method.Reporting.DefaultMinConcentrationToReport = importedMethod.Reporting.DefaultMinConcentrationToReport;
        method.Reporting.DefaultUseCalculatedTextQuantitativeAbove = importedMethod.Reporting.DefaultUseCalculatedTextQuantitativeAbove;

        UpdateMethodReagentsFromImport(method.Reagents, importedMethod.Reagents);

        UpdateMethodConcentrationUnitsFromImport(method.ConcentrationUnits, importedMethod.ConcentrationUnits);

        UpdateMethodClassTablesFromImport(method.ClassTables, importedMethod.ClassTables);

        UpdateMethodClassTableBelowsFromImport(method.ClassTableBelows, importedMethod.ClassTableBelows);

        UpdateMethodTestsFromImport(method, importedTests);

        UpdateMethodCurveControlsFromImport(method, importedMethod);

        UpdateMethodCalibrationCurveFromImport(method, importedMethod);
    }

    private void UpdateMethodReagentsFromImport(IList<MethodReagent> reagents, IList<MethodReagent> importedReagents)
    {
        // remove reagents that are not in the method file
        for (var i = 0; i < reagents.Count;)
        {
            if (importedReagents.Any(x => x.ReagentType == reagents[i].ReagentType))
            {
                i++;
            }
            else
            {
                _dbContext.MethodReagents.Remove(reagents[i]);
                reagents.RemoveAt(i);
            }
        }

        // add or update reagents in the database
        foreach (var importedReagent in importedReagents)
        {
            var reagent = reagents.FirstOrDefault(x => x.ReagentType == importedReagent.ReagentType);
            if (reagent == null)
            {
                _dbContext.MethodReagents.Add(importedReagent);
                reagents.Add(importedReagent);
                continue;
            }
            reagent.DefaultReagentIdentity = importedReagent.DefaultReagentIdentity;
            reagent.Volume = importedReagent.Volume;
        }
    }

    private void UpdateMethodConcentrationUnitsFromImport(IList<MethodConcentrationUnit> methodUnits, IList<MethodConcentrationUnit> importedMethodUnits)
    {
        for (var i = 0; i < methodUnits.Count;)
        {
            if (importedMethodUnits.Any(x => x.UnitId == methodUnits[i].UnitId))
            {
                i++;
            }
            else
            {
                _dbContext.MethodConcentrationUnits.Remove(methodUnits[i]);
                methodUnits.RemoveAt(i);
            }
        }

        foreach (var importedUnit in importedMethodUnits)
        {
            var unit = methodUnits.FirstOrDefault(x => x.UnitId == importedUnit.UnitId);
            if (unit == null)
            {
                _dbContext.MethodConcentrationUnits.Add(importedUnit);
                methodUnits.Add(importedUnit);
            }
            else
            {
                unit.ConcentrationUnit = importedUnit.ConcentrationUnit;
            }
        }
    }

    private void UpdateMethodClassTablesFromImport(IList<MethodClassTable> methodClassTables, IList<MethodClassTable> importedMethodClassTables)
    {
        foreach (var importedClassTable in importedMethodClassTables)
        {
            if (importedClassTable.ClassTableId > _maxNumberOfClassTables)
            {
                continue;
            }

            var classTable = methodClassTables
                .FirstOrDefault(x => x.ClassTableId == importedClassTable.ClassTableId && x.ClassTableRowId == importedClassTable.ClassTableRowId);

            if (classTable == null)
            {
                _dbContext.MethodClassTables.Add(importedClassTable);
                methodClassTables.Add(importedClassTable);
                continue;
            }

            classTable.Concentration = importedClassTable.Concentration;
            classTable.DefaultTextAboveConcentration = importedClassTable.DefaultTextAboveConcentration;
        }

        // remove classes that are not in the method file
        for (var i = 0; i < methodClassTables.Count;)
        {
            if (importedMethodClassTables.Any(x => x.ClassTableId == methodClassTables[i].ClassTableId && x.ClassTableRowId == methodClassTables[i].ClassTableRowId))
            {
                i++;
            }
            else
            {
                _dbContext.MethodClassTables.Remove(methodClassTables[i]);
                methodClassTables.RemoveAt(i);
            }
        }
    }

    private void UpdateMethodClassTableBelowsFromImport(IList<MethodClassTableBelow> methodClassTables, IList<MethodClassTableBelow> importedMethodClassTables)
    {
        foreach (var importedClassTable in importedMethodClassTables)
        {
            if (importedClassTable.ClassTableId > _maxNumberOfClassTables)
            {
                continue;
            }

            var classTable = methodClassTables.FirstOrDefault(x => x.ClassTableId == importedClassTable.ClassTableId);
            if (classTable == null)
            {
                _dbContext.MethodClassTableBelows.Add(importedClassTable);
                methodClassTables.Add(importedClassTable);
                continue;
            }

            classTable.DefaultTextBelowConcentration = importedClassTable.DefaultTextBelowConcentration;
        }

        // remove classes that are not in the method file
        for (var i = 0; i < methodClassTables.Count;)
        {
            if (importedMethodClassTables.Any(x => x.ClassTableId == methodClassTables[i].ClassTableId))
            {
                i++;
            }
            else
            {
                _dbContext.MethodClassTableBelows.Remove(methodClassTables[i]);
                methodClassTables.RemoveAt(i);
            }
        }
    }

    #endregion

    #region Method Tests

    private void UpdateMethodTestsFromImport(Method method, List<MethodTest> allImportedTests)
    {
        var importedMethodTests = allImportedTests.Where(x => x.Method.MethodName == method.MethodName).ToList();

        if (importedMethodTests.Count == 0)
        {
            // We are importing the method file only, we skip the test update.
            // This can be ok for non-empty database.
            return;
        }

        // remove method tests that are not in the test parameters file
        for (var i = 0; i < method.Tests.Count;)
        {
            var test = method.Tests[i];
            if (importedMethodTests.Any(x => x.TestName == test.TestName))
            {
                i++;
            }
            else
            {
                _dbContext.MethodTests.Remove(test);
                method.Tests.RemoveAt(i);
            }
        }

        foreach (var importedTest in importedMethodTests)
        {
            var isNewTest = false;
            var test = method.Tests.FirstOrDefault(x => x.TestName == importedTest.TestName);
            if (test == null)
            {
                isNewTest = true;
                test = new MethodTest
                {
                    TestName = importedTest.TestName,
                    LabTestNames = new List<MethodTestLabName>(),
                };
                method.Tests.Add(test);
                _dbContext.MethodTests.Add(test);
            }
            UpdateMethodTestParametersFromImport(test, importedTest, method, isNewTest);
        }
    }

    private void UpdateMethodTestParametersFromImport(
        MethodTest test,
        MethodTest importedTest,
        Method method,
        bool isNewTest)
    {
        test.FullTestName = importedTest.FullTestName;
        test.DefaultReplicates = importedTest.DefaultReplicates;
        test.DefaultReportConcentration = importedTest.DefaultReportConcentration;
        test.DefaultReportClassId = importedTest.DefaultReportClassId;
        test.DefaultReportCutOff = importedTest.DefaultReportCutOff;
        test.DefaultReportQuotient = importedTest.DefaultReportQuotient;
        test.DefaultMethodConcentrationUnitId = importedTest.DefaultMethodConcentrationUnitId;
        test.HighestConcentrationToReport = importedTest.HighestConcentrationToReport;
        test.NumberOfDecimalsForConcentrationBelow10 = importedTest.NumberOfDecimalsForConcentrationBelow10;
        test.UseLotSpecificCode = importedTest.UseLotSpecificCode;
        test.DefaultDilutionFactor = importedTest.DefaultDilutionFactor;
        test.DefaultCutOffConcentration = importedTest.DefaultCutOffConcentration;
        test.DefaultLabTestName = importedTest.DefaultLabTestName;
        test.DefaultDilutionMethod = importedTest.DefaultDilutionMethod;
        test.DiluentIdentity = importedTest.DiluentIdentity;
        test.DefaultEmphasizeOnReports = importedTest.DefaultEmphasizeOnReports;
        test.DefaultTextQuantitativeAboveForDilutedTest = importedTest.DefaultTextQuantitativeAboveForDilutedTest;
        test.DefaultTextQuantitativeBelowForDilutedTest = importedTest.DefaultTextQuantitativeBelowForDilutedTest;
        test.ReportConcentrationAboveCurveFlag = importedTest.ReportConcentrationAboveCurveFlag;
        test.ReportConcentrationBelowCurveFlag = importedTest.ReportConcentrationBelowCurveFlag;
        test.NumberOfDecimalsForConcentrationBetween10And100 = importedTest.NumberOfDecimalsForConcentrationBetween10And100;
        test.NumberOfDecimalsForConcentrationAbove100 = importedTest.NumberOfDecimalsForConcentrationAbove100;
        test.MixOfAllergens = importedTest.MixOfAllergens;
        if (importedTest.Priority != null)
        {
            test.Priority = importedTest.Priority;
        }
        test.IterativeAlfaBeta1Beta2Flag = importedTest.IterativeAlfaBeta1Beta2Flag;
        test.AdjustmentForAlfaBeta1Beta2 = importedTest.AdjustmentForAlfaBeta1Beta2;
        test.SpecialMethodName = importedTest.SpecialMethodName;
        test.Disabled = importedTest.Disabled;
        test.ConcentrationFactor = importedTest.ConcentrationFactor;
        test.LowerDetectionLimit = importedTest.LowerDetectionLimit;
        test.DevelopmentSolutionIdentity = importedTest.DevelopmentSolutionIdentity;
        test.IsRuo = importedTest.IsRuo;

        // If the test is new and its method or the test itself is RUO, or it has a special method name, disable it by default.
        //TODO: This logic should be revisited when we decide how we populate IsRuo flag for RUO method tests
        if (isNewTest && (method.IsRuo || test.IsRuo || !string.IsNullOrEmpty(test.SpecialMethodName)))
        {
            test.Active = false;
        }

        if (!_methodTestIdsWithLabNames?.Contains(test.Id) ?? false)
        {
            // add the default name to the list of the laboratory test name
            _dbContext.MethodTestLabNames.Add(new MethodTestLabName
            {
                TestName = importedTest.DefaultLabTestName,
                SortOrder = 1,
                MethodTest = test
            });
        }
    }

    #endregion

    #region Curve Controls

    private void UpdateMethodCurveControlsFromImport(Method method, Method importedMethod)
    {
        // curve controls are not editable by user, so we just replace them
        foreach (var cc in method.CurveControls)
        {
            _dbContext.MethodCurveControls.Remove(cc);
        }

        foreach (var cc in importedMethod.CurveControls)
        {
            var test = method.Tests.First(x => x.TestName == cc.MethodTest.TestName);
            cc.MethodTest = test;
            method.CurveControls.Add(cc);
            _dbContext.MethodCurveControls.Add(cc);
        }
    }

    #endregion

    #region Calibration Curve

    private void UpdateMethodCalibrationCurveFromImport(Method method, Method importedMethod)
    {
        method.CalibrationCurve.DefaultMaxNumberOfDays = importedMethod.CalibrationCurve.DefaultMaxNumberOfDays;
        method.CalibrationCurve.MaxBadReplicates = importedMethod.CalibrationCurve.MaxBadReplicates;
        method.CalibrationCurve.QuotientIndex = importedMethod.CalibrationCurve.QuotientIndex;
        method.CalibrationCurve.ExcludeLowestCalibratorFromMeanDiff = importedMethod.CalibrationCurve.ExcludeLowestCalibratorFromMeanDiff;
        method.CalibrationCurve.ReportLowRuForZeroCalibrators = importedMethod.CalibrationCurve.ReportLowRuForZeroCalibrators;
        method.CalibrationCurve.MethodTest = method.Tests.First(x => x.TestName == importedMethod.CalibrationCurve.MethodTest.TestName);

        UpdateMethodCalibrationPointsFromImport(method.CalibrationCurve.CalibrationPoints, importedMethod.CalibrationCurve.CalibrationPoints);
    }

    private void UpdateMethodCalibrationPointsFromImport(IList<MethodCalibrationPoint> calibrationPoints, IList<MethodCalibrationPoint> importedCalibrationPoints)
    {
        // calibration point are only editable by R&D, so we just replace them
        foreach (var point in calibrationPoints)
        {
            _dbContext.MethodCalibrationPoints.Remove(point);
        }

        foreach (var point in importedCalibrationPoints)
        {
            calibrationPoints.Add(point);
            _dbContext.MethodCalibrationPoints.Add(point);
        }
    }

    #endregion

    #region Quality Controls
    
    private async Task HandlePhadiaQualityControls(List<Method> importedMethods)
    {
        foreach (var importedMethod in importedMethods)
        {
            foreach (var qc in importedMethod.PhadiaQualityControlTestsForParser)
            {
                qc.MethodName = importedMethod.MethodName;
            }
        }

        var importedQcs = importedMethods.SelectMany(m => m.PhadiaQualityControlTestsForParser).GroupBy(x => x.QualityControlName).ToList();

        foreach (var importedQc in importedQcs)
        {
            var existingQc = await GetExistingQc(importedQc.Key) ?? await CreateNewQc(importedQc.Key);
            
            foreach (var qc in importedQc)
            {
                var test = await GetExistingTest(importedQc.Key, qc.TestName, qc.MethodName) ?? await CreateNewTest(existingQc, qc.TestName, qc.MethodName);
                if (test != null)
                {
                    MapQcTestValues(test, qc);
                }
            }
        }

        // remove QC tests that are not in the method file
        foreach (var method in importedMethods)
        {
            var qcTests = await _dbContext.PhadiaQualityControlTests
                .Include(x => x.QualityControl)
                .Include(x => x.Test)
                .ThenInclude(x => x.Method)
                .Where(x => x.Test.Method.MethodName == method.MethodName)
                .ToListAsync();

            foreach (var qcTest in qcTests)
            {
                var existsInImportedMethod = method.PhadiaQualityControlTestsForParser.Any(x => qcTest.QualityControl.Name == x.QualityControlName && x.TestName == qcTest.Test.TestName && x.MethodName == qcTest.Test.Method.MethodName);
                if(!existsInImportedMethod)
                {
                    // QC test does not exist in the imported method, remove it
                    _dbContext.PhadiaQualityControlTests.Remove(qcTest);
                }
            }
        }
    }

    private static void MapQcTestValues(PhadiaQualityControlTest test, PhadiaQualityControlTestForParser qc)
    {
        //map values from the parameter file
        test.QualityControlType = qc.QualityControlType;
        test.PreDilutionFactor = qc.PreDilutionFactor;
        test.InstrumentDilutionFactor = qc.InstrumentDilutionFactor ?? 1;

        test.SortOrder = qc.SortOrder;
    }

    private async Task<PhadiaQualityControlTest?> GetExistingTest(string qualityControlName, string testName, string methodName)
    {
        return await _dbContext.PhadiaQualityControlTests
            .Include(x => x.QualityControl)
            .Include(x => x.Test)
            .ThenInclude(x => x.Method)
            .Where(x => x.QualityControl.Name == qualityControlName && x.Test.TestName == testName && x.Test.Method.MethodName == methodName)
            .FirstOrDefaultAsync();
    }

    private async Task<PhadiaQualityControlTest?> CreateNewTest(PhadiaQualityControl existingQc, string testName, string methodName)
    {
        var testId = await GetTestId(testName, methodName);
        if (testId is null)
        {
            return null;
        }

        var qcTest = new PhadiaQualityControlTest
        {
            Enabled = false,
            ApplyAcceptanceRange = false,
            InstrumentDilutionFactor = 1,
            MaxCv = 1,
            NumberOfSd = 1,
            NumberOfTests = 5,
            PreDilutionFactor = 1,
            QualityControlType = QualityControlType.Concentration,
            ReplicateCvLimit = 0,
            Replicates = 1,
            UseAcceptanceRange = false,
            UseLotQualification = false,
            TestId = testId.Value,
            Schedule = QualityControlSchedule.Default()
        };

        existingQc.Tests.Add(qcTest);
        return qcTest;
    }

    private async Task<int?> GetTestId(string testName, string methodName)
    {
        return await _dbContext.MethodTests
            .Include(x => x.Method)
            .Where(x => x.TestName == testName && x.Method.MethodName == methodName)
            .AsNoTracking()
            .Select(x => x.Id).FirstOrDefaultAsync();
    }

    private async Task<PhadiaQualityControl> CreateNewQc(string qualityControlName)
    {
        var isQcInTube = await IsQcInTube(qualityControlName);

        var qc = new PhadiaQualityControl
        {
            Name = qualityControlName,
            IsQcInTube = isQcInTube,
            Tests = new List<BaseQualityControlTest>()
        };

        _dbContext.PhadiaQualityControl.Add(qc);

        return qc;
    }

    private async Task<bool> IsQcInTube(string qualityControlName)
    {
        var isTubeQc = false;
        var article = await _dbContext.Articles.Where(x => x.Identity == qualityControlName).FirstOrDefaultAsync();

        if (article != null)
        {
            isTubeQc = article.ArticleType == ArticleType.QualityControlTube;
        }

        return isTubeQc;
    }

    private async Task<PhadiaQualityControl?> GetExistingQc(string qualityControlName)
    {
        var qc = await _dbContext.PhadiaQualityControl
            .Where(x => x.Name == qualityControlName)
            .FirstOrDefaultAsync();

        //if QC exists
        if (qc != null)
        {
            //check if the qc type has changed
            var isQcInTube = await IsQcInTube(qualityControlName);
            if (qc.IsQcInTube != isQcInTube)
            {
                qc.IsQcInTube = isQcInTube;
            }

            // if QC is not tube QC, make sure that TubeId is set to 0
            if (!qc.IsQcInTube)
            {
                qc.TubeTypeId = 0;
            }
        }

        return qc;
    }

    #endregion

}
