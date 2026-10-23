using Pharos.Server.Domain.Parameters.Methods;

namespace Pharos.Server.Parameters.Application.Queries.QualityControl;

public class MethodQualityControlsOverviewDto
{
    /// <summary>
    /// Unique row id (quality control id).
    /// </summary>
    public int Id { get; set; }

    public int MethodId { get; set; }

    public required string MethodName { get; set; }

    public QualityControlSystemType QcType { get; set; }

    public string QualityControlName { get; set; } = string.Empty;

    public string TestName { get; set; } = string.Empty;

    public int? LotId { get; set; }

    public string? CurrentLot { get; set; }

    public LotType? LimitDefinition { get; set; }

    public string? CurrentIssue { get; set; }
}