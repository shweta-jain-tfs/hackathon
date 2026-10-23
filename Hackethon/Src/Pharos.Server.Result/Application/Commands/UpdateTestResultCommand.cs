using MediatR;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Localization;
using Pharos.Server.Result.Application.Interfaces;

namespace Pharos.Server.Result.Application.Commands;

public class UpdateTestResultCommand : IRequest
{
    public int Id { get; set; }
    public string? OperatorComment { get; set; }
}

internal sealed class UpdateTestResultCommandHandler(IResultDbContext context) : IRequestHandler<UpdateTestResultCommand>
{
    public async Task Handle(UpdateTestResultCommand request, CancellationToken cancellationToken)
    {
        var resultEntity = await context.TestResults
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (resultEntity == null)
        {
            throw new ObjectGoneException(Translations.ObjectGone);
        }

        //Update the operator comment
        resultEntity.OperatorComment = request.OperatorComment;

        await context.SaveChangesAsync(cancellationToken);
    }
}