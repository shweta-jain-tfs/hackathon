using FluentValidation;
using Pharos.Server.Infrastructure;
using Pharos.Server.Request.Application.Queries.GetPatient;
using Pharos.Server.Domain;

namespace Pharos.Server.Request.Application.Commands;

internal class PatientValidator : AbstractValidator<PatientDto?>
{
    public PatientValidator(IDateTime dateTime)
    {
        When(x => x != null, () =>
        {
            RuleFor(x => x!.Name)
                .MaximumLength(Constants.PatientNameLength);

            RuleFor(x => x!.PatientNumber)
                .MaximumLength(Constants.PatientNumberLength);

            RuleFor(x => x!.MedicalRecordNumber)
                .MaximumLength(Constants.MedicalRecordNumberLength);

            RuleFor(x => x!.PatientId)
                .MaximumLength(Constants.PatientIdLength);

            RuleFor(x => x!.Address)
                .MaximumLength(Constants.PatientAddressLength);

            RuleFor(x => x!.ZipCode)
                .MaximumLength(Constants.PatientZipCodeLength);

            RuleFor(x => x!.Diagnosis)
                .MaximumLength(Constants.PatientDiagnosisLength);

            RuleFor(x => x!.Comment)
                .MaximumLength(Constants.PatientCommentLength);

            // Birth date must not be in the future
            RuleFor(x => x!.BirthDate)
                .Must(date => !date.HasValue || date.Value <= DateOnly.FromDateTime(dateTime.Now.Date))
                .WithMessage("Birth date cannot be in the future.");
        });
    }
}