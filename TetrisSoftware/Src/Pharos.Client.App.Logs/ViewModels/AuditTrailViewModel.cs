using Pharos.Client.App.Logs.Models.AuditTrail;
using Pharos.Common.Web.Models.DataGrid;
using Pharos.Client.Common.ViewModels.Interfaces;

namespace Pharos.Client.App.Logs.ViewModels;

public class AuditTrailViewModel(IAuditTrailDataService auditTrailDataService, IExceptionHandlerService exceptionHandler)
    : VirtualGridViewModel<AuditTrailRecord, int>(auditTrailDataService, exceptionHandler)
{
    private DataSourceRequest? _lastDataSourceRequest;

    public new async ValueTask<GridItemsProviderResult<AuditTrailRecord>> ProvideGridItems(GridItemsProviderRequest<AuditTrailRecord> request)
    {
        _lastDataSourceRequest = ToDataSourceRequest(request);
        return await base.ProvideGridItems(request);
    }

    public async Task<string> ExportAuditTrailAsync()
    {
        DataSourceRequest request;

        if (_lastDataSourceRequest != null)
        {
            // Export with same filters and sorting as the grid
            request = new DataSourceRequest
            {
                StartIndex = 0,
                Filters = _lastDataSourceRequest.Filters
            };
        }
        else
        {
            // No grid loaded yet - export all records with no filters
            request = new DataSourceRequest
            {
                StartIndex = 0,
                Filters = []
            };
        }

        return await auditTrailDataService.ExportAuditTrailAsync(request);
    }

    public async Task<List<string>> GetOperatorNamesAsync()
    {
        return await auditTrailDataService.GetOperatorNamesAsync();
    }
}