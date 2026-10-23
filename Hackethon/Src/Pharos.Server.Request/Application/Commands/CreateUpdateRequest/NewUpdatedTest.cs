namespace Pharos.Server.Request.Application.Commands.CreateUpdateRequest;

/// <summary>
/// Represents a test order that is being created or updated as part of a request.
/// This class is used for both creating new tests and updating existing tests,
/// which is why it contains properties that are relevant for both scenarios
/// (e.g., Id for updates).
/// </summary>
public class NewUpdatedTest
{
    public int Id { get; set; }
    public int InstrumentDilutionFactor { get; set; }
    public bool IsReset { get; set; }
    public string? LabTestName { get; set; }
    public string MethodName { get; set; } = null!;
    public int NumberOfReplicates { get; set; }
    public List<int>? ReflexDefinitionIds { get; set; }
    public string TestName { get; set; } = null!;
    public List<int> TestPanelIds { get; set; } = null!;
    public int TestSortOrder { get; set; }
    public bool IsActive { get; set; }
}