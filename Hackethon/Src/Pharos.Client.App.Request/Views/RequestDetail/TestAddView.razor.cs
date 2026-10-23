using Pharos.Client.App.Request.ViewModels;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Request.Views.RequestDetail;

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
        InvokeAsync(StateHasChanged);
    }

    private static bool MatchesTestOrderTest(MethodTest methodTest, object obj)
    {
        var requestTest = (TestOrderViewModel)obj;
        return methodTest.MethodName.Equals(requestTest.MethodName, StringComparison.Ordinal) && methodTest.TestName.Equals(requestTest.TestName, StringComparison.Ordinal) && requestTest.IsNotFinished;
    }

    public void Dispose()
    {
        ViewModel.ViewRefreshNeeded -= OnViewRefreshNeeded;
        ViewModel.Dispose();
        GC.SuppressFinalize(this);
    }
}