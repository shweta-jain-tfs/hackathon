using Pharos.Server.Application.Parameters;
using Pharos.Server.Domain.Parameters.Methods;

namespace Pharos.Server.Parameters.Application.Queries.GetMethodsAndTest;

public class MethodTestDetailsDto
{
    public int Id { get; set; }
    public string TestName { get; set; } = string.Empty;
    public string? FullTestName { get; set; }
    public int MethodId { get; set; }
    public string MethodName { get; set; } = string.Empty;
    public int Replicates { get; set; }
    public int MethodConcentrationUnitId { get; set; }
    public string DefaultConcentrationUnit { get; set; } = string.Empty;
    public List<MethodConcentrationUnitOptionDto> ConcentrationUnits { get; set; } = [];
    public string DefaultLabTestName { get; set; } = default!;
    public bool Active { get; set; }
    public bool ReportConcentration { get; set; }
    public bool DefaultReportConcentration { get; set; }
    public bool ReportCutOff { get; set; }
    public bool DefaultReportCutOff { get; set; }
    public QuotientReportMode ReportQuotient { get; set; }
    public QuotientReportMode DefaultReportQuotient { get; set; }
    public bool ReportClass { get; set; }
    public int ReportClassId { get; set; }
    public int DefaultReportClassId { get; set; }
    public List<MethodClassTableOptionDto> ClassTables { get; set; } = [];
    public int DilutionFactor { get; set; }
    public int DefaultDilutionFactor { get; set; }
    public DilutionMethod DilutionMethod { get; set; }
    public DilutionMethod DefaultDilutionMethod { get; set; }
    public bool IsRuo { get; set; }
    public double? CutOffConcentration { get; set; }
    public double? DefaultCutOffConcentration { get; set; }
    public string? TextQuantitativeAbove { get; set; }
    public string? TextQuantitativeBelow { get; set; }
    public string? TextQuantitativeAboveForDilutedTest { get; set; }
    public string? TextQuantitativeBelowForDilutedTest { get; set; }
    public MethodTestPriorityDto? Priority { get; set; }
    public bool PreventDilutionFactorsBelowDefault { get; set; }
    public AssayTechnology Technology { get; set; }
    public List<string> LaboratoryTestNamesDuplicatedAcrossMethods { get; set; } = [];

    /// <summary>
    /// The date and time when the method test details were queried. This is used to merge concurrent edits.
    /// </summary>
    public DateTimeOffset QueriedAt { get; set; }

    public List<MethodTestLabNameDto>? MethodTestLabNames { get; set; }
}

public class MethodConcentrationUnitOptionDto
{
    public int UnitId { get; set; }
    public string ConcentrationUnit { get; set; } = string.Empty;
}

public class MethodClassTableOptionDto
{
    public int ClassTableId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}

public class MethodTestPriorityDto
{
    public bool IncludeInPriorityGroupA { get; set; }
    public int? OrderInPriorityGroupA { get; set; }
    public bool IncludeInPriorityGroupB { get; set; }
    public int? OrderInPriorityGroupB { get; set; }
}
