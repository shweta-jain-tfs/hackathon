namespace Pharos.Client.App.ServiceTools.Services;

public class RequestGeneratorState
{
    public string SampleIdHeadingCharacters { get; set; } = string.Empty;
    public int SampleIdNumberOfDigits { get; set; }
    public int SampleIdStartOffset { get; set; }
    public string SampleIdTrailingCharacters { get; set; } = string.Empty;
    public bool PlaceSamplesInTrays { get; set; }
    public int TraysStartOffset { get; set; }
    public int SamplesPerTray { get; set; }
    public int AmountToGenerate { get; set; }
    public int PreDilutionFactor { get; set; }
}
