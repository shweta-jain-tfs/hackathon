using Microsoft.AspNetCore.Components;
using Pharos.Client.App.TaskCard.ViewModels;
using Pharos.Client.App.TaskCard.Views.TaskCard;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.TaskCard.Views;

public partial class TaskCardsPage : IDisposable
{
    [Inject] private TaskCardsPageViewModel ViewModels { get; set; } = null!;
    [Inject] private IModalService Modal { get; set; } = null!;

    private IModalReference? _helpModalRef;
    private TaskCardType? _helpModalCardType;

    protected override Task OnInitializedAsync()
    {
        ViewModels.ViewRefreshNeeded += RefreshView;
        return ViewModels.LoadAsync();
    }

    private void RefreshView(object? sender, EventArgs e)
    {
        CloseHelpModal();
        StateHasChanged();
    }
    public void Dispose()
    {
        ViewModels.ViewRefreshNeeded -= RefreshView;
        ViewModels.Dispose();
        GC.SuppressFinalize(this);
    }

    private void HelpClicked(TaskCardType cardType, string title, string connectedPageTitle, string connectedPageUri)
    {
        var cardTypeId = (int)cardType;

        var parameters = new ModalParameters
        {
            {nameof(TaskCardHelpContent.CardType), cardTypeId},
            {nameof(TaskCardHelpContent.Title), title },
            {nameof(TaskCardHelpContent.ConnectedPageTitle), connectedPageTitle},
            {nameof(TaskCardHelpContent.ConnectedPageUri), connectedPageUri},
        };
        _helpModalRef = Modal.Show<TaskCardHelpContent>(new ModalOptions { Title = title, ModalHeight = ModalSize.Large, ModalWidth = ModalSize.Small }, parameters);
        _helpModalCardType = cardType;
    }

    private void CloseHelpModal()
    {
        if (_helpModalRef is null || _helpModalCardType is null)
        {
            return;
        }

        var cardStillPresent = ViewModels.Any(x => x.CardType == _helpModalCardType.Value);
        if (!cardStillPresent)
        {
            Modal.CloseModal(_helpModalRef);
            _helpModalRef = null;
            _helpModalCardType = null;
        }
    }
}