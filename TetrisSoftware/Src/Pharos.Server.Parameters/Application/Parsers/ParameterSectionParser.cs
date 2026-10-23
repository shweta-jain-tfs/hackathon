using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Parsers;

internal record ParameterSectionParser<T>(string Name, bool HasToExist, Action<ParameterFile, List<ParameterProperty>>? Parser, List<ParameterPropertyParserBase<T>>? PropertyParsers, List<string>? SkipProperties = null);