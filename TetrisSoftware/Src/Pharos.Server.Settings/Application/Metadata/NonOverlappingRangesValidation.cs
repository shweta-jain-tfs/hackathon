using System.Text.Json.Serialization;
using Pharos.Server.Settings.Application.Services;

namespace Pharos.Server.Settings.Application.Metadata;

public class NonOverlappingRangesValidation
{
    public string FirstStartKey { get; set; } = string.Empty;
    public string FirstLengthKey { get; set; } = string.Empty;
    public string SecondStartKey { get; set; } = string.Empty;
    public string SecondLengthKey { get; set; } = string.Empty;
    public List<Dependency> EnabledWhen { get; set; } = [];
    public string Error { get; set; } = "SectionsOverlap";

    [JsonIgnore]
    public string[] RangeKeys =>
    [
        FirstStartKey,
        FirstLengthKey,
        SecondStartKey,
        SecondLengthKey
    ];

    public NonOverlappingRangesValidation DeepCopy(string settingsKeyPrefix)
    {
        return new NonOverlappingRangesValidation
        {
            FirstStartKey = PageHelper.CopySettingKey(FirstStartKey, settingsKeyPrefix),
            FirstLengthKey = PageHelper.CopySettingKey(FirstLengthKey, settingsKeyPrefix),
            SecondStartKey = PageHelper.CopySettingKey(SecondStartKey, settingsKeyPrefix),
            SecondLengthKey = PageHelper.CopySettingKey(SecondLengthKey, settingsKeyPrefix),
            EnabledWhen = CopyEnabledWhenDependencies(settingsKeyPrefix),
            Error = Error
        };
    }

    private List<Dependency> CopyEnabledWhenDependencies(string settingsKeyPrefix)
    {
        return string.IsNullOrEmpty(settingsKeyPrefix)
            ? EnabledWhen.Select(dependency => new Dependency
            {
                Type = dependency.Type,
                MainOptionKey = dependency.MainOptionKey,
                MainOptionValue = dependency.MainOptionValue
            }).ToList()
            : PageHelper.CopyDependency(EnabledWhen, settingsKeyPrefix);
    }
}
