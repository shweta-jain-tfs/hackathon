using FluentValidation;
using Pharos.Server.Application.LogDump;

namespace Pharos.Server.LogDump.Application.Commands.StartLogDump;

internal sealed class StartLogDumpCollectionCommandValidator : AbstractValidator<StartLogDumpCollectionCommand>
{
    public StartLogDumpCollectionCommandValidator()
    {
        RuleFor(command => command.Source).IsInEnum();
        RuleFor(command => command.Request.DateMode).IsInEnum();
        RuleFor(command => command.Request.PharosFiles).IsInEnum();
        RuleFor(command => command.Request.InstrumentFiles).IsInEnum();

        When(command => command.Request.DateMode == LogCollectionDateMode.SingleDate, () =>
        {
            RuleFor(command => command.Request.SingleDate)
                .NotNull()
                .WithMessage("Single date must be provided.");
        });

        When(command => command.Request.DateMode == LogCollectionDateMode.DateRange, () =>
        {
            RuleFor(command => command.Request.FromDate)
                .NotNull()
                .WithMessage("From date must be provided.");

            RuleFor(command => command.Request.ToDate)
                .NotNull()
                .WithMessage("To date must be provided.");

            RuleFor(command => command.Request)
                .Must(request => request.FromDate <= request.ToDate)
                .WithMessage("From date must be on or before the To date.");
        });
    }
}
