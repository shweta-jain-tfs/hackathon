using Pharos.Client.App.Request.Models;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Common.Web.Components.Modal;
using Pharos.Contracts.Security;

namespace Pharos.Client.App.Request.Services;

internal sealed class SampleScannedBarcodeService(NavigationManager navigationManager, ModalState modalState, IRequestDetailsDataService requestDetailsDataService, IAuthorizationService authorizationService) : IScannedBarcodeService
{
    public async Task HandleScannedBarcode(ScannedBarcode scannedBarcode, CancellationToken ct = default)
    {
        if (modalState.HasOpenModals())
        {
            return;
        }

        if (await authorizationService.IsAuthorized(Permissions.ManageSamples))
        {
            await requestDetailsDataService.QueryRefreshExistingRequestAsync(scannedBarcode.Barcode);
        }

        navigationManager.NavigateTo($"{PageRoute.SampleDetails.Default}/{scannedBarcode.RequestId}");
    }
}
