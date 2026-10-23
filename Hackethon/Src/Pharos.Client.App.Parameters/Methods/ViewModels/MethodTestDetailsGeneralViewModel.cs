using Pharos.Common.Web.Components.FormElements;
using Pharos.Contracts.Parameters;

namespace Pharos.Client.App.Parameters.Methods.ViewModels;

public sealed class MethodTestDetailsGeneralViewModel : ViewModelItem<MethodTestDetails>
{
    public MethodTestDetailsGeneralViewModel(MethodTestDetails model) : base(model)
    {
        FillDropDownForDilutionMethods();
        FillDropdownForDilutionFactors(Model.Technology);
    }

    public List<DropDownOption<DilutionMethod>> DilutionMethodDropDownValues { get; private set; } = new List<DropDownOption<DilutionMethod>>();

    public List<DropDownOption<int>> DilutionFactorDropDownValues { get; private set; } = new List<DropDownOption<int>>();

    public int Replicates
    {
        get => Model.Replicates;
        set => SetProperty(Model.Replicates, value, v => Model.Replicates = v);
    }

    public bool Enabled
    {
        get => Model.Active;
        set => SetProperty(Model.Active, value, v => Model.Active = v);
    }

    public DilutionMethod DilutionMethod
    {
        get => Model.DilutionMethod;
        set => SetProperty(Model.DilutionMethod, value, v => Model.DilutionMethod = v);
    }

    public int DilutionFactor
    {
        get => Model.DilutionFactor;
        set => SetProperty(Model.DilutionFactor, value, v => Model.DilutionFactor = v);
    }

    private void FillDropdownForDilutionFactors(AssayTechnology technology)
    {
        var allFactors = technology switch
        {
            AssayTechnology.ImmunoCap => InstrumentDilutionFactors.ImmunoCap,
            AssayTechnology.ImmunoCapIgG => InstrumentDilutionFactors.ImmunoCapIgG,
            AssayTechnology.EliA => InstrumentDilutionFactors.Elia,
            AssayTechnology.ViewAllergy => InstrumentDilutionFactors.ViewAllergy,
            _ => throw new ArgumentOutOfRangeException(nameof(technology), technology, null)
        };
        DilutionFactorDropDownValues = allFactors
            .Where(f => f >= Model.DefaultDilutionFactor)
            .Select(f => new DropDownOption<int> { Value = f, Text = f.ToString()})
            .ToList();
    }

    private void FillDropDownForDilutionMethods()
    {
        var methods = Enum.GetValues<DilutionMethod>();
        DilutionMethodDropDownValues = methods
            .Select(m => new DropDownOption<DilutionMethod> { Value = m, Text = m.ToLocalizedString("TestDetailsGeneralTab") })
            .ToList();
    }
}
