using System.ComponentModel;
using Pharos.Client.App.Parameters.Common.Models;
using Pharos.Client.App.Parameters.Reflex.Models;

namespace Pharos.Client.App.Parameters.Reflex.ViewModels;

public sealed class ReflexDefinitionDetailsViewModel : ViewModelItem<ReflexDefinition>
{
    private bool _testDeleted;
    private string? _nonUniqueDefinitionName;
    private readonly string? _originalName;

    public ReflexDefinitionDetailsViewModel(ReflexDefinition model, IMethodTestDataSource methodTestDataSource, RequestorViewModel requestorViewModel, UiSettings uiSettings, bool canEditReflex) : base(model)
    {
        CanEdit = canEditReflex;
        UiSettings = uiSettings;

        ReflexConditionViewModel = new ReflexConditionViewModel(model.ReflexCondition, methodTestDataSource, canEditReflex);

        ReflexTestViewModels = [];
        foreach (var reflexTest in model.ReflexTests)
        {
            var reflexTestViewModel = new ReflexTestViewModel(reflexTest);
            ReflexTestViewModels.Add(reflexTestViewModel);
        }

        RequestorViewModel = requestorViewModel;

        RegisterListeners();

        // Store original name for validation optimization
        _originalName = IsNewEntity ? null : Model.Name;
    }

    public string? DefinitionName
    {
        get => Model.Name;
        set => SetProperty(Model.Name, value, v => Model.Name = v);
    }

    public bool Automatic
    {
        get => Model.Automatic;
        set => SetProperty(Model.Automatic, value, v => Model.Automatic = v);
    }

    public bool Active
    {
        get => Model.Active;
        set => SetProperty(Model.Active, value, v => Model.Active = v);
    }

    public bool HasRequestorEnabled
    {
        get => Model.HasRequestorEnabled;
        set => SetProperty(Model.HasRequestorEnabled, value, v => Model.HasRequestorEnabled = v);
    }

    public string? RequestorId
    {
        get => Model.RequestorId;
        set => SetProperty(Model.RequestorId, value, v => Model.RequestorId = v);
    }

    public string? CommentCode
    {
        get => Model.CommentCode;
        set => SetProperty(Model.CommentCode, value, v => Model.CommentCode = v);
    }

    public ReflexConditionViewModel ReflexConditionViewModel { get; }
    public List<ReflexTestViewModel> ReflexTestViewModels { get; }

    public bool IsNewEntity => Model.Id == 0;

    public override bool IsModified => base.IsModified || ReflexConditionViewModel.IsModified || ReflexTestViewModels.Any(x => x.IsModified) || _testDeleted;

    public bool IsUniqueDefinitionName => string.IsNullOrEmpty(DefinitionName) || DefinitionName != _nonUniqueDefinitionName;
    public RequestorViewModel RequestorViewModel { get; }
    public UiSettings UiSettings { get; }
    public bool CanEdit { get; }

    public void SetNonUniqueDefinitionName(string? name)
    {
        _nonUniqueDefinitionName = name;
    }

    /// <summary>
    /// Validate definition name.
    /// </summary>
    public async Task<bool> ValidateDefinitionNameAsync(IReflexDetailDataService dataService)
    {
        var nameToValidate = DefinitionName?.Trim();

        // Empty check
        if (string.IsNullOrWhiteSpace(nameToValidate))
        {
            return false;
        }

        // Skip validation if name hasn't changed when editing
        if (!IsNewEntity && string.Equals(nameToValidate, _originalName, StringComparison.OrdinalIgnoreCase))
        {
            SetNonUniqueDefinitionName(null);
            return true;
        }

        // Server validation
        var isUnique = await dataService.ValidateDefinitionNameAsync(nameToValidate);
        SetNonUniqueDefinitionName(isUnique ? null : nameToValidate);
        return isUnique;
    }

    protected override void Disposing()
    {
        base.Disposing();
        UnregisterListeners(true);
    }

    protected override void OnSetAsUnmodified()
    {
        UnregisterListeners();

        ReflexConditionViewModel.SetAsUnmodified();
        foreach (var viewModel in ReflexTestViewModels)
        {
            viewModel.SetAsUnmodified();
        }
        _testDeleted = false;

        RegisterListeners();
    }

    private void RegisterListeners()
    {
        ReflexConditionViewModel.PropertyChanged += SetModifiedFlagOnPropertyChanged;
        foreach (var viewModel in ReflexTestViewModels)
        {
            viewModel.PropertyChanged += SetModifiedFlagOnPropertyChanged;
        }

        RequestorViewModel.RequestorSelected += OnRequestorSelected;
    }

    private void OnRequestorSelected(object? sender, string requestorId)
    {
        RequestorId = requestorId;
    }

    private void UnregisterListeners(bool dispose = false)
    {
        ReflexConditionViewModel.PropertyChanged -= SetModifiedFlagOnPropertyChanged;
        if (dispose)
        {
            ReflexConditionViewModel.Dispose();
        }

        foreach (var viewModel in ReflexTestViewModels)
        {
            viewModel.PropertyChanged -= SetModifiedFlagOnPropertyChanged;
            if (dispose)
            {
                viewModel.Dispose();
            }
        }

        RequestorViewModel.RequestorSelected -= OnRequestorSelected;
    }

    private void SetModifiedFlagOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        ModifiedChanged();
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
            var existingTest = ReflexTestViewModels.FirstOrDefault(x => x.TestName == methodTest.TestName && x.MethodName == methodTest.MethodName);

            if (existingTest == null)
            {
                var sortOrder = ReflexTestViewModels.Count > 0 ? ReflexTestViewModels.Max(x => x.TestSortOrder) + 1 : 1;

                var model = new ReflexTest();
                SetValues(model, methodTest, sortOrder);
                Model.ReflexTests.Add(model);

                var viewModel = new ReflexTestViewModel(model, true);
                viewModel.PropertyChanged += SetModifiedFlagOnPropertyChanged;
                ReflexTestViewModels.Add(viewModel);

                affectingState = true;
            }
        }

        if (affectingState)
        {
            ModifiedChanged();
        }
    }

    private static void SetValues(ReflexTest model, MethodTest methodTest, int sortOrder)
    {
        model.TestName = methodTest.TestName;
        model.TestFullName = methodTest.FullTestName;
        model.MethodName = methodTest.MethodName;
        model.MethodFullName = methodTest.LongMethodName;
        model.NumberOfReplicates = methodTest.Replicates;
        model.TestSortOrder = sortOrder;

        if (methodTest.UsesInstrumentDilution)
        {
            model.InstrumentDilutionFactor = methodTest.DilutionFactor;
        }
        else
        {
            model.InstrumentDilutionFactor = 1;
        }
    }

    public void DeleteTest(HashSet<ReflexTestViewModel> selectedItems)
    {
        foreach (var item in selectedItems)
        {
            item.PropertyChanged -= SetModifiedFlagOnPropertyChanged;
            ReflexTestViewModels.Remove(item);
            Model.ReflexTests.Remove(item.Model);

            //items with id 0 has never been saved, if they are removed nothing has really changed
            if (item.Model.Id != 0)
            {
                _testDeleted = true;
            }
        }

        // Sort order might have changed. Re-order from 1
        var testSortOrder = 1;
        foreach (var testOrder in ReflexTestViewModels.OrderBy(x => x.TestSortOrder))
        {
            testOrder.TestSortOrder = testSortOrder;
            testSortOrder++;
        }

        ModifiedChanged();
    }
}