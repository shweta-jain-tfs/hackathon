using Google.Protobuf.WellKnownTypes;
using MediatR;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Settings.Instrument;
using Pharos.Server.Domain;
using Pharos.Server.Infrastructure;
using Pharos.Server.Reagents.Application.Interfaces;
using Tetris.Protocol.Pharos.Storage;

namespace Pharos.Server.Reagents.Application.Queries;

/// <summary>
/// Query to get liquid drawer details from cached InstrumentStorage data
/// </summary>
public class GetLiquidDrawerDetailsQuery : IRequest<LiquidDrawerDetailsDto>
{
    public string InstrumentNumber { get; set; } = string.Empty;
}

/// <summary>
/// Handler for retrieving liquid drawer details from InstrumentReagentStorage
/// </summary>
internal class GetLiquidDrawerDetailsQueryHandler(
    IInstrumentReagentStorage instrumentStorage,
    IInstrumentManager instrumentManager,
    IOptionsSnapshot<InstrumentSettings> instrumentSettings,
    IDateTime dateTime) : IRequestHandler<GetLiquidDrawerDetailsQuery, LiquidDrawerDetailsDto>
{
    public Task<LiquidDrawerDetailsDto> Handle(GetLiquidDrawerDetailsQuery request, CancellationToken cancellationToken)
    {
        var lockState = instrumentManager.GetInstrument(request.InstrumentNumber).DrawersLockState[DrawerType.RinseWaste].LockState;
        var instrumentSpecific = instrumentSettings.Value.GetInstrumentSettings(request.InstrumentNumber);

        // Get liquid waste and rinse data from cached storage
        var wasteContainers = instrumentStorage.GetLiquidWaste(request.InstrumentNumber);
        var rinseContainers = instrumentStorage.GetRinseContainers(request.InstrumentNumber);

        // Create predefined container list with protocol coordinate ordering: Waste 1,2 then Rinse 1,2
        var containers = FillLiquidContainers(instrumentSpecific);

        // Fill container details from protocol data
        PopulateWasteContainerData(containers, wasteContainers, lockState, dateTime.UtcNow.UtcDateTime);
        PopulateRinseContainerData(containers, rinseContainers, lockState, dateTime.UtcNow.UtcDateTime);

        var result = new LiquidDrawerDetailsDto
        {
            LockState = lockState,
            Containers = containers
        };

        return Task.FromResult(result);
    }

    /// <summary>
    /// Creates predefined container list in protocol coordinate order: Waste 1, Waste 2, Rinse 1, Rinse 2
    /// All containers start as empty (IsEmpty = true)
    /// </summary>
    private static List<LiquidContainer> FillLiquidContainers(InstrumentSpecificSettings instrumentSpecific)
    {
        return new List<LiquidContainer>
        {
            // Waste containers first (position 1, then 2)
            InitializeContainer(1, LiquidContainerType.Waste, instrumentSpecific),
            InitializeContainer(2, LiquidContainerType.Waste, instrumentSpecific),

            // Rinse containers second (position 1, then 2)
            InitializeContainer(1, LiquidContainerType.Rinse, instrumentSpecific),
            InitializeContainer(2, LiquidContainerType.Rinse, instrumentSpecific)
        };
    }

    private static LiquidContainer InitializeContainer(int position, LiquidContainerType type, InstrumentSpecificSettings instrumentSpecific)
    {
        return new()
        {
            Position = position,
            Type = type,
            IsMissing = true,
            Status = ReagentPositionStatus.Empty,
            IsPipedEnabled = IsPipedEnabled(type, instrumentSpecific),
            FillOrEmpty = FillOrEmptyIndicationType.None
        };
    }

    private static bool IsPipedEnabled(LiquidContainerType type, InstrumentSpecificSettings instrumentSpecific)
    {
        return type switch
        {
            LiquidContainerType.Rinse => instrumentSpecific.Rinse.PipedInRinseInstalled && instrumentSpecific.Rinse.PipedInActive,
            LiquidContainerType.Waste => instrumentSpecific.Waste.PipedOutWasteInstalled && instrumentSpecific.Waste.PipedOutActive,
            _ => false
        };
    }

    /// <summary>
    /// Fills waste container data from protocol into predefined containers
    /// </summary>
    private static void PopulateWasteContainerData(List<LiquidContainer> containers, IReadOnlyList<WasteContainer> wasteContainers, LockState lockState, DateTime utcNow)
    {
        containers = containers.Where(c => c.Type == LiquidContainerType.Waste).ToList();

        foreach (var container in containers)
        {
            var wasteContainer = wasteContainers.FirstOrDefault(c => c.Position == container.Position);
            if (wasteContainer != null)
            {
                container.IsMissing = false;
                container.InUse = wasteContainer.InUse;
                container.Percentage = wasteContainer.PercentageFull == -1 ? Constants.DefaultWasteLevel : wasteContainer.PercentageFull;
                container.Status = MapWasteContainerStatus(wasteContainer, container.IsPipedEnabled);
                container.FillOrEmpty = GetEmptyIndicationForWaste(wasteContainer, utcNow);
            }
            else
            {
                container.Status = lockState == LockState.Open ? ReagentPositionStatus.Empty : ReagentPositionStatus.MissingBottle;
            }
        }
    }

    /// <summary>
    /// Fills rinse container data from protocol into predefined containers
    /// </summary>
    private static void PopulateRinseContainerData(List<LiquidContainer> containers, IReadOnlyList<RinseContainer> rinseContainers, LockState lockState, DateTime utcNow)
    {
        containers = containers.Where(c => c.Type == LiquidContainerType.Rinse).ToList();

        foreach (var container in containers)
        {
            var rinseContainer = rinseContainers.FirstOrDefault(r => r.Position == container.Position);
            if (rinseContainer != null)
            {
                container.IsMissing = false;
                container.InUse = rinseContainer.InUse;
                container.Percentage = rinseContainer.PercentageFull == -1 ? Constants.DefaultRinseLevel : rinseContainer.PercentageFull;
                container.Status = MapRinseContainerStatus(rinseContainer, container.IsPipedEnabled);
                container.FillOrEmpty = GetFillIndicationForRinse(rinseContainer, utcNow);
            }
            else
            {
                container.Status = lockState == LockState.Open ? ReagentPositionStatus.Empty : ReagentPositionStatus.MissingBottle;
            }
        }
    }

    /// <summary>
    /// Maps waste container protocol data to ReagentPositionStatus
    /// Implements piped out logic per requirements
    /// </summary>
    private static ReagentPositionStatus MapWasteContainerStatus(WasteContainer container, bool isPipedEnabled)
    {
        // Check error conditions first
        if (container.Error == EnumWasteContainerError.WasteContainerVolumeReadError)
        {
            return ReagentPositionStatus.UndetectedVolume;
        }

        // If not in use and piped out is enabled, show as disabled
        if (isPipedEnabled && !container.InUse)
        {
            return ReagentPositionStatus.Disabled;
        }

        // Default to available/normal
        return ReagentPositionStatus.Available;
    }

    /// <summary>
    /// Maps rinse container protocol data to ReagentPositionStatus
    /// Implements piped in logic per requirements
    /// </summary>
    private static ReagentPositionStatus MapRinseContainerStatus(RinseContainer container, bool isPipedEnabled)
    {
        // Handle error conditions first
        if (container.Error == EnumRinseBottleError.RinseBottleVolumeReadError)
        {
            return ReagentPositionStatus.UndetectedVolume;
        }

        if (container.Error == EnumRinseBottleError.RinseBottleCannotPumpLiquidError)
        {
            return ReagentPositionStatus.DisconnectedBottle;
        }

        // If not in use and piped in is enabled, show as disabled
        if (isPipedEnabled && !container.InUse)
        {
            return ReagentPositionStatus.Disabled;
        }

        // Default to available
        return ReagentPositionStatus.Available;
    }

    private static FillOrEmptyIndicationType GetFillIndicationForRinse(RinseContainer container, DateTime utcNow)
    {
        if (!container.InUse && container.PercentageFull != -1)
        {
            if (container.CapDispenseStopped)
            {
                return FillOrEmptyIndicationType.Alarm;
            }
            else if (IsWithinCapDispenseWarningWindow(container.TimeCapDispenseStop, utcNow))
            {
                return FillOrEmptyIndicationType.Warning;
            }
            else if (container.PercentageFull < 50)
            {
                return FillOrEmptyIndicationType.Pending;
            }
        }

        return FillOrEmptyIndicationType.None;
    }

    private static FillOrEmptyIndicationType GetEmptyIndicationForWaste(WasteContainer container, DateTime utcNow)
    {
        if (!container.InUse && container.PercentageFull != -1)
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
