using MediatR;
using Pharos.Server.Application.Queries;
using Pharos.Server.Request.Application.Interfaces;
using Pharos.Server.Request.Domain;

namespace Pharos.Server.Request.Application.Queries.GetPatient;

public class GetPatientQuery : SortedPartialQuery<PatientDto>
{
    public string? PatientId { get; set; }
}

internal sealed class GetPatientQueryHandler : PartialQueryHandlerBase<GetPatientQuery, Patient, PatientDto>, IRequestHandler<GetPatientQuery, PartialQueryResult<PatientDto>>
{
    private readonly IRequestDbContext _context;

    public GetPatientQueryHandler(IRequestDbContext context)
    {
        _context = context;

    }
    public async Task<PartialQueryResult<PatientDto>> Handle(GetPatientQuery request, CancellationToken cancellation)
    {
        var query = _context.Patients;

        var result = await GetPartialQueryResultAsync(query, request, cancellation);

        return result;
    }

    protected override IQueryable<Patient> AddFiltering(IQueryable<Patient> query, GetPatientQuery request)
    {
        query = query.Where(x => x.PatientId == request.PatientId);

        return query;
    }

    protected override IQueryable<Patient> AddSorting(IQueryable<Patient> query, string? sortProperty, bool ascending)
    {
        return query;
    }

    protected override IQueryable<PatientDto> ApplyProjection(IQueryable<Patient> query)
    {
        return query.Select(p => new PatientDto
        {
            Name = p.Name,
            Gender = p.Gender,
            BirthDate = p.BirthDate,
            PatientNumber = p.PatientNumber,
            MedicalRecordNumber = p.MedicalRecordNumber,
            PatientId = p.PatientId,
            Address = p.Address,
            ZipCode = p.ZipCode,
            Diagnosis = p.Diagnosis,
            Comment = p.Comment
        });
    }
}
