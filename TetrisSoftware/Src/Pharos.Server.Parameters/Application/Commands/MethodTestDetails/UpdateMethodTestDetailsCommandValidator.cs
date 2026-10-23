using System.Text;
using FluentValidation;
using Pharos.Contracts.Security;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Localization;

namespace Pharos.Server.Parameters.Application.Commands.MethodTestDetails;

internal class UpdateMethodTestDetailsCommandValidator : AbstractValidator<UpdateMethodTestDetailsCommand>
{
    private static readonly CompositeFormat _tooManyLaboratoryNamesFormat = CompositeFormat.Parse(Translations.TooManyLaboratoryNames);

    public UpdateMethodTestDetailsCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);

        RuleFor(x => x.QueriedAt)
            .NotEmpty()
            .NotEqual(DateTimeOffset.MinValue);

        RuleFor(x => x.MethodTestLabNames)
            .Must(x => x == null || x.Count <= LaboratoryTest.MaximumNumberOfNames)
            .WithMessage(string.Format(null, _tooManyLaboratoryNamesFormat, LaboratoryTest.MaximumNumberOfNames));
        When(x => x.ReportConcentration, () =>
        {
            RuleFor(x => x.MethodConcentrationUnitId)
                .GreaterThan(0);
        });

        When(x => x.ReportCutOff, () =>
        {
            RuleFor(x => x.CutOffConcentration)
                .NotNull()
                .InclusiveBetween(0, 99999);
        });

        When(x => x.CutOffConcentration is not null, () =>
        {
            RuleFor(x => x.CutOffConcentration)
                .InclusiveBetween(0, 99999);
        });

        When(x => x.ReportClass, () =>
        {
            RuleFor(x => x.ReportClassId)
                .GreaterThan(0);
        });

        When(x => !x.ReportClass, () =>
        {
            RuleFor(x => x.ReportClassId)
                .Equal(0);
        });

        RuleFor(x => x.TextQuantitativeBelow)
            .MaximumLength(MethodTest.TextQuantitativeResultLength);

        RuleFor(x => x.TextQuantitativeAbove)
            .MaximumLength(MethodTest.TextQuantitativeResultLength);

        RuleFor(x => x.TextQuantitativeBelowForDilutedTest)
            .MaximumLength(MethodTest.TextQuantitativeResultLength);

        RuleFor(x => x.TextQuantitativeAboveForDilutedTest)
            .MaximumLength(MethodTest.TextQuantitativeResultLength);
        RuleFor(x => x.MethodTestLabNames)
            .NotEmpty()
            .WithMessage("At least one laboratory test name is required.");

        RuleForEach(x => x.MethodTestLabNames)
            .ChildRules(labName =>
            {
                labName.RuleFor(x => x.TestName)
                    .NotEmpty()
                    .MaximumLength(Constants.LabTestNameLength);

                labName.RuleFor(x => x.SortOrder)
                    .GreaterThan(0);
            });

        RuleFor(x => x.MethodTestLabNames)
            .Must(HaveUniqueLaboratoryTestNames)
            .WithMessage("Laboratory test names must be unique.");

        RuleFor(x => x.MethodTestLabNames)
            .Must(HaveUniqueSortOrders)
            .WithMessage("Laboratory test name sort orders must be unique.");
    }

    private static bool HaveUniqueLaboratoryTestNames(IReadOnlyCollection<Pharos.Server.Application.Parameters.MethodTestLabNameDto>? laboratoryTestNames)
    {
        return laboratoryTestNames is null
               || laboratoryTestNames
                   .Select(x => x.TestName?.Trim())
                   .Distinct(StringComparer.OrdinalIgnoreCase)
                   .Count() == laboratoryTestNames.Count;
    }

    private static bool HaveUniqueSortOrders(IReadOnlyCollection<Pharos.Server.Application.Parameters.MethodTestLabNameDto>? laboratoryTestNames)
    {
        return laboratoryTestNames is null
               || laboratoryTestNames
                   .Select(x => x.SortOrder)
                   .Distinct()
                   .Count() == laboratoryTestNames.Count;
    }
}
