using FluentValidation;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Parameters.Methods;

namespace Pharos.Server.Parameters.Application.Commands.ImportMethodsAndTests;

internal class MethodTestValidator : AbstractValidator<MethodTest>
{
    public MethodTestValidator()
    {
        RuleFor(x => x.TestName).NotEmpty().MaximumLength(Constants.TestNameLength);
        RuleFor(x => x.FullTestName).MaximumLength(MethodTest.FullTestNameLength);
        RuleFor(x => x.DefaultReplicates).InclusiveBetween(1, 5);
        RuleFor(x => x.DefaultReportQuotient).IsInEnum();
        RuleFor(x => x.DefaultMethodConcentrationUnitId).InclusiveBetween(1, 10);
        RuleFor(x => x.HighestConcentrationToReport).InclusiveBetween(0, 99999);
        RuleFor(x => (decimal)(x.HighestConcentrationToReport ?? 0)).InclusiveBetween(0, 99999).PrecisionScale(8, 3, true); //needed?
        RuleFor(x => x.NumberOfDecimalsForConcentrationBelow10).InclusiveBetween(0, 2);
        RuleFor(x => x.DefaultDilutionFactor).InclusiveBetween(1, 10000);
        RuleFor(x => x.DefaultCutOffConcentration).InclusiveBetween(0, 99999);
        RuleFor(x => x.DefaultLabTestName).NotEmpty().MaximumLength(Constants.LabTestNameLength);
        RuleFor(x => x.DilutionMethod).IsInEnum();
        RuleFor(x => x.DiluentIdentity).MaximumLength(MethodTest.DiluentIdentityLength);
        RuleFor(x => x.DefaultTextQuantitativeAboveForDilutedTest).MaximumLength(MethodTest.TextQuantitativeResultLength);
        RuleFor(x => x.DefaultTextQuantitativeBelowForDilutedTest).MaximumLength(MethodTest.TextQuantitativeResultLength);
        RuleFor(x => x.NumberOfDecimalsForConcentrationAbove100).InclusiveBetween(0, 2);
        RuleFor(x => x.NumberOfDecimalsForConcentrationBetween10And100).InclusiveBetween(0, 2);
        RuleFor(x => x.SpecialMethodName).MaximumLength(Constants.MethodNameLength);
        When(x => x.Priority != null, () =>
        {
            RuleFor(x => x.Priority!.OrderInPriorityGroupA).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Priority!.OrderInPriorityGroupB).GreaterThanOrEqualTo(0);
        });

        RuleFor(x => x.AdjustmentForAlfaBeta1Beta2).InclusiveBetween(0, 99999);
    }
}