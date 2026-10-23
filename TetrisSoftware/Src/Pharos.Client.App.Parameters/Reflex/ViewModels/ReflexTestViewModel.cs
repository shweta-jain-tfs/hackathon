namespace Pharos.Client.App.Parameters.Reflex.ViewModels;

public class ReflexTestViewModel : ViewModelItem<ReflexTest>, IEquatable<ReflexTestViewModel>
{
    public ReflexTestViewModel(ReflexTest model, bool isModified = false) : base(model)
    {
        if (isModified)
        {
            SetAsModified();
        }
    }

    public int InstrumentDilutionFactor
    {
        get => Model.InstrumentDilutionFactor;
        set => SetProperty(Model.InstrumentDilutionFactor, value, v => Model.InstrumentDilutionFactor = v);
    }

    public int NumberOfReplicates
    {
        get => Model.NumberOfReplicates;
        set => SetProperty(Model.NumberOfReplicates, value, v => Model.NumberOfReplicates = v);
    }

    public int TestSortOrder
    {
        get => Model.TestSortOrder;
        set => SetProperty(Model.TestSortOrder, value, v => Model.TestSortOrder = v);
    }

    public string? TestName => Model.TestName;

    public string? TestFullName => Model.TestFullName;

    public string? MethodName => Model.MethodName;

    public string? MethodFullName => Model.MethodFullName;

    public bool Equals(ReflexTestViewModel? other)
    {
        return Model.Equals(other?.Model);
    }

    public override bool Equals(object? obj)
    {
        return obj is ReflexTestViewModel other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Model.GetHashCode();
    }
}