using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Request;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Sample.Application.Interfaces;
using Pharos.Server.Sample.Domain;
using SampleTray = Pharos.Server.Sample.Domain.SampleTray;
using SampleTube = Pharos.Server.Sample.Domain.SampleTube;

namespace Pharos.Server.Sample.Application.Queries.GetSampleDrawerDetails;

internal record GetSampleDrawerDetailsQuery(string InstrumentNumber, DrawerType Drawer) : IRequest<SampleDrawerDto>;

internal class GetSampleDrawerDetailsQueryHandler(
    IInstrumentSampleStorage sampleStorage,
    IInstrumentManager instrumentManager,
    ISampleDbContext dbContext,
    ISampleIdFormatter sampleIdFormatter) : IRequestHandler<GetSampleDrawerDetailsQuery, SampleDrawerDto>
{
    public async Task<SampleDrawerDto> Handle(GetSampleDrawerDetailsQuery request, CancellationToken cancellationToken)
    {
        var instrumentNumber = request.InstrumentNumber;
        var sampleDrawerDto = FillSampleDrawer();

        var state = instrumentManager.GetInstrument(instrumentNumber).DrawersLockState[request.Drawer];
        sampleDrawerDto.Drawer = request.Drawer;
        sampleDrawerDto.LockState = state.LockState;
        sampleDrawerDto.IsUnlockInProgress = state.IsUnlockInProgress;
        sampleDrawerDto.IsScanOngoing = sampleStorage.GetDrawer(instrumentNumber, request.Drawer).ScanningOngoing;

        if (state.LockState == LockState.Open || sampleDrawerDto.IsScanOngoing)
        {
            // populate the drawer from the database
            var trays = await dbContext.SampleTrays
                .Where(t => t.InstrumentNumber == instrumentNumber && t.Drawer == request.Drawer)
                .Include(t => t.SampleTubes)
                .ToListAsync(cancellationToken);

            foreach (var tray in trays)
            {
                var trayDto = sampleDrawerDto.SampleTrays[tray.TrayPosition!.Value - 1];
                PopulateSampleTray(trayDto, tray);
                foreach (var tube in tray.SampleTubes)
                {
                    var tubeDto = trayDto.SamplePositions[tube.TubePosition!.Value - 1];
                    PopulateSamplePosition(tubeDto, tube, sampleIdFormatter);
                }
            }

            // add trays with errors to from the storage, because they are not persisted in the database
            PopulateTraysFromStorage(instrumentNumber, request.Drawer, sampleDrawerDto, true);
        }
        else
        {
            // populate the drawer from the in-memory storage
            PopulateTraysFromStorage(instrumentNumber, request.Drawer, sampleDrawerDto);
        }

        return sampleDrawerDto;
    }

    private void PopulateTraysFromStorage(string instrumentNumber, DrawerType drawerType, SampleDrawerDto sampleDrawerDto, bool traysWithErrorsOnly = false)
    {
        var tubesQuery = sampleStorage.TubesAsQueryable(instrumentNumber);
        var trays = sampleStorage.GetTrays(instrumentNumber, drawerType);
        if (traysWithErrorsOnly)
        {
            trays = trays.Where(x => x.Error != SampleTrayError.None).ToList();
        }
        foreach (var tray in trays)
        {
            var trayDto = sampleDrawerDto.SampleTrays[tray.TrayPosition!.Value - 1];
            PopulateSampleTray(trayDto, tray);
            var area = tray.Drawer == DrawerType.Sample1 ? SampleArea.SampleDrawer1 : SampleArea.SampleDrawer2;
            var tubes = tubesQuery.Where(x => x.Area == area && x.TrayPosition == tray.TrayPosition).ToList();
            foreach (var tube in tubes)
            {
                var tubeDto = trayDto.SamplePositions[tube.TubePosition!.Value - 1];
                PopulateSamplePosition(tubeDto, tube, sampleIdFormatter);
            }
        }
    }

    private static SampleDrawerDto FillSampleDrawer()
    {
        var sampleTrays = new List<SampleTrayDetailsDto>();

        for (var i = 1; i <= Constants.MaxTraysInSampleDrawer; i++)
        {
            sampleTrays.Add(new SampleTrayDetailsDto
            {
                TrayPosition = i,
                IsPositionEmpty = true
            });
        }

        return new SampleDrawerDto { SampleTrays = sampleTrays };
    }

    private static void PopulateSampleTray(SampleTrayDetailsDto trayDto, SampleTray tray)
    {
        trayDto.Id = tray.Id;
        trayDto.TrayPosition = tray.TrayPosition;
        trayDto.IsPositionEmpty = false;
        trayDto.TrayError = tray.Error;
        FillSampleTray(trayDto);
    }

    private static void FillSampleTray(SampleTrayDetailsDto sampleTray)
    {
        sampleTray.SamplePositions = new List<SampleTrayPositionDto>();

        for (var i = 1; i <= Constants.MaxSampleTubesInTray; i++)
        {
            sampleTray.SamplePositions.Add(new SampleTrayPositionDto
            {
                Position = i,
                SampleTubeStatus = SampleTubeStatus.None
            });
        }
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
}