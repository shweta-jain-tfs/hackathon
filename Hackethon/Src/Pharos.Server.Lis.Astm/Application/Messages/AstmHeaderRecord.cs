using System.Globalization;

namespace Pharos.Server.Lis.Astm.Application.Messages;

/// <summary>
/// Represents the ASTM processing ID that indicates the type of processing for the message.
/// </summary>
internal enum AstmProcessingId
{
    Undefined,
    /// <summary>
    /// Production mode - used for real patient samples and results.
    /// </summary>
    Production,

    /// <summary>
    /// Quality control mode - used for quality control samples and results.
    /// </summary>
    QualityControl
}

internal sealed record AstmHeaderRecord(
    AstmFieldDelimiters Delimiters,
    string SenderName,
    string SenderVersion,
    string UnifaceVersion,
    string ReceiverHostName,
    string ReceiverIpAddress,
    AstmProcessingId ProcessingId,
    DateTimeOffset? DateAndTimeOfMessage) : IAstmRecord
{
    public const string AstmVersion = "1";

    public char RecordType => AstmRecordType.MessageHeader;

    public IList<string> ParsingErrors { get; set; } = new List<string>();

    public int SequenceNumber { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    public static AstmHeaderRecord Parse(string recordString)
    {
        if (recordString.Length < 5 || AstmRecordParser.GetRecordType(recordString) != AstmRecordType.MessageHeader)
        {
            throw new AstmException("The record is not an ASTM message header.");
        }

        var delimiters = AstmFieldDelimiters.Parse(recordString);
        var parsingErrors = new List<string>();
        var parser = new AstmRecordParser(recordString, delimiters, parsingErrors);

        var record = new AstmHeaderRecord
        (
            delimiters,
            parser.FieldPart(HeaderRecordField.SenderNameOrId, 0),
            parser.FieldPart(HeaderRecordField.SenderNameOrId, 1),
            parser.FieldPart(HeaderRecordField.SenderNameOrId, 2),
            parser.FieldPart(HeaderRecordField.ReceiverId, 0),
            parser.FieldPart(HeaderRecordField.ReceiverId, 1),
            parser.Field(HeaderRecordField.ProcessingId).SubComponents(0).Component(0) switch
            {
                "P" => AstmProcessingId.Production,
                _ => AstmProcessingId.Undefined
            },
            parser.Field(HeaderRecordField.DateAndTimeOfMessage).AsDateAndTime()
        )
        {
            ParsingErrors = parsingErrors
        };

        return record;
    }

    public override string ToString()
    {
        var rb = new AstmRecordBuilder<HeaderRecordField>(Delimiters);

        rb.AddField(HeaderRecordField.RecordTypeId, RecordType.ToString());
        rb.AddField(HeaderRecordField.DelimiterDefinition, new DelimiterRecordField());
        rb.AddField(HeaderRecordField.SenderNameOrId, SenderName, SenderVersion, UnifaceVersion);
        rb.AddField(HeaderRecordField.ReceiverId, ReceiverHostName, ReceiverIpAddress);
        rb.AddField(HeaderRecordField.ProcessingId, ProcessingId switch
        {
            AstmProcessingId.Production => "P",
            AstmProcessingId.QualityControl => "Q",
            _ => "P"
        });
        rb.AddField(HeaderRecordField.VersionNo, AstmVersion);
        rb.AddField(HeaderRecordField.DateAndTimeOfMessage, DateAndTimeOfMessage?.ToString(AstmFieldParser.DateTimeFormat, CultureInfo.InvariantCulture) ?? "");

        return rb.ToRecordString();
    }

    string IAstmRecord.ToRecordString(AstmFieldDelimiters delimiters, bool redactSensitiveData)
    {
        return ToString();
    }

    private enum HeaderRecordField
    {
        RecordTypeId,
        DelimiterDefinition,
        MessageControlId,
        AccessPassword,
        SenderNameOrId,
        SenderStreetAddress,
        ReservedField,
        SenderTelephoneNumber,
        CharacteristicsOfSender,
        ReceiverId,
        Comment,
        ProcessingId,
        VersionNo,
        DateAndTimeOfMessage
    }
}
