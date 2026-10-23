using MediatR;
using Pharos.Server.Application.Reagents;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Queries.GetTestReplicateDetails;

public record GetTestReplicateDetailsQuery(int[] Ids) : IRequest<List<TestReplicateDetailDto>>;

public record TestReplicateDetailDto
{
    public int Id { get; init; }
    public int ReplicateNumber { get; init; }
    public DateTimeOffset MeasurementDateTime { get; init; }
    public List<ReplicateErrorDto> Errors { get; init; } = [];
    public List<ReplicateReagentDetailDto> Reagents { get; init; } = [];
}

public record ReplicateErrorDto
{
    public int ErrorCode { get; init; }
}

public record ReplicateReagentDetailDto
{
    public ArticleType ReagentType { get; init; }
    public string LotNumber { get; init; } = string.Empty;
    public bool Expired { get; init; }
    public DateTimeOffset ExpiryDate { get; set; }
    public string Factor { get; set; } = string.Empty;
}

internal sealed class GetTestReplicateDetailsQueryHandler(IResultDbContext context, IReagentBarcodeService barcodeService)
    : IRequestHandler<GetTestReplicateDetailsQuery, List<TestReplicateDetailDto>>
{
    public async Task<List<TestReplicateDetailDto>> Handle(GetTestReplicateDetailsQuery request, CancellationToken cancellationToken)
    {
        var replicates = await context.TestReplicates
            .Where(x => request.Ids.Contains(x.Id))
            .Include(x => x.TestReplicateErrors)
            .Include(x => x.TestReplicateReagents)
            .ToListAsync(cancellationToken);

        return replicates.Select(x => new TestReplicateDetailDto
        {
            Id = x.Id,
            ReplicateNumber = x.ReplicateNumber,
            MeasurementDateTime = x.MeasurementDateTime,
            Errors = x.TestReplicateErrors!.Select(error => new ReplicateErrorDto { ErrorCode = error.ErrorCode }).ToList(),
            Reagents = x.TestReplicateReagents!.Select(MapReagents).ToList()
        }).ToList();
    }

    private ReplicateReagentDetailDto MapReagents(TestReplicateReagent reagent)
    {
        var item = new ReplicateReagentDetailDto
        {
            ReagentType = reagent.ReagentType,
            LotNumber = reagent.LotNumber,
            Expired = reagent.Expired
        };

        var reagentInfo = barcodeService.GetReagentLotInformation(reagent.ReagentType, reagent.Barcode, reagent.Udi);

        if (reagentInfo != null)
        {
            item.ExpiryDate = reagentInfo.ExpiryDate;

            if (reagentInfo is EliaCalibratorWellLotInformation calibratorInfo)
            {
                item.Factor = calibratorInfo.CorrectionFactorText;
            }
            else if (reagentInfo is EliaWellLotInformation eliaWellInfo)
            {
                item.Factor = eliaWellInfo.CorrectionFactorText;
            }
        }

        return item;
    }
}