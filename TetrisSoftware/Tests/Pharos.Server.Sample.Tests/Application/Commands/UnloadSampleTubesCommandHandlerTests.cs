using MediatR;
using Pharos.Contracts.Notifications;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Sample;
using Pharos.Server.Domain.Entities;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Sample.Application.Commands;
using Pharos.Server.Sample.Application.Services;
using Pharos.Server.Sample.Domain;

namespace Pharos.Server.Sample.Tests.Application.Commands;

public class UnloadSampleTubesCommandHandlerTests
{
    private readonly InstrumentSampleStorage _sampleStorage;
    private readonly Mock<ISender> _senderMock;
    private readonly Mock<ISampleTubeHub> _sampleTubeHubMock;
    private readonly UnloadSampleTubesCommandHandler _sut;

    public UnloadSampleTubesCommandHandlerTests()
    {
        _sampleStorage = new InstrumentSampleStorage();
        _senderMock = new Mock<ISender>();
        _sampleTubeHubMock = new Mock<ISampleTubeHub>();
        _sut = new UnloadSampleTubesCommandHandler(_senderMock.Object, _sampleStorage, _sampleTubeHubMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldSendMoveSampleQcCommandForEachTube()
    {
        var sampleId = "sample123";
        var tube1 = new SampleTube { TubeId = "tube1", InstrumentNumber = "inst1", SampleId = sampleId, TubePosition = 1, Area = SampleArea.InternalStorage };
        var tube2 = new SampleTube { TubeId = "tube2", InstrumentNumber = "inst2", SampleId = sampleId, TubePosition = 1, Area = SampleArea.InternalStorage };

        // Add tubes to storage
        _sampleStorage.AddOrUpdateTube(tube1, t => t);
        _sampleStorage.AddOrUpdateTube(tube2, t => t);

        _senderMock.Setup(x => x.Send(It.IsAny<SendMoveSampleQcCommand>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var requestId = 42;
        var command = new UnloadSampleTubesCommand(requestId, new RequestUid { SampleId = sampleId });

        await _sut.Handle(command, TestContext.Current.CancellationToken);

        _senderMock.Verify(x => x.Send(It.IsAny<SendMoveSampleQcCommand>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _sampleTubeHubMock.Verify(x => x.SampleTubeUpdated(requestId), Times.Once);

        // Verify tubes in storage
        var storageTube1 = _sampleStorage.GetTube("tube1");
        var storageTube2 = _sampleStorage.GetTube("tube2");
        Assert.True(storageTube1?.UnloadRequested);
        Assert.True(storageTube2?.UnloadRequested);
    }

    [Fact]
    public async Task Handle_ShouldNotSendMoveSampleQcCommand_WhenTubeIsEmpty()
    {
        var sampleId = "sample123";
        var tube1 = new SampleTube { TubeId = "tube1", InstrumentNumber = "inst1", SampleId = sampleId, TubePosition = 1, Area = SampleArea.Shuttle1 };
        var tube2 = new SampleTube { TubeId = null, InstrumentNumber = "inst2", SampleId = sampleId, TubePosition = 1, Area = SampleArea.Shuttle2 };

        // Add tubes to storage
        _sampleStorage.AddOrUpdateTube(tube1, t => t);
        _sampleStorage.AddOrUpdateTube(tube2, t => t);

        _senderMock.Setup(x => x.Send(It.IsAny<SendMoveSampleQcCommand>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var requestId = 43;
        var command = new UnloadSampleTubesCommand(requestId, new RequestUid { SampleId = sampleId });

        await _sut.Handle(command, TestContext.Current.CancellationToken);

        _senderMock.Verify(x => x.Send(It.IsAny<SendMoveSampleQcCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        _sampleTubeHubMock.Verify(x => x.SampleTubeUpdated(requestId), Times.Once);

        // Verify tubes in storage
        var storageTube1 = _sampleStorage.GetTube("tube1");
        var storageTube2 = _sampleStorage.TubesAsQueryable("inst2").Single();
        Assert.True(storageTube1?.UnloadRequested);
        Assert.False(storageTube2?.UnloadRequested);
    }

    [Fact]
    public async Task Handle_ShouldSendMoveSampleQcCommand_WhenTubeHasError()
    {
        var sampleId = "sample123";
        var tube1 = new SampleTube { TubeId = "tube1", InstrumentNumber = "inst1", SampleId = sampleId, TubePosition = 1, Area = SampleArea.Shuttle1, InstrumentError = SampleTubeError.TubeGripError };
        var tube2 = new SampleTube { TubeId = "tube2", InstrumentNumber = "inst2", SampleId = sampleId, TubePosition = 1, Area = SampleArea.Shuttle2 };

        // Add tubes to storage
        _sampleStorage.AddOrUpdateTube(tube1, t => t);
        _sampleStorage.AddOrUpdateTube(tube2, t => t);

        _senderMock.Setup(x => x.Send(It.IsAny<SendMoveSampleQcCommand>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var requestId = 44;
        var command = new UnloadSampleTubesCommand(requestId, new RequestUid { SampleId = sampleId });

        await _sut.Handle(command, TestContext.Current.CancellationToken);

        _senderMock.Verify(x => x.Send(It.IsAny<SendMoveSampleQcCommand>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _sampleTubeHubMock.Verify(x => x.SampleTubeUpdated(requestId), Times.Once);

        // Verify tubes in storage
        var storageTube1 = _sampleStorage.GetTube("tube1");
        var storageTube2 = _sampleStorage.GetTube("tube2");
        Assert.True(storageTube1?.UnloadRequested);
        Assert.True(storageTube2?.UnloadRequested);
    }
}