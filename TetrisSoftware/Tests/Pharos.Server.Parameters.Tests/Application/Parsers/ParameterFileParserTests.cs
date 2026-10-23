using Microsoft.Extensions.Logging;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Parameters.Application.Interfaces;
using Pharos.Server.Parameters.Application.Parsers.ValueTypeParsers;

namespace Pharos.Server.Parameters.Application.Parsers;

public class ParameterFileParserTests
{
    private readonly Mock<ILogger<ParameterFileParser>> _loggerParameterFileParserMock = new();

    [Fact]
    public void ShouldMultipleKey()
    {
        var method = new Method
        {
            CalibrationCurve = new MethodCalibrationCurve
            {
                CalibrationPoints = new List<MethodCalibrationPoint>(),
                MethodTest = new MethodTest()
            }
        };

        var sectionList = new List<ParameterSection>();

        var pSection = new ParameterSection("[Section1]", 1);
        var p = new ParameterProperty("Point", "1;11;111");
        var p2 = new ParameterProperty("Point", "2;22;222");
        var p3 = new ParameterProperty("TestName", "3");
        var p4 = new ParameterProperty("AgeUntilWarning", "4");
        pSection.Parameters.Add(p);
        pSection.Parameters.Add(p2);
        pSection.Parameters.Add(p3);
        pSection.Parameters.Add(p4);

        var pSectionCLASS2 = new ParameterSection("[CLASSB2]", 2);
        var pClass = new ParameterProperty("TextBelow", "Negative");
        var pClass1 = new ParameterProperty("Class", "7;Eq");
        var pClass2 = new ParameterProperty("Class", "10; Positive");
        pSectionCLASS2.Parameters.Add(pClass);
        pSectionCLASS2.Parameters.Add(pClass1);
        pSectionCLASS2.Parameters.Add(pClass2);

        var pSectionCLASS3 = new ParameterSection("[CLASSB3]", 3);
        var pClass3 = new ParameterProperty("TextBelow", "Negative3");
        var pClass31 = new ParameterProperty("Class", "73;Eq3");
        var pClass32 = new ParameterProperty("Class", "103; Positive3");
        pSectionCLASS3.Parameters.Add(pClass3);
        pSectionCLASS3.Parameters.Add(pClass31);
        pSectionCLASS3.Parameters.Add(pClass32);

        //add to section list
        sectionList.Add(pSection);
        sectionList.Add(pSectionCLASS2);
        sectionList.Add(pSectionCLASS3);

        var file = new ParameterFile("fileName", sectionList);
        var pppBaseList = new List<ParameterPropertyParserBase<Method>>();

        var pppBase2 = new PropertyObjectParser<Method, MethodCalibrationPoint>("Point", true, new List<ParameterArrayParser<MethodCalibrationPoint>>
                       {
                           new ParameterArrayValueParser<MethodCalibrationPoint,string>(0, false, new ParserDefault<string>("",true), (model,value) => model.CalibratorName = value , new StringParser()),
                           new ParameterArrayValueParser<MethodCalibrationPoint,double>(1, false, (model,value) => model.ExpectedConcentration = value , new DoubleParser(3)),
                           new ParameterArrayValueParser<MethodCalibrationPoint,double>(2, false, (model,value) => model.StdDevLimit = value , new DoubleParser(1))
                       }, (m, mCalPoints) => m.CalibrationCurve.CalibrationPoints.Add(mCalPoints));


        var pppBase3 = new PropertyArrayParser<Method>("AgeUntilWarning", true, new List<ParameterArrayParser<Method>>
                       {
                           new ParameterArrayValueParser<Method,int>(0, false, (model,value) => model.CalibrationCurve.DefaultMaxNumberOfDays = value, new IntParser())
                       });

        var pppBase1 = new PropertyArrayParser<Method>("TestName", true, new List<ParameterArrayParser<Method>>
                       {
                           new ParameterArrayValueParser<Method,string>(0, false, (model,value) => model.CalibrationCurve.MethodTest.TestName = value, new StringParser())
                       });


        pppBaseList.Add(pppBase2);
        pppBaseList.Add(pppBase3);
        pppBaseList.Add(pppBase1);

        var sectionParserList = new List<ParameterSectionParser<Method>>();

        var sectionParser = new ParameterSectionParser<Method>("[Section1]", true, null, pppBaseList);


        sectionParserList.Add(sectionParser);
        AddManySectionParser("CLASSB", 2, 20, false, ref sectionParserList);



        //Parse
        var fileParser = new ParameterFileParser(_loggerParameterFileParserMock.Object);

        fileParser.Parse(file, sectionParserList, method);

        //test that elements have been set. 
        method.CalibrationCurve.CalibrationPoints.ElementAt(0).CalibratorName.Should().Be("1");
        method.CalibrationCurve.CalibrationPoints.ElementAt(1).CalibratorName.Should().Be("2");


        method.CalibrationCurve.CalibrationPoints.ElementAt(0).ExpectedConcentration.Should().Be(11);
        method.CalibrationCurve.CalibrationPoints.ElementAt(1).ExpectedConcentration.Should().Be(22);

        method.CalibrationCurve.CalibrationPoints.ElementAt(0).StdDevLimit.Should().Be(111);
        method.CalibrationCurve.CalibrationPoints.ElementAt(1).StdDevLimit.Should().Be(222);

        method.CalibrationCurve.MaxNumberOfDays.Should().Be(4);
        method.CalibrationCurve.MethodTest.TestName.Should().Be("3");
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

                switch (sectionName)
                {
                    case "CLASS":

                        var classP1 = new PropertyObjectParser<Method, MethodClassTableBelow>("TextBelow", true, new List<ParameterArrayParser<MethodClassTableBelow>>{
                                                                                                 new ParameterValueParser<MethodClassTableBelow,string>(false, (model,value) => model.DefaultTextBelowConcentration = value, new StringParser())
                                                                                             }, (mParent, mObject) =>
                                                                                                {
                                                                                                    mObject.ClassTableId = ind;
                                                                                                    mParent.ClassTableBelows.Add(mObject);
                                                                                                });
                        var classP2 = new PropertyObjectParser<Method, MethodClassTable>("Class", true, new List<ParameterArrayParser<MethodClassTable>>{
                                                                                                 new ParameterArrayValueParser<MethodClassTable,double>(0, false, (model,value) => model.Concentration = value , new DoubleParser(3)),
                                                                                                 new ParameterArrayValueParser<MethodClassTable,string>(1, false, (model,value) => model.DefaultTextAboveConcentration = value, new StringParser())
                                                                                             }, (mParent, mObject) =>
                                                                                                {
                                                                                                    var rowI = mParent.ClassTables.Where(x => x.ClassTableId == ind).Select(x => x).ToList().Count + 1;
                                                                                                    mObject.ClassTableId = ind;
                                                                                                    mObject.ClassTableRowId = rowI;
                                                                                                    mParent.ClassTables.Add(mObject);
                                                                                                });
                        pppBaseList.Add(classP1);
                        pppBaseList.Add(classP2);
                        break;
                }

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