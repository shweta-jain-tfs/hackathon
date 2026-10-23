using FluentValidation;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Reflex;

namespace Pharos.Server.Reflex.Application.Commands.UpdateReflexDefinitionCommand;

internal class UpdateReflexDefinitionCommandValidator : AbstractValidator<UpdateReflexDefinitionCommand>
{
    public UpdateReflexDefinitionCommandValidator()
    {
        RuleFor(c => c.ReflexDefinitionQueriedAt).NotEmpty().NotEqual(DateTimeOffset.MinValue);
        RuleForEach(c => c.ReflexTests).SetValidator(new UpdatedReflexTestValidator());
        RuleFor(c => c.ReflexCondition).NotNull().NotEmpty().WithMessage("Reflex definition must have a condition");
        RuleFor(c => c.ReflexCondition).NotNull().NotEmpty().SetValidator(new UpdatedReflexConditionValidator());
    }
}

internal class UpdatedReflexConditionValidator : AbstractValidator<UpdatedReflexCondition?>
{
    public UpdatedReflexConditionValidator()
    {
        RuleFor(c => c!.TestName).NotEmpty().WithMessage("Reflex definition must have a condition with test name");
        RuleFor(c => c!.MethodName).NotEmpty().WithMessage("Reflex definition must have a condition with method name");
        RuleFor(c => c!.ResultType).Must((dto, resultType) =>
        {
            switch (resultType)
            {
                case ResultType.Default:
                    return false;
                case ResultType.Concentration:
                    if (dto!.ReportingConcentration)
                    {
                        return true;
                    }
                    return false;
                case ResultType.Class:
                    if (dto!.ReportingClass)
                    {
                        return true;
                    }
                    return false;
                case ResultType.CutOff:
                    if (dto!.ReportingCutOff)
                    {
                        return true;
                    }
                    return false;
                case ResultType.Quotient:
                    if (dto!.ReportingQuotient)
                    {
                        return true;
                    }
                    return false;
                case ResultType.ConcentrationRaw:
                    if (dto!.ReportingConcentration)
                    {
                        return true;
                    }
                    return false;
                case ResultType.RU:
                    return true;
                default:
                    throw new ArgumentOutOfRangeException(nameof(resultType), resultType, null);
            }
        }).WithMessage("Selected condition result type is not supported for the selected test");

        RuleFor(c => c!.CompareType).NotEqual(CompareType.Default).WithMessage("Compare type must be selected");

        RuleFor(c => c!.ResultType).Must((dto, resultType) =>
        {
            switch (resultType)
            {
                case ResultType.Default:
                    return false;
                case ResultType.Concentration:
                    switch (dto!.CompareType)
                    {
                        case CompareType.Default:
                        case CompareType.EqualTo:
                            return false;
                        case CompareType.Above:
                        case CompareType.Below:
                            if (dto.ConcentrationCompareValue == null)
                            {
                                return false;
                            }

                            return true;
                        case CompareType.Between:
                            if (dto.ConcentrationCompareValue == null || dto.ConcentrationCompareValueUpperLimit == null)
                            {
                                return false;
                            }

                            return true;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                case ResultType.Class:
                    switch (dto!.CompareType)
                    {
                        case CompareType.Default:
                        case CompareType.Above:
                        case CompareType.Below:
                        case CompareType.Between:
                            return false;
                        case CompareType.EqualTo:
                            if (dto.ClassCompareValue is null or "")
                            {
                                return false;
                            }

                            return true;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                case ResultType.CutOff:
                    switch (dto!.CompareType)
                    {
                        case CompareType.Default:
                        case CompareType.Above:
                        case CompareType.Below:
                        case CompareType.Between:
                            return false;
                        case CompareType.EqualTo:
                            if (dto.CutOffCompareValue is null or "")
                            {
                                return false;
                            }

                            return true;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                case ResultType.Quotient:
                    switch (dto!.CompareType)
                    {
                        case CompareType.Default:
                        case CompareType.EqualTo:
                            return false;
                        case CompareType.Above:
                        case CompareType.Below:
                            if (dto.QuotientCompareValue is null)
                            {
                                return false;
                            }

                            return true;
                        case CompareType.Between:
                            if (dto.QuotientCompareValue == null || dto.QuotientCompareValueUpperLimit == null)
                            {
                                return false;
                            }

                            return true;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                case ResultType.ConcentrationRaw:
                    switch (dto!.CompareType)
                    {
                        case CompareType.Default:
                        case CompareType.EqualTo:
                            return false;
                        case CompareType.Above:
                        case CompareType.Below:
                            if (dto.ConcentrationRawCompareValue == null)
                            {
                                return false;
                            }

                            return true;
                        case CompareType.Between:
                            if (dto.ConcentrationRawCompareValue == null || dto.ConcentrationRawCompareValueUpperLimit == null)
                            {
                                return false;
                            }

                            return true;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                case ResultType.RU:
                    switch (dto!.CompareType)
                    {
                        case CompareType.Default:
                        case CompareType.EqualTo: 
                        case CompareType.Between:
                        case CompareType.Above:
                            return false;
                        case CompareType.Below:
                            if (dto.RuCompareValue is null)
                            {
                                return false;
                            }
                            return true;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(resultType), resultType, null);
            }
        }).WithMessage("Compare type or compare value is empty or incorrect");

        RuleFor(c => c!.CompareType).Must((dto, compareType) =>
        {
            if (compareType == CompareType.Between)
            {
                if (dto!.ResultType == ResultType.Concentration)
                {
                    if (dto.ConcentrationCompareValue >= dto.ConcentrationCompareValueUpperLimit)
                    {
                        return false;
                    }
                }
                else if (dto.ResultType == ResultType.ConcentrationRaw)
                {
                    if (dto.ConcentrationRawCompareValue >= dto.ConcentrationRawCompareValueUpperLimit)
                    {
                        return false;
                    }
                }
                else if (dto.ResultType == ResultType.Quotient)  
                {
                    if (dto.QuotientCompareValue >= dto.QuotientCompareValueUpperLimit)
                    {
                        return false;
                    }
                }
            }

            return true;
        }).WithMessage("Condition upper value is greater than lower value");

        // Validate that the compare values are within the acceptable range for each type
        RuleFor(c => c!.ConcentrationCompareValue)
            .InclusiveBetween(0d, 10000d)
            .When(c => c!.ConcentrationCompareValue.HasValue);

        RuleFor(c => c!.ConcentrationCompareValueUpperLimit)
            .InclusiveBetween(0d, 10000d)
            .When(c => c!.ConcentrationCompareValueUpperLimit.HasValue);

        RuleFor(c => c!.ConcentrationRawCompareValue)
            .InclusiveBetween(0d, 10000d)
            .When(c => c!.ConcentrationRawCompareValue.HasValue);

        RuleFor(c => c!.ConcentrationRawCompareValueUpperLimit)
            .InclusiveBetween(0d, 10000d)
            .When(c => c!.ConcentrationRawCompareValueUpperLimit.HasValue);

        RuleFor(c => c!.QuotientCompareValue)
            .InclusiveBetween(0d, 10000d)
            .When(c => c!.QuotientCompareValue.HasValue);

        RuleFor(c => c!.QuotientCompareValueUpperLimit)
            .InclusiveBetween(0d, 10000d)
            .When(c => c!.QuotientCompareValueUpperLimit.HasValue);

        RuleFor(c => c!.RuCompareValue)
            .InclusiveBetween(6d, 100d)
            .When(c => c!.RuCompareValue.HasValue);
    }
}

internal class UpdatedReflexTestValidator : AbstractValidator<UpdatedReflexTest>
{
    public UpdatedReflexTestValidator()
    {
        RuleFor(c => c.TestName).NotEmpty().MaximumLength(Constants.TestNameLength);
        RuleFor(c => c.MethodName).NotEmpty().MaximumLength(Constants.MethodNameLength);
        RuleFor(c => c.NumberOfReplicates).NotNull();
    }
}
