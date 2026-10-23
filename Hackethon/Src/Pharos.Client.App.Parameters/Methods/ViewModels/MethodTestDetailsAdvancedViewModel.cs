namespace Pharos.Client.App.Parameters.Methods.ViewModels;

public sealed class MethodTestDetailsAdvancedViewModel : ViewModelItem<MethodTestPriority>
{
    public MethodTestDetailsAdvancedViewModel(MethodTestPriority model) : base(model)
    {
    }

    public int? OrderInPriorityGroupA
    {
        get => Model.OrderInPriorityGroupA;
        set => SetProperty(Model.OrderInPriorityGroupA, value, v => Model.OrderInPriorityGroupA = v);
    }

    public bool IncludeInPriorityGroupA
    {
        get => Model.IncludeInPriorityGroupA;
        set => SetProperty(Model.IncludeInPriorityGroupA, value, v => Model.IncludeInPriorityGroupA = v);
    }

    public int? OrderInPriorityGroupB
    {
        get => Model.OrderInPriorityGroupB;
        set => SetProperty(Model.OrderInPriorityGroupB, value, v => Model.OrderInPriorityGroupB = v);
    }

    public bool IncludeInPriorityGroupB
    {
        get => Model.IncludeInPriorityGroupB;
        set => SetProperty(Model.IncludeInPriorityGroupB, value, v => Model.IncludeInPriorityGroupB = v);
    }
}
