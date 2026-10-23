using Pharos.Common.Web.Components.FormElements;

namespace Pharos.Client.App.Parameters.Methods.ViewModels;

public sealed class MethodTestDetailsReportViewModel : ViewModelItem<MethodTestDetails>
{
    public MethodTestDetailsReportViewModel(MethodTestDetails model) : base(model)
    {
        FillConcentrationUnitDropDownValues();
        FillReportClassIdDropDownValues();
        FillReportQuotientDropDownValues();
    }

    public List<DropDownOption<int>> ConcentrationUnitDropDownValues { get; private set; } = [];

    public List<DropDownOption<int>> ReportClassIdDropDownValues { get; private set; } = [];

    public List<DropDownOption<QuotientReportMode>> ReportQuotientDropDownValues { get; private set; } = [];

    public bool ReportConcentration
    {
        get => Model.ReportConcentration;
        set
        {
            if (SetProperty(Model.ReportConcentration, value, v => Model.ReportConcentration = v))
            {
                OnPropertyChanged(nameof(CanEditConcentrationUnit));
            }
        }
    }

    public bool DefaultReportConcentration => Model.DefaultReportConcentration;

    public bool CanEditConcentrationUnit => ReportConcentration;

    public int MethodConcentrationUnitId
    {
        get => Model.MethodConcentrationUnitId;
        set => SetProperty(Model.MethodConcentrationUnitId, value, v => Model.MethodConcentrationUnitId = v);
    }

    public string DefaultConcentrationUnit => Model.DefaultConcentrationUnit;

    public bool ReportCutOff
    {
        get => Model.ReportCutOff;
        set
        {
            if (SetProperty(Model.ReportCutOff, value, v => Model.ReportCutOff = v))
            {
                if (value && Model.CutOffConcentration is null)
                {
                    CutOffConcentration = 0;
                }

                OnPropertyChanged(nameof(CanEditCutOffConcentration));
            }
        }
    }

    public bool DefaultReportCutOff => Model.DefaultReportCutOff;

    public bool CanEditCutOffConcentration => ReportCutOff;

    public double CutOffConcentration
    {
        get => Model.CutOffConcentration ?? default;
        set => SetProperty(Model.CutOffConcentration ?? default, value, v => Model.CutOffConcentration = v);
    }

    public double? DefaultCutOffConcentration => Model.DefaultCutOffConcentration;

    public bool ReportClass
    {
        get => Model.ReportClass;
        set
        {
            if (!SetProperty(Model.ReportClass, value, v => Model.ReportClass = v))
            {
                return;
            }

            if (!value)
            {
                ReportClassId = 0;
            }

            OnPropertyChanged(nameof(CanEditClassTable));
        }
    }

    public bool CanEditClassTable => ReportClass;

    public int ReportClassId
    {
        get => Model.ReportClassId;
        set
        {
            if (!SetProperty(Model.ReportClassId, value, v => Model.ReportClassId = v))
            {
                return;
            }

            var reportClass = value != 0;
            if (Model.ReportClass != reportClass)
            {
                SetProperty(Model.ReportClass, reportClass, v => Model.ReportClass = v, nameof(ReportClass));
                OnPropertyChanged(nameof(CanEditClassTable));
            }
        }
    }

    public int DefaultReportClassId => Model.DefaultReportClassId;

    public QuotientReportMode ReportQuotient
    {
        get => Model.ReportQuotient;
        set => SetProperty(Model.ReportQuotient, value, v => Model.ReportQuotient = v);
    }

    public QuotientReportMode DefaultReportQuotient => Model.DefaultReportQuotient;

    public string? TextQuantitativeBelow
    {
        get => Model.TextQuantitativeBelow;
        set => SetProperty(Model.TextQuantitativeBelow, value, v => Model.TextQuantitativeBelow = v);
    }

    public string? TextQuantitativeAbove
    {
        get => Model.TextQuantitativeAbove;
        set => SetProperty(Model.TextQuantitativeAbove, value, v => Model.TextQuantitativeAbove = v);
    }

    public string? TextQuantitativeBelowForDilutedTest
    {
        get => Model.TextQuantitativeBelowForDilutedTest;
        set => SetProperty(Model.TextQuantitativeBelowForDilutedTest, value, v => Model.TextQuantitativeBelowForDilutedTest = v);
    }

    public string? TextQuantitativeAboveForDilutedTest
    {
        get => Model.TextQuantitativeAboveForDilutedTest;
        set => SetProperty(Model.TextQuantitativeAboveForDilutedTest, value, v => Model.TextQuantitativeAboveForDilutedTest = v);
    }

    private void FillReportQuotientDropDownValues()
    {
        ReportQuotientDropDownValues = Enum.GetValues<QuotientReportMode>()
            .Select(x => new DropDownOption<QuotientReportMode>
            {
                Text = x.ToLocalizedString("MethodDetailsTestTab"),
                Value = x
            })
            .ToList();
    }

    private void FillConcentrationUnitDropDownValues()
    {
        ConcentrationUnitDropDownValues = (Model.ConcentrationUnits ?? [])
            .Select(x => new DropDownOption<int>
            {
                Text = x.ConcentrationUnit,
                Value = x.UnitId
            })
            .ToList();
    }

    private void FillReportClassIdDropDownValues()
    {
        ReportClassIdDropDownValues =
        [
            new DropDownOption<int>
            {
                Text = 0.ToString(),
                Value = 0
            },
            .. (Model.ClassTables ?? [])
                .OrderBy(x => x.ClassTableId)
                .Select(x => new DropDownOption<int>
                {
                    Text = x.DisplayName,
                    Value = x.ClassTableId
                })
        ];
    }
}
