using FluentValidation;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Parameters.Application.Queries.GetMethods;

namespace Pharos.Server.Parameters.Application.Commands;

internal sealed class UpdateMethodCommandValidator : AbstractValidator<UpdateMethodCommand>
{
    public UpdateMethodCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);

        RuleFor(x => x.Report)
            .NotEmpty();

        RuleFor(x => x.Report!)
            .SetValidator(new MethodReportingDtoValidator());

        RuleForEach(x => x.ClassTables)
            .SetValidator(new MethodClassTableDtoValidator());
    }
}

internal sealed class MethodReportingDtoValidator : AbstractValidator<MethodReportingDto>
{
    private const int _minConcentrationLowerLimit = 0;
    private const int _minConcentrationUpperLimit = 99999;

    public MethodReportingDtoValidator()
    {
        RuleFor(x => x.MinConcentrationToReport)
            .InclusiveBetween(_minConcentrationLowerLimit, _minConcentrationUpperLimit);

        RuleFor(x => x.TextBelowCutOff)
            .MaximumLength(MethodReporting.ClassResultTextLength);

        RuleFor(x => x.TextAboveCutOff)
            .MaximumLength(MethodReporting.ClassResultTextLength);

        RuleFor(x => x.TextQuantitativeBelow)
            .MaximumLength(MethodReporting.QuantitativeResultTextLength);

        RuleFor(x => x.TextQuantitativeDoubtful)
            .MaximumLength(MethodReporting.QuantitativeResultTextLength);

        RuleFor(x => x.TextQuantitativeAbove)
            .MaximumLength(MethodReporting.QuantitativeResultTextLength);
    }
}

internal sealed class MethodClassTableDtoValidator : AbstractValidator<MethodClassTableDto>
{
    public MethodClassTableDtoValidator()
    {
        RuleFor(x => x.ClassTableId)
            .InclusiveBetween(1, 20);

        RuleFor(x => x.TextBelow)
            .NotEmpty()
            .MaximumLength(MethodReporting.ClassResultTextLength);

        RuleForEach(x => x.ClassTableRow)
            .SetValidator(new ClassTableRowInfoDtoValidator());
    }
}

internal sealed class ClassTableRowInfoDtoValidator : AbstractValidator<ClassTableRowInfoDto>
{
    public ClassTableRowInfoDtoValidator()
    {
        RuleFor(x => x.ClassTableRowId)
            .InclusiveBetween(1, 10);

        RuleFor(x => x.TextAbove)
            .NotEmpty()
            .MaximumLength(MethodReporting.ClassResultTextLength);
    }
}
