using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Request;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Localization;
using Pharos.Server.Sample.Application.Interfaces;
using SampleTray = Pharos.Server.Sample.Domain.SampleTray;
using SampleTube = Pharos.Server.Sample.Domain.SampleTube;

namespace Pharos.Server.Sample.Application.Queries.GetSampleTrayDetails;

internal sealed record GetSampleTrayDetailsQuery(string TrayId) : IRequest<SampleTrayDetailsDto>;

internal sealed class GetSampleTrayDetailsQueryHandler(
    ISampleDbContext dbContext,
    IInstrumentSampleStorage sampleStorage,
    IInstrumentManager instrumentManager,
    ISampleIdFormatter sampleIdFormatter) : IRequestHandler<GetSampleTrayDetailsQuery, SampleTrayDetailsDto>
{
    public async Task<SampleTrayDetailsDto> Handle(GetSampleTrayDetailsQuery request, CancellationToken cancellationToken)
    {
        var trayId = request.TrayId;
        var tray = sampleStorage.GetTray(trayId);
        var isLoaded = true;

        if (tray == null)
        {
            // the tray is not in the storage, we need to get it from the database
            isLoaded = false;
            tray = await dbContext.SampleTrays
                .Where(x => x.Id == trayId)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (tray == null)
            {
                throw new NotFoundException();
            }
        }

        var trayDto = CreateSampleTrayDto(tray);

        if (isLoaded)
        {
            var instrumentNumber = tray.InstrumentNumber!;

            // the tray is in the instrument
            var drawerLockState = instrumentManager.GetInstrument(instrumentNumber).DrawersLockState[tray.Drawer!.Value].LockState;
            var isScanOngoing = sampleStorage.GetDrawer(instrumentNumber, tray.Drawer!.Value).ScanningOngoing;

            if (drawerLockState != LockState.Open && !isScanOngoing)
            {
                // the drawer is closed, so we need to get the tubes from the storage
                trayDto.IsTrayEditable = false;
                var area = tray.Drawer == DrawerType.Sample1 ? SampleArea.SampleDrawer1 : SampleArea.SampleDrawer2;
                var tubes = sampleStorage.TubesAsQueryable(instrumentNumber)
                    .Where(x => x.Area == area && x.TrayPosition == tray.TrayPosition)
                    .ToList();

                foreach (var tube in tubes)
                {
                    var tubeDto = trayDto.SamplePositions[tube.TubePosition!.Value - 1];
                    PopulateSamplePosition(tubeDto, tube, sampleIdFormatter);
                }

                return trayDto;
            }
        }

        trayDto.IsTrayEditable = true;

        var dbTubes = await dbContext.SampleTubes
            .Where(t => t.TrayId == tray.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var tube in dbTubes)
        {
            var tubeDto = trayDto.SamplePositions[tube.TubePosition!.Value - 1];
            PopulateSamplePosition(tubeDto, tube, sampleIdFormatter);
        }

        return trayDto;
    }

    private SampleTrayDetailsDto CreateSampleTrayDto(SampleTray tray)
    {
        var trayDto = new SampleTrayDetailsDto
        {
            Id = tray.Id,
            TrayPosition = tray.TrayPosition,
            TrayLocation = GetTrayLocation(tray.InstrumentNumber, tray.Drawer, tray.TrayPosition),
            SamplePositions = new List<SampleTrayPositionDto>()
        };

        FillSampleTray(trayDto);
        return trayDto;
    }

    private static void PopulateSamplePosition(SampleTrayPositionDto tubeDto, SampleTube tube, ISampleIdFormatter sampleIdFormatter)
    {
        tubeDto.Position = tube.TubePosition;
        tubeDto.SampleId = tube.SampleId;
        tubeDto.SampleIdDisplay = sampleIdFormatter.Format(tube.SampleId ?? string.Empty, tube.RequestCreatedDate);
        tubeDto.TubeBarcode = tube.TubeBarcode;
        tubeDto.IsHighPrioritySet = tube.HighPriority;
        tubeDto.SampleTubeStatus = tube.Status;
        tubeDto.IsUnloadRequested = tube.UnloadRequested;
        tubeDto.SamplePositionSource = tube.IdentitySource;
        tubeDto.Error = tube.Error;
        tubeDto.SampleType = tube.SampleType;
    }

    private static void FillSampleTray(SampleTrayDetailsDto sampleTray)
    {
        for (var i = 1; i <= Constants.MaxSampleTubesInTray; i++)
        {
            sampleTray.SamplePositions?.Add(new SampleTrayPositionDto
            {
                Position = i,
                SampleTubeStatus = SampleTubeStatus.None
            });
        }
    }

    private string GetTrayLocation(string? instrumentNumber, DrawerType? drawerType, int? position)
    {
        if (string.IsNullOrEmpty(instrumentNumber))
        {
            return Translations.TrayLocation_NotLoaded;
        }

        var instrumentName = instrumentManager.FindInstrument(instrumentNumber)?.DisplayName ?? instrumentNumber;
        var drawerName = drawerType is DrawerType.Sample1 ? Translations.Drawer_LeftSampleDrawer : Translations.Drawer_RightSampleDrawer;
        var trayPosition = $"{Translations.TrayPosition} {position}";

        var trayLocation = $"{instrumentName}, {drawerName}, {trayPosition}";

        return trayLocation;
    }
}
