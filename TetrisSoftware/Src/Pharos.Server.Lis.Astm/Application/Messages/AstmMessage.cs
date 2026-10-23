using System.Text;

namespace Pharos.Server.Lis.Astm.Application.Messages;

internal sealed class AstmMessage : IAstmMessage
{
    public AstmHeaderRecord? Header { get; set; }

    public IList<IAstmRecord> Records { get; } = new List<IAstmRecord>();

    public AstmTerminatorRecord? Terminator { get; set; }

    public string GetMessageString(bool redactSensitiveData = false)
    {
        var messageText = new StringBuilder();
        var delimiters = Header?.Delimiters ?? AstmFieldDelimiters.Default;

        if (Header is not null)
        {
            messageText.Append(Header);
            messageText.Append('\r');
        }

        foreach (var record in Records)
        {
            messageText.Append(record.ToRecordString(delimiters, redactSensitiveData));
            messageText.Append('\r');
        }

        if (Terminator is not null)
        {
            messageText.Append(Terminator.ToRecordString(delimiters, redactSensitiveData));
            messageText.Append('\r');
        }

        return messageText.ToString();
    }
}
