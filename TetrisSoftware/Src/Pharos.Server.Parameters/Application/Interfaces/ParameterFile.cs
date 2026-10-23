namespace Pharos.Server.Parameters.Application.Interfaces;

internal record ParameterFile(string Name, List<ParameterSection> Sections);

internal record ParameterSection(string SectionName, int SectionRowIndex)
{
    public List<ParameterProperty> Parameters { get; } = new();
}

internal record ParameterProperty(string Key, string Value)
{
    public bool HasKey => !string.IsNullOrEmpty(Key);
}