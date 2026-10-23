using System.ComponentModel;

namespace Pharos.Client.App.Parameters.Panels.ViewModels;

public sealed class PanelsDetailsViewModel : ViewModelItem<MethodPanelDetails>
{
    private bool _testDeleted;

    public PanelsDetailsViewModel(MethodPanelDetails model) : base(model)
    {
        Tests = Model.Tests
            .OrderBy(x => x.SortOrder)
            .Select(x => CreateChild(x, model => new PanelTestViewModel(model)))
            .ToList();
    }

    public List<PanelTestViewModel> Tests { get; }

    public bool IsNewEntity => Model.Id == 0;

    public override bool IsModified => base.IsModified || Tests.Any(x => x.IsModified) || _testDeleted;

    public string? PanelName
    {
        get => Model.PanelName;
        set => SetProperty(Model.PanelName, value ?? string.Empty, v => Model.PanelName = v);
    }

    public string? PanelLongName
    {
        get => Model.PanelLongName;
        set => SetProperty(Model.PanelLongName, value ?? string.Empty, v => Model.PanelLongName = v);
    }

    public bool Grouped
    {
        get => Model.Grouped;
        set => SetProperty(Model.Grouped, value, v => Model.Grouped = v);
    }

    public bool Enabled
    {
        get => Model.Enabled;
        set => SetProperty(Model.Enabled, value, v => Model.Enabled = v);
    }

    public void AddMethodTests(HashSet<MethodTest>? selectedItems)
    {
        if (selectedItems is null)
        {
            return;
        }

        var affectingState = false;
        foreach (var methodTest in selectedItems)
        {
            var existingTest = Tests.FirstOrDefault(x => x.TestName == methodTest.TestName && x.MethodName == methodTest.MethodName);
            if (existingTest is not null)
            {
                continue;
            }

            var sortOrder = Tests.Count > 0 ? Tests.Max(x => x.SortOrder ?? 0) + 1 : 1;
            var model = new MethodPanelTest
            {
                MethodName = methodTest.MethodName,
                TestName = methodTest.TestName,
                FullTestName = methodTest.FullTestName,
                Replicates = methodTest.Replicates,
                InstrumentDilutionFactor = methodTest.UsesInstrumentDilution ? methodTest.DilutionFactor : 1,
                SortOrder = sortOrder
            };

            Model.Tests.Add(model);

            var viewModel = CreateChild(model, panelTest => new PanelTestViewModel(panelTest, true));
            Tests.Add(viewModel);
            affectingState = true;
        }

        if (affectingState)
        {
            ModifiedChanged();
        }
    }

    public void DeleteTests(HashSet<PanelTestViewModel> selectedItems)
    {
        foreach (var item in selectedItems)
        {
            item.PropertyChanged -= OnChildPropertyChanged;
            Tests.Remove(item);
            Model.Tests.Remove(item.Model);

            if (item.Model.Id is > 0)
            {
                _testDeleted = true;
            }
        }

        NormalizeSortOrder();
        ModifiedChanged();
    }

    public void NormalizeSortOrder()
    {
        var sortOrder = 1;
        foreach (var test in Tests.OrderBy(x => x.SortOrder))
        {
            test.SortOrder = sortOrder;
            sortOrder++;
        }
    }

    protected override void OnSetAsUnmodified()
    {
        foreach (var viewModel in Tests)
        {
            viewModel.SetAsUnmodified();
        }

        _testDeleted = false;
    }

    protected override void Disposing()
    {
        base.Disposing();
        foreach (var viewModel in Tests)
        {
            viewModel.PropertyChanged -= OnChildPropertyChanged;
            viewModel.Dispose();
        }
    }

    private T CreateChild<TModel, T>(TModel model, Func<TModel, T> factory) where T : INotifyPropertyChanged
    {
        var child = factory(model);
        child.PropertyChanged += OnChildPropertyChanged;
        return child;
    }

    private void OnChildPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IsModified))
        {
            return;
        }

        SetAsModified();
    }
}
