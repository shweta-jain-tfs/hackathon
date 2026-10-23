using System.ComponentModel;
using Pharos.Common.Web.Components.FormElements;

namespace Pharos.Client.App.Parameters.Methods.ViewModels;

public sealed class MethodDetailsViewModel : ViewModelItem<MethodDetails>
{
    public MethodDetailsViewModel(MethodDetails model, IEnumerable<LisChannel> lisChannels) : base(model)
    {
        Report = CreateChild(model.Report ??= new MethodReporting(), m => new MethodReportViewModel(m));
        CalibratorCurve = CreateChild(model.CalibratorCurve ??= new MethodCalibratorCurve(), m => new MethodCalibratorsViewModel(m));
        CalibratorCurve.AllowMonthlyCalibration = model.AllowMonthlyCalibration;

        LisChannels = lisChannels.Select(x => new DropDownOption<int?>
        {
            Text = x.Name,
            Value = x.Id
        }).ToList();
    }

    public MethodReportViewModel Report { get; }
    public MethodCalibratorsViewModel CalibratorCurve { get; }
    public List<DropDownOption<int?>> LisChannels { get; }

    public bool Enabled
    {
        get => Model.Enabled;
        set => SetProperty(Model.Enabled, value, v => Model.Enabled = v);
    }
    public string MethodNameLis
    {
        get => Model.MethodNameLis;
        set => SetProperty(Model.MethodNameLis, value ?? string.Empty, v => Model.MethodNameLis = v);
    }
    public int? DefaultLisChannelId
    {
        get => Model.DefaultLisChannelId ?? LisChannels.FirstOrDefault()?.Value;
        set => SetProperty(Model.DefaultLisChannelId, value, v => Model.DefaultLisChannelId = v);
    }

    public void ApplyClassTableDetails(MethodClassTableDetailsViewModel classTableDetails)
    {
        var classTable = Model.ClassTables?.FirstOrDefault(x => x.ClassTableId == classTableDetails.ClassTableId);
        if (classTable is null)
        {
            return;
        }

        var hasChanges = false;
        var belowRow = classTableDetails.Rows.FirstOrDefault(x => x.ClassTableRowId is null);
        if (belowRow is not null && classTable.TextBelow != belowRow.Text)
        {
            classTable.TextBelow = belowRow.Text ?? string.Empty;
            hasChanges = true;
        }

        foreach (var detailsRow in classTableDetails.Rows.Where(x => x.ClassTableRowId is not null))
        {
            var classTableRow = classTable.ClassTableRow?.FirstOrDefault(x => x.ClassTableRowId == detailsRow.ClassTableRowId);
            if (classTableRow is not null && classTableRow.TextAbove != detailsRow.Text)
            {
                classTableRow.TextAbove = detailsRow.Text ?? string.Empty;
                hasChanges = true;
            }
        }

        if (hasChanges)
        {
            SetAsModified();
        }
    }

    protected override void Disposing()
    {
        Report.PropertyChanged -= OnChildPropertyChanged;
        CalibratorCurve.PropertyChanged -= OnChildPropertyChanged;
    }

    private T CreateChild<TModel, T>(TModel model, Func<TModel, T> factory) where T : INotifyPropertyChanged
    {
        var child = factory(model);
        child.PropertyChanged += OnChildPropertyChanged;
        return child;
    }

    private void OnChildPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        SetAsModified();
    }
}