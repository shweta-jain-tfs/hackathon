using Microsoft.Extensions.Logging;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Parsers;

internal class ParameterFileParser : IParameterFileParser
{
    private readonly ILogger<ParameterFileParser> _logger;

    public ParameterFileParser(ILogger<ParameterFileParser> logger)
    {
        _logger = logger;
    }

    public void Parse<T>(ParameterFile file, List<ParameterSectionParser<T>>? sectionParsers, T model)
    {
        if (sectionParsers == null || sectionParsers.Count == 0)
        {
            return;
        }

        VerifyAllSectionsExists(sectionParsers, file.Sections, file.Name);

        foreach (var section in file.Sections)
        {

            var parser = sectionParsers.SingleOrDefault(x => x.Name == section.SectionName);

            if (parser != null)
            {
                ParseParameters(file, parser, section.Parameters, model);
            }
            else
            {
                _logger.LogError("Found unknown section in file {File}. Section name {SectionName}", file.Name, section.SectionName);
            }
        }
    }

    private void ParseParameters<T>(ParameterFile file, ParameterSectionParser<T> parameterSectionParser, List<ParameterProperty> parameters, T model)
    {
        var skipProperties = parameterSectionParser.SkipProperties ?? new List<string>();

        VerifyAllPropertiesExists(parameterSectionParser.PropertyParsers, skipProperties, parameters, file.Name);

        //invoke the parser for section
        parameterSectionParser.Parser?.Invoke(file, parameters);

        if (parameterSectionParser.PropertyParsers == null || parameterSectionParser.PropertyParsers.Count == 0)
        {
            return;
        }

        foreach (var parameterData in parameters)
        {
            if (skipProperties.Contains(parameterData.Key))
            {
                continue;
            }

            var propertyParsers = parameterSectionParser.PropertyParsers.Where(x => x.Name == parameterData.Key);

            foreach (var propertyParser in propertyParsers)
            {
                if (propertyParsers != null)
                {
                    if (parameterSectionParser.Parser != null)
                    {
                        VerifyAllArrayPositionsExists(propertyParser, parameterData, file.Name.ToString());
                    }

                    ParserProperty(file, propertyParser, parameterData, model);
                }
                else
                {
                    _logger.LogError("Found unknown parameter in section {Section}. Parameter name {ParameterName}. File {File}", parameterSectionParser.Name, parameterData.Key, file.Name);
                }
            }
        }
    }

    private static void ParserProperty<T>(ParameterFile file, ParameterPropertyParserBase<T> parser, ParameterProperty parameterData, T model)
    {
        parser.Parse(file, parameterData, model);
    }

    private void VerifyAllPropertiesExists<T>(IEnumerable<ParameterPropertyParserBase<T>>? propertyParsers, List<string> skipProperties, List<ParameterProperty> dataSections, string fileName)
    {
        if (propertyParsers == null)
        {
            return;
        }

        var hasToExistsList = propertyParsers.Where(x => x.HasToExist).ToList();
        if (hasToExistsList.Count == 0)
        {
            return;
        }

        var missing = VerifyAllExists(hasToExistsList.Select(x => x.Name).Except(skipProperties), dataSections.Select(x => x.Key).ToList(), fileName);

        if (missing.Count > 0)
        {
            _logger.LogError("Could not find required parameter(s) {Required} in parameter file {Name}", string.Join(",", missing), fileName);
            throw new InvalidDataException("Missing one or many required properties in section: " + string.Join(",", missing));
        }
    }

    private void VerifyAllArrayPositionsExists<T>(ParameterPropertyParserBase<T> parser, ParameterProperty parameterProperty, string fileName)
    {
        var lengthOfParameterArray = parameterProperty.Value.Split(";").Length;

        if (parser.LengthOfRequiredArray > lengthOfParameterArray)
        {
            _logger.LogError("Length of required array {Required} does not match length of given array in file {Name}", parser.Name, fileName);
            throw new InvalidDataException("Missing required array properties in property with key:" + parameterProperty.Key);
        }
    }

    private void VerifyAllSectionsExists<T>(IEnumerable<ParameterSectionParser<T>> sectionsParsers, List<ParameterSection> sections, string fileName)
    {
        var missing = VerifyAllExists(sectionsParsers.Where(x => x.HasToExist).Select(x => x.Name), sections.Select(x => x.SectionName).ToList(), fileName);

        if (missing.Count > 0)
        {
            _logger.LogError("Could not find required section(s) {Required} in parameter file {Name}", string.Join(",", missing), fileName);
            throw new InvalidDataException("Missing one or many required sections" + string.Join(",", missing));
        }
    }

    private static List<string> VerifyAllExists(IEnumerable<string>? requiredItems, List<string> existingItems, string fileName)
    {
        var missing = new List<string>();

        if (requiredItems == null)
        {
            return missing;
        }

        var list = requiredItems.ToList();
        if (list.Count == 0)
        {
            return missing;
        }

        foreach (var required in list)
        {
            var exists = existingItems.FirstOrDefault(x => x == required) != null;
            if (!exists)
            {
                switch (required)
                {
                    //IS-E does not have any calibrator points
                    case "Point" when fileName == "IS-E.mth":
                        break;
                    default:
                        missing.Add(required);
                        break;
                }
            }
        }

        return missing;
    }
}