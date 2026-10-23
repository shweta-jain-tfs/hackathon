using Pharos.Server.Application.Sample;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Sample.Application.Queries.GetSampleTubes;
using Pharos.Server.Sample.Application.Services;
using Pharos.Server.Sample.Domain;

namespace Pharos.Server.Sample.Tests.Application.Queries;

public class GetProcessableSampleTubesQueryHandlerTests
{
    private readonly InstrumentSampleStorage _sampleStorage = new();
    private readonly string _instrumentNumber = "S001";
    private readonly string _sampleId1 = "SAMPLE001";
    private readonly string _sampleId2 = "SAMPLE002";

    [Fact]
    public async Task Handle_ShouldReturnOnlyMatchingSampleTubesWithoutErrors()
    {
        // Arrange
        AddStorageTube("TUBE01", _sampleId1, unloadRequested: false, tubePosition: 1, sampleVolume: 12.5);
        AddStorageTube("TUBE02", _sampleId2, unloadRequested: false, tubePosition: 2, sampleVolume: 10.0);
        AddStorageTube("TUBE03", _sampleId1, unloadRequested: false, tubePosition: 3, sampleVolume: 8.0, instrumentError: SampleTubeError.ClotDetected);

        var sut = new GetProcessableSampleTubesQueryHandler(_sampleStorage);

        // Act
        var result = await sut.Handle(new GetProcessableSampleTubesQuery(_sampleId1), TestContext.Current.CancellationToken);

        // Assert
        var tube = result.Should().ContainSingle().Subject;
        tube.TubeId.Should().Be("TUBE01");
        tube.SampleVolume.Should().Be(12.5);
        tube.InstrumentNumber.Should().Be(_instrumentNumber);
    }

    [Fact]
    public async Task Handle_ShouldExcludeTubesWithUnloadRequested()
    {
        // Arrange
        AddStorageTube("TUBE01", _sampleId1, unloadRequested: true, tubePosition: 1, sampleVolume: 12.5);
        AddStorageTube("TUBE02", _sampleId1, unloadRequested: true, tubePosition: 2, sampleVolume: 10.0);

        var sut = new GetProcessableSampleTubesQueryHandler(_sampleStorage);

        // Act
        var result = await sut.Handle(new GetProcessableSampleTubesQuery(_sampleId1), TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEmpty();
    }

    private void AddStorageTube(string tubeId,
        string sampleId,
        bool unloadRequested,
        int tubePosition,
        double sampleVolume,
        SampleTubeError instrumentError = SampleTubeError.None)
    {
        var tube = new SampleTube
        {
            TubeId = tubeId,
            SampleId = sampleId,
            TubePosition = tubePosition,
            InstrumentError = instrumentError,
            Status = SampleTubeStatus.InProgress,
            UnloadRequested = unloadRequested,
            SampleVolume = sampleVolume,
            Area = SampleArea.InternalStorage,
            InstrumentNumber = _instrumentNumber
        };

        _sampleStorage.AddOrUpdateTube(tube, _ => tube);
    }
}
