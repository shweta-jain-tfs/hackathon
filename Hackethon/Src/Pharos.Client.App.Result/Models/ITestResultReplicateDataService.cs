namespace Pharos.Client.App.Result.Models;

public interface ITestResultReplicateDataService
{
    Task<ICollection<TestReplicateDetail>> GetTestReplicateDetails(int[] ids);
}