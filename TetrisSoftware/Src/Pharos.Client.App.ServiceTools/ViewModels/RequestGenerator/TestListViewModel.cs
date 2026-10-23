using System.Collections.ObjectModel;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.ServiceTools.Services;
using Pharos.Client.App.ServiceTools.Views.RequestGenerator;
using Pharos.Client.Common.Localization;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.ServiceTools.ViewModels.RequestGenerator;

public class TestListViewModel : ViewModelBase
{
    private readonly IStringLocalizer<Translations> _localizer;
    private readonly IModalService _modalService;
    private readonly IRequestGeneratorStateService _stateService;
    private bool _stateLoaded;

    public TestListViewModel(
        IStringLocalizer<Translations> loc,
        IModalService modalService,
        IRequestGeneratorStateService stateService)
    {
        _localizer = loc;
        _modalService = modalService;
        _stateService = stateService;
        Tests = new ObservableCollection<RequestGeneratorTest>();
    }

    public ObservableCollection<RequestGeneratorTest> Tests { get; }

    public HashSet<RequestGeneratorTest> SelectedTests { get; private set; } = [];

    public bool HasSelection => SelectedTests.Count > 0;

    public int TestCount => Tests.Count;

    public int IncludedTestCount => Tests.Count(t => t.IsActive);

    public async Task LoadAsync()
    {
        if (!_stateLoaded)
        {
            var savedTests = await _stateService.GetStateAsync<List<RequestGeneratorTest>>(nameof(TestListViewModel));
            Restore(savedTests);
            _stateLoaded = true;
        }
    }

    public async Task AddTestAsync()
    {
        var parameters = new ModalParameters
        {
            { nameof(AddTestView.CurrentTests), Tests }
        };

        var modal = _modalService.Show<AddTestView>(
            new ModalOptions
            {
                Title = _localizer["RequestGenerator_AddTest_Title"],
                ModalHeight = ModalSize.XLarge,
                ModalWidth = ModalSize.Large
            },
            parameters);

        var result = await modal.Result;

        if (result.Canceled || result.Data is not HashSet<MethodTest> selectedMethodTests)
        {
            return;
        }

        foreach (var methodTest in selectedMethodTests)
        {
            if (ContainsTest(methodTest.MethodName, methodTest.TestName))
            {
                continue;
            }
            var createdTest = CreateRequestGeneratorTest(methodTest);
            AddTest(createdTest);
        }
    }

    private void AddTest(RequestGeneratorTest test)
    {
        Tests.Add(test);
        NotifyTestsChanged(includedTestCountChanged: true);
    }

    public void SetTestActive(RequestGeneratorTest test, bool isActive)
    {
        if (!Tests.Contains(test) || test.IsActive == isActive)
        {
            return;
        }

        test.IsActive = isActive;
        NotifyTestsChanged(includedTestCountChanged: true);
    }

    public void UpdateTest(RequestGeneratorTest currentTest, RequestGeneratorTest updatedTest)
    {
        if (!Tests.Contains(currentTest))
        {
            return;
        }

        currentTest.AssayTechnology = updatedTest.AssayTechnology;
        currentTest.Replicates = updatedTest.Replicates;
        currentTest.InstrumentDilutionFactor = updatedTest.InstrumentDilutionFactor;
        NotifyTestsChanged();
    }

    public void DeleteSelectedTests()
    {
        if (!HasSelection)
        {
            return;
        }

        var selectedTests = SelectedTests.ToList();
        foreach (var test in selectedTests)
        {
            Tests.Remove(test);
        }

        SelectedTests = [];
        NotifyTestsChanged(includedTestCountChanged: true);
        OnPropertyChanged(nameof(HasSelection));
    }

    public void UpdateSelectedTests(HashSet<RequestGeneratorTest> selectedItems)
    {
        SelectedTests = [.. selectedItems];
        OnPropertyChanged(nameof(HasSelection));
    }

    private void Restore(IEnumerable<RequestGeneratorTest>? savedTests)
    {
        Tests.Clear();
        foreach (var test in savedTests ?? [])
        {
            Tests.Add(test);
        }

        SelectedTests = [];
        NotifyTestsChanged(includedTestCountChanged: true);
        OnPropertyChanged(nameof(HasSelection));
    }

    private void NotifyTestsChanged(bool includedTestCountChanged = false)
    {
        OnPropertyChanged(nameof(Tests));
        OnPropertyChanged(nameof(TestCount));

        if (includedTestCountChanged)
        {
            OnPropertyChanged(nameof(IncludedTestCount));
        }
        _ = PersistStateAsync();
    }

    private bool ContainsTest(string methodName, string testName)
    {
        return Tests.Any(t => t.MethodName == methodName && t.TestName == testName);
    }

    private static RequestGeneratorTest CreateRequestGeneratorTest(MethodTest methodTest)
    {
        return new RequestGeneratorTest
        {
            AssayTechnology = methodTest.Technology,
            MethodName = methodTest.MethodName,
            TestName = methodTest.TestName,
            Replicates = methodTest.Replicates,
            InstrumentDilutionFactor = methodTest.UsesInstrumentDilution ? methodTest.DilutionFactor : 1,
            IsActive = true
        };
    }

    private Task PersistStateAsync()
    {
        if (!_stateLoaded)
        {
            return Task.CompletedTask;
        }

        return _stateService.SetStateAsync(nameof(TestListViewModel), Tests.ToList());
    }
}
