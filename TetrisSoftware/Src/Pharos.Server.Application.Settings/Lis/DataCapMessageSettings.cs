using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Application.Settings.Lis;

public record DataCapMessageSettings
{
    [DisplayNameLocal("DataCapMessageSettings_FormatDefinitions")]
    public List<DataCapParameterFormatItem> FormatDefinitions { get; set; } = [];

    [DisplayNameLocal("DataCapMessageSettings_FixedSizeFields")]
    public bool FixedSizeFields { get; set; }

    [DisplayNameLocal("DataCapMessageSettings_HeaderFixedSizeFields")]
    public bool HeaderFixedSizeFields { get; set; }

    [DisplayNameLocal("DataCapMessageSettings_HeaderDelimiter")]
    public string HeaderDelimiter { get; set; }

    [DisplayNameLocal("DataCapMessageSettings_DataSectionDelimiter")]
    public string DataSectionDelimiter { get; set; }

    public DataCapMessageSettings()
    {
        FixedSizeFields = false;
        HeaderFixedSizeFields = false;
        HeaderDelimiter = ";";
        DataSectionDelimiter = ";";
        FormatDefinitions = new List<DataCapParameterFormatItem>
        {
            new("sample", 16, true, false, "", "sample"),
            new("cap", 10, true, false, "", "cap"),
            new("set", 5, true, false, "", "set"),
            new("dil", 5, true, false, "", "dil"),
            new("rep", 2, true, false, "", "rep"),
            new("dose", 7, true, false, "", "dose"),
            new("judge", 10, true, false, "", "judge"),
            new("method", 4, true, false, "", "method"),
            new("status", 2, true, false, "", "status"),
            new("patient", 24, true, false, "", "patient"),
            new("gender", 2, true, false, "", "gender"),
            new("bthdt", 6, true, false, "", "bthdt"), // TODO: set default length to 7 when Japanese supported
            new("age", 3, true, false, "", "age"),
            new("patid", 12, true, false, "", "patid"),
            new("carte", 12, true, false, "", "carte"),
            new("reqno", 12, true, false, "", "reqno"),
            new("section", 4, true, false, "", "section"),
            new("sectnam", 16, true, false, "", "sectnam"),
            new("ward", 4, true, false, "", "ward"),
            new("wardnam", 24, true, false, "", "wardnam"),
            new("doctor", 10, true, false, "", "doctor"),
            new("doctnam", 16, true, false, "", "doctnam"),
            new("smpdt", 6, true, false, "", "smpdt"),
            new("reqdt", 6, true, false, "", "reqdt"),
            new("rptdt", 6, true, false, "", "rptdt"),
            new("pntknd", 2, true, false, "", "pntknd"),
            new("idil", 5, true, false, "", "idil"),
            new("cdil", 5, true, false, "", "cdil"),
            new("hosp", 10, true, false, "", "hosp"),
            new("hospnam", 24, true, false, "", "hospnam"),
            new("rack", 15, true, false, "", "rack"),
            new("rackpos", 2, true, false, "", "rackpos"),
            new("tubetyp", 1, true, false, "", "tubetyp"),
            new("sampseq", 5, true, false, "", "sampseq"),
            new("measdt", 6, true, false, "", "measdt"),
            new("ru", 8, true, false, "", "ru"),
            new("cutoff", 10, true, false, "", "cutoff"),
            new("cv", 8, true, false, "", "cv"),
            new("conjlot", 5, true, false, "", "conjlot"),
            new("instrid", 2, true, false, "", "instrid"),
            new("arid", 19, true, false, "", "arid"),
            new("qcccid", 16, true, false, "", "qcccid"),
            new("qclot", 5, true, false, "", "qclot"),
            new("rtrayid", 4, true, false, "", "rtrayid"),
            new("reqprio", 4, true, false, "", "reqprio"),
            new("irwpos", 3, true, false, "", "irwpos"),
            new("erwpos", 3, true, false, "", "erwpos")
        };
    }
}