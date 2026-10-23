using System.Text;
using Pharos.Client.App.Logs.Models.Errors;
using Pharos.Client.Common.Models.Notifications;
using Pharos.Common.Web.Components.Status;

namespace Pharos.Client.App.Logs.ViewModels.Errors;

public class SystemErrorDetailsViewModel : EntityEditorViewModel<SystemErrorDetails>
{
    private readonly ISystemErrorDetailsDataService _dataService;
    private readonly IErrorTranslator _translator;
    private static readonly CompositeFormat _errorImageFormat = CompositeFormat.Parse("_content/Pharos.Client.App.Logs/Images/SystemError_{0}.svg");
    private static readonly HashSet<int> _supportedErrorImageCodes = [1, 1000, 1001, 1010];

    public SystemErrorDetailsViewModel(ISystemErrorDetailsDataService dataService, IErrorTranslator translator, IEntityNotificationProvider<SystemErrorDetails> notificationProvider) : base(dataService, notificationProvider)
    {
        _dataService = dataService;
        _translator = translator;
    }

    protected override async Task DoLoadAsync(int entityId)
    {
        await base.DoLoadAsync(entityId);

        Title = _translator.GetTitle(Code);
        StatusType = _translator.GetStatusType(Classification);

        var parameters = Entity.Parameters ?? [];
        Content = _translator.GetContent(Code, parameters.ToArray());

        ErrorImagePath = _supportedErrorImageCodes.Contains(Code)
            ? string.Format(null, _errorImageFormat, Code)
            : null;

        var hasResponded = Entity.Response.HasValue;
        ResponseButtonsDisabled = hasResponded;
        HasResponded = hasResponded;
    }

    public string Title { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public StatusType StatusType { get; set; }

    public int Code => Entity.Code;
    public ErrorClassification Classification => Entity.Classification;
    public DateTimeOffset Timestamp => Entity.Timestamp;
    public string InstrumentName => Entity.InstrumentName;
    public ResponseAction? Response => Entity.Response;
    public DateTimeOffset? ResponseTimestamp => Entity.ResponseTimestamp;
    public ICollection<ResponseAction> PossibleResponses => Entity.PossibleResponses ?? [];

    public string ClassificationText => _translator.GetErrorLevel(Classification);

    public string ResponseActionText => _translator.GetResponseActionText(Response);

    public string ResponseText
    {
        get
        {
            var hasPossibleResponses = PossibleResponses.Count > 0;
            return _translator.GetResponseStatusText(hasPossibleResponses, Response);
        }
    }

    public string GetResponseButtonText(ResponseAction responseAction)
    {
        return _translator.GetResponseButtonText(responseAction);
    }

    public string? ErrorImagePath { get; private set; }
    public bool ResponseButtonsDisabled { get; private set; } = true;
    public bool HasResponded { get; private set; }
    public Func<Task>? OnResponseSubmitted { get; set; }

    public async Task RespondToError(ResponseAction responseAction)
    {
        var responseTimestamp = await _dataService.RespondToError(Entity.Id, responseAction);

        Entity.Response = responseAction;
        Entity.ResponseTimestamp = responseTimestamp;
        ResponseButtonsDisabled = true;
        HasResponded = true;

        await (OnResponseSubmitted?.Invoke() ?? Task.CompletedTask);
    }
}
