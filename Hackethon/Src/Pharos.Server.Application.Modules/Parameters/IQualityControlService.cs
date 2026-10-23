namespace Pharos.Server.Application.Parameters;
public interface IQualityControlService
{
    Task<IReadOnlyList<PhadiaQualityControlDto>> GetPhadiaQualityControlTests(string articleNumber, CancellationToken cancellationToken);
    Task<bool> CheckIfQcLotExistsForTest(int qualityControlId, string lotNumber, CancellationToken cancellationToken);
}
