using FluentValidation;
using Pharos.Server.Domain;

namespace Pharos.Server.Result.Application.Commands;

internal class UpdateTestResultCommandValidator : AbstractValidator<UpdateTestResultCommand>
{
    public UpdateTestResultCommandValidator()
    {
        RuleFor(x => x.OperatorComment).MaximumLength(Constants.OperatorCommentLength).WithMessage("Comment cannot be longer than 40 characters");
    }
}