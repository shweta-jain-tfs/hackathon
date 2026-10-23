using Pharos.Server.Request.Application.Queries.GetPatient;
using Pharos.Server.Request.Domain;
using Riok.Mapperly.Abstractions;

namespace Pharos.Server.Request.Application.Mapping;

[Mapper]
internal static partial class PatientMapper
{
    [MapperIgnoreTarget(nameof(Patient.Id))]
    [MapperIgnoreTarget(nameof(Patient.Created))]
    [MapperIgnoreTarget(nameof(Patient.CreatedBy))]
    [MapperIgnoreTarget(nameof(Patient.LastModified))]
    [MapperIgnoreTarget(nameof(Patient.LastModifiedBy))]
    [MapperIgnoreTarget(nameof(Patient.Requests))]
    public static partial void UpdatePatientFromDto(PatientDto source, Patient target);
}
