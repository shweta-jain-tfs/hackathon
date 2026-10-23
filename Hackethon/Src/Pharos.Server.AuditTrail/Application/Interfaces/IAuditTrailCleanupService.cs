namespace Pharos.Server.AuditTrail.Application.Interfaces;
public interface IAuditTrailCleanupService
{
    Task<bool> AuditTrailCleanupAsync();
}