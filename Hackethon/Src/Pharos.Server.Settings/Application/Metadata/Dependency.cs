using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Settings.Application.Metadata;

public class Dependency
{
    public int Id { get; set; }

    public DependencyType? Type { get; set; }

    public string MainOptionKey { get; set; } = "";

    public string MainOptionValue { get; set; } = "";
}