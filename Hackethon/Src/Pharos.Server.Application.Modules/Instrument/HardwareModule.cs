namespace Pharos.Server.Application.Instrument;

public record HardwareModule
{
    public ModuleType ModuleType { get; init; }
    public string? ArticleNumber { get; init; }
    public string? Model { get; init; }
    public string? Version { get; init; }
    public string? SerialNumber { get; init; }
    public DateTimeOffset? InstallationDate { get; init; }
}

public enum ModuleType
{
    UnspecifiedModuleType = 0,
    IswService = 1,
    UBoard = 2,
    BarcodeReader = 3,
}