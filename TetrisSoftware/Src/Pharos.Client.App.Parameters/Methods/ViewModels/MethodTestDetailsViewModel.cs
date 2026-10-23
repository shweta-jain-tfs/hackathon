using System.ComponentModel;
using Pharos.Contracts.Security;

namespace Pharos.Client.App.Parameters.Methods.ViewModels;

public sealed class MethodTestDetailsViewModel : ViewModelItem<MethodTestDetails>
{
    public const int LaboratoryTestNameMaximumLength = LaboratoryTest.MaximumNameLength;

    public MethodTestDetailsViewModel(MethodTestDetails model) : base(model)
    {
        model ??= new MethodTestDetails();
        TestDetailsGeneralViewModel = CreateChild(model, m => new MethodTestDetailsGeneralViewModel(m));
        TestDetailsReportViewModel = CreateChild(model, m => new MethodTestDetailsReportViewModel(m));
        TestDetailsAdvancedViewModel = CreateChild(model.Priority ??= new MethodTestPriority(), m => new MethodTestDetailsAdvancedViewModel(m));
    }

    public MethodTestDetailsGeneralViewModel TestDetailsGeneralViewModel { get; }
    public MethodTestDetailsAdvancedViewModel TestDetailsAdvancedViewModel { get; }

    public MethodTestDetailsReportViewModel TestDetailsReportViewModel { get; }

    public List<MethodTestLabName> MethodTestLabNames
    {
        get => Model.MethodTestLabNames?.ToList() ?? [];
        set => Model.MethodTestLabNames = value;
    }

    public List<MethodTestLabName> SortedMethodTestLabNames => MethodTestLabNames.OrderBy(x => x.SortOrder).ToList();

    public bool IsLimitOfLabNamesReached => Model.MethodTestLabNames?.Count >= LaboratoryTest.MaximumNumberOfNames;

    public bool IsLaboratoryTestNamesValid()
    {
        if (MethodTestLabNames.Count <= 0)
        {
            return false;
        }

        if (!IsLabNameCountWithinAllowedLimit())
        {
            return false;
        }

        if (MethodTestLabNames.Any(x => !IsLabNameValid(x.TestName)))
        {
            return false;
        }

        if (!IsLabNamesUniqueWithinMethod())
        {
            return false;
        }

        return true;
    }

    private bool IsLabNameCountWithinAllowedLimit()
    {
        return MethodTestLabNames.Count <= LaboratoryTest.MaximumNumberOfNames;
    }

    private static bool IsLabNameValid(string labName)
    {
        if (string.IsNullOrWhiteSpace(labName))
        {
            return false;
        }

        return labName.Length <= LaboratoryTestNameMaximumLength;

    }

    private bool IsLabNamesUniqueWithinMethod()
    {
        return MethodTestLabNames
            .Select(x => x.TestName.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count() == MethodTestLabNames.Count;
    }

    public bool HighlightErrorIfDuplicate(string labTestName)
    {
        return Model.LaboratoryTestNamesDuplicatedAcrossMethods
            ?.Contains(labTestName, StringComparer.OrdinalIgnoreCase) == true;
    }

    public void AddDraftLaboratoryName(MethodTestLabName draft, string labName)
    {
        var trimmedLabName = labName.Trim();
        draft.TestName = trimmedLabName;
        draft.SortOrder = MethodTestLabNames.Count + 1;
        Model.MethodTestLabNames.Add(draft);

        SetAsModified();
    }

    public void RemoveNewTestLaboratoryName(MethodTestLabName laboratoryName)
    {
        if (laboratoryName.Id != 0 || !MethodTestLabNames.Contains(laboratoryName))
        {
            return;
        }

        MethodTestLabNames = MethodTestLabNames
            .Where(x => x != laboratoryName)
            .ToList();
        ReassignLaboratoryNameSortOrders();

        SetAsModified();
    }

    public void UpdateLaboratoryName(MethodTestLabName laboratoryName, string labName)
    {
        var trimmedLabName = labName.Trim();
        if (laboratoryName.TestName == trimmedLabName)
        {
            return;
        }

        laboratoryName.TestName = trimmedLabName;

        SetAsModified();
    }

    public void RemoveLaboratoryNames(HashSet<MethodTestLabName> selectedItems)
    {
        if (!CanDeleteLaboratoryNames(selectedItems))
        {
            return;
        }

        MethodTestLabNames = MethodTestLabNames
            .Where(x => !selectedItems.Contains(x))
            .ToList();

        ReassignLaboratoryNameSortOrders();

        SetAsModified();
    }

    public bool CanDeleteLaboratoryNames(IReadOnlyCollection<MethodTestLabName> selectedItems)
    {
        return selectedItems.Count > 0 && MethodTestLabNames.Count - selectedItems.Count >= 1;
    }

    public void MoveLaboratoryNameToTop(MethodTestLabName laboratoryName)
    {
        var orderedLabNames = SortedMethodTestLabNames;
        if (orderedLabNames.Count == 0 || orderedLabNames[0] == laboratoryName)
        {
            return;
        }

        var sortOrder = 1;
        laboratoryName.SortOrder = sortOrder++;
        foreach (var labName in orderedLabNames.Where(x => x != laboratoryName))
        {
            labName.SortOrder = sortOrder++;
        }

        SetAsModified();
    }

    private void ReassignLaboratoryNameSortOrders()
    {
        var sortOrder = 1;
        foreach (var laboratoryName in MethodTestLabNames.OrderBy(x => x.SortOrder))
        {
            laboratoryName.SortOrder = sortOrder++;
        }
    }

    private void OnChildPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        SetAsModified();
    }

    protected override void Disposing()
    {
        TestDetailsGeneralViewModel.PropertyChanged -= OnChildPropertyChanged;
        TestDetailsAdvancedViewModel.PropertyChanged -= OnChildPropertyChanged;
        TestDetailsReportViewModel.PropertyChanged -= OnChildPropertyChanged;
    }

    private T CreateChild<TModel, T>(TModel model, Func<TModel, T> factory) where T : INotifyPropertyChanged
    {
        var child = factory(model);
        child.PropertyChanged += OnChildPropertyChanged;
        return child;
    }

}
