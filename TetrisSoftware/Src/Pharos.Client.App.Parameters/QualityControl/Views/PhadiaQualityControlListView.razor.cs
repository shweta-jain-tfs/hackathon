using Pharos.Client.App.Parameters.QualityControl.ViewModels;

namespace Pharos.Client.App.Parameters.QualityControl.Views;

public partial class PhadiaQualityControlListView
{
    private Dictionary<string, string> _methodNameDict = [];
    [Inject] public PhadiaQualityControlListViewModel ViewModel { get; set; } = default!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        _methodNameDict = await ViewModel.GetMethodNames();
    }
}