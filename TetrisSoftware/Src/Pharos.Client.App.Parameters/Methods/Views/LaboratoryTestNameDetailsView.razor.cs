using System.Runtime.CompilerServices;
using Pharos.Client.App.Parameters.Methods.ViewModels;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Common.Web.Components.DataGrid;
using Pharos.Common.Web.Components.FormElements;
using Pharos.Contracts.Security;

namespace Pharos.Client.App.Parameters.Methods.Views;

public partial class LaboratoryTestNameDetailsView
{
    private readonly Dictionary<MethodTestLabName, LaboratoryTestNameGridRow> _gridRows = new(ReferenceEqualityComparer<MethodTestLabName>.Instance);
    private readonly HashSet<LaboratoryTestNameGridRow> _selectedRows = [];
    private PhDataGrid<LaboratoryTestNameGridRow>? _labNamesGrid;
    private PhTextBox? _editingTextBox;
    private bool _shouldFocusTextBox;
    private MethodTestLabName? _editingLabName;
    private MethodTestLabName? _draftLabName;

    [Parameter] public MethodTestDetailsViewModel ViewModel { get; set; } = default!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] private IAuthorizationService AuthorizationService { get; set; } = default!;

    private bool IsEditingDisabled { get; set; }

    private static int LaboratoryTestNameMaximumLength => MethodTestDetailsViewModel.LaboratoryTestNameMaximumLength;

    private IReadOnlyList<LaboratoryTestNameGridRow> GridItems => GetGridItems();

    private string EditingValue { get; set; } = string.Empty;

    private bool IsAddDisabled => IsEditingDisabled || ViewModel.IsLimitOfLabNamesReached || (_draftLabName is not null && string.IsNullOrWhiteSpace(EditingValue));

    private bool IsDeleteDisabled => IsEditingDisabled || !ViewModel.CanDeleteLaboratoryNames(SelectedLabNames);

    private bool IsMoveToTopDisabled =>
        IsEditingDisabled || _selectedRows.Count != 1 || ViewModel.SortedMethodTestLabNames.FirstOrDefault() == _selectedRows.Single().LaboratoryName;

    private HashSet<MethodTestLabName> SelectedLabNames => _selectedRows.Select(x => x.LaboratoryName).ToHashSet(ReferenceEqualityComparer<MethodTestLabName>.Instance);

    protected override async Task OnInitializedAsync()
    {
        IsEditingDisabled = !await AuthorizationService.IsAuthorized(Permissions.EditSystemSettings);
    }

    private List<LaboratoryTestNameGridRow> GetGridItems()
    {
        var laboratoryNames = ViewModel.SortedMethodTestLabNames
            .Where(x => x != _draftLabName)
            .ToList();
        var currentNames = laboratoryNames.ToHashSet(ReferenceEqualityComparer<MethodTestLabName>.Instance);
        foreach (var staleRow in _gridRows.Keys.Where(x => !currentNames.Contains(x)).ToList())
        {
            _gridRows.Remove(staleRow);
        }

        return laboratoryNames.Select(GetGridRow).ToList();
    }

    private LaboratoryTestNameGridRow GetGridRow(MethodTestLabName laboratoryName)
    {
        if (!_gridRows.TryGetValue(laboratoryName, out var row))
        {
            row = new LaboratoryTestNameGridRow(laboratoryName);
            _gridRows.Add(laboratoryName, row);
        }

        return row;
    }

    private bool IsEditing(LaboratoryTestNameGridRow row)
    {
        return _editingLabName == row.LaboratoryName;
    }

    private void BeginEdit(LaboratoryTestNameGridRow row)
    {
        _editingLabName = row.LaboratoryName;
        EditingValue = row.LaboratoryName.TestName ?? string.Empty;
        _shouldFocusTextBox = true;
    }

    private void AddLaboratoryTestNameClicked()
    {
        if (_draftLabName is not null && string.IsNullOrWhiteSpace(EditingValue))
        {
            return;
        }

        _draftLabName = new MethodTestLabName
        {
            TestName = string.Empty,
            SortOrder = ViewModel.MethodTestLabNames.Count + 1
        };
        _editingLabName = _draftLabName;
        EditingValue = string.Empty;
        _shouldFocusTextBox = true;
    }

    private void LabNameInputChanged(MethodTestLabName labName, string? value)
    {
        EditingValue = value ?? string.Empty;
        if (_draftLabName == labName)
        {
            UpdateDraftLaboratoryName();
            return;
        }

        ViewModel.UpdateLaboratoryName(labName, EditingValue);
    }

    private void UpdateDraftLaboratoryName()
    {
        if (_draftLabName is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(EditingValue))
        {
            ViewModel.RemoveNewTestLaboratoryName(_draftLabName);
            _draftLabName.TestName = string.Empty;
            return;
        }

        if (ViewModel.MethodTestLabNames.Contains(_draftLabName))
        {
            ViewModel.UpdateLaboratoryName(_draftLabName, EditingValue);
            EditingValue = _draftLabName.TestName;
        }
        else
        {
            ViewModel.AddDraftLaboratoryName(_draftLabName, EditingValue);
            EditingValue = _draftLabName.TestName;
        }
    }

    private void LabNameInputClosed()
    {
        if (_draftLabName is not null && string.IsNullOrWhiteSpace(EditingValue))
        {
            ViewModel.RemoveNewTestLaboratoryName(_draftLabName);
            _draftLabName = null;
            _editingLabName = null;
            EditingValue = string.Empty;
            return;
        }

        _draftLabName = null;
        _editingLabName = null;
        EditingValue = string.Empty;
    }

    private Task SelectedLabNameChanged(HashSet<LaboratoryTestNameGridRow> rows)
    {
        _selectedRows.Clear();
        foreach (var row in rows)
        {
            _selectedRows.Add(row);
        }

        return Task.CompletedTask;
    }

    private async Task DeleteSelectedLaboratoryTestNames()
    {
        var selectedLabNames = SelectedLabNames;
        ViewModel.RemoveLaboratoryNames(selectedLabNames);
        if (_editingLabName is not null && selectedLabNames.Contains(_editingLabName))
        {
            _editingLabName = null;
            EditingValue = string.Empty;
        }

        _selectedRows.Clear();
        if (_labNamesGrid is not null)
        {
            await _labNamesGrid.ClearSelectedItem();
        }
    }

    private void MoveSelectedLaboratoryTestNameToTop()
    {
        if (_selectedRows.Count != 1)
        {
            return;
        }

        ViewModel.MoveLaboratoryNameToTop(_selectedRows.Single().LaboratoryName);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_shouldFocusTextBox && _editingTextBox is not null)
        {
            _shouldFocusTextBox = false;
            await _editingTextBox.FocusAsync();
        }
    }

    public sealed class LaboratoryTestNameGridRow(MethodTestLabName laboratoryName) : IEquatable<LaboratoryTestNameGridRow>
    {
        private readonly Guid _key = Guid.NewGuid();

        public MethodTestLabName LaboratoryName { get; } = laboratoryName;

        public bool Equals(LaboratoryTestNameGridRow? other)
        {
            return other is not null && _key == other._key;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as LaboratoryTestNameGridRow);
        }

        public override int GetHashCode()
        {
            return _key.GetHashCode();
        }
    }

    private sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T> where T : class
    {
        public static ReferenceEqualityComparer<T> Instance { get; } = new();

        public bool Equals(T? x, T? y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(T obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }
}
