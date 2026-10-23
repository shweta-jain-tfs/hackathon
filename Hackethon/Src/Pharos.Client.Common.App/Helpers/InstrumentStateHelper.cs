namespace Pharos.Client.Common.App.Helpers;

public enum InfoRow
{
    None,
    AssayEnds,
    AssayStartsIn,
    Finishing,
    TestsEnds,
    TestsFinished,
    Until,
}

/// <summary>
/// Holds keys and values used to build localized instrument state rows.
/// </summary>
public sealed record InstrumentStateRowsData(
    string StateKey,
    string? ProcedureKey,
    int? NumberOfTests,
    string? InfoKey,
    DateTimeOffset? TimeWhenNextStateReached);

/// <summary>
/// Converts instrument state into row data that the UI can translate and show.
/// </summary>
public static class InstrumentStateHelper
{
    private static readonly TimeSpan _standbyAssayCountdownThreshold = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Checks whether the standby assay start countdown should be visible.
    /// </summary>
    /// <param name="assaySchedulerNextRunType">Type of the next assay scheduler run.</param>
    /// <param name="assaySchedulerNextRun">Date/time of the next assay scheduler run.</param>
    /// <param name="now">Optional current timestamp override (primarily for deterministic tests).</param>
    /// <returns><see langword="true"/> when countdown UI should be shown; otherwise <see langword="false"/>.</returns>
    public static bool ShouldShowAssayStartCountdown(
        AssaySchedulerNextRunType assaySchedulerNextRunType,
        DateTimeOffset? assaySchedulerNextRun,
        DateTimeOffset? now = null)
    {
        // Countdown is only relevant for scheduled Start runs with a known next-run time.
        if (assaySchedulerNextRunType != AssaySchedulerNextRunType.Start || !assaySchedulerNextRun.HasValue)
        {
            return false;
        }

        var currentTime = now ?? DateTimeOffset.Now;
        var timeLeft = assaySchedulerNextRun.Value - currentTime;

        // Show only during the final standby window before auto-start.
        return timeLeft > TimeSpan.Zero && timeLeft <= _standbyAssayCountdownThreshold;
    }

