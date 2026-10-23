using Pharos.Server.Domain.Common;

namespace Pharos.Server.Request.Application.Queries.GetPatient;

public class PatientDto
{
    [ProtectedData] public string? Name { get; set; } = string.Empty;
    public Gender Gender { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string? PatientNumber { get; set; } = string.Empty;
    public string? MedicalRecordNumber { get; set; } = string.Empty;
    public string? PatientId { get; set; } = string.Empty;
    [ProtectedData] public string? Address { get; set; } = string.Empty;
    public string? ZipCode { get; set; } = string.Empty;
    public string? Diagnosis { get; set; } = string.Empty;
    [ProtectedData] public string? Comment { get; set; } = string.Empty;
}