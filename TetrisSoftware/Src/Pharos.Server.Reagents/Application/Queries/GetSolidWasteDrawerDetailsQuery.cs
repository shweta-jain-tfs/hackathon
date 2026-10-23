using Google.Protobuf.WellKnownTypes;
using MediatR;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Domain;
using Pharos.Server.Infrastructure;
using Pharos.Server.Reagents.Application.Interfaces;
using Tetris.Protocol.Pharos.Storage;

namespace Pharos.Server.Reagents.Application.Queries;

/// <summary>
/// Query to get solid waste drawer details
/// </summary>
public class GetSolidWasteDrawerDetailsQuery : IRequest<SolidWasteDrawerDetailsDto>
{
    public string InstrumentNumber { get; set; }

    public GetSolidWasteDrawerDetailsQuery(string instrumentNumber)
    {
        InstrumentNumber = instrumentNumber;
    }
}

/// <summary>
/// Handler for retrieving waste drawer details
/// Gets data from protocol storage when available
/// </summary>
internal class GetSolidWasteDrawerDetailsQueryHandler(
    IInstrumentReagentStorage instrumentStorage,
    IInstrumentManager instrumentManager,
    IDateTime dateTime) : IRequestHandler<GetSolidWasteDrawerDetailsQuery, SolidWasteDrawerDetailsDto>
{
    public Task<SolidWasteDrawerDetailsDto> Handle(GetSolidWasteDrawerDetailsQuery request, CancellationToken cancellationToken)
    {
        // Get actual lock state from instrument service
        var drawerState = instrumentManager.GetInstrument(request.InstrumentNumber).DrawersLockState[DrawerType.SolidWaste];

        // Get solid waste data from cached storage
        var solidWasteContainers = instrumentStorage.GetSolidWaste(request.InstrumentNumber);

        // Create predefined container list (Biohazard position 1, Recyclable position 2)
        var containers = FillWasteContainers();

        // Fill container details from protocol data
        PopulateSolidWasteData(containers, solidWasteContainers, drawerState.LockState, dateTime.UtcNow.UtcDateTime);

        var result = new SolidWasteDrawerDetailsDto
        {
            LockState = drawerState.LockState,
            IsUnlockInProgress = drawerState.IsUnlockInProgress,
            Containers = containers
        };

        return Task.FromResult(result);
    }

    /// <summary>
    /// Creates predefined container list: Biohazard (1), Recyclable (2)
    /// </summary>
    private static List<WasteContainerDto> FillWasteContainers()
    {
        return new List<WasteContainerDto>
        {
            new()
            {
                Position = 1,
                Type = WasteType.Biohazard,
                Percentage = Constants.DefaultWasteLevel,
                Status = ReagentPositionStatus.Empty
            },
            new()
            {
                Position = 2,
                Type = WasteType.Recyclable,
                Percentage = Constants.DefaultWasteLevel,
                Status = ReagentPositionStatus.Empty
            }
        };
    }

    /// <summary>
    /// Fills solid waste container data from protocol into predefined containers
    /// ISW uses 0-based positions (0=Biohazard, 1=Recyclable)
    /// UI uses 1-based positions (1=Biohazard, 2=Recyclable)
    /// </summary>
    private static void PopulateSolidWasteData(List<WasteContainerDto> containers, IReadOnlyList<WasteContainer> solidWasteContainers, LockState lockState, DateTime utcNow)
    {
        foreach (var container in containers)
        {
            var solidWaste = solidWasteContainers.FirstOrDefault(c => c.Position == container.Position);
            if (solidWaste != null)
            {
                container.IsPresent = true;
                container.Percentage = solidWaste.PercentageFull;
                container.Status = MapSolidWasteStatus(solidWaste);
                container.EmptyIndicationType = GetEmptyIndicationForWaste(solidWaste, utcNow);
            }
            else
            {
                container.Status = lockState == LockState.Open ? ReagentPositionStatus.Empty : ReagentPositionStatus.MissingBottle;
            }
        }
    }

    /// <summary>
    /// Maps solid waste container protocol data to ReagentPositionStatus
    /// </summary>
    private static ReagentPositionStatus MapSolidWasteStatus(WasteContainer container)
    {
        // Handle error conditions first
        return container.Error switch
        {
            EnumWasteContainerError.WasteContainerVolumeReadError => ReagentPositionStatus.UndetectedVolume,
            _ => ReagentPositionStatus.Available
        };
    }

    /// <summary>
    /// Determines empty indication type for waste container based on fill level and dispensing status
    /// </summary>
    /// <param name="container">Waste container protocol data</param>
    /// <param name="utcNow">Current UTC time.</param>
    /// <returns>
    /// Alarm: CapDispenseStopped = true (Red icon)
    /// Warning: TimeCapDispenseStop within 1 hour (Yellow icon)
    /// Pending: PercentageFull > 50% (Blue icon)
    /// None: Normal state
    /// </returns>
    private static FillOrEmptyIndicationType GetEmptyIndicationForWaste(WasteContainer container, DateTime utcNow)
    {
        if (container.CapDispenseStopped)
        {
            return FillOrEmptyIndicationType.Alarm;
        }
        else if (IsWithinCapDispenseWarningWindow(container.TimeCapDispenseStop, utcNow))
        {
            return FillOrEmptyIndicationType.Warning;
        }
        else if (container.PercentageFull > 50)
        {
            return FillOrEmptyIndicationType.Pending;
        }

        return FillOrEmptyIndicationType.None;
    }

    private static bool IsWithinCapDispenseWarningWindow(Timestamp? timeCapDispenseStop, DateTime utcNow)
    {
        if (timeCapDispenseStop == null)
        {
            return false;
        }

        var stopTime = timeCapDispenseStop.ToDateTime();
        return stopTime >= utcNow && stopTime < utcNow.AddHours(Constants.DefaultCapDispenseWarningHours);
    }
}
