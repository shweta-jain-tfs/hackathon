using Pharos.Client.App.Parameters.Common.Models;
using Pharos.Client.App.Parameters.QualityControl.Models;
using Pharos.Client.Common.ViewModels.Interfaces;

namespace Pharos.Client.App.Parameters.QualityControl.ViewModels;

public class QualityControlScheduleListViewModel : VirtualGridViewModel<QualityControlSchedule>
{
    private readonly IQualityControlScheduleItemSource _itemSource;
    private readonly IMethodTestDataSource _methodTestDataSource;

    public QualityControlScheduleListViewModel(IQualityControlScheduleItemSource itemSource, IMethodTestDataSource methodTestDataSource, IExceptionHandlerService exceptionHandler) : base(itemSource, exceptionHandler)
    {
        _itemSource = itemSource;
        _methodTestDataSource = methodTestDataSource;
    }

    public async Task<Dictionary<string, string>> GetMethodNames()
    {
        var names = await _methodTestDataSource.GetMethodNamesAsync();
        return names.ToDictionary(x => x, x => x);
    }

    public async Task UpdateFirstInRun(QualityControlSchedule context, bool isChecked)
    {
        var command = CreateCommand(context);
        command.FirstInRun = isChecked;
        await _itemSource.Update(command);
    }

    public async Task UpdateLastInRun(QualityControlSchedule context, bool isChecked)
    {
        var command = CreateCommand(context);
        command.LastInRun = isChecked;
        await _itemSource.Update(command);
    }

    public async Task UpdateAfterCalibration(QualityControlSchedule context, bool isChecked)
    {
        var command = CreateCommand(context);
        command.AfterCalibration = isChecked;
        await _itemSource.Update(command);
    }

    public async Task UpdatePeriodicalInformation(QualityControlSchedule context, Periodically periodically, int startAfter, int runEvery)
    {
        var command = CreateCommand(context);
        command.Periodically = periodically;
        command.StartAfter = startAfter;
        command.RunEvery = runEvery;
        await _itemSource.Update(command);
    }

    private static UpdateQualityControlScheduleCommand CreateCommand(QualityControlSchedule item)
    {
        return new UpdateQualityControlScheduleCommand
        {
            Id = item.Id,
            FirstInRun = item.FirstInRun,
            LastInRun = item.LastInRun,
            AfterCalibration = item.AfterCalibration,
            StartAfter = item.StartAfter,
            RunEvery = item.RunEvery,
            Periodically = item.Periodically
        };
    }
}