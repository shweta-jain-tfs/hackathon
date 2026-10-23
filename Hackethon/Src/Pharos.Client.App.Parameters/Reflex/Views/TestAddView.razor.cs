using Pharos.Client.App.Parameters.Common.ViewModels;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Parameters.Reflex.Views;

public partial class TestAddView : IDisposable
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = null!;
    [Inject] private MethodListViewModel ViewModel { get; set; } = default!;

    [CascadingParameter] public ModalInstance Modal { get; set; } = default!;
    [Parameter] public IEnumerable<object> CurrentTestOrders { get; set; } = [];

    private Dictionary<string, string> _methodList = new();
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

    private static bool MatchesTestOrderTest(MethodTest methodTest, object obj)
    {
        var reflexTest = (ReflexTest)obj;
        return methodTest.MethodName.Equals(reflexTest.MethodName, StringComparison.Ordinal) && methodTest.TestName.Equals(reflexTest.TestName, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        ViewModel.ViewRefreshNeeded -= OnViewRefreshNeeded;
        ViewModel.Dispose();
        GC.SuppressFinalize(this);
    }
}