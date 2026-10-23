
namespace Pharos.Contracts.Notifications;

public interface IResultHub : IEntityHub
{
    Task CurveControlChartUpdated(string instrumentSerial, string methodName);
    Task CalibratorCurveChartUpdated(string instrumentSerial, string methodName);
    Task QualityControlChartUpdated(string instrumentSerial, string methodName);

}