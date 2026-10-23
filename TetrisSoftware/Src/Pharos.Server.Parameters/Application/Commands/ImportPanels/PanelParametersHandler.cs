using FluentValidation;
using Pharos.Server.Domain.Parameters;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Parameters.Application.Common;
using Pharos.Server.Parameters.Application.Interfaces;
using Pharos.Server.Parameters.Application.Parsers;
using Pharos.Server.Parameters.Application.Parsers.ValueTypeParsers;
using Pharos.Server.Parameters.Domain;

namespace Pharos.Server.Parameters.Application.Commands.ImportPanels;

internal class PanelParametersHandler : ParameterHandler<List<Panel>>
{
    private string _currentPanel;
    private readonly IValidator<Panel> _validator;

    public PanelParametersHandler(IParameterFileContentParser parameterFileContentParser, IParameterFileParser parameterFileParser, IValidator<Panel> validator) : base(parameterFileContentParser, parameterFileParser)
    {
        _validator = validator;
        _currentPanel = string.Empty;
    }

    protected override List<Panel> HandleFile(ParameterFile file)
    {
        var result = new List<Panel>();
        var sectionParsers = new List<ParameterSectionParser<List<Panel>>>
                             {
                                 new("[FILE]", true, (_, parameters) => HandleFileSection(file.Name, parameters, MethodUpdateConstants.PanelsFileType, MethodUpdateConstants.PanelsFileFormatVersion, out var _), null),

                                 new("[TESTPANELS]", true, null,
                                 new List<ParameterPropertyParserBase<List<Panel>>>
                                 {
                                     new PropertyObjectParser<List<Panel>, Panel>("", true, [
                                         new ParameterArrayValueParser<Panel, string>(0, true, (model, value) => model.PanelName = value, new StringParser()),
                                         new ParameterArrayValueParser<Panel, string>(1, false, (model, value) => model.PanelLongName = value, new StringParser()),
                                         new ParameterArrayValueParser<Panel, bool>(5, false, (model, value) => model.GroupedPanel = value, new BoolParser(BoolStrategy.ZeroAsFalseOneAsTrue))
                                     ], (panels, mObject) =>
                                        {
                                            _currentPanel = mObject.PanelName;
                                            var p = panels.FirstOrDefault(x => x.PanelName == mObject.PanelName);
                                            if (p == null)
                                            {
                                                mObject.Tests = new List<PanelTest>();
                                                panels.Add(mObject);
                                            }
                                        }),

                                      new PropertyObjectParser<List<Panel>, PanelTest>("", true, [
                                          new ParameterArrayValueParser<PanelTest, string>(2, true, (model, value) => model.MethodTest = new MethodTest
                                          {
                                              TestName = value
                                          }, new StringParser()),
                                          new ParameterArrayValueParser<PanelTest, string>(3, true, (model, value) => model.MethodTest.Method = new Method
                                          {
                                              MethodName = value
                                          }, new StringParser()),
                                          new ParameterArrayValueParser<PanelTest, int>(4, false, (model, value) => model.Replicates = value, new IntParser())

                                      ], (panels, mObject) =>
                                        {
                                            var pp = panels.Find(x=> x.PanelName == _currentPanel);

                                            pp?.Tests.Add(mObject);
                                        })
                                 }),
                                 new("[END]", true, null, null)
                             };

        ParseFileWithSectionParsers(file, sectionParsers, result);

        foreach (var panel in result)
        {
            _validator.ValidateAndThrow(panel);
        }

        return result;
    }
}
