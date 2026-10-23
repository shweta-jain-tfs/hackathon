using Microsoft.AspNetCore.Authorization;
using Pharos.Client.App.Logs.Views.Errors;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.App.Layout.Instrument;
using Pharos.Client.Common.App.State.CurrentInstrument;
using Pharos.Common.Web.Components.Modal;
using Pharos.Common.Web.Components.Toaster;

namespace Pharos.Client.Views;

[Authorize]
public class GlobalMessageSection : ComponentBase
{
    [Inject] public IModalService ModalService { get; set; } = null!;
    [Inject] public ICurrentInstrumentViewModel InstrumentModel { get; set; } = null!;
    [Inject] private IToasterService ToasterService { get; set; } = null!;

    private readonly List<IModalReference> _modalRefs = [];


    protected override void OnInitialized()
    {
        InstrumentModel.OnFatalErrorChanged += Instrument_OnFatalErrorChanged;
        InstrumentModel.OnCurrentInstrumentUnknownBarcodeScanned += HandleUnknownBarcodeScanned;

    }

    private void HandleUnknownBarcodeScanned(string barcode)
    {
        ToasterService.ShowToast(ToastType.Warning, $"Unknown barcode scanned: {barcode}");
    }
    private void Instrument_OnFatalErrorChanged(List<int> ids)
    {
        List<IModalReference> modalRefs = [.. _modalRefs];
        _modalRefs.Clear();

        for (var i = 0; i < ids.Count; i++)
        {
            var parameters = new ModalParameters
            {
                { nameof(SystemErrorDetailsView.Id), ids[i] },
                { nameof(SystemErrorDetailsView.CloseOnResponseSubmitted), true }
            };

            _modalRefs.Add(
               ModalService.Show<SystemErrorDetailsView>(new ModalOptions
               {
                   Title = $"Error (1 of {i + 1})",
                   ModalHeight = ModalSize.XLarge,
                   ModalWidth = ModalSize.Large,
                   Status = ModalStatus.Alarm,
                   HeaderActions = builder =>
                   {
                       builder.OpenComponent<MuteSoundButton>(0);
                       builder.CloseComponent();
                   }
               }
                   , parameters)
           );
        }


        if (modalRefs is { Count: > 0 })
        {
            foreach (var modalRef in modalRefs)
            {
                ModalService.CloseModal(modalRef);
            }
        }
    }
}
