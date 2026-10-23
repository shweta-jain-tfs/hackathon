using Microsoft.Extensions.Localization;

namespace Pharos.Client.App.Quality.Views.QualityControls.Helpers;
public static class MethodQualityControlHelper
{
    public static bool IsLotValid(QualityControlLot lot)
    {
        if (lot == null)
        {
            return false;
        }
        if (lot.LotType == LotType.TargetValue)
        {
            if (lot.ExpectedConcentration == null
                || lot.ExpectedStdDev == null
                || lot.StdDevLimit == null)
            {
                return false;
            }
            return lot.ExpectedConcentration >= 0
                && lot.ExpectedStdDev >= 0
                && lot.StdDevLimit >= 0;
        }
        else if (lot.LotType == LotType.AcceptanceRange)
        {
            var min = lot.RangeMin;
            var max = lot.RangeMax;

            if (!min.HasValue)
            {
                // invalid if both min and max is missing
                return max.HasValue;
            }

            if (!max.HasValue)
            {
                return true;
            }

            return min <= max;
        }
        else if (lot.LotType == LotType.UseDefaultQcValues)
        {
            return true;
        }
        return false;
    }
    public static string QualityControlTypeToString(bool isUserdefined, IStringLocalizer localizer)
    {
        return isUserdefined
            ? localizer["MethodQualityControl_Lab"]
            : localizer["MethodQualityControl_Phadia"];
    }
}
