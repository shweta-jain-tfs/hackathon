using Microsoft.Extensions.Localization;
using Pharos.Client.Common.App.PageRouting;
using Pharos.Client.Common.Localization;

namespace Pharos.Client.App.TaskCard.ViewModels;

public interface ITaskCardConnectedPage
{
    (string Url, string Title) GetConnectedPageInfo(TaskCardType taskCard);
}

public class TaskCardConnectedPage : ITaskCardConnectedPage
{
    private readonly IStringLocalizer<Translations> _loc;

    public TaskCardConnectedPage(IStringLocalizer<Translations> localizer)
    {
        _loc = localizer;
    }

    public (string Url, string Title) GetConnectedPageInfo(TaskCardType taskCard)
    {
        switch (taskCard)
        {
            case TaskCardType.SampleDrawerOpen:
            case TaskCardType.SampleTrayError:
            case TaskCardType.SampleTubeError:
            case TaskCardType.SamplePartlyCompleted:
            case TaskCardType.LoadEmptySampleTray:
            case TaskCardType.LoadEmptyLeftSampleTray:
            case TaskCardType.LoadEmptyRightSampleTray:
            case TaskCardType.RemoveSamplesFromDrawer:
                return (PageRoute.Samples, _loc["NavMenu_SampleDrawer"]);

            case TaskCardType.Error:
                return (PageRoute.Logs, _loc["NavMenu_Logs"]);

            case TaskCardType.ResultReviewPending:
            case TaskCardType.CalibrationIssue:
            case TaskCardType.ResultsIssue:
            case TaskCardType.QCIssue:
                return (PageRoute.Results, _loc["NavMenu_Results"]);

            case TaskCardType.ReagentDrawerOpen:
            case TaskCardType.ReagentDrawerError:
            case TaskCardType.RemoveReagentsFromDrawer:
            case TaskCardType.ReagentLoaderWithError:
            case TaskCardType.NeedEmptyLoader:
                return (PageRoute.Reagent, _loc["NavMenu_Reagents"]);

            case TaskCardType.InventoryCarrierError:
                return ($"{PageRoute.Inventory}?tab=carriers", _loc["NavMenu_Inventory"]);

            case TaskCardType.InventoryCartridgeError:
                return ($"{PageRoute.Inventory}?tab=cartridges", _loc["NavMenu_Inventory"]);

            case TaskCardType.MaintenanceCleaning:
                return (PageRoute.Maintenance + $"?showMaintenance={(int)MaintenanceActivityType.Cleaning}", _loc["NavMenu_Maintenance"]);

            case TaskCardType.MaintenanceManualCleaning:
                return (PageRoute.Maintenance + $"?showMaintenance={(int)MaintenanceActivityType.ManualCleaning}", _loc["NavMenu_Maintenance"]);

            case TaskCardType.BottleDrawerError:
            case TaskCardType.BottleDrawerOpen:
                return (PageRoute.Bottle, _loc["NavMenu_BottledReagentDrawer"]);
            case TaskCardType.UrgentLoading:
                return (PageRoute.LoadList, _loc["NavMenu_LoadList"]);
            case TaskCardType.LiquidDrawerOpen:
            case TaskCardType.CheckLiquidDrawer:
            case TaskCardType.LiquidEmptyWaste:
            case TaskCardType.LiquidRefillRinse:
                return (PageRoute.Liquid, _loc["NavMenu_LiquidWasteDrawer"]);

            case TaskCardType.UpdatesAvailable:
            case TaskCardType.UpdateSuccessful:
            case TaskCardType.UpdateFailed:
                return (PageRoute.About, _loc["NavMenu_About"]);
            case TaskCardType.SolidWasteDrawerOpen:
            case TaskCardType.SolidWasteEmpty:
            case TaskCardType.CheckWasteDrawer:
                return (PageRoute.SolidWaste, _loc["NavMenu_SolidWasteDrawer"]);
            case TaskCardType.TemperatureOutOfRange:
                return (PageRoute.DashboardTemperatures, _loc["NavMenu_Dashboard"]);
            case TaskCardType.HumidityOutOfRange:
                return (PageRoute.DashboardHumidity, _loc["NavMenu_Dashboard"]);
            case TaskCardType.InstrumentSoftwareOffline:
                return (PageRoute.Instruments, _loc["NavMenu_Instruments"]);
            case TaskCardType.ConnectionFailure:
                return (PageRoute.DashboardConnections, _loc["ConnectionsPage_Title"]);
            case TaskCardType.RequestSampleIssue:
            case TaskCardType.RequestReportResult:
                return (PageRoute.Requests, _loc["NavMenu_Requests"]);
            default:
                return (string.Empty, string.Empty);
        }
    }
}