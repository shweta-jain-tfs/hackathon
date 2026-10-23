using System.Globalization;
using Pharos.Server.Lis.HL7.Application.Interfaces;

namespace Pharos.Server.Lis.HL7.Application.Messages;

internal enum HL7CommentSource
{
    Prime, // O
    AllerQ, // A
    Report, // R
    ResultInterpretation, // N
    ImmunoCAPGuide, // L
    PhadiaXplain, // X
    PhadiaXplainICAP // E
}

internal enum HL7CommentType
{
    Generic, // G
    Automatic // I
}

internal sealed class HL7CommentSegment : IHL7Segment
{
    public string SegmentType => HL7SegmentType.Comment;
    public int SequenceNumber { get; set; } = 1;
    public HL7CommentSource CommentSource { get; set; }
    public string Comment { get; set; } = "";
    public HL7CommentType CommentType { get; set; }

    public string ToSegmentString(HL7FieldDelimiters delimiters, bool redactSensitiveData = false)
    {
        var builder = new HL7SegmentBuilder<CommentSegmentField>(delimiters);

        builder.AddField(CommentSegmentField.SegmentTypeId, SegmentType);
        builder.AddField(CommentSegmentField.SetId, SequenceNumber.ToString(CultureInfo.InvariantCulture));
        builder.AddField(CommentSegmentField.SourceOfComment, MapCommentSource(CommentSource));
        builder.AddField(CommentSegmentField.Comment, Comment);
        builder.AddField(CommentSegmentField.CommentType, CommentType == HL7CommentType.Automatic ? "I" : "G");
        return builder.ToSegmentString();
    }

    private static string MapCommentSource(HL7CommentSource source)
    {
        return source switch
        {
            HL7CommentSource.Prime => "O",
            HL7CommentSource.AllerQ => "A",
            HL7CommentSource.Report => "R",
            HL7CommentSource.ResultInterpretation => "N",
            HL7CommentSource.ImmunoCAPGuide => "L",
            HL7CommentSource.PhadiaXplain => "X",
            HL7CommentSource.PhadiaXplainICAP => "E",
            _ => "O"
        };
    }

    private enum CommentSegmentField
    {
        SegmentTypeId,
        SetId,
        SourceOfComment,
        Comment,
        CommentType
    }
}
