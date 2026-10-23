namespace Pharos.Server.Lis.Astm.Application.Messages;

internal sealed class AstmMessageParser : ILisMessageParser<IAstmMessage>
{
    public IAstmMessage ParseMessage(string messageString)
    {
        var records = messageString.Split(AstmControlSymbols.RecordDelimiter)
                             .Where(s => !string.IsNullOrEmpty(s))
                             .ToList();

        if (records.Count <= 1)
        {
            // need at least header and terminator
            throw new AstmException("ASTM message must include at least two records.");
        }

        var headerRecord = records.First();
        var header = AstmHeaderRecord.Parse(headerRecord);
        var delimiters = header.Delimiters;

        var message = new AstmMessage
        {
            Header = header
        };

        for (var i = 1; i < records.Count; i++)
        {
            var recordString = records[i];
            if (!IsSupportedRecordType(AstmRecordParser.GetRecordType(recordString)))
            {
                message.Records.Add(new AstmUnsupportedRecord(recordString));
                continue;
            }

            var record = ParseRecord(recordString, delimiters);

            if (record is AstmHeaderRecord repeatedHeader)
            {
                delimiters = repeatedHeader.Delimiters;
                message.Records.Add(repeatedHeader);
                continue;
            }

            if (record is AstmTerminatorRecord terminator)
            {
                message.Terminator = terminator;
                continue;
            }

            message.Records.Add(record);
        }

        if (message.Terminator == null)
        {
            throw new AstmException("The ASTM message contains no terminator record.");
        }

        return message;
    }

    private static IAstmRecord ParseRecord(string recordString, AstmFieldDelimiters delimiters)
    {
        var recordType = AstmRecordParser.GetRecordType(recordString);

        return recordType switch
        {
            AstmRecordType.MessageHeader => AstmHeaderRecord.Parse(recordString),
            AstmRecordType.PatientInformation => AstmPatientRecord.Parse(recordString, delimiters),
            AstmRecordType.TestOrder => AstmTestOrderRecord.Parse(recordString, delimiters),
            AstmRecordType.MessageTerminator => AstmTerminatorRecord.Parse(recordString, delimiters),
            AstmRecordType.RequestInformation => AstmRequestRecord.Parse(recordString, delimiters),
            AstmRecordType.Result => throw new AstmException($"Received an unsupported ASTM record type '{recordType}'."),
            AstmRecordType.Comment => throw new AstmException($"Received an unsupported ASTM record type '{recordType}'."),
            AstmRecordType.Scientific => throw new AstmException($"Received an unsupported ASTM record type '{recordType}'."),
            AstmRecordType.ManufacturerInformation => throw new AstmException($"Received an unsupported ASTM record type '{recordType}'."),
            _ => throw new AstmException($"Unknown ASTM record type '{recordType}'.")
        };
    }

    private static bool IsSupportedRecordType(char recordType)
    {
        return recordType is AstmRecordType.MessageHeader
            or AstmRecordType.PatientInformation
            or AstmRecordType.TestOrder
            or AstmRecordType.MessageTerminator
            or AstmRecordType.RequestInformation;
    }

}
