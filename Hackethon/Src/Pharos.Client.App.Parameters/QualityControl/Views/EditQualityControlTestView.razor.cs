using Pharos.Client.App.Parameters.QualityControl.ViewModels;

namespace Pharos.Client.App.Parameters.QualityControl.Views;

public partial class EditQualityControlTestView
{
    [Parameter] [EditorRequired] public EditQualityControlTestViewModel ViewModel { get; set; }
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
}