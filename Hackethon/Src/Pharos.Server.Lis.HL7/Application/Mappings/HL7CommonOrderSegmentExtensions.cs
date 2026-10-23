using Pharos.Server.Application.Lis;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Lis.HL7.Application.Messages;

namespace Pharos.Server.Lis.HL7.Application.Mappings;

internal static class HL7CommonOrderSegmentExtensions
{
    public static void MapFromSample(this HL7CommonOrderSegment segment, LisSample sample, HL7ExportSettings exportSettings)
    {
        segment.OrderControl = HL7OrderControl.New;
        segment.PlacerOrderNumber = sample.RequestId ?? "";
        segment.TransactionDateTime = sample.RequestDate;

        if (sample.Requestor == null || exportSettings.SkipExportOfRequestorInformation)
        {
            return;
        }

        var nameParts = SplitValue(sample.Requestor.Name, ',');
        var addressParts = SplitLines(sample.Requestor.Address);

        segment.RequestorId = sample.Requestor.RequestorId;
        segment.RequestorLastName = ValueAt(nameParts, 0);
        segment.RequestorFirstName = ValueAt(nameParts, 1);
        segment.RequestorMiddleName = ValueAt(nameParts, 2);
        segment.RequestorStreetAddress = ValueAt(addressParts, 0);
        segment.RequestorOtherAddress = ValueAt(addressParts, 1);
        segment.RequestorCity = ValueAt(addressParts, 2);
        segment.RequestorState = ValueAt(addressParts, 3);
        segment.RequestorZipCode = sample.Requestor.ZipCode;
        segment.RequestorCountry = ValueAt(addressParts, 4);
        segment.RequestorPhone = sample.Requestor.PhoneNumber ?? "";
    }

    public static void MapToSample(this HL7CommonOrderSegment order, LisSample sample, bool skipRequestorInformation)
    {
        sample.RequestDate = order.TransactionDateTime;
        sample.SampleCollectionDate = order.TransactionDateTime;

        if (skipRequestorInformation)
        {
            return;
        }

        sample.Requestor = new LisRequestor
        {
            RequestorId = order.RequestorId,
            Name = CombineValues(", ", order.RequestorLastName, order.RequestorMiddleName, order.RequestorFirstName),
            Address = CombineValues(Environment.NewLine, order.RequestorStreetAddress, order.RequestorCity, order.RequestorState, order.RequestorZipCode, order.RequestorCountry),
            ZipCode = order.RequestorZipCode,
            PhoneNumber = order.RequestorPhone
        };
    }

    private static string CombineValues(string separator, params string[] values)
    {
        return string.Join(separator, values.Where(v => !string.IsNullOrEmpty(v)));
    }

    private static string ValueAt(string[] values, int index)
    {
        return index < values.Length ? values[index] : "";
    }

    private static string[] SplitValue(string? value, char separator)
    {
        return string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(separator, StringSplitOptions.TrimEntries);
    }

    private static string[] SplitLines(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? []
            : value.Replace("\r", "").Split('\n', StringSplitOptions.TrimEntries);
    }
}
