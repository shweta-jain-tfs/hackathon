using MediatR;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Localization;
using Pharos.Server.Result.Application.Events;
using Pharos.Server.Result.Application.Interfaces;

namespace Pharos.Server.Result.Application.Commands;

public record UpdateQualityControlCommentCommand : IRequest
{
    public int Id { get; set; }
    public string? Comment { get; set; }
}

internal sealed class UpdateQualityControlCommentCommandHandler(IResultDbContext context) : IRequestHandler<UpdateQualityControlCommentCommand>
{
    public async Task Handle(UpdateQualityControlCommentCommand request, CancellationToken cancellationToken)
    {
        var resultEntity = await context.QualityControlTestResults
            .Include(x => x.Replicates)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (resultEntity == null)
        {
            throw new ObjectGoneException(Translations.ObjectGone);
        }

        resultEntity.OperatorComment = request.Comment;
        
        resultEntity.AddDomainEvent(new TestResultUpdatedEvent(resultEntity));
        
        await context.SaveChangesAsync(cancellationToken);
    }
}
