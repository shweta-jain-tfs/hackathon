using Microsoft.Extensions.Options;
using Pharos.Contracts.Security;
using Pharos.Server.Application.Sample;
using Pharos.Server.Application.Security;
using Pharos.Server.Application.Settings.Request;
using Pharos.Server.Application.Settings.Sample;
using Pharos.Server.Domain.Entities;
using Pharos.Server.Request.Application.Queries.GetRequests;

namespace Pharos.Server.Request.Application.Queries;

public class GetRequestsQueryHandlerTests
{
    private readonly Mock<IRequestDbContext> _dbContextMock = new();
    private readonly Mock<IOptionsSnapshot<RequestSettings>> _requestSettingsMock = new();
    private readonly Mock<IAuthorizationService> _authorizationServiceMock = new();
    private readonly Mock<ISampleIdFormatter> _formatterMock = new();
    private readonly Mock<ISender> _senderMock = new();
    private readonly Mock<IInstrumentManager> _instrumentServiceMock = new();
    private readonly Mock<IOptionsSnapshot<TubeTypesSettings>> _tubeSettingsMock = new();

    public GetRequestsQueryHandlerTests()
    {
        _authorizationServiceMock
            .Setup(x => x.AuthorizeCurrentUserAsync(Permissions.ViewPersonalAndHealthData))
            .ReturnsAsync(true);

        _requestSettingsMock.Setup(x => x.Value).Returns(new RequestSettings
        {
            IsPatientInformationEnabled = true,
            IsRequestorInformationEnabled = true
        });

        _tubeSettingsMock.Setup(x => x.Value).Returns(new TubeTypesSettings
        {
            TubeTypeList = []
        });

        _senderMock
            .Setup(x => x.Send(It.IsAny<GetRequestListSampleTubesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _formatterMock
            .Setup(x => x.Format(It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
            .Returns((string sampleId, DateTimeOffset _) => sampleId);
    }

    private GetRequestsQueryHandler CreateHandler() => new(
        _dbContextMock.Object,
        _requestSettingsMock.Object,
        _authorizationServiceMock.Object,
        _formatterMock.Object,
        _senderMock.Object,
        _instrumentServiceMock.Object);

    private static RequestSummaryDto CreateItem() => new()
    {
        Id = 1,
        RequestUid = new RequestUid
        {
            SampleId = "S001",
            RequestCreatedDate = DateTimeOffset.UnixEpoch
        },
        SampleId = "S001"
    };

    private Task Enrich(RequestSummaryDto item) =>
        CreateHandler().EnrichWithInstrumentAndTubeInfo([item], CancellationToken.None);

    private void SetupCurrentTubes(RequestSummaryDto item, params CurrentSampleTubeDto[] tubes)
    {
        _senderMock
            .Setup(x => x.Send(
                It.Is<GetRequestListSampleTubesQuery>(query =>
                    query.RequestUids.Any(uid =>
                        uid.SampleId == item.RequestUid.SampleId &&
                        uid.RequestCreatedDate == item.RequestUid.RequestCreatedDate)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([new CurrentSampleTubesDto(item.RequestUid, tubes.ToList())]);
    }

    [Fact]
    public async Task AttachExtraInformation_Should_Set_InstrumentName_When_Tube_Is_Loaded()
    {
        var item = CreateItem();

        SetupCurrentTubes(item,
            new CurrentSampleTubeDto("I1", "TRAY01", 1, DateTimeOffset.UtcNow));

        _instrumentServiceMock
            .Setup(x => x.FindInstrument("I1"))
            .Returns(Mock.Of<IInstrument>(instrument =>
                instrument.SerialNumber == "I1" &&
                instrument.InstrumentType == InstrumentType.Small &&
                instrument.DisplayName == "Billy"));

        await Enrich(item);

        item.InstrumentName.Should().Be("Billy");
    }

    [Fact]
    public async Task AttachExtraInformation_Should_Set_InstrumentName_To_Null_When_No_Tubes_Loaded()
    {
        var item = CreateItem();

        SetupCurrentTubes(item);

        await Enrich(item);

        item.InstrumentName.Should().BeNull();
    }

    [Fact]
    public async Task AttachExtraInformation_Should_Join_Multiple_InstrumentNames_With_Comma()
    {
        var item = CreateItem();

        SetupCurrentTubes(item,
            new CurrentSampleTubeDto("I1", "TRAY01", 1, DateTimeOffset.UtcNow),
            new CurrentSampleTubeDto("I2", "TRAY02", 2, DateTimeOffset.UtcNow));

        _instrumentServiceMock.Setup(x => x.FindInstrument("I1"))
            .Returns(Mock.Of<IInstrument>(instrument =>
                instrument.SerialNumber == "I1" &&
                instrument.InstrumentType == InstrumentType.Small &&
                instrument.DisplayName == "Billy"));
        _instrumentServiceMock.Setup(x => x.FindInstrument("I2"))
            .Returns(Mock.Of<IInstrument>(instrument =>
                instrument.SerialNumber == "I2" &&
                instrument.InstrumentType == InstrumentType.Small &&
                instrument.DisplayName == "Kallax"));

        await Enrich(item);

        item.InstrumentName.Should().Be("Billy, Kallax");
    }

    [Fact]
    public async Task AttachExtraInformation_Should_Deduplicate_Instruments_When_Multiple_Tubes_In_Same_Instrument()
    {
        var item = CreateItem();

        SetupCurrentTubes(item,
            new CurrentSampleTubeDto("I1", "TRAY01", 1, DateTimeOffset.UtcNow),
            new CurrentSampleTubeDto("I1", "TRAY01", 2, DateTimeOffset.UtcNow));

        _instrumentServiceMock.Setup(x => x.FindInstrument("I1"))
            .Returns(Mock.Of<IInstrument>(instrument =>
                instrument.SerialNumber == "I1" &&
                instrument.InstrumentType == InstrumentType.Small &&
                instrument.DisplayName == "Billy"));

        await Enrich(item);

        item.InstrumentName.Should().Be("Billy");
    }

    [Fact]
    public async Task AttachExtraInformation_Should_Fall_Back_To_InstrumentNumber_When_DisplayName_Not_Found()
    {
        var item = CreateItem();

        SetupCurrentTubes(item,
            new CurrentSampleTubeDto("I_UNKNOWN", "TRAY01", 1, DateTimeOffset.UtcNow));

        _instrumentServiceMock.Setup(x => x.FindInstrument("I_UNKNOWN")).Returns((IInstrument?)null);

        await Enrich(item);

        item.InstrumentName.Should().Be("I_UNKNOWN");
    }

    [Fact]
    public async Task AttachExtraInformation_Should_Resolve_TubeType_Name_From_Settings()
    {
        var item = CreateItem();

        _tubeSettingsMock.Setup(x => x.Value).Returns(new TubeTypesSettings
        {
            TubeTypeList = [new TubeType { TubeDefinitionId = 42, Name = "Red Cap" }]
        });

        await Enrich(item);
    }

    [Fact]
    public async Task AttachExtraInformation_Should_Set_TubeType_Empty_When_Id_Not_In_Settings()
    {
        var item = CreateItem();

        _tubeSettingsMock.Setup(x => x.Value).Returns(new TubeTypesSettings
        {
            TubeTypeList = [new TubeType { TubeDefinitionId = 1, Name = "Blue Cap" }]
        });

        await Enrich(item);
    }

    [Trait("ProductRequirement", "PR_2210")]
    [Trait("ModuleRequirement", "MR_5732")]
    [Fact]
    public async Task AttachExtraInformation_Should_Set_LastTray_From_Tube()
    {
        var item = CreateItem();
        var newer = DateTimeOffset.UtcNow;

        SetupCurrentTubes(item,
            new CurrentSampleTubeDto("I2", "TRAY01", 3, newer, "A3"));

        await Enrich(item);

        item.LastTray.Should().Be("TRAY01 (A3)");
        item.LastTrayId.Should().Be("TRAY01");
        item.LastTrayTubePosition.Should().Be(3);
        item.HasMultipleTubes.Should().BeFalse();
    }

    [Trait("ProductRequirement", "PR_2210")]
    [Trait("ModuleRequirement", "MR_5732")]
    [Fact]
    public async Task AttachExtraInformation_Should_Set_HasMultipleTubes_When_Sample_Is_In_Multiple_Trays_In_Same_Instrument()
    {
        var item = CreateItem();

        SetupCurrentTubes(item,
            new CurrentSampleTubeDto("I1", "TRAY01", 1, DateTimeOffset.UtcNow.AddMinutes(-1), "A1"),
            new CurrentSampleTubeDto("I1", "TRAY02", 3, DateTimeOffset.UtcNow, "A3"));

        await Enrich(item);

        item.LastTray.Should().BeNull();
        item.LastTrayId.Should().BeNull();
        item.LastTrayTubePosition.Should().BeNull();
        item.HasMultipleTubes.Should().BeTrue();
    }

    [Trait("ProductRequirement", "PR_2210")]
    [Trait("ModuleRequirement", "MR_5732")]
    [Fact]
    public async Task AttachExtraInformation_Should_Set_HasMultipleTubes_When_Sample_Has_Multiple_Tubes_In_Same_Tray()
    {
        var item = CreateItem();

        SetupCurrentTubes(item,
            new CurrentSampleTubeDto("I1", "TRAY01", 1, DateTimeOffset.UtcNow.AddMinutes(-1), "A1"),
            new CurrentSampleTubeDto("I1", "TRAY01", 3, DateTimeOffset.UtcNow, "A3"));

        await Enrich(item);

        item.LastTray.Should().BeNull();
        item.LastTrayId.Should().BeNull();
        item.LastTrayTubePosition.Should().BeNull();
        item.HasMultipleTubes.Should().BeTrue();
    }

    [Trait("ProductRequirement", "PR_2210")]
    [Trait("ModuleRequirement", "MR_5732")]
    [Fact]
    public async Task AttachExtraInformation_Should_Set_HasMultipleTubes_When_Sample_Has_Tray_And_Trayless_Tubes()
    {
        var item = CreateItem();

        SetupCurrentTubes(item,
            new CurrentSampleTubeDto("I1", "TRAY01", 1, DateTimeOffset.UtcNow.AddMinutes(-1), "A1"),
            new CurrentSampleTubeDto("I1", null, null, DateTimeOffset.UtcNow));

        await Enrich(item);

        item.LastTray.Should().BeNull();
        item.LastTrayId.Should().BeNull();
        item.LastTrayTubePosition.Should().BeNull();
        item.HasMultipleTubes.Should().BeTrue();
    }

    [Trait("ProductRequirement", "PR_2210")]
    [Trait("ModuleRequirement", "MR_5732")]
    [Fact]
    public async Task AttachExtraInformation_Should_Set_HasMultipleTubes_When_Sample_Has_Multiple_Trayless_Tubes()
    {
        var item = CreateItem();

        SetupCurrentTubes(item,
            new CurrentSampleTubeDto("I1", null, null, DateTimeOffset.UtcNow.AddMinutes(-1)),
            new CurrentSampleTubeDto("I1", null, null, DateTimeOffset.UtcNow));

        await Enrich(item);

        item.LastTray.Should().BeNull();
        item.LastTrayId.Should().BeNull();
        item.LastTrayTubePosition.Should().BeNull();
        item.HasMultipleTubes.Should().BeTrue();
    }
}
