using Pharos.Client.App.Request.ViewModels;

namespace Pharos.Client.App.Request.Views.RequestDetail;

public partial class RequestorDetailsView : ComponentBase
{
    [Parameter] public RequestorDetailsViewModel ViewModel { get; set; } = default!;
    [Inject] public IStringLocalizer<Translations> Loc { get; set; } = default!;
}