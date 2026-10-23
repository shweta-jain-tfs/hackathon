namespace Pharos.Client.App.Parameters.QualityControl.ViewModels;

public record FormatPosition(char DisplayValue, bool Valid);

public interface IBarcodeFormatBuilder
{
    FormatPosition[] GetFormat(int totalLength, int qcStartPosition, int qcLength, bool containsLotInformation, int lotStartPosition, int lotLength);
    string GetString(int totalLength, int qcStartPosition, int qcLength, bool containsLotInformation, int lotStartPosition, int lotLength);
}

public class BarcodeFormatBuilder : IBarcodeFormatBuilder
{
    public FormatPosition[] GetFormat(int totalLength, int qcStartPosition, int qcLength, bool containsLotInformation, int lotStartPosition, int lotLength)
    {
        if (totalLength <= 0)
        {
            return [];
        }

        // Calculate the maximum position needed to show all QC and Lot positions
        var maxLength = totalLength;

        if (qcStartPosition > 0 && qcLength > 0)
        {
            var qcEnd = qcStartPosition - 1 + qcLength;
            maxLength = Math.Max(maxLength, qcEnd);
        }

        if (containsLotInformation && lotStartPosition > 0 && lotLength > 0)
        {
            var lotEnd = lotStartPosition - 1 + lotLength;
            maxLength = Math.Max(maxLength, lotEnd);
        }

        var positions = new FormatPosition[maxLength];

        // Fill valid positions with '•' and invalid positions (beyond totalLength) with placeholder
        for (var i = 0; i < maxLength; i++)
        {
            var isValid = i < totalLength;
            positions[i] = new FormatPosition('•', isValid);
        }

        // Mark QC positions (1-based to 0-based conversion)
        if (qcStartPosition > 0 && qcLength > 0)
        {
            var startIndex = qcStartPosition - 1;
            for (var i = 0; i < qcLength && startIndex + i < maxLength; i++)
            {
                var index = startIndex + i;
                var isValid = index < totalLength;
                positions[index] = new FormatPosition('Q', isValid);
            }
        }

        // Mark Lot positions (1-based to 0-based conversion)
        if (containsLotInformation && lotStartPosition > 0 && lotLength > 0)
        {
            var startIndex = lotStartPosition - 1;
            for (var i = 0; i < lotLength && startIndex + i < maxLength; i++)
            {
                var index = startIndex + i;
                var isValid = index < totalLength;

                // Mark overlap as 'X' (always invalid) if position is already marked as 'Q'
                if (positions[index].DisplayValue == 'Q')
                {
                    positions[index] = new FormatPosition('X', false);
                }
                else
                {
                    positions[index] = new FormatPosition('L', isValid);
                }
            }
        }

        return positions;
    }

    public string GetString(int totalLength, int qcStartPosition, int qcLength, bool containsLotInformation, int lotStartPosition, int lotLength)
    {
        var format = GetFormat(totalLength, qcStartPosition, qcLength, containsLotInformation, lotStartPosition, lotLength);
        return new string(format.Select(f => f.DisplayValue).ToArray());
    }
}