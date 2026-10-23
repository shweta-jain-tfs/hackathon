using System.Net;
using MediatR;
using Moq;
using Moq.EntityFrameworkCore;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Instrument.Application.Commands.SendActiveMethods;
using Pharos.Server.Instrument.Application.Commands.UpdateInstrumentMethod;
using Pharos.Server.Instrument.Application.Interfaces;
using Pharos.Server.Instrument.Domain;
using Pharos.Server.Instrument.Infrastructure.Grpc;

namespace Pharos.Server.Instrument.Application.Commands;

public class UpdateInstrumentMethodCommandHandlerTests
{
    private readonly Mock<IInstrumentDbContext> _dbContextMock;
    private readonly Mock<IInstrumentManager> _instrumentManagerMock;
    private readonly Mock<ISender> _senderMock;
    private readonly Mock<ICommunicationSessionManager> _sessionManagerMock;

    private readonly UpdateInstrumentMethodCommandHandler _sut;

    public UpdateInstrumentMethodCommandHandlerTests()
    {
        _dbContextMock = new Mock<IInstrumentDbContext>();
        _instrumentManagerMock = new Mock<IInstrumentManager>();
        var publisherMock = new Mock<IPublisher>();
        _senderMock = new Mock<ISender>();
        _sessionManagerMock = new Mock<ICommunicationSessionManager>();

        _sut = new UpdateInstrumentMethodCommandHandler(_dbContextMock.Object, _senderMock.Object, publisherMock.Object, _instrumentManagerMock.Object, new Mock<ICurrentInstrumentServerHub>().Object, _sessionManagerMock.Object);
    }

    [Fact]
    public async Task UpdateInstrumentMethod_Should_SaveToDatabaseAndTrySendUpdatedMethodsToInstrument()
    {
        var instrument = new Domain.Instrument
        {
            Id = 1
        };

        var instrumentMethods = new List<InstrumentMethod>
                                {
                                    new()
                                    {
                                        Id = 1,
                                        Instrument = instrument,
                                        IsActive = true
                                    }
                                };
        _dbContextMock.Setup(x => x.InstrumentMethods).ReturnsDbSet(instrumentMethods);

        var existingInstrument = new Domain.Instrument
        {
            Id = 1,
            SerialNumber = "123456",
            State = InstrumentState.Standby
        };
        _instrumentManagerMock.Setup(x => x.GetInstrument(instrument.Id)).Returns(existingInstrument);

        var communicationSession = new CommunicationSession
        {
            SessionId = Guid.NewGuid(),
            InstrumentNumber = existingInstrument.SerialNumber,
            Connection = new InstrumentConnection(IPAddress.Loopback, 0, Guid.NewGuid(), existingInstrument.SerialNumber, null!)
        };
        communicationSession.SetSynchronizationFlag(CommunicationSynchronizationState.Synchronized);
        _sessionManagerMock.Setup(x => x.GetSession(existingInstrument.SerialNumber)).Returns(communicationSession);

        var request = new UpdateInstrumentMethodCommand(1, 1, false, false, InstrumentMethodWhenToRun.Directly);
        await _sut.Handle(request, TestContext.Current.CancellationToken);

        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _senderMock.Verify(x => x.Send(It.IsAny<SendActiveMethodsCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateInstrumentMethod_When_UpdatingToTheSameValue_Should_NotDoAnything()
    {
        var instrument = new Domain.Instrument
        {
            Id = 1
        };

        var instrumentMethods = new List<InstrumentMethod>
                                {
                                    new()
                                    {
                                        Id = 1,
                                        Instrument = instrument,
                                        IsActive = true,
                                        Calibrator = true,
                                        ForceCalibrator = true,
                                        WhenToRun = InstrumentMethodWhenToRun.Directly
                                    }
                                };
        _dbContextMock.Setup(x => x.InstrumentMethods).ReturnsDbSet(instrumentMethods);

        var existingInstrument = new Domain.Instrument
        {
            Id = 1,

            SerialNumber = "123456",
            State = InstrumentState.Standby
        };
        _instrumentManagerMock.Setup(x => x.GetInstrument(instrument.Id)).Returns(existingInstrument);

        //trying to set value to same values as in the database (Calibrator is not included as it's set by instrument)
        var request = new UpdateInstrumentMethodCommand(1, 1, true, true, InstrumentMethodWhenToRun.Directly);
        await _sut.Handle(request, TestContext.Current.CancellationToken);

        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _senderMock.Verify(x => x.Send(It.IsAny<SendActiveMethodsCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}