    /// <summary>
    /// Builds instrument state row data (translation keys and values) for UI formatting.
    /// </summary>
    /// <param name="enabled">Indicates whether the instrument is enabled.</param>
    /// <param name="state">The current instrument state.</param>
    /// <param name="numberOfTests">The current number of tests.</param>
    /// <param name="timeWhenNextStateReached">The time when the next state is reached.</param>
    /// <param name="assaySchedulerNextRunType">Type of the next assay scheduler run.</param>
    /// <param name="assaySchedulerNextRun">Date/time of the next assay scheduler run.</param>
    /// <param name="hidePrimingTestCount">When true, forces numberOfTests to null for Priming and BlankMeasurement states (used by lockscreen).</param>
    /// <returns>Semantic row data used to format localized state rows.</returns>
    public static InstrumentStateRowsData GetInstrumentStateRowsData(
        bool enabled,
        InstrumentState state,
        int numberOfTests,
        DateTimeOffset? timeWhenNextStateReached,
        AssaySchedulerNextRunType assaySchedulerNextRunType = AssaySchedulerNextRunType.None,
        DateTimeOffset? assaySchedulerNextRun = null,
        bool hidePrimingTestCount = false)
    {
        if (!enabled)
        {
            return new InstrumentStateRowsData("InstrumentStateHelper_State_Disabled", null, null, null, null);
        }

        return state switch
        {
            // NOT from the communication protocol:
            InstrumentState.Offline => GetStateRows(
                stateKey: "InstrumentStateHelper_State_Offline",
                procedureKey: null,
                numberOfTests: null,
                infoRow: InfoRow.None,
                timeWhenNextStateReached: null),
            InstrumentState.Limited => GetStateRows(
                stateKey: "InstrumentStateHelper_State_Limited",
                procedureKey: null,
                numberOfTests: null,
                infoRow: InfoRow.None,
                timeWhenNextStateReached: null),
            InstrumentState.Synchronizing => GetStateRows(
                stateKey: "InstrumentStateHelper_State_Synchronizing",
                procedureKey: null,
                numberOfTests: null,
                infoRow: InfoRow.None,
                timeWhenNextStateReached: null),

            // From the communication protocol:
            InstrumentState.Aborting => GetStateRows(
                stateKey: "InstrumentStateHelper_State_Aborting",
                procedureKey: null,
                numberOfTests: null,
                infoRow: InfoRow.Until,
                timeWhenNextStateReached: timeWhenNextStateReached),
            InstrumentState.Assay => GetStateRows(
                stateKey: "InstrumentStateHelper_State_Assay",
                procedureKey: null,
                numberOfTests: numberOfTests,
                infoRow: InfoRow.TestsEnds,
                timeWhenNextStateReached: timeWhenNextStateReached,
                isAssayStatus: true),
            InstrumentState.AssayCmr => GetStateRows(
                stateKey: "InstrumentStateHelper_State_AssayCmr",
                procedureKey: null,
                numberOfTests: numberOfTests,
                infoRow: InfoRow.AssayEnds,
                timeWhenNextStateReached: timeWhenNextStateReached),
            InstrumentState.AssayCmrFinished => GetStateRows(
                stateKey: "InstrumentStateHelper_State_AssayCmrFinished",
                procedureKey: null,
                numberOfTests: null,
                infoRow: InfoRow.None,
                timeWhenNextStateReached: null),
            InstrumentState.AssayFinishing => GetStateRows(
                stateKey: "InstrumentStateHelper_State_Assay",
                procedureKey: null,
                numberOfTests: numberOfTests,
                infoRow: InfoRow.AssayEnds,
                timeWhenNextStateReached: timeWhenNextStateReached),
            InstrumentState.AssayFinishOrdered => GetStateRows(
                stateKey: "InstrumentStateHelper_State_Assay",
                procedureKey: null,
                numberOfTests: numberOfTests,
                infoRow: InfoRow.Finishing,
                timeWhenNextStateReached: timeWhenNextStateReached),
            InstrumentState.BlankMeasurement => GetStateRows(
                stateKey: "InstrumentStateHelper_State_Priming",
                procedureKey: null,
                numberOfTests: hidePrimingTestCount ? null : numberOfTests,
                infoRow: InfoRow.Until,
                timeWhenNextStateReached: timeWhenNextStateReached),
            InstrumentState.CmrPrepare => GetStateRows(
                stateKey: "InstrumentStateHelper_State_CmrPrepare",
                procedureKey: null,
                numberOfTests: numberOfTests,
                infoRow: InfoRow.None,
                timeWhenNextStateReached: null),
            InstrumentState.MaintenanceBlankMeasurement => GetStateRows(
                stateKey: "InstrumentStateHelper_State_Maintenance",
                procedureKey: "InstrumentStateHelper_Procedure_BlankMeasurement",
                numberOfTests: null,
                infoRow: InfoRow.Until,
                timeWhenNextStateReached: timeWhenNextStateReached),
            InstrumentState.MaintenanceCleaning => GetStateRows(
                stateKey: "InstrumentStateHelper_State_Maintenance",
                procedureKey: "InstrumentStateHelper_Procedure_Cleaning",
                numberOfTests: null,
                infoRow: InfoRow.Until,
                timeWhenNextStateReached: timeWhenNextStateReached),
            InstrumentState.MaintenanceExtendedRinsing => GetStateRows(
                stateKey: "InstrumentStateHelper_State_Maintenance",
                procedureKey: "InstrumentStateHelper_Procedure_ExtendedRinse",
                numberOfTests: null,
                infoRow: InfoRow.Until,
                timeWhenNextStateReached: timeWhenNextStateReached),
            InstrumentState.MaintenanceManualCleaning => GetStateRows(
                stateKey: "InstrumentStateHelper_State_Maintenance",
                procedureKey: "InstrumentStateHelper_Procedure_ManualCleaning",
                numberOfTests: null,
                infoRow: InfoRow.Until,
                timeWhenNextStateReached: timeWhenNextStateReached),
            InstrumentState.MaintenanceRinsing => GetStateRows(
                stateKey: "InstrumentStateHelper_State_Maintenance",
                procedureKey: "InstrumentStateHelper_Procedure_Rinsing",
                numberOfTests: null,
                infoRow: InfoRow.Until,
                timeWhenNextStateReached: timeWhenNextStateReached),
            InstrumentState.PowerSaving => GetStateRows(
                stateKey: "InstrumentStateHelper_State_PowerSaving",
                procedureKey: null,
                numberOfTests: null,
                infoRow: InfoRow.None,
                timeWhenNextStateReached: null),
            InstrumentState.Priming => GetStateRows(
                stateKey: "InstrumentStateHelper_State_Priming",
                procedureKey: null,
                numberOfTests: hidePrimingTestCount ? null : numberOfTests,
                infoRow: InfoRow.Until,
                timeWhenNextStateReached: timeWhenNextStateReached),
            InstrumentState.Rinsing => GetStateRows(
                stateKey: "InstrumentStateHelper_State_Rinsing",
                procedureKey: null,
                numberOfTests: null,
                infoRow: InfoRow.Until,
                timeWhenNextStateReached: timeWhenNextStateReached),
            InstrumentState.Service => GetStateRows(
                stateKey: "InstrumentStateHelper_State_Service",
                procedureKey: null,
                numberOfTests: null,
                infoRow: InfoRow.None,
                timeWhenNextStateReached: null),
            InstrumentState.ServiceEjectingCapWell => GetStateRows(
                stateKey: "InstrumentStateHelper_State_Service",
                procedureKey: "InstrumentStateHelper_Procedure_CapWellEject",
                numberOfTests: null,
                infoRow: InfoRow.Until,
                timeWhenNextStateReached: timeWhenNextStateReached),
            InstrumentState.ServiceVerifyingStorage => GetStateRows(
                stateKey: "InstrumentStateHelper_State_Service",
                procedureKey: "InstrumentStateHelper_Procedure_VerifyingStorage",
                numberOfTests: null,
                infoRow: InfoRow.Until,
                timeWhenNextStateReached: timeWhenNextStateReached),
            InstrumentState.Shutdown => GetStateRows(
                stateKey: "InstrumentStateHelper_State_Shutdown",
                procedureKey: null,
                numberOfTests: null,
                infoRow: InfoRow.Until,
                timeWhenNextStateReached: timeWhenNextStateReached),
            InstrumentState.Standby => GetStandbyStateRows(numberOfTests, assaySchedulerNextRunType, assaySchedulerNextRun),
            InstrumentState.Startup => GetStateRows(
                stateKey: "InstrumentStateHelper_State_Startup",
                procedureKey: null,
                numberOfTests: null,
                infoRow: InfoRow.Until,
                timeWhenNextStateReached: timeWhenNextStateReached),
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
        };
    }

