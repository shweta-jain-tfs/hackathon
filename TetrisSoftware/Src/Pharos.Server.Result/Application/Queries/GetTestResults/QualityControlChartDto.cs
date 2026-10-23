namespace Pharos.Server.Result.Application.Queries.GetTestResults;
public class QualityControlChartDto
{
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    public List<QualityControlTestResultDto>? Results { get; set; }
    public List<int>? ActivePoints { get; set; }
}