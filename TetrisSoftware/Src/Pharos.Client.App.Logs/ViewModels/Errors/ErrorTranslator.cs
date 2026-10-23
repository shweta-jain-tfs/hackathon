using Pharos.Common.Web.Components.Status;

namespace Pharos.Client.App.Logs.ViewModels.Errors;

public interface IErrorTranslator
{
    string GetTitle(int errorCode);
    string GetContent(int errorCode, params object[] parameters);
    string GetResponseStatusText(bool hasPossibleResponses, ResponseAction? responseAction);
    string GetResponseButtonText(ResponseAction responseAction);
    string GetErrorLevel(ErrorClassification classification);
    StatusType GetStatusType(ErrorClassification classification);
    string GetResponseActionText(ResponseAction? responseAction);
}

public class ErrorTranslator : IErrorTranslator
{
    private readonly IStringLocalizer<Translations> _localization;
    private const string _translationPrefix = "SystemError_";
    private const string _titleSuffix = "_Title";
    private const string _contentSuffix = "_Content";
    private const string _unknown = "Unknown";
    private const string _unknownContent = $"{_translationPrefix}{_unknown}{_contentSuffix}";
    private const string _unknownTitle = $"{_translationPrefix}{_unknown}{_titleSuffix}";
    private const string _responseActionButtonPrefix = "SystemErrorResponseButton";
    private const string _responseActionStatusPrefix = "SystemErrorResponseStatus";

    public ErrorTranslator(IStringLocalizer<Translations> localization)
    {
        _localization = localization;
    }

    public string GetTitle(int errorCode)
    {
        return GetTranslation($"{_translationPrefix}{errorCode}{_titleSuffix}", _unknownTitle);
    }

    public string GetContent(int errorCode, params object[] parameters)
    {
        return GetTranslation($"{_translationPrefix}{errorCode}{_contentSuffix}", _unknownContent, parameters);
    }

    private string GetTranslation(string key, string unknownKey, params object[] parameters)
    {
        LocalizedString translatedValue;

        if (parameters.Length == 0)
        {
            translatedValue = _localization.GetString(key);
        }
        else
        {
            try
            {
                translatedValue = _localization.GetString(key, parameters);
            }
            catch (FormatException)
            {
                translatedValue = _localization.GetString(key);
            }
        }

        if (translatedValue.ResourceNotFound)
        {
            translatedValue = _localization.GetString(unknownKey);
        }

        return translatedValue;
    }

    public string GetResponseStatusText(bool hasPossibleResponses, ResponseAction? responseAction)
    {
        if (!hasPossibleResponses)
        {
            return _localization["SystemErrorsPage_NotApplicable"];
        }

        if (!responseAction.HasValue)
        {
            return string.Empty;
        }

        return responseAction.Value.ToLocalizedString(_responseActionStatusPrefix);
    }

    public string GetResponseButtonText(ResponseAction responseAction)
    {
        return responseAction.ToLocalizedString(_responseActionButtonPrefix);
    }

    public string GetResponseActionText(ResponseAction? responseAction)
    {
        return responseAction.HasValue
            ? responseAction.Value.ToLocalizedString(_responseActionStatusPrefix)
            : string.Empty;
    }

    public string GetErrorLevel(ErrorClassification classification)
    {
        return classification.ToLocalizedString("SystemErrorsPage");
    }

    public StatusType GetStatusType(ErrorClassification classification)
    {
        return classification switch
        {
            ErrorClassification.Fatal => StatusType.Alarm,
            ErrorClassification.Error => StatusType.Alarm,
            ErrorClassification.Warning => StatusType.Warning,
            ErrorClassification.Information => StatusType.Info,
            _ => StatusType.None
        };
    }
}
