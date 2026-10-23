using FluentValidation;
using Tetris.Protocol.Pharos;

namespace Pharos.Server.Instrument.Application.Validators;

internal class TestOrderStatusChangeValidator : AbstractValidator<TestOrderStatusChange>
{
    public TestOrderStatusChangeValidator()
    {
        RuleFor(x => x.TubeId)
            .NotEmpty()
            .MaximumLength(64);

        RuleFor(x => x.TestOrderStatus.Status)
            .NotEqual(EnumTestOrderStatus.UnspecifiedTestOrderStatus)
            .IsInEnum();

        RuleFor(x => x.TestOrderStatus.TestOrderId)
            .NotEmpty()
            .Must(x => Guid.TryParse(x, out _));
    }

}
