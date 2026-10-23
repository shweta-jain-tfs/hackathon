using Pharos.Server.Parameters.Application.Parsers;

namespace Pharos.Server.Parameters.Application.Interfaces;

internal interface IParameterFileParser
{
    void Parse<T>(ParameterFile file, List<ParameterSectionParser<T>>? sectionParsers, T model);
}