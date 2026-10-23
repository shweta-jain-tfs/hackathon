using System.Globalization;
using System.Text;
using Microsoft.JSInterop;
using Pharos.Client.App.Logs.ViewModels;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Common.Web.Components.Modal;
using Pharos.Common.Web.Components.Toaster;
using Pharos.Common.Web.Extensions;
using Pharos.Common.Web.Models.DataGrid;

namespace Pharos.Client.App.Logs.Views.AuditTrail;

public partial class AuditTrailView
{
    [Inject] public AuditTrailViewModel ViewModel { get; set; } = default!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] private IModalService Modal { get; set; } = default!;
    [Inject] private IToasterService ToasterService { get; set; } = default!;

    private bool _isExporting;
    private int _totalItemCount;

    private bool CannotExport => _isExporting || _totalItemCount == 0;

    private HashSet<AuditTrailRecord> _allRecords = [];

    // Keep a stable set across paging and filtering
    private readonly HashSet<string> _knownUserNames = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, string> _userNamesDict = new(StringComparer.OrdinalIgnoreCase);

    private IJSObjectReference? _script;

    protected override async void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            _script = await ImportCommonUtilityJSAsync("download.js");
        }
    }

    public override async ValueTask DisposeAsync()
    {
        await _script.SafeDisposeAsync();
        ViewModel.ViewRefreshNeeded -= OnViewRefreshNeeded;
        ViewModel.Dispose();
        GC.SuppressFinalize(this);
    }

    // Use only users who have audit trail records (server endpoint)
    protected override async Task OnInitializedAsync()
    {
        var operatorNames = await ViewModel.GetOperatorNamesAsync();
        foreach (var name in operatorNames)
        {
            _knownUserNames.Add(name);
        }
        RebuildUserNamesDict();
        ViewModel.ViewRefreshNeeded += OnViewRefreshNeeded;
    }

    private void RebuildUserNamesDict()
    {
        _userNamesDict = _knownUserNames
            .OrderBy(n => n, StringComparer.CurrentCultureIgnoreCase)
            .ToDictionary(n => n, n => n, StringComparer.OrdinalIgnoreCase);
    }

    private async ValueTask<GridItemsProviderResult<AuditTrailRecord>> ProvideGridItems(GridItemsProviderRequest<AuditTrailRecord> request)
    {
        var result = await ViewModel.ProvideGridItems(request);
        _allRecords = result.Items.ToHashSet();

        var totalItemCountChanged = _totalItemCount != result.TotalItemCount;
        _totalItemCount = result.TotalItemCount;

        var added = false;
        foreach (var name in result.Items.Select(r => r.CreatedBy)
                     .Where(n => !string.IsNullOrWhiteSpace(n))
                     .Select(n => n!.Trim()))
        {
            if (_knownUserNames.Add(name))
            {
                added = true;
            }
        }

        if (added)
        {
            RebuildUserNamesDict(); // keep alphabetical order after new names
        }

        if (added || totalItemCountChanged)
        {
            await InvokeAsync(StateHasChanged);
        }

        return result;
    }

    private void OnViewRefreshNeeded(object? sender, EventArgs e)
    {
        InvokeAsync(StateHasChanged);
    }

    private async Task ShowEventDetails(int id)
    {
        var record = _allRecords.FirstOrDefault(r => r.Id == id);
        if (record is null)
        {
            return;
        }

        var options = new ModalOptions
        {
            Title = Loc["AuditTrail_RecordDetailsModal_Title"],
            ModalHeight = ModalSize.Large,
            ModalWidth = ModalSize.Large
        };

        var parameters = new ModalParameters
        {
            { nameof(AuditTrailRecordDetailsView.Record), record }
        };

        await Modal.Show<AuditTrailRecordDetailsView>(options, parameters).Result;
    }

    private async Task ExportAuditTrail()
    {
        if (_isExporting || CannotExport || _script is null)
        {
            return;
        }

        try
        {
            var fileName = $"{Loc["AuditTrail_File_Export"]}_{DateTimeOffset.Now.ToString("yyyy-MM-dd", CultureInfo.CurrentUICulture)}.csv";
            _isExporting = await _script.InvokeAsync<bool>("checkExportDialog", fileName);
            if (!_isExporting)
            {
                return;
            }

            var content = await ViewModel.ExportAuditTrailAsync();
            if (string.IsNullOrEmpty(content))
            {
                return;
            }

            var bytes = Encoding.UTF8.GetBytes(content);
            using var memoryStream = new MemoryStream(bytes);
            using var streamRef = new DotNetStreamReference(stream: memoryStream);
            await _script.InvokeVoidAsync("exportIfSavePressed", fileName, streamRef);

            ToasterService.ShowToast(ToastType.Confirmation, Loc["AuditTrail_Export_Successful"]);
        }
        catch
        {
            ToasterService.ShowToast(ToastType.Alarm, Loc["AuditTrail_Export_Failure"]);
        }
        finally
        {
            _isExporting = false;
        }
    }
}