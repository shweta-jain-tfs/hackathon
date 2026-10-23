using System.Globalization;
using Pharos.Server.Infrastructure.Logging;
using Pharos.Server.Lis.HL7.Application.Interfaces;

namespace Pharos.Server.Lis.HL7.Application.Messages;

internal enum HL7OrderControl
{
    New, // NW
    Cancel, // CA
    Replace, // RP
    Change // CH
}

internal sealed class HL7CommonOrderSegment : IHL7Segment
{
    public string SegmentType => HL7SegmentType.CommonOrder;
    public int SequenceNumber { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public HL7OrderControl OrderControl { get; set; }
    public string PlacerOrderNumber { get; set; } = "";
    public DateTimeOffset? TransactionDateTime { get; set; }
    public string RequestorId { get; set; } = "";
    public string RequestorLastName { get; set; } = "";
    public string RequestorFirstName { get; set; } = "";
    public string RequestorMiddleName { get; set; } = "";
    public string RequestorStreetAddress { get; set; } = "";
    public string RequestorOtherAddress { get; set; } = "";
    public string RequestorCity { get; set; } = "";
    public string RequestorState { get; set; } = "";
    public string RequestorZipCode { get; set; } = "";
    public string RequestorCountry { get; set; } = "";
    public string RequestorPhone { get; set; } = "";

    public static HL7CommonOrderSegment Parse(string segmentString, HL7FieldDelimiters delimiters)
    {
        var parser = new HL7SegmentParser(segmentString, delimiters);

        var segment = new HL7CommonOrderSegment
        {
            OrderControl = parser.Field(CommonOrderSegmentField.OrderControl).Component(0) switch
            {
                "CA" => HL7OrderControl.Cancel,
                "RP" => HL7OrderControl.Replace,
                "CH" => HL7OrderControl.Change,
                _ => HL7OrderControl.New
            },
            PlacerOrderNumber = parser.Field(CommonOrderSegmentField.PlacerOrderNumber).Component(0),
            TransactionDateTime = parser.Field(CommonOrderSegmentField.TransactionDateTime).AsDateAndTime(),
            RequestorId = parser.FieldPart(CommonOrderSegmentField.OrderingProvider, 0),
            RequestorLastName = parser.FieldPart(CommonOrderSegmentField.OrderingProvider, 1),
            RequestorFirstName = parser.FieldPart(CommonOrderSegmentField.OrderingProvider, 2),
            RequestorMiddleName = parser.FieldPart(CommonOrderSegmentField.OrderingProvider, 3),
            RequestorPhone = parser.FieldPart(CommonOrderSegmentField.OrderingFacilityPhoneNumber, 0),
            RequestorStreetAddress = parser.FieldPart(CommonOrderSegmentField.OrderingProviderAddress, 0),
            RequestorOtherAddress = parser.FieldPart(CommonOrderSegmentField.OrderingProviderAddress, 1),
            RequestorCity = parser.FieldPart(CommonOrderSegmentField.OrderingProviderAddress, 2),
            RequestorState = parser.FieldPart(CommonOrderSegmentField.OrderingProviderAddress, 3),
            RequestorZipCode = parser.FieldPart(CommonOrderSegmentField.OrderingProviderAddress, 4),
            RequestorCountry = parser.FieldPart(CommonOrderSegmentField.OrderingProviderAddress, 5)
        };

        return segment;
    }

    public string ToSegmentString(HL7FieldDelimiters delimiters, bool redactSensitiveData = false)
    {
        var builder = new HL7SegmentBuilder<CommonOrderSegmentField>(delimiters);

        builder.AddField(CommonOrderSegmentField.SegmentTypeId, SegmentType);
        builder.AddField(CommonOrderSegmentField.OrderControl, "RE");
        builder.AddField(CommonOrderSegmentField.PlacerOrderNumber, PlacerOrderNumber.Redact(redactSensitiveData));
        builder.AddField(CommonOrderSegmentField.TransactionDateTime, TransactionDateTime?.ToString(HL7FieldParser.DateTimeFormat, CultureInfo.InvariantCulture) ?? "");
        builder.AddField(
            CommonOrderSegmentField.OrderingProvider,
            RequestorId,
            RequestorLastName.Redact(redactSensitiveData),
            RequestorFirstName.Redact(redactSensitiveData),
            RequestorMiddleName.Redact(redactSensitiveData));
        builder.AddField(CommonOrderSegmentField.OrderingFacilityPhoneNumber, RequestorPhone.Redact(redactSensitiveData));
        builder.AddField(
            CommonOrderSegmentField.OrderingProviderAddress,
            RequestorStreetAddress.Redact(redactSensitiveData),
            RequestorOtherAddress.Redact(redactSensitiveData),
            RequestorCity.Redact(redactSensitiveData),
            RequestorState.Redact(redactSensitiveData),
            RequestorZipCode.Redact(redactSensitiveData),
            RequestorCountry.Redact(redactSensitiveData));

        return builder.ToSegmentString();
    }

    private enum CommonOrderSegmentField
    {
        SegmentTypeId,
        OrderControl,
        PlacerOrderNumber,
        FillerOrderNumber,
        PlacerGroupNumber,
        OrderStatus,
        ResponseFlag,
        QuantityTiming,
        Parent,
        TransactionDateTime,
        EnteredBy,
        VerifiedBy,
        OrderingProvider,
        EntreesLocation,
        CallBackPhoneNumber,
        OrderEffectiveDateTime,
        OrderControlCodeReason,
        EnteringOrganization,
        EnteringDevice,
        ActionBy,
        PlaceHolder1,
        PlaceHolder2,
        OrderingFacilityAddress,
        OrderingFacilityPhoneNumber,
        OrderingProviderAddress
    }
}
