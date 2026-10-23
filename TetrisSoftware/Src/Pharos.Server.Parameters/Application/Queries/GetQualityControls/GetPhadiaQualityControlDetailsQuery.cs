using MediatR;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Queries.GetQualityControls;

internal record GetPhadiaQualityControlDetailsQuery(int Id) : IRequest<PhadiaQualityControlDetailDto>;

public class PhadiaQualityControlDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int TubeTypeId { get; set; }
    public bool IsQcInTube { get; set; }
    public List<PhadiaQualityControlTestDto> PhadiaQualityControlTests { get; set; } = [];
}

public class PhadiaQualityControlTestDto
{
    public int Id { get; set; }
    public string MethodName { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public int Replicates { get; set; }
    public double ReplicateCvLimit { get; set; }
    public int PreDilutionFactor { get; set; }
    public int InstrumentDilutionFactor { get; set; }
    public bool ApplyAcceptanceRange { get; set; }
    public bool UseAcceptanceRange { get; set; }
    public double MaxCv { get; set; }
    public int NumberOfSd { get; set; }
    public bool UseLotQualification { get; set; }
    public int NumberOfTests { get; set; }
    public QualityControlType QualityControlType { get; set; }
}

internal class GetPhadiaQualityControlDetailsQueryHandler : IRequestHandler<GetPhadiaQualityControlDetailsQuery, PhadiaQualityControlDetailDto>
{
    private readonly IParametersDbContext _dbContext;

    public GetPhadiaQualityControlDetailsQueryHandler(IParametersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PhadiaQualityControlDetailDto> Handle(GetPhadiaQualityControlDetailsQuery request, CancellationToken cancellationToken)
    {
        var qc = await _dbContext.PhadiaQualityControl
            .Include(x => x.Tests)
            .ThenInclude(x => x.Test)
            .ThenInclude(x => x.Method)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (qc == null)
        {
            throw new KeyNotFoundException("Phadia Quality Control not found.");
        }

        return new PhadiaQualityControlDetailDto
        {
            Id = qc.Id,
            Name = qc.Name,
            TubeTypeId = qc.TubeTypeId,
            IsQcInTube = qc.IsQcInTube,
            PhadiaQualityControlTests = qc.Tests.Select(t => new PhadiaQualityControlTestDto
            {
                Id = t.Id,
                MethodName = t.Test.Method.MethodName,
                TestName = t.Test.TestName,
                Replicates = t.Replicates,
                ReplicateCvLimit = t.ReplicateCvLimit,
                PreDilutionFactor = t.PreDilutionFactor,
                InstrumentDilutionFactor = t.InstrumentDilutionFactor,
                QualityControlType = t.QualityControlType,
                ApplyAcceptanceRange = t.ApplyAcceptanceRange,
                UseAcceptanceRange = t.UseAcceptanceRange,
                MaxCv = t.MaxCv,
                NumberOfSd = t.NumberOfSd,
                UseLotQualification = t.UseLotQualification,
                NumberOfTests = t.NumberOfTests
            }).ToList()
        };
    }
}