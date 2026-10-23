using Pharos.Client.App.Parameters.Reflex.ViewModels;

namespace Pharos.Client.App.Parameters.Reflex.Views;

public partial class RequestorDetailsView : ComponentBase
{
    [Parameter] public RequestorDetailsViewModel ViewModel { get; set; } = default!;
    [Inject] public IStringLocalizer<Translations> Loc { get; set; } = default!;
}