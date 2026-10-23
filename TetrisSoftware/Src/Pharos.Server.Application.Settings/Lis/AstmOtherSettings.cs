using System.Text;
using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Application.Settings.Lis;

public class AstmOtherSettings
{
    /// <summary>
    /// Gets the name of encoding for the message text.
    /// </summary>
    [DisplayNameLocal("AstmOtherSettings_EncodingName")]
    public string EncodingName
    {
        get => Encoding.WebName;
        set
        {
            var encoding = Encoding.GetEncoding(value);
            if (!encoding.Equals(Encoding.UTF8) && !encoding.Equals(Encoding.Unicode))
            {
                throw new ArgumentException("ASTM messages can only have UTF-8 or UTF-16 text encoding.");
            }
            Encoding = encoding;
        }
    }

    /// <summary>
    /// Gets the encoding for the message text.
    /// </summary>
    public Encoding Encoding { get; private set; } = Encoding.UTF8;

    [DisplayNameLocal("AstmOtherSettings_PatientNameInTwoFields")]
    public bool PatientNameInTwoFields { get; set; }

    [DisplayNameLocal("AstmOtherSettings_CombineRequestorAddressLinesOneToThree")]
    public bool CombineRequestorAddressLinesOneToThree { get; set; }

    [DisplayNameLocal("AstmOtherSettings_NonUniqueOrderingPhysicianCode")]
    public bool NonUniqueOrderingPhysicianCode { get; set; }

    [DisplayNameLocal("AstmOtherSettings_AllowExtraLongPatientComments")]
    public bool AllowExtraLongPatientComments { get; set; }
}
