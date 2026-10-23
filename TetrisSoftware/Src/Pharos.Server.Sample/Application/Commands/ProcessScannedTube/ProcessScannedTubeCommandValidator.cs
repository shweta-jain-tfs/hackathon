using FluentValidation;
using Pharos.Server.Application.Sample;
using Pharos.Server.Domain;

namespace Pharos.Server.Sample.Application.Commands.ProcessScannedTube;

internal class ProcessScannedTubeCommandValidator : AbstractValidator<ProcessScannedTubeCommand>
{
    public ProcessScannedTubeCommandValidator()
    {
        RuleFor(c => c.Message.TubeBarcode).MaximumLength(Constants.SampleIdMaxLength);
        RuleFor(c => c.Message.CurrentPosition.TrayBarcode).MaximumLength(Constants.TrayIdLength);
        RuleFor(c => c.Message.CurrentPosition.TrayPosition).InclusiveBetween(1, Constants.MaxTraysInSampleDrawer);
        RuleFor(c => c.Message.CurrentPosition.TubePosition).InclusiveBetween(1, Constants.MaxSampleTubesInTray);
    }
}