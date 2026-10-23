using Pharos.Client.Common.App.Interfaces;

namespace Pharos.Client.Common.App.Services;

public class TaskCardTranslator : ITaskCardTranslator
{
    private readonly IStringLocalizer<Translations> _localization;
    private const string _translationPrefix = "TaskCard_";
    private const string _descriptionSuffix = "_Description";
    private const string _titleSuffix = "_Title";
    private const string _unknown = "Unknown";
    private const string _unknownTitle = $"{_translationPrefix}{_unknown}{_titleSuffix}";

    public TaskCardTranslator(IStringLocalizer<Translations> localization)
    {
        _localization = localization;
    }

    public string? GetDescription(TaskCardType taskCardType)
    {
        var translatedValue = _localization.GetString($"{_translationPrefix}{taskCardType}{_descriptionSuffix}");
        return translatedValue.ResourceNotFound ? null : translatedValue.Value;
    }

    public string GetTitle(TaskCardType taskCardType)
    {
        return GetTranslation($"{_translationPrefix}{taskCardType}{_titleSuffix}", _unknownTitle);
    }

    public string GetTranslation(string key, string unknownKey)
    {
        var translatedValue = _localization.GetString(key);

        if (translatedValue.ResourceNotFound)
        {
            translatedValue = _localization.GetString(unknownKey);
        }

        return translatedValue;
    }
}
