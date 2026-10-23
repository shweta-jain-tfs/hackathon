using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Parsers;

internal class ParameterFileContentParser : IParameterFileContentParser
{
    public ParameterFile Parse(string? fileContentAsString, string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileContentAsString))
        {
            throw new ArgumentNullException(nameof(fileContentAsString));
        }

        var sections = GetSections(fileContentAsString);
        return new ParameterFile(fileName, sections);
    }

    private static List<ParameterSection> GetSections(string content)
    {
        var sections = new List<ParameterSection>();
        var lines = content.Split(Environment.NewLine);

        ParameterSection? currentSection = null;
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                currentSection = new ParameterSection(line, i);
                sections.Add(currentSection);
            }
            else
            {
                AddSectionDataToSection(line, currentSection);
            }
        }

        return sections;
    }

    private static void AddSectionDataToSection(string line, ParameterSection? section)
    {
        if (string.IsNullOrWhiteSpace(line) || section == null)
        {
            return;
        }

        var kvp = line.Split("=");

        var key = string.Empty;
        var value = string.Empty;

        // If one value in array, set it as value
        if (kvp.Length == 1)
        {
            value = kvp[0].Trim();
        }

        // If two values in array, set the first as key and the second as value
        if (kvp.Length == 2)
        {
            key = kvp[0].Trim();
            value = kvp[1].Trim();
        }

        if (kvp.Length > 2)
        {
            throw new ArgumentOutOfRangeException(nameof(line), "Line consist of more than one equals sign.");
        }

        section.Parameters.Add(new ParameterProperty(key, value));
    }
}