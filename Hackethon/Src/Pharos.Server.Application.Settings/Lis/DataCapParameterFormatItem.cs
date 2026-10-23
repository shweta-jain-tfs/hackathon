using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Application.Settings.Lis;

public record DataCapParameterFormatItem
{
    [DisplayNameLocal("DataCapMessageSettings_DataCapParameterFormatItem_LisChannel")]
    public int LisChannel { get; set; }

    [DisplayNameLocal("DataCapMessageSettings_DataCapParameterFormatItem_ParameterType")]
    public int ParameterType { get; set; }

    [DisplayNameLocal("DataCapMessageSettings_DataCapParameterFormatItem_Code")]
    public string Code { get; set; } = "";

    [DisplayNameLocal("DataCapMessageSettings_DataCapParameterFormatItem_Name")]
    public string Name { get; set; } = "";

    [DisplayNameLocal("DataCapMessageSettings_DataCapParameterFormatItem_ImportParameterNumber")]
    public int ImportParameterNumber { get; set; }

    [DisplayNameLocal("DataCapMessageSettings_DataCapParameterFormatItem_ImportLength")]
    public int ImportLength { get; set; }

    [DisplayNameLocal("DataCapMessageSettings_DataCapParameterFormatItem_ExportParameterNumber")]
    public int ExportParameterNumber { get; set; }

    [DisplayNameLocal("DataCapMessageSettings_DataCapParameterFormatItem_ExportLength")]
    public int ExportLength { get; set; }

    [DisplayNameLocal("DataCapMessageSettings_DataCapParameterFormatItem_RequiredFlag")]
    public bool RequiredFlag { get; set; }

    [DisplayNameLocal("DataCapMessageSettings_DataCapParameterFormatItem_CustomFieldFlag")]
    public bool CustomFieldFlag { get; set; }

    [DisplayNameLocal("DataCapMessageSettings_DataCapParameterFormatItem_VisibleInPharosFlag")]
    public bool VisibleInPharosFlag { get; set; }

    [DisplayNameLocal("DataCapMessageSettings_DataCapParameterFormatItem_RightAlignedInExportFlag")]
    public bool RightAlignedInExportFlag { get; set; }

    [DisplayNameLocal("DataCapMessageSettings_DataCapParameterFormatItem_PaddingCharacter")]
    public string PaddingCharacter { get; set; } = "";

    [DisplayNameLocal("DataCapMessageSettings_DataCapParameterFormatItem_MaxLength")]
    public int MaxLength { get; set; }

    [DisplayNameLocal("DataCapMessageSettings_DataCapParameterFormatItem_MinLength")]
    public int MinLength { get; set; }

    public DataCapParameterFormatItem(string code, int length, bool visibility, bool rightAligned, string paddingChar, string name)
    {
        Code = code;
        ImportLength = length;
        ExportLength = length;
        VisibleInPharosFlag = visibility;
        RightAlignedInExportFlag = rightAligned;
        PaddingCharacter = paddingChar;
        Name = name;
    }

    public DataCapParameterFormatItem()
    {
    }
}