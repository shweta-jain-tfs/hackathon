using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Parameters.Application.Common;
using Pharos.Server.Parameters.Application.Interfaces;
using Pharos.Server.Parameters.Application.Parsers;
using Pharos.Server.Parameters.Application.Parsers.ValueTypeParsers;
using Pharos.Server.Parameters.Domain;

namespace Pharos.Server.Parameters.Application.Commands.ImportMethodsAndTests;

internal class MethodTestParametersHandler(
    IParameterFileContentParser parameterFileContentParser,
    IParameterFileParser parameterFileParser) : ParameterHandler<List<MethodTest>>(parameterFileContentParser, parameterFileParser)
{
    protected override List<MethodTest> HandleFile(ParameterFile file)
    {
        var dataParser = new List<ParameterPropertyParserBase<List<MethodTest>>>();

        var methodTests = new List<MethodTest>();

        var methodName = string.Empty;

        var sectionParsers = new List<ParameterSectionParser<List<MethodTest>>>
                             {
                                 new("[FILE]", true, (_, parameters) => HandleFileSection(file.Name, parameters, MethodUpdateConstants.TestFileType, MethodUpdateConstants.TestFileFormatVersion, out var _, false), null),
                                 new("[INFORMATION]", true, (f, parameters) => HandleMethodParameter(parameters, out methodName), null),
                                 new("[TESTS]", true, (_, parameters)=> GenerateParsers(parameters, methodName, dataParser), dataParser)
                             };

        ParseFileWithSectionParsers(file, sectionParsers, methodTests);

        return methodTests;
    }

    private static void GenerateParsers(List<ParameterProperty> parameters, string methodName, List<ParameterPropertyParserBase<List<MethodTest>>> dataParser)
    {
        foreach (var parameter in parameters)
        {
            var testName = parameter.Key;

            var methodTestObjectParser = new PropertyObjectParser<List<MethodTest>, MethodTest>(
                testName,
                true,
                [
                    new ParameterArrayValueParser<MethodTest, string>(
                        0,
                        false,
                        (model, value) => model.FullTestName = value,
                        new StringParser()),

                    new ParameterArrayValueParser<MethodTest, int>(
                        1,
                        false,
                        (model, value) => model.DefaultReplicates = value,
                        new IntParser()),

                    new ParameterArrayValueParser<MethodTest, bool>(
                        2,
                        false,
                        (model, value) => model.DefaultReportConcentration = value,
                        new BoolParser(BoolStrategy.ZeroAsFalseOneAsTrue)),

                    new ParameterArrayValueParser<MethodTest, int>(
                        3,
                        false,
                        (model, value) => model.DefaultReportClassId = value,
                        new IntParser()),

                    new ParameterArrayValueParser<MethodTest, bool>(
                        4,
                        false,
                        (model, value) => model.DefaultReportCutOff = value,
                        new BoolParser(BoolStrategy.ZeroAsFalseOneAsTrue)),

                    new ParameterArrayValueParser<MethodTest, QuotientReportMode>(
                        5,
                        false,
                        (model, value) => model.DefaultReportQuotient = value,
                        new EnumParser<QuotientReportMode>()),

                    // In the test parameters concentration unit ID has value 0..9 (not 1..10).
                    // Thus, we add 1 to the value.
                    new ParameterArrayValueParser<MethodTest, int>(
                        6,
                        false,
                        (model, value) => model.DefaultMethodConcentrationUnitId = value + 1,
                        new IntParser()),

                    new ParameterArrayValueParser<MethodTest, double>(
                        7,
                        false,
                        (model, value) => model.HighestConcentrationToReport = value,
                        new DoubleParser(3)),

                    new ParameterArrayValueParser<MethodTest, int>(
                        8,
                        false,
                        (model, value) => model.NumberOfDecimalsForConcentrationBelow10 = value,
                        new IntParser()),

                    new ParameterArrayValueParser<MethodTest, bool>(
                        9,
                        false,
                        (model, value) => model.UseLotSpecificCode = value,
                        new BoolParser(BoolStrategy.ZeroAsFalseOneAsTrue)),

                    new ParameterArrayValueParser<MethodTest, int>(
                        10,
                        false,
                        (model, value) => model.DefaultDilutionFactor = value,
                        new IntParser()),

                    new ParameterArrayValueParser<MethodTest, double>(
                        11,
                        false,
                        (model, value) => model.DefaultCutOffConcentration = value,
                        new DoubleParser(3)),

                    new ParameterArrayValueParser<MethodTest, string>(
                        12,
                        false,
                        (model, value) => model.DefaultLabTestName = value,
                        new StringParser()),

                    new ParameterArrayValueParser<MethodTest, DilutionMethod>(
                        13,
                        false,
                        (model, value) => model.DefaultDilutionMethod = value,
                        new EnumParser<DilutionMethod>()),

                    new ParameterArrayValueParser<MethodTest, string>(
                        14,
                        false,
                        (model, value) => model.DiluentIdentity = value,
                        new StringParser()),

                    new ParameterArrayValueParser<MethodTest, bool>(
                        15,
                        false,
                        (model, value) => model.DefaultEmphasizeOnReports = value,
                        new BoolParser(BoolStrategy.ZeroAsFalseOneAsTrue)),

                    new ParameterArrayValueParser<MethodTest, string>(
                        16,
                        false,
                        (model, value) => model.DefaultTextQuantitativeBelowForDilutedTest = value,
                        new StringParser()),

                    new ParameterArrayValueParser<MethodTest, string>(
                        17,
                        false,
                        (model, value) => model.DefaultTextQuantitativeAboveForDilutedTest = value,
                        new StringParser()),

                    new ParameterArrayValueParser<MethodTest, bool>(
                        18,
                        false,
                        (model, value) => model.ReportConcentrationAboveCurveFlag = value,
                        new BoolParser(BoolStrategy.ZeroAsFalseOneAsTrue)),

                    new ParameterArrayValueParser<MethodTest, bool>(
                        19,
                        false,
                        (model, value) => model.ReportConcentrationBelowCurveFlag = value,
                        new BoolParser(BoolStrategy.ZeroAsFalseOneAsTrue)),

                    new ParameterArrayValueParser<MethodTest, int>(
                        20,
                        false,
                        new ParserDefault<int>(2, true),
                        (model, value) => model.NumberOfDecimalsForConcentrationAbove100 = value,
                        new IntParser()),

                    new ParameterArrayValueParser<MethodTest, int>(
                        21,
                        false,
                        new ParserDefault<int>(2, true),
                        (model, value) => model.NumberOfDecimalsForConcentrationBetween10And100 = value,
                        new IntParser()),

                    new ParameterArrayValueParser<MethodTest, bool>(
                        22,
                        false,
                        (model, value) => model.MixOfAllergens = value,
                        new BoolParser(BoolStrategy.ZeroAsFalseOneAsTrue)),

                    new ParameterArrayValueParser<MethodTest, int>(
                        23,
                        false,
                        (model, value) =>
                        {
                            model.Priority ??= new MethodTestPriority();
                            model.Priority.OrderInPriorityGroupA = value;
                        },
                        new IntParser()),

                    new ParameterArrayValueParser<MethodTest, int>(
                        24,
                        false,
                        (model, value) =>
                        {
                            model.Priority ??= new MethodTestPriority();
                            model.Priority.OrderInPriorityGroupB = value;
                        },
                        new IntParser()),

                    new ParameterArrayValueParser<MethodTest, bool>(
                        25,
                        false,
                        (model, value) => model.IterativeAlfaBeta1Beta2Flag = value,
                        new BoolParser(BoolStrategy.ZeroAsFalseOneAsTrue)),

                    new ParameterArrayValueParser<MethodTest, double>(
                        26,
                        false,
                        new ParserDefault<double>(1, true),
                        (model, value) => model.AdjustmentForAlfaBeta1Beta2 = value,
                        new DoubleParser(5)),


                    new ParameterArrayValueParser<MethodTest, bool>(
                        27,
                        false,
                        (model, value) =>
                        {
                            model.Priority ??= new MethodTestPriority();
                            model.Priority.IncludeInPriorityGroupA = value;
                        },
                        new BoolParser(BoolStrategy.ZeroAsFalseOneAsTrue)),


                    new ParameterArrayValueParser<MethodTest, bool>(
                        28,
                        false,
                        (model, value) =>
                        {
                            model.Priority ??= new MethodTestPriority();
                            model.Priority.IncludeInPriorityGroupB = value;
                        },
                        new BoolParser(BoolStrategy.ZeroAsFalseOneAsTrue)),

                    new ParameterArrayValueParser<MethodTest, string>(
                        29,
                        false,
                        (model, value) => model.SpecialMethodName = value,
                        new StringParser()),

                    new ParameterArrayValueParser<MethodTest, bool>(
                        30,
                        false,
                        (model, value) => model.Disabled = value,
                        new BoolParser(BoolStrategy.ZeroAsFalseOneAsTrue)),

                    new ParameterArrayValueParser<MethodTest, double>(
                        31,
                        false,
                        (model, value) => model.ConcentrationFactor = value,
                        new DoubleParser(5)),

                    new ParameterArrayValueParser<MethodTest, double>(
                        32,
                        false,
                        (model, value) => model.LowerDetectionLimit = value,
                        new DoubleParser(3)),

                    new ParameterArrayValueParser<MethodTest, string>(
                        33,
                        false,
                        (model, value) => model.DevelopmentSolutionIdentity = value,
                        new StringParser()),

                    new ParameterArrayValueParser<MethodTest, bool>(
                        34,
                        false,
                        (model, value) => model.IsRuo = value,
                        new BoolParser(BoolStrategy.ZeroAsFalseOneAsTrue))
                ],
                (mParent, mObject) =>
                {
                    mObject.TestName = testName;
                    mObject.Method = new Method { MethodName = methodName };
                    mParent.Add(mObject);
                });

            dataParser.Add(methodTestObjectParser);
        }
    }

    private static void HandleMethodParameter(List<ParameterProperty> parameters, out string methodName)
    {
        methodName = string.Empty;
        foreach (var parameter in parameters)
        {
            if (parameter.Key == "Method")
            {
                methodName = parameter.Value;
            }
        }
    }
}
