using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models.HelpContent;
using Pharos.Common.Web.Utilities;

namespace Pharos.Client.App.TaskCard.Views.TaskCard;
public partial class TaskCardHelpContent
{
    [Inject] private IHelpContentService HelpContent { get; set; } = null!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    [Parameter, EditorRequired] public TaskCardType CardType { get; set; }
    [Parameter] public string? Title { get; set; }
    [Parameter] public string? ConnectedPageTitle { get; set; }
    [Parameter] public string? ConnectedPageUri { get; set; }

    private HelpContentTaskCard? _helpContent;
    private MarkupString? _body;
    protected override async Task OnInitializedAsync()
    {
        var cardTypeId = (int)CardType;
        _helpContent = await HelpContent.GetTaskCardHelp(cardTypeId);

        _body = HtmlCleaner.ToCleanRichText(_helpContent.Content);
    }

    private void NavigateTo(string uri)
    {
        Navigation.NavigateTo(uri);
    }
}