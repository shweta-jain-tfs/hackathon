using Pharos.Server.Parameters.Application.Common;
using Pharos.Server.Parameters.Application.Interfaces;
using Pharos.Server.Parameters.Application.Parsers;
using Pharos.Server.Parameters.Domain;

namespace Pharos.Server.Parameters.Application.Commands.ImportVersion;

internal class VersionParametersHandler(
    IParameterFileContentParser parameterFileContentParser,
    IParameterFileParser parameterFileParser)
    : ParameterHandler<ParameterUpdatePackageVersion>(parameterFileContentParser, parameterFileParser)
{
    protected override ParameterUpdatePackageVersion HandleFile(ParameterFile file)
    {
        var result = new ParameterUpdatePackageVersion();
        var version = 0;
        var sectionParsers = new List<ParameterSectionParser<ParameterUpdatePackageVersion>>
        {
            new("[FILE]", true, (_, parameters)
                => HandleFileSection(file.Name, parameters, MethodUpdateConstants.VersionFileType,
                    MethodUpdateConstants.VersionFileFormatVersion, out version), null)
        };

        ParseFileWithSectionParsers(file, sectionParsers, result);
        result.VersionNumber = version;

        return result;
    }
}