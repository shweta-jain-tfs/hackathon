using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Application.Settings.Sample;

[NonOverlappingRanges(
    nameof(SampleIdStartPosition),
    nameof(SampleIdLength),
    nameof(SampleIdSuffixStartPosition),
    nameof(SampleIdSuffixLength),
    [nameof(ExtractSampleIdFromBarcode), nameof(SampleTubesWithSameSampleId), nameof(ExtractSampleIdSuffixFromBarcode)],
    ["True", "True", "True"])]
public class SpecialSampleHandlingSettings
{
    [DisplayNameLocal("SpecialSampleHandlingSettings_ExtractSampleIdFromBarcode")]
    public bool ExtractSampleIdFromBarcode { get; set; } = false;

    [DependsFrom(nameof(ExtractSampleIdFromBarcode), "True", DependencyType.Disable)]
    [DisplayNameLocal("SpecialSampleHandlingSettings_SampleIdStartPosition")]
    [SettingsRange(1, int.MaxValue)]
    public int SampleIdStartPosition { get; set; } = 1;

    [DependsFrom(nameof(ExtractSampleIdFromBarcode), "True", DependencyType.Disable)]
    [DisplayNameLocal("SpecialSampleHandlingSettings_SampleIdLength")]
    [SettingsRange(1, int.MaxValue)]
    public int SampleIdLength { get; set; } = 1;

    [DisplayNameLocal("SpecialSampleHandlingSettings_SampleTubesWithSameSampleId")]
    public bool SampleTubesWithSameSampleId { get; set; } = false;

    [DependsFrom([nameof(ExtractSampleIdFromBarcode), nameof(SampleTubesWithSameSampleId)], ["True", "True"], DependencyType.Disable)]
    [DisplayNameLocal("SpecialSampleHandlingSettings_ExtractSampleIdSuffixFromBarcode")]
    public bool ExtractSampleIdSuffixFromBarcode { get; set; } = false; //can only be true if ExtractSampleIdFromBarcode is true and SampleTubesWithSameSampleId is true

    [DependsFrom([nameof(ExtractSampleIdFromBarcode), nameof(SampleTubesWithSameSampleId), nameof(ExtractSampleIdSuffixFromBarcode)], ["True", "True", "True"], DependencyType.Disable)]
    [DisplayNameLocal("SpecialSampleHandlingSettings_SampleIdSuffixStartPosition")]
    [SettingsRange(1, int.MaxValue)]
    public int SampleIdSuffixStartPosition { get; set; } = 1;

    [DependsFrom([nameof(ExtractSampleIdFromBarcode), nameof(SampleTubesWithSameSampleId), nameof(ExtractSampleIdSuffixFromBarcode)], ["True", "True", "True"], DependencyType.Disable)]
    [DisplayNameLocal("SpecialSampleHandlingSettings_SampleIdSuffixLength")]
    [SettingsRange(1, int.MaxValue)]
    public int SampleIdSuffixLength { get; set; } = 1;
}

public static class SpecialSampleHandlingSettingsExtensions
{
    public static bool ShowTubeId(this SpecialSampleHandlingSettings settings)
    {
        return settings is { ExtractSampleIdFromBarcode: true, SampleTubesWithSameSampleId: true, ExtractSampleIdSuffixFromBarcode: true };
    }
}