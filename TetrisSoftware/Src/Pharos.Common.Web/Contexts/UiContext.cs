namespace Pharos.Common.Web.Contexts;

public record UiContext
{
    public required DeviceType Device { get; init; }
    public required UiComponentStrings ComponentStrings { get; init; }
}
