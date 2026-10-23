using System.ComponentModel.DataAnnotations;
using Pharos.Server.Domain.Enums;

namespace Pharos.Server.Domain.Parameters.Methods;

#pragma warning disable CS8618
public class PhadiaQualityControlTestForParser
{
    [MaxLength(Constants.QualityControlNameLength)]
    public string QualityControlName { get; set; }

    public QualityControlType QualityControlType { get; set; }

    public int PreDilutionFactor { get; set; }

    public int? InstrumentDilutionFactor { get; set; }

    [MaxLength(Constants.TestNameLength)]
    public string TestName { get; set; }

    [MaxLength(Constants.MethodNameLength)]
    public string MethodName { get; set; }

    //SortOrder only exists to be able to export the tests in the same order that they were imported
    public int SortOrder { get; set; }
}