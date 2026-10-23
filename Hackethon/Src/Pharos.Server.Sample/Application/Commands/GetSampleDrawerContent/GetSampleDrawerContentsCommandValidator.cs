using FluentValidation;
using Pharos.Server.Application.Sample;
using Pharos.Server.Domain;
using Tetris.Protocol.Common;
using Tetris.Protocol.Pharos.Tray;

namespace Pharos.Server.Sample.Application.Commands.GetSampleDrawerContent;

internal class GetSampleDrawerContentsCommandValidator : AbstractValidator<GetSampleDrawerContentsCommand>
{
    public GetSampleDrawerContentsCommandValidator()
    {
        RuleFor(x => x.Message.Drawer).Must(x => x != EnumSampleDrawer.UnspecifiedSampleDrawer);
        RuleForEach(x => x.Message.SampleTrays).SetValidator(new DetectedSampleTrayValidator());
    }
}

internal class DetectedSampleTrayValidator : AbstractValidator<DetectedSampleTray>
{
    public DetectedSampleTrayValidator()
    {
        RuleFor(x => x.TrayPosition).InclusiveBetween(1, Constants.MaxTraysInSampleDrawer);
        RuleForEach(x => x.DetectedTubes).SetValidator(new DetectedSampleTubeValidator());
    }
}

internal class DetectedSampleTubeValidator : AbstractValidator<DetectedSampleTube>
{
    public DetectedSampleTubeValidator()
    {
        RuleFor(x => x.SamplePosition).InclusiveBetween(1, Constants.MaxSampleTubesInTray);
    }
}