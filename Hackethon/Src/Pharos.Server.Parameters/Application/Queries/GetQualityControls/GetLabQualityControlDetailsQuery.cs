using MediatR;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Infrastructure;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Queries.GetQualityControls;

internal record GetLabQualityControlDetailsQuery(int Id) : IRequest<LabQualityControlDetailDto>;

public class LabQualityControlDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int TubeTypeId { get; set; }
    public string IdOnBarcode { get; set; } = string.Empty;
    public int? BarcodeFormatId { get; set; }
    public List<LabQualityControlTestDto> LabQualityControlTests { get; set; } = [];

    /// <summary>
    /// The date and time when the QC details were queried. This is used to merge concurrent edits.
    /// </summary>
    public DateTimeOffset QueriedAt { get; set; }
}

public class LabQualityControlTestDto
{
    public int Id { get; set; }
    public string MethodName { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public int TestId { get; set; }
    public int Replicates { get; set; }
    public double ReplicateCvLimit { get; set; }
    public int PreDilutionFactor { get; set; }
    public int InstrumentDilutionFactor { get; set; }
    public bool ApplyAcceptanceRange { get; set; }
    public bool UseAcceptanceRange { get; set; }
    public double MaxCv { get; set; }
    public int NumberOfSd { get; set; }
    public double? UpperLimit { get; set; }
    public double? LowerLimit { get; set; }
    public bool UseLotQualification { get; set; }
    public int NumberOfTests { get; set; }
    public QualityControlType QualityControlType { get; set; }
}

internal class GetLabQualityControlDetailsQueryHandler(IParametersDbContext dbContext, IDateTime dateTime) : IRequestHandler<GetLabQualityControlDetailsQuery, LabQualityControlDetailDto>
{
    public async Task<LabQualityControlDetailDto> Handle(GetLabQualityControlDetailsQuery request, CancellationToken cancellationToken)
    {
        var qc = await dbContext.LabQualityControls
            .Include(x => x.Tests)
            .ThenInclude(x => x.Test)
            .ThenInclude(x => x.Method)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        if (qc == null)
        {
            throw new NotFoundException("Lab Quality Control not found.");
        }

        return new LabQualityControlDetailDto
        {
            Id = qc.Id,
            Name = qc.Name,
            IdOnBarcode = qc.IdOnBarcode,
            BarcodeFormatId = qc.BarcodeFormatId,
            TubeTypeId = qc.TubeTypeId,
            QueriedAt = dateTime.Now,
            LabQualityControlTests = qc.Tests.OfType<LabQualityControlTest>().Select(test => new LabQualityControlTestDto
            {
                Id = test.Id,
                MethodName = test.Test.Method.MethodName,
                TestName = test.Test.TestName,
                TestId = test.Test.Id,
                Replicates = test.Replicates,
                ReplicateCvLimit = test.ReplicateCvLimit,
                PreDilutionFactor = test.PreDilutionFactor,
                InstrumentDilutionFactor = test.InstrumentDilutionFactor,
                ApplyAcceptanceRange = test.ApplyAcceptanceRange,
                UseAcceptanceRange = test.UseAcceptanceRange,
                MaxCv = test.MaxCv,
                NumberOfSd = test.NumberOfSd,
                UpperLimit = test.UpperLimit,
                LowerLimit = test.LowerLimit,
                UseLotQualification = test.UseLotQualification,
                NumberOfTests = test.NumberOfTests,
                QualityControlType = test.QualityControlType
            }).ToList()
        };
    }
}