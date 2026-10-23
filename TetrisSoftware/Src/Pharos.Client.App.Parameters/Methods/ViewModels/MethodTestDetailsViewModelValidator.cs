using FluentValidation;

namespace Pharos.Client.App.Parameters.Methods.ViewModels;

public sealed class MethodTestDetailsViewModelValidator : AbstractValidator<MethodTestDetailsGeneralViewModel>
{
    private const int _replicatesLowerLimit = 1;
    private const int _replicatesUpperLimit = 5;

    public MethodTestDetailsViewModelValidator(IStringLocalizer<Translations> loc)
    {
        RuleFor(x => x.Replicates)
            .InclusiveBetween(_replicatesLowerLimit, _replicatesUpperLimit)
            .WithMessage(string.Format(loc["TestDetailsGeneralTab_ReplicatesError"], _replicatesLowerLimit, _replicatesUpperLimit));
    }
}

public sealed class MethodTestDetailsAdvancedViewModelValidator : AbstractValidator<MethodTestDetailsAdvancedViewModel>
{
    private const int _minOrderInPriorityGroupA = 0;
    private const int _maxOrderInPriorityGroupA = 9999;
    private const int _minOrderInPriorityGroupB = 0;
    private const int _maxOrderInPriorityGroupB = 9999;

    public MethodTestDetailsAdvancedViewModelValidator(IStringLocalizer<Translations> loc)
    {
        RuleFor(x => x.OrderInPriorityGroupA)
            .InclusiveBetween(_minOrderInPriorityGroupA, _maxOrderInPriorityGroupA)
            .WithMessage(string.Format(loc["TestDetailsAdvancedTab_OrderInPriorityGroupError"], _minOrderInPriorityGroupA, _maxOrderInPriorityGroupA));

        RuleFor(x => x.OrderInPriorityGroupB)
            .InclusiveBetween(_minOrderInPriorityGroupB, _maxOrderInPriorityGroupB)
            .WithMessage(string.Format(loc["TestDetailsAdvancedTab_OrderInPriorityGroupError"], _minOrderInPriorityGroupB, _maxOrderInPriorityGroupB));
    }
}

