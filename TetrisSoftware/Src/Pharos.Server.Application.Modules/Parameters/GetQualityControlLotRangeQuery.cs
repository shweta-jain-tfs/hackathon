using MediatR;

namespace Pharos.Server.Application.Parameters;
/// <summary>
/// Gets lot-specific judgement criteria for a quality control.
/// If <see cref="QualityControlBarcode"/> is not provided (which means th lot number cannot be extracted), the result will contain the default QC range.
/// </summary>
public record GetQualityControlLotRangeQuery(string MethodName, string TestName, string QualityControlName, string? QualityControlBarcode) : IRequest<QualityControlLotRangeDto?>;
