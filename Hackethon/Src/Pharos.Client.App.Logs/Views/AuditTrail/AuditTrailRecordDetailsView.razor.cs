using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Logs.Views.AuditTrail;

public partial class AuditTrailRecordDetailsView
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Parameter] public AuditTrailRecord Record { get; set; } = default!;
    [CascadingParameter] public ModalInstance ModalInstance { get; set; } = default!;

    private async Task HandleCloseAsync()
    {
        await ModalInstance.CloseAsync(ModalResult.Cancel());
    }
}