using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.Drawer;

public partial class PhSampleTray : ComponentBase
{
    #region Properties

    [Parameter]
    public string Barcode { get; set; } = string.Empty;

    [Parameter]
    public bool IsTrayAbsent { get; set; } = false;

    [Parameter]
    public ICollection<SampleTube>? SampleTubes { get; set; }

    [Parameter]
    public bool IsTrayDim { get; set; } = false;

    [Parameter]
    public bool IsTrayHighLight { get; set; } = false;

    [Parameter]
    public TrayBarcodePosition BarcodePosition { get; set; } = default!;

    [Parameter]
    public bool IsTraySelected { get; set; }

    [Parameter]
    public bool IsScanningOnGoing { get; set; } = default!;

    [Parameter]
    public bool IsTrayError { get; set; } = default;

    [Parameter]
    public string TrayErrorName {  get; set; } = default!;

    [Parameter]
    public EventCallback OnTraySelected { get; set; }

    public string SampleTrayBarcodeCss { get; set; } = string.Empty;
    public string SampleTrayCss { get; set; } = string.Empty;
    public string SampleTrayContainerCss { get; set; } = string.Empty;
    public string SampleTraySelectionFrameCss { get; set; } = string.Empty;
    private bool HideBarcode => BarcodePosition == TrayBarcodePosition.None;

    #endregion

    #region Life Cycle Events

    protected override void OnParametersSet()
    {
        SetSampleTrayCss();
        SetSampleTrayContainerCss();
        SetSampleTrayBarcodePositionCss();
        SetSampleTraySelectedCss();
    }

    #endregion

    #region private methods

    private void SetSampleTrayCss()
    {
        if (IsScanningOnGoing)
        {
            SampleTrayCss = new CssBuilder("ph-tray")
               .AddClass("ph-tray--scan")
               .Build();

            IsTrayAbsent = false;
            SampleTubes = default;
        }
        else
        {
            SampleTrayCss = new CssBuilder("ph-tray")
            .AddClass("ph-tray--empty", IsTrayAbsent)
            .AddClass("ph-tray--filled", !IsTrayAbsent && !IsTrayError)
            .AddClass("ph-tray--error-filled", IsTrayError)
            .Build();
        }

    }
    private void SetSampleTrayContainerCss()
    {
        SampleTrayContainerCss = new CssBuilder("")
            .AddClass("ph-tray__content", !IsTrayAbsent && !IsTrayError)
            .AddClass("ph-tray__error-content", ShowTrayError())
            .Build();
    }

    private void SetSampleTraySelectedCss()
    {
        SampleTraySelectionFrameCss = new CssBuilder("")
            .AddClass("ph-tray__selection-frame", IsTraySelectable())
            .AddClass("selected", IsSampleTraySelected())
            .Build();
    }

    private bool IsTraySelectable()
    {
        return !IsTrayAbsent && !IsScanningOnGoing && !IsTrayError;
    }

    private bool IsSampleTraySelected()
    {
        return IsTraySelectable() && IsTraySelected && HideBarcode;
    }

    private void SetSampleTrayBarcodePositionCss()
    {
        SampleTrayBarcodeCss = new CssBuilder("ph-tray__barcode-container")
           .AddClass("ph-tray__barcode-container--hidden", HideBarcode)
           .AddClass("ph-tray__barcode-container--dim", IsTrayDim)
           .AddClass("ph-tray__barcode-container--highlight", IsTrayHighLight)
           .AddClass("ph-tray__barcode-container--error", IsTrayError)
           .Build();
    }
    private async Task OnTrayClicked()
    {
        if (!IsTrayAbsent && !IsScanningOnGoing && !IsTrayError)
        {
            await OnTraySelected.InvokeAsync();
        }
    }
    private bool ShowTrayError()
    {
        return IsTrayError && !IsScanningOnGoing;
    }

    #endregion
}

public class SampleTube
{
    public TubeStatus Status { get; set; }

    public string SampleId { get; set; } = string.Empty;

    public string TubeBarcode { get; set; } = string.Empty;

    public int? Position { get; set; }

    public bool IsHighPriority { get; set; }

    public bool IsUnloadRequested { get; set; }

    public TubePositionSource PositionSource { get; set; }

    public TubeError TubeError { get; set; } = TubeError.None;

    public SampleType SampleType { get; set; } = SampleType.None;
    public string SampleIdDisplay { get; set; } = string.Empty;
}

public enum TrayBarcodePosition
{
    None,
    Left,
    Right
}