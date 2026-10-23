using Pharos.Client.Common.Models;
using Pharos.Common.Web.Components.Status;

namespace Pharos.Client.App.InstrumentManagement.ViewModels.Dashboard
{
    public enum SensorStatus
    {
        DataNotAvailable,
        InsideLimit,
        OutsideLimit,
    }

    public static class SensorStatusHelper
    {
        /// <summary>
        /// Determines the status for a list of sensors.
        /// </summary>
        public static SensorStatus GetSensorStatus(ICollection<SensorValue> sensors)
        {
            if (sensors.Any(sensor => sensor.OutsideLimit))
            {
                return SensorStatus.OutsideLimit;
            }

            if (sensors.Any(sensor => sensor.Value == null || sensor.LowerLimit == null || sensor.UpperLimit == null))
            {
                return SensorStatus.DataNotAvailable;
            }

            return SensorStatus.InsideLimit;
        }

        /// <summary>
        /// Maps SensorStatus to StatusType for UI.
        /// </summary>
        public static StatusType ToStatusType(SensorStatus status)
        {
            return status switch
            {
                SensorStatus.InsideLimit => StatusType.Success,
                SensorStatus.OutsideLimit => StatusType.Alarm,
                SensorStatus.DataNotAvailable => StatusType.Disconnected,
                _ => StatusType.Disconnected
            };
        }
    }
}