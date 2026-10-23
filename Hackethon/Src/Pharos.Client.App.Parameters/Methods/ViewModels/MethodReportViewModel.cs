namespace Pharos.Client.App.Parameters.Methods.ViewModels;

public sealed class MethodReportViewModel : ViewModelItem<MethodReporting>
{
    public MethodReportViewModel(MethodReporting model) : base(model)
    {
    }

    public double MinConcentrationToReport
    {
        get => Model.MinConcentrationToReport;
        set => SetProperty(Model.MinConcentrationToReport, value, v => Model.MinConcentrationToReport = v);
    }

    public double DefaultMinConcentrationToReport => Model.DefaultMinConcentrationToReport;

    public string? TextBelowCutOff
    {
        get => Model.TextBelowCutOff;
        set => SetProperty(Model.TextBelowCutOff, value, v => Model.TextBelowCutOff = v);
    }

    public string? DefaultTextBelowCutOff => Model.DefaultTextBelowCutOff;

    public string? TextAboveCutOff
    {
        get => Model.TextAboveCutOff;
        set => SetProperty(Model.TextAboveCutOff, value, v => Model.TextAboveCutOff = v);
    }

    public string? DefaultTextAboveCutOff => Model.DefaultTextAboveCutOff;

    public string? TextQuantitativeBelow
    {
        get => Model.TextQuantitativeBelow;
        set => SetProperty(Model.TextQuantitativeBelow, value, v => Model.TextQuantitativeBelow = v);
    }

    public string? DefaultTextQuantitativeBelow => Model.DefaultTextQuantitativeBelow;

    public string? TextQuantitativeDoubtful
    {
        get => Model.TextQuantitativeDoubtful;
        set => SetProperty(Model.TextQuantitativeDoubtful, value, v => Model.TextQuantitativeDoubtful = v);
    }

    public string? DefaultTextQuantitativeDoubtful => Model.DefaultTextQuantitativeDoubtful;

    public string? TextQuantitativeAbove
    {
        get => Model.TextQuantitativeAbove;
        set => SetProperty(Model.TextQuantitativeAbove, value, v => Model.TextQuantitativeAbove = v);
    }

    public string? DefaultTextQuantitativeAbove => Model.DefaultTextQuantitativeAbove;

    public bool UseCalculatedTextQuantitativeAbove
    {
        get => Model.UseCalculatedTextQuantitativeAbove;
        set => SetProperty(Model.UseCalculatedTextQuantitativeAbove, value, v => Model.UseCalculatedTextQuantitativeAbove = v);
    }
}
