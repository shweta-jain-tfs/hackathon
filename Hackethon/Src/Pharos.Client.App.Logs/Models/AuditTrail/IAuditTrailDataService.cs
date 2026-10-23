namespace Pharos.Client.App.Logs.Models.AuditTrail;
public interface IAuditTrailDataService : IEntityDataService<AuditTrailRecord, int>
{
    Task<string> ExportAuditTrailAsync(DataSourceRequest request);
    Task<List<string>> GetOperatorNamesAsync();
}
