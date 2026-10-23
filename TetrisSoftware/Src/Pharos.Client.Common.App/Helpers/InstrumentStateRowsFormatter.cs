using Pharos.Client.Common.App.Extensions;

namespace Pharos.Client.Common.App.Helpers;

public static class InstrumentStateRowsFormatter
{
    private const string _assayStartsInInfoKey = "InstrumentStateHelper_Info_AssayStartsIn";

    public static (string StateRow1, string StateRow2) Format(
        InstrumentStateRowsData rowData,
        IStringLocalizer<Translations> loc)
    {
        var stateRow = loc[rowData.StateKey].Value;

        if (!string.IsNullOrEmpty(rowData.ProcedureKey))
        {
            var procedure = loc[rowData.ProcedureKey].Value;
            stateRow = loc["InstrumentStateHelper_Format_StateWithProcedure", stateRow, procedure].Value;
        }

        if (rowData.NumberOfTests.HasValue)
        {
            stateRow = loc["InstrumentStateHelper_Format_StateWithTests", stateRow, rowData.NumberOfTests.Value].Value;
        }

        return (stateRow, GetInfoRow(rowData, loc));
    }

    public static (string StateRow1, string StateRow2, string StateRow3) FormatLockScreen(
        InstrumentStateRowsData rowData,
        IStringLocalizer<Translations> loc)
    {
        var stateRow = loc[rowData.StateKey].Value;

        if (!string.IsNullOrEmpty(rowData.ProcedureKey))
        {
            var procedure = loc[rowData.ProcedureKey].Value;
            stateRow = loc["Lockscreen_InstrumentStateHelper_Format_StateWithProcedure", stateRow, procedure].Value;
        }

        var testsRow = rowData.NumberOfTests.HasValue
            ? loc["Lockscreen_InstrumentStateHelper_Format_RemainingTests", rowData.NumberOfTests.Value].Value
            : string.Empty;

        return (stateRow, testsRow, GetInfoRow(rowData, loc));
    }

    private static string GetInfoRow(
        InstrumentStateRowsData rowData,
        IStringLocalizer<Translations> loc)
    {
        var infoRow = string.Empty;
        if (!string.IsNullOrEmpty(rowData.InfoKey) && rowData.TimeWhenNextStateReached.HasValue)
        {
            var label = loc[rowData.InfoKey].Value;

            if (rowData.InfoKey == _assayStartsInInfoKey)
            {
                var countdown = DateTimeHelper.GetTimeLeftMinutesSeconds(rowData.TimeWhenNextStateReached.Value);
                infoRow = loc["InstrumentStateHelper_Format_InfoRowCountdown", label, countdown].Value;
            }
            else
            {
                var time = rowData.TimeWhenNextStateReached.Value.ToShortTimeString();
                var timeLeft = DateTimeHelper.GetTimeLeft(rowData.TimeWhenNextStateReached.Value);
                infoRow = loc["InstrumentStateHelper_Format_InfoRow", label, time, timeLeft].Value;
            }
        }
        return infoRow;
    }
}