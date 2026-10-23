using FluentValidation;
using Pharos.Server.Parameters.Application.Interfaces;
using Pharos.Server.Parameters.Application.Parsers;

namespace Pharos.Server.Parameters.Application.Common;

internal abstract class ParameterHandler<T>(
    IParameterFileContentParser parameterFileContentParser,
    IParameterFileParser parameterFileParser)
{
    public async Task<T> Handle(byte[] fileContent, string fileName)
    {
        var fileContentAsString = await GetFileContent(fileContent);
        var file = parameterFileContentParser.Parse(fileContentAsString, fileName);
        return HandleFile(file);
    }

    protected abstract T HandleFile(ParameterFile file);

    protected void ParseFileWithSectionParsers<T2>(ParameterFile file, List<ParameterSectionParser<T2>> sectionParsers, T2 model)
    {
        parameterFileParser.Parse(file, sectionParsers, model);
    }

    protected static void HandleFileSection(string fileName, List<ParameterProperty> parameters, string expectedFileType, string expectedFormatVersion, out int fileVersion, bool versionRequired = true)
    {
        fileVersion = 0;
        foreach (var parameter in parameters)
        {
            switch (parameter.Key)
            {
                case "Type":
                    if (!parameter.Value.Equals(expectedFileType, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new ValidationException($"Expected file type of the {fileName} file is '{expectedFileType}' but the actual is {parameter.Value}.");
                    }
                    break;

                case "FormatVersion":
                    if (!parameter.Value.Equals(expectedFormatVersion, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new ValidationException($"The file format version of the {fileName} file is not supported. The supported version is {expectedFormatVersion} and the file version is {parameter.Value}.");
                    }
                    break;

                case "Version":
                    if (!int.TryParse(parameter.Value, out fileVersion))
                    {
                        fileVersion = 0;
                    }
                    break;
            }
        }

        if (fileVersion == 0 && versionRequired)
        {
            throw new ValidationException($"The version of the {fileName} file is missing or has incorrect format.");
        }
    }

    private static async Task<string> GetFileContent(byte[] fileContent)
    {
        await using var stream = new MemoryStream(fileContent);
        var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();
        reader.Close();
        reader.Dispose();

        return content;
    }
}