using System.Globalization;

namespace Pharos.Server.Lis.Astm.Application.Messages;

internal enum AstmCommentSource
{
    Prime, // COMMENT_SOURCE_UDM As String = "O"
    AllerQuest, // COMMENT_SOURCE_ALLERQUEST As String = "A"
    Inventory, // COMMENT_SOURCE_INVENTORY As String = "I"
    ResultInterpretation, // COMMENT_SOURCE_RESULT_INTERPRETATION As String = "N"
    SampleReport, // COMMENT_SOURCE_SAMPLE_REPORT_REF As String = "R"
    Tray, // COMMENT_SOURCE_TRAY_REF As String = "T"
    PhadiaXplain, // COMMENT_SOURCE_PHADIA_XPLAIN As String = "X"
    ImmunoCAPExtendedReport // COMMENT_SOURCE_IMMUNOCAP_EXTENDED_REPORT As String = "E"
}
internal enum AstmCommentType
{
    Automatic // COMMENT_TYPE_AUTOMATIC As String = "I"
}

internal enum AstmCommentTextClass
{
    None = 0,
    Disclaimer = 1
}

internal enum AstmCommentTextType
{
    ReferencePathTypeXml = 0,
    ReferencePathTypePdf = 1
}

internal sealed class AstmCommentRecord : IAstmRecord
{
    public char RecordType => AstmRecordType.Comment;
    public int SequenceNumber { get; set; }
    public AstmCommentTextType CommentTextType { get; set; }
    public string Header { get; set; } = "";
    public AstmCommentSource Source { get; set; }
    public string Text { get; set; } = "";
    public AstmCommentTextClass TextClass { get; set; }
    public AstmCommentType Type { get; set; }
    public IList<string> ParsingErrors { get; set; } = new List<string>();

    public string ToRecordString(AstmFieldDelimiters delimiters, bool redactSensitiveData = false)
    {
        var rb = new AstmRecordBuilder<CommentRecordField>(delimiters);

        rb.AddField(CommentRecordField.RecordTypeId, RecordType.ToString());

        rb.AddField(CommentRecordField.SequenceNumber, SequenceNumber.ToString(CultureInfo.InvariantCulture));
        rb.AddField(CommentRecordField.CommentSource, MapCommentSource(Source));

        var commentParts = new List<string>();
        if (Source == AstmCommentSource.PhadiaXplain)
        {
            commentParts.Add(Header);
            commentParts.Add(Text);
            commentParts.Add(((int)TextClass).ToString(CultureInfo.InvariantCulture));
        }
        else if (Source == AstmCommentSource.ImmunoCAPExtendedReport)
        {
            commentParts.Add(((int)CommentTextType).ToString(CultureInfo.InvariantCulture));
            commentParts.Add(Text);
        }
        else
        {
            commentParts.Add(Text);
        }

        rb.AddField(CommentRecordField.CommentText, commentParts.ToArray());

        rb.AddField(CommentRecordField.CommentType, Type == AstmCommentType.Automatic ? "I" : "");
        return rb.ToRecordString();
    }

    private static string MapCommentSource(AstmCommentSource source)
    {
        return source switch
        {
            AstmCommentSource.Prime => "O",
            AstmCommentSource.AllerQuest => "A",
            AstmCommentSource.Inventory => "I",
            AstmCommentSource.ResultInterpretation => "N",
            AstmCommentSource.SampleReport => "R",
            AstmCommentSource.Tray => "T",
            AstmCommentSource.PhadiaXplain => "X",
            AstmCommentSource.ImmunoCAPExtendedReport => "E",
            _ => ""
        };
    }

    private enum CommentRecordField
    {
        RecordTypeId,
        SequenceNumber,
        CommentSource,
        CommentText,
        CommentType
    }
}