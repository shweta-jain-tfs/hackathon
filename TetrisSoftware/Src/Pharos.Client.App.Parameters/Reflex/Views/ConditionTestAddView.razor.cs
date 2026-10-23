using Pharos.Client.App.Parameters.Common.ViewModels;
using Pharos.Common.Web.Components.DataGrid;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Parameters.Reflex.Views;

public partial class ConditionTestAddView : IDisposable
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = null!;
    [Inject] private MethodListViewModel ViewModel { get; set; } = default!;

    [CascadingParameter] public ModalInstance Modal { get; set; } = default!;
    [Parameter] public IEnumerable<object> CurrentTestOrders { get; set; } = [];

    private Dictionary<string, string> _methodList = new();
    private MethodTest? _selectedItem;
    private ItemStyleOptions<MethodTest> _itemStyleOptions = new()
    {
        Style = ItemStyleFlags.None,
        Match = static (_, _) => false,
        Items = []
    };

    protected override void OnInitialized()
    {
        ViewModel.ViewRefreshNeeded += OnViewRefreshNeeded;
    }

    protected override async Task OnInitializedAsync()
    {
        _methodList = await ViewModel.GetMultiFilterDataSource();
        _itemStyleOptions = new ItemStyleOptions<MethodTest>
        {
            Style = ItemStyleFlags.Checked,
            Match = MatchesTestOrderTest,
            Items = CurrentTestOrders
        };
    }

    private async Task CloseModal(ModalResult result)
    {
        await Modal.CloseAsync(result);
    }

    private void SelectedTestChanged(MethodTest selectedTest)
    {
        _selectedItem = selectedTest;
        _itemStyleOptions = new ItemStyleOptions<MethodTest>
        {
            Style = ItemStyleFlags.None,
            Match = static (_, _) => false,
            Items = []
        };
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
