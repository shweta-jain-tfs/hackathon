using Pharos.Client.Common.Models;

namespace Pharos.Client.App.SystemUpdate.Models;

public interface ISystemUpdateDataService
{
    Task<List<string>> GetAvailableUpdates();
    Task InitiateUpdate();
    Task AcknowledgeUpdate();

    Task<SystemUpdateOutcome> GetUnacknowledgedOutcome();
}