    private static InstrumentStateRowsData GetStandbyStateRows(
        int numberOfTests,
        AssaySchedulerNextRunType assaySchedulerNextRunType,
        DateTimeOffset? assaySchedulerNextRun)
    {
        var showAssayStartCountdown = ShouldShowAssayStartCountdown(assaySchedulerNextRunType, assaySchedulerNextRun);

        return GetStateRows(
            stateKey: "InstrumentStateHelper_State_Standby",
            procedureKey: null,
            numberOfTests: numberOfTests,
            infoRow: showAssayStartCountdown ? InfoRow.AssayStartsIn : InfoRow.None,
            timeWhenNextStateReached: showAssayStartCountdown ? assaySchedulerNextRun : null);
    }

    /// <summary>
    /// Creates row data for one state, including optional tests and info row details.
    /// </summary>
    /// <param name="stateKey">Translation key for the main state text.</param>
    /// <param name="procedureKey">Optional translation key for a procedure text.</param>
    /// <param name="numberOfTests">Optional number of tests to show.</param>
    /// <param name="infoRow">Info row type to map to a translation key.</param>
    /// <param name="timeWhenNextStateReached">Optional time used in the info row.</param>
    /// <param name="isAssayStatus">Whether assay rules should be used for showing tests/info.</param>
    /// <returns>Row data with state/procedure keys and optional info details.</returns>
    private static InstrumentStateRowsData GetStateRows(
        string stateKey,
        string? procedureKey,
        int? numberOfTests,
        InfoRow infoRow,
        DateTimeOffset? timeWhenNextStateReached,
        bool isAssayStatus = false)
    {
        // Show tests only when tests > 0 or this is an assay state.
        var testsToShow = numberOfTests.HasValue && (numberOfTests.Value > 0 || isAssayStatus)
            ? numberOfTests
            : null;

        string? infoKey = null;
        DateTimeOffset? infoTime = null;

        if (infoRow != InfoRow.None)
        {
            if (infoRow == InfoRow.TestsFinished)
            {
                infoKey = GetInfoKey(infoRow);
                infoTime = null;
            }
            else if (timeWhenNextStateReached.HasValue)
            {
                var showInfoRow = !isAssayStatus || numberOfTests != 0;

                if (showInfoRow)
                {
                    infoKey = GetInfoKey(infoRow);
                    infoTime = timeWhenNextStateReached.Value;
                }
            }
        }

        return new InstrumentStateRowsData(stateKey, procedureKey, testsToShow, infoKey, infoTime);
    }

    private static string GetInfoKey(InfoRow infoRow)
    {
        return infoRow switch
        {
            InfoRow.AssayEnds => "InstrumentStateHelper_Info_AssayEnds",
            InfoRow.AssayStartsIn => "InstrumentStateHelper_Info_AssayStartsIn",
            InfoRow.Finishing => "InstrumentStateHelper_Info_Finishing",
            InfoRow.TestsEnds => "InstrumentStateHelper_Info_TestsEnds",
            InfoRow.TestsFinished => "InstrumentStateHelper_Info_TestsFinished",
            InfoRow.Until => "InstrumentStateHelper_Info_Until",
            _ => throw new ArgumentOutOfRangeException(nameof(infoRow), infoRow, null)
        };
    }
}