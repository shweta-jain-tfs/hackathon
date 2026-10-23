using FluentValidation;
using Pharos.Server.Sample.Application.Events;
using Pharos.Server.Sample.Application.Interfaces;
using Pharos.Server.Localization;
using Pharos.Server.Sample.Domain;

namespace Pharos.Server.Sample.Application.Commands.CreateSampleTray;

public class CreateSampleTrayCommand : IRequest<string>
{
    public string TrayId { get; set; } = string.Empty;
}

internal sealed class CreateSampleTrayCommandHandler(ISampleDbContext dbContext) : IRequestHandler<CreateSampleTrayCommand, string>
{
    public async Task<string> Handle(CreateSampleTrayCommand request, CancellationToken cancellationToken)
    {
        if (dbContext.SampleTrays.Any(s => s.Id == request.TrayId))
        {
            throw new ValidationException(Translations.SampleTrayAlreadyExists);
        }

        var sampleTray = new SampleTray
        {
            Id = request.TrayId,
        };

        sampleTray.AddDomainEvent(new SampleTrayCreatedEvent(sampleTray));

        await dbContext.SampleTrays.AddAsync(sampleTray, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return sampleTray.Id;
    }
}
