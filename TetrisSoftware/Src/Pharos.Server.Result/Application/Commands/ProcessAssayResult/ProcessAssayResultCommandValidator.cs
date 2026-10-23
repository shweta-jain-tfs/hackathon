using FluentValidation;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Result;
using Pharos.Server.Application.Settings.Request;
using Tetris.Protocol.Pharos.TestResult;

namespace Pharos.Server.Result.Application.Commands.ProcessAssayResult;

internal class ProcessAssayResultCommandValidator : AbstractValidator<ProcessAssayResultCommand>
{
    public ProcessAssayResultCommandValidator(IOptionsSnapshot<RequestSettings> requestSettings)
    {
        When(x => !string.IsNullOrWhiteSpace(x.Message.Run.AnalyticalRunId), () =>
        {
            RuleFor(x => x.Message.Run.AnalyticalRunId)
                .Must(x => Guid.TryParse(x, out _))
                .WithMessage("Analytical run ID must be a GUID");
        });

        When(x => !string.IsNullOrWhiteSpace(x.Message.Run.AssayRunId), () =>
        {
            RuleFor(x => x.Message.Run.AssayRunId)
                .Must(x => Guid.TryParse(x, out _))
                .WithMessage("Assay run ID must be a GUID");
        });

        // This rule is NOT in the sample assay result validator because we want the following rule to be checked before
        // PreProcessAssayResultCommand is executed (because ProcessSampleResultCommand is a nested command called from ProcessAssayResultCommandHandler).
        When(x => x.Message.TestType == EnumTestType.Sample, () =>
        {
            RuleFor(x => x.Message.Sample.SampleDate)
                .Must(x => x != null || !requestSettings.Value.ReuseSampleId)
                .WithMessage("SampleDate cannot be empty when Pharos is configured to reuse sample IDs.");

        });
    }
}
