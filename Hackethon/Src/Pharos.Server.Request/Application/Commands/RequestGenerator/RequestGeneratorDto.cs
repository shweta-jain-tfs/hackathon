namespace Pharos.Server.Request.Application.Commands.RequestGenerator;

public record GenerateRequestsDto
{
    public string SampleIdHeadingCharacters { get; init; } = string.Empty;
    public int SampleIdNumberOfDigits { get; init; } = 6;
    public int SampleIdStartOffset { get; init; }
    public string SampleIdTrailingCharacters { get; init; } = string.Empty;

    public bool PlaceSamplesInTrays { get; init; }
    public int TraysStartOffset { get; init; }
    public int SamplesPerTray { get; init; } = 10;

    public int AmountToGenerate { get; init; } = 100;
    public int PreDilutionFactor { get; init; } = 100;

    public List<TestDetails> Tests { get; init; } = new();
}

public record TestDetails
{
    public string MethodName { get; init; } = string.Empty;
    public string TestName { get; init; } = string.Empty;
    public int Replicates { get; init; } = 1;
    public int InstrumentDilutionFactor { get; init; } = 1;
}

public record RequestPreviewDto
{
    public int Id { get; init; }
    public string SampleId { get; init; } = string.Empty;
    public string Tests { get; init; } = string.Empty;
    public string? Tray { get; init; }
    public string? TrayPosition { get; init; }
}
