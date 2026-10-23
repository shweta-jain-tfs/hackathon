using Microsoft.Extensions.Localization;
using Pharos.Client.App.ServiceTools.Services;
using Pharos.Client.App.ServiceTools.ViewModels.RequestGenerator;
using Pharos.Client.Common.Localization;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.ServiceTools.Views.RequestGenerator;

public partial class AddTestView : IDisposable
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = null!;
    [Inject] private AddTestViewModel ViewModel { get; set; } = null!;

    [CascadingParameter] public ModalInstance Modal { get; set; } = default!;
    [Parameter] public IEnumerable<RequestGeneratorTest> CurrentTests { get; set; } = [];

    private Dictionary<string, string> _methodFilters = new();
    private HashSet<MethodTest>? _selectedTests;

    protected override void OnInitialized()
    {
        ViewModel.ViewRefreshNeeded += OnViewRefreshNeeded;
    }

    protected override async Task OnInitializedAsync()
    {
        _methodFilters = await ViewModel.GetMultiFilterDataSource();
    }

    private async Task CloseModalAsync(ModalResult result)
    {
        await Modal.CloseAsync(result);
    }

    private void SelectedTestsChanged(HashSet<MethodTest> selectedTests)
    {
        _selectedTests = selectedTests;
    }

    private void OnViewRefreshNeeded(object? sender, EventArgs e)
    {
        InvokeAsync(StateHasChanged);
    }

    private static bool MatchesExistingRequestGeneratorTest(MethodTest methodTest, object obj)
    {
        var existingTest = (RequestGeneratorTest)obj;
        return methodTest.MethodName.Equals(existingTest.MethodName, StringComparison.Ordinal)
               && methodTest.TestName.Equals(existingTest.TestName, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        ViewModel.ViewRefreshNeeded -= OnViewRefreshNeeded;
        GC.SuppressFinalize(this);
    }
}
