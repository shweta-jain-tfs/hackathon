using System.ComponentModel;
using Microsoft.Extensions.Localization;
using Pharos.Common.Web.Components.FormElements;

namespace Pharos.Client.App.Result.Views;

public partial class QualityControlDetailsPage : IDisposable
{
    [Parameter] public int Id { get; set; }
    [Parameter] public string QcName { get; set; } = default!;
    [Parameter] public string TestName { get; set; } = default!;
    [Inject] private MethodNameViewModel MethodNameViewModel { get; set; } = default!;
    [Inject] private QualityControlViewModel QualityControlViewModel { get; set; } = default!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    private string? MethodName { get; set; }
    private readonly List<DropDownOption<(string, string)>> _dropdownValues = [];
    private bool SideBySide { get; set; }
    private bool ShowTitleAsDropdown { get; set; }

    protected override async Task OnInitializedAsync()
    {
        var info = await MethodNameViewModel.GetMethodNameForRun(Id);
        MethodName = info.MethodName;
        QualityControlViewModel.AnalyticalRunId = Id;
        QualityControlViewModel.MethodName = info.MethodName;
        QualityControlViewModel.InstrumentSerial = info.InstrumentNumber;
        QualityControlViewModel.Miniature = false;
        await QualityControlViewModel.LoadAsync();

        if (QualityControlViewModel.ChartHolders is { Count: > 1 })
        {
            ShowTitleAsDropdown = true;
            PopulateDropDown();
            SetSelectedQcs();
        }
        else
        {
            if (QualityControlViewModel.ChartHolders != null)
            {
                var defaultQc = QualityControlViewModel.ChartHolders.First();
                QualityControlViewModel.SelectedLeftTestResult = (defaultQc.QualityControlName, defaultQc.TestName);
            }
        }

        QualityControlViewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void PopulateDropDown()
    {
        foreach (var op in QualityControlViewModel.ChartHolders!.Select(result => new DropDownOption<(string, string)>
        {
            StatusType = result.Status,
            Text = $"{result.QualityControlName} : {result.TestName}",
            Value = (result.QualityControlName, result.TestName)
        }))
        {
            _dropdownValues.Add(op);
        }
    }

    private void SetSelectedQcs()
    {
        var qcName = QcName;
        var qcTestName = TestName;
        if (QualityControlViewModel.ChartHolders != null)
        {
            if (string.IsNullOrEmpty(qcName) || string.IsNullOrEmpty(qcTestName))
            {
                var defaultQc = QualityControlViewModel.ChartHolders[0];
                var defaultRightQc = QualityControlViewModel.ChartHolders[1];
                QualityControlViewModel.SelectedLeftTestResult = (defaultQc.QualityControlName, defaultQc.TestName);
                QualityControlViewModel.SelectedRightTestResult = (defaultRightQc.QualityControlName, defaultRightQc.TestName);
            }
            else
            {
                var selectedQc = QualityControlViewModel.ChartHolders.FirstOrDefault(x => x.QualityControlName == qcName && x.TestName == qcTestName);
                if (selectedQc != null)
                {
                    //Set the values
                    QualityControlViewModel.SelectedLeftTestResult = (selectedQc.QualityControlName, selectedQc.TestName);
                    var clickedQc = _dropdownValues.Single(x => x.Value == (selectedQc.QualityControlName, selectedQc.TestName));

                    var i = _dropdownValues.IndexOf(clickedQc) + 1;
                    QualityControlViewModel.SelectedRightTestResult = i > _dropdownValues.Count - 1 ? _dropdownValues.First().Value : _dropdownValues[i].Value;
                }
            }
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(QualityControlViewModel.ChartHolders))
        {
            if (QualityControlViewModel.ChartHolders is { Count: > 1 })
            {
                ShowTitleAsDropdown = true;
                _dropdownValues.Clear();
                PopulateDropDown();
            }

            if (QualityControlViewModel.SelectedRightTestResult == (null, null))
            {
                SetSelectedQcs();
            }
        }
        StateHasChanged();
    }

    protected List<BreadcrumbItem> Breadcrumbs =>
    [
        new BreadcrumbItem("Results", PageRoute.Results),
        new BreadcrumbItem(MethodName ?? "", $"{PageRoute.AnalyticalRunDetails.Default}/{Id}"),
        new BreadcrumbItem("Quality Controls", "")
    ];
    public void Dispose()
    {
        QualityControlViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        QualityControlViewModel.Dispose();
        GC.SuppressFinalize(this);
    }
}