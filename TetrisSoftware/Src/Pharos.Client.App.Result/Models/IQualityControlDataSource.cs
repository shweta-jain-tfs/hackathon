namespace Pharos.Client.App.Result.Models;

public interface IQualityControlDataSource : IEntityDataSource<QualityControlTestResult>
{
    public int AnalyticalRunId { get; set; }
    public Task<QualityControlChart> GetQualityControls(int analyticalRunId, string qualityControlName, string testName, int points);
    Task UpdateQualityControlAsync(QualityControlTestResult resultData);
}