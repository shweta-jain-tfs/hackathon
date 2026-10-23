using System.Net;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Instrument.Application.Events;
using Pharos.Server.Instrument.Application.Interfaces;
using Pharos.Server.Instrument.Application.Mapping;
using Tetris.Protocol.Pharos;

namespace Pharos.Server.Instrument.Application.Commands.ProcessModuleInformation;

internal record ProcessModuleInformationCommand(IPAddress IpAddress, Guid SessionId, ModuleInformation Message) : IRequest;

internal class ProcessModuleInformationHandler(
    IInstrumentDbContext dbContext,
    IPublisher publisher,
    IInstrumentManager instrumentManager)
    : IRequestHandler<ProcessModuleInformationCommand>
{
    public async Task Handle(ProcessModuleInformationCommand request, CancellationToken cancellationToken)
    {
        var message = request.Message;

        var instrumentInMemory = (Domain.Instrument)instrumentManager.GetInstrument(message.SerialNumber);

        var instrumentType = request.Message.Type switch
        {
            EnumInstrumentType.Small => InstrumentType.Small,
            _ => InstrumentType.Medium
        };

        if (instrumentInMemory.InstrumentType != instrumentType)
        {
            var instrument = await dbContext.Instruments
                .Where(i => i.SerialNumber == message.SerialNumber)
                .FirstAsync(cancellationToken);

            instrument.InstrumentType = instrumentType;
            instrumentInMemory.InstrumentType = instrumentType;
            instrument.AddDomainEvent(new InstrumentUpdatedEvent(instrument, false));
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        instrumentInMemory.InstallationDate = message.InstallationDate.ToDateTimeOffset();
        instrumentInMemory.Modules = message.Modules?.Select(m => new HardwareModule
        {
            ModuleType = m.Type.ToModuleType(),
            ArticleNumber = m.ArticleNumber,
            Model = m.Model,
            Version = m.Version,
            SerialNumber = m.SerialNumber,
            InstallationDate = m.InstallationDate?.ToDateTimeOffset(),
        }).ToList() ?? [];

        // HandleNotification module information received event to notify clients
        await publisher.Publish(new ModuleInformationReceivedEvent(instrumentInMemory.Id), cancellationToken);
    }
}