using Pharos.Client.App.Parameters.Common.ViewModels;
using Pharos.Client.App.Parameters.Panels.ViewModels;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Parameters.Panels.Views;

public partial class PanelTestAddView : IDisposable
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] private MethodListViewModel ViewModel { get; set; } = default!;

    [CascadingParameter] public ModalInstance Modal { get; set; } = default!;
    [Parameter] public IEnumerable<PanelTestViewModel> CurrentPanelTests { get; set; } = [];

    private Dictionary<string, string> _methodList = [];
    private HashSet<MethodTest>? _selectedItems;

    protected override void OnInitialized()
    {
        ViewModel.ViewRefreshNeeded += OnViewRefreshNeeded;
    }

    protected override async Task OnInitializedAsync()
    {
        _methodList = await ViewModel.GetMultiFilterDataSource();
    }

    private async Task CloseModal(ModalResult result)
    {
        await Modal.CloseAsync(result);
    }

    private void SelectedTestsChanged(HashSet<MethodTest> selectedTest)
    {
        _selectedItems = selectedTest;
    }

    private void OnViewRefreshNeeded(object? sender, EventArgs e)
    {
        StateHasChanged();
    }

    private static bool MatchesCurrentPanelTest(MethodTest methodTest, object obj)
    {
        var panelTest = (PanelTestViewModel)obj;
        return methodTest.MethodName.Equals(panelTest.MethodName, StringComparison.Ordinal) && methodTest.TestName.Equals(panelTest.TestName, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        ViewModel.ViewRefreshNeeded -= OnViewRefreshNeeded;
        ViewModel.Dispose();
        GC.SuppressFinalize(this);
    }
}
