namespace Pharos.Server.Parameters.Application.Interfaces;

internal interface IParameterFileContentParser
{
    ParameterFile Parse(string fileContentAsString, string fileName);
}