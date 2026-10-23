using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Parameters.Application.Common;
using Pharos.Server.Parameters.Application.Interfaces;
using Pharos.Server.Parameters.Application.Parsers;
using Pharos.Server.Parameters.Application.Parsers.ValueTypeParsers;
using Pharos.Server.Parameters.Domain;

namespace Pharos.Server.Parameters.Application.Commands.ImportMethodsAndTests;

internal class MethodParametersHandler(
    IParameterFileContentParser parameterFileContentParser,
    IParameterFileParser parameterFileParser) : ParameterHandler<Method>(parameterFileContentParser, parameterFileParser)
{
    protected override Method HandleFile(ParameterFile file)
    {
        var method = new Method
        {
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
            PhadiaQualityControlTestsForParser = new List<PhadiaQualityControlTestForParser>(),
            Presentation = new MethodPresentation(),
            Reagents = new List<MethodReagent>(),
            Reporting = new MethodReporting(),
        };

        var sectionParsers = new List<ParameterSectionParser<Method>>
        {
            new("[FILE]",
                true,
                (_, parameters) => HandleFileSection(file.Name, parameters, MethodUpdateConstants.MethodFileType, MethodUpdateConstants.MethodFileFormatVersion, out var _), null),

            new("[METHOD]", true, null, new List<ParameterPropertyParserBase<Method>>
            {
                new PropertyParser<Method>("Name", true, new ParameterValueParser<Method,string>(false, (model, value) => model.FullMethodName = value, new StringParser())),
                new PropertyParser<Method>("AbbreviatedName", true, new ParameterValueParser<Method,string>(false, (model, value) => model.MethodName = value, new StringParser())),
                new PropertyParser<Method>("MethodVersion", true, new ParameterValueParser<Method,int>(false, (model, value) => model.MethodVersion = value, new IntParser())),
                new PropertyParser<Method>("TemplateFlag", true, new ParameterValueParser<Method,bool>(false, (model, value) => model.IsTemplate = value, new BoolParser(BoolStrategy.ZeroAsFalseOneAsTrue))),
                new PropertyParser<Method>("Default", true, new ParameterValueParser<Method,bool>(false, (model, value) => model.IsUserDefined = !value, new BoolParser(BoolStrategy.ZeroAsFalseOneAsTrue))),
                new PropertyParser<Method>("SequenceNumber", true, new ParameterValueParser<Method,AssayTechnology>(false, (model, value) => model.Technology = value, new EnumParser<AssayTechnology>())),
                new PropertyParser<Method>("InstrumentDilutionLocked", true,  new ParameterValueParser<Method,bool>(false, (model, value) => model.PreventDilutionFactorsBelowDefault = value, new BoolParser(BoolStrategy.ZeroAsFalseOneAsTrue))),
                new PropertyParser<Method>("AllocationOrder", true, new ParameterValueParser<Method,CalibratorOrder>( true, (model, value) => model.CalibratorPipettingOrder = value, new EnumParser<CalibratorOrder>())),
                new PropertyParser<Method>("CalibratorPiercingOrder", false, new ParameterValueParser<Method,CalibratorOrder>( true, (model, value) => model.CalibratorPiercingOrder = value, new EnumParser<CalibratorOrder>())),
                new PropertyParser<Method>("SortOrder", true, new ParameterValueParser<Method,int>( false, (model, value) => model.SortOrder = value, new IntParser())),
                new PropertyParser<Method>("DefaultConcentrationFactor", true, new ParameterValueParser<Method,double>(false, (model,value) => model.DefaultConcentrationFactor = value, new DoubleParser(5))),
                new PropertyParser<Method>("SpecialMethod", false, new ParameterValueParser<Method,bool>(false, (model,value) => model.IsSpecialMethod = value, new BoolParser(BoolStrategy.ZeroAsFalseOneAsTrue))),
                new PropertyParser<Method>("RUO", true,  new ParameterValueParser<Method,bool>( false, (model, value) => model.IsRuo = value, new BoolParser(BoolStrategy.ZeroAsFalseOneAsTrue))),
                new PropertyParser<Method>("Allow50percentWash", false,  new ParameterValueParser<Method,bool>( false, (model, value) => model.Allow50PercentWash = value, new BoolParser(BoolStrategy.ZeroAsFalseOneAsTrue))),
                new PropertyParser<Method>("AllowDilutionInPipette", false, new ParameterValueParser<Method,bool>(false, (model, value) => model.AllowDilutionInPipette = value, new BoolParser(BoolStrategy.ZeroAsFalseOneAsTrue)))
            }),

            new("[REAGENTS]", true, null, new List<ParameterPropertyParserBase<Method>>
            {
                new PropertyParser<Method>(
                    "Sample",
                    true,
                    new ParameterArrayValueParser<Method,int>(
                        0,
                        false,
                        (model, value) => model.Reagents.Add(new MethodReagent { ReagentType = ReagentType.Sample, Volume = value}),
                        new IntParser())),

                new PropertyObjectParser<Method, MethodReagent>(
                    "Conjugate",
                    false,
                    [
                        new ParameterArrayValueParser<MethodReagent, int>(0, false, (model, value) => model.Volume = value, new IntParser()),
                        new ParameterArrayValueParser<MethodReagent, string>(1, false, (model, value) => model.DefaultReagentIdentity = value, new StringParser())
                    ],
                    (mParent, mObject) => {
                        mObject.ReagentType = ReagentType.Conjugate;
                        mParent.Reagents.Add(mObject);
                    }),

                new PropertyParser<Method>(
                    "DevSol",
                    true,
                    new ParameterArrayValueParser<Method,int>(
                        0,
                        false,
                        (model, value) => model.Reagents.Add(new MethodReagent { ReagentType = ReagentType.DevelopmentSolution, Volume = value }),
                        new IntParser())),


                new PropertyParser<Method>(
                    "StopSol",
                    true,
                    new ParameterArrayValueParser<Method,int>(
                        0,
                        false,
                        (model, value) => model.Reagents.Add(new MethodReagent { ReagentType = ReagentType.StopSolution, Volume = value }),
                        new IntParser())),

                new PropertyParser<Method>(
                    "WashSolConc",
                    true,
                    new ParameterValueParser<Method,string>(
                        false,
                        (model, value) => model.Reagents.Add(new MethodReagent { ReagentType = ReagentType.WashSolutionConcentrate, DefaultReagentIdentity = value }),
                        new StringParser())),

                new PropertyParser<Method>(
                    "WashSolAdd",
                    false,
                    new ParameterValueParser<Method,string>(
                        false,
                        (model, value) => model.Reagents.Add(new MethodReagent { ReagentType = ReagentType.WashAdditive, DefaultReagentIdentity = value }),
                        new StringParser())),
            }),

            new("[CALCONTROL]", false,  null, new List<ParameterPropertyParserBase<Method>>
            {
                new PropertyObjectParser<Method, MethodCurveControl>(
                    "Point",
                    false,
                    [
                        new ParameterArrayValueParser<MethodCurveControl, bool>(0, false, (model, value) => model.Active = value, new BoolParser(BoolStrategy.ZeroAsFalseOneAsTrue)),
                        new ParameterArrayValueParser<MethodCurveControl, string>(1, false, (model, value) => model.CurveControlName = value, new StringParser()),
                        new ParameterArrayValueParser<MethodCurveControl, string>(2, false, (model, value) => model.MethodTest = new MethodTest
                        {
                            TestName = value
                        }, new StringParser()),
                        new ParameterArrayValueParser<MethodCurveControl, int>(3, false, (model, value) => model.Replicates = value, new IntParser()),
                        new ParameterArrayValueParser<MethodCurveControl, double>(4, false, (model, value) => model.ExpectedConcentration = value, new DoubleParser(3)),
                        new ParameterArrayValueParser<MethodCurveControl, double>(5, false, (model, value) => model.ExpectedStdDev = value, new DoubleParser(3)),
                        new ParameterArrayValueParser<MethodCurveControl, double>(6, false, (model, value) => model.InnerStdDevLimit = value, new DoubleParser(2)),
                        new ParameterArrayValueParser<MethodCurveControl, double>(7, false, (model, value) => model.OuterStdDevLimit = value, new DoubleParser(2)),
                        new ParameterArrayValueParser<MethodCurveControl, double>(8, false, (model, value) => model.AdjustmentForAlfaBeta1Beta2 = value, new DoubleParser(5))
                    ],
                (mParent, mObject) => mParent.CurveControls.Add(mObject))
            }),

            new("[CALCURVE]", true, null, new List<ParameterPropertyParserBase<Method>>
            {
                new PropertyParser<Method>("TestName", true, new ParameterValueParser<Method,string>(false, (model, value) => model.CalibrationCurve.MethodTest = new MethodTest { TestName = value }, new StringParser())),
                new PropertyArrayParser<Method>("AgeUntilWarning", true, [new ParameterArrayValueParser<Method, int>(0, false, (model, value) => model.CalibrationCurve.DefaultMaxNumberOfDays = value, new IntParser())]),
                new PropertyParser<Method>("MaxBadReplicates", true, new ParameterValueParser<Method,int>(false, (model, value) => model.CalibrationCurve.MaxBadReplicates = value, new IntParser())),
                new PropertyParser<Method>("ReportLowRUZeroPoint", true,new ParameterValueParser<Method,bool>(false, (model, value) => model.CalibrationCurve.ReportLowRuForZeroCalibrators = value, new BoolParser(BoolStrategy.ZeroAsFalseRestAsTrue))),
                new PropertyParser<Method>("QuotientIndex", true, new ParameterValueParser<Method,int>(false, (model, value) => model.CalibrationCurve.QuotientIndex = value, new IntParser())),
                new PropertyParser<Method>("ExcludeLowestInMeanDiff", true, new ParameterValueParser<Method,bool>(false, (model, value) => model.CalibrationCurve.ExcludeLowestCalibratorFromMeanDiff = value, new BoolParser(BoolStrategy.ZeroAsFalseRestAsTrue))),

                new PropertyObjectParser<Method, MethodCalibrationPoint>(
                    "Point",
                    true,
                    [
                        new ParameterArrayValueParser<MethodCalibrationPoint, string>(0, false, (model, value) => model.CalibratorName = value, new StringParser()),
                        new ParameterArrayValueParser<MethodCalibrationPoint, double>(1, false, (model, value) => model.ExpectedConcentration = value, new DoubleParser(3)),
                        new ParameterArrayValueParser<MethodCalibrationPoint, int>(2, false, (model, value) => model.Replicates = value, new IntParser()),
                        new ParameterArrayValueParser<MethodCalibrationPoint, double>(3, true, new ParserDefault<double>(1, true), (model, value) => model.AdjustmentForAlfaBeta1Beta2 = value, new DoubleParser(5)),
                        new ParameterArrayValueParser<MethodCalibrationPoint, double>(4, false, (model, value) => model.IdealResponse = value, new DoubleParser(1)),
                        new ParameterArrayValueParser<MethodCalibrationPoint, double>(5, false, (model, value) => model.ExpectedStdDev = value, new DoubleParser(1)),
                        new ParameterArrayValueParser<MethodCalibrationPoint, double>(6, false, (model, value) => model.UpperResponseLimit = value, new DoubleParser(1)),
                        new ParameterArrayValueParser<MethodCalibrationPoint, double>(7, false, (model, value) => model.LowerResponseLimit = value, new DoubleParser(1)),
                        new ParameterArrayValueParser<MethodCalibrationPoint, double>(8, false, (model, value) => model.StdDevPrecisionLimit = value, new DoubleParser(2)),
                        new ParameterArrayValueParser<MethodCalibrationPoint, double>(9, false, (model, value) => model.StdDevLimit = value, new DoubleParser(2)),
                        new ParameterArrayValueParser<MethodCalibrationPoint, double>(10, false, (model, value) => model.UpperLogarithmicLimit = value, new DoubleParser(3)),
                        new ParameterArrayValueParser<MethodCalibrationPoint, double>(11, false, (model, value) => model.LowerLogarithmicLimit = value, new DoubleParser(3))
                    ],
                    (mParent, mObject) => mParent.CalibrationCurve.CalibrationPoints.Add(mObject)),
            }),

            new("[BLANK]", true, null, new List<ParameterPropertyParserBase<Method>>
            {
                new PropertyParser<Method>("Subtract", false, new ParameterValueParser<Method,bool>( false, (model, value) => model.SubtractBlank = value, new BoolParser(BoolStrategy.ZeroAsFalseOneAsTrue)))
            }),

            new("[PRESENTATION]", true,null, new List<ParameterPropertyParserBase<Method>>
            {
                new PropertyParser<Method>("XaxisText", true, new ParameterValueParser<Method,string>(false ,(model,value) => model.Presentation.XAxisText = value, new StringParser())),
                new PropertyParser<Method>("XaxisUnit", true, new ParameterValueParser<Method,string>(false ,(model,value) => model.Presentation.XAxisUnit = value, new StringParser())),
                new PropertyParser<Method>("XaxisScale", true, new ParameterValueParser<Method, AxisScale>(false, (model, value) => model.Presentation.XAxisScale = value, new EnumParser<AxisScale>())),
                new PropertyParser<Method>("YaxisText", true, new ParameterValueParser<Method, string>(false ,(model, value) => model.Presentation.YAxisText = value, new StringParser())),
                new PropertyParser<Method>("YaxisUnit", true, new ParameterValueParser<Method, string>(false ,(model, value) => model.Presentation.YAxisUnit = value, new StringParser())),
                new PropertyParser<Method>("YaxisScale", true, new ParameterValueParser<Method, AxisScale>(false, (model, value) => model.Presentation.YAxisScale = value, new EnumParser<AxisScale>())),
                new PropertyArrayParser<Method>("FixedYScale", true, new List<ParameterArrayParser<Method>>
                {
                    new ParameterArrayValueParser<Method,bool>(0, false ,(model,value) => model.Presentation.FixedYAxis = value, new BoolParser(BoolStrategy.ZeroAsFalseOneAsTrue)),
                    new ParameterArrayValueParser<Method,double>(1, false ,(model,value) => model.Presentation.FixedYAxisLowerLimit = value, new DoubleParser(1)),
                    new ParameterArrayValueParser<Method,double>(2, false ,(model,value) => model.Presentation.FixedYAxisUpperLimit = value, new DoubleParser(1))
                })
            }),

            new("[CALCULATION]", true, null, new List<ParameterPropertyParserBase<Method>>
            {
                new PropertyParser<Method>("Type", true, new ParameterValueParser<Method,CalculationType>(false, (model, value) => model.Calculation.CalculationType = value, new EnumParser<CalculationType>())),
                new PropertyParser<Method>("NormalWeight", true, new ParameterValueParser<Method,NormalWeight>(false, (model, value) => model.Calculation.NormalWeight = value, new EnumParser<NormalWeight>())),
                new PropertyArrayParser<Method>("PredictedVariance", true, new List<ParameterArrayParser<Method>>{
                            new ParameterArrayValueParser<Method,double>(0, false, (model, value) => model.Calculation.PredictedVarianceA = value, new DoubleParser(6)),
                            new ParameterArrayValueParser<Method,double>(1, false, (model, value) => model.Calculation.PredictedVarianceB = value, new DoubleParser(6))}),
                new PropertyParser<Method>("RobustWeight", true, new ParameterValueParser<Method,RobustWeight>(false, (model, value) => model.Calculation.RobustWeight = value, new EnumParser<RobustWeight>())),
                new PropertyParser<Method>("MinCoeffCorr", true, new ParameterValueParser<Method,double>(false, (model, value) => model.Calculation.MinCoefficientOfCorrelation = value, new DoubleParser(5))),
                new PropertyArrayParser<Method>("ZeroPoint", true, new List<ParameterArrayParser<Method>>{
                            new ParameterArrayValueParser<Method,int>(0, false, (model, value) => model.Calculation.CalibratorStartingPoint = value, new IntParser()),
                            new ParameterArrayValueParser<Method,int>(1, false, (model, value) => model.Calculation.CalibratorsForLeastSquareCalc = value, new IntParser()) }),
                new PropertyParser<Method>("LowestConc", true, new ParameterValueParser<Method,double>(false, (model, value) => model.Reporting.DefaultMinConcentrationToReport = value, new DoubleParser(3))),
            }),

            new("[CUTOFF]", true, null, new List<ParameterPropertyParserBase<Method>>
            {
                new PropertyParser<Method>("TextBelow", true, new ParameterValueParser<Method,string>(false, (model, value) => model.Reporting.DefaultTextBelowCutOff = value, new StringParser())),
                new PropertyParser<Method>("TextAbove", true, new ParameterValueParser<Method,string>(false, (model, value) => model.Reporting.DefaultTextAboveCutOff = value, new StringParser()))
            }),

            new("[REFERENCECURVE]", true, null, new List<ParameterPropertyParserBase<Method>>
            {
                new PropertyParser<Method>("QuantBelow", true, new ParameterValueParser<Method,string>(false, (model, value) => model.Reporting.DefaultTextQuantitativeBelow = value, new StringParser())),
                new PropertyParser<Method>("UseCalculatedTextQuantitiveAbove", true, new ParameterValueParser<Method,bool>(false, (model, value) => model.Reporting.DefaultUseCalculatedTextQuantitativeAbove = value, new BoolParser(BoolStrategy.ZeroAsFalseRestAsTrue))),
                new PropertyParser<Method>("QuantAbove", true, new ParameterValueParser<Method,string>(false, (model, value) => model.Reporting.DefaultTextQuantitativeAbove = value, new StringParser())),
                new PropertyParser<Method>("QuantDoubtful", true, new ParameterValueParser<Method,string>(false, (model, value) => model.Reporting.DefaultTextQuantitativeDoubtful = value, new StringParser())),

                new PropertyObjectParser<Method, MethodClassTableBelow>(
                    "TextBelow",
                    true,
                    [new ParameterArrayValueParser<MethodClassTableBelow, string>(0, false, (model, value) => model.DefaultTextBelowConcentration = value, new StringParser())],
                    (mParent, mObject) =>
                    {
                        mObject.ClassTableId = 1;
                        mParent.ClassTableBelows.Add(mObject);
                    }),

                new PropertyObjectParser<Method, MethodClassTable >(
                    "Class",
                    true,
                    [
                        new ParameterArrayValueParser<MethodClassTable, double>(0, false, (model, value) => model.Concentration = value, new DoubleParser(3)),
                        new ParameterArrayValueParser<MethodClassTable, string>(1, false, (model, value) => model.DefaultTextAboveConcentration = value, new StringParser())
                    ],
                    (mParent, mObject) =>
                    {
                        var rowI = mParent.ClassTables.Where(x=>x.ClassTableId==1).Select(x=>x).ToList().Count + 1;
                        mObject.ClassTableId = 1;
                        mObject.ClassTableRowId = rowI;
                        mParent.ClassTables.Add(mObject);
                    }),

                new PropertyObjectParser<Method, MethodConcentrationUnitUsedForParseOnly>(
                    "ConcUnit",
                    true,
                    [new ParameterArrayValueParser<MethodConcentrationUnitUsedForParseOnly, string>(0, false, (model, value) => model.ConcentrationUnitAsString = value, new StringParser(), true)],
                    (mParent, mObject) =>
                    {
                        var units = mObject.ConcentrationUnitAsString.Split(";");
                        var unitId = 1;
                        foreach (var unit in units)
                        {
                            mParent.ConcentrationUnits.Add(new MethodConcentrationUnit{ConcentrationUnit=unit, UnitId = unitId});
                            unitId++;
                        }
                    })
            }),

            // TODO: check the method parameters spec
            //new("[UNKNOWNS]", true, null, new List<ParameterPropertyParserBase<Method>>
            //{
            //    new PropertyParser<Method>("CVlimit", true,new ParameterValueParser<Method,double>( false, (model, value) => model.CVlimit = value, new DoubleParser(1)))
            //}),

            new("[INTERNALQC]", false, null, new List<ParameterPropertyParserBase<Method>>
            {
                new PropertyObjectParser<Method, PhadiaQualityControlTestForParser>(
                    "Point",
                    false,
                    [
                        new ParameterArrayValueParser<PhadiaQualityControlTestForParser, string>(0, false, (model, value) => model.QualityControlName = value, new StringParser()),
                        new ParameterArrayValueParser<PhadiaQualityControlTestForParser, string>(1, false, (model, value) => model.TestName = value, new StringParser()),
                        new ParameterArrayValueParser<PhadiaQualityControlTestForParser, int>(2, false, (model, value) => model.PreDilutionFactor = value, new IntParser()),
                        new ParameterArrayValueParser<PhadiaQualityControlTestForParser, QualityControlType>(3, false, (model, value) => model.QualityControlType = value, new EnumParser<QualityControlType>()),
                        new ParameterArrayValueParser<PhadiaQualityControlTestForParser, int>(4, false, (model, value) => model.InstrumentDilutionFactor = value, new IntParser())
                    ],
                    (mParent, mObject) =>
                    {
                        mObject.SortOrder = mParent.PhadiaQualityControlTestsForParser.Count;
                        mParent.PhadiaQualityControlTestsForParser.Add(mObject);
                    })
            }),

            new("[ANALYTICALRUN]", true, null, new List<ParameterPropertyParserBase<Method>>
            {
                new PropertyParser<Method>("MaxAnalyticalRunSize", true, new ParameterValueParser<Method,int>(false, (model, value) => model.AnalyticalRun.DefaultMaxAnalyticalRunSize = value, new IntParser())),
                new PropertyParser<Method>("AllowMonthlyCalibration", true,new ParameterValueParser<Method,bool>(false, (model, value) => model.AnalyticalRun.DefaultAllowMonthlyCalibration = value, new BoolParser(BoolStrategy.ZeroAsFalseOneAsTrue))),
                new PropertyParser<Method>("ConnectCarrierAndConjugateLot", true, new ParameterValueParser<Method,bool>(false, (model, value) => model.AnalyticalRun.RunCalibrationOnCarrierLotChange = value, new BoolParser(BoolStrategy.ZeroAsFalseOneAsTrue))),
            }),
        };

        AddManySectionParser("CLASS", 2, 20, false, ref sectionParsers);

        ParseFileWithSectionParsers(file, sectionParsers, method);

        return method;
    }

    private static void AddManySectionParser(string sectionName, int firstIndex, int lastIndex, bool addWithIndex1, ref List<ParameterSectionParser<Method>> sectionParsers)
    {
        for (var i = firstIndex; i <= lastIndex; i++)
        {
            var arrIndex = new List<int> { i };
            if (addWithIndex1)
            {
                arrIndex.Insert(0, 1);
            }

            foreach (var ind in arrIndex)
            {
                var pppBaseList = new List<ParameterPropertyParserBase<Method>>();

                var classP1 = new PropertyObjectParser<Method, MethodClassTableBelow>(
                    "TextBelow",
                    true,
                    [
                        new ParameterValueParser<MethodClassTableBelow, string>(
                            false,
                            (model, value) => model.DefaultTextBelowConcentration = value, new StringParser())
                    ],
                    (mParent, mObject) =>
                    {
                        mObject.ClassTableId = ind;
                        mParent.ClassTableBelows.Add(mObject);
                    });

                var classP2 = new PropertyObjectParser<Method, MethodClassTable>(
                    "Class",
                    true,
                    [
                        new ParameterArrayValueParser<MethodClassTable, double>(0, false, (model, value) => model.Concentration = value, new DoubleParser(3)),
                        new ParameterArrayValueParser<MethodClassTable, string>(1, false, (model, value) => model.DefaultTextAboveConcentration = value, new StringParser())
                    ],
                    (mParent, mObject) =>
                    {
                        var rowI = mParent.ClassTables.Where(x => x.ClassTableId == ind).Select(x => x).ToList().Count + 1;
                        mObject.ClassTableId = ind;
                        mObject.ClassTableRowId = rowI;
                        mParent.ClassTables.Add(mObject);
                    });

                pppBaseList.Add(classP1);
                pppBaseList.Add(classP2);

                if (addWithIndex1)
                {
                    sectionParsers.Add(new ParameterSectionParser<Method>($"[{sectionName}]", false, null, pppBaseList));
                    addWithIndex1 = false;
                }
                else
                {
                    sectionParsers.Add(new ParameterSectionParser<Method>($"[{sectionName}{ind}]", false, null, pppBaseList));
                }
            }
        }
    }
}

#pragma warning disable CS8618

internal class MethodConcentrationUnitUsedForParseOnly
{
    public string ConcentrationUnitAsString { get; set; }
}
