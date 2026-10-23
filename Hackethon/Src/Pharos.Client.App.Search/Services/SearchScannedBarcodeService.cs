using Pharos.Client.App.Search.Models;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.App.PageRouting;
using Pharos.Common.Web.Components.Modal;
using Pharos.Contracts.Security;

namespace Pharos.Client.App.Search.Services;

internal class SearchScannedBarcodeService(NavigationManager navigationManager, ModalState modalState, ISearchResultDataSource searchResultDataSource, IAuthorizationService authorizationService) : IScannedBarcodeService
{
    public async Task HandleScannedBarcode(ScannedBarcode scannedBarcode, CancellationToken ct = default)
    {
        if (modalState.HasOpenModals())
        {
            return;
        }

        if (IsOnRequestPage()
            && scannedBarcode.CanQueryMissingRequest
            && await authorizationService.IsAuthorized(Permissions.ManageSamples))
        {
            await searchResultDataSource.QueryAskMissingRequestAsync(scannedBarcode.Barcode);
        }
        else
        {
            navigationManager.NavigateTo($"{PageRoute.Search.Default}?search={Uri.EscapeDataString(scannedBarcode.Barcode)}");
        }
    }

    private bool IsOnRequestPage()
    {
        var relativePath = navigationManager.ToBaseRelativePath(navigationManager.Uri);

        return relativePath.StartsWith(PageRoute.Requests, StringComparison.OrdinalIgnoreCase)
            || relativePath.StartsWith(PageRoute.SampleDetails.Default.Split('/')[0], StringComparison.OrdinalIgnoreCase);
    }
}