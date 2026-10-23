using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Settings.Helpers;

/// <summary>
/// FOR TESTING
/// </summary>
[RootConfigSection(sortOrder: 1)]
[DisplayNameLocal("Complex Label")]
internal class ComplexSettingsTest
{
    [SettingsRange(0, 100)]
    [DisplayNameLocal("LabelName")]
    public int SomeNumber { get; set; }

    [DisplayNameLocal("TestLabel")]
    public bool PreviewEnabled { get; set; } = true;

    [DependsFrom("PreviewEnabled", "false", DependencyType.Disable)]
    [DisplayNameLocal("ReportFolder")]
    public string ReportFolder { get; set; } = "";

    [DisplayNameLocal("Page Orientation")]
    public PageOrientation DefaultPageOrientation { get; set; } = PageOrientation.Portrait;

    [DisplayNameLocal("DueByDate")]
    public DateTimeOffset DueByDate { get; set; } = DateTimeOffset.Now;

    [DependsFrom("PreviewEnabled", "true", DependencyType.Hide)]
    [DisplayNameLocal("LabReport")]
    public LabReport? LabReport { get; set; }

    [DisplayNameLocal("LabReportPage")]
    public LabReport? LabReportPage { get; set; }

    [DisplayNameLocal("SubPage")]
    public SubPage? SubPage { get; set; }

    [DisplayNameLocal("AfterClass")]
    public int AfterClass { get; set; }

    [DisplayNameLocal("Test Table")]
    public IEnumerable<MethodReportComment> MethodReportComments { get; set; }
        = new List<MethodReportComment> {
            new ()
        {
            MethodName = "Default Name",
            MethodComment = "Default Comment",
            ConcentrationComment = "Default Comment",
            Test = 2,
        } };

    [DisplayNameLocal("OtherMethodReportComments")]
    public IEnumerable<MethodReportComment> OtherMethodReportComments { get; set; } = new List<MethodReportComment>();
}

internal class MethodReportComment
{


    [DisplayNameLocal("Method Name")]
    public string MethodName { get; set; } = "Def Name";
    [DisplayNameLocal("Method Comment")]
    public string? MethodComment { get; set; }
    [DisplayNameLocal("Concentration Comment")]
    public string? ConcentrationComment { get; set; }
    [DisplayNameLocal("Test int")]
    public int Test { get; set; } = 9;
    //public LabReport labtest { get; set; }

    [DisplayNameLocal("Name")]
    public string Name { get; set; } = "";
}

internal class LabReport
{
    [DisplayNameLocal("DisableLine")]
    public bool DisableLine { get; set; } = true;

    [DisplayNameLocal("GeneratePdfWhenApproved")]
    public bool GeneratePdfWhenApproved { get; set; }

    [DependsFrom("GeneratePdfWhenApproved", "false", DependencyType.Hide)]
    [DisplayNameLocal("IncludeCalibrators")]
    public bool IncludeCalibrators { get; set; } = true;
}

internal class SubPage
{
    [DisplayNameLocal("Name")]
    public string Name { get; set; } = "SubPage1";

    [DisplayNameLocal("LabReport")]
    public LabReport? LabReport { get; set; }

    [DisplayNameLocal("MethodReportComments")]
    public IEnumerable<MethodReportComment> MethodReportComments { get; set; }
       = new List<MethodReportComment> {
            new ()
        {
            MethodName = "Default Name",
            MethodComment = "Default Comment",
            ConcentrationComment = "Default Comment",
            Test = 2,
        } };
}

internal enum PageOrientation
{
    [DisplayNameLocal("PageOrientation_Portrait")]
    Portrait = 1,

    [DisplayNameLocal("PageOrientation_Landscape")]
    Landscape = 2
}