namespace Pharos.Client.App.Parameters.Panels.ViewModels;

public sealed class PanelTestViewModel : ViewModelItem<MethodPanelTest>, IEquatable<PanelTestViewModel>
{
    public PanelTestViewModel(MethodPanelTest model, bool isModified = false) : base(model)
    {
        if (isModified)
        {
            SetAsModified();
        }
    }

    public string? TestName => Model.TestName;

    public string? MethodName => Model.MethodName;

    public string? FullTestName => Model.FullTestName;

    public string? FullMethodName => Model.FullMethodName;

    public int Replicates
    {
        get => Model.Replicates;
        set => SetProperty(Model.Replicates, value, v => Model.Replicates = v);
    }

    public int InstrumentDilutionFactor
    {
        get => Model.InstrumentDilutionFactor;
        set => SetProperty(Model.InstrumentDilutionFactor, value, v => Model.InstrumentDilutionFactor = v);
    }

    public int? SortOrder
    {
        get => Model.SortOrder;
        set => SetProperty(Model.SortOrder, value, v => Model.SortOrder = v);
    }

    public bool Equals(PanelTestViewModel? other)
    {
        return Model.Equals(other?.Model);
    }

    public override bool Equals(object? obj)
    {
        return obj is PanelTestViewModel other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Model.GetHashCode();
    }
}