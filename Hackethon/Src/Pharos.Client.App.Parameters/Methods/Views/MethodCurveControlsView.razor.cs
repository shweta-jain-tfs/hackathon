using Pharos.Client.App.Parameters.Methods.ViewModels;

namespace Pharos.Client.App.Parameters.Methods.Views;

public partial class MethodCurveControlsView
{
    [Parameter] public MethodDetailsViewModel ViewModel { get; set; } = default!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